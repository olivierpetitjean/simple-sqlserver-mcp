using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ListStoredProceduresTool(ISchemaExplorerService schemaExplorerService)
{
    private readonly ISchemaExplorerService _schemaExplorerService = schemaExplorerService;

    [McpServerTool(Name = "list_stored_procedures"), Description("Lists stored procedures in a target database with optional schema and name filtering.")]
    public Task<ListStoredProceduresResult> ListStoredProcedures(
        [Description("Target database name.")] string database,
        [Description("Optional schema filter.")] string? schema = null,
        [Description("Optional case-insensitive stored-procedure-name filter.")] string? search = null,
        [Description("Maximum number of stored procedures to return.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaExplorerService.ListStoredProceduresAsync(database, schema, search, limit, cancellationToken);
    }
}
