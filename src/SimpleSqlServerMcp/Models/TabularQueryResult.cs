namespace SimpleSqlServerMcp.Models;

internal sealed class TabularQueryResult
{
    public IReadOnlyList<string> Columns { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = [];

    public int RowCount { get; init; }

    public bool Truncated { get; init; }
}
