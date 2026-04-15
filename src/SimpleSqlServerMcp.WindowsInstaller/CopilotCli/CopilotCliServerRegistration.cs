using System.Text.Json.Nodes;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class CopilotCliServerRegistration
{
    public string ServerName { get; }
    public string Command { get; }
    public string? WorkingDirectory { get; }
    public IReadOnlyList<string> Args { get; }
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    private CopilotCliServerRegistration(
        string serverName,
        string command,
        string? workingDirectory,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string> environmentVariables)
    {
        ServerName = serverName;
        Command = command;
        WorkingDirectory = workingDirectory;
        Args = args;
        EnvironmentVariables = environmentVariables;
    }

    public static CopilotCliServerRegistration Create(InstallerOptions options)
        => new(
            serverName: options.ServerName,
            command: options.CommandPath,
            workingDirectory: options.WorkingDirectory,
            args: Array.Empty<string>(),
            environmentVariables: WindowsEnvironmentVariablesBuilder.Build(options));

    public JsonObject ToJsonObject()
    {
        var result = new JsonObject
        {
            ["type"] = "local",
            ["command"] = Command,
            ["args"] = new JsonArray(Args.Select(argument => (JsonNode?)argument).ToArray()),
            ["env"] = new JsonObject(EnvironmentVariables.Select(pair => new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value)).ToArray()),
            ["tools"] = new JsonArray("*")
        };

        if (!string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            result["cwd"] = WorkingDirectory;
        }

        return result;
    }
}
