using System.Text.RegularExpressions;
using IdentityDocument.Api.Domain;
using IdentityDocument.Api.Ocr;

namespace IdentityDocument.Api.Extraction;

/// <summary>
/// Turns raw OCR lines into a clean list of extracted fields by applying a
/// document-definition (per field: regex / line heuristic + normalizer).
/// Kept free of persistence concerns so it stays reusable for document #2.
/// </summary>
public sealed class DocumentExtractor
{
    private static readonly Regex QidPattern = new(@"[234]\d{2}\s?\d{4}\s?\d{4}", RegexOptions.Compiled);
    private static readonly Regex DatePattern = new(@"\d{1,2}/\d{1,2}/\d{2,4}|\d{1,2}/\d{2,4}", RegexOptions.Compiled);

    public IReadOnlyList<ExtractedField> Extract(DocumentDefinition definition, IReadOnlyList<OcrLine> lines)
    {
        var fields = new List<ExtractedField>(definition.Fields.Count);
        foreach (var fieldDef in definition.Fields)
        {
            var field = fieldDef.Kind switch
            {
                "regex" => ExtractRegexField(fieldDef, lines),
                "textLine" => ExtractTextLineField(fieldDef, lines),
                _ => Missing(fieldDef)
            };
            fields.Add(field);
        }
        return fields;
    }

    /// <summary>Overall confidence = mean of per-field confidences (missing fields count as 0).</summary>
    public static double ComputeOverall(IReadOnlyList<ExtractedField> fields) =>
        fields.Count == 0
            ? 0.0
            : Math.Round(fields.Average(f => f.Confidence), 3);

    // ---------- regex fields ----------

    private static ExtractedField ExtractRegexField(FieldDefinition fieldDef, IReadOnlyList<OcrLine> lines)
    {
        if (string.IsNullOrEmpty(fieldDef.Pattern))
        {
            return Missing(fieldDef);
        }

        var regex = new Regex(fieldDef.Pattern, RegexOptions.Compiled);

        (OcrLine Line, string Value)? best = null;
        foreach (var line in lines)
        {
            var value = MatchValue(regex, fieldDef.Match, line.Text);
            if (value is null)
            {
                continue;
            }

            if (best is null || line.Confidence > best.Value.Line.Confidence)
            {
                best = (line, value);
            }
        }

        if (best is null)
        {
            return Missing(fieldDef);
        }

        var (bestLine, raw) = best.Value;
        var normalized = Normalize(fieldDef, raw);
        return new ExtractedField
        {
            FieldName = fieldDef.Name,
            Label = fieldDef.Label,
            Value = raw,
            NormalizedValue = normalized,
            // Un-normalizable values are treated as invalid → 0 confidence → REVIEW_REQUIRED.
            Confidence = normalized is null ? 0.0 : Math.Round(bestLine.Confidence, 3)
        };
    }

    private static string? MatchValue(Regex regex, string matchMode, string text)
    {
        var trimmed = text.Trim();
        return matchMode switch
        {
            "exact" => regex.IsMatch(trimmed) ? trimmed : null,
            "endOfLine" => MatchAtEnd(regex, trimmed),
            _ => regex.Match(text).Success ? regex.Match(text).Value : null
        };
    }

    /// <summary>Returns the match that is the last token of the line (e.g. "Nationality: QAT"),
    /// or null when no match sits at the end of the line.</summary>
    private static string? MatchAtEnd(Regex regex, string trimmed)
    {
        var atEnd = regex.Matches(trimmed)
            .Where(m => m.Index + m.Length == trimmed.Length)
            .OrderByDescending(m => m.Length)
            .FirstOrDefault();
        return atEnd?.Value;
    }

    // ---------- text-line fields (name heuristic) ----------

    private static ExtractedField ExtractTextLineField(FieldDefinition fieldDef, IReadOnlyList<OcrLine> lines)
    {
        var candidates = lines
            .Where(l => l.Confidence >= 0.5)
            .Select(l => (Line: l, Text: l.Text.Trim()))
            .Where(x => x.Text.Length is >= 3 and <= 40)
            .Where(x => !QidPattern.IsMatch(x.Text))
            .Where(x => !DatePattern.IsMatch(x.Text))
            .Where(x => !x.Text.All(char.IsDigit))
            .Select(x => new
            {
                x.Line,
                x.Text,
                // 0..1 vertical position (0 = top of image). Names sit in the upper half of the card.
                Y = x.Line.NormalizedY ?? 1.0
            })
            .OrderByDescending(x => NameScore(x.Line.Confidence, x.Y, x.Text.Length))
            .ToList();

        var best = candidates.FirstOrDefault();
        if (best is null)
        {
            return Missing(fieldDef);
        }

        var normalized = Normalize(fieldDef, best.Text);
        return new ExtractedField
        {
            FieldName = fieldDef.Name,
            Label = fieldDef.Label,
            Value = best.Text,
            NormalizedValue = normalized,
            Confidence = normalized is null ? 0.0 : Math.Round(best.Line.Confidence, 3)
        };
    }

    /// <summary>Names tend to be near the top of the card, fairly long, and clearly read.</summary>
    private static double NameScore(double confidence, double y, int length)
    {
        var positionWeight = y <= 0.5 ? 1.0 : 0.4;
        var lengthWeight = Math.Min(length, 30) / 30.0;
        return confidence * positionWeight * lengthWeight;
    }

    // ---------- shared ----------

    private static string? Normalize(FieldDefinition fieldDef, string raw) =>
        fieldDef.Normalizer is null ? raw.Trim() : FieldNormalizers.Normalize(fieldDef.Name, raw);

    private static ExtractedField Missing(FieldDefinition fieldDef) => new()
    {
        FieldName = fieldDef.Name,
        Label = fieldDef.Label,
        Confidence = 0.0
    };
}