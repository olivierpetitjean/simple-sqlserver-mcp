using System.Text;
using System.Text.RegularExpressions;
using Tomlyn.Model;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class CodexServerRegistration
{
    private static readonly Regex BareKeyPattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    public string ServerName { get; }
    public string Command { get; }
    public string? WorkingDirectory { get; }
    public IReadOnlyList<string> Args { get; }
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    private CodexServerRegistration(
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

    public string Header => $"[mcp_servers.{FormatTableKey(ServerName)}]";

    public static CodexServerRegistration Create(InstallerOptions options)
        => new CodexServerRegistration(
            serverName: options.ServerName,
            command: options.CommandPath,
            workingDirectory: options.WorkingDirectory,
            args: Array.Empty<string>(),
            environmentVariables: WindowsEnvironmentVariablesBuilder.Build(options));

    public TomlTable ToTomlTable()
    {
        var serverTable = new TomlTable
        {
            ["command"] = Command,
            ["args"] = new TomlArray()
        };

        var environmentTable = new TomlTable();
        foreach (var pair in EnvironmentVariables)
        {
            environmentTable[pair.Key] = pair.Value;
        }

        serverTable["env"] = environmentTable;

        if (!string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            serverTable["cwd"] = WorkingDirectory;
        }

        return serverTable;
    }

    public string ToTomlBlock(string newLine)
    {
        var builder = new StringBuilder();
        builder.Append(Header).Append(newLine);
        builder.Append("command = ").Append(FormatTomlString(Command)).Append(newLine);
        builder.Append("args = []").Append(newLine);
        builder.Append("env = { ");
        builder.Append(string.Join(", ", EnvironmentVariables.Select(pair => $"{pair.Key} = {FormatTomlString(pair.Value)}")));
        builder.Append(" }").Append(newLine);

        if (!string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            builder.Append("cwd = ").Append(FormatTomlString(WorkingDirectory!)).Append(newLine);
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }

    private static string FormatTableKey(string value)
        => BareKeyPattern.IsMatch(value) ? value : FormatTomlString(value);

    private static string FormatTomlString(string value)
        => "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal) + "\"";
}
