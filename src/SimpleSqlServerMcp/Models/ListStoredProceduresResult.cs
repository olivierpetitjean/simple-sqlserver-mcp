namespace SimpleSqlServerMcp.Models;

internal sealed class ListStoredProceduresResult
{
    public required string Database { get; init; }

    public string? SchemaFilter { get; init; }

    public string? Search { get; init; }

    public int Count { get; init; }

    public IReadOnlyList<StoredProcedureInfo> Items { get; init; } = [];
}
