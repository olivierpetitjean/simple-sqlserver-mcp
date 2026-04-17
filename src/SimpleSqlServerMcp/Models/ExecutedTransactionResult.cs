namespace SimpleSqlServerMcp.Models;

internal sealed class ExecutedTransactionResult
{
    public required string Database { get; init; }

    public required string IsolationLevel { get; init; }

    public bool Committed { get; init; }

    public IReadOnlyList<ExecutedTransactionStatementResult> Statements { get; init; } = [];

    public long DurationMilliseconds { get; init; }
}
