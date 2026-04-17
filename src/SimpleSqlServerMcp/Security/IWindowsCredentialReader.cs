namespace SimpleSqlServerMcp.Security;

internal interface IWindowsCredentialReader
{
    string? ReadGenericCredential(string targetName);
}
