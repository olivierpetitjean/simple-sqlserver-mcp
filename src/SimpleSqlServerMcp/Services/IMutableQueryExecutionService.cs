using SimpleSqlServerMcp.Models;

namespace SimpleSqlServerMcp.Services;

internal interface IMutableQueryExecutionService
{
    Task<ExecutedWriteQueryResult> ExecuteWriteQueryAsync(
        string sql,
        string? targetDatabase,
        int? timeoutSeconds,
        CancellationToken cancellationToken);
}
