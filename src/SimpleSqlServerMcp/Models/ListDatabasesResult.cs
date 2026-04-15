namespace SimpleSqlServerMcp.Models;

internal sealed class ListDatabasesResult
{
    public int Count { get; init; }

    public IReadOnlyList<DatabaseInfo> Items { get; init; } = [];
}
