using IdentityDocument.Api.Domain;
using MongoDB.Driver;

namespace IdentityDocument.Api.Persistence;

public sealed class MongoDocumentRepository : IDocumentRepository
{
    private readonly IMongoCollection<Document> _documents;

    public MongoDocumentRepository(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _documents = client.GetDatabase(databaseName).GetCollection<Document>("documents");
    }

    public async Task<Document?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var matches = await _documents.Find(d => d.Id == id).ToListAsync(ct);
        return matches.FirstOrDefault();
    }

    public Task InsertAsync(Document document, CancellationToken ct = default) =>
        _documents.InsertOneAsync(document, cancellationToken: ct);

    public Task ReplaceAsync(Document document, CancellationToken ct = default) =>
        _documents.ReplaceOneAsync(d => d.Id == document.Id, document, cancellationToken: ct);

    public async Task<IReadOnlyList<Document>> FindByStatusAsync(DocumentStatus status, CancellationToken ct = default) =>
        await _documents.Find(d => d.Status == status).ToListAsync(ct);
}