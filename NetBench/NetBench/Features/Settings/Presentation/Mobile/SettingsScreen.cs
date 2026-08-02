using System.Globalization;
using Avalonia.Media;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Theme;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.UI;
using Plumix.Widgets;

namespace NetBench.Features.Settings.Presentation.Mobile;

/// <summary>
/// Настройки приложения: язык интерфейса и оформление.
/// Обе настройки применяются сразу — язык через <see cref="LocaleSettings{T}"/>
/// (перестраивает всех, кто читал строки из контекста), тема через <see cref="NetBenchTheme"/>.
/// </summary>
public sealed class SettingsScreen : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);

        return new Scaffold(
            body: new SafeArea(
                new Padding(
                    new Avalonia.Thickness(20, 8, 20, 0),
                    new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            BuildTopBar(context, palette, strings.Settings.Title),
                            new Expanded(
                                new SingleChildScrollView(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children:
                                        [
                                            BuildSectionHead(palette, strings.Settings.Language),
                                            BuildLanguageGroup(context, palette),
                                            BuildSectionHead(palette, strings.Settings.Appearance),
                                            BuildThemeGroup(context, palette, strings),
                                            new SizedBox(height: 28),
                                            BuildFooter(palette, strings),
                                            new SizedBox(height: 24),
                                        ]))),
                        ]))));
    }

    private static Padding BuildTopBar(BuildContext context, NetBenchPalette palette, string title) =>
        new Padding(
            new Avalonia.Thickness(0, 0, 0, 8),
            new Row(
                spacing: 12,
                children:
                [
                    new Material(
                        color: palette.BgCard,
                        borderRadius: BorderRadius.Circular(8),
                        child: new IconButton(
                            icon: new Icon(Icons.ArrowBackIosNewRounded, size: 16, color: palette.Text),
                            onPressed: () => Navigator.Pop(context),
                            constraints: BoxConstraints.Tight(new Avalonia.Size(34, 34)),
                            padding: default(Avalonia.Thickness))),
                    new Text(
                        title,
                        fontSize: 20,
                        fontWeight: FontWeight.ExtraBold,
                        color: palette.TextHi,
                        fontFamily: NetBenchFonts.Ui),
                ]));

    private static Padding BuildSectionHead(NetBenchPalette palette, string title) =>
        new Padding(
            new Avalonia.Thickness(0, 26, 0, 12),
            new Text(
                title.ToUpperInvariant(),
                fontSize: 12,
                fontWeight: FontWeight.Bold,
                color: palette.TextMid,
                letterSpacing: 0.6,
                fontFamily: NetBenchFonts.Ui));

    private static Material BuildLanguageGroup(BuildContext context, NetBenchPalette palette)
    {
        var supported = LocaleSettings<Strings>.SupportedCultures;
        var current = Translations<Strings>.CultureOf(context);

        return BuildGroup(
            palette,
            [
                .. supported.Select(Widget (culture) => BuildOptionRow(
                    palette,
                    DisplayName(culture),
                    culture.TwoLetterISOLanguageName.ToUpperInvariant(),
                    selected: string.Equals(culture.Name, current.Name, StringComparison.OrdinalIgnoreCase),
                    onTap: () => LocaleSettings<Strings>.SetLocale(culture))),
            ]);
    }

    private static Material BuildThemeGroup(BuildContext context, NetBenchPalette palette, Strings strings)
    {
        var theme = NetBenchTheme.Of(context);

        // ToggleTheme — единственная точка смены темы, поэтому дёргаем её только
        // когда выбранная строка отличается от текущей: повторный тап ничего не делает.
        Widget Row(string label, bool isDark) => BuildOptionRow(
            palette,
            label,
            trailing: null,
            selected: palette.IsDark == isDark,
            onTap: palette.IsDark == isDark ? null : theme.ToggleTheme);

        return BuildGroup(
            palette,
            [
                Row(strings.Settings.ThemeDark, isDark: true),
                Row(strings.Settings.ThemeLight, isDark: false),
            ]);
    }

    /// <summary>Карточка-группа: строки настроек, разделённые тонкой линией.</summary>
    private static Material BuildGroup(NetBenchPalette palette, IReadOnlyList<Widget> rows)
    {
        var children = new List<Widget>(rows.Count * 2);
        for (var i = 0; i < rows.Count; i++)
        {
            if (i > 0)
                children.Add(new Container(height: 1, color: palette.Border));

            children.Add(rows[i]);
        }

        return new Material(
            color: palette.BgCard,
            borderRadius: BorderRadius.Circular(10),
            clipBehavior: Clip.AntiAlias,
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: children));
    }

    private static InkWell BuildOptionRow(
        NetBenchPalette palette,
        string label,
        string? trailing,
        bool selected,
        Action? onTap)
    {
        var children = new List<Widget>
        {
            new Expanded(
                new Text(
                    label,
                    fontSize: 15,
                    fontWeight: selected ? FontWeight.SemiBold : FontWeight.Normal,
                    color: selected ? palette.TextHi : palette.Text,
                    fontFamily: NetBenchFonts.Ui,
                    maxLines: 1)),
        };

        if (!string.IsNullOrEmpty(trailing))
        {
            children.Add(new Text(
                trailing,
                fontSize: 12.5,
                color: palette.TextFaint,
                fontFamily: NetBenchFonts.Mono));
        }

        // Место под галочку занято всегда — иначе строки дёргаются при смене выбора.
        children.Add(new SizedBox(
            width: 20,
            height: 14,
            child: selected
                ? new Align(
                    alignment: Alignment.CenterRight,
                    child: new IconGlyph(GlyphKind.Check, palette.Rps, 14))
                : (Widget?)null));

        return new InkWell(
            onTap: onTap,
            child: new Container(
                // 15 + 15 + строка ≈ 52 — комфортная цель для пальца
                padding: new Avalonia.Thickness(16, 15),
                child: new Row(spacing: 10, children: children)));
    }

    private static Column BuildFooter(NetBenchPalette palette, Strings strings) =>
        new Column(
            crossAxisAlignment: CrossAxisAlignment.Center,
            children:
            [
                new Text(
                    strings.App.Title,
                    fontSize: 13,
                    fontWeight: FontWeight.Bold,
                    color: palette.TextDim,
                    fontFamily: NetBenchFonts.Ui),
                new SizedBox(height: 2),
                new Text(
                    strings.App.Tagline,
                    fontSize: 12,
                    color: palette.TextFaint,
                    fontFamily: NetBenchFonts.Ui),
            ]);

    /// <summary>«ru-RU» → «Русский»: родное имя языка без страны, с заглавной буквы.</summary>
    private static string DisplayName(CultureInfo culture)
    {
        var native = culture.NativeName;
        var cut = native.IndexOf(" (", StringComparison.Ordinal);
        if (cut > 0)
            native = native[..cut];

        return native.Length == 0
            ? culture.Name
            : string.Concat(native[..1].ToUpper(culture), native[1..]);
    }
}
