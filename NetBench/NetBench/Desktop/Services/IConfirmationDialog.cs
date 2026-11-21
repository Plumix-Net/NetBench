namespace NetBench.Desktop.Services;

/// <summary>Модальный вопрос «подтвердить / отмена» поверх главного окна.</summary>
public interface IConfirmationDialog
{
    /// <returns><c>true</c>, если пользователь нажал кнопку подтверждения.</returns>
    Task<bool> ConfirmAsync(string title, string message, string confirm, string cancel);
}
