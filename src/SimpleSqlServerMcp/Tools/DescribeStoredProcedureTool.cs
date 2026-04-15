using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class DescribeStoredProcedureTool(ISchemaExplorerService schemaExplorerService)
{
    private readonly ISchemaExplorerService _schemaExplorerService = schemaExplorerService;

    [McpServerTool(Name = "describe_stored_procedure"), Description("Describes a stored procedure, including parameters, definition, and first result-set metadata when SQL Server can infer it.")]
    public Task<StoredProcedureDescriptionResult> DescribeStoredProcedure(
        [Description("Target database name.")] string database,
        [Description("Target schema name.")] string schema,
        [Description("Target stored procedure name.")] string procedure,
        CancellationToken cancellationToken = default)
    {
        return _schemaExplorerService.DescribeStoredProcedureAsync(database, schema, procedure, cancellationToken);
    }
}
