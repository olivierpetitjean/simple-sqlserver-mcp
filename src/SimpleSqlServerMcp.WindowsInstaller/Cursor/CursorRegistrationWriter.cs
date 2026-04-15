namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class CursorRegistrationWriter
{
    public string Merge(string? existingContent, CursorServerRegistration registration)
    {
        var root = JsonConfigurationHelper.LoadRootObject(existingContent, "Cursor");
        var mcpServers = JsonConfigurationHelper.GetOrCreateObject(root, "mcpServers", "Cursor");
        mcpServers[registration.ServerName] = registration.ToJsonObject();
        return JsonConfigurationHelper.Serialize(root);
    }
}
