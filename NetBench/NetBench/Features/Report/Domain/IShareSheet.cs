namespace NetBench.Features.Report.Domain;

/// <summary>
/// Системное меню «Поделиться» (Android Intent.ACTION_SEND, iOS UIActivityViewController).
/// Реализуют платформенные головы; общий код знает только этот контракт.
/// </summary>
public interface IShareSheet
{
    Task ShareTextAsync(string text, string title);

    /// <param name="path">Файл во временном каталоге приложения.</param>
    /// <param name="mimeType">Тип содержимого — по нему система подбирает приложения-получатели.</param>
    Task ShareFileAsync(string path, string mimeType, string title);
}
