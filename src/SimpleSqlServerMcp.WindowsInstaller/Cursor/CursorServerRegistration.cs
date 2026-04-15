using System.Text.Json.Nodes;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class CursorServerRegistration
{
    public string ServerName { get; }
    public string Command { get; }
    public IReadOnlyList<string> Args { get; }
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    private CursorServerRegistration(
        string serverName,
        string command,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string> environmentVariables)
    {
        ServerName = serverName;
        Command = command;
        Args = args;
        EnvironmentVariables = environmentVariables;
    }

    public static CursorServerRegistration Create(InstallerOptions options)
        => new(
            serverName: options.ServerName,
            command: options.CommandPath,
            args: Array.Empty<string>(),
            environmentVariables: WindowsEnvironmentVariablesBuilder.Build(options));

    public JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["command"] = Command,
            ["args"] = new JsonArray(Args.Select(argument => (JsonNode?)argument).ToArray()),
            ["env"] = new JsonObject(EnvironmentVariables.Select(pair => new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value)).ToArray())
        };
    }
}
