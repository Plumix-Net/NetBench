using NetBench.Features.Scenarios.Domain;
using Xunit;

namespace NetBench.Tests;

public class TargetUrlTests
{
    [Theory]
    [InlineData("https://api.example.com")]
    [InlineData("http://localhost:8080/health")]
    [InlineData("  https://example.com/path?q=1  ")]
    public void AcceptsAbsoluteHttpUrls(string target) =>
        Assert.True(TargetUrl.IsValid(target));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("https://")]
    [InlineData("https://exa mple.com")]
    public void RejectsMissingOrUnsupportedUrls(string? target) =>
        Assert.False(TargetUrl.IsValid(target));

    [Theory]
    [InlineData("example.com", "https://example.com")]
    [InlineData("  example.com/api  ", "https://example.com/api")]
    [InlineData("http://localhost:5000", "http://localhost:5000")]
    [InlineData("HTTPS://Example.com", "HTTPS://Example.com")]
    public void NormalizeAddsHttpsWhenSchemeIsMissing(string input, string expected)
    {
        Assert.True(TargetUrl.TryNormalize(input, out var url));
        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://example.com")]
    [InlineData("exa mple.com")]
    [InlineData("://")]
    public void NormalizeRejectsInvalidInput(string input)
    {
        Assert.False(TargetUrl.TryNormalize(input, out var url));
        Assert.Null(url);
    }
}
