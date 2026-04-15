using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Safety;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.Services;

internal sealed class QueryExecutionService(
    ISqlConnectionFactory connectionFactory,
    IReadOnlyQueryValidator readOnlyQueryValidator,
    IOptions<SqlServerMcpOptions> options) : IQueryExecutionService
{
    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly IReadOnlyQueryValidator _readOnlyQueryValidator = readOnlyQueryValidator;
    private readonly SqlServerMcpOptions _options = options.Value;

    public async Task<ExecutedReadQueryResult> ExecuteReadQueryAsync(
        string sql,
        string? targetDatabase,
        int? maxRows,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        _readOnlyQueryValidator.Validate(sql);

        int effectiveMaxRows = NormalizeRequestedValue(maxRows, _options.MaxRows);
        int effectiveTimeoutSeconds = NormalizeRequestedValue(timeoutSeconds, _options.CommandTimeoutSeconds);
        string effectiveDatabase = ResolveDatabase(targetDatabase);

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(effectiveDatabase, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = effectiveTimeoutSeconds;

        Stopwatch stopwatch = Stopwatch.StartNew();
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        TabularQueryResult tabularResult = await ReadTabularResultAsync(reader, effectiveMaxRows, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        return new ExecutedReadQueryResult
        {
            Columns = tabularResult.Columns,
            Rows = tabularResult.Rows,
            RowCount = tabularResult.RowCount,
            Truncated = tabularResult.Truncated,
            DurationMilliseconds = stopwatch.ElapsedMilliseconds,
        };
    }

    private string ResolveDatabase(string? targetDatabase)
    {
        string database = string.IsNullOrWhiteSpace(targetDatabase)
            ? _options.Database
            : targetDatabase.Trim();

        DatabaseAccessPolicy.EnsureAllowed(_options, database);
        return database;
    }

    private static int NormalizeRequestedValue(int? requestedValue, int fallback)
    {
        if (requestedValue is null or <= 0)
        {
            return fallback;
        }

        return Math.Min(requestedValue.Value, fallback);
    }

    private static async Task<TabularQueryResult> ReadTabularResultAsync(
        SqlDataReader reader,
        int effectiveLimit,
        CancellationToken cancellationToken)
    {
        List<string> columns = [];
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        List<IReadOnlyList<object?>> rows = [];
        bool truncated = false;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (rows.Count >= effectiveLimit)
            {
                truncated = true;
                break;
            }

            object?[] values = new object?[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        return new TabularQueryResult
        {
            Columns = columns,
            Rows = rows,
            RowCount = rows.Count,
            Truncated = truncated,
        };
    }
}
