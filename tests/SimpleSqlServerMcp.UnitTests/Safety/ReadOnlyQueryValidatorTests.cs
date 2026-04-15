using SimpleSqlServerMcp.Safety;
using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.UnitTests.Safety;

public sealed class ReadOnlyQueryValidatorTests
{
    private readonly ReadOnlyQueryValidator _validator = CreateValidator();

    [Fact]
    public void Validate_ShouldAllow_SimpleSelect()
    {
        // Arrange

        // Act
        _validator.Validate("SELECT 1");

        // Assert
    }

    [Fact]
    public void Validate_ShouldAllow_CteSelect()
    {
        // Arrange

        // Act
        _validator.Validate(
            """
            WITH users_cte AS (
                SELECT 1 AS Id
            )
            SELECT Id FROM users_cte
            """);

        // Assert
    }

    [Fact]
    public void Validate_ShouldReject_Update()
    {
        // Arrange

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _validator.Validate("UPDATE dbo.Users SET Name = 'x'"));

        // Assert
        exception.Message.Should().ContainEquivalentOf("SELECT");
    }

    [Fact]
    public void Validate_ShouldReject_MultipleStatements()
    {
        // Arrange

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _validator.Validate("SELECT 1; SELECT 2;"));

        // Assert
        exception.Message.Should().ContainEquivalentOf("exactly one");
    }

    [Fact]
    public void Validate_ShouldReject_SelectInto()
    {
        // Arrange

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _validator.Validate("SELECT * INTO dbo.UsersCopy FROM dbo.Users"));

        // Assert
        exception.Message.Should().ContainEquivalentOf("SELECT INTO");
    }

    [Fact]
    public void Validate_ShouldReject_ExecuteProcedure()
    {
        // Arrange

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _validator.Validate("EXEC dbo.MyProcedure"));

        // Assert
        exception.Message.Should().ContainEquivalentOf("SELECT");
    }

    [Fact]
    public void Validate_ShouldAllow_KeywordInsideStringLiteral()
    {
        // Arrange

        // Act
        _validator.Validate("SELECT 'delete from users' AS DebugText");

        // Assert
    }

    [Fact]
    public void Validate_ShouldAllowStatementsMatchedByUnsafePatterns()
    {
        // Arrange
        ReadOnlyQueryValidator validator = CreateValidator("^DBCC\\s+CHECKIDENT");

        // Act
        validator.Validate("DBCC CHECKIDENT ('dbo.Users', RESEED, 0)");

        // Assert
    }

    private static ReadOnlyQueryValidator CreateValidator(params string[] unsafePatterns)
    {
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            UnsafeAllowedPatterns = unsafePatterns,
        };

        return new ReadOnlyQueryValidator(Options.Create(options));
    }
}
