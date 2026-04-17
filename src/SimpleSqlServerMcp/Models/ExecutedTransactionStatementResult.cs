namespace SimpleSqlServerMcp.Models;

internal sealed class ExecutedTransactionStatementResult
{
    public int Index { get; init; }

    public required string StatementType { get; init; }

    public int RowsAffected { get; init; }

    public long DurationMilliseconds { get; init; }
}
