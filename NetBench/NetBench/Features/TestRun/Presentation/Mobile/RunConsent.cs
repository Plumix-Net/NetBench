using NetBench.Features.Settings.Domain;
using NetBench.Localization;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Slang;
using Plumix.Widgets;
using AlertDialog = Plumix.Material.AlertDialog;

namespace NetBench.Features.TestRun.Presentation.Mobile;

/// <summary>
/// Перед первым прогоном — предупреждение «тестируйте только свои серверы». Нагрузочный
/// инструмент легко принять за средство DoS; подтверждение сохраняется в настройках,
/// и дальше прогоны стартуют без вопросов.
/// </summary>
public static class RunConsent
{
    /// <returns><c>true</c>, если прогон можно начинать.</returns>
    public static async Task<bool> EnsureAsync(BuildContext context)
    {
        var store = RepositoryProvider<IAppSettingsStore>.ReadOf(context);
        if (store.Current.OwnTargetsConfirmed)
            return true;

        var strings = Translations<Strings>.ReadOf(context);
        var confirmed = await MaterialDialogs.ShowDialog<bool>(
            context,
            dialogContext => new AlertDialog(
                title: new Text(strings.Mobile.OwnTargetsTitle),
                content: new Text(strings.Mobile.OwnTargetsMessage),
                actions:
                [
                    new TextButton(
                        child: new Text(strings.Common.Cancel),
                        onPressed: () => Navigator.Pop(dialogContext, false)),
                    new TextButton(
                        child: new Text(strings.Mobile.OwnTargetsConfirm),
                        onPressed: () => Navigator.Pop(dialogContext, true)),
                ]));

        if (confirmed != true)
            return false;

        store.Save(store.Current with { OwnTargetsConfirmed = true });
        return true;
    }
}
