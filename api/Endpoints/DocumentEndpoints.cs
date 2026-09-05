using IdentityDocument.Api.Domain;
using IdentityDocument.Api.Extraction;
using IdentityDocument.Api.Persistence;
using IdentityDocument.Api.Processing;
using IdentityDocument.Api.Storage;

namespace IdentityDocument.Api.Endpoints;

public static class DocumentEndpoints
{
    private const long MaxUploadBytes = 15 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff"
    };

    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/documents", UploadAsync);
        app.MapGet("/api/v1/documents/{id:guid}", GetAsync);
        app.MapPost("/api/v1/documents/{id:guid}/review", ReviewAsync);
    }

    // ---------- POST /api/v1/documents ----------

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        IDocumentRepository repository,
        IDocumentStorage storage,
        DefinitionLoader definitions,
        ProcessingQueue queue)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Expected multipart/form-data" });
        }

        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "file is required" });
        }
        if (file.Length > MaxUploadBytes)
        {
            return Results.Json(new { error = "File too large (max 15 MB)" }, statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        var extension = Path.GetExtension(file.FileName);
        if (extension.Length == 0 || !AllowedExtensions.Contains(extension))
        {
            return Results.BadRequest(new { error = $"Unsupported file type '{extension}'. Allowed: jpg, png, webp, bmp, tif" });
        }

        var documentType = string.IsNullOrWhiteSpace(form["documentType"]) ? "QID" : form["documentType"]!.ToString();

        DocumentDefinition definition;
        try
        {
            definition = definitions.Get("QA", documentType);
        }
        catch (KeyNotFoundException)
        {
            return Results.BadRequest(new { error = $"Unsupported document type '{documentType}'" });
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            CountryCode = definition.CountryCode,
            DocumentType = definition.DocumentType,
            ContentType = file.ContentType ?? "image/jpeg",
            Status = DocumentStatus.UPLOADED
        };

        try
        {
            await using var stream = file.OpenReadStream();
            document.FilePath = await storage.SaveAsync(document.Id, extension, stream);
        }
        catch (Exception)
        {
            return Results.Json(new { error = "Failed to store upload" }, statusCode: StatusCodes.Status500InternalServerError);
        }

        await repository.InsertAsync(document);
        queue.Enqueue(document.Id);

        return Results.Accepted(
            $"/api/v1/documents/{document.Id}",
            new { documentId = document.Id, status = document.Status.ToString() });
    }

    // ---------- GET /api/v1/documents/{id} ----------

    private static async Task<IResult> GetAsync(Guid id, IDocumentRepository repository)
    {
        var document = await repository.GetAsync(id);
        return document is null ? Results.NotFound(new { error = "Document not found" }) : Results.Ok(ToDto(document));
    }

    // ---------- POST /api/v1/documents/{id}/review ----------

    private static async Task<IResult> ReviewAsync(Guid id, ReviewRequest request, IDocumentRepository repository)
    {
        var document = await repository.GetAsync(id);
        if (document is null)
        {
            return Results.NotFound(new { error = "Document not found" });
        }

        var decision = request.Decision?.Trim().ToLowerInvariant();
        switch (decision)
        {
            case "approve":
                document.Review = new Review { Decision = "approved", ReviewedAt = DateTime.UtcNow };
                document.Status = DocumentStatus.APPROVED;
                break;

            case "reject":
                document.Review = new Review { Decision = "rejected", ReviewedAt = DateTime.UtcNow };
                document.Status = DocumentStatus.REJECTED;
                break;

            case "correct":
                if (request.CorrectedFields is null || request.CorrectedFields.Count == 0)
                {
                    return Results.BadRequest(new { error = "correct requires correctedFields" });
                }
                ApplyCorrections(document, request.CorrectedFields);
                document.Review = new Review
                {
                    Decision = "approved",
                    ReviewedAt = DateTime.UtcNow,
                    CorrectedFields = request.CorrectedFields
                };
                document.Status = DocumentStatus.APPROVED;
                break;

            default:
                return Results.BadRequest(new { error = "decision must be approve, reject or correct" });
        }

        await repository.ReplaceAsync(document);
        return Results.Ok(ToDto(document));
    }

    private static void ApplyCorrections(Document document, List<FieldCorrection> corrections)
    {
        foreach (var correction in corrections)
        {
            var field = document.ExtractedFields.FirstOrDefault(f => f.FieldName == correction.FieldName);
            if (field is null)
            {
                continue;
            }

            field.ReviewedValue = correction.Value;
            var normalized = FieldNormalizers.Normalize(field.FieldName, correction.Value);
            if (normalized is not null)
            {
                field.NormalizedValue = normalized;
            }
        }
    }

    // ---------- DTO ----------

    private static object ToDto(Document document) => new
    {
        documentId = document.Id,
        status = document.Status.ToString(),
        countryCode = document.CountryCode,
        documentType = document.DocumentType,
        createdAt = document.CreatedAt,
        processedAt = document.ProcessedAt,
        overallConfidence = document.OverallConfidence,
        quality = document.Quality is null
            ? null
            : new
            {
                passed = document.Quality.Passed,
                width = document.Quality.Width,
                height = document.Quality.Height,
                blurScore = document.Quality.BlurScore,
                reason = document.Quality.Reason
            },
        extractedFields = document.ExtractedFields.Select(f => new
        {
            fieldName = f.FieldName,
            label = f.Label,
            value = f.Value,
            normalizedValue = f.NormalizedValue,
            confidence = f.Confidence,
            reviewedValue = f.ReviewedValue
        }),
        review = document.Review is null
            ? null
            : new
            {
                decision = document.Review.Decision,
                reviewedAt = document.Review.ReviewedAt,
                correctedFields = document.Review.CorrectedFields
            },
        errorMessage = document.ErrorMessage
    };

    public sealed record ReviewRequest(string? Decision, List<FieldCorrection>? CorrectedFields);
}