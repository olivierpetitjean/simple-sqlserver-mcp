using System.Text.Json.Nodes;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class OpenCodeServerRegistration
{
    public string ServerName { get; }
    public string Command { get; }
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    private OpenCodeServerRegistration(
        string serverName,
        string command,
        IReadOnlyDictionary<string, string> environmentVariables)
    {
        ServerName = serverName;
        Command = command;
        EnvironmentVariables = environmentVariables;
    }

    public static OpenCodeServerRegistration Create(InstallerOptions options)
        => new(
            serverName: options.ServerName,
            command: options.CommandPath,
            environmentVariables: WindowsEnvironmentVariablesBuilder.Build(options));

    public JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["type"] = "local",
            ["command"] = new JsonArray(Command),
            ["enabled"] = true,
            ["environment"] = new JsonObject(EnvironmentVariables.Select(pair => new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value)).ToArray())
        };
    }
}
