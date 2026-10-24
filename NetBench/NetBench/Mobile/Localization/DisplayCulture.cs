using System.Globalization;
using Plumix.Slang;

namespace NetBench.Localization;

public static class DisplayCulture
{
    /// <summary>
    /// Культура, на которой сейчас показаны строки, — для дат в подписях. CurrentUICulture
    /// не годится: на устройстве с неподдерживаемым языком (hr-HR) строки уходят в базовую
    /// культуру, а дата рисовалась бы по-хорватски посреди русского текста.
    /// </summary>
    public static CultureInfo Current => LocaleSettings<Strings>.Resolve(CultureInfo.CurrentUICulture);
}
