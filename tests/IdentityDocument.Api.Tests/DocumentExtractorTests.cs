using IdentityDocument.Api.Domain;
using IdentityDocument.Api.Extraction;
using IdentityDocument.Api.Ocr;

namespace IdentityDocument.Api.Tests;

public class DocumentExtractorTests
{
    private static readonly DefinitionLoader Loader = new(Path.Combine(AppContext.BaseDirectory, "definitions"));

    private static OcrLine Line(string text, double confidence, double y = 500) => new()
    {
        Text = text,
        Confidence = confidence,
        Box = [[100, y], [500, y], [500, y + 30], [100, y + 30]]
    };

    private static readonly List<OcrLine> MockQidLines =
    [
        Line("STATE OF QATAR", 0.99, 40),
        Line("QATAR IDENTITY CARD", 0.98, 90),
        Line("AHMED MOHAMMED AL-THANI", 0.97, 160),
        Line("234 2123 4567", 0.96, 240),
        Line("Date of Birth: 15/08/1990", 0.95, 310),
        Line("Expiry: 09/2030", 0.94, 370),
        Line("Nationality: QAT", 0.93, 430)
    ];

    [Fact]
    public void Extract_AllFields_FoundAndNormalized()
    {
        var definition = Loader.Get("QA", "QID");
        var fields = new DocumentExtractor().Extract(definition, MockQidLines);

        var byName = fields.ToDictionary(f => f.FieldName);

        Assert.Equal("AHMED MOHAMMED AL-THANI", byName["name"].NormalizedValue);
        Assert.Equal("23421234567", byName["qidNumber"].NormalizedValue);
        Assert.Equal("1990-08-15", byName["dateOfBirth"].NormalizedValue);
        Assert.Equal("2030-09", byName["expiryDate"].NormalizedValue);
        Assert.Equal("QAT", byName["nationality"].NormalizedValue);

        Assert.True(byName.All(f => f.Value.Confidence > 0.9), "all mock fields should have high confidence");
    }

    [Fact]
    public void Extract_ExpiryDoesNotStealSubstringFromDob()
    {
        // "15/08/1990" must not satisfy the expiry (20[2-9]\d year) pattern.
        var definition = Loader.Get("QA", "QID");
        var lines = new List<OcrLine> { Line("Date of Birth: 15/08/1990", 0.99) };

        var fields = new DocumentExtractor().Extract(definition, lines);

        Assert.Null(fields.Single(f => f.FieldName == "expiryDate").Value);
        Assert.Equal("1990-08-15", fields.Single(f => f.FieldName == "dateOfBirth").NormalizedValue);
    }

    [Fact]
    public void Extract_MissingField_HasZeroConfidence()
    {
        // A single short line can't satisfy any field (name heuristic needs >= 3 chars).
        var definition = Loader.Get("QA", "QID");
        var fields = new DocumentExtractor().Extract(definition, [Line("X", 0.99)]);

        Assert.All(fields, f => Assert.Equal(0.0, f.Confidence));
        Assert.Null(fields.Single(f => f.FieldName == "qidNumber").Value);
    }

    [Fact]
    public void Extract_Nationality_UsesEndOfLine()
    {
        var definition = Loader.Get("QA", "QID");
        var lines = new List<OcrLine>
        {
            Line("THE MINISTRY OF INTERIOR", 0.99, 100),
            Line("Nationality: QAT", 0.8, 400)
        };

        var fields = new DocumentExtractor().Extract(definition, lines);

        Assert.Equal("QAT", fields.Single(f => f.FieldName == "nationality").Value);
    }

    [Fact]
    public void Extract_Name_PrefersTopLongReadableLine()
    {
        var definition = Loader.Get("QA", "QID");
        var lines = new List<OcrLine>
        {
            Line("STATE OF QATAR", 0.99, 40),
            Line("QATAR IDENTITY CARD", 0.98, 90),
            Line("AHMED MOHAMMED AL-THANI", 0.97, 160)
        };

        var fields = new DocumentExtractor().Extract(definition, lines);

        Assert.Equal("AHMED MOHAMMED AL-THANI", fields.Single(f => f.FieldName == "name").Value);
    }

    [Fact]
    public void Extract_InvalidQidLine_NotExtracted()
    {
        // "999 9999 9999" fails the [234] prefix pattern → field simply missing.
        var definition = Loader.Get("QA", "QID");
        var lines = new List<OcrLine> { Line("ID No: 999 9999 9999", 0.99) };

        var fields = new DocumentExtractor().Extract(definition, lines);

        var qid = fields.Single(f => f.FieldName == "qidNumber");
        Assert.Null(qid.Value);
        Assert.Equal(0.0, qid.Confidence);
    }

    [Fact]
    public void ComputeOverall_AverageOfFieldConfidences()
    {
        var fields = new List<ExtractedField>
        {
            new() { FieldName = "a", Label = "A", Confidence = 1.0 },
            new() { FieldName = "b", Label = "B", Confidence = 0.5 },
            new() { FieldName = "c", Label = "C", Confidence = 0.0 }
        };

        Assert.Equal(0.5, DocumentExtractor.ComputeOverall(fields));
    }
}