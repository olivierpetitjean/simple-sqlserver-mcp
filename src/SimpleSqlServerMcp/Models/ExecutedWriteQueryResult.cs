namespace SimpleSqlServerMcp.Models;

internal sealed class ExecutedWriteQueryResult
{
    public required string StatementType { get; init; }

    public int RowsAffected { get; init; }

    public long DurationMilliseconds { get; init; }
}
