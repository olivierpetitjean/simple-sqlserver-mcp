using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Safety;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.Services;

internal sealed class TransactionExecutionService(
    ISqlConnectionFactory connectionFactory,
    IMutableQueryValidator mutableQueryValidator,
    IOptions<SqlServerMcpOptions> options) : ITransactionExecutionService
{
    private static readonly HashSet<string> TransactionUnsupportedStatementTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "UNSAFE PATTERN",
        "CREATE DATABASE",
        "ALTER DATABASE",
        "DROP DATABASE",
        "BACKUP",
        "BULK INSERT",
    };

    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly IMutableQueryValidator _mutableQueryValidator = mutableQueryValidator;
    private readonly SqlServerMcpOptions _options = options.Value;

    public async Task<ExecutedTransactionResult> ExecuteTransactionAsync(
        IReadOnlyList<string> statements,
        string? targetDatabase,
        string? isolationLevel,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (_options.Mode != QueryExecutionMode.Mutable)
        {
            throw new InvalidOperationException(
                "execute_transaction is disabled because MCP_SQLSERVER_MODE is not set to mutable.");
        }

        ArgumentNullException.ThrowIfNull(statements);
        if (statements.Count == 0)
        {
            throw new InvalidOperationException("execute_transaction requires at least one SQL statement.");
        }

        List<(string Sql, string StatementType)> validatedStatements = ValidateStatements(statements);
        int effectiveTimeoutSeconds = NormalizeRequestedValue(timeoutSeconds, _options.CommandTimeoutSeconds);
        string effectiveDatabase = ResolveDatabase(targetDatabase);
        (IsolationLevel? isolationLevelValue, string isolationLevelName) = ParseIsolationLevel(isolationLevel);

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(effectiveDatabase, cancellationToken).ConfigureAwait(false);
        await EnableXactAbortAsync(connection, effectiveTimeoutSeconds, cancellationToken).ConfigureAwait(false);
        await using SqlTransaction transaction = (SqlTransaction)(isolationLevelValue is null
            ? await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : await connection.BeginTransactionAsync(isolationLevelValue.Value, cancellationToken).ConfigureAwait(false));

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        List<ExecutedTransactionStatementResult> results = [];

        try
        {
            for (int i = 0; i < validatedStatements.Count; i++)
            {
                (string sql, string statementType) = validatedStatements[i];

                await using SqlCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.CommandTimeout = effectiveTimeoutSeconds;

                Stopwatch statementStopwatch = Stopwatch.StartNew();
                int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                statementStopwatch.Stop();

                results.Add(new ExecutedTransactionStatementResult
                {
                    Index = i,
                    StatementType = statementType,
                    RowsAffected = rowsAffected,
                    DurationMilliseconds = statementStopwatch.ElapsedMilliseconds,
                });
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            totalStopwatch.Stop();

            return new ExecutedTransactionResult
            {
                Database = effectiveDatabase,
                IsolationLevel = isolationLevelName,
                Committed = true,
                Statements = results,
                DurationMilliseconds = totalStopwatch.ElapsedMilliseconds,
            };
        }
        catch (Exception exception)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception rollbackException)
            {
                throw new InvalidOperationException(
                    $"Transaction failed and rollback also failed: {rollbackException.Message}",
                    new AggregateException(exception, rollbackException));
            }

            throw new InvalidOperationException(
                BuildFailureMessage(
                    validatedStatements.Select(static statement => statement.StatementType).ToArray(),
                    results.Count,
                    exception.Message),
                exception);
        }
    }

    private List<(string Sql, string StatementType)> ValidateStatements(IReadOnlyList<string> statements)
    {
        List<(string Sql, string StatementType)> validatedStatements = new(statements.Count);
        foreach (string statement in statements)
        {
            string statementType = _mutableQueryValidator.Validate(statement);
            if (TransactionUnsupportedStatementTypes.Contains(statementType))
            {
                throw new InvalidOperationException(
                    $"execute_transaction does not support '{statementType}' statements. Use execute_write_query for standalone execution instead.");
            }

            validatedStatements.Add((statement, statementType));
        }

        return validatedStatements;
    }

    private string ResolveDatabase(string? targetDatabase)
    {
        string database = string.IsNullOrWhiteSpace(targetDatabase)
            ? _options.Database
            : targetDatabase.Trim();

        DatabaseAccessPolicy.EnsureAllowed(_options, database);
        return database;
    }

    private static async Task EnableXactAbortAsync(
        SqlConnection connection,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SET XACT_ABORT ON;";
        command.CommandTimeout = timeoutSeconds;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static int NormalizeRequestedValue(int? requestedValue, int fallback)
    {
        if (requestedValue is null or <= 0)
        {
            return fallback;
        }

        return Math.Min(requestedValue.Value, fallback);
    }

    private static (IsolationLevel? Value, string Name) ParseIsolationLevel(string? isolationLevel)
    {
        if (string.IsNullOrWhiteSpace(isolationLevel))
        {
            return (null, "default");
        }

        string normalized = isolationLevel
            .Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized switch
        {
            "read_committed" => (IsolationLevel.ReadCommitted, "read_committed"),
            "read_uncommitted" => (IsolationLevel.ReadUncommitted, "read_uncommitted"),
            "repeatable_read" => (IsolationLevel.RepeatableRead, "repeatable_read"),
            "serializable" => (IsolationLevel.Serializable, "serializable"),
            "snapshot" => (IsolationLevel.Snapshot, "snapshot"),
            _ => throw new InvalidOperationException(
                "Unsupported isolationLevel. Allowed values are: read_committed, read_uncommitted, repeatable_read, serializable, snapshot."),
        };
    }

    internal static string BuildFailureMessage(
        IReadOnlyList<string> statementTypes,
        int completedStatementCount,
        string failureMessage)
    {
        if (completedStatementCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedStatementCount));
        }

        if (completedStatementCount < statementTypes.Count)
        {
            string failedStatementType = statementTypes[completedStatementCount];
            return $"Transaction rolled back after statement {completedStatementCount + 1} ({failedStatementType}) failed: {failureMessage}";
        }

        return $"Transaction rolled back after commit failed: {failureMessage}";
    }
}
