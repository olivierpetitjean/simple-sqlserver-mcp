# Safety

## What "Safe" Means Here

This project targets developer and test databases.

So the goal is not production security in the usual sense. The goal is mostly:

- prevent accidental AI writes in read-only mode
- make mutable actions explicit
- keep the SQL surface understandable and testable

## Read-only Validation

Read-only execution is validated with:

- `Microsoft.SqlServer.TransactSql.ScriptDom`

Accepted:

- exactly one statement
- one `SELECT`
- `WITH ... SELECT`
- joins
- subqueries
- `UNION`
- window functions
- cross-database reads

Rejected:

- multiple statements
- `SELECT INTO`
- `INSERT`
- `UPDATE`
- `DELETE`
- `MERGE`
- `EXEC`
- any non-`SELECT` statement

This allows complex read queries while preventing accidental writes caused by hallucinated SQL.

## Mutable Validation

Mutable execution is disabled unless:

- `MCP_SQLSERVER_MODE=mutable`

When mutable mode is enabled, the MCP still uses a whitelist. Only explicitly supported SQL Server statement families are allowed.

Supported families currently include:

- `SELECT INTO`
- DML
- table DDL
- schema DDL
- view / procedure / function / trigger DDL
- database create / drop / alter
- backup
- bulk import

Not supported in `execute_write_query`:

- free-form `EXEC`
- arbitrary multi-statement scripts
- unsupported SQL Server statement families outside the whitelist

## Transactions

Transactions are supported through:

- `execute_transaction`

This is intentionally separate from `execute_write_query`.

Why:

- `execute_write_query` stays single-statement and easy to reason about
- transaction scope remains bounded to one MCP call
- the server does not keep open transaction state between calls

`execute_transaction`:

- validates each statement with the mutable whitelist
- commits only if all statements succeed
- rolls back the entire transaction if one statement fails

The current transaction tool intentionally excludes:

- `UNSAFE PATTERN`
- `CREATE DATABASE`
- `ALTER DATABASE`
- `DROP DATABASE`
- `BACKUP`
- `BULK INSERT`

Those statements can still run individually through `execute_write_query` when allowed, but not inside `execute_transaction`.

`execute_transaction` also supports an optional `isolationLevel`.

Allowed values:

- `read_committed`
- `read_uncommitted`
- `repeatable_read`
- `serializable`
- `snapshot`

If `isolationLevel` is omitted, the MCP uses the default SQL Server transaction behavior for the session.

`snapshot` may still fail if the target database is not configured to allow snapshot isolation.

## Stored Procedures

Stored procedures are not treated as generic mutable SQL.

Instead, they have their own contract:

- `list_stored_procedures`
- `describe_stored_procedure`
- `execute_stored_procedure`

This keeps:

- parameter binding explicit
- result sets structured
- output parameters and return values visible to the caller

## `targetDatabase`

`targetDatabase` changes the execution context for one tool call.

It does not bypass validation.

It exists because some SQL Server operations are valid only when executed inside a specific database context, especially:

- `CREATE VIEW`
- `CREATE PROCEDURE`
- `CREATE FUNCTION`
- `CREATE TRIGGER`
- transaction execution against a specific database
- stored procedure execution against a specific database

It also does not bypass database filtering. If the resolved database is excluded or not present in `MCP_SQLSERVER_ALLOWED_DATABASES`, execution is rejected before SQL runs.

## Unsafe Pattern Overrides

The MCP supports an explicit escape hatch:

- `MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS`

This variable contains semicolon-separated .NET regex patterns. If a raw SQL string matches one of these patterns, the statement is allowed before the built-in whitelist is applied.

Example:

```text
MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS=^DBCC\s+CHECKIDENT\b
```

This is intentionally unsafe:

- the MCP no longer guarantees that the statement belongs to the curated read-only or mutable set
- the burden shifts to the user who configured the override

This option is acceptable for local dev/test databases. It should be documented clearly whenever it is enabled.

## Recommended User Guidance

For normal use:

- keep `MCP_SQLSERVER_MODE=read-only`
- use `mutable` only when you want explicit writes
- keep `MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS` empty

For advanced local-only use:

- enable `mutable`
- optionally add a narrow unsafe regex if a needed SQL form is not yet whitelisted
- prefer the narrowest pattern possible
