namespace SimpleSqlServerMcp.Models;

internal sealed class SearchColumnsResult
{
    public required string Database { get; init; }

    public string? SchemaFilter { get; init; }

    public required string Search { get; init; }

    public int Count { get; init; }

    public IReadOnlyList<ColumnSearchResultItem> Items { get; init; } = [];
}
