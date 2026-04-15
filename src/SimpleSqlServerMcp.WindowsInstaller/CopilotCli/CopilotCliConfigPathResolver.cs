namespace SimpleSqlServerMcp.WindowsInstaller;

public static class CopilotCliConfigPathResolver
{
    public static string ResolveDefault()
    {
        var copilotHome = Environment.GetEnvironmentVariable("COPILOT_HOME");
        if (!string.IsNullOrWhiteSpace(copilotHome))
        {
            return Path.Combine(copilotHome, "mcp-config.json");
        }

        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, "copilot", "mcp-config.json");
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

        return Path.Combine(homeDirectory, ".copilot", "mcp-config.json");
    }
}
