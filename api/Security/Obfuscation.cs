namespace IdentityDocument.Api.Security;

/// <summary>
/// Redaction for sensitive values (QID numbers etc.) so they never appear in logs.
/// </summary>
public static class Obfuscation
{
    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(empty)";
        }

        return value.Length <= 6
            ? "****"
            : $"{new string('*', value.Length - 4)}{value[^4..]}";
    }
}