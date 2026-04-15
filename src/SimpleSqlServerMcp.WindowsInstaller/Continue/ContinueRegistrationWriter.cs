using System.Text;
using YamlDotNet.RepresentationModel;

namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class ContinueRegistrationWriter
{
    public string Merge(string? existingContent, ContinueServerRegistration registration)
    {
        var root = LoadRoot(existingContent);
        EnsureMetadata(root);

        var serverSequence = GetOrCreateServerSequence(root);
        RemoveExistingServer(serverSequence, registration.ServerName);
        serverSequence.Add(registration.ToYamlNode());

        var stream = new YamlStream(new YamlDocument(root));
        using var writer = new StringWriter(new StringBuilder());
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    private static YamlMappingNode LoadRoot(string? existingContent)
    {
        if (string.IsNullOrWhiteSpace(existingContent))
        {
            return [];
        }

        var stream = new YamlStream();
        using var reader = new StringReader(existingContent);
        stream.Load(reader);

        return stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode mapping
            ? mapping
            : [];
    }

    private static void EnsureMetadata(YamlMappingNode root)
    {
        AddIfMissing(root, "name", "Local Config");
        AddIfMissing(root, "version", "1.0.0");
        AddIfMissing(root, "schema", "v1");
    }

    private static void AddIfMissing(YamlMappingNode root, string key, string value)
    {
        var keyNode = new YamlScalarNode(key);
        if (!root.Children.ContainsKey(keyNode))
        {
            root.Add(key, value);
        }
    }

    private static YamlSequenceNode GetOrCreateServerSequence(YamlMappingNode root)
    {
        var keyNode = new YamlScalarNode("mcpServers");
        if (root.Children.TryGetValue(keyNode, out var existingNode) && existingNode is YamlSequenceNode existingSequence)
        {
            return existingSequence;
        }

        var created = new YamlSequenceNode();
        root.Children[keyNode] = created;
        return created;
    }

    private static void RemoveExistingServer(YamlSequenceNode sequence, string serverName)
    {
        var matchingNodes = sequence.Children
            .OfType<YamlMappingNode>()
            .Where(node => node.Children.TryGetValue(new YamlScalarNode("name"), out var nameNode) &&
                           string.Equals((nameNode as YamlScalarNode)?.Value, serverName, StringComparison.Ordinal))
            .ToList();

        foreach (var node in matchingNodes)
        {
            sequence.Children.Remove(node);
        }
    }
}
