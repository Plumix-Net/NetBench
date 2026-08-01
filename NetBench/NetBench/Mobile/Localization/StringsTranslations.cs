using System.ComponentModel;
using Plumix.Slang;

namespace NetBench.Localization;

/// <summary>
/// Мост между сгенерированным Slang.Net классом и Plumix.Slang: даёт
/// <see cref="TranslationProvider{T}"/> точку подписки на смену культуры.
/// Только для мобильных таргетов — desktop берёт строки через биндинги.
/// Остальные члены контракта (BaseCulture/SupportedCultures/SetCulture) генерирует Slang.Net.
/// </summary>
public partial class Strings : ITranslations<Strings>
{
    static Strings ITranslations<Strings>.CurrentTranslations => Instance.Root;

    static INotifyPropertyChanged ITranslations<Strings>.ChangeSource => Instance;
}
