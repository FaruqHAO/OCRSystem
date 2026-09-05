using IdentityDocument.Api.Endpoints;
using IdentityDocument.Api.Extraction;
using IdentityDocument.Api.Ocr;
using IdentityDocument.Api.Persistence;
using IdentityDocument.Api.Processing;
using IdentityDocument.Api.Security;
using IdentityDocument.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// ---------- File storage (local disk for MVP; MinIO later) ----------
var storageRoot = builder.Configuration["Storage:RootPath"] ?? Path.Combine(Path.GetTempPath(), "id-uploads");
builder.Services.AddSingleton<IDocumentStorage>(new LocalDiskDocumentStorage(storageRoot));

// ---------- Persistence (MongoDB) ----------
var mongoConnection = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "identity_documents";
builder.Services.AddSingleton<IDocumentRepository>(new MongoDocumentRepository(mongoConnection, mongoDatabase));

// ---------- Extraction (document definitions + extractor) ----------
var definitionsPath = builder.Configuration["Definitions:Path"] ?? Path.Combine(AppContext.BaseDirectory, "definitions");
builder.Services.AddSingleton(new DefinitionLoader(definitionsPath));
builder.Services.AddSingleton<DocumentExtractor>();

// ---------- OCR engine (Python microservice; swap the provider here) ----------
var ocrBaseUrl = builder.Configuration["Ocr:BaseUrl"] ?? "http://localhost:8000";
builder.Services.AddHttpClient<IOcrEngine, HttpOcrEngine>(client => client.BaseAddress = new Uri(ocrBaseUrl))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        // Local service; fail fast rather than hang on a dead OCR pod.
        ConnectTimeout = TimeSpan.FromSeconds(5)
    });

// ---------- Async processing (in-process queue; RabbitMQ later) ----------
builder.Services.AddSingleton<ProcessingQueue>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

var app = builder.Build();

// ---------- MVP auth: hardcoded API key ----------
var apiKey = builder.Configuration["ApiKey"] ?? "dev-api-key";
app.UseMiddleware<ApiKeyMiddleware>(apiKey);

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "identity-document-api" }));
app.MapDocumentEndpoints();

app.Run();

public partial class Program;