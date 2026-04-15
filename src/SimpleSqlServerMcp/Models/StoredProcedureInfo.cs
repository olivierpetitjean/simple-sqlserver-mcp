namespace SimpleSqlServerMcp.Models;

internal sealed class StoredProcedureInfo
{
    public required string Database { get; init; }

    public required string Schema { get; init; }

    public required string Name { get; init; }

    public DateTime? CreatedAtUtc { get; init; }

    public DateTime? ModifiedAtUtc { get; init; }
}
