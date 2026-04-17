namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class WindowsToolConfigurationApplier
{
    private readonly WindowsInstallerSqlPasswordPreparation _sqlPasswordPreparation;

    public WindowsToolConfigurationApplier(WindowsInstallerSqlPasswordPreparation? sqlPasswordPreparation = null)
    {
        _sqlPasswordPreparation = sqlPasswordPreparation ?? new WindowsInstallerSqlPasswordPreparation();
    }

    public IReadOnlyList<string> Apply(InstallerOptions options)
    {
        options = _sqlPasswordPreparation.Prepare(options);

        var configuredPaths = new List<string>();

        if (options.InstallCodex)
        {
            var configPath = options.ConfigPath ?? CodexConfigPathResolver.ResolveDefault();
            var registration = CodexServerRegistration.Create(options);
            var content = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            var merged = new CodexRegistrationWriter().Merge(content, registration);

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, merged);
            configuredPaths.Add(configPath);
        }

        if (options.InstallCursor)
        {
            var configPath = CursorConfigPathResolver.ResolveDefault();
            var registration = CursorServerRegistration.Create(options);
            var content = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            var merged = new CursorRegistrationWriter().Merge(content, registration);

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, merged);
            configuredPaths.Add(configPath);
        }

        if (options.InstallGeminiCli)
        {
            var configPath = GeminiCliConfigPathResolver.ResolveDefault();
            var registration = GeminiCliServerRegistration.Create(options);
            var content = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            var merged = new GeminiCliRegistrationWriter().Merge(content, registration);

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, merged);
            configuredPaths.Add(configPath);
        }

        if (options.InstallGitHubCopilotCli)
        {
            var configPath = CopilotCliConfigPathResolver.ResolveDefault();
            var registration = CopilotCliServerRegistration.Create(options);
            var content = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            var merged = new CopilotCliRegistrationWriter().Merge(content, registration);

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, merged);
            configuredPaths.Add(configPath);
        }

        if (options.InstallContinue)
        {
            var configPath = ContinueConfigPathResolver.ResolveDefault();
            var registration = ContinueServerRegistration.Create(options);
            var content = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            var merged = new ContinueRegistrationWriter().Merge(content, registration);

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, merged);
            configuredPaths.Add(configPath);
        }

        if (options.InstallOpenCode)
        {
            var configPath = OpenCodeConfigPathResolver.ResolveDefault();
            var registration = OpenCodeServerRegistration.Create(options);
            var content = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            var merged = new OpenCodeRegistrationWriter().Merge(content, registration);

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, merged);
            configuredPaths.Add(configPath);
        }

        return configuredPaths;
    }
}
