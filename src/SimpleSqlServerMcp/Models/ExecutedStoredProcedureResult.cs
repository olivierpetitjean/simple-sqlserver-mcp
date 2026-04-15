namespace SimpleSqlServerMcp.Models;

internal sealed class ExecutedStoredProcedureResult
{
    public required string Database { get; init; }

    public required string Schema { get; init; }

    public required string Procedure { get; init; }

    public IReadOnlyList<TabularQueryResult> ResultSets { get; init; } = [];

    public IReadOnlyDictionary<string, object?> OutputParameters { get; init; } = new Dictionary<string, object?>();

    public int ReturnValue { get; init; }

    public int RowsAffected { get; init; }

    public long DurationMilliseconds { get; init; }
}
