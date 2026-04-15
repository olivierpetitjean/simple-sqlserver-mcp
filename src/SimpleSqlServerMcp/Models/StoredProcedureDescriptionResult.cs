namespace SimpleSqlServerMcp.Models;

internal sealed class StoredProcedureDescriptionResult
{
    public required string Database { get; init; }

    public required string Schema { get; init; }

    public required string Procedure { get; init; }

    public DateTime? CreatedAtUtc { get; init; }

    public DateTime? ModifiedAtUtc { get; init; }

    public string? Definition { get; init; }

    public IReadOnlyList<StoredProcedureParameterInfo> Parameters { get; init; } = [];

    public IReadOnlyList<StoredProcedureResultSetColumnInfo> FirstResultSet { get; init; } = [];
}
