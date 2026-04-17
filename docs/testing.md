# Testing

## Test Stack

The project uses:

- `xUnit`
- `AwesomeAssertions`
- `Testcontainers.MsSql`

## Test Projects

- `tests/SimpleSqlServerMcp.UnitTests`
- `tests/SimpleSqlServerMcp.IntegrationTests`
- `tests/SimpleSqlServerMcp.WindowsInstaller.Tests`

## Unit Tests

Unit tests cover:

- configuration binding
- configuration validation
- default values
- SQL identifier quoting
- read-only validator behavior
- mutable validator behavior
- unsafe-pattern validation
- service-level guardrails
- transactional execution guardrails
- database allow-list behavior
- installer environment-variable payload generation
- installer config merge behavior for supported clients

Unit tests do not depend on Docker or a real SQL Server.

## Integration Tests

Integration tests are real end-to-end tests.

They launch:

- a real SQL Server in Docker via Testcontainers
- the real MCP server process over `stdio`

They do not call application services directly as a substitute for MCP.

This means integration tests validate:

- process startup
- environment-variable binding
- MCP tool discovery
- MCP tool invocation
- SQL Server behavior against a real instance

## Integration Test Structure

Current layout:

```text
tests/
  SimpleSqlServerMcp.IntegrationTests/
    Infrastructure/
    Tests/
      Connection/
      ReadOnly/
      Mutable/
```

Meaning:

- `Infrastructure/`
  - shared SQL Server container fixture
  - shared MCP process host
  - per-test database scope helpers
- `Tests/Connection/`
  - MCP startup and tool discovery
- `Tests/ReadOnly/`
  - read-only MCP tool coverage
- `Tests/Mutable/`
  - mutable MCP tool coverage

## Container Strategy

Integration tests use:

- one SQL Server Docker container per xUnit collection
- one database per test

Why:

- the container is expensive to start, so it is shared
- each test database is isolated, so tests do not depend on one another
- destructive tests remain easy to reason about

## Current Coverage

Read-only integration tests cover:

- `server_info`
- `list_databases`
- `list_tables`
- `describe_table`
- `search_columns`
- `get_table_sample`
- `execute_read_query`
- `list_stored_procedures`
- `describe_stored_procedure`

Mutable integration tests cover:

- `execute_write_query`
- `execute_transaction`
- `execute_stored_procedure`
- mode guardrails
- unsafe override behavior

The mutable suite covers the current write whitelist, including:

- DML
- table DDL
- schema DDL
- programmability DDL
- database operations
- backup
- bulk insert

Stored procedure execution coverage includes:

- simple result sets
- multiple result sets
- output parameters
- return value
- no-result-set procedures

Transactional execution coverage includes:

- commit of multiple statements
- rollback on failure
- read-only mode rejection
- rejection of transaction-incompatible statement families
- support for the documented isolation levels
- `snapshot` with explicit database enablement in the test database

## Commands

Build:

```powershell
dotnet build SimpleSqlServerMcp.sln
```

Run all tests:

```powershell
dotnet test SimpleSqlServerMcp.sln
```

Run only runtime unit tests:

```powershell
dotnet test .\tests\SimpleSqlServerMcp.UnitTests\SimpleSqlServerMcp.UnitTests.csproj
```

Run only integration tests:

```powershell
dotnet test .\tests\SimpleSqlServerMcp.IntegrationTests\SimpleSqlServerMcp.IntegrationTests.csproj
```

Run only Windows installer tests:

```powershell
dotnet test .\tests\SimpleSqlServerMcp.WindowsInstaller.Tests\SimpleSqlServerMcp.WindowsInstaller.Tests.csproj
```
