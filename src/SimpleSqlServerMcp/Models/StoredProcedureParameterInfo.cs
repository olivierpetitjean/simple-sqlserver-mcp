namespace SimpleSqlServerMcp.Models;

internal sealed class StoredProcedureParameterInfo
{
    public required string Name { get; init; }

    public required string DataType { get; init; }

    public bool IsOutput { get; init; }

    public bool HasDefaultValue { get; init; }

    public int Ordinal { get; init; }
}
