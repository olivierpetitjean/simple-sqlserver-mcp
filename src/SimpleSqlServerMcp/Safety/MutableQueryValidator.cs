using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.Safety;

internal sealed class MutableQueryValidator(IOptions<SqlServerMcpOptions> options) : IMutableQueryValidator
{
    private readonly Regex[] _unsafeAllowedPatterns = options.Value.UnsafeAllowedPatterns
        .Select(static pattern => new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        .ToArray();

    public string Validate(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (MatchesUnsafePattern(sql))
        {
            return "UNSAFE PATTERN";
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
            throw new InvalidOperationException("Mutable mode accepts exactly one SQL statement.");
        }

        TSqlStatement statement = script.Batches.Single().Statements.Single();

        return statement switch
        {
            SelectStatement selectStatement when selectStatement.Into is not null => "SELECT INTO",
            InsertStatement => "INSERT",
            UpdateStatement => "UPDATE",
            DeleteStatement => "DELETE",
            MergeStatement => "MERGE",
            CreateDatabaseStatement => "CREATE DATABASE",
            AlterDatabaseStatement => "ALTER DATABASE",
            BackupStatement => "BACKUP",
            BulkInsertStatement => "BULK INSERT",
            CreateTableStatement => "CREATE TABLE",
            AlterTableStatement => "ALTER TABLE",
            DropTableStatement => "DROP TABLE",
            DropDatabaseStatement => "DROP DATABASE",
            TruncateTableStatement => "TRUNCATE TABLE",
            CreateProcedureStatement => "CREATE PROCEDURE",
            AlterProcedureStatement => "ALTER PROCEDURE",
            CreateOrAlterProcedureStatement => "CREATE OR ALTER PROCEDURE",
            DropProcedureStatement => "DROP PROCEDURE",
            CreateFunctionStatement => "CREATE FUNCTION",
            AlterFunctionStatement => "ALTER FUNCTION",
            CreateOrAlterFunctionStatement => "CREATE OR ALTER FUNCTION",
            DropFunctionStatement => "DROP FUNCTION",
            CreateViewStatement => "CREATE VIEW",
            AlterViewStatement => "ALTER VIEW",
            CreateOrAlterViewStatement => "CREATE OR ALTER VIEW",
            DropViewStatement => "DROP VIEW",
            CreateIndexStatement => "CREATE INDEX",
            AlterIndexStatement => "ALTER INDEX",
            DropIndexStatement => "DROP INDEX",
            CreateSchemaStatement => "CREATE SCHEMA",
            AlterSchemaStatement => "ALTER SCHEMA",
            DropSchemaStatement => "DROP SCHEMA",
            CreateSequenceStatement => "CREATE SEQUENCE",
            AlterSequenceStatement => "ALTER SEQUENCE",
            DropSequenceStatement => "DROP SEQUENCE",
            CreateTypeStatement => "CREATE TYPE",
            DropTypeStatement => "DROP TYPE",
            CreateSynonymStatement => "CREATE SYNONYM",
            DropSynonymStatement => "DROP SYNONYM",
            CreateTriggerStatement => "CREATE TRIGGER",
            AlterTriggerStatement => "ALTER TRIGGER",
            CreateOrAlterTriggerStatement => "CREATE OR ALTER TRIGGER",
            DropTriggerStatement => "DROP TRIGGER",
            _ => throw new InvalidOperationException(
                "Mutable mode only supports explicitly whitelisted single statements. EXEC and stored procedure execution are not supported yet. You can override this with MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS if you explicitly accept the risk."),
        };
    }

    private bool MatchesUnsafePattern(string sql)
    {
        return _unsafeAllowedPatterns.Any(pattern => pattern.IsMatch(sql));
    }
}
