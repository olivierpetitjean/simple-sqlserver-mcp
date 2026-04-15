namespace SimpleSqlServerMcp.Models;

internal sealed class ColumnSearchResultItem
{
    public required string Database { get; init; }

    public required string Schema { get; init; }

    public required string Table { get; init; }

    public required string Column { get; init; }

    public required string DataType { get; init; }
}
