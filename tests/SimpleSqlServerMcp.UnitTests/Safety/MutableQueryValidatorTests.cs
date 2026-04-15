using SimpleSqlServerMcp.Safety;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.UnitTests.Safety;

public sealed class MutableQueryValidatorTests
{
    private readonly MutableQueryValidator _validator = CreateValidator();

    [Theory]
    [InlineData("INSERT INTO dbo.Users(Id) VALUES (1)", "INSERT")]
    [InlineData("SELECT TOP (1) * INTO dbo.UsersCopy FROM dbo.Users", "SELECT INTO")]
    [InlineData("UPDATE dbo.Users SET Name = 'x'", "UPDATE")]
    [InlineData("DELETE FROM dbo.Users", "DELETE")]
    [InlineData("MERGE dbo.Target AS t USING dbo.Source AS s ON t.Id = s.Id WHEN MATCHED THEN UPDATE SET t.Name = s.Name;", "MERGE")]
    [InlineData("CREATE DATABASE SimpleSqlServerMcpTest", "CREATE DATABASE")]
    [InlineData("ALTER DATABASE SimpleSqlServerMcpTest SET RECOVERY SIMPLE", "ALTER DATABASE")]
    [InlineData("BACKUP DATABASE SimpleSqlServerMcpTest TO DISK = 'C:\\Backup\\SimpleSqlServerMcpTest.bak'", "BACKUP")]
    [InlineData("BULK INSERT dbo.Users FROM 'C:\\Temp\\users.csv' WITH (FIRSTROW = 2)", "BULK INSERT")]
    [InlineData("CREATE TABLE dbo.Users(Id int NOT NULL)", "CREATE TABLE")]
    [InlineData("ALTER TABLE dbo.Users ADD Name nvarchar(100) NULL", "ALTER TABLE")]
    [InlineData("DROP TABLE dbo.Users", "DROP TABLE")]
    [InlineData("DROP DATABASE SimpleSqlServerMcpTest", "DROP DATABASE")]
    [InlineData("TRUNCATE TABLE dbo.Users", "TRUNCATE TABLE")]
    [InlineData("CREATE PROCEDURE dbo.TestProc AS SELECT 1", "CREATE PROCEDURE")]
    [InlineData("ALTER PROCEDURE dbo.TestProc AS SELECT 2", "ALTER PROCEDURE")]
    [InlineData("CREATE OR ALTER PROCEDURE dbo.TestProc AS SELECT 3", "CREATE OR ALTER PROCEDURE")]
    [InlineData("DROP PROCEDURE dbo.TestProc", "DROP PROCEDURE")]
    [InlineData("CREATE FUNCTION dbo.TestFunc() RETURNS int AS BEGIN RETURN 1 END", "CREATE FUNCTION")]
    [InlineData("ALTER FUNCTION dbo.TestFunc() RETURNS int AS BEGIN RETURN 2 END", "ALTER FUNCTION")]
    [InlineData("CREATE OR ALTER FUNCTION dbo.TestFunc() RETURNS int AS BEGIN RETURN 3 END", "CREATE OR ALTER FUNCTION")]
    [InlineData("DROP FUNCTION dbo.TestFunc", "DROP FUNCTION")]
    [InlineData("CREATE VIEW dbo.TestView AS SELECT 1 AS Value", "CREATE VIEW")]
    [InlineData("ALTER VIEW dbo.TestView AS SELECT 2 AS Value", "ALTER VIEW")]
    [InlineData("CREATE OR ALTER VIEW dbo.TestView AS SELECT 3 AS Value", "CREATE OR ALTER VIEW")]
    [InlineData("DROP VIEW dbo.TestView", "DROP VIEW")]
    [InlineData("CREATE INDEX IX_Users_Name ON dbo.Users(Name)", "CREATE INDEX")]
    [InlineData("ALTER INDEX IX_Users_Name ON dbo.Users REBUILD", "ALTER INDEX")]
    [InlineData("DROP INDEX IX_Users_Name ON dbo.Users", "DROP INDEX")]
    [InlineData("CREATE SCHEMA reporting", "CREATE SCHEMA")]
    [InlineData("ALTER SCHEMA reporting TRANSFER dbo.Users", "ALTER SCHEMA")]
    [InlineData("DROP SCHEMA reporting", "DROP SCHEMA")]
    [InlineData("CREATE SEQUENCE dbo.TestSeq AS int START WITH 1 INCREMENT BY 1", "CREATE SEQUENCE")]
    [InlineData("ALTER SEQUENCE dbo.TestSeq RESTART WITH 10", "ALTER SEQUENCE")]
    [InlineData("DROP SEQUENCE dbo.TestSeq", "DROP SEQUENCE")]
    [InlineData("CREATE TYPE dbo.EmailAddress FROM nvarchar(256) NOT NULL", "CREATE TYPE")]
    [InlineData("DROP TYPE dbo.EmailAddress", "DROP TYPE")]
    [InlineData("CREATE SYNONYM dbo.UsersSyn FOR dbo.Users", "CREATE SYNONYM")]
    [InlineData("DROP SYNONYM dbo.UsersSyn", "DROP SYNONYM")]
    [InlineData("CREATE TRIGGER dbo.UsersTrigger ON dbo.Users AFTER INSERT AS SELECT 1", "CREATE TRIGGER")]
    [InlineData("ALTER TRIGGER dbo.UsersTrigger ON dbo.Users AFTER INSERT AS SELECT 2", "ALTER TRIGGER")]
    [InlineData("CREATE OR ALTER TRIGGER dbo.UsersTrigger ON dbo.Users AFTER INSERT AS SELECT 3", "CREATE OR ALTER TRIGGER")]
    [InlineData("DROP TRIGGER dbo.UsersTrigger", "DROP TRIGGER")]
    public void Validate_ShouldAllow_SupportedMutableStatements(string sql, string expectedStatementType)
    {
        // Arrange

        // Act
        string statementType = _validator.Validate(sql);

        // Assert
        statementType.Should().Be(expectedStatementType);
    }

    [Fact]
    public void Validate_ShouldReject_Select()
    {
        // Arrange

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _validator.Validate("SELECT 1"));

        // Assert
        exception.Message.Should().ContainEquivalentOf("explicitly whitelisted");
    }

    [Fact]
    public void Validate_ShouldReject_ExecuteProcedure()
    {
        // Arrange

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _validator.Validate("EXEC dbo.MyProcedure"));

        // Assert
        exception.Message.Should().ContainEquivalentOf("explicitly whitelisted");
    }

    [Fact]
    public void Validate_ShouldReject_MultipleStatements()
    {
        // Arrange

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _validator.Validate("UPDATE dbo.Users SET Name = 'x'; DELETE FROM dbo.Users;"));

        // Assert
        exception.Message.Should().ContainEquivalentOf("exactly one");
    }

    [Fact]
    public void Validate_ShouldAllowStatementsMatchedByUnsafePatterns()
    {
        // Arrange
        MutableQueryValidator validator = CreateValidator("^DBCC\\s+CHECKIDENT");

        // Act
        string statementType = validator.Validate("DBCC CHECKIDENT ('dbo.Users', RESEED, 0)");

        // Assert
        statementType.Should().Be("UNSAFE PATTERN");
    }

    private static MutableQueryValidator CreateValidator(params string[] unsafePatterns)
    {
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            UnsafeAllowedPatterns = unsafePatterns,
        };

        return new MutableQueryValidator(Options.Create(options));
    }
}
