namespace SimpleSqlServerMcp.WindowsInstaller;

public static class ContinueConfigPathResolver
{
    public static string ResolveDefault()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            homeDirectory = Environment.GetEnvironmentVariable("USERPROFILE");
        }

        if (string.IsNullOrWhiteSpace(homeDirectory))
        {
            throw new InvalidOperationException("Unable to resolve the current Windows user profile directory.");
        }

        return Path.Combine(homeDirectory, ".continue", "config.yaml");
    }
}
