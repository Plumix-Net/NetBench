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
        // Базовая культура — запасная для неподдерживаемых языков устройства (hr-HR и т.п.):
        // в международных сторах это английский.
        Assert.Equal("en-US", Strings.BaseCulture.Name);
    }
}
