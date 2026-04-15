using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.Services;

internal sealed class ServerInfoService(
    ISqlConnectionFactory connectionFactory,
    IOptions<SqlServerMcpOptions> options) : IServerInfoService
{
    private const string Query = """
        SELECT
            CAST(SERVERPROPERTY('ServerName') AS nvarchar(256)) AS ServerName,
            CAST(SUSER_SNAME() AS nvarchar(256)) AS LoginName,
            CAST(DB_NAME() AS nvarchar(256)) AS CurrentDatabase,
            CAST(SERVERPROPERTY('Edition') AS nvarchar(256)) AS Edition,
            CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(256)) AS ProductVersion
        """;

    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly SqlServerMcpOptions _options = options.Value;

    public async Task<ServerInfoResult> GetServerInfoAsync(CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(targetDatabase: null, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = Query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("SQL Server did not return server information.");
        }

        return new ServerInfoResult
        {
            ServerName = GetNullableString(reader, "ServerName") ?? "(unknown)",
            LoginName = GetNullableString(reader, "LoginName"),
            CurrentDatabase = GetNullableString(reader, "CurrentDatabase"),
            Edition = GetNullableString(reader, "Edition"),
            ProductVersion = GetNullableString(reader, "ProductVersion"),
            Mode = _options.Mode,
            MaxRows = _options.MaxRows,
            CommandTimeoutSeconds = _options.CommandTimeoutSeconds,
            ExcludeSystemDatabases = _options.ExcludeSystemDatabases,
            AllowedDatabases = _options.AllowedDatabases,
        };
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
