using SimpleSqlServerMcp.Models;

namespace SimpleSqlServerMcp.Services;

internal interface ISchemaExplorerService
{
    Task<ListDatabasesResult> ListDatabasesAsync(string? search, int? limit, CancellationToken cancellationToken);

    Task<ListTablesResult> ListTablesAsync(
        string database,
        string? schema,
        string? search,
        int? limit,
        CancellationToken cancellationToken);

    Task<TableDescriptionResult> DescribeTableAsync(
        string database,
        string schema,
        string table,
        CancellationToken cancellationToken);

    Task<SearchColumnsResult> SearchColumnsAsync(
        string database,
        string search,
        string? schema,
        int? limit,
        CancellationToken cancellationToken);

    Task<TabularQueryResult> GetTableSampleAsync(
        string database,
        string schema,
        string table,
        int? limit,
        CancellationToken cancellationToken);

    Task<ListStoredProceduresResult> ListStoredProceduresAsync(
        string database,
        string? schema,
        string? search,
        int? limit,
        CancellationToken cancellationToken);

    Task<StoredProcedureDescriptionResult> DescribeStoredProcedureAsync(
        string database,
        string schema,
        string procedure,
        CancellationToken cancellationToken);
}
