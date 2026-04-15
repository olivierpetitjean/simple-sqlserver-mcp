using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.Services;

internal sealed class SchemaExplorerService(
    ISqlConnectionFactory connectionFactory,
    IOptions<SqlServerMcpOptions> options) : ISchemaExplorerService
{
    private const string Query = """
        SELECT
            d.name AS DatabaseName,
            d.state_desc AS StateDescription,
            d.compatibility_level AS CompatibilityLevel
        FROM sys.databases AS d
        WHERE HAS_DBACCESS(d.name) = 1
        ORDER BY d.name
        """;

    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly SqlServerMcpOptions _options = options.Value;

    public async Task<ListDatabasesResult> ListDatabasesAsync(string? search, int? limit, CancellationToken cancellationToken)
    {
        int effectiveLimit = NormalizeLimit(limit, _options.MaxRows);
        List<DatabaseInfo> databases = [];

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(targetDatabase: null, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = Query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            DatabaseInfo database = new()
            {
                Name = reader.GetString(reader.GetOrdinal("DatabaseName")),
                State = GetNullableString(reader, "StateDescription"),
                CompatibilityLevel = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("CompatibilityLevel")), System.Globalization.CultureInfo.InvariantCulture),
            };

            if (!IsAllowed(database, search))
            {
                continue;
            }

            databases.Add(database);

            if (databases.Count >= effectiveLimit)
            {
                break;
            }
        }

        return new ListDatabasesResult
        {
            Count = databases.Count,
            Items = databases,
        };
    }

    public async Task<ListTablesResult> ListTablesAsync(
        string database,
        string? schema,
        string? search,
        int? limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        EnsureDatabaseAllowed(database);

        int effectiveLimit = NormalizeLimit(limit, _options.MaxRows);
        string quotedDatabase = SqlIdentifier.Quote(database);
        string query = $"""
            SELECT TOP (@Limit)
                @Database AS DatabaseName,
                s.name AS SchemaName,
                t.name AS TableName
            FROM {quotedDatabase}.sys.tables AS t
            INNER JOIN {quotedDatabase}.sys.schemas AS s
                ON s.schema_id = t.schema_id
            WHERE (@Schema IS NULL OR s.name = @Schema)
              AND (@Search IS NULL OR t.name LIKE '%' + @Search + '%')
            ORDER BY s.name, t.name
            """;

        List<TableInfo> tables = [];

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(targetDatabase: null, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@Limit", effectiveLimit);
        command.Parameters.AddWithValue("@Database", database);
        command.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);
        command.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            tables.Add(new TableInfo
            {
                Database = reader.GetString(reader.GetOrdinal("DatabaseName")),
                Schema = reader.GetString(reader.GetOrdinal("SchemaName")),
                Name = reader.GetString(reader.GetOrdinal("TableName")),
            });
        }

        return new ListTablesResult
        {
            Database = database,
            Count = tables.Count,
            Items = tables,
        };
    }

    public async Task<TableDescriptionResult> DescribeTableAsync(
        string database,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        EnsureDatabaseAllowed(database);

        string quotedDatabase = SqlIdentifier.Quote(database);
        string columnsQuery = $"""
            SELECT
                c.name AS ColumnName,
                ty.name AS DataType,
                c.is_nullable AS IsNullable,
                c.is_identity AS IsIdentity
            FROM {quotedDatabase}.sys.tables AS t
            INNER JOIN {quotedDatabase}.sys.schemas AS s
                ON s.schema_id = t.schema_id
            INNER JOIN {quotedDatabase}.sys.columns AS c
                ON c.object_id = t.object_id
            INNER JOIN {quotedDatabase}.sys.types AS ty
                ON ty.user_type_id = c.user_type_id
            WHERE s.name = @Schema
              AND t.name = @Table
            ORDER BY c.column_id
            """;
        string primaryKeyQuery = $"""
            SELECT
                c.name AS ColumnName
            FROM {quotedDatabase}.sys.tables AS t
            INNER JOIN {quotedDatabase}.sys.schemas AS s
                ON s.schema_id = t.schema_id
            INNER JOIN {quotedDatabase}.sys.key_constraints AS kc
                ON kc.parent_object_id = t.object_id
               AND kc.type = 'PK'
            INNER JOIN {quotedDatabase}.sys.index_columns AS ic
                ON ic.object_id = kc.parent_object_id
               AND ic.index_id = kc.unique_index_id
            INNER JOIN {quotedDatabase}.sys.columns AS c
                ON c.object_id = ic.object_id
               AND c.column_id = ic.column_id
            WHERE s.name = @Schema
              AND t.name = @Table
            ORDER BY ic.key_ordinal
            """;
        string foreignKeysQuery = $"""
            SELECT
                fk.name AS ForeignKeyName,
                sourceColumn.name AS SourceColumnName,
                referencedSchema.name AS ReferencedSchemaName,
                referencedTable.name AS ReferencedTableName,
                referencedColumn.name AS ReferencedColumnName
            FROM {quotedDatabase}.sys.tables AS sourceTable
            INNER JOIN {quotedDatabase}.sys.schemas AS sourceSchema
                ON sourceSchema.schema_id = sourceTable.schema_id
            INNER JOIN {quotedDatabase}.sys.foreign_keys AS fk
                ON fk.parent_object_id = sourceTable.object_id
            INNER JOIN {quotedDatabase}.sys.foreign_key_columns AS fkc
                ON fkc.constraint_object_id = fk.object_id
            INNER JOIN {quotedDatabase}.sys.columns AS sourceColumn
                ON sourceColumn.object_id = fkc.parent_object_id
               AND sourceColumn.column_id = fkc.parent_column_id
            INNER JOIN {quotedDatabase}.sys.tables AS referencedTable
                ON referencedTable.object_id = fkc.referenced_object_id
            INNER JOIN {quotedDatabase}.sys.schemas AS referencedSchema
                ON referencedSchema.schema_id = referencedTable.schema_id
            INNER JOIN {quotedDatabase}.sys.columns AS referencedColumn
                ON referencedColumn.object_id = fkc.referenced_object_id
               AND referencedColumn.column_id = fkc.referenced_column_id
            WHERE sourceSchema.name = @Schema
              AND sourceTable.name = @Table
            ORDER BY fk.name, fkc.constraint_column_id
            """;

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(targetDatabase: null, cancellationToken).ConfigureAwait(false);

        List<TableColumnInfo> columns = await ReadColumnsAsync(connection, columnsQuery, schema, table, cancellationToken).ConfigureAwait(false);
        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"Table '{database}.{schema}.{table}' was not found.");
        }

        List<string> primaryKeyColumns = await ReadPrimaryKeyColumnsAsync(connection, primaryKeyQuery, schema, table, cancellationToken).ConfigureAwait(false);
        List<ForeignKeyInfo> foreignKeys = await ReadForeignKeysAsync(connection, foreignKeysQuery, schema, table, cancellationToken).ConfigureAwait(false);

        return new TableDescriptionResult
        {
            Database = database,
            Schema = schema,
            Table = table,
            Columns = columns,
            PrimaryKeyColumns = primaryKeyColumns,
            ForeignKeys = foreignKeys,
        };
    }

    public async Task<SearchColumnsResult> SearchColumnsAsync(
        string database,
        string search,
        string? schema,
        int? limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(search);

        EnsureDatabaseAllowed(database);

        int effectiveLimit = NormalizeLimit(limit, _options.MaxRows);
        string quotedDatabase = SqlIdentifier.Quote(database);
        string query = $"""
            SELECT TOP (@Limit)
                @Database AS DatabaseName,
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName,
                ty.name AS DataType
            FROM {quotedDatabase}.sys.tables AS t
            INNER JOIN {quotedDatabase}.sys.schemas AS s
                ON s.schema_id = t.schema_id
            INNER JOIN {quotedDatabase}.sys.columns AS c
                ON c.object_id = t.object_id
            INNER JOIN {quotedDatabase}.sys.types AS ty
                ON ty.user_type_id = c.user_type_id
            WHERE (@Schema IS NULL OR s.name = @Schema)
              AND c.name LIKE '%' + @Search + '%'
            ORDER BY s.name, t.name, c.column_id
            """;

        List<ColumnSearchResultItem> items = [];

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(targetDatabase: null, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@Limit", effectiveLimit);
        command.Parameters.AddWithValue("@Database", database);
        command.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);
        command.Parameters.AddWithValue("@Search", search);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(new ColumnSearchResultItem
            {
                Database = reader.GetString(reader.GetOrdinal("DatabaseName")),
                Schema = reader.GetString(reader.GetOrdinal("SchemaName")),
                Table = reader.GetString(reader.GetOrdinal("TableName")),
                Column = reader.GetString(reader.GetOrdinal("ColumnName")),
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
            });
        }

        return new SearchColumnsResult
        {
            Database = database,
            SchemaFilter = schema,
            Search = search,
            Count = items.Count,
            Items = items,
        };
    }

    public async Task<TabularQueryResult> GetTableSampleAsync(
        string database,
        string schema,
        string table,
        int? limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        EnsureDatabaseAllowed(database);

        int effectiveLimit = NormalizeLimit(limit, _options.MaxRows);
        string quotedDatabase = SqlIdentifier.Quote(database);
        string quotedSchema = SqlIdentifier.Quote(schema);
        string quotedTable = SqlIdentifier.Quote(table);
        string query = $"""
            SELECT TOP ({effectiveLimit + 1}) *
            FROM {quotedDatabase}.{quotedSchema}.{quotedTable}
            """;

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(targetDatabase: null, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await ReadTabularResultAsync(reader, effectiveLimit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ListStoredProceduresResult> ListStoredProceduresAsync(
        string database,
        string? schema,
        string? search,
        int? limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        EnsureDatabaseAllowed(database);

        const string query = """
            SELECT TOP (@Limit)
                DB_NAME() AS DatabaseName,
                s.name AS SchemaName,
                p.name AS ProcedureName,
                p.create_date AS CreatedAtUtc,
                p.modify_date AS ModifiedAtUtc
            FROM sys.procedures AS p
            INNER JOIN sys.schemas AS s
                ON s.schema_id = p.schema_id
            WHERE (@Schema IS NULL OR s.name = @Schema)
              AND (@Search IS NULL OR p.name LIKE '%' + @Search + '%')
            ORDER BY s.name, p.name
            """;

        int effectiveLimit = NormalizeLimit(limit, _options.MaxRows);
        List<StoredProcedureInfo> procedures = [];

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(database, cancellationToken).ConfigureAwait(false);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@Limit", effectiveLimit);
        command.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);
        command.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            procedures.Add(new StoredProcedureInfo
            {
                Database = reader.GetString(reader.GetOrdinal("DatabaseName")),
                Schema = reader.GetString(reader.GetOrdinal("SchemaName")),
                Name = reader.GetString(reader.GetOrdinal("ProcedureName")),
                CreatedAtUtc = GetNullableDateTime(reader, "CreatedAtUtc"),
                ModifiedAtUtc = GetNullableDateTime(reader, "ModifiedAtUtc"),
            });
        }

        return new ListStoredProceduresResult
        {
            Database = database,
            SchemaFilter = schema,
            Search = search,
            Count = procedures.Count,
            Items = procedures,
        };
    }

    public async Task<StoredProcedureDescriptionResult> DescribeStoredProcedureAsync(
        string database,
        string schema,
        string procedure,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(procedure);

        EnsureDatabaseAllowed(database);

        const string procedureQuery = """
            SELECT
                p.object_id AS ObjectId,
                p.create_date AS CreatedAtUtc,
                p.modify_date AS ModifiedAtUtc,
                m.definition AS Definition
            FROM sys.procedures AS p
            INNER JOIN sys.schemas AS s
                ON s.schema_id = p.schema_id
            LEFT JOIN sys.sql_modules AS m
                ON m.object_id = p.object_id
            WHERE s.name = @Schema
              AND p.name = @Procedure
            """;
        const string parametersQuery = """
            SELECT
                p.name AS ParameterName,
                CASE
                    WHEN ty.name IN ('nvarchar', 'nchar') AND p.max_length > 0 THEN ty.name + '(' + CAST(p.max_length / 2 AS nvarchar(10)) + ')'
                    WHEN ty.name IN ('varchar', 'char', 'varbinary', 'binary') AND p.max_length > 0 THEN ty.name + '(' + CAST(p.max_length AS nvarchar(10)) + ')'
                    WHEN ty.name IN ('nvarchar', 'varchar', 'varbinary') AND p.max_length = -1 THEN ty.name + '(max)'
                    WHEN ty.name IN ('decimal', 'numeric') THEN ty.name + '(' + CAST(p.precision AS nvarchar(10)) + ',' + CAST(p.scale AS nvarchar(10)) + ')'
                    WHEN ty.name IN ('datetime2', 'datetimeoffset', 'time') THEN ty.name + '(' + CAST(p.scale AS nvarchar(10)) + ')'
                    ELSE ty.name
                END AS DataType,
                p.is_output AS IsOutput,
                p.has_default_value AS HasDefaultValue,
                p.parameter_id AS Ordinal
            FROM sys.parameters AS p
            INNER JOIN sys.types AS ty
                ON ty.user_type_id = p.user_type_id
            WHERE p.object_id = @ObjectId
            ORDER BY p.parameter_id
            """;
        const string resultSetQuery = """
            SELECT
                column_ordinal AS Ordinal,
                name AS ColumnName,
                system_type_name AS DataType,
                is_nullable AS IsNullable,
                error_type AS ErrorType
            FROM sys.dm_exec_describe_first_result_set_for_object(@ObjectId, NULL)
            WHERE error_type IS NULL
            ORDER BY column_ordinal
            """;

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(database, cancellationToken).ConfigureAwait(false);

        (int objectId, DateTime? createdAtUtc, DateTime? modifiedAtUtc, string? definition) = await ReadStoredProcedureHeaderAsync(
            connection,
            procedureQuery,
            schema,
            procedure,
            cancellationToken).ConfigureAwait(false);

        List<StoredProcedureParameterInfo> parameters = await ReadStoredProcedureParametersAsync(
            connection,
            parametersQuery,
            objectId,
            cancellationToken).ConfigureAwait(false);

        List<StoredProcedureResultSetColumnInfo> firstResultSet = await ReadStoredProcedureResultSetAsync(
            connection,
            resultSetQuery,
            objectId,
            cancellationToken).ConfigureAwait(false);

        return new StoredProcedureDescriptionResult
        {
            Database = database,
            Schema = schema,
            Procedure = procedure,
            CreatedAtUtc = createdAtUtc,
            ModifiedAtUtc = modifiedAtUtc,
            Definition = definition,
            Parameters = parameters,
            FirstResultSet = firstResultSet,
        };
    }

    private bool IsAllowed(DatabaseInfo database, string? search)
    {
        if (!DatabaseAccessPolicy.IsAllowed(_options, database.Name))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(search) &&
            database.Name.Contains(search, StringComparison.OrdinalIgnoreCase) is false)
        {
            return false;
        }

        return true;
    }

    private void EnsureDatabaseAllowed(string database)
    {
        DatabaseAccessPolicy.EnsureAllowed(_options, database);
    }

    private async Task<List<TableColumnInfo>> ReadColumnsAsync(
        SqlConnection connection,
        string query,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        List<TableColumnInfo> columns = [];

        await using SqlCommand command = CreateSchemaCommand(connection, query, schema, table);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            columns.Add(new TableColumnInfo
            {
                Name = reader.GetString(reader.GetOrdinal("ColumnName")),
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
                IsNullable = reader.GetBoolean(reader.GetOrdinal("IsNullable")),
                IsIdentity = reader.GetBoolean(reader.GetOrdinal("IsIdentity")),
            });
        }

        return columns;
    }

    private async Task<List<string>> ReadPrimaryKeyColumnsAsync(
        SqlConnection connection,
        string query,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        List<string> primaryKeyColumns = [];

        await using SqlCommand command = CreateSchemaCommand(connection, query, schema, table);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            primaryKeyColumns.Add(reader.GetString(reader.GetOrdinal("ColumnName")));
        }

        return primaryKeyColumns;
    }

    private async Task<List<ForeignKeyInfo>> ReadForeignKeysAsync(
        SqlConnection connection,
        string query,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        List<ForeignKeyInfo> foreignKeys = [];

        await using SqlCommand command = CreateSchemaCommand(connection, query, schema, table);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                Name = reader.GetString(reader.GetOrdinal("ForeignKeyName")),
                SourceColumn = reader.GetString(reader.GetOrdinal("SourceColumnName")),
                ReferencedSchema = reader.GetString(reader.GetOrdinal("ReferencedSchemaName")),
                ReferencedTable = reader.GetString(reader.GetOrdinal("ReferencedTableName")),
                ReferencedColumn = reader.GetString(reader.GetOrdinal("ReferencedColumnName")),
            });
        }

        return foreignKeys;
    }

    private SqlCommand CreateSchemaCommand(SqlConnection connection, string query, string schema, string table)
    {
        SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@Table", table);
        return command;
    }

    private async Task<(int ObjectId, DateTime? CreatedAtUtc, DateTime? ModifiedAtUtc, string? Definition)> ReadStoredProcedureHeaderAsync(
        SqlConnection connection,
        string query,
        string schema,
        string procedure,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@Procedure", procedure);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Stored procedure '{connection.Database}.{schema}.{procedure}' was not found.");
        }

        return (
            reader.GetInt32(reader.GetOrdinal("ObjectId")),
            GetNullableDateTime(reader, "CreatedAtUtc"),
            GetNullableDateTime(reader, "ModifiedAtUtc"),
            GetNullableString(reader, "Definition"));
    }

    private async Task<List<StoredProcedureParameterInfo>> ReadStoredProcedureParametersAsync(
        SqlConnection connection,
        string query,
        int objectId,
        CancellationToken cancellationToken)
    {
        List<StoredProcedureParameterInfo> parameters = [];

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@ObjectId", objectId);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            parameters.Add(new StoredProcedureParameterInfo
            {
                Name = reader.GetString(reader.GetOrdinal("ParameterName")),
                DataType = reader.GetString(reader.GetOrdinal("DataType")),
                IsOutput = reader.GetBoolean(reader.GetOrdinal("IsOutput")),
                HasDefaultValue = reader.GetBoolean(reader.GetOrdinal("HasDefaultValue")),
                Ordinal = reader.GetInt32(reader.GetOrdinal("Ordinal")),
            });
        }

        return parameters;
    }

    private async Task<List<StoredProcedureResultSetColumnInfo>> ReadStoredProcedureResultSetAsync(
        SqlConnection connection,
        string query,
        int objectId,
        CancellationToken cancellationToken)
    {
        List<StoredProcedureResultSetColumnInfo> columns = [];

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        command.Parameters.AddWithValue("@ObjectId", objectId);

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            columns.Add(new StoredProcedureResultSetColumnInfo
            {
                Ordinal = reader.GetInt32(reader.GetOrdinal("Ordinal")),
                Name = GetNullableString(reader, "ColumnName"),
                DataType = GetNullableString(reader, "DataType"),
                IsNullable = GetNullableBoolean(reader, "IsNullable"),
            });
        }

        return columns;
    }

    private static async Task<TabularQueryResult> ReadTabularResultAsync(
        SqlDataReader reader,
        int effectiveLimit,
        CancellationToken cancellationToken)
    {
        List<string> columns = [];
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        List<IReadOnlyList<object?>> rows = [];
        bool truncated = false;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (rows.Count >= effectiveLimit)
            {
                truncated = true;
                break;
            }

            object?[] values = new object?[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        return new TabularQueryResult
        {
            Columns = columns,
            Rows = rows,
            RowCount = rows.Count,
            Truncated = truncated,
        };
    }

    private static int NormalizeLimit(int? requestedLimit, int configuredMaxRows)
    {
        if (requestedLimit is null or <= 0)
        {
            return configuredMaxRows;
        }

        return Math.Min(requestedLimit.Value, configuredMaxRows);
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static bool? GetNullableBoolean(SqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }
}
