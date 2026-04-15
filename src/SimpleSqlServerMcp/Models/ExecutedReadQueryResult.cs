namespace SimpleSqlServerMcp.Models;

internal sealed class ExecutedReadQueryResult
{
    public IReadOnlyList<string> Columns { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = [];

    public int RowCount { get; init; }

    public bool Truncated { get; init; }

    public long DurationMilliseconds { get; init; }
}
