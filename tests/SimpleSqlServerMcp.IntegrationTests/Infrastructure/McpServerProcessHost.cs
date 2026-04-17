using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace SimpleSqlServerMcp.IntegrationTests.Infrastructure;

public sealed class McpServerProcessHost : IAsyncDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpClient _client;
    private readonly List<string> _stderrLines = [];

    private McpServerProcessHost(
        ILoggerFactory loggerFactory,
        McpClient client)
    {
        _loggerFactory = loggerFactory;
        _client = client;
    }

    public static async Task<McpServerProcessHost> StartAsync(
        SqlServerCollectionFixture fixture,
        string mode = "read-only",
        string? defaultDatabase = null,
        IReadOnlyDictionary<string, string?>? additionalEnvironmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        SqlConnectionStringBuilder builder = new(fixture.MasterConnectionString);
        ILoggerFactory loggerFactory = LoggerFactory.Create(static _ => { });
        List<string> stderrLines = [];
        Dictionary<string, string?> environmentVariables = new()
        {
            ["SQLSERVER_HOST"] = ResolveHost(builder),
            ["SQLSERVER_PORT"] = ResolvePort(builder).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["SQLSERVER_DATABASE"] = defaultDatabase ?? "master",
            ["SQLSERVER_USERNAME"] = builder.UserID,
            ["SQLSERVER_PASSWORD"] = builder.Password,
            ["SQLSERVER_INTEGRATED_SECURITY"] = bool.FalseString.ToLowerInvariant(),
            ["SQLSERVER_ENCRYPT"] = bool.FalseString.ToLowerInvariant(),
            ["SQLSERVER_TRUST_SERVER_CERTIFICATE"] = bool.TrueString.ToLowerInvariant(),
            ["SQLSERVER_APPLICATION_NAME"] = "SimpleSqlServerMcp.IntegrationTests",
            ["MCP_SQLSERVER_MODE"] = mode,
            ["MCP_SQLSERVER_MAX_ROWS"] = "100",
            ["MCP_SQLSERVER_COMMAND_TIMEOUT"] = "15",
            ["MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES"] = "false",
        };

        if (additionalEnvironmentVariables is not null)
        {
            foreach ((string key, string? value) in additionalEnvironmentVariables)
            {
                environmentVariables[key] = value;
            }
        }

        StdioClientTransport transport = new(
            new StdioClientTransportOptions
            {
                Name = "SimpleSqlServerMcp.IntegrationTests",
                Command = ResolveExecutablePath(),
                WorkingDirectory = ResolveRepositoryRoot(),
                ShutdownTimeout = TimeSpan.FromSeconds(10),
                StandardErrorLines = line => stderrLines.Add(line),
                EnvironmentVariables = environmentVariables,
            },
            loggerFactory);

        McpClient client = await McpClient.CreateAsync(
            transport,
            new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "SimpleSqlServerMcp.IntegrationTests",
                    Version = "1.0.0",
                },
                InitializationTimeout = TimeSpan.FromSeconds(30),
            },
            loggerFactory,
            cancellationToken).ConfigureAwait(false);

        McpServerProcessHost host = new(loggerFactory, client);
        host._stderrLines.AddRange(stderrLines);
        return host;
    }

    public async Task<IReadOnlyList<McpClientTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        return (await _client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).ToArray();
    }

    public async Task<CallToolResult> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        return await _client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        _loggerFactory.Dispose();
    }

    private static string ResolveExecutablePath()
    {
        string? explicitPath = Environment.GetEnvironmentVariable("SIMPLE_SQLSERVER_MCP_TEST_SERVER_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            string normalizedExplicitPath = Path.GetFullPath(explicitPath);
            if (!File.Exists(normalizedExplicitPath))
            {
                throw new FileNotFoundException(
                    $"The override path from SIMPLE_SQLSERVER_MCP_TEST_SERVER_PATH does not exist: '{normalizedExplicitPath}'.");
            }

            return normalizedExplicitPath;
        }

        string executableName = OperatingSystem.IsWindows()
            ? "SimpleSqlServerMcp.exe"
            : "SimpleSqlServerMcp";

        string runtimeOutputRoot = Path.GetFullPath(Path.Combine(
            ResolveRepositoryRoot(),
            "src",
            "SimpleSqlServerMcp",
            "bin"));

        string preferredConfiguration = ResolvePreferredBuildConfiguration();
        string fallbackConfiguration = string.Equals(preferredConfiguration, "Debug", StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";

        string[] candidatePaths =
        [
            Path.Combine(runtimeOutputRoot, preferredConfiguration, "net8.0", executableName),
            Path.Combine(runtimeOutputRoot, fallbackConfiguration, "net8.0", executableName),
        ];

        string? path = candidatePaths.FirstOrDefault(File.Exists);
        if (path is null)
        {
            throw new FileNotFoundException(
                "Could not locate MCP server executable. Checked: "
                + string.Join(", ", candidatePaths.Select(static candidate => $"'{candidate}'")));
        }

        return path;
    }

    private static string ResolvePreferredBuildConfiguration()
    {
        string baseDirectory = AppContext.BaseDirectory;

        if (baseDirectory.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            baseDirectory.Contains($"{Path.AltDirectorySeparatorChar}Debug{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return "Debug";
        }

        if (baseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            baseDirectory.Contains($"{Path.AltDirectorySeparatorChar}Release{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return "Release";
        }

        return "Debug";
    }

    private static string ResolveRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));
    }

    private static string ResolveHost(SqlConnectionStringBuilder builder)
    {
        string dataSource = builder.DataSource;
        string[] segments = dataSource.Split([','], 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return segments[0];
    }

    private static int ResolvePort(SqlConnectionStringBuilder builder)
    {
        string dataSource = builder.DataSource;
        string[] segments = dataSource.Split([','], 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 1
            ? int.Parse(segments[1], System.Globalization.CultureInfo.InvariantCulture)
            : 1433;
    }
}
