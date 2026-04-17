using SimpleSqlServerMcp.WindowsInstaller;

Environment.ExitCode = new InstallerCommandRunner().Run(args, Console.Out, Console.Error);

namespace SimpleSqlServerMcp.WindowsInstaller
{
    public sealed class InstallerCommandRunner
    {
        private readonly Func<IReadOnlyList<string>, InstallerOptions> parseOptions;
        private readonly Func<InstallerOptions> loadInstallerSession;
        private readonly Func<InstallerOptions, IReadOnlyList<string>> applyToolConfiguration;

        public InstallerCommandRunner(
            Func<IReadOnlyList<string>, InstallerOptions>? parseOptions = null,
            Func<InstallerOptions>? loadInstallerSession = null,
            Func<InstallerOptions, IReadOnlyList<string>>? applyToolConfiguration = null)
        {
            this.parseOptions = parseOptions ?? (arguments => InstallerOptions.Parse(arguments));
            this.loadInstallerSession = loadInstallerSession ?? InstallerSessionRegistry.LoadCurrentUserSession;
            this.applyToolConfiguration = applyToolConfiguration ?? (options => new WindowsToolConfigurationApplier().Apply(options));
        }

        public int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
        {
            if (args.Count == 0 || string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase) || args[0] is "--help" or "-h")
            {
                PrintHelp(output);
                return 0;
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "preview-codex":
                    {
                        var options = parseOptions(args.Skip(1).ToArray());
                        var configPath = options.ConfigPath ?? CodexConfigPathResolver.ResolveDefault();
                        var registration = CodexServerRegistration.Create(options);

                        output.WriteLine($"Target config: {configPath}");
                        output.WriteLine();
                        output.WriteLine(registration.ToTomlBlock(Environment.NewLine));
                        return 0;
                    }

                    case "configure-codex":
                    {
                        var options = parseOptions(args.Skip(1).ToArray());
                        var configPath = options.ConfigPath ?? CodexConfigPathResolver.ResolveDefault();
                        var registration = CodexServerRegistration.Create(options);
                        var existingContent = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
                        var mergedContent = new CodexRegistrationWriter().Merge(existingContent, registration);

                        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                        File.WriteAllText(configPath, mergedContent);

                        output.WriteLine($"Configured Codex MCP server `{options.ServerName}` at `{configPath}`.");
                        return 0;
                    }

                    case "apply-config":
                    {
                        var options = parseOptions(args.Skip(1).ToArray());
                        var configuredPaths = applyToolConfiguration(options);

                        if (configuredPaths.Count == 0)
                        {
                            output.WriteLine("No client tool was selected, so no config files were written.");
                            return 0;
                        }

                        output.WriteLine($"Configured `{options.ServerName}` for:");
                        foreach (var path in configuredPaths)
                        {
                            output.WriteLine($"- {path}");
                        }

                        return 0;
                    }

                    case "apply-config-from-registry":
                    {
                        var options = loadInstallerSession();
                        var configuredPaths = applyToolConfiguration(options);

                        if (configuredPaths.Count == 0)
                        {
                            output.WriteLine("No client tool was selected, so no config files were written.");
                            return 0;
                        }

                        output.WriteLine("Configured MCP registrations from the Windows installer session.");
                        foreach (var path in configuredPaths)
                        {
                            output.WriteLine($"- {path}");
                        }

                        return 0;
                    }

                    default:
                        throw new ArgumentException($"Unknown command `{args[0]}`.");
                }
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                error.WriteLine(exception.Message);
                PrintHelp(output);
                return 1;
            }
        }

        private static void PrintHelp(TextWriter output)
        {
            output.WriteLine("""
SimpleSqlServerMcp.WindowsInstaller

Commands:
  preview-codex
  configure-codex
  apply-config
  apply-config-from-registry

Required options:
  --command-path <path>
  --sql-host <host>
  --sql-database <database>

Optional options:
  --server-name <name>               Default: simple-sqlserver-mcp
  --working-directory <path>
  --config-path <path>              Default: %USERPROFILE%\.codex\config.toml
  --sql-port <port>                 Default: 1433
  --integrated-security <bool>      Default: false
  --encrypt <bool>                  Default: true
  --trust-server-certificate <bool> Default: false
  --sql-username <name>             Required when integrated security is false
  --sql-password <password>         Required when storing a new password or using inline password mode
  --sql-password-secret-name <name> Optional existing Windows Credential Manager entry name
  --store-password-in-windows-credential-manager <bool>
                                     Default: false
  --mode <read-only|mutable>        Default: read-only
  --tool-codex <bool>               Default: false
  --tool-cursor <bool>              Default: false
  --tool-gemini-cli <bool>          Default: false
  --tool-github-copilot-cli <bool>  Default: false
  --tool-continue <bool>            Default: false
  --tool-opencode <bool>            Default: false
""");
        }
    }
}
