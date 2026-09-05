namespace IdentityDocument.Api.Storage;

/// <summary>
/// Abstraction over where original document images live.
/// Currently backed by local disk; a MinIO/S3 implementation can be added
/// without touching callers.
/// </summary>
public interface IDocumentStorage
{
    /// <summary>Stores the stream and returns the storage path/object key.</summary>
    Task<string> SaveAsync(Guid documentId, string extension, Stream content, CancellationToken ct = default);

    /// <summary>Opens the stored image for reading. Throws if missing.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Deletes the stored image (best effort).</summary>
    Task DeleteAsync(string path, CancellationToken ct = default);
}