namespace IdentityDocument.Api.Storage;

public sealed class LocalDiskDocumentStorage : IDocumentStorage
{
    private readonly string _rootPath;

    public LocalDiskDocumentStorage(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Guid documentId, string extension, Stream content, CancellationToken ct = default)
    {
        var safeExt = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension.TrimStart('.');
        var path = Path.Combine(_rootPath, $"{documentId:N}.{safeExt}");
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
        return path;
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }
}