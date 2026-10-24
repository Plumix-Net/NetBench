using Avalonia.Media;
using NetBench.Features.Report.Presentation.Mobile;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Settings.Presentation.Mobile;
using NetBench.Features.TestRun.Domain;
using NetBench.Features.TestRun.Presentation.Mobile;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.UI;
using Plumix.Widgets;
using AlertDialog = Plumix.Material.AlertDialog;
using Colors = Plumix.Material.Colors;
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

        // Кнопка быстрого теста — в слоте FAB, а не поверх Stack: Scaffold поднимает её над
        // снекбаром, иначе снекбар («Некорректный URL», «Сценарий удалён») накрывал кнопку.
        return new Scaffold(
            floatingActionButton: new Padding(
                new Avalonia.Thickness(20, 0, 20, 12),
                new SizedBox(
                    width: double.PositiveInfinity,
                    child: BuildQuickTestButton(context, palette, strings.Mobile.QuickTest))),
            floatingActionButtonLocation: FloatingActionButtonLocation.CenterFloat,
            body: new SafeArea(
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
                        ]))));
    }

    private static Row BuildHeader(BuildContext context, NetBenchPalette palette, string title, string subtitle)
    {
        var theme = NetBenchTheme.Of(context);
        var strings = Translations<Strings>.Of(context);

        return new Row(
            mainAxisAlignment: MainAxisAlignment.SpaceBetween,
            crossAxisAlignment: CrossAxisAlignment.Center,
            spacing: 8,
            children:
            [
                // Expanded + scaleDown: при крупном шрифте заголовок ужимается, а не наезжает на кнопки
                new Expanded(new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    mainAxisSize: MainAxisSize.Min,
                    children:
                    [
                        new FittedBox(
                            fit: BoxFit.ScaleDown,
                            alignment: Alignment.CenterLeft,
                            child: new Semantics(
                                header: true,
                                child: new Text(
                                    title,
                                    style: new TextStyle(
                                        FontSize: 26,
                                        FontWeight: FontWeight.ExtraBold,
                                        Color: palette.TextHi,
                                        LetterSpacing: -0.5,
                                        FontFamily: NetBenchFonts.Ui)))),
                        new SizedBox(height: 2),
                        new Text(
                            subtitle,
                            style: new TextStyle(
                                FontSize: 13,
                                Color: palette.TextMid,
                                FontFamily: NetBenchFonts.Ui)),
                    ])),
                new Row(
                    mainAxisSize: MainAxisSize.Min,
                    spacing: 8,
                    children:
                    [
                        new ToolButton(
                            icon: new IconGlyph(GlyphKind.Plus, palette.TextMid, 15),
                            onPressed: () => _ = Navigator.Of(context).Push(
                                new MaterialPageRoute(static _ => new ScenarioEditorScreen())),
                            tooltip: strings.Scenarios.New),
                        new ToolButton(
                            icon: new IconGlyph(GlyphKind.Gear, palette.TextMid, 17),
                            onPressed: () => _ = Navigator.Of(context).Push(
                                new MaterialPageRoute(static _ => new SettingsScreen(), new RouteSettings("settings"))),
                            tooltip: strings.Settings.OpenSettings),
                        new ToolButton(
                            icon: new IconGlyph(palette.IsDark ? GlyphKind.Moon : GlyphKind.Sun, palette.TextMid, 17),
                            onPressed: theme.ToggleTheme,
                            tooltip: strings.Mobile.ToggleTheme),
                    ]),
            ]);
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
                ? [BuildPlaceholder(palette, strings.Mobile.EmptyHint)]
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
                    style: new TextStyle(
                        FontSize: 13.5,
                        Color: palette.TextMid,
                        FontFamily: NetBenchFonts.Ui),
                    textAlign: TextAlign.Center)));

    private static Padding BuildSectionHead(NetBenchPalette palette, string title) =>
        new Padding(
            new Avalonia.Thickness(0, 14, 0, 12),
            new Semantics(header: true, child: new Text(
                title.ToUpperInvariant(),
                style: new TextStyle(
                    FontSize: 12,
                    FontWeight: FontWeight.Bold,
                    Color: palette.TextMid,
                    LetterSpacing: 0.6,
                    FontFamily: NetBenchFonts.Ui))));

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
                new Semantics(
                    button: true,
                    child: new InkWell(
                        onTap: () => _ = Navigator.Of(context).Push(
                            new MaterialPageRoute(_ => new ResultScreen(report))),
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
                                                    strings.DisplayNameOf(report.Scenario),
                                                    style: new TextStyle(
                                                        FontSize: 14,
                                                        FontWeight: FontWeight.SemiBold,
                                                        Color: palette.TextHi,
                                                        FontFamily: NetBenchFonts.Ui),
                                                    maxLines: 1),
                                                new SizedBox(height: 2),
                                                new Text(
                                                    host,
                                                    style: new TextStyle(
                                                        FontSize: 11.5,
                                                        Color: palette.TextFaint,
                                                        FontFamily: NetBenchFonts.Mono),
                                                    maxLines: 1),
                                            ])),
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.End,
                                        mainAxisSize: MainAxisSize.Min,
                                        children:
                                        [
                                            new Text(
                                                strings.Mobile.SessionRps(FormatRps(report.Summary.RequestsPerSecond)),
                                                style: new TextStyle(
                                                    FontSize: 12.5,
                                                    FontWeight: FontWeight.SemiBold,
                                                    Color: palette.Rps,
                                                    FontFamily: NetBenchFonts.Mono),
                                                maxLines: 1),
                                            new SizedBox(height: 2),
                                            new Text(
                                                FormatFinishedAt(report.FinishedAt),
                                                style: new TextStyle(
                                                    FontSize: 11,
                                                    Color: palette.TextFaint,
                                                    FontFamily: NetBenchFonts.Mono),
                                                maxLines: 1),
                                        ]),
                                    new IconGlyph(GlyphKind.ChevronRight, palette.TextFaint, 12),
                                ])))),
                new Container(height: 1, color: palette.Border),
            ]);
    }

    /// <summary>
    /// Сегодняшние прогоны — только время, остальные — с датой; подпись следует языку интерфейса.
    /// </summary>
    private static string FormatFinishedAt(DateTime finishedAt)
    {
        var local = finishedAt.ToLocalTime();
        var culture = DisplayCulture.Current;

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
        var name = strings.DisplayNameOf(scenario);

        return new Dismissible(
            key: new ValueKey<Guid>(scenario.Id),
            direction: DismissDirection.EndToStart,
            background: BuildDeleteBackground(palette, strings.Mobile.Delete),
            confirmDismiss: _ => ConfirmDeleteAsync(context, name),
            onDismissed: direction => _ = DeleteScenarioAsync(context, scenario, name),
            child: new Semantics(
                // Свайп недоступен с TalkBack/VoiceOver — удаление дублируется действием в меню скринридера
                customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
                {
                    [new CustomSemanticsAction(strings.Mobile.Delete)] = () => _ = DeleteWithConfirmationAsync(
                        context,
                        scenario,
                        name),
                },
                child: BuildCard(context, scenario, lastRun)));
    }

    private static async Task DeleteWithConfirmationAsync(BuildContext context, LoadScenario scenario, string name)
    {
        if (await ConfirmDeleteAsync(context, name) == true)
            await DeleteScenarioAsync(context, scenario, name);
    }

    /// <summary>
    /// Кубит убирает карточку синхронно, а снекбар ждёт записи на диск: «удалён» говорим,
    /// только если файл действительно удалён, иначе карточка вернулась — сообщаем об ошибке.
    /// </summary>
    private static async Task DeleteScenarioAsync(BuildContext context, LoadScenario scenario, string name)
    {
        // Всё из контекста берём до await — после него элемент списка уже размонтирован.
        var messenger = ScaffoldMessenger.Of(context);
        var strings = Translations<Strings>.ReadOf(context);
        var deleted = await context.Read<ScenarioListCubit>().DeleteScenarioAsync(scenario);

        messenger.ShowSnackBar(new SnackBar(content: new Text(deleted
            ? strings.Mobile.ScenarioDeleted(name)
            : strings.Mobile.ScenarioDeleteFailed(name))));
    }

    private static async Task<bool?> ConfirmDeleteAsync(BuildContext context, string name)
    {
        var strings = Translations<Strings>.ReadOf(context);

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
                style: new TextStyle(
                    FontSize: 13,
                    FontWeight: FontWeight.Bold,
                    Color: Colors.White,
                    LetterSpacing: 0.6,
                    FontFamily: NetBenchFonts.Ui)));

    private static Padding BuildCard(BuildContext context, LoadScenario scenario, TestRunReport? lastRun)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);

        var name = strings.DisplayNameOf(scenario);
        var host = string.IsNullOrWhiteSpace(scenario.Target)
            ? strings.Scenarios.TargetNotSet
            : StripScheme(scenario.Target);

        Widget status = lastRun is null
            ? new Text(
                strings.Mobile.NoRunsChip,
                style: new TextStyle(
                    FontSize: 11.5,
                    Color: palette.TextFaint,
                    FontFamily: NetBenchFonts.Ui))
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
                child: new Semantics(
                    button: true,
                    child: new InkWell(
                        onTap: () => _ = OpenScenarioAsync(context, scenario),
                        borderRadius: radius,
                        child: new Padding(
                            new Avalonia.Thickness(16),
                            new Column(
                                crossAxisAlignment: CrossAxisAlignment.Start,
                                children:
                                [
                                    new Text(
                                        name,
                                        style: new TextStyle(
                                            FontSize: 16,
                                            FontWeight: FontWeight.Bold,
                                            Color: palette.TextHi,
                                            FontFamily: NetBenchFonts.Ui),
                                        maxLines: 1),
                                    new SizedBox(height: 3),
                                    new Text(
                                        host,
                                        style: new TextStyle(
                                            FontSize: 12.5,
                                            Color: palette.TextMid,
                                            FontFamily: NetBenchFonts.Mono),
                                        maxLines: 1),
                                    new SizedBox(height: 12),
                                    new Row(
                                        mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                                        children:
                                        [
                                            status,
                                            // Тап по карточке запускает прогон, правка — отдельной кнопкой
                                            new ToolButton(
                                                icon: new Icon(Icons.EditOutlined, size: 16, color: palette.TextMid),
                                                onPressed: () => _ = Navigator.Of(context).Push(
                                                    new MaterialPageRoute(_ => new ScenarioEditorScreen(scenario))),
                                                tooltip: strings.Scenarios.Edit,
                                                size: 32,
                                                radius: 8),
                                        ]),
                                ]))))));
    }

    private static async Task OpenScenarioAsync(BuildContext context, LoadScenario scenario)
    {
        // Сценарии приходят и с desktop-редактора, где Target не валидируется:
        // с битым адресом прогон отработал бы всю длительность одними ошибками.
        if (!TargetUrl.IsValid(scenario.Target))
        {
            var strings = Translations<Strings>.ReadOf(context);
            var message = string.IsNullOrWhiteSpace(scenario.Target)
                ? strings.Shell.TargetRequired
                : strings.Mobile.InvalidUrl;
            ScaffoldMessenger.Of(context).ShowSnackBar(new SnackBar(content: new Text(message)));
            return;
        }

        var navigator = Navigator.Of(context);
        if (await RunConsent.EnsureAsync(context))
            _ = navigator.Push(new MaterialPageRoute(_ => new MonitorScreen(scenario)));
    }

    private static Material BuildQuickTestButton(BuildContext context, NetBenchPalette palette, string label)
    {
        var radius = BorderRadius.Circular(100);

        return new Material(
            color: palette.Rps,
            borderRadius: radius,
            elevation: 6,
            shadowColor: palette.Rps,
            child: new Semantics(
                button: true,
                child: new InkWell(
                    onTap: () => _ = Navigator.Of(context).Push(new MaterialPageRoute(static _ => new QuickTestScreen())),
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
                                    style: new TextStyle(
                                        FontSize: 15,
                                        FontWeight: FontWeight.Bold,
                                        Color: Colors.White,
                                        FontFamily: NetBenchFonts.Ui)),
                            ])))));
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
