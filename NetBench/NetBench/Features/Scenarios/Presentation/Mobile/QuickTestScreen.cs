using Avalonia.Media;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Presentation.Mobile;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Navigation;
using NetBench.Mobile.Theme;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.UI;
using Plumix.Widgets;
using Colors = Plumix.Material.Colors;

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
        var input = _controller.Text.Trim();
        var enabled = TargetUrl.TryNormalize(input, out _);
        // Ошибку показываем только для непустого ввода: пустое поле — не ошибка, а ещё не начатый ввод.
        var error = input.Length > 0 && !enabled ? strings.Mobile.InvalidUrl : null;

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
                            BuildUrlField(context, palette, strings.Scenarios.Editor.TargetPlaceholder, error),
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
    /// <remarks>
    /// Avalonia называет Backspace <c>Key.Back</c>, и Plumix-хост принимает Backspace, который
    /// никто не обработал, за системный «назад». А Plumix-поле обрабатывает только KeyDown,
    /// тогда как на Android повторные DEL от клавиатуры приходят как KeyRepeat (KeyUp первого
    /// нажатия теряется) — второе стирание подряд закрывало экран. Предок-Focus добивает
    /// такие Backspace сам и не отпускает их к хосту.
    /// </remarks>
    private Focus BuildUrlField(BuildContext context, NetBenchPalette palette, string placeholder, string? error)
    {
        var monoStyle = new TextStyle(
            FontFamily: NetBenchFonts.Mono,
            FontSize: 15,
            Color: palette.TextHi);

        return new Focus(
            canRequestFocus: false,
            skipTraversal: true,
            onKeyEvent: HandleLeakedBackspace,
            child: new TextField(
                controller: _controller,
                autofocus: true,
                keyboardType: TextInputType.Url,
                textInputAction: TextInputAction.Go,
                autocorrect: false,
                enableSuggestions: false,
                onSubmitted: _ => StartRun(context),
                style: monoStyle,
                decoration: new InputDecoration(
                    hintText: placeholder,
                    hintStyle: monoStyle with { Color = palette.TextFaint },
                    hintMaxLines: 1,
                    errorText: error,
                    errorStyle: new TextStyle(FontFamily: NetBenchFonts.Ui, FontSize: 12, Color: palette.Error),
                    filled: true,
                    fillColor: palette.BgCard,
                    isDense: true,
                    contentPadding: new Avalonia.Thickness(16, 14),
                    border: new OutlineInputBorder(
                        borderRadius: BorderRadius.Circular(10),
                        borderSide: new BorderSide(Colors.Transparent, style: BorderStyle.None)),
                    errorBorder: new OutlineInputBorder(
                        borderRadius: BorderRadius.Circular(10),
                        borderSide: new BorderSide(palette.Error)),
                    focusedErrorBorder: new OutlineInputBorder(
                        borderRadius: BorderRadius.Circular(10),
                        borderSide: new BorderSide(palette.Error, width: 1.5)))));
    }

    /// <summary>Сюда доходит только то, что поле не обработало само.</summary>
    private KeyEventResult HandleLeakedBackspace(FocusNode node, KeyEvent keyEvent)
    {
        if (keyEvent is KeyUpEvent || keyEvent.LogicalKey != LogicalKeyboardKey.Backspace)
            return KeyEventResult.Ignored;

        // В пустом поле DeleteBackward просто ничего не сделает — но и «назад» не случится.
        _controller.DeleteBackward();
        return KeyEventResult.Handled;
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
        // Кнопка с невалидным URL неактивна, но «Go» на клавиатуре приходит в обход неё.
        if (!TargetUrl.TryNormalize(_controller.Text, out var url))
            return;

        var scenario = new LoadScenario
        {
            Name = Translations<Strings>.ReadOf(context).Mobile.QuickTest,
            Target = url,
        };

        Navigator.Of(context).Push(ThemedPageRoute.Of(_ => new MonitorScreen(scenario)));
    }
}
