using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace SimpleSqlServerMcp.Configuration;

internal sealed class SqlServerMcpOptionsValidator : IValidateOptions<SqlServerMcpOptions>
{
    public ValidateOptionsResult Validate(string? name, SqlServerMcpOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            failures.Add("SQLSERVER_HOST must be configured.");
        }

        if (options.Port <= 0)
        {
            failures.Add("SQLSERVER_PORT must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(options.Database))
        {
            failures.Add("SQLSERVER_DATABASE must be configured.");
        }

        if (!options.IntegratedSecurity)
        {
            if (string.IsNullOrWhiteSpace(options.Username))
            {
                failures.Add("SQLSERVER_USERNAME must be configured when integrated security is disabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                failures.Add("SQLSERVER_PASSWORD must be configured when integrated security is disabled.");
            }
        }

        if (options.MaxRows <= 0)
        {
            failures.Add("MCP_SQLSERVER_MAX_ROWS must be greater than 0.");
        }

        if (options.CommandTimeoutSeconds <= 0)
        {
            failures.Add("MCP_SQLSERVER_COMMAND_TIMEOUT must be greater than 0.");
        }

        foreach (string pattern in options.UnsafeAllowedPatterns)
        {
            try
            {
                _ = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            }
            catch (ArgumentException exception)
            {
                failures.Add($"MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS contains an invalid regex '{pattern}': {exception.Message}");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
