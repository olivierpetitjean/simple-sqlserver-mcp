using System.Text.Json;
using System.Text.Json.Nodes;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class CopilotCliRegistrationWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Merge(string? existingContent, CopilotCliServerRegistration registration)
    {
        JsonObject root;

        if (string.IsNullOrWhiteSpace(existingContent))
        {
            root = [];
        }
        else
        {
            root = JsonNode.Parse(existingContent)?.AsObject()
                ?? throw new InvalidOperationException("Existing GitHub Copilot CLI config is not a JSON object.");
        }

        var mcpServers = root["mcpServers"] as JsonObject ?? new JsonObject();
        root["mcpServers"] = mcpServers;
        mcpServers[registration.ServerName] = registration.ToJsonObject();

        return root.ToJsonString(SerializerOptions) + Environment.NewLine;
    }
}
