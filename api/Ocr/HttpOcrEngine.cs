using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace IdentityDocument.Api.Ocr;

/// <summary>
/// Calls the Python OCR microservice over HTTP (single internal endpoint per document type).
/// </summary>
public sealed class HttpOcrEngine : IOcrEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly ILogger<HttpOcrEngine> _logger;

    public HttpOcrEngine(HttpClient http, ILogger<HttpOcrEngine> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<OcrResult> ExtractAsync(string documentType, Stream imageStream, string contentType, CancellationToken ct)
    {
        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType);

        var response = await _http.PostAsync($"/v1/ocr/{Uri.EscapeDataString(documentType)}", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("OCR service returned {StatusCode} for {DocumentType}", (int)response.StatusCode, documentType);
            throw new HttpRequestException($"OCR service returned {(int)response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<OcrResult>(JsonOptions, ct)
                     ?? throw new HttpRequestException("OCR service returned an empty payload");
        return result;
    }
}