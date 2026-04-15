using YamlDotNet.RepresentationModel;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class ContinueServerRegistration
{
    public string ServerName { get; }
    public string Command { get; }
    public string? WorkingDirectory { get; }
    public IReadOnlyList<string> Args { get; }
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    private ContinueServerRegistration(
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

    public static ContinueServerRegistration Create(InstallerOptions options)
        => new(
            serverName: options.ServerName,
            command: options.CommandPath,
            workingDirectory: options.WorkingDirectory,
            args: Array.Empty<string>(),
            environmentVariables: WindowsEnvironmentVariablesBuilder.Build(options));

    public YamlMappingNode ToYamlNode()
    {
        var mapping = new YamlMappingNode
        {
            { "name", ServerName },
            { "command", Command },
            { "args", new YamlSequenceNode(Args.Select(argument => new YamlScalarNode(argument))) },
            { "env", new YamlMappingNode(EnvironmentVariables.Select(pair => new KeyValuePair<YamlNode, YamlNode>(new YamlScalarNode(pair.Key), new YamlScalarNode(pair.Value)))) }
        };

        if (!string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            mapping.Add("cwd", WorkingDirectory);
        }

        return mapping;
    }
}
