using System.Globalization;
using Avalonia.Media;
using NetBench.Features.Report.Presentation.Mobile;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Settings.Presentation.Mobile;
using NetBench.Features.TestRun.Domain;
using NetBench.Features.TestRun.Presentation.Mobile;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Navigation;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.UI;
using Plumix.Widgets;
using AlertDialog = Plumix.Material.AlertDialog;
using DismissDirection = Plumix.Widgets.DismissDirection;

namespace NetBench.Features.Scenarios.Presentation.Mobile;

/// <summary>
/// Домашний экран мобильного приложения: заголовок с переключателем темы,
/// карточки сценариев с чипом последнего прогона и кнопка быстрого теста.
/// </summary>
public sealed class ScenarioListScreen : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);

        return new Scaffold(
            body: new SafeArea(
                new Stack(
                    fit: StackFit.Expand,
                    children:
                    [
                        new Padding(
                            new Avalonia.Thickness(20, 12, 20, 0),
                            new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                children:
                                [
                                    BuildHeader(context, palette, strings.App.Title, strings.Mobile.HomeSubtitle),
                                    new SizedBox(height: 20),
                                    new Expanded(
                                        new BlocBuilder<ScenarioListCubit, ScenarioListState>(
                                            builder: BuildBody)),
                                ])),
                        new Positioned(
                            left: 20,
                            right: 20,
                            bottom: 28,
                            child: BuildQuickTestButton(context, palette, strings.Mobile.QuickTest)),
                    ])));
    }

    private static Row BuildHeader(BuildContext context, NetBenchPalette palette, string title, string subtitle)
    {
        var theme = NetBenchTheme.Of(context);

        return new Row(
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            crossAxisAlignment: CrossAxisAlignment.Center,
            children:
            [
                new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    mainAxisSize: MainAxisSize.Min,
                    children:
                    [
                        new Text(
                            title,
                            fontSize: 26,
                            fontWeight: FontWeight.ExtraBold,
                            color: palette.TextHi,
                            letterSpacing: -0.5,
                            fontFamily: NetBenchFonts.Ui),
                        new SizedBox(height: 2),
                        new Text(
                            subtitle,
                            fontSize: 13,
                            color: palette.TextMid,
                            fontFamily: NetBenchFonts.Ui),
                    ]),
                new Row(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 8,
                    children:
                    [
                        BuildSettingsButton(context, palette),
                        new Material(
                            color: palette.BgCard,
                            borderRadius: BorderRadius.Circular(9),
                            child: new IconButton(
                                icon: new IconGlyph(
                                    palette.IsDark ? GlyphKind.Moon : GlyphKind.Sun,
                                    palette.TextMid,
                                    17),
                                onPressed: theme.ToggleTheme,
                                constraints: BoxConstraints.Tight(new Avalonia.Size(36, 36)),
                                padding: default(Avalonia.Thickness))),
                    ]),
            ]);
    }

    /// <summary>Шестерёнка настроек: язык и оформление — на отдельном экране.</summary>
    private static Material BuildSettingsButton(BuildContext context, NetBenchPalette palette)
    {
        var radius = BorderRadius.Circular(9);

        return new Material(
            color: palette.BgCard,
            borderRadius: radius,
            child: new InkWell(
                onTap: () => Navigator.Of(context).Push(
                    ThemedPageRoute.Of(static _ => new SettingsScreen(), new RouteSettings("settings"))),
                borderRadius: radius,
                child: new Container(
                    width: 36,
                    height: 36,
                    alignment: Alignment.Center,
                    child: new IconGlyph(GlyphKind.Gear, palette.TextMid, 17))));
    }

    private static Widget BuildBody(BuildContext context, ScenarioListState state)
    {
        var strings = Translations<Strings>.Of(context);

        if (state.Status == ScenarioListStatus.Loading)
            return new Center(child: new CircularProgressIndicator());

        var palette = NetBenchTheme.PaletteOf(context);
        var cubit = context.Read<ScenarioListCubit>();

        // Bouncing-физика оставляет overscroll даже когда список короче экрана,
        // иначе RefreshIndicator не за что зацепиться на пустом состоянии.
        List<Widget> children = state.Status == ScenarioListStatus.Failure
            ? [BuildPlaceholder(palette, strings.Scenarios.LoadFailed(state.Error ?? string.Empty))]
            : state.Scenarios.Count == 0
                ? [BuildPlaceholder(palette, strings.Shell.EmptyDescription)]
                : [.. state.Scenarios.Select(scenario => BuildDismissibleCard(
                    context,
                    scenario,
                    state.LastRunOf(scenario.Id)))];

        // История прогонов: сюда попадают и быстрые тесты, чьего сценария нет в списке.
        if (state.RecentSessions.Count > 0)
        {
            children.Add(BuildSectionHead(palette, strings.Mobile.RecentSessions));
            children.AddRange(state.RecentSessions.Select(report => BuildSessionRow(context, report)));
        }

        return new RefreshIndicator(
            onRefresh: () => cubit.RefreshAsync(),
            color: palette.Rps,
            backgroundColor: palette.BgCard,
            child: new ListView(
                padding: new Avalonia.Thickness(0, 0, 0, 100),
                physics: new BouncingScrollPhysics(),
                children: children));
    }

    private static SizedBox BuildPlaceholder(NetBenchPalette palette, string message) =>
        new SizedBox(
            height: 220,
            child: new Center(
                child: new Text(
                    message,
                    fontSize: 13.5,
                    color: palette.TextMid,
                    textAlign: TextAlign.Center,
                    fontFamily: NetBenchFonts.Ui)));

    private static Padding BuildSectionHead(NetBenchPalette palette, string title) =>
        new Padding(
            new Avalonia.Thickness(0, 14, 0, 12),
            new Text(
                title.ToUpperInvariant(),
                fontSize: 12,
                fontWeight: FontWeight.Bold,
                color: palette.TextMid,
                letterSpacing: 0.6,
                fontFamily: NetBenchFonts.Ui));

    /// <summary>
    /// Строка истории: точка статуса, сценарий с хостом, RPS и время прогона.
    /// Плотнее карточки сценария — сессий накапливается много, и это не точка входа,
    /// а журнал; тап открывает сохранённый отчёт.
    /// </summary>
    private static Column BuildSessionRow(BuildContext context, TestRunReport report)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);
        var errorPct = report.Summary.ErrorRate * 100;
        var failed = errorPct >= 5;
        var host = string.IsNullOrWhiteSpace(report.Scenario.Target)
            ? strings.Scenarios.TargetNotSet
            : StripScheme(report.Scenario.Target);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new InkWell(
                    onTap: () => Navigator.Of(context).Push(
                        ThemedPageRoute.Of(_ => new ResultScreen(report))),
                    child: new Container(
                        padding: new Avalonia.Thickness(0, 12),
                        child: new Row(
                            spacing: 10,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            children:
                            [
                                new Container(
                                    width: 7,
                                    height: 7,
                                    decoration: new BoxDecoration(
                                        Color: failed ? palette.Error : palette.Success,
                                        Shape: BoxShape.Circle)),
                                new Expanded(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Start,
                                        mainAxisSize: MainAxisSize.Min,
                                        children:
                                        [
                                            new Text(
                                                DisplayName(strings, report.Scenario),
                                                fontSize: 14,
                                                fontWeight: FontWeight.SemiBold,
                                                color: palette.TextHi,
                                                fontFamily: NetBenchFonts.Ui,
                                                maxLines: 1),
                                            new SizedBox(height: 2),
                                            new Text(
                                                host,
                                                fontSize: 11.5,
                                                color: palette.TextFaint,
                                                fontFamily: NetBenchFonts.Mono,
                                                maxLines: 1),
                                        ])),
                                new Column(
                                    crossAxisAlignment: CrossAxisAlignment.End,
                                    mainAxisSize: MainAxisSize.Min,
                                    children:
                                    [
                                        new Text(
                                            strings.Mobile.SessionRps(FormatRps(report.Summary.RequestsPerSecond)),
                                            fontSize: 12.5,
                                            fontWeight: FontWeight.SemiBold,
                                            color: palette.Rps,
                                            fontFamily: NetBenchFonts.Mono,
                                            maxLines: 1),
                                        new SizedBox(height: 2),
                                        new Text(
                                            FormatFinishedAt(report.FinishedAt),
                                            fontSize: 11,
                                            color: palette.TextFaint,
                                            fontFamily: NetBenchFonts.Mono,
                                            maxLines: 1),
                                    ]),
                                new IconGlyph(GlyphKind.ChevronRight, palette.TextFaint, 12),
                            ]))),
                new Container(height: 1, color: palette.Border),
            ]);
    }

    /// <summary>
    /// Сегодняшние прогоны — только время, остальные — с датой. Культура берётся
    /// UI-шная: SetLocale по умолчанию меняет только её, а подпись должна следовать языку.
    /// </summary>
    private static string FormatFinishedAt(DateTime finishedAt)
    {
        var local = finishedAt.ToLocalTime();
        var culture = CultureInfo.CurrentUICulture;

        return local.Date == DateTime.Now.Date
            ? local.ToString("HH:mm", culture)
            : local.ToString("d MMM, HH:mm", culture);
    }

    /// <summary>
    /// Карточка со свайпом влево: подтверждение и удаление сценария из репозитория.
    /// Key обязателен — Dismissible опознаёт по нему элемент при перестроении списка.
    /// </summary>
    private static Dismissible BuildDismissibleCard(
        BuildContext context,
        LoadScenario scenario,
        TestRunReport? lastRun)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);
        var cubit = context.Read<ScenarioListCubit>();
        var name = DisplayName(strings, scenario);

        return new Dismissible(
            key: new ValueKey<Guid>(scenario.Id),
            direction: DismissDirection.EndToStart,
            background: BuildDeleteBackground(palette, strings.Mobile.Delete),
            confirmDismiss: _ => ConfirmDeleteAsync(context, name),
            onDismissed: direction =>
            {
                _ = cubit.DeleteScenarioAsync(scenario);
                ScaffoldMessenger.Of(context).ShowSnackBar(
                    new SnackBar(content: new Text(strings.Mobile.ScenarioDeleted(name))));
            },
            child: BuildCard(context, scenario, lastRun));
    }

    private static async Task<bool?> ConfirmDeleteAsync(BuildContext context, string name)
    {
        var strings = Translations<Strings>.Of(context);

        return await MaterialDialogs.ShowDialog<bool>(
            context,
            dialogContext => new AlertDialog(
                title: new Text(strings.Mobile.DeleteScenarioTitle),
                content: new Text(strings.Mobile.DeleteScenarioMessage(name)),
                actions:
                [
                    new TextButton(
                        child: new Text(strings.Common.Cancel),
                        onPressed: () => Navigator.Pop(dialogContext, false)),
                    new TextButton(
                        child: new Text(strings.Mobile.Delete),
                        onPressed: () => Navigator.Pop(dialogContext, true)),
                ]));
    }

    private static Container BuildDeleteBackground(NetBenchPalette palette, string label) =>
        new Container(
            margin: new Avalonia.Thickness(0, 0, 0, 12),
            padding: new Avalonia.Thickness(20, 0),
            alignment: Alignment.CenterRight,
            decoration: new BoxDecoration(
                Color: palette.Error,
                BorderRadius: BorderRadius.Circular(10)),
            child: new Text(
                label.ToUpperInvariant(),
                fontSize: 13,
                fontWeight: FontWeight.Bold,
                color: Colors.White,
                letterSpacing: 0.6,
                fontFamily: NetBenchFonts.Ui));

    private static Padding BuildCard(BuildContext context, LoadScenario scenario, TestRunReport? lastRun)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);

        var name = DisplayName(strings, scenario);
        var host = string.IsNullOrWhiteSpace(scenario.Target)
            ? strings.Scenarios.TargetNotSet
            : StripScheme(scenario.Target);

        Widget status = lastRun is null
            ? new Text(
                strings.Mobile.NoRunsChip,
                fontSize: 11.5,
                color: palette.TextFaint,
                fontFamily: NetBenchFonts.Ui)
            : new StatusChip(
                strings.Mobile.LastRunChip(
                    (lastRun.Summary.ErrorRate * 100).ToString("F1"),
                    FormatRps(lastRun.Summary.RequestsPerSecond)),
                lastRun.Summary.ErrorRate >= 0.05 ? StatusChipState.Bad : StatusChipState.Good);

        var radius = BorderRadius.Circular(10);

        return new Padding(
            new Avalonia.Thickness(0, 0, 0, 12),
            new Material(
                color: palette.BgCard,
                borderRadius: radius,
                clipBehavior: Clip.AntiAlias,
                child: new InkWell(
                    onTap: () => OpenScenario(context, scenario),
                    borderRadius: radius,
                    child: new Padding(
                        new Avalonia.Thickness(16),
                        new Column(
                            crossAxisAlignment: CrossAxisAlignment.Start,
                            children:
                            [
                                new Text(
                                    name,
                                    fontSize: 16,
                                    fontWeight: FontWeight.Bold,
                                    color: palette.TextHi,
                                    fontFamily: NetBenchFonts.Ui,
                                    maxLines: 1),
                                new SizedBox(height: 3),
                                new Text(
                                    host,
                                    fontSize: 12.5,
                                    color: palette.TextMid,
                                    fontFamily: NetBenchFonts.Mono,
                                    maxLines: 1),
                                new SizedBox(height: 12),
                                new Row(
                                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                                    children:
                                    [
                                        status,
                                        new IconGlyph(GlyphKind.ChevronRight, palette.TextFaint, 14),
                                    ]),
                            ])))));
    }

    private static string DisplayName(Strings strings, LoadScenario scenario) =>
        string.IsNullOrWhiteSpace(scenario.Name)
            ? strings.Scenarios.Unnamed
            : scenario.Name;

    private static void OpenScenario(BuildContext context, LoadScenario scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.Target))
        {
            ScaffoldMessenger.Of(context).ShowSnackBar(
                new SnackBar(content: new Text(Translations<Strings>.ReadOf(context).Shell.TargetRequired)));
            return;
        }

        Navigator.Of(context).Push(ThemedPageRoute.Of(_ => new MonitorScreen(scenario)));
    }

    private static Material BuildQuickTestButton(BuildContext context, NetBenchPalette palette, string label)
    {
        var radius = BorderRadius.Circular(100);

        return new Material(
            color: palette.Rps,
            borderRadius: radius,
            elevation: 6,
            shadowColor: palette.Rps,
            child: new InkWell(
                onTap: () => Navigator.Of(context).Push(ThemedPageRoute.Of(static _ => new QuickTestScreen())),
                borderRadius: radius,
                child: new Container(
                    height: 54,
                    alignment: Alignment.Center,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 8,
                        children:
                        [
                            new IconGlyph(GlyphKind.Plus, Colors.White, 14),
                            new Text(
                                label,
                                fontSize: 15,
                                fontWeight: FontWeight.Bold,
                                color: Colors.White,
                                fontFamily: NetBenchFonts.Ui),
                        ]))));
    }

    private static string StripScheme(string target)
    {
        var trimmed = target.Trim();
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return trimmed["https://".Length..];
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return trimmed["http://".Length..];
        return trimmed;
    }

    private static string FormatRps(double rps) =>
        rps >= 1000 ? $"{rps / 1000:F1}k" : rps.ToString("N0");
}
