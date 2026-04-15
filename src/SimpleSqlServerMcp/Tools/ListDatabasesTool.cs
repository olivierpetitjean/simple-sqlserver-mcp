using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ListDatabasesTool(ISchemaExplorerService schemaExplorerService)
{
    private readonly ISchemaExplorerService _schemaExplorerService = schemaExplorerService;

    [McpServerTool(Name = "list_databases"), Description("Lists accessible SQL Server databases with optional filtering.")]
    public Task<ListDatabasesResult> ListDatabases(
        [Description("Optional case-insensitive filter applied to database names.")] string? search = null,
        [Description("Maximum number of databases to return.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaExplorerService.ListDatabasesAsync(search, limit, cancellationToken);
    }
}
