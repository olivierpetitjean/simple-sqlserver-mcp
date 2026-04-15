namespace SimpleSqlServerMcp.Models;

internal sealed class ForeignKeyInfo
{
    public required string Name { get; init; }

    public required string SourceColumn { get; init; }

    public required string ReferencedSchema { get; init; }

    public required string ReferencedTable { get; init; }

    public required string ReferencedColumn { get; init; }
}
