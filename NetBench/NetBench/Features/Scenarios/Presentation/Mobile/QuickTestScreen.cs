using Avalonia.Media;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Presentation.Mobile;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Theme;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.Widgets;

namespace NetBench.Features.Scenarios.Presentation.Mobile;

/// <summary>Экран быстрого теста: один URL и большая кнопка запуска.</summary>
public sealed class QuickTestScreen : StatefulWidget
{
    public override State CreateState() => new QuickTestScreenState();
}

internal sealed class QuickTestScreenState : State, IDisposable
{
    private readonly TextEditingController _controller = new();

    public override void InitState()
    {
        base.InitState();
        _controller.AddListener(OnUrlChanged);
    }

    public override void Dispose()
    {
        _controller.RemoveListener(OnUrlChanged);
        _controller.Dispose();
        base.Dispose();
    }

    private void OnUrlChanged() => SetState(() => { });

    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);
        var url = _controller.Text.Trim();
        var enabled = url.Length > 0;

        return new Scaffold(
            body: new SafeArea(
                new Padding(
                    new Avalonia.Thickness(20, 8, 20, 0),
                    new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            BuildTopBar(context, palette, strings.Mobile.QuickTest),
                            new SizedBox(height: 28),
                            new Text(
                                strings.Mobile.TargetUrl,
                                fontSize: 12,
                                fontWeight: FontWeight.SemiBold,
                                color: palette.TextMid,
                                letterSpacing: 0.5,
                                fontFamily: NetBenchFonts.Ui),
                            new SizedBox(height: 8),
                            BuildUrlField(palette, strings.Scenarios.Editor.TargetPlaceholder),
                            new Spacer(),
                            BuildRunButton(context, palette, strings.Mobile.Run, enabled),
                            new SizedBox(height: 40),
                        ]))));
    }

    private static Row BuildTopBar(BuildContext context, NetBenchPalette palette, string title)
    {
        return new Row(
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
            ]);
    }

    /// <summary>
    /// Плейсхолдер отдан InputDecoration.hintText — ручной Stack с текстом больше не нужен.
    /// </summary>
    private TextField BuildUrlField(NetBenchPalette palette, string placeholder)
    {
        var monoStyle = new TextStyle(
            FontFamily: NetBenchFonts.Mono,
            FontSize: 15,
            Color: palette.TextHi);

        return new TextField(
            controller: _controller,
            autofocus: true,
            style: monoStyle,
            decoration: new InputDecoration(
                hintText: placeholder,
                hintStyle: monoStyle with { Color = palette.TextFaint },
                hintMaxLines: 1,
                filled: true,
                fillColor: palette.BgCard,
                isDense: true,
                contentPadding: new Avalonia.Thickness(16, 14),
                border: new OutlineInputBorder(
                    borderRadius: BorderRadius.Circular(10),
                    borderSide: new BorderSide(Colors.Transparent, style: BorderStyle.None))));
    }

    private Material BuildRunButton(BuildContext context, NetBenchPalette palette, string label, bool enabled)
    {
        var radius = BorderRadius.Circular(16);
        var foreground = enabled ? Colors.White : palette.TextFaint;

        // Material с animateColor: цвет фона перетекает, когда поле URL перестаёт быть пустым.
        return new Material(
            color: enabled ? palette.Rps : palette.BorderStrong,
            borderRadius: radius,
            animateColor: true,
            animationDuration: TimeSpan.FromMilliseconds(180),
            child: new InkWell(
                onTap: enabled ? () => StartRun(context) : null,
                borderRadius: radius,
                child: new Container(
                    height: 64,
                    alignment: Alignment.Center,
                    child: new Row(
                        mainAxisSize: MainAxisSize.Min,
                        spacing: 10,
                        children:
                        [
                            new IconGlyph(GlyphKind.Play, foreground, 14),
                            new Text(
                                label,
                                fontSize: 17,
                                fontWeight: FontWeight.ExtraBold,
                                color: foreground,
                                fontFamily: NetBenchFonts.Ui),
                        ]))));
    }

    private void StartRun(BuildContext context)
    {
        var url = _controller.Text.Trim();
        if (url.Length == 0)
            return;

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        var scenario = new LoadScenario
        {
            Name = Translations<Strings>.ReadOf(context).Mobile.QuickTest,
            Target = url,
        };

        Navigator.Of(context).Push(new BuilderPageRoute(_ => new MonitorScreen(scenario)));
    }
}
