# Configuration

## Overview

The server is configured through environment variables.

The SQL Server connection string is built internally from structured settings. Users do not need to provide a raw connection string.

## SQL Server Variables

| Variable | Required | Default | Notes |
|---|---|---|---|
| `SQLSERVER_HOST` | Yes | none | SQL Server host or instance name |
| `SQLSERVER_PORT` | No | `1433` | TCP port |
| `SQLSERVER_DATABASE` | No | `master` | Default database used when no `targetDatabase` is supplied |
| `SQLSERVER_USERNAME` | Sometimes | none | Required when integrated security is disabled |
| `SQLSERVER_PASSWORD` | Sometimes | none | Required when integrated security is disabled |
| `SQLSERVER_INTEGRATED_SECURITY` | No | `false` | Set to `true` for Windows auth / integrated auth |
| `SQLSERVER_ENCRYPT` | No | `true` | SQL client encryption flag |
| `SQLSERVER_TRUST_SERVER_CERTIFICATE` | No | `false` | Trust server certificate flag |
| `SQLSERVER_APPLICATION_NAME` | No | `SimpleSqlServerMcp` | Application name sent to SQL Server |

## MCP Variables

| Variable | Required | Default | Notes |
|---|---|---|---|
| `MCP_SQLSERVER_MODE` | No | `read-only` | Allowed values: `read-only`, `mutable`, `read-write` |
| `MCP_SQLSERVER_MAX_ROWS` | No | `100` | Max rows returned by bounded tools and per result set |
| `MCP_SQLSERVER_COMMAND_TIMEOUT` | No | `15` | Timeout in seconds |
| `MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES` | No | `true` | Hides `master`, `model`, `msdb`, `tempdb` from discovery |
| `MCP_SQLSERVER_ALLOWED_DATABASES` | No | `*` | `*` allows all non-excluded databases; otherwise use a comma-separated allow-list |
| `MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS` | No | empty | Optional semicolon-separated .NET regex bypasses |
| `MCP_SQLSERVER_LOG_LEVEL` | No | `Information` | Logging verbosity |

## Authentication Rules

When `SQLSERVER_INTEGRATED_SECURITY=true`:

- `SQLSERVER_USERNAME` is not required
- `SQLSERVER_PASSWORD` is not required

When `SQLSERVER_INTEGRATED_SECURITY=false`:

- `SQLSERVER_USERNAME` is required
- `SQLSERVER_PASSWORD` is required

## `targetDatabase`

These tools accept `targetDatabase?` at call time:

- `execute_read_query`
- `execute_write_query`
- `execute_transaction`
- `execute_stored_procedure`

This is not an environment variable.

It is an execution-context override for the current tool call only. Use it when SQL Server expects a command to run inside a specific database context.

Examples:

- create a view inside a database
- create or alter a procedure in a database
- execute a procedure in a specific database

`targetDatabase` does not bypass database filtering. If the resolved database is excluded or outside `MCP_SQLSERVER_ALLOWED_DATABASES`, the call fails.

## Database Filtering

`MCP_SQLSERVER_ALLOWED_DATABASES` controls which databases the MCP may access.

Supported forms:

- `*`
- `DatabaseA,DatabaseB,DatabaseC`

Behavior:

- if the value is `*`, every accessible database is allowed
- if the value is omitted or blank, the effective behavior is also `*`
- if specific names are provided, only those databases are allowed
- `MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES=true` still wins over `*`

Examples:

```text
MCP_SQLSERVER_ALLOWED_DATABASES=*
```

```text
MCP_SQLSERVER_ALLOWED_DATABASES=Developpe-2022,Reporting
```

The filter applies to:

- `list_databases`
- schema exploration tools such as `list_tables`, `describe_table`, `search_columns`, `get_table_sample`
- `execute_read_query`
- `execute_write_query`
- `execute_transaction`
- `list_stored_procedures`
- `describe_stored_procedure`
- `execute_stored_procedure`

## Unsafe Overrides

`MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS` is an advanced escape hatch.

Format:

- semicolon-separated .NET regex patterns

Behavior:

- patterns are tested against the raw SQL text
- a matching statement is allowed before the normal whitelist check
- this applies to both read-only and mutable validation

Example:

```text
MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS=^DBCC\s+CHECKIDENT\b;^RESTORE\s+VERIFYONLY\b
```

Use this only when you explicitly accept a weaker MCP-side safety model.

## Example Local Run

Integrated security:

```powershell
$env:SQLSERVER_HOST="localhost"
$env:SQLSERVER_DATABASE="master"
$env:SQLSERVER_INTEGRATED_SECURITY="true"
$env:SQLSERVER_ENCRYPT="false"
$env:SQLSERVER_TRUST_SERVER_CERTIFICATE="true"
$env:MCP_SQLSERVER_ALLOWED_DATABASES="*"
dotnet run --project .\src\SimpleSqlServerMcp
```

SQL authentication:

```powershell
$env:SQLSERVER_HOST="localhost"
$env:SQLSERVER_DATABASE="master"
$env:SQLSERVER_USERNAME="sa"
$env:SQLSERVER_PASSWORD="YourStrong(!)Password"
$env:SQLSERVER_ENCRYPT="false"
$env:SQLSERVER_TRUST_SERVER_CERTIFICATE="true"
$env:MCP_SQLSERVER_ALLOWED_DATABASES="Developpe-2022,Reporting"
dotnet run --project .\src\SimpleSqlServerMcp
```
