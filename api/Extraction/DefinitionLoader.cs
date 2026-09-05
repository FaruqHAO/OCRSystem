using System.Text.Json;

namespace IdentityDocument.Api.Extraction;

/// <summary>
/// Loads and caches document-definition JSON files from a directory.
/// One file per document type (e.g. qa-qid.json).
/// </summary>
public sealed class DefinitionLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IReadOnlyDictionary<(string CountryCode, string DocumentType), DocumentDefinition> _definitions;

    public DefinitionLoader(string directory)
    {
        var loaded = new Dictionary<(string, string), DocumentDefinition>();
        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var definition = JsonSerializer.Deserialize<DocumentDefinition>(json, JsonOptions)
                             ?? throw new InvalidOperationException($"Document definition '{file}' failed to deserialize");
            loaded[(definition.CountryCode, definition.DocumentType)] = definition;
        }
        _definitions = loaded;
    }

    public DocumentDefinition Get(string countryCode, string documentType) =>
        _definitions.TryGetValue((countryCode, documentType), out var definition)
            ? definition
            : throw new KeyNotFoundException($"No document definition for {countryCode}/{documentType}");
}