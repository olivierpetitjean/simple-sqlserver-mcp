using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.Safety;

internal sealed class ReadOnlyQueryValidator(IOptions<SqlServerMcpOptions> options) : IReadOnlyQueryValidator
{
    private readonly Regex[] _unsafeAllowedPatterns = options.Value.UnsafeAllowedPatterns
        .Select(static pattern => new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        .ToArray();

    public void Validate(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (MatchesUnsafePattern(sql))
        {
            return;
        }

        TSql160Parser parser = new(initialQuotedIdentifiers: true);
        using StringReader reader = new(sql);
        TSqlFragment fragment = parser.Parse(reader, out IList<ParseError> errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Query parsing failed: {errors[0].Message}");
        }

        if (fragment is not TSqlScript script)
        {
            throw new InvalidOperationException("Only T-SQL scripts are supported.");
        }

        int statementCount = script.Batches.Sum(batch => batch.Statements.Count);
        if (statementCount != 1)
        {
            throw new InvalidOperationException("Read-only mode accepts exactly one SQL statement.");
        }

        TSqlStatement statement = script.Batches.Single().Statements.Single();
        if (statement is not SelectStatement selectStatement)
        {
            throw new InvalidOperationException("Read-only mode only accepts SELECT statements.");
        }

        if (selectStatement.Into is not null)
        {
            throw new InvalidOperationException("SELECT INTO is not allowed in read-only mode.");
        }
    }

    private bool MatchesUnsafePattern(string sql)
    {
        return _unsafeAllowedPatterns.Any(pattern => pattern.IsMatch(sql));
    }
}
