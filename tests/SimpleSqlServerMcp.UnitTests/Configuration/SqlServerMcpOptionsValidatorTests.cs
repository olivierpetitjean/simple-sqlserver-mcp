using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;

namespace SimpleSqlServerMcp.UnitTests.Configuration;

public sealed class SqlServerMcpOptionsValidatorTests
{
    private readonly SqlServerMcpOptionsValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenHostIsMissing()
    {
        // Arrange
        SqlServerMcpOptions options = new();

        // Act
        ValidateOptionsResult result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().NotBeNull();
        result.Failures!.Should().Contain(failure => failure.Contains("SQLSERVER_HOST", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldFail_WhenMaxRowsIsNotPositive()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = true,
            MaxRows = 0,
        };

        // Act
        ValidateOptionsResult result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().NotBeNull();
        result.Failures!.Should().Contain(failure => failure.Contains("MCP_SQLSERVER_MAX_ROWS", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenOptionsAreValid()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = true,
            MaxRows = 100,
            CommandTimeoutSeconds = 15,
        };

        // Act
        ValidateOptionsResult result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenIntegratedSecurityIsEnabled_AndCredentialsAreMissing()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = true,
        };

        // Act
        ValidateOptionsResult result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIntegratedSecurityIsDisabled_AndCredentialsAreMissing()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = false,
        };

        // Act
        ValidateOptionsResult result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().NotBeNull();
        result.Failures!.Should().Contain(failure => failure.Contains("SQLSERVER_USERNAME", StringComparison.Ordinal));
        result.Failures!.Should().Contain(failure => failure.Contains("SQLSERVER_PASSWORD", StringComparison.Ordinal));
        result.Failures!.Should().Contain(failure => failure.Contains("SQLSERVER_PASSWORD_SECRET_NAME", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenIntegratedSecurityIsDisabled_AndPasswordSecretNameIsProvided()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = false,
            Username = "sa",
            PasswordSecretName = "SimpleSqlServerMcp/SqlPassword/test",
        };

        // Act
        ValidateOptionsResult result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUnsafeAllowedPatternsContainInvalidRegex()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = true,
            UnsafeAllowedPatterns = ["("],
        };

        // Act
        ValidateOptionsResult result = _validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().NotBeNull();
        result.Failures!.Should().Contain(failure => failure.Contains("MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS", StringComparison.Ordinal));
    }
}
