using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ExecuteStoredProcedureTool(IStoredProcedureExecutionService storedProcedureExecutionService)
{
    private readonly IStoredProcedureExecutionService _storedProcedureExecutionService = storedProcedureExecutionService;

    [McpServerTool(Name = "execute_stored_procedure"), Description("Executes a stored procedure in mutable mode and returns result sets, output parameters, and return value.")]
    public Task<ExecutedStoredProcedureResult> ExecuteStoredProcedure(
        [Description("Target database name where the procedure exists.")] string database,
        [Description("Target schema name.")] string schema,
        [Description("Target stored procedure name.")] string procedure,
        [Description("Optional procedure parameters keyed by parameter name, with or without the leading @.")] IReadOnlyDictionary<string, JsonElement>? parameters = null,
        [Description("Optional execution context database. Defaults to the database argument.")] string? targetDatabase = null,
        [Description("Optional maximum number of rows to return per result set.")] int? maxRows = null,
        [Description("Optional timeout in seconds.")] int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return _storedProcedureExecutionService.ExecuteStoredProcedureAsync(
            database,
            schema,
            procedure,
            parameters,
            targetDatabase,
            maxRows,
            timeoutSeconds,
            cancellationToken);
    }
}
