namespace IdentityDocument.Api.Domain;

/// <summary>
/// A single field extracted from a document, embedded in the Document record.
/// </summary>
public sealed class ExtractedField
{
    public string FieldName { get; set; } = "";
    public string Label { get; set; } = "";

    /// <summary>Raw value as produced by the extractor (from OCR text).</summary>
    public string? Value { get; set; }

    /// <summary>Normalized value (e.g. ISO date, 11-digit QID). Null if normalization failed.</summary>
    public string? NormalizedValue { get; set; }

    /// <summary>0..1 confidence from the OCR line that produced the value. 0 if not found or not normalizable.</summary>
    public double Confidence { get; set; }

    /// <summary>Value corrected by a reviewer. Null until reviewed.</summary>
    public string? ReviewedValue { get; set; }
}