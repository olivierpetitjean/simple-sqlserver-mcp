using SimpleSqlServerMcp.Models;

namespace SimpleSqlServerMcp.Services;

internal interface IQueryExecutionService
{
    Task<ExecutedReadQueryResult> ExecuteReadQueryAsync(
        string sql,
        string? targetDatabase,
        int? maxRows,
        int? timeoutSeconds,
        CancellationToken cancellationToken);
}
