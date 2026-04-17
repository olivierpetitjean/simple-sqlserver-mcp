namespace SimpleSqlServerMcp.WindowsInstaller;

public interface IWindowsCredentialWriter
{
    void WriteGenericCredential(string targetName, string secret, string? username);
}
