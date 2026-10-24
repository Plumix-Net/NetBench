using Avalonia.Media;
using NetBench.Features.Scenarios.Domain;
using NetBench.Localization;
using NetBench.Mobile.Controls;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Slang;
using Plumix.UI;
using Plumix.Widgets;
using Colors = Plumix.Material.Colors;

namespace NetBench.Features.Scenarios.Presentation.Mobile;

/// <summary>
/// Создание и правка сценария с телефона: имя, URL, метод, число пользователей и длительность.
/// Многошаговые сценарии (их собирают в desktop-версии) здесь меняются целиком, кроме шагов:
/// метод доступен, только пока шаг один.
/// </summary>
public sealed class ScenarioEditorScreen : StatefulWidget
{
    private static readonly IReadOnlyList<string> _methods = ["GET", "POST", "PUT", "DELETE"];

    /// <param name="scenario">Сценарий для правки; <c>null</c> — новый.</param>
    public ScenarioEditorScreen(LoadScenario? scenario = null)
    {
        Scenario = scenario;
    }

    public LoadScenario? Scenario { get; }

    internal static IReadOnlyList<string> Methods => _methods;

    /// <summary>Новый сценарий с адресом, нагрузкой и запросами быстрого теста; имя пользователь даст сам.</summary>
    public static ScenarioEditorScreen FromQuickTest(LoadScenario quickTest)
    {
        var scenario = new LoadScenario
        {
            Target = quickTest.Target,
            Load = new LoadConfig
            {
                ConcurrentUsers = quickTest.Load.ConcurrentUsers,
                Duration = quickTest.Load.Duration,
            },
        };
        scenario.Requests.AddRange(quickTest.Requests.Select(CopyStep));

        return new ScenarioEditorScreen(scenario) { IsNew = true };
    }

    /// <summary>Сценарий ещё не в репозитории (заголовок «Новый сценарий»).</summary>
    public bool IsNew { get; private init; }

    internal static RequestStep CopyStep(RequestStep step)
    {
        var copy = new RequestStep { Method = step.Method, Path = step.Path, Body = step.Body };
        foreach (var (name, value) in step.Headers)
            copy.Headers[name] = value;
        return copy;
    }

    public override State CreateState() => new ScenarioEditorScreenState();
}

internal sealed class ScenarioEditorScreenState : State<ScenarioEditorScreen>, IDisposable
{
    private readonly TextEditingController _name = new();
    private readonly TextEditingController _target = new();
    private string _method = "GET";
    private int _users;
    private TimeSpan _duration;
    private bool _showErrors;
    private bool _saving;

    private bool IsNew => Widget.Scenario is null || Widget.IsNew;

    private int StepCount => Widget.Scenario?.Requests.Count ?? 0;

    public override void InitState()
    {
        base.InitState();

        var source = Widget.Scenario;
        var load = MobileLoadLimits.Clamp(source?.Load ?? new LoadConfig());
        _name.Text = source?.Name ?? string.Empty;
        _target.Text = source?.Target ?? string.Empty;
        _method = source?.Requests.FirstOrDefault()?.Method ?? "GET";
        _users = load.ConcurrentUsers;
        _duration = load.Duration;
        _target.AddListener(OnTargetChanged);
    }

    public override void Dispose()
    {
        _target.RemoveListener(OnTargetChanged);
        _name.Dispose();
        _target.Dispose();
        base.Dispose();
    }

    // Ошибка URL обновляется на лету только после первой попытки сохранить
    private void OnTargetChanged()
    {
        if (_showErrors)
            SetState(() => { });
    }

    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var strings = Translations<Strings>.Of(context);
        var targetValid = TargetUrl.TryNormalize(_target.Text, out _);
        var targetError = _showErrors && !targetValid
            ? string.IsNullOrWhiteSpace(_target.Text) ? strings.Shell.TargetRequired : strings.Mobile.InvalidUrl
            : null;

        List<Widget> form =
        [
            new SizedBox(height: 20),
            BuildLabel(palette, strings.Scenarios.Editor.Name),
            BuildField(palette, _name, strings.Scenarios.Editor.NamePlaceholder, error: null, mono: false),
            new SizedBox(height: 20),
            BuildLabel(palette, strings.Mobile.TargetUrl),
            BuildField(palette, _target, strings.Scenarios.Editor.TargetPlaceholder, targetError, mono: true),
            new SizedBox(height: 20),
            BuildLabel(palette, strings.Mobile.Method),
        ];

        if (StepCount > 1)
        {
            form.Add(new Text(
                strings.Mobile.MultiStepHint(StepCount.ToString(DisplayCulture.Current)),
                style: new TextStyle(FontSize: 13, Color: palette.TextMid, FontFamily: NetBenchFonts.Ui)));
        }
        else
        {
            form.Add(new PresetPicker<string>(
                ScenarioEditorScreen.Methods,
                _method,
                static method => method,
                method => SetState(() => _method = method)));
        }

        form.AddRange(
        [
            new SizedBox(height: 20),
            BuildLabel(palette, strings.Mobile.Users),
            new PresetPicker<int>(
                MobileLoadLimits.UserPresets,
                _users,
                static users => users.ToString(DisplayCulture.Current),
                users => SetState(() => _users = users)),
            new SizedBox(height: 20),
            BuildLabel(palette, strings.Mobile.Duration),
            new PresetPicker<TimeSpan>(
                MobileLoadLimits.DurationPresets,
                _duration,
                strings.DurationLabel,
                duration => SetState(() => _duration = duration)),
            new SizedBox(height: 24),
        ]);

        return new Scaffold(
            body: new SafeArea(
                new Padding(
                    new Avalonia.Thickness(20, 8, 20, 0),
                    new Column(
                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                        children:
                        [
                            BuildTopBar(
                                context,
                                palette,
                                IsNew ? strings.Mobile.NewScenarioTitle : strings.Mobile.EditScenarioTitle),
                            new Expanded(
                                new SingleChildScrollView(
                                    new Column(
                                        crossAxisAlignment: CrossAxisAlignment.Stretch,
                                        children: form))),
                            BuildSaveButton(context, palette, strings.Common.Save),
                            new SizedBox(height: 24),
                        ]))));
    }

    private static Row BuildTopBar(BuildContext context, NetBenchPalette palette, string title) =>
        new(
            spacing: 12,
            children:
            [
                new ToolButton(
                    icon: new Icon(Icons.ArrowBackIosNewRounded, size: 16, color: palette.Text),
                    onPressed: () => Navigator.Pop(context),
                    tooltip: Translations<Strings>.Of(context).Mobile.Back,
                    size: 34,
                    radius: 8),
                new Expanded(
                    new Text(
                        title,
                        style: new TextStyle(
                            FontSize: 20,
                            FontWeight: FontWeight.ExtraBold,
                            Color: palette.TextHi,
                            FontFamily: NetBenchFonts.Ui),
                        maxLines: 1,
                        overflow: TextOverflow.Ellipsis)),
            ]);

    private static Padding BuildLabel(NetBenchPalette palette, string text) =>
        new(
            new Avalonia.Thickness(0, 0, 0, 8),
            new Text(
                text,
                style: new TextStyle(
                    FontSize: 12,
                    FontWeight: FontWeight.SemiBold,
                    Color: palette.TextMid,
                    LetterSpacing: 0.5,
                    FontFamily: NetBenchFonts.Ui)));

    private static TextField BuildField(
        NetBenchPalette palette,
        TextEditingController controller,
        string placeholder,
        string? error,
        bool mono)
    {
        var style = new TextStyle(
            FontFamily: mono ? NetBenchFonts.Mono : NetBenchFonts.Ui,
            FontSize: 15,
            Color: palette.TextHi);

        return new TextField(
            controller: controller,
            keyboardType: mono ? TextInputType.Url : TextInputType.Text,
            textInputAction: TextInputActionType.Next,
            autocorrect: !mono,
            enableSuggestions: !mono,
            style: style,
            decoration: new InputDecoration(
                hintText: placeholder,
                hintStyle: style.CopyWith(color: palette.TextFaint),
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
                    borderSide: new BorderSide(palette.Error, width: 1.5))));
    }

    private Material BuildSaveButton(BuildContext context, NetBenchPalette palette, string label)
    {
        var radius = BorderRadius.Circular(16);

        return new Material(
            color: palette.Rps,
            borderRadius: radius,
            child: new InkWell(
                onTap: _saving ? null : () => _ = SaveAsync(context),
                borderRadius: radius,
                child: new Container(
                    height: 56,
                    alignment: Alignment.Center,
                    child: new Text(
                        label,
                        style: new TextStyle(
                            FontSize: 16,
                            FontWeight: FontWeight.ExtraBold,
                            Color: Colors.White,
                            FontFamily: NetBenchFonts.Ui)))));
    }

    private async Task SaveAsync(BuildContext context)
    {
        if (!TargetUrl.TryNormalize(_target.Text, out var url))
        {
            SetState(() => _showErrors = true);
            return;
        }

        var strings = Translations<Strings>.ReadOf(context);
        var cubit = context.Read<ScenarioListCubit>();
        var navigator = Navigator.Of(context);
        var messenger = ScaffoldMessenger.Of(context);
        var scenario = BuildScenario(url);

        SetState(() => _saving = true);
        var saved = await cubit.SaveScenarioAsync(scenario);
        if (!Mounted)
            return;

        if (!saved)
        {
            SetState(() => _saving = false);
            messenger.ShowSnackBar(new SnackBar(content: new Text(strings.Mobile.ScenarioSaveFailed)));
            return;
        }

        // С экрана результата быстрого теста — сразу домой, к новой карточке.
        navigator.PopUntil(static route => route.IsFirst);
        messenger.ShowSnackBar(new SnackBar(
            content: new Text(strings.Mobile.ScenarioSaved(strings.DisplayNameOf(scenario)))));
    }

    /// <summary>
    /// Новый объект, а не правка исходного: исходный сценарий лежит в состоянии кубита,
    /// и до успешной записи список должен показывать старую версию.
    /// </summary>
    private LoadScenario BuildScenario(string url)
    {
        var source = Widget.Scenario;
        var load = source?.Load ?? new LoadConfig();
        var scenario = new LoadScenario
        {
            Id = source?.Id ?? Guid.NewGuid(),
            Name = _name.Text.Trim(),
            Target = url,
            Load = new LoadConfig
            {
                ConcurrentUsers = _users,
                Duration = _duration,
                RampUp = load.RampUp > _duration ? _duration : load.RampUp,
                ThinkTime = load.ThinkTime,
            },
        };

        var steps = source?.Requests.Select(ScenarioEditorScreen.CopyStep).ToList() ?? [];
        if (steps.Count == 0)
            steps.Add(new RequestStep { Method = _method, Path = "/" });
        else if (steps.Count == 1)
            steps[0].Method = _method;
        scenario.Requests.AddRange(steps);

        return scenario;
    }
}
