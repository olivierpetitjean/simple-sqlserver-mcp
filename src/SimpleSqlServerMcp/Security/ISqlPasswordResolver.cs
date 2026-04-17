namespace SimpleSqlServerMcp.Security;

internal interface ISqlPasswordResolver
{
    string? ResolvePassword();
}
