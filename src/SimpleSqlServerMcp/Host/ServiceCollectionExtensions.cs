using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using SimpleSqlServerMcp.Safety;
using SimpleSqlServerMcp.Services;
using SimpleSqlServerMcp.Sql;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.Host;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerMcp(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SqlServerMcpOptions>()
            .Configure(options =>
            {
                options.Host = configuration["SQLSERVER_HOST"];
                options.Port = ParseInt(configuration["SQLSERVER_PORT"], 1433);
                options.Database = configuration["SQLSERVER_DATABASE"] ?? "master";
                options.Username = configuration["SQLSERVER_USERNAME"];
                options.Password = configuration["SQLSERVER_PASSWORD"];
                options.IntegratedSecurity = ParseBool(configuration["SQLSERVER_INTEGRATED_SECURITY"], false);
                options.Encrypt = ParseBool(configuration["SQLSERVER_ENCRYPT"], true);
                options.TrustServerCertificate = ParseBool(configuration["SQLSERVER_TRUST_SERVER_CERTIFICATE"], false);
                options.ApplicationName = configuration["SQLSERVER_APPLICATION_NAME"] ?? "SimpleSqlServerMcp";
                options.Mode = ParseMode(configuration["MCP_SQLSERVER_MODE"]);
                options.MaxRows = ParseInt(configuration["MCP_SQLSERVER_MAX_ROWS"], 100);
                options.CommandTimeoutSeconds = ParseInt(configuration["MCP_SQLSERVER_COMMAND_TIMEOUT"], 15);
                options.ExcludeSystemDatabases = ParseBool(configuration["MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES"], true);
                options.AllowedDatabases = ParseAllowedDatabases(configuration["MCP_SQLSERVER_ALLOWED_DATABASES"]);
                options.UnsafeAllowedPatterns = ParseSeparatedValues(configuration["MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS"], ';');
                options.LogLevel = configuration["MCP_SQLSERVER_LOG_LEVEL"] ?? "Information";
            })
            .Services
            .AddSingleton<IValidateOptions<SqlServerMcpOptions>, SqlServerMcpOptionsValidator>()
            .AddSingleton<IReadOnlyQueryValidator, ReadOnlyQueryValidator>()
            .AddSingleton<IMutableQueryValidator, MutableQueryValidator>()
            .AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>()
            .AddSingleton<IServerInfoService, ServerInfoService>()
            .AddSingleton<ISchemaExplorerService, SchemaExplorerService>()
            .AddSingleton<IQueryExecutionService, QueryExecutionService>()
            .AddSingleton<IMutableQueryExecutionService, MutableQueryExecutionService>()
            .AddSingleton<IStoredProcedureExecutionService, StoredProcedureExecutionService>();

        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithRequestFilters(filters =>
            {
                filters.AddCallToolFilter(next => async (request, cancellationToken) =>
                {
                    try
                    {
                        return await next(request, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        return McpToolErrorResultFactory.Create(request.Params.Name, exception);
                    }
                });
            })
            .WithToolsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    private static QueryExecutionMode ParseMode(string? rawMode)
    {
        if (string.Equals(rawMode, "mutable", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rawMode, "read-write", StringComparison.OrdinalIgnoreCase))
        {
            return QueryExecutionMode.Mutable;
        }

        return QueryExecutionMode.ReadOnly;
    }

    private static int ParseInt(string? rawValue, int defaultValue)
    {
        return int.TryParse(rawValue, out int parsedValue) ? parsedValue : defaultValue;
    }

    private static bool ParseBool(string? rawValue, bool defaultValue)
    {
        return bool.TryParse(rawValue, out bool parsedValue) ? parsedValue : defaultValue;
    }

    private static string[] ParseCsv(string? rawValue)
    {
        return ParseSeparatedValues(rawValue, ',');
    }

    private static string[] ParseAllowedDatabases(string? rawValue)
    {
        string[] databases = ParseCsv(rawValue);
        return databases.Length == 0 || databases.Contains("*", StringComparer.OrdinalIgnoreCase)
            ? ["*"]
            : databases;
    }

    private static string[] ParseSeparatedValues(string? rawValue, params char[] separators)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return [];
        }

        return rawValue
            .Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
