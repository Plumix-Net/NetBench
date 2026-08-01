using System.Globalization;
using Avalonia.Media;
using NetBench.Features.TestRun.Domain;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Theme;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.Widgets;

namespace NetBench.Features.Report.Presentation.Mobile;

/// <summary>Экран результата прогона: сводка, сетка метрик, распределение задержек и коды ответов.</summary>
public sealed class ResultScreen : StatelessWidget
{
    private readonly TestRunReport _report;

    public ResultScreen(TestRunReport report)
    {
        _report = report;
    }

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
                            BuildTopBar(context, palette, strings.Mobile.ResultTitle),
                            new Expanded(
                                new SingleChildScrollView(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children:
                                        [
                                            BuildSummaryCard(strings, palette),
                                            new SizedBox(height: 20),
                                            BuildMetricGrid(strings),
                                            BuildSectionHead(palette, strings.Mobile.LatencyDistribution),
                                            BuildLatencyRows(strings),
                                            BuildSectionHead(palette, strings.Mobile.StatusCodes),
                                            .. BuildStatusRows(strings, palette),
                                            new SizedBox(height: 24),
                                            BuildShareButton(context, strings, palette),
                                            new SizedBox(height: 24),
                                        ]))),
                        ]))));
    }

    private static Padding BuildTopBar(BuildContext context, NetBenchPalette palette, string title)
    {
        return new Padding(
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
    }

    private Container BuildSummaryCard(Strings strings, NetBenchPalette palette)
    {
        var scenario = _report.Scenario;
        var seconds = (_report.FinishedAt - _report.StartedAt).TotalSeconds;
        var meta = strings.Mobile.RunMeta(
            _report.FinishedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            seconds.ToString("F0"));

        return new Container(
            padding: new Avalonia.Thickness(18),
            decoration: new BoxDecoration(
                Color: palette.BgCard,
                BorderRadius: BorderRadius.Circular(10)),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                children:
                [
                    new Text(
                        scenario.Name,
                        fontSize: 19,
                        fontWeight: FontWeight.ExtraBold,
                        color: palette.TextHi,
                        fontFamily: NetBenchFonts.Ui,
                        maxLines: 1),
                    new SizedBox(height: 3),
                    // SelectableText: длинный URL можно выделить и скопировать по частям
                    new SelectableText(
                        scenario.Target,
                        style: new TextStyle(
                            FontFamily: NetBenchFonts.Mono,
                            FontSize: 12.5,
                            Color: palette.TextMid),
                        maxLines: 1),
                    new SizedBox(height: 10),
                    new Text(
                        meta,
                        fontSize: 11.5,
                        color: palette.TextFaint,
                        fontFamily: NetBenchFonts.Mono,
                        maxLines: 1),
                ]));
    }

    private Column BuildMetricGrid(Strings strings)
    {
        var summary = _report.Summary;
        var errorPct = summary.ErrorRate * 100;
        var errorTone = errorPct >= 5 ? MetricTone.Error : MetricTone.Success;
        var ms = strings.Common.MillisecondsShort;

        // Не Stretch: в скролле высота не ограничена, растяжение по вертикали дало бы Infinity
        Widget Pair(Widget left, Widget right) => new Row(
            spacing: 10,
            crossAxisAlignment: CrossAxisAlignment.Start,
            children: [new Expanded(left), new Expanded(right)]);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 10,
            children:
            [
                Pair(
                    new MetricCard(strings.Mobile.AvgRps, summary.RequestsPerSecond.ToString("N0"), tone: MetricTone.Rps),
                    new MetricCard(strings.Mobile.TotalRequests, summary.TotalRequests.ToString("N0"))),
                Pair(
                    new MetricCard(strings.Mobile.P95Latency, summary.LatencyP95Ms.ToString("F0"), ms, MetricTone.Latency),
                    new MetricCard(strings.Mobile.P99Latency, summary.LatencyP99Ms.ToString("F0"), ms, MetricTone.Latency)),
                Pair(
                    new MetricCard(strings.Mobile.ErrorRate, errorPct.ToString("F1"), strings.Common.Percent, errorTone),
                    new MetricCard(strings.Mobile.P50Latency, summary.LatencyP50Ms.ToString("F0"), ms, MetricTone.Latency)),
            ]);
    }

    private static Padding BuildSectionHead(NetBenchPalette palette, string title)
    {
        return new Padding(
            new Avalonia.Thickness(0, 26, 0, 12),
            new Text(
                title.ToUpperInvariant(),
                fontSize: 12,
                fontWeight: FontWeight.Bold,
                color: palette.TextMid,
                letterSpacing: 0.6,
                fontFamily: NetBenchFonts.Ui));
    }

    private Column BuildLatencyRows(Strings strings)
    {
        var summary = _report.Summary;
        (string Label, double Ms)[] rows =
        [
            (strings.Report.LatencyMin, summary.LatencyMinMs),
            (strings.Common.P50, summary.LatencyP50Ms),
            (strings.Report.LatencyMean, summary.LatencyMeanMs),
            (strings.Common.P95, summary.LatencyP95Ms),
            (strings.Common.P99, summary.LatencyP99Ms),
            (strings.Report.LatencyMax, summary.LatencyMaxMs),
        ];
        var max = Math.Max(1, summary.LatencyMaxMs);

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 9,
            children:
            [
                .. rows.Select(Widget (row) => new LatencyBar(
                    row.Label,
                    row.Ms / max,
                    strings.Report.LatencyValue(row.Ms.ToString("F0")))),
            ]);
    }

    private IEnumerable<Widget> BuildStatusRows(Strings strings, NetBenchPalette palette)
    {
        var total = Math.Max(1, _report.Summary.TotalRequests);

        foreach (var bucket in _report.StatusCodes.OrderBy(b => b.StatusCode == 0 ? 999 : b.StatusCode))
        {
            var color = StatusColor(palette, bucket.StatusCode);
            var codeLabel = bucket.StatusCode == 0 ? "ERR" : bucket.StatusCode.ToString();
            var pct = bucket.Count * 100.0 / total;

            yield return new Container(
                padding: new Avalonia.Thickness(0, 10),
                decoration: new BoxDecoration(),
                child: new Row(
                    spacing: 10,
                    children:
                    [
                        new Container(
                            width: 7,
                            height: 7,
                            decoration: new BoxDecoration(Color: color, Shape: BoxShape.Circle)),
                        new SizedBox(
                            width: 44,
                            child: new Text(
                                codeLabel,
                                fontSize: 14,
                                fontWeight: FontWeight.Bold,
                                color: color,
                                fontFamily: NetBenchFonts.Mono)),
                        new Expanded(
                            new Text(
                                strings.Mobile.StatusRequests(bucket.Count.ToString("N0")),
                                fontSize: 13,
                                color: palette.TextMid,
                                fontFamily: NetBenchFonts.Mono,
                                maxLines: 1)),
                        new Text(
                            $"{pct.ToString("F1")}{strings.Common.Percent}",
                            fontSize: 13,
                            color: palette.TextMid,
                            fontFamily: NetBenchFonts.Mono),
                    ]));
            yield return new Container(height: 1, color: palette.Border);
        }
    }

    private static Color StatusColor(NetBenchPalette palette, int statusCode) => statusCode switch
    {
        >= 200 and < 300 => palette.Success,
        >= 300 and < 400 => palette.Rps,
        >= 400 and < 500 => palette.Latency,
        _ => palette.Error,
    };

    private Material BuildShareButton(BuildContext context, Strings strings, NetBenchPalette palette)
    {
        var radius = BorderRadius.Circular(12);

        return new Material(
            color: palette.Rps,
            borderRadius: radius,
            child: new InkWell(
                onTap: () =>
                {
                    TextClipboard.SetText(BuildShareText(strings));
                    ScaffoldMessenger.Of(context).ShowSnackBar(
                        new SnackBar(content: new Text(strings.Mobile.ReportCopied)));
                },
                borderRadius: radius,
                child: new Container(
                    height: 54,
                    alignment: Alignment.Center,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 8,
                        children:
                        [
                            new IconGlyph(GlyphKind.Share, Colors.White, 14),
                            new Text(
                                strings.Mobile.ShareReport,
                                fontSize: 15,
                                fontWeight: FontWeight.Bold,
                                color: Colors.White,
                                fontFamily: NetBenchFonts.Ui),
                        ]))));
    }

    private string BuildShareText(Strings strings)
    {
        var summary = _report.Summary;
        return $"""
            {strings.App.Title} — {_report.Scenario.Name}
            {_report.Scenario.Target}
            {strings.Mobile.AvgRps}: {summary.RequestsPerSecond:N0}
            {strings.Mobile.TotalRequests}: {summary.TotalRequests:N0}
            {strings.Mobile.ErrorRate}: {summary.ErrorRate * 100:F1}%
            p50/p95/p99: {summary.LatencyP50Ms:F0}/{summary.LatencyP95Ms:F0}/{summary.LatencyP99Ms:F0} {strings.Common.MillisecondsShort}
            """;
    }
}
