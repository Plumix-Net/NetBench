namespace NetBench.Desktop.Services;

/// <summary>Сохранение текстового содержимого через системный диалог «Сохранить как».</summary>
public interface IFileSaveService
{
    Task SaveTextAsync(string suggestedFileName, string content, CancellationToken ct = default);
}
