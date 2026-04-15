namespace SimpleSqlServerMcp.WindowsInstaller;

public static class CodexConfigPathResolver
{
    public static string ResolveDefault()
    {
        var codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(codexHome))
        {
            return Path.Combine(codexHome, "config.toml");
        }

        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            homeDirectory = Environment.GetEnvironmentVariable("USERPROFILE");
        }

        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            throw new InvalidOperationException("Unable to resolve the current Windows user profile directory.");
        }

        return ResolveDefault(homeDirectory);
    }

    public static string ResolveDefault(string homeDirectory)
        => Path.Combine(homeDirectory, ".codex", "config.toml");
}
