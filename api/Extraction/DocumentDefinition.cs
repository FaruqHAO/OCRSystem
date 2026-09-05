namespace IdentityDocument.Api.Extraction;

/// <summary>
/// A document-definition JSON entry. Declarative: adding document type #2 is a new JSON
/// file plus (only if needed) a small normalizer/extractor tweak.
/// </summary>
public sealed class FieldDefinition
{
    /// <summary>Stable key, e.g. "qidNumber".</summary>
    public required string Name { get; init; }

    /// <summary>Human label for the UI, e.g. "QID Number".</summary>
    public required string Label { get; init; }

    /// <summary>"regex" — pull value from OCR text via <see cref="Pattern"/>;
    /// "textLine" — heuristic line selection (e.g. name).</summary>
    public required string Kind { get; init; }

    public string? Pattern { get; init; }

    /// <summary>Named normalizer: "qidNumber", "date", "monthYear", "upper", "name", or null.</summary>
    public string? Normalizer { get; init; }

    /// <summary>For regex fields: "contains" (anywhere), "exact" (whole trimmed line),
    /// "endOfLine" (last token of the line). Default "contains".</summary>
    public string Match { get; init; } = "contains";

    /// <summary>Kind-specific hint, e.g. "primaryName".</summary>
    public string? Hint { get; init; }
}

public sealed class DocumentDefinition
{
    public required string CountryCode { get; init; }
    public required string DocumentType { get; init; }
    public required IReadOnlyList<FieldDefinition> Fields { get; init; }
}