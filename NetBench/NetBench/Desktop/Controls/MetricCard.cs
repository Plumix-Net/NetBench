using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace NetBench.Desktop.Controls;

/// <summary>Семантический тон метрики — цвет фиксирован дизайн-системой.</summary>
public enum MetricTone
{
    Neutral,
    Rps,
    Latency,
    Error,
    Success,
}

/// <summary>Карточка метрики: подпись, крупное моноширинное значение, опциональные юнит и точка-индикатор.</summary>
public class MetricCard : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MetricCard, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<MetricCard, string?>(nameof(Value));

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<MetricCard, string?>(nameof(Unit));

    public static readonly StyledProperty<MetricTone> ToneProperty =
        AvaloniaProperty.Register<MetricCard, MetricTone>(nameof(Tone));

    public static readonly StyledProperty<bool> ShowDotProperty =
        AvaloniaProperty.Register<MetricCard, bool>(nameof(ShowDot));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public MetricTone Tone
    {
        get => GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    public bool ShowDot
    {
        get => GetValue(ShowDotProperty);
        set => SetValue(ShowDotProperty, value);
    }

    public MetricCard() => UpdatePseudoClasses();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ToneProperty || change.Property == UnitProperty)
            UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":rps", Tone == MetricTone.Rps);
        PseudoClasses.Set(":latency", Tone == MetricTone.Latency);
        PseudoClasses.Set(":error", Tone == MetricTone.Error);
        PseudoClasses.Set(":success", Tone == MetricTone.Success);
        PseudoClasses.Set(":neutral", Tone == MetricTone.Neutral);
        PseudoClasses.Set(":unit", !string.IsNullOrEmpty(Unit));
    }
}
