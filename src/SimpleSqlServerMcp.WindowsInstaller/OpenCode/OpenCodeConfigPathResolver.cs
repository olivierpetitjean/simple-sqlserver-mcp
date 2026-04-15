namespace SimpleSqlServerMcp.WindowsInstaller;

public static class OpenCodeConfigPathResolver
{
    public static string ResolveDefault()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDirectory = Path.Combine(userProfile, ".config", "opencode");
        var jsoncPath = Path.Combine(configDirectory, "opencode.jsonc");
        var jsonPath = Path.Combine(configDirectory, "opencode.json");

        if (File.Exists(jsoncPath))
        {
            return jsoncPath;
        }

        if (File.Exists(jsonPath))
        {
            return jsonPath;
        }

        return jsoncPath;
    }
}
