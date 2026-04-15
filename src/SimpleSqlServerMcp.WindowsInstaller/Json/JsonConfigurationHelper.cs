using System.Text.Json;
using System.Text.Json.Nodes;

namespace SimpleSqlServerMcp.WindowsInstaller;

public static class JsonConfigurationHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static JsonObject LoadRootObject(string? existingContent, string configDisplayName)
    {
        if (string.IsNullOrWhiteSpace(existingContent))
        {
            return [];
        }

        return JsonNode.Parse(existingContent, documentOptions: DocumentOptions)?.AsObject()
            ?? throw new InvalidOperationException($"Existing {configDisplayName} config is not a JSON object.");
    }

    public static JsonObject GetOrCreateObject(JsonObject root, string propertyName, string configDisplayName)
    {
        if (root[propertyName] is null)
        {
            var created = new JsonObject();
            root[propertyName] = created;
            return created;
        }

        return root[propertyName] as JsonObject
            ?? throw new InvalidOperationException(
                $"Existing {configDisplayName} config contains `{propertyName}` but it is not a JSON object.");
    }

    public static string Serialize(JsonObject root)
        => root.ToJsonString(SerializerOptions) + Environment.NewLine;
}
