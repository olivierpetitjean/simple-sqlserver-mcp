using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class GetTableSampleTool(ISchemaExplorerService schemaExplorerService)
{
    private readonly ISchemaExplorerService _schemaExplorerService = schemaExplorerService;

    [McpServerTool(Name = "get_table_sample"), Description("Returns a bounded sample of rows from a target table.")]
    public Task<TabularQueryResult> GetTableSample(
        [Description("Target database name.")] string database,
        [Description("Target schema name.")] string schema,
        [Description("Target table name.")] string table,
        [Description("Maximum number of rows to return.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        return _schemaExplorerService.GetTableSampleAsync(database, schema, table, limit, cancellationToken);
    }
}
