using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.Sql;

internal sealed class SqlConnectionFactory(IOptions<SqlServerMcpOptions> options) : ISqlConnectionFactory
{
    private readonly SqlServerMcpOptions _options = options.Value;

    public async Task<SqlConnection> OpenConnectionAsync(string? targetDatabase, CancellationToken cancellationToken)
    {
        string effectiveDatabase = ResolveDatabase(targetDatabase);
        SqlConnection connection = new(BuildConnectionString(_options, effectiveDatabase));
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private string ResolveDatabase(string? targetDatabase)
    {
        if (string.IsNullOrWhiteSpace(targetDatabase))
        {
            return _options.Database;
        }

        string effectiveDatabase = targetDatabase.Trim();
        DatabaseAccessPolicy.EnsureAllowed(_options, effectiveDatabase);
        return effectiveDatabase;
    }

    private static string BuildConnectionString(SqlServerMcpOptions options, string initialCatalog)
    {
        SqlConnectionStringBuilder builder = new()
        {
            DataSource = $"{options.Host},{options.Port}",
            InitialCatalog = initialCatalog,
            IntegratedSecurity = options.IntegratedSecurity,
            Encrypt = options.Encrypt,
            TrustServerCertificate = options.TrustServerCertificate,
            ApplicationName = options.ApplicationName,
        };

        if (!options.IntegratedSecurity)
        {
            builder.UserID = options.Username;
            builder.Password = options.Password;
        }

        return builder.ConnectionString;
    }
}
