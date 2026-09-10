using System.Reflection;

namespace CrystariumBoutique;

internal static class BoutiqueVersion
{
    public static string SemanticVersion { get; } = ResolveSemanticVersion();

    public static string DisplayLabel => $"v{SemanticVersion}";

    private static string ResolveSemanticVersion()
    {
        var informationalVersion = typeof(BoutiqueVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return typeof(BoutiqueVersion).Assembly.GetName().Version?.ToString()
                ?? "unknown";
        }

        var metadataSeparator = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return metadataSeparator >= 0
            ? informationalVersion[..metadataSeparator]
            : informationalVersion;
    }
}
