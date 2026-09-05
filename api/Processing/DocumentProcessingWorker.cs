using IdentityDocument.Api.Domain;
using IdentityDocument.Api.Extraction;
using IdentityDocument.Api.Ocr;
using IdentityDocument.Api.Persistence;
using IdentityDocument.Api.Storage;

namespace IdentityDocument.Api.Processing;

/// <summary>
/// Background worker that consumes the processing queue:
/// quality check → OCR → extraction → normalization → confidence → status.
/// </summary>
public sealed class DocumentProcessingWorker : BackgroundService
{
    private readonly ProcessingQueue _queue;
    private readonly IDocumentRepository _repository;
    private readonly IDocumentStorage _storage;
    private readonly IOcrEngine _ocr;
    private readonly DefinitionLoader _definitions;
    private readonly DocumentExtractor _extractor;
    private readonly double _completedThreshold;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        ProcessingQueue queue,
        IDocumentRepository repository,
        IDocumentStorage storage,
        IOcrEngine ocr,
        DefinitionLoader definitions,
        DocumentExtractor extractor,
        IConfiguration configuration,
        ILogger<DocumentProcessingWorker> logger)
    {
        _queue = queue;
        _repository = repository;
        _storage = storage;
        _ocr = ocr;
        _definitions = definitions;
        _extractor = extractor;
        _completedThreshold = configuration.GetValue<double?>("Processing:CompletedThreshold") ?? 0.75;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeuePendingAsync(stoppingToken);

        await foreach (var documentId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(documentId, stoppingToken);
        }
    }

    /// <summary>Crash recovery: anything left UPLOADED/PROCESSING is re-enqueued.</summary>
    private async Task RequeuePendingAsync(CancellationToken ct)
    {
        foreach (var status in new[] { DocumentStatus.UPLOADED, DocumentStatus.PROCESSING })
        {
            var pending = await _repository.FindByStatusAsync(status, ct);
            foreach (var document in pending)
            {
                _logger.LogInformation("Re-enqueuing {Status} document {DocumentId}", status, document.Id);
                _queue.Enqueue(document.Id);
            }
        }
    }

    private async Task ProcessAsync(Guid documentId, CancellationToken ct)
    {
        var document = await _repository.GetAsync(documentId, ct);
        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} not found; skipping", documentId);
            return;
        }
        if (document.Status is not (DocumentStatus.UPLOADED or DocumentStatus.PROCESSING))
        {
            return; // already terminal — ignore duplicates
        }

        document.Status = DocumentStatus.PROCESSING;
        await _repository.ReplaceAsync(document, ct);

        try
        {
            await using var image = await _storage.OpenReadAsync(document.FilePath, ct);
            var ocrResult = await _ocr.ExtractAsync(document.DocumentType, image, document.ContentType, ct);

            document.Quality = ocrResult.Quality;
            document.ProcessedAt = DateTime.UtcNow;

            if (!ocrResult.Quality.Passed)
            {
                document.Status = DocumentStatus.QUALITY_FAILED;
                document.ErrorMessage = ocrResult.Quality.Reason;
                _logger.LogInformation("Document {DocumentId} failed quality check: {Reason}", documentId, ocrResult.Quality.Reason);
                await _repository.ReplaceAsync(document, ct);
                return;
            }

            var definition = _definitions.Get(document.CountryCode, document.DocumentType);
            var fields = _extractor.Extract(definition, ocrResult.Lines);

            document.ExtractedFields = fields.ToList();
            document.OverallConfidence = DocumentExtractor.ComputeOverall(fields);
            document.Status = document.OverallConfidence >= _completedThreshold
                ? DocumentStatus.COMPLETED
                : DocumentStatus.REVIEW_REQUIRED;

            _logger.LogInformation(
                "Document {DocumentId} processed: status={Status} overall={Overall}",
                documentId, document.Status, document.OverallConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {DocumentId}", documentId);
            document.Status = DocumentStatus.FAILED;
            document.ErrorMessage = "Processing failed — please retry";
        }

        await _repository.ReplaceAsync(document, ct);
    }
}