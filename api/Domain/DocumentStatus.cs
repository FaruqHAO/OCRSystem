namespace IdentityDocument.Api.Domain;

/// <summary>
/// Lifecycle of a document through the pipeline.
/// </summary>
public enum DocumentStatus
{
    UPLOADED,
    PROCESSING,
    QUALITY_FAILED,
    COMPLETED,
    REVIEW_REQUIRED,
    APPROVED,
    REJECTED,
    FAILED
}