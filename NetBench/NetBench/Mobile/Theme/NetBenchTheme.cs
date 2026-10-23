using Avalonia.Media;
using Plumix.Material;
using Plumix.Widgets;
using Color = Plumix.UI.Color;

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
        BgBase = new(0xFF101014),
        BgSunken = new(0xFF0A0A0C),
        BgPanel = new(0xFF16161A),
        BgCard = new(0xFF1E1E24),
        BgCardHover = new(0xFF25252C),
        Border = new(0xFF232329),
        BorderStrong = new(0xFF2A2A31),
        TextHi = new(0xFFFFFFFF),
        Text = new(0xFFEDEDF0),
        TextMid = new(0xFF8A8A94),
        TextDim = new(0xFF6A6A74),
        TextFaint = new(0xFF5A5A64),
        Rps = new(0xFF1E90FF),
        Latency = new(0xFFFFA500),
        Error = new(0xFFFF4D4F),
        Success = new(0xFF52C41A),
        RpsTint = Color.FromARGB(36, 0x1E, 0x90, 0xFF),
        LatencyTint = Color.FromARGB(36, 0xFF, 0xA5, 0x00),
        ErrorTint = Color.FromARGB(36, 0xFF, 0x4D, 0x4F),
        SuccessTint = Color.FromARGB(36, 0x52, 0xC4, 0x1A),
    };

    public static NetBenchPalette Light { get; } = new()
    {
        IsDark = false,
        BgBase = new(0xFFF5F6F8),
        BgSunken = new(0xFFE7E9ED),
        BgPanel = new(0xFFFFFFFF),
        BgCard = new(0xFFFFFFFF),
        BgCardHover = new(0xFFEEF0F3),
        Border = new(0xFFE3E5EA),
        BorderStrong = new(0xFFD2D6DD),
        TextHi = new(0xFF0C0D10),
        Text = new(0xFF22242A),
        TextMid = new(0xFF5C616C),
        TextDim = new(0xFF878C97),
        TextFaint = new(0xFFAEB2BC),
        // Акценты затемнены для AA-контраста на светлых поверхностях
        Rps = new(0xFF1370D6),
        Latency = new(0xFFD9820A),
        Error = new(0xFFE23A3C),
        Success = new(0xFF3E9C12),
        RpsTint = Color.FromARGB(31, 0x13, 0x70, 0xD6),
        LatencyTint = Color.FromARGB(41, 0xD9, 0x82, 0x0A),
        ErrorTint = Color.FromARGB(31, 0xE2, 0x3A, 0x3C),
        SuccessTint = Color.FromARGB(36, 0x3E, 0x9C, 0x12),
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
        Plumix.Foundation.Key? key = null) : base(child, key)
    {
        Palette = palette;
        ToggleTheme = toggleTheme;
    }

    public NetBenchPalette Palette { get; }

    public Action ToggleTheme { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget) =>
        !Equals(((NetBenchTheme)oldWidget).Palette, Palette);

    public static NetBenchPalette PaletteOf(BuildContext context) => Of(context).Palette;

    public static NetBenchTheme Of(BuildContext context) =>
        context.DependOnInheritedWidgetOfExactType<NetBenchTheme>()
        ?? throw new InvalidOperationException("NetBenchTheme не найдена в дереве виджетов.");

    /// <summary>Material-тема из палитры — для стандартных виджетов (SnackBar, поля ввода и т.п.).</summary>
    public static ThemeData CreateThemeData(NetBenchPalette palette)
    {
        // Базовая схема M3 под яркость, поверх — только роли, для которых есть токены дизайна.
        var colorScheme = (palette.IsDark ? ColorScheme.Dark() : ColorScheme.Light()) with
        {
            Primary = palette.Rps,
            Surface = palette.BgCard,
            OnSurface = palette.Text,
            OnSurfaceVariant = palette.TextMid,
            Outline = palette.BorderStrong,
            OutlineVariant = palette.Border,
            Error = palette.Error,
            SurfaceContainerLow = palette.BgPanel,
        };

        return new ThemeData(
            colorScheme: colorScheme,
            scaffoldBackgroundColor: palette.BgBase,
            canvasColor: palette.BgBase,
            dividerColor: palette.Border,
            cardColor: palette.BgCard,
            iconTheme: new IconThemeData(Color: palette.Text, Size: 24));
    }
}
