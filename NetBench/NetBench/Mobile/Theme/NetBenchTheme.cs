using Avalonia.Media;
using Plumix.Material;
using Plumix.Widgets;

namespace NetBench.Mobile.Theme;

/// <summary>
/// Дизайн-токены NetBench для мобильной темы — мобильный аналог
/// Desktop/Themes/Colors.axaml, значения из tokens.css дизайн-проекта.
/// </summary>
public sealed record NetBenchPalette
{
    public required bool IsDark { get; init; }

    // Поверхности
    public required Color BgBase { get; init; }
    public required Color BgSunken { get; init; }
    public required Color BgPanel { get; init; }
    public required Color BgCard { get; init; }
    public required Color BgCardHover { get; init; }

    // Структура
    public required Color Border { get; init; }
    public required Color BorderStrong { get; init; }

    // Текстовая шкала
    public required Color TextHi { get; init; }
    public required Color Text { get; init; }
    public required Color TextMid { get; init; }
    public required Color TextDim { get; init; }
    public required Color TextFaint { get; init; }

    // Семантические акценты — значение фиксировано во всём приложении
    public required Color Rps { get; init; }
    public required Color Latency { get; init; }
    public required Color Error { get; init; }
    public required Color Success { get; init; }

    // Подложки акцентов
    public required Color RpsTint { get; init; }
    public required Color LatencyTint { get; init; }
    public required Color ErrorTint { get; init; }
    public required Color SuccessTint { get; init; }

    public static NetBenchPalette Dark { get; } = new()
    {
        IsDark = true,
        BgBase = Color.Parse("#101014"),
        BgSunken = Color.Parse("#0A0A0C"),
        BgPanel = Color.Parse("#16161A"),
        BgCard = Color.Parse("#1E1E24"),
        BgCardHover = Color.Parse("#25252C"),
        Border = Color.Parse("#232329"),
        BorderStrong = Color.Parse("#2A2A31"),
        TextHi = Color.Parse("#FFFFFF"),
        Text = Color.Parse("#EDEDF0"),
        TextMid = Color.Parse("#8A8A94"),
        TextDim = Color.Parse("#6A6A74"),
        TextFaint = Color.Parse("#5A5A64"),
        Rps = Color.Parse("#1E90FF"),
        Latency = Color.Parse("#FFA500"),
        Error = Color.Parse("#FF4D4F"),
        Success = Color.Parse("#52C41A"),
        RpsTint = Color.FromArgb(36, 0x1E, 0x90, 0xFF),
        LatencyTint = Color.FromArgb(36, 0xFF, 0xA5, 0x00),
        ErrorTint = Color.FromArgb(36, 0xFF, 0x4D, 0x4F),
        SuccessTint = Color.FromArgb(36, 0x52, 0xC4, 0x1A),
    };

    public static NetBenchPalette Light { get; } = new()
    {
        IsDark = false,
        BgBase = Color.Parse("#F5F6F8"),
        BgSunken = Color.Parse("#E7E9ED"),
        BgPanel = Color.Parse("#FFFFFF"),
        BgCard = Color.Parse("#FFFFFF"),
        BgCardHover = Color.Parse("#EEF0F3"),
        Border = Color.Parse("#E3E5EA"),
        BorderStrong = Color.Parse("#D2D6DD"),
        TextHi = Color.Parse("#0C0D10"),
        Text = Color.Parse("#22242A"),
        TextMid = Color.Parse("#5C616C"),
        TextDim = Color.Parse("#878C97"),
        TextFaint = Color.Parse("#AEB2BC"),
        // Акценты затемнены для AA-контраста на светлых поверхностях
        Rps = Color.Parse("#1370D6"),
        Latency = Color.Parse("#D9820A"),
        Error = Color.Parse("#E23A3C"),
        Success = Color.Parse("#3E9C12"),
        RpsTint = Color.FromArgb(31, 0x13, 0x70, 0xD6),
        LatencyTint = Color.FromArgb(41, 0xD9, 0x82, 0x0A),
        ErrorTint = Color.FromArgb(31, 0xE2, 0x3A, 0x3C),
        SuccessTint = Color.FromArgb(36, 0x3E, 0x9C, 0x12),
    };
}

/// <summary>Шрифты дизайн-системы: Inter — интерфейс, моноширинный — метрики и URL.</summary>
public static class NetBenchFonts
{
    public static FontFamily Ui { get; } = FontFamily.Parse("fonts:Inter#Inter");

    // JetBrains Mono не встроен — деградируем на системные моноширинные (как на desktop)
    public static FontFamily Mono { get; } =
        FontFamily.Parse("JetBrains Mono,SF Mono,Menlo,Roboto Mono,Consolas,monospace");
}

/// <summary>
/// Раздаёт палитру токенов и переключатель темы вниз по дереву —
/// мобильный аналог ThemeDictionaries из Desktop/Themes.
/// </summary>
public sealed class NetBenchTheme : InheritedWidget
{
    public NetBenchTheme(
        NetBenchPalette palette,
        Action toggleTheme,
        Widget child,
        Plumix.Foundation.Key? key = null) : base(key)
    {
        Palette = palette;
        ToggleTheme = toggleTheme;
        Child = child;
    }

    public NetBenchPalette Palette { get; }

    public Action ToggleTheme { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((NetBenchTheme)oldWidget).Palette, Palette);

    public static NetBenchPalette PaletteOf(BuildContext context) => Of(context).Palette;

    public static NetBenchTheme Of(BuildContext context) =>
        context.DependOnInherited<NetBenchTheme>()
        ?? throw new InvalidOperationException("NetBenchTheme не найдена в дереве виджетов.");

    /// <summary>Material-тема из палитры — для стандартных виджетов (SnackBar, поля ввода и т.п.).</summary>
    public static ThemeData CreateThemeData(NetBenchPalette palette) => new(
        brightness: palette.IsDark ? Brightness.Dark : Brightness.Light,
        scaffoldBackgroundColor: palette.BgBase,
        canvasColor: palette.BgBase,
        primaryColor: palette.Rps,
        surfaceColor: palette.BgCard,
        onSurfaceColor: palette.Text,
        onSurfaceVariantColor: palette.TextMid,
        outlineColor: palette.BorderStrong,
        outlineVariantColor: palette.Border,
        dividerColor: palette.Border,
        cardColor: palette.BgCard,
        errorColor: palette.Error,
        surfaceContainerLowColor: palette.BgPanel,
        iconTheme: new IconThemeData(Color: palette.Text, Size: 24));
}
