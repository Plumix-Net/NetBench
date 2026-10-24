using Avalonia.Media;
using NetBench.Localization;
using NetBench.Mobile;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Licenses;
using NetBench.Mobile.Platform;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.Widgets;

namespace NetBench.Features.Settings.Presentation.Mobile;

/// <summary>
/// «О приложении»: версия и build (то же, что в сторах), ссылки на политику, условия,
/// обратную связь и исходники, лицензии открытого ПО (LicensePage из Plumix.Material).
/// </summary>
public sealed class AboutScreen : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);
        var version = strings.About.Version(AppInfo.Version, AppInfo.BuildNumber);

        return new Scaffold(
            body: new SafeArea(
                new Padding(
                    new Avalonia.Thickness(20, 8, 20, 0),
                    new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            BuildTopBar(context, palette, strings.About.Title),
                            new Expanded(
                                new SingleChildScrollView(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children:
                                        [
                                            new SizedBox(height: 20),
                                            BuildHeader(palette, strings, version),
                                            new SizedBox(height: 26),
                                            SettingsScreen.BuildGroup(
                                                palette,
                                                [
                                                    Link(context, palette, strings.About.Privacy, AppInfo.PrivacyPolicy),
                                                    Link(context, palette, strings.About.Terms, AppInfo.Terms),
                                                    Link(context, palette, strings.About.Feedback, AppInfo.Issues),
                                                    Link(context, palette, strings.About.SourceCode, AppInfo.SourceCode),
                                                ]),
                                            new SizedBox(height: 20),
                                            SettingsScreen.BuildGroup(
                                                palette,
                                                [
                                                    SettingsScreen.BuildNavigationRow(
                                                        palette,
                                                        strings.About.Licenses,
                                                        () => ShowLicenses(context, strings, version)),
                                                ]),
                                            new SizedBox(height: 24),
                                        ]))),
                        ]))));
    }

    private static Padding BuildTopBar(BuildContext context, NetBenchPalette palette, string title) =>
        new(
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

    private static Column BuildHeader(NetBenchPalette palette, Strings strings, string version) =>
        new(
            crossAxisAlignment: CrossAxisAlignment.Start,
            children:
            [
                new Text(
                    strings.App.Title,
                    style: new TextStyle(
                        FontSize: 26,
                        FontWeight: FontWeight.ExtraBold,
                        Color: palette.TextHi,
                        LetterSpacing: -0.5,
                        FontFamily: NetBenchFonts.Ui)),
                new SizedBox(height: 2),
                new Text(
                    version,
                    style: new TextStyle(FontSize: 13, Color: palette.TextMid, FontFamily: NetBenchFonts.Mono)),
                new SizedBox(height: 14),
                new Text(
                    strings.About.Description,
                    style: new TextStyle(FontSize: 14.5, Color: palette.Text, Height: 1.4, FontFamily: NetBenchFonts.Ui)),
                new SizedBox(height: 10),
                new Text(
                    strings.About.PoweredBy,
                    style: new TextStyle(FontSize: 13, Color: palette.TextMid, FontFamily: NetBenchFonts.Ui)),
            ]);

    private static Semantics Link(BuildContext context, NetBenchPalette palette, string label, Uri uri) =>
        SettingsScreen.BuildNavigationRow(palette, label, () => _ = OpenAsync(context, uri), link: true);

    private static async Task OpenAsync(BuildContext context, Uri uri)
    {
        var platform = RepositoryProvider<IPlatformServices>.ReadOf(context);
        var messenger = ScaffoldMessenger.Of(context);
        var strings = Translations<Strings>.ReadOf(context);

        if (!await platform.OpenUrlAsync(uri))
            messenger.ShowSnackBar(new SnackBar(content: new Text(strings.About.OpenFailed)));
    }

    private static void ShowLicenses(BuildContext context, Strings strings, string version)
    {
        OssLicenses.Register();
        AboutDialogs.ShowLicensePage(
            context,
            applicationName: strings.App.Title,
            applicationVersion: version,
            applicationLegalese: strings.About.PoweredBy);
    }
}
