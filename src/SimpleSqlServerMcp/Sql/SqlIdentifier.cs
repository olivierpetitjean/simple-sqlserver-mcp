namespace SimpleSqlServerMcp.Sql;

internal static class SqlIdentifier
{
    public static string Quote(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("SQL identifier cannot be null or whitespace.", nameof(identifier));
        }

        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
