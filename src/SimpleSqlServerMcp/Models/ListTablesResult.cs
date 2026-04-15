namespace SimpleSqlServerMcp.Models;

internal sealed class ListTablesResult
{
    public required string Database { get; init; }

    public int Count { get; init; }

    public IReadOnlyList<TableInfo> Items { get; init; } = [];
}
