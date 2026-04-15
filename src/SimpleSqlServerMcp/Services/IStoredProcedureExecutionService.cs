using System.Text.Json;
using SimpleSqlServerMcp.Models;

namespace SimpleSqlServerMcp.Services;

internal interface IStoredProcedureExecutionService
{
    Task<ExecutedStoredProcedureResult> ExecuteStoredProcedureAsync(
        string database,
        string schema,
        string procedure,
        IReadOnlyDictionary<string, JsonElement>? parameters,
        string? targetDatabase,
        int? maxRows,
        int? timeoutSeconds,
        CancellationToken cancellationToken);
}
