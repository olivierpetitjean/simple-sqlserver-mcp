namespace SimpleSqlServerMcp.WindowsInstaller;

public static class CursorConfigPathResolver
{
    public static string ResolveDefault()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cursor",
            "mcp.json");
}
