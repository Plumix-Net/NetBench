using System.Globalization;
using Avalonia.Media;
using NetBench.Features.Settings.Domain;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.UI;
using Plumix.Widgets;

namespace NetBench.Features.Settings.Presentation.Mobile;

/// <summary>
/// Настройки приложения: язык интерфейса и оформление.
/// Обе настройки применяются сразу — язык через <see cref="LocaleSettings{T}"/>
/// (перестраивает всех, кто читал строки из контекста), тема через <see cref="NetBenchTheme"/> —
/// и сохраняются в <see cref="IAppSettingsStore"/>, чтобы пережить перезапуск.
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
                                            BuildLanguageGroup(palette, strings),
                                            BuildSectionHead(palette, strings.Settings.Appearance),
                                            BuildThemeGroup(context, palette, strings),
                                            new SizedBox(height: 26),
                                            BuildGroup(
                                                palette,
                                                [
                                                    BuildNavigationRow(
                                                        palette,
                                                        strings.Settings.About,
                                                        () => _ = Navigator.Of(context).Push(new MaterialPageRoute(
                                                            static _ => new AboutScreen(),
                                                            new RouteSettings("about")))),
                                                ]),
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
                    new ToolButton(
                        icon: new Icon(Icons.ArrowBackIosNewRounded, size: 16, color: palette.Text),
                        onPressed: () => Navigator.Pop(context),
                        tooltip: Translations<Strings>.Of(context).Mobile.Back,
                        size: 34,
                        radius: 8),
                    new Expanded(new ScreenTitle(title)),
                ]));

    private static Padding BuildSectionHead(NetBenchPalette palette, string title) =>
        new Padding(
            new Avalonia.Thickness(0, 26, 0, 12),
            new Semantics(header: true, child: new Text(
                title.ToUpperInvariant(),
                style: new TextStyle(
                    FontSize: 12,
                    FontWeight: FontWeight.Bold,
                    Color: palette.TextMid,
                    LetterSpacing: 0.6,
                    FontFamily: NetBenchFonts.Ui))));

    /// <summary>
    /// StatefulBuilder: «как в системе» может разрешиться в ту же культуру, что уже активна, —
    /// тогда TranslationProvider ничего не перестроит, и галочку переставляем сами.
    /// </summary>
    private static StatefulBuilder BuildLanguageGroup(NetBenchPalette palette, Strings strings) =>
        new((context, setState) =>
        {
            var store = RepositoryProvider<IAppSettingsStore>.Of(context);
            var saved = store.Current.Culture;
            var current = Translations<Strings>.CultureOf(context);

            void Select(string? culture)
            {
                setState(() => store.Save(store.Current with { Culture = culture }));
                if (culture is null)
                    LocaleSettings<Strings>.UseDeviceLocale();
                else
                    LocaleSettings<Strings>.SetLocaleRaw(culture);
            }

            return BuildGroup(
                palette,
                [
                    BuildOptionRow(
                        palette,
                        strings.Settings.LanguageSystem,
                        trailing: null,
                        selected: saved is null,
                        onTap: saved is null ? null : () => Select(null)),
                    .. LocaleSettings<Strings>.SupportedCultures.Select(Widget (culture) =>
                    {
                        // Выбранной считаем только явно сохранённую культуру: «как в системе» тоже
                        // разрешается в одну из них, но это другой выбор.
                        var selected = saved is not null
                            && string.Equals(culture.Name, current.Name, StringComparison.OrdinalIgnoreCase);
                        return BuildOptionRow(
                            palette,
                            DisplayName(culture),
                            culture.TwoLetterISOLanguageName.ToUpperInvariant(),
                            selected,
                            onTap: selected ? null : () => Select(culture.Name));
                    }),
                ]);
        });

    private static Material BuildThemeGroup(BuildContext context, NetBenchPalette palette, Strings strings)
    {
        var theme = NetBenchTheme.Of(context);

        // Повторный тап по выбранной строке ничего не делает.
        Widget Row(string label, ThemePreference preference) => BuildOptionRow(
            palette,
            label,
            trailing: null,
            selected: theme.Preference == preference,
            onTap: theme.Preference == preference ? null : () => theme.SetPreference(preference));

        return BuildGroup(
            palette,
            [
                Row(strings.Settings.ThemeSystem, ThemePreference.System),
                Row(strings.Settings.ThemeDark, ThemePreference.Dark),
                Row(strings.Settings.ThemeLight, ThemePreference.Light),
            ]);
    }

    /// <summary>Карточка-группа: строки настроек, разделённые тонкой линией.</summary>
    internal static Material BuildGroup(NetBenchPalette palette, IReadOnlyList<Widget> rows)
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

    private static Semantics BuildOptionRow(
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
                    style: new TextStyle(
                        FontSize: 15,
                        FontWeight: selected ? FontWeight.SemiBold : FontWeight.Normal,
                        Color: selected ? palette.TextHi : palette.Text,
                        FontFamily: NetBenchFonts.Ui),
                    maxLines: 1)),
        };

        if (!string.IsNullOrEmpty(trailing))
        {
            children.Add(new Text(
                trailing,
                style: new TextStyle(
                    FontSize: 12.5,
                    Color: palette.TextFaint,
                    FontFamily: NetBenchFonts.Mono)));
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

        // Строки группы — взаимоисключающий выбор: TalkBack/VoiceOver объявляют «выбрано» у галочки.
        return new Semantics(
            button: true,
            selected: selected,
            inMutuallyExclusiveGroup: true,
            child: new InkWell(
                onTap: onTap,
                child: new Container(
                    // 15 + 15 + строка ≈ 52 — комфортная цель для пальца
                    padding: new Avalonia.Thickness(16, 15),
                    child: new Row(spacing: 10, children: children))));
    }

    /// <summary>Строка-переход на другой экран (или внешняя ссылка — <paramref name="link"/>): подпись и шеврон.</summary>
    internal static Semantics BuildNavigationRow(NetBenchPalette palette, string label, Action onTap, bool link = false) =>
        new(
            button: !link,
            link: link,
            child: new InkWell(
                onTap: onTap,
                child: new Container(
                    padding: new Avalonia.Thickness(16, 15),
                    child: new Row(
                        spacing: 10,
                        children:
                        [
                            new Expanded(
                                new Text(
                                    label,
                                    style: new TextStyle(FontSize: 15, Color: palette.Text, FontFamily: NetBenchFonts.Ui),
                                    maxLines: 2)),
                            new IconGlyph(GlyphKind.ChevronRight, palette.TextFaint, 12),
                        ]))));

    private static Column BuildFooter(NetBenchPalette palette, Strings strings) =>
        new Column(
            crossAxisAlignment: CrossAxisAlignment.Center,
            children:
            [
                new Text(
                    strings.App.Title,
                    style: new TextStyle(
                        FontSize: 13,
                        FontWeight: FontWeight.Bold,
                        Color: palette.TextDim,
                        FontFamily: NetBenchFonts.Ui)),
                new SizedBox(height: 2),
                new Text(
                    strings.App.Tagline,
                    style: new TextStyle(
                        FontSize: 12,
                        Color: palette.TextFaint,
                        FontFamily: NetBenchFonts.Ui)),
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
