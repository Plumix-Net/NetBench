using System.Reflection;

namespace NetBench.Mobile;

/// <summary>
/// Версия и build number приложения — из атрибутов сборки, которые проставляет
/// Directory.Build.props (NetBenchVersion/NetBenchBuildNumber), как и версии в сторах.
/// </summary>
public static class AppInfo
{
    private static readonly Assembly _assembly = typeof(AppInfo).Assembly;

    public static string Version { get; } =
        _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? _assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";

    public static string BuildNumber { get; } =
        _assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "BuildNumber")?.Value
        ?? "0";

    public static Uri PrivacyPolicy { get; } = new("https://plumix.net/netbench/privacy");

    public static Uri Terms { get; } = new("https://plumix.net/netbench/terms");

    public static Uri SourceCode { get; } = new("https://github.com/Plumix-Net/NetBench");

    public static Uri Issues { get; } = new("https://github.com/Plumix-Net/NetBench/issues");

    public static Uri Plumix { get; } = new("https://plumix.net");
}
