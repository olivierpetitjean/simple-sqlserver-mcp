# Changelog

All notable changes to this project will be documented in this file.

The format is inspired by Keep a Changelog and the project follows semantic versioning.

## [Unreleased]

## [1.2.0] - 2026-04-17

### Added
- Added optional Windows Credential Manager support for SQL Server passwords through `SQLSERVER_PASSWORD_SECRET_NAME`.
- Added Windows installer support for storing SQL passwords in the current user's Windows Credential Manager instead of writing `SQLSERVER_PASSWORD` into generated MCP client configs.
- Added a repository `CHANGELOG.md`.
- Added `.github/release.yml` to organize GitHub auto-generated release notes into categories.

### Changed
- Clarified runtime password resolution and Windows-only secret-store behavior in the public documentation.
- Automated version propagation from Git tags into .NET assembly metadata and the Windows MSI version.
- Added richer executable and package metadata such as author, company, license, repository URL, and product description.

## [1.1.0] - 2026-04-17

### Added
- Added `execute_transaction` for atomic multi-statement SQL Server write operations.
- Added optional transaction `isolationLevel` support.

### Changed
- Improved integration-test process resolution so the test host launches the current build configuration.
- Expanded the public documentation for transactional execution and isolation levels.

## [1.0.0] - 2026-04-15

### Added
- Initial public release of Simple SQL Server MCP.
- Added MCP tools for SQL Server inspection, schema exploration, sampling, read queries, mutable queries, and stored procedure exploration/execution.
- Added Windows MSI and Linux source-based installation flows.
