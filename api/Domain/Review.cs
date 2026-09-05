namespace IdentityDocument.Api.Domain;

public sealed class Review
{
    /// <summary>"approved" | "rejected"</summary>
    public string Decision { get; set; } = "";
    public DateTime ReviewedAt { get; set; }
    public List<FieldCorrection> CorrectedFields { get; set; } = new();
}

public sealed class FieldCorrection
{
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
}