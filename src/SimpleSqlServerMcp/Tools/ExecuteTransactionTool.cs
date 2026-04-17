using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ExecuteTransactionTool(ITransactionExecutionService transactionExecutionService)
{
    private readonly ITransactionExecutionService _transactionExecutionService = transactionExecutionService;

    [McpServerTool(Name = "execute_transaction"), Description("Executes one or more whitelisted mutable SQL statements atomically inside a single SQL Server transaction when mutable mode is enabled.")]
    public Task<ExecutedTransactionResult> ExecuteTransaction(
        [Description("One or more whitelisted mutable SQL statements executed atomically in order.")] IReadOnlyList<string> statements,
        [Description("Optional SQL Server database context to use for the transaction. Leave empty to use the configured default database.")] string? targetDatabase = null,
        [Description("Optional SQL Server transaction isolation level. Supported values: read_committed, read_uncommitted, repeatable_read, serializable, snapshot. Leave empty to use the default SQL Server behavior.")] string? isolationLevel = null,
        [Description("Optional timeout in seconds applied to each statement in the transaction.")] int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return _transactionExecutionService.ExecuteTransactionAsync(statements, targetDatabase, isolationLevel, timeoutSeconds, cancellationToken);
    }
}
