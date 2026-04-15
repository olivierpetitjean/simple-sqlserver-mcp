using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Safety;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.Services;

internal sealed class MutableQueryExecutionService(
    ISqlConnectionFactory connectionFactory,
    IMutableQueryValidator mutableQueryValidator,
    IOptions<SqlServerMcpOptions> options) : IMutableQueryExecutionService
{
    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly IMutableQueryValidator _mutableQueryValidator = mutableQueryValidator;
    private readonly SqlServerMcpOptions _options = options.Value;

    public async Task<ExecutedWriteQueryResult> ExecuteWriteQueryAsync(
        string sql,
        string? targetDatabase,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (_options.Mode != QueryExecutionMode.Mutable)
        {
            throw new InvalidOperationException(
                "execute_write_query is disabled because MCP_SQLSERVER_MODE is not set to mutable.");
        }

        string statementType = _mutableQueryValidator.Validate(sql);
        int effectiveTimeoutSeconds = NormalizeRequestedValue(timeoutSeconds, _options.CommandTimeoutSeconds);
        string effectiveDatabase = ResolveDatabase(targetDatabase);

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(effectiveDatabase, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = effectiveTimeoutSeconds;

        Stopwatch stopwatch = Stopwatch.StartNew();
        int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        return new ExecutedWriteQueryResult
        {
            StatementType = statementType,
            RowsAffected = rowsAffected,
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
}
