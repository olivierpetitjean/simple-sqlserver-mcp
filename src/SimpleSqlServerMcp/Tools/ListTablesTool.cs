using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ListTablesTool(ISchemaExplorerService schemaExplorerService)
{
    private readonly ISchemaExplorerService _schemaExplorerService = schemaExplorerService;

    [McpServerTool(Name = "list_tables"), Description("Lists tables in a target database with optional schema and name filtering.")]
    public Task<ListTablesResult> ListTables(
        [Description("Target database name.")] string database,
        [Description("Optional schema filter.")] string? schema = null,
        [Description("Optional case-insensitive table-name filter.")] string? search = null,
        [Description("Maximum number of tables to return.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaExplorerService.ListTablesAsync(database, schema, search, limit, cancellationToken);
    }
}
