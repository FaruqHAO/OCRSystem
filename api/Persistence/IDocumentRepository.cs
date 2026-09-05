using IdentityDocument.Api.Domain;

namespace IdentityDocument.Api.Persistence;

public interface IDocumentRepository
{
    Task<Document?> GetAsync(Guid id, CancellationToken ct = default);
    Task InsertAsync(Document document, CancellationToken ct = default);
    Task ReplaceAsync(Document document, CancellationToken ct = default);

    /// <summary>Documents stuck in a non-terminal status (used for crash recovery on startup).</summary>
    Task<IReadOnlyList<Document>> FindByStatusAsync(DocumentStatus status, CancellationToken ct = default);
}