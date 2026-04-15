using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace SimpleSqlServerMcp.Host;

internal static class McpToolErrorResultFactory
{
    public static CallToolResult Create(string toolName, Exception exception)
    {
        MappedToolError mappedError = Map(exception);

        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = $"[{mappedError.Code}] {mappedError.Message}",
                },
            ],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                error = new
                {
                    code = mappedError.Code,
                    message = mappedError.Message,
                    tool = toolName,
                    retryable = mappedError.Retryable,
                },
            }),
        };
    }

    private static MappedToolError Map(Exception exception)
    {
        return exception switch
        {
            OptionsValidationException validationException => new(
                "CONFIG_INVALID",
                JoinMessages(validationException.Failures) ?? Normalize(validationException.Message),
                Retryable: false),

            ArgumentException argumentException => new(
                "INVALID_ARGUMENT",
                Normalize(argumentException.Message),
                Retryable: false),

            InvalidOperationException invalidOperationException => new(
                "INVALID_OPERATION",
                Normalize(invalidOperationException.Message),
                Retryable: false),

            SqlException sqlException when sqlException.Number == -2 => new(
                "SQL_TIMEOUT",
                "The SQL Server operation timed out.",
                Retryable: true),

            SqlException sqlException when sqlException.Number == 18456 => new(
                "SQL_AUTH_FAILED",
                "SQL Server authentication failed. Check SQLSERVER_USERNAME and SQLSERVER_PASSWORD.",
                Retryable: false),

            SqlException sqlException when sqlException.Number == 4060 => new(
                "SQL_DATABASE_UNAVAILABLE",
                TryCreateDatabaseUnavailableMessage(sqlException.Message),
                Retryable: false),

            SqlException sqlException => new(
                "SQL_OPERATION_FAILED",
                Normalize(sqlException.Message),
                Retryable: false),

            OperationCanceledException => new(
                "REQUEST_CANCELLED",
                "The request was cancelled before the SQL Server operation completed.",
                Retryable: true),

            _ => new(
                "INTERNAL_ERROR",
                Normalize(exception.Message),
                Retryable: false),
        };
    }

    private static string? JoinMessages(IEnumerable<string>? messages)
    {
        if (messages is null)
        {
            return null;
        }

        string combined = string.Join(" ", messages.Where(static message => !string.IsNullOrWhiteSpace(message)));
        return string.IsNullOrWhiteSpace(combined) ? null : Normalize(combined);
    }

    private static string Normalize(string message)
    {
        return string.Join(" ", message.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string TryCreateDatabaseUnavailableMessage(string sqlExceptionMessage)
    {
        string normalized = Normalize(sqlExceptionMessage);
        Match match = Regex.Match(
            normalized,
            "Cannot open database ['\"](?<database>[^'\"]+)['\"] requested by the login",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (match.Success)
        {
            return $"Database '{match.Groups["database"].Value}' could not be opened.";
        }

        return normalized;
    }

    private sealed record MappedToolError(string Code, string Message, bool Retryable);
}
