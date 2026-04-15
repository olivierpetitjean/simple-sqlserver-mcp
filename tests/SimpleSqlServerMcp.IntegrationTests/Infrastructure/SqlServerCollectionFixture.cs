using Microsoft.Data.SqlClient;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;

namespace SimpleSqlServerMcp.IntegrationTests.Infrastructure;

public sealed class SqlServerCollectionFixture : IAsyncLifetime
{
    private const string BulkInsertUsersCsv = """
        Id,Name
        1,Alice
        2,Bob
        3,Charlie
        """;

    private readonly MsSqlContainer _container;

    public SqlServerCollectionFixture()
    {
        string password = $"A{Guid.NewGuid():N}a!";
        FixtureId = Guid.NewGuid();
        SeedMarker = Guid.NewGuid();
        DatabaseName = $"Integration_{Guid.NewGuid():N}";

        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(password)
            .WithResourceMapping(
                System.Text.Encoding.UTF8.GetBytes(BulkInsertUsersCsv),
                BulkInsertContainerFilePath)
            .Build();
    }

    public Guid FixtureId { get; }

    public Guid SeedMarker { get; }

    public string DatabaseName { get; }

    public string ContainerId => _container.Id;

    public string BulkInsertContainerFilePath => "/var/opt/mssql/import/bulk-users.csv";

    public string BackupDirectory => "/var/opt/mssql/backup";

    public string MasterConnectionString => _container.GetConnectionString();

    public string DatabaseConnectionString
    {
        get
        {
            return BuildConnectionString(DatabaseName);
        }
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        await EnsureBackupDirectoryAsync().ConfigureAwait(false);
        await CreateDatabaseAsync().ConfigureAwait(false);
        await SeedDatabaseAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<SqlConnection> OpenDatabaseConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqlConnection connection = new(DatabaseConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public async Task<SqlConnection> OpenMasterConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqlConnection connection = new(MasterConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public async Task<SqlConnection> OpenConnectionAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        SqlConnection connection = new(BuildConnectionString(databaseName));
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public Task<IntegrationDatabaseScope> CreateDatabaseScopeAsync(
        string? databaseName = null,
        string? seedSql = null,
        CancellationToken cancellationToken = default)
    {
        return IntegrationDatabaseScope.CreateAsync(this, databaseName, seedSql, cancellationToken);
    }

    public string BuildConnectionString(string databaseName)
    {
        SqlConnectionStringBuilder builder = new(_container.GetConnectionString())
        {
            InitialCatalog = databaseName,
        };

        return builder.ConnectionString;
    }

    public async Task EnsureBackupDirectoryAsync()
    {
        ExecResult result = await _container.ExecAsync(
            ["/bin/sh", "-c", $"mkdir -p {BackupDirectory}"]).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Could not prepare backup directory. STDERR: {result.Stderr}");
        }
    }

    public async Task<bool> FileExistsInContainerAsync(string path)
    {
        ExecResult result = await _container.ExecAsync(
            ["/bin/sh", "-c", $"test -f '{path}'"]).ConfigureAwait(false);

        return result.ExitCode == 0;
    }

    private async Task CreateDatabaseAsync()
    {
        string sql = $"CREATE DATABASE [{DatabaseName}]";

        await using SqlConnection connection = new(MasterConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private async Task SeedDatabaseAsync()
    {
        const string sql = """
            CREATE TABLE dbo.CollectionState
            (
                Id int NOT NULL PRIMARY KEY,
                FixtureId uniqueidentifier NOT NULL,
                SeedMarker uniqueidentifier NOT NULL,
                ContainerId nvarchar(256) NOT NULL
            );

            INSERT INTO dbo.CollectionState (Id, FixtureId, SeedMarker, ContainerId)
            VALUES (1, @FixtureId, @SeedMarker, @ContainerId);
            """;

        await using SqlConnection connection = new(DatabaseConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@FixtureId", FixtureId);
        command.Parameters.AddWithValue("@SeedMarker", SeedMarker);
        command.Parameters.AddWithValue("@ContainerId", ContainerId);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
