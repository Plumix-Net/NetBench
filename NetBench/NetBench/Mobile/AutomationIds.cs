namespace NetBench.Mobile;

/// <summary>
/// Стабильные идентификаторы узлов семантики (<c>Semantics(identifier:)</c>) для e2e-тестов в
/// <c>e2e/</c>: на Android они становятся resource-id, на iOS — accessibilityIdentifier.
/// Нужны там, где подписи не хватает: у текстового поля Android отдаёт наружу его значение, а не label.
/// Переименование ломает флоу Maestro — меняй вместе с ними.
/// </summary>
public static class AutomationIds
{
    public const string QuickTestUrl = "quick_test_url";

    public const string ScenarioName = "scenario_name";

    public const string ScenarioUrl = "scenario_url";
}
