namespace SimpleSqlServerMcp.Models;

internal sealed class TableColumnInfo
{
    public required string Name { get; init; }

    public required string DataType { get; init; }

    public bool IsNullable { get; init; }

    public bool IsIdentity { get; init; }
}
