using Avalonia;
using Avalonia.Controls.Primitives;

namespace NetBench.Desktop.Controls;

/// <summary>Строка распределения задержек: подпись, полоса-процент, значение справа.</summary>
public class LatencyBar : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<LatencyBar, string?>(nameof(Label));

    /// <summary>Заполнение полосы, 0–100.</summary>
    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<LatencyBar, double>(nameof(Percentage));

    public static readonly StyledProperty<string?> DisplayProperty =
        AvaloniaProperty.Register<LatencyBar, string?>(nameof(Display));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Percentage
    {
        get => GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public string? Display
    {
        get => GetValue(DisplayProperty);
        set => SetValue(DisplayProperty, value);
    }
}
