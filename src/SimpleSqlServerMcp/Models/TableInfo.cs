namespace SimpleSqlServerMcp.Models;

internal sealed class TableInfo
{
    public required string Database { get; init; }

    public required string Schema { get; init; }

    public required string Name { get; init; }
}
