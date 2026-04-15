namespace SimpleSqlServerMcp.Models;

internal sealed class StoredProcedureResultSetColumnInfo
{
    public int Ordinal { get; init; }

    public string? Name { get; init; }

    public string? DataType { get; init; }

    public bool? IsNullable { get; init; }
}
