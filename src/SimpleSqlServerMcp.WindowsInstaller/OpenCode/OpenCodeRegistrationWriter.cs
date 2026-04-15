namespace SimpleSqlServerMcp.WindowsInstaller;

public sealed class OpenCodeRegistrationWriter
{
    public string Merge(string? existingContent, OpenCodeServerRegistration registration)
    {
        var root = JsonConfigurationHelper.LoadRootObject(existingContent, "OpenCode");
        var mcp = JsonConfigurationHelper.GetOrCreateObject(root, "mcp", "OpenCode");
        mcp[registration.ServerName] = registration.ToJsonObject();
        return JsonConfigurationHelper.Serialize(root);
    }
}
