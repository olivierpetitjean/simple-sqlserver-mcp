namespace SimpleSqlServerMcp.WindowsInstaller.CustomActions;

internal static class ToolDetectionCatalog
{
    public static IReadOnlyList<DetectedToolProperty> All { get; } = new List<DetectedToolProperty>
    {
        new DetectedToolProperty(
            "CODEX",
            "INSTALL_TOOL_CODEX",
            new[] { "Codex", "OpenAI Codex" },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "codex.exe")
            },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "config.toml")
            },
            new[] { "codex" },
            Array.Empty<string>(),
            Array.Empty<string>()),

        new DetectedToolProperty(
            "CURSOR",
            "INSTALL_TOOL_CURSOR",
            new[] { "Cursor" },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Cursor", "Cursor.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cursor", "Cursor.exe")
            },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cursor", "mcp.json")
            },
            new[] { "cursor", "cursor-agent" },
            Array.Empty<string>(),
            Array.Empty<string>()),

        new DetectedToolProperty(
            "GEMINI_CLI",
            "INSTALL_TOOL_GEMINI_CLI",
            new[] { "Gemini CLI", "Gemini" },
            Array.Empty<string>(),
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "settings.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "gemini-cli", "settings.json")
            },
            new[] { "gemini" },
            Array.Empty<string>(),
            Array.Empty<string>()),

        new DetectedToolProperty(
            "GITHUB_COPILOT_CLI",
            "INSTALL_TOOL_GITHUB_COPILOT_CLI",
            new[] { "GitHub CLI", "GitHub Copilot" },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GitHub CLI", "gh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "GitHub CLI", "gh.exe")
            },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".copilot", "mcp-config.json")
            },
            new[] { "gh" },
            Array.Empty<string>(),
            Array.Empty<string>()),

        new DetectedToolProperty(
            "CONTINUE",
            "INSTALL_TOOL_CONTINUE",
            new[] { "Continue" },
            Array.Empty<string>(),
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".continue", "config.yaml")
            },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>()),

        new DetectedToolProperty(
            "OPENCODE",
            "INSTALL_TOOL_OPENCODE",
            new[] { "OpenCode", "Open Code" },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenCode", "OpenCode.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "OpenCode", "OpenCode.exe")
            },
            new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "opencode", "opencode.jsonc"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "opencode", "opencode.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "opencode")
            },
            new[] { "opencode" },
            Array.Empty<string>(),
            Array.Empty<string>()),
    };
}
