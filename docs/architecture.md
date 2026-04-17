# Architecture

## Overview

The repository uses:

- one main MCP server project
- one Windows installer project
- separate test projects for runtime and the Windows installer

The runtime server itself keeps a lightweight layered structure.

This is intentionally simpler than a full multi-project Clean Architecture setup.

Goals:

- testable
- decoupled enough to evolve
- no unnecessary abstraction explosion

## Runtime Structure

Main folders:

```text
src/
  SimpleSqlServerMcp/
    Configuration/
    Host/
    Models/
    Safety/
    Services/
    Sql/
    Tools/
  SimpleSqlServerMcp.WindowsInstaller/
installer/
  windows/
```

## Responsibilities

### `Host/`

Contains MCP and hosting glue:

- dependency injection setup
- MCP server registration
- stdio transport
- top-level tool error mapping

### `Configuration/`

Contains:

- options model
- config parsing
- config validation

### `Tools/`

Contains MCP tool entry points.

These classes are intentionally thin. They:

- define tool names and descriptions
- validate argument shape at the contract level
- call services

### `Services/`

Contains application logic.

Examples:

- schema exploration
- read-query execution
- write-query execution
- transactional execution
- stored procedure execution

These services know SQL Server behavior but do not know MCP transport details.

### `Sql/`

Contains SQL infrastructure:

- connection factory
- database access policy
- SQL identifier quoting

### `Safety/`

Contains SQL validation logic:

- read-only validation
- mutable validation
- unsafe pattern overrides

Transactional execution reuses mutable validation statement by statement rather than introducing a second free-form SQL execution surface.

### `Models/`

Contains DTOs returned by services and tools.

## Dependency Direction

The intended dependency flow is:

- `Tools -> Services`
- `Services -> Sql`
- `Services -> Safety`
- `Host -> all registrations`

`Services` should not depend on MCP transport types unless there is a strong reason.

## Testing Architecture

### Unit tests

Unit tests target:

- validators
- config
- helper infrastructure
- service guardrails
- installer config writers and path resolvers

### Integration tests

Integration tests target the system end-to-end:

- real SQL Server Docker container
- real MCP process over `stdio`
- real tool calls

This gives much stronger confidence than calling services directly in the integration layer.

## Installer Structure

The installer architecture is intentionally split by platform.

Windows-specific logic lives in:

- `src/SimpleSqlServerMcp.WindowsInstaller`
- `installer/windows`

Unix installation currently lives in:

- `install.sh`

Windows tool registrations are implemented per provider, with dedicated path resolution and format-aware merge logic for each supported client configuration.
