namespace SimpleSqlServerMcp.WindowsInstaller;

public static class GeminiCliConfigPathResolver
{
    public static string ResolveDefault()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gemini",
            "settings.json");
}
