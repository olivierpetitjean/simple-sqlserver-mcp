using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class SearchColumnsTool(ISchemaExplorerService schemaExplorerService)
{
    private readonly ISchemaExplorerService _schemaExplorerService = schemaExplorerService;

    [McpServerTool(Name = "search_columns"), Description("Searches columns by name in a target database, optionally filtered by schema.")]
    public Task<SearchColumnsResult> SearchColumns(
        [Description("Target database name.")] string database,
        [Description("Case-insensitive column-name search term.")] string search,
        [Description("Optional schema filter.")] string? schema = null,
        [Description("Maximum number of matches to return.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaExplorerService.SearchColumnsAsync(database, search, schema, limit, cancellationToken);
    }
}
