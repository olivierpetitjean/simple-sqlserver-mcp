using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class DescribeTableTool(ISchemaExplorerService schemaExplorerService)
{
    private readonly ISchemaExplorerService _schemaExplorerService = schemaExplorerService;

    [McpServerTool(Name = "describe_table"), Description("Describes a table with columns, primary key columns, and outgoing foreign keys.")]
    public Task<TableDescriptionResult> DescribeTable(
        [Description("Target database name.")] string database,
        [Description("Target schema name.")] string schema,
        [Description("Target table name.")] string table,
        CancellationToken cancellationToken = default)
    {
        return _schemaExplorerService.DescribeTableAsync(database, schema, table, cancellationToken);
    }
}
