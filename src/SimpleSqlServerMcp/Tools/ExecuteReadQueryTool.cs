using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ExecuteReadQueryTool(IQueryExecutionService queryExecutionService)
{
    private readonly IQueryExecutionService _queryExecutionService = queryExecutionService;

    [McpServerTool(Name = "execute_read_query"), Description("Executes a single read-only SELECT query with bounded rows and timeout.")]
    public Task<ExecutedReadQueryResult> ExecuteReadQuery(
        [Description("A single read-only T-SQL SELECT statement.")] string sql,
        [Description("Optional SQL Server database context to use for execution. Leave empty to use the configured default database.")] string? targetDatabase = null,
        [Description("Optional maximum number of rows to return.")] int? maxRows = null,
        [Description("Optional timeout in seconds.")] int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return _queryExecutionService.ExecuteReadQueryAsync(sql, targetDatabase, maxRows, timeoutSeconds, cancellationToken);
    }
}
