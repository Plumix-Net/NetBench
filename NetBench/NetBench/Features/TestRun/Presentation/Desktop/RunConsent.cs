using NetBench.Desktop.Services;
using NetBench.Features.Settings.Domain;
using NetBench.Localization;

namespace NetBench.Features.TestRun.Presentation.Desktop;

/// <summary>
/// Перед первым прогоном — предупреждение «тестируйте только свои серверы», как на мобиле.
/// Подтверждение сохраняется в настройках, и дальше прогоны стартуют без вопросов.
/// </summary>
public sealed class RunConsent(IAppSettingsStore settings, IConfirmationDialog dialog)
{
    /// <returns><c>true</c>, если прогон можно начинать.</returns>
    public async Task<bool> EnsureAsync()
    {
        if (settings.Current.OwnTargetsConfirmed)
            return true;

        var strings = Strings.Instance.Root;
        var confirmed = await dialog.ConfirmAsync(
            strings.TestRun.OwnTargetsTitle,
            strings.TestRun.OwnTargetsMessage,
            strings.TestRun.OwnTargetsConfirm,
            strings.Common.Cancel);
        if (!confirmed)
            return false;

        settings.Save(settings.Current with { OwnTargetsConfirmed = true });
        return true;
    }
}
