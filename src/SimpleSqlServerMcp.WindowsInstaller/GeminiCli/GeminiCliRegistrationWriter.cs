namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class GeminiCliRegistrationWriter
{
    public string Merge(string? existingContent, GeminiCliServerRegistration registration)
    {
        var root = JsonConfigurationHelper.LoadRootObject(existingContent, "Gemini CLI");
        var mcpServers = JsonConfigurationHelper.GetOrCreateObject(root, "mcpServers", "Gemini CLI");
        mcpServers[registration.ServerName] = registration.ToJsonObject();
        return JsonConfigurationHelper.Serialize(root);
    }
}
