using Avalonia.Media;
using NetBench.Features.Report.Presentation.Mobile;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Theme;
using Plumix;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.Widgets;
using AlertDialog = Plumix.Material.AlertDialog;

namespace NetBench.Features.TestRun.Presentation.Mobile;

/// <summary>Экран живого прогона: большие метрики, спарклайн RPS, прогресс и «Стоп».</summary>
public sealed class MonitorScreen : StatelessWidget
{
    private readonly LoadScenario _scenario;

    public MonitorScreen(LoadScenario scenario)
    {
        _scenario = scenario;
    }

    public override Widget Build(BuildContext context)
    {
        return new BlocProvider<TestRunCubit>(
            create: providerContext =>
            {
                var cubit = new TestRunCubit(
                    _scenario,
                    RepositoryProvider<IReportStore>.ReadOf(providerContext));
                _ = cubit.StartAsync();
                return cubit;
            },
            child: new Builder(BuildScaffold));
    }

    /// <summary>
    /// PopScope перехватывает системный «назад»: прогон нельзя бросить молча —
    /// спрашиваем подтверждение и останавливаем движок (отчёт придёт через RunCompleted).
    /// </summary>
    private static PopScope<object> BuildScaffold(BuildContext context)
    {
        return new PopScope<object>(
            canPop: false,
            onPopInvokedWithResult: (didPop, _) =>
            {
                if (!didPop)
                    _ = ConfirmAbortAsync(context);
            },
            child: new Scaffold(
                body: new SafeArea(
                    new Padding(
                        new Avalonia.Thickness(20, 12, 20, 0),
                        new BlocConsumer<TestRunCubit, TestRunState>(
                            listener: OnStateChanged,
                            listenWhen: static (previous, next) => previous.Phase != next.Phase,
                            builder: BuildBody)))));
    }

    private static async Task ConfirmAbortAsync(BuildContext context)
    {
        var strings = Translations<Strings>.Of(context);
        var cubit = context.Read<TestRunCubit>();

        var confirmed = await MaterialDialogs.ShowDialog<bool>(
            context,
            dialogContext => new AlertDialog(
                title: new Text(strings.Mobile.AbortRunTitle),
                content: new Text(strings.Mobile.AbortRunMessage),
                actions:
                [
                    new TextButton(
                        child: new Text(strings.Common.Cancel),
                        onPressed: () => Navigator.Pop(dialogContext, false)),
                    new TextButton(
                        child: new Text(strings.Mobile.AbortRunConfirm),
                        onPressed: () => Navigator.Pop(dialogContext, true)),
                ]));

        if (confirmed == true)
            cubit.Stop();
    }

    /// <summary>Прогон завершён — заменяем монитор экраном результата (домой можно вернуться кнопкой «назад»).</summary>
    private static void OnStateChanged(BuildContext context, TestRunState state)
    {
        if (state.Phase != TestRunPhase.Completed || state.Report is null)
            return;

        var report = state.Report;
        Navigator.Of(context).PushAndRemoveUntil(
            new BuilderPageRoute(_ => new ResultScreen(report), settings: new RouteSettings("result")),
            static route => route.Settings.Name == "home");
    }

    private static Column BuildBody(BuildContext context, TestRunState state)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);
        var cubit = context.Read<TestRunCubit>();
        var scenario = cubit.Scenario;
        var stats = state.Stats;

        var errorRatePct = (stats?.ErrorRate ?? 0) * 100;
        var errorColor = errorRatePct >= 5 ? palette.Error : palette.Success;
        var duration = scenario.Load.Duration;
        var elapsed = stats?.Elapsed > duration ? duration : stats?.Elapsed ?? TimeSpan.Zero;
        var progress = duration > TimeSpan.Zero
            ? Math.Min(1, elapsed / duration)
            : 0;

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new Row(
                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                    children:
                    [
                        new Text(
                            scenario.Name,
                            fontSize: 13,
                            color: palette.TextMid,
                            fontFamily: NetBenchFonts.Ui,
                            maxLines: 1),
                        new Row(
                            mainAxisSize: MainAxisSize.Min,
                            spacing: 6,
                            children:
                            [
                                new Container(
                                    width: 8,
                                    height: 8,
                                    decoration: new BoxDecoration(
                                        Color: palette.Error,
                                        Shape: BoxShape.Circle)),
                                new Text(
                                    strings.Mobile.Rec,
                                    fontSize: 12,
                                    fontWeight: FontWeight.Bold,
                                    color: palette.Error,
                                    fontFamily: NetBenchFonts.Ui),
                            ]),
                    ]),
                BuildMetricRow(
                    palette,
                    strings.Mobile.RequestsPerSecond,
                    palette.Rps,
                    stats?.RequestsPerSecond ?? 0,
                    "N0",
                    unit: null,
                    valueColor: palette.Rps),
                BuildDivider(palette),
                BuildMetricRow(
                    palette,
                    strings.Mobile.P95Latency,
                    palette.Latency,
                    stats?.LatencyP95Ms ?? 0,
                    "F0",
                    unit: strings.Common.MillisecondsShort,
                    valueColor: palette.Latency),
                BuildDivider(palette),
                BuildMetricRow(
                    palette,
                    strings.Mobile.ErrorRate,
                    errorColor,
                    errorRatePct,
                    "F1",
                    unit: strings.Common.Percent,
                    valueColor: errorColor),
                new Padding(
                    new Avalonia.Thickness(0, 18, 0, 6),
                    new Sparkline(state.RpsPoints, palette.Rps, palette.RpsTint)),
                BuildProgress(palette, progress, Format(elapsed), Format(duration)),
                new Spacer(),
                BuildStopButton(palette, strings.Mobile.Stop, cubit.Stop),
                new SizedBox(height: 24),
            ]);
    }

    private static Container BuildDivider(NetBenchPalette palette) =>
        new Container(height: 1, color: palette.Border);

    private static Padding BuildMetricRow(
        NetBenchPalette palette,
        string label,
        Color dotColor,
        double value,
        string format,
        string? unit,
        Color valueColor)
    {
        // Снапшоты статистики приходят раз в ~250 мс — TweenAnimationBuilder
        // «докручивает» число между ними, чтобы счётчик не прыгал ступеньками.
        var valueRow = new List<Widget>
        {
            new TweenAnimationBuilder<double>(
                tween: new DoubleTween(end: value),
                duration: TimeSpan.FromMilliseconds(250),
                curve: Curves.Linear,
                builder: (_, animated, _) => new Text(
                    animated.ToString(format),
                    fontSize: 52,
                    fontWeight: FontWeight.ExtraBold,
                    color: valueColor,
                    fontFamily: NetBenchFonts.Mono,
                    height: 1.1,
                    maxLines: 1)),
        };
        if (!string.IsNullOrEmpty(unit))
        {
            valueRow.Add(new Padding(
                new Avalonia.Thickness(0, 0, 0, 9),
                new Text(
                    unit,
                    fontSize: 20,
                    fontWeight: FontWeight.ExtraBold,
                    color: valueColor,
                    fontFamily: NetBenchFonts.Mono)));
        }

        return new Padding(
            new Avalonia.Thickness(0, 14),
            new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                children:
                [
                    new Row(
                        spacing: 8,
                        children:
                        [
                            new Container(
                                width: 7,
                                height: 7,
                                decoration: new BoxDecoration(Color: dotColor, Shape: BoxShape.Circle)),
                            new Text(
                                label.ToUpperInvariant(),
                                fontSize: 12,
                                fontWeight: FontWeight.SemiBold,
                                color: palette.TextMid,
                                letterSpacing: 0.6,
                                fontFamily: NetBenchFonts.Ui),
                        ]),
                    new SizedBox(height: 2),
                    new Row(
                        mainAxisSize: MainAxisSize.Min,
                        crossAxisAlignment: CrossAxisAlignment.End,
                        children: valueRow),
                ]));
    }

    private static Column BuildProgress(
        NetBenchPalette palette,
        double progress,
        string elapsedLabel,
        string totalLabel)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            children:
            [
                new LinearProgressIndicator(
                    value: Math.Clamp(progress, 0, 1),
                    minHeight: 6,
                    color: palette.Rps,
                    backgroundColor: palette.Border,
                    borderRadius: BorderRadius.Circular(100)),
                new SizedBox(height: 8),
                new Row(
                    mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                    children:
                    [
                        new Text(elapsedLabel, fontSize: 12.5, color: palette.TextMid, fontFamily: NetBenchFonts.Mono),
                        new Text(totalLabel, fontSize: 12.5, color: palette.TextMid, fontFamily: NetBenchFonts.Mono),
                    ]),
            ]);
    }

    private static Material BuildStopButton(NetBenchPalette palette, string label, Action onTap)
    {
        var radius = BorderRadius.Circular(16);

        return new Material(
            color: palette.Error,
            borderRadius: radius,
            child: new InkWell(
                onTap: onTap,
                borderRadius: radius,
                child: new Container(
                    height: 64,
                    alignment: Alignment.Center,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 10,
                        children:
                        [
                            new IconGlyph(GlyphKind.Stop, Colors.White, 14),
                            new Text(
                                label,
                                fontSize: 17,
                                fontWeight: FontWeight.ExtraBold,
                                color: Colors.White,
                                fontFamily: NetBenchFonts.Ui),
                        ]))));
    }

    private static string Format(TimeSpan time) =>
        $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
}
