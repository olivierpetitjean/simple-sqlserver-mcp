using SimpleSqlServerMcp.Models;

namespace SimpleSqlServerMcp.Services;

internal interface ITransactionExecutionService
{
    Task<ExecutedTransactionResult> ExecuteTransactionAsync(
        IReadOnlyList<string> statements,
        string? targetDatabase,
        string? isolationLevel,
        int? timeoutSeconds,
        CancellationToken cancellationToken);
}
