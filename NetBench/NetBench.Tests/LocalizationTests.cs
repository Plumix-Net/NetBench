using NetBench.Localization;
using Xunit;

namespace NetBench.Tests;

public class LocalizationTests
{
    [Fact]
    public void RussianAndEnglishCulturesAreAvailable()
    {
        var cultureNames = Strings.SupportedCultures.Select(culture => culture.Name);

        Assert.Contains("ru-RU", cultureNames);
        Assert.Contains("en-US", cultureNames);
        Assert.Equal("ru-RU", Strings.BaseCulture.Name);
    }
}
