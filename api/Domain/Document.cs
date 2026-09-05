using IdentityDocument.Api.Ocr;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IdentityDocument.Api.Domain;

/// <summary>
/// The single document aggregate. Extracted fields are embedded as an array
/// (no separate collection / joins) per the MVP scope.
/// </summary>
public sealed class Document
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public DocumentStatus Status { get; set; } = DocumentStatus.UPLOADED;

    public string CountryCode { get; set; } = "QA";
    public string DocumentType { get; set; } = "QID";

    /// <summary>Path under the configured storage root (see IDocumentStorage).</summary>
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "image/jpeg";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public double? OverallConfidence { get; set; }

    /// <summary>Result of the image quality check (resolution + blur).</summary>
    public QualityResult? Quality { get; set; }

    public List<ExtractedField> ExtractedFields { get; set; } = new();

    public Review? Review { get; set; }

    /// <summary>Human-readable failure reason (never contains raw document data).</summary>
    public string? ErrorMessage { get; set; }
}