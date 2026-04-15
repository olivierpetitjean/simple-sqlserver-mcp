using Microsoft.Data.SqlClient;

namespace SimpleSqlServerMcp.IntegrationTests.Infrastructure;

public sealed class IntegrationDatabaseScope : IAsyncDisposable
{
    private readonly SqlServerCollectionFixture _fixture;
    private bool _disposed;

    private IntegrationDatabaseScope(SqlServerCollectionFixture fixture, string databaseName)
    {
        _fixture = fixture;
        DatabaseName = databaseName;
    }

    public string DatabaseName { get; }

    public string ConnectionString => _fixture.BuildConnectionString(DatabaseName);

    public static async Task<IntegrationDatabaseScope> CreateAsync(
        SqlServerCollectionFixture fixture,
        string? databaseName = null,
        string? seedSql = null,
        CancellationToken cancellationToken = default)
    {
        string effectiveDatabaseName = string.IsNullOrWhiteSpace(databaseName)
            ? $"Test_{Guid.NewGuid():N}"
            : databaseName.Trim();

        IntegrationDatabaseScope scope = new(fixture, effectiveDatabaseName);
        await scope.CreateDatabaseAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(seedSql))
        {
            await scope.ExecuteAsync(seedSql, cancellationToken).ConfigureAwait(false);
        }

        return scope;
    }

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _fixture.OpenConnectionAsync(DatabaseName, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        await using SqlConnection connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        result.Should().NotBeNull();
        return (T)Convert.ChangeType(result!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await using SqlConnection connection = await _fixture.OpenMasterConnectionAsync().ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{DatabaseName.Replace("'", "''")}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private async Task CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _fixture.OpenMasterConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{DatabaseName}]";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
