using System.Globalization;
using System.Text.RegularExpressions;

namespace IdentityDocument.Api.Extraction;

/// <summary>
/// Per-field normalizers. Returns null when the raw value cannot be normalized
/// (the caller then treats the field as low-confidence / invalid).
/// </summary>
public static partial class FieldNormalizers
{
    public static string? Normalize(string fieldName, string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return fieldName switch
        {
            "qidNumber" => NormalizeQidNumber(trimmed),
            "dateOfBirth" => NormalizeDate(trimmed),
            "expiryDate" => NormalizeMonthYear(trimmed),
            "nationality" => NormalizeUpper(trimmed),
            "name" => NormalizeName(trimmed),
            _ => trimmed
        };
    }

    /// <summary>QID numbers are 11 digits starting with 2 (resident), 3 (citizen) or 4 (GCC).</summary>
    public static string? NormalizeQidNumber(string value)
    {
        var digits = DigitsOnly().Replace(value, "");
        return digits.Length == 11 && digits[0] is '2' or '3' or '4' ? digits : null;
    }

    /// <summary>Accepts dd/MM/yyyy → ISO yyyy-MM-dd.</summary>
    public static string? NormalizeDate(string value)
    {
        if (!DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>Accepts MM/yyyy → ISO yyyy-MM (day-less, as printed on QID).</summary>
    public static string? NormalizeMonthYear(string value)
    {
        if (!DateTime.TryParseExact(value, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        return date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }

    public static string? NormalizeUpper(string value)
    {
        var upper = value.ToUpperInvariant();
        return upper.Length > 0 ? upper : null;
    }

    public static string? NormalizeName(string value) =>
        Whitespace().Replace(value, " ").Trim().ToUpperInvariant();

    [GeneratedRegex(@"\D+")]
    private static partial Regex DigitsOnly();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}