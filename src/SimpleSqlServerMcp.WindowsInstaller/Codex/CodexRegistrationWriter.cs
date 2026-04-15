using System.Text.Json;
using Tomlyn;
using Tomlyn.Model;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class CodexRegistrationWriter
{
    private static readonly TomlSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull
    };

    public string Merge(string? existingContent, CodexServerRegistration registration)
    {
        var root = LoadRoot(existingContent);
        var mcpServers = GetOrCreateTomlTable(root, "mcp_servers");
        mcpServers[registration.ServerName] = registration.ToTomlTable();

        return TomlSerializer.Serialize(root, SerializerOptions).TrimEnd('\r', '\n') + Environment.NewLine;
    }

    private static TomlTable LoadRoot(string? existingContent)
    {
        if (string.IsNullOrWhiteSpace(existingContent))
        {
            return new TomlTable();
        }

        var parsed = TomlSerializer.Deserialize<TomlTable>(existingContent);
        return parsed ?? throw new InvalidOperationException("Existing Codex config is not a TOML table.");
    }

    private static TomlTable GetOrCreateTomlTable(TomlTable root, string key)
    {
        if (root.TryGetValue(key, out var existingValue))
        {
            if (existingValue is TomlTable existingTable)
            {
                return existingTable;
            }

            throw new InvalidOperationException(
                $"Existing Codex config contains `{key}` but it is not a TOML table.");
        }

        var createdTable = new TomlTable();
        root[key] = createdTable;
        return createdTable;
    }
}
