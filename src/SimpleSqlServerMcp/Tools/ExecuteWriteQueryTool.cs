using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ExecuteWriteQueryTool(IMutableQueryExecutionService mutableQueryExecutionService)
{
    private readonly IMutableQueryExecutionService _mutableQueryExecutionService = mutableQueryExecutionService;

    [McpServerTool(Name = "execute_write_query"), Description("Executes a single mutable SQL statement when mutable mode is explicitly enabled.")]
    public Task<ExecutedWriteQueryResult> ExecuteWriteQuery(
        [Description("A single whitelisted mutable SQL statement.")] string sql,
        [Description("Optional SQL Server database context to use for execution. Leave empty to use the configured default database. Useful for DDL that must run inside a specific database context.")] string? targetDatabase = null,
        [Description("Optional timeout in seconds.")] int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return _mutableQueryExecutionService.ExecuteWriteQueryAsync(sql, targetDatabase, timeoutSeconds, cancellationToken);
    }
}
