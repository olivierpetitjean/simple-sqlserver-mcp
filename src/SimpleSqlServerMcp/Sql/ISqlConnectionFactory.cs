using Microsoft.Data.SqlClient;

namespace SimpleSqlServerMcp.Sql;

internal interface ISqlConnectionFactory
{
    Task<SqlConnection> OpenConnectionAsync(string? targetDatabase, CancellationToken cancellationToken);
}
