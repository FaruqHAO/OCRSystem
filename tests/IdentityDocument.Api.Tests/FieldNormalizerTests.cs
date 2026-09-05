using IdentityDocument.Api.Extraction;

namespace IdentityDocument.Api.Tests;

public class FieldNormalizerTests
{
    [Theory]
    [InlineData("234 2123 4567", "23421234567")]
    [InlineData("23421234567", "23421234567")]
    [InlineData(" 342 1234 5678 ", "34212345678")]
    [InlineData("421 2345 6789", "42123456789")]
    public void NormalizeQidNumber_Valid_ReturnsElevenDigits(string raw, string expected)
    {
        Assert.Equal(expected, FieldNormalizers.NormalizeQidNumber(raw));
    }

    [Theory]
    [InlineData("123 456 789")]
    [InlineData("234 2123 456")]      // 10 digits
    [InlineData("123421234567")]      // 12 digits
    [InlineData("13421234567")]       // wrong first digit
    [InlineData("")]
    public void NormalizeQidNumber_Invalid_ReturnsNull(string raw)
    {
        Assert.Null(FieldNormalizers.NormalizeQidNumber(raw));
    }

    [Theory]
    [InlineData("15/08/1990", "1990-08-15")]
    [InlineData("01/01/2000", "2000-01-01")]
    [InlineData("31/12/1985", "1985-12-31")]
    public void NormalizeDate_Valid_ReturnsIso(string raw, string expected)
    {
        Assert.Equal(expected, FieldNormalizers.NormalizeDate(raw));
    }

    [Theory]
    [InlineData("32/13/1990")]
    [InlineData("1990-08-15")]
    [InlineData("15-08-1990")]
    public void NormalizeDate_Invalid_ReturnsNull(string raw)
    {
        Assert.Null(FieldNormalizers.NormalizeDate(raw));
    }

    [Theory]
    [InlineData("09/2030", "2030-09")]
    [InlineData("12/2029", "2029-12")]
    public void NormalizeMonthYear_Valid_ReturnsYearMonth(string raw, string expected)
    {
        Assert.Equal(expected, FieldNormalizers.NormalizeMonthYear(raw));
    }

    [Theory]
    [InlineData("13/2030")]
    [InlineData("09/30")]
    public void NormalizeMonthYear_Invalid_ReturnsNull(string raw)
    {
        Assert.Null(FieldNormalizers.NormalizeMonthYear(raw));
    }

    [Fact]
    public void NormalizeUpper_Uppercases()
    {
        Assert.Equal("QAT", FieldNormalizers.NormalizeUpper("qat"));
    }

    [Fact]
    public void NormalizeName_CollapsesWhitespaceAndUppercases()
    {
        Assert.Equal("AHMED MOHAMMED AL-THANI", FieldNormalizers.NormalizeName("  ahmed   mohammed al-thani "));
    }

    [Fact]
    public void Normalize_UnknownField_FallsBackToTrimmedValue()
    {
        Assert.Equal("whatever", FieldNormalizers.Normalize("unknownField", "  whatever  "));
    }
}