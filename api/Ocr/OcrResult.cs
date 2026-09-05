namespace IdentityDocument.Api.Ocr;

/// <summary>DTOs mirroring the Python OCR service JSON contract.</summary>

public sealed class OcrResult
{
    public QualityResult Quality { get; set; } = new();
    public List<OcrLine> Lines { get; set; } = new();
    public string? Language { get; set; }
}

public sealed class QualityResult
{
    public bool Passed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double BlurScore { get; set; }
    public string? Reason { get; set; }
}

public sealed class OcrLine
{
    public string Text { get; set; } = "";
    public double Confidence { get; set; }

    /// <summary>Quad of [x, y] points (PaddleOCR poly format). May be null in mock mode.</summary>
    public List<List<double>>? Box { get; set; }

    /// <summary>Vertical center of the line box (normalized 0..1 by image height), null if no box.</summary>
    public double? NormalizedY => Box is { Count: > 0 } box
        ? box.Average(p => p[1]) / 1000.0
        : null;
}