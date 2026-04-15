namespace SimpleSqlServerMcp.Models;

internal sealed class TableDescriptionResult
{
    public required string Database { get; init; }

    public required string Schema { get; init; }

    public required string Table { get; init; }

    public IReadOnlyList<TableColumnInfo> Columns { get; init; } = [];

    public IReadOnlyList<string> PrimaryKeyColumns { get; init; } = [];

    public IReadOnlyList<ForeignKeyInfo> ForeignKeys { get; init; } = [];
}
