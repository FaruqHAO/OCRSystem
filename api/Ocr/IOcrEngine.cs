namespace IdentityDocument.Api.Ocr;

/// <summary>
/// Abstraction over the OCR backend so the provider can be swapped
/// (PaddleOCR today, cloud OCR, Tesseract, etc. later).
/// </summary>
public interface IOcrEngine
{
    /// <summary>
    /// Runs quality check + OCR on the image.
    /// </summary>
    /// <param name="documentType">e.g. "QID" — lets the backend pick per-type models/pipelines.</param>
    Task<OcrResult> ExtractAsync(string documentType, Stream imageStream, string contentType, CancellationToken ct);
}