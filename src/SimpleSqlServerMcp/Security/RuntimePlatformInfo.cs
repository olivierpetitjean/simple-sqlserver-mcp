namespace SimpleSqlServerMcp.Security;

internal sealed class RuntimePlatformInfo : IPlatformInfo
{
    public bool IsWindows() => OperatingSystem.IsWindows();
}
