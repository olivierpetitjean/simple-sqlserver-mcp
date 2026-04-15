using System.Diagnostics;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.Services;

internal sealed class StoredProcedureExecutionService(
    ISqlConnectionFactory connectionFactory,
    IOptions<SqlServerMcpOptions> options) : IStoredProcedureExecutionService
{
    private readonly ISqlConnectionFactory _connectionFactory = connectionFactory;
    private readonly SqlServerMcpOptions _options = options.Value;

    public async Task<ExecutedStoredProcedureResult> ExecuteStoredProcedureAsync(
        string database,
        string schema,
        string procedure,
        IReadOnlyDictionary<string, JsonElement>? parameters,
        string? targetDatabase,
        int? maxRows,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(procedure);

        if (_options.Mode != QueryExecutionMode.Mutable)
        {
            throw new InvalidOperationException(
                "execute_stored_procedure is disabled because MCP_SQLSERVER_MODE is not set to mutable.");
        }

        string effectiveDatabase = string.IsNullOrWhiteSpace(targetDatabase) ? database : targetDatabase;
        DatabaseAccessPolicy.EnsureAllowed(_options, effectiveDatabase);
        int effectiveMaxRows = NormalizeRequestedValue(maxRows, _options.MaxRows);
        int effectiveTimeoutSeconds = NormalizeRequestedValue(timeoutSeconds, _options.CommandTimeoutSeconds);

        await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(effectiveDatabase, cancellationToken).ConfigureAwait(false);
        StoredProcedureMetadata metadata = await ReadStoredProcedureMetadataAsync(connection, schema, procedure, cancellationToken).ConfigureAwait(false);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = procedure;
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.CommandTimeout = effectiveTimeoutSeconds;

        foreach (StoredProcedureParameterMetadata parameter in metadata.Parameters)
        {
            SqlParameter sqlParameter = command.CreateParameter();
            sqlParameter.ParameterName = parameter.Name;
            sqlParameter.SqlDbType = parameter.SqlDbType;
            sqlParameter.Direction = parameter.IsOutput
                ? ParameterDirection.InputOutput
                : ParameterDirection.Input;

            if (parameter.Size is > 0)
            {
                sqlParameter.Size = parameter.Size.Value;
            }

            if (parameter.Precision is > 0)
            {
                sqlParameter.Precision = parameter.Precision.Value;
            }

            if (parameter.Scale is > 0)
            {
                sqlParameter.Scale = parameter.Scale.Value;
            }

            if (TryGetProvidedValue(parameters, parameter.Name, out JsonElement value))
            {
                sqlParameter.Value = ConvertJsonElement(value);
            }
            else
            {
                sqlParameter.Value = DBNull.Value;

                if (parameter.IsOutput)
                {
                    sqlParameter.Direction = ParameterDirection.Output;
                }
            }

            command.Parameters.Add(sqlParameter);
        }

        SqlParameter returnValueParameter = command.CreateParameter();
        returnValueParameter.ParameterName = "@RETURN_VALUE";
        returnValueParameter.Direction = ParameterDirection.ReturnValue;
        command.Parameters.Add(returnValueParameter);

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<TabularQueryResult> resultSets = [];

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        do
        {
            TabularQueryResult resultSet = await ReadTabularResultAsync(reader, effectiveMaxRows, cancellationToken).ConfigureAwait(false);
            if (resultSet.Columns.Count > 0 || resultSet.Rows.Count > 0)
            {
                resultSets.Add(resultSet);
            }
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

        stopwatch.Stop();

        Dictionary<string, object?> outputParameters = command.Parameters
            .OfType<SqlParameter>()
            .Where(static parameter => parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
            .ToDictionary(
                static parameter => parameter.ParameterName,
                static parameter => parameter.Value is DBNull ? null : parameter.Value,
                StringComparer.OrdinalIgnoreCase);

        return new ExecutedStoredProcedureResult
        {
            Database = effectiveDatabase,
            Schema = schema,
            Procedure = procedure,
            ResultSets = resultSets,
            OutputParameters = outputParameters,
            ReturnValue = returnValueParameter.Value is int intValue ? intValue : Convert.ToInt32(returnValueParameter.Value, System.Globalization.CultureInfo.InvariantCulture),
            RowsAffected = reader.RecordsAffected,
            DurationMilliseconds = stopwatch.ElapsedMilliseconds,
        };
    }

    private async Task<StoredProcedureMetadata> ReadStoredProcedureMetadataAsync(
        SqlConnection connection,
        string schema,
        string procedure,
        CancellationToken cancellationToken)
    {
        const string procedureQuery = """
            SELECT p.object_id AS ObjectId
            FROM sys.procedures AS p
            INNER JOIN sys.schemas AS s
                ON s.schema_id = p.schema_id
            WHERE s.name = @Schema
              AND p.name = @Procedure
            """;
        const string parametersQuery = """
            SELECT
                p.name AS ParameterName,
                p.is_output AS IsOutput,
                ty.name AS TypeName,
                p.max_length AS MaxLength,
                p.precision AS PrecisionValue,
                p.scale AS ScaleValue
            FROM sys.parameters AS p
            INNER JOIN sys.types AS ty
                ON ty.user_type_id = p.user_type_id
            WHERE p.object_id = @ObjectId
            ORDER BY p.parameter_id
            """;

        await using SqlCommand procedureCommand = connection.CreateCommand();
        procedureCommand.CommandText = procedureQuery;
        procedureCommand.CommandTimeout = _options.CommandTimeoutSeconds;
        procedureCommand.Parameters.AddWithValue("@Schema", schema);
        procedureCommand.Parameters.AddWithValue("@Procedure", procedure);

        object? objectIdValue = await procedureCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (objectIdValue is null or DBNull)
        {
            throw new InvalidOperationException($"Stored procedure '{connection.Database}.{schema}.{procedure}' was not found.");
        }

        int objectId = Convert.ToInt32(objectIdValue, System.Globalization.CultureInfo.InvariantCulture);
        List<StoredProcedureParameterMetadata> parameters = [];

        await using SqlCommand parametersCommand = connection.CreateCommand();
        parametersCommand.CommandText = parametersQuery;
        parametersCommand.CommandTimeout = _options.CommandTimeoutSeconds;
        parametersCommand.Parameters.AddWithValue("@ObjectId", objectId);

        await using SqlDataReader reader = await parametersCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            string typeName = reader.GetString(reader.GetOrdinal("TypeName"));
            parameters.Add(new StoredProcedureParameterMetadata(
                reader.GetString(reader.GetOrdinal("ParameterName")),
                reader.GetBoolean(reader.GetOrdinal("IsOutput")),
                MapSqlDbType(typeName),
                NormalizeSize(typeName, reader.GetInt16(reader.GetOrdinal("MaxLength"))),
                NormalizePrecision(reader.GetByte(reader.GetOrdinal("PrecisionValue"))),
                NormalizeScale(reader.GetByte(reader.GetOrdinal("ScaleValue")))));
        }

        return new StoredProcedureMetadata(objectId, parameters);
    }

    private static bool TryGetProvidedValue(
        IReadOnlyDictionary<string, JsonElement>? parameters,
        string parameterName,
        out JsonElement value)
    {
        value = default;
        if (parameters is null)
        {
            return false;
        }

        if (parameters.TryGetValue(parameterName, out value))
        {
            return true;
        }

        string normalizedName = parameterName.TrimStart('@');
        return parameters.TryGetValue(normalizedName, out value);
    }

    private static object? ConvertJsonElement(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => DBNull.Value,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out int intValue) => intValue,
            JsonValueKind.Number when value.TryGetInt64(out long longValue) => longValue,
            JsonValueKind.Number when value.TryGetDecimal(out decimal decimalValue) => decimalValue,
            JsonValueKind.Number => value.GetDouble(),
            _ => value.ToString(),
        };
    }

    private static int? NormalizeSize(string typeName, short maxLength)
    {
        return typeName switch
        {
            "nvarchar" or "nchar" when maxLength > 0 => maxLength / 2,
            "varchar" or "char" or "varbinary" or "binary" when maxLength > 0 => maxLength,
            "nvarchar" or "varchar" or "varbinary" when maxLength == -1 => -1,
            _ => null,
        };
    }

    private static byte? NormalizePrecision(byte precision)
    {
        return precision == 0 ? null : precision;
    }

    private static byte? NormalizeScale(byte scale)
    {
        return scale == 0 ? null : scale;
    }

    private static SqlDbType MapSqlDbType(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "bigint" => SqlDbType.BigInt,
            "binary" => SqlDbType.Binary,
            "bit" => SqlDbType.Bit,
            "char" => SqlDbType.Char,
            "date" => SqlDbType.Date,
            "datetime" => SqlDbType.DateTime,
            "datetime2" => SqlDbType.DateTime2,
            "datetimeoffset" => SqlDbType.DateTimeOffset,
            "decimal" => SqlDbType.Decimal,
            "float" => SqlDbType.Float,
            "image" => SqlDbType.Image,
            "int" => SqlDbType.Int,
            "money" => SqlDbType.Money,
            "nchar" => SqlDbType.NChar,
            "ntext" => SqlDbType.NText,
            "numeric" => SqlDbType.Decimal,
            "nvarchar" => SqlDbType.NVarChar,
            "real" => SqlDbType.Real,
            "smalldatetime" => SqlDbType.SmallDateTime,
            "smallint" => SqlDbType.SmallInt,
            "smallmoney" => SqlDbType.SmallMoney,
            "text" => SqlDbType.Text,
            "time" => SqlDbType.Time,
            "timestamp" => SqlDbType.Timestamp,
            "tinyint" => SqlDbType.TinyInt,
            "uniqueidentifier" => SqlDbType.UniqueIdentifier,
            "varbinary" => SqlDbType.VarBinary,
            "varchar" => SqlDbType.VarChar,
            "xml" => SqlDbType.Xml,
            _ => SqlDbType.Variant,
        };
    }

    private static int NormalizeRequestedValue(int? requestedValue, int fallback)
    {
        if (requestedValue is null or <= 0)
        {
            return fallback;
        }

        return Math.Min(requestedValue.Value, fallback);
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

    private sealed record StoredProcedureMetadata(int ObjectId, IReadOnlyList<StoredProcedureParameterMetadata> Parameters);

    private sealed record StoredProcedureParameterMetadata(
        string Name,
        bool IsOutput,
        SqlDbType SqlDbType,
        int? Size,
        byte? Precision,
        byte? Scale);
}
