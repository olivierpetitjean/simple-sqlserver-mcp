using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.UnitTests.Sql;

public sealed class DatabaseAccessPolicyTests
{
    [Fact]
    public void IsAllowed_ShouldAllowAnyNonSystemDatabase_WhenWildcardIsConfigured()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            ExcludeSystemDatabases = true,
            AllowedDatabases = ["*"],
        };

        // Act
        bool allowed = DatabaseAccessPolicy.IsAllowed(options, "Developpe");

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_ShouldTreatEmptyAllowListAsWildcard_ForBackwardCompatibility()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            ExcludeSystemDatabases = false,
            AllowedDatabases = [],
        };

        // Act
        bool allowed = DatabaseAccessPolicy.IsAllowed(options, "Developpe");

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void EnsureAllowed_ShouldRejectSystemDatabases_EvenWhenWildcardIsConfigured()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            ExcludeSystemDatabases = true,
            AllowedDatabases = ["*"],
        };

        // Act
        Action act = () => DatabaseAccessPolicy.EnsureAllowed(options, "master");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*excluded by configuration*");
    }

    [Fact]
    public void EnsureAllowed_ShouldRejectDatabasesOutsideTheAllowList()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            ExcludeSystemDatabases = false,
            AllowedDatabases = ["Developpe"],
        };

        // Act
        Action act = () => DatabaseAccessPolicy.EnsureAllowed(options, "master");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not allowed by configuration*");
    }
}
