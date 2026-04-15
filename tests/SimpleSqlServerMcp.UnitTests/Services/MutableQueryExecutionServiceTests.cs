using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Safety;
using SimpleSqlServerMcp.Services;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.UnitTests.Services;

public sealed class MutableQueryExecutionServiceTests
{
    [Fact]
    public async Task ExecuteWriteQueryAsync_ShouldFailBeforeTouchingDatabase_WhenModeIsNotMutable()
    {
        // Arrange
        RecordingMutableQueryValidator validator = new();
        ThrowingSqlConnectionFactory connectionFactory = new();
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            Mode = QueryExecutionMode.ReadOnly,
        };
        MutableQueryExecutionService service = new(connectionFactory, validator, Options.Create(options));

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteWriteQueryAsync("UPDATE dbo.Users SET Name = 'x'", targetDatabase: "AppDb", timeoutSeconds: null, CancellationToken.None));

        // Assert
        exception.Message.Should().ContainEquivalentOf("not set to mutable");
        validator.WasCalled.Should().BeFalse();
        connectionFactory.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteWriteQueryAsync_ShouldPassTargetDatabaseToConnectionFactory_WhenModeIsMutable()
    {
        // Arrange
        RecordingMutableQueryValidator validator = new();
        ThrowingSqlConnectionFactory connectionFactory = new();
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            Mode = QueryExecutionMode.Mutable,
        };
        MutableQueryExecutionService service = new(connectionFactory, validator, Options.Create(options));

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteWriteQueryAsync("UPDATE dbo.Users SET Name = 'x'", targetDatabase: "AppDb", timeoutSeconds: null, CancellationToken.None));

        // Assert
        exception.Message.Should().Be("The database should not be touched in this test.");
        validator.WasCalled.Should().BeTrue();
        connectionFactory.WasCalled.Should().BeTrue();
        connectionFactory.TargetDatabase.Should().Be("AppDb");
    }

    private sealed class RecordingMutableQueryValidator : IMutableQueryValidator
    {
        public bool WasCalled { get; private set; }

        public string Validate(string sql)
        {
            WasCalled = true;
            return "UPDATE";
        }
    }

    private sealed class ThrowingSqlConnectionFactory : ISqlConnectionFactory
    {
        public bool WasCalled { get; private set; }
        public string? TargetDatabase { get; private set; }

        public Task<Microsoft.Data.SqlClient.SqlConnection> OpenConnectionAsync(string? targetDatabase, CancellationToken cancellationToken)
        {
            WasCalled = true;
            TargetDatabase = targetDatabase;
            throw new InvalidOperationException("The database should not be touched in this test.");
        }
    }
}
