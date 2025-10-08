using Xunit;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Tests;

public class AutomationIdSanitizerTests
{
    [Theory]
    [InlineData(null, "module", "module")]
    [InlineData("", "module", "module")]
    [InlineData("   ", "module", "module")]
    [InlineData(" QA Owner ", "fallback", "qa-owner")]
    [InlineData("Field_Name", "fallback", "field-name")]
    [InlineData("Članak Šifra", "fallback", "članak-šifra")]
    [InlineData("Status: Closed!", "fallback", "status-closed")]
    [InlineData("0123", "fallback", "0123")]
    public void Normalize_ReturnsExpectedToken(string? input, string fallback, string expected)
    {
        var actual = AutomationIdSanitizer.Normalize(input, fallback);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Normalize_WhenFallbackSanitizesEmpty_UsesDefaultToken()
    {
        var actual = AutomationIdSanitizer.Normalize(null, "???");

        Assert.Equal("fallback", actual);
    }

    [Fact]
    public void Normalize_CompressesRepeatedSeparators()
    {
        var actual = AutomationIdSanitizer.Normalize("Status---Changed", "fallback");

        Assert.Equal("status-changed", actual);
    }
}
