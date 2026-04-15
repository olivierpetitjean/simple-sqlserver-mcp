# Specification

## Design Intent

Simple SQL Server MCP is meant to be useful for AI-assisted developer workflows without trying to model all of SQL Server.

Core principles:

- powerful schema and data exploration
- strict read-only validation by default
- explicit opt-in for mutation
- explicit support for stored procedure exploration and execution
- optional unsafe escape hatch for advanced local-only scenarios

## Tools

### `server_info`

Purpose:

- return basic SQL Server connection context and active MCP mode

Typical output:

- server name
- login name
- current database
- edition
- version
- active mode

### `list_databases`

Purpose:

- list accessible databases on the SQL Server instance

Behavior:

- respects `MCP_SQLSERVER_ALLOWED_DATABASES`
- respects `MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES`

Parameters:

- `search?`
- `limit?`

### `list_tables`

Purpose:

- list tables inside one database

Parameters:

- `database`
- `schema?`
- `search?`
- `limit?`

### `describe_table`

Purpose:

- return table structure

Parameters:

- `database`
- `schema`
- `table`

Output includes:

- columns
- SQL types
- nullability
- identity flags
- primary key columns
- outgoing foreign keys

### `search_columns`

Purpose:

- search column names inside one database

Parameters:

- `database`
- `search`
- `schema?`
- `limit?`

### `get_table_sample`

Purpose:

- return a bounded sample of rows from one table

Parameters:

- `database`
- `schema`
- `table`
- `limit?`

### `execute_read_query`

Purpose:

- execute one read-only SQL statement after validation

Parameters:

- `sql`
- `targetDatabase?`
- `maxRows?`
- `timeoutSeconds?`

Output:

- `columns`
- `rows`
- `rowCount`
- `truncated`
- `durationMilliseconds`

### `execute_write_query`

Purpose:

- execute one mutable SQL statement when mutable mode is enabled

Parameters:

- `sql`
- `targetDatabase?`
- `timeoutSeconds?`

Output:

- `statementType`
- `rowsAffected`
- `durationMilliseconds`

### `list_stored_procedures`

Purpose:

- list stored procedures inside one database

Parameters:

- `database`
- `schema?`
- `search?`
- `limit?`

Output includes:

- schema
- procedure name
- creation timestamp
- modification timestamp

### `describe_stored_procedure`

Purpose:

- describe one stored procedure

Parameters:

- `database`
- `schema`
- `procedure`

Output includes:

- creation timestamp
- modification timestamp
- definition text
- parameters
- first result-set metadata when SQL Server can infer it

### `execute_stored_procedure`

Purpose:

- execute one stored procedure in mutable mode

Parameters:

- `database`
- `schema`
- `procedure`
- `parameters?`
- `targetDatabase?`
- `maxRows?`
- `timeoutSeconds?`

Behavior:

- procedure parameters can be passed with or without `@`
- result sets are returned as a list of tabular payloads
- output parameters are returned by name
- return value is captured

Output includes:

- `resultSets`
- `outputParameters`
- `returnValue`
- `rowsAffected`
- `durationMilliseconds`

## Read-only Policy

Accepted:

- exactly one `SELECT`
- `WITH ... SELECT`
- joins
- subqueries
- `UNION`
- window functions
- cross-database reads

Rejected:

- multiple statements
- `SELECT INTO`
- mutable statements
- `EXEC`

## Mutable Policy

Mutable execution is disabled unless:

- `MCP_SQLSERVER_MODE=mutable`

Currently supported statement families:

- `SELECT INTO`
- `INSERT`
- `UPDATE`
- `DELETE`
- `MERGE`
- `CREATE DATABASE`
- `ALTER DATABASE`
- `DROP DATABASE`
- `BACKUP`
- `BULK INSERT`
- `CREATE TABLE`
- `ALTER TABLE`
- `DROP TABLE`
- `TRUNCATE TABLE`
- `CREATE/ALTER/DROP PROCEDURE`
- `CREATE OR ALTER PROCEDURE`
- `CREATE/ALTER/DROP FUNCTION`
- `CREATE OR ALTER FUNCTION`
- `CREATE/ALTER/DROP VIEW`
- `CREATE OR ALTER VIEW`
- `CREATE/ALTER/DROP INDEX`
- `CREATE/ALTER/DROP SCHEMA`
- `CREATE/ALTER/DROP SEQUENCE`
- `CREATE/DROP TYPE`
- `CREATE/DROP SYNONYM`
- `CREATE/ALTER/DROP TRIGGER`
- `CREATE OR ALTER TRIGGER`

Not supported through `execute_write_query`:

- free-form `EXEC`
- arbitrary multi-statement scripts
- unsupported SQL Server statement families outside the whitelist

## Stored Procedure Execution Notes

Stored procedures are intentionally separated from `execute_write_query`.

Why:

- they need parameter binding
- they may return multiple result sets
- they may expose output parameters
- they may expose a return value

This makes them a different contract from free-form SQL mutation.

## `targetDatabase`

`targetDatabase` is an execution-context override. It is not a SQL rewrite feature.

Use it when SQL Server expects the statement or procedure to run inside a specific database context.

Important cases:

- `CREATE VIEW`
- `CREATE PROCEDURE`
- `CREATE FUNCTION`
- `CREATE TRIGGER`
- stored procedure execution in a specific database

If omitted, the configured default database is used.

`targetDatabase` never bypasses database filtering. The resolved database must still be allowed by `MCP_SQLSERVER_ALLOWED_DATABASES` and not blocked by system-database exclusion.

## Database Access Filtering

The server supports an environment-based database allow-list:

- `MCP_SQLSERVER_ALLOWED_DATABASES=*`
- `MCP_SQLSERVER_ALLOWED_DATABASES=DatabaseA,DatabaseB`

Rules:

- `*` means all databases are allowed
- omitting the variable is treated as `*`
- when a list is provided, only listed databases are allowed
- system-database exclusion still applies when enabled

## Unsafe Escape Hatch

`MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS` allows semicolon-separated .NET regex patterns to bypass the built-in whitelist.

Example:

```text
MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS=^DBCC\s+CHECKIDENT\b;^RESTORE\s+VERIFYONLY\b
```

This exists for local dev/test flexibility only. It weakens the MCP safety guarantees by design.
