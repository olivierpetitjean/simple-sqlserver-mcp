using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Safety;
using SimpleSqlServerMcp.Services;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.UnitTests.Services;

public sealed class QueryExecutionServiceTests
{
    [Fact]
    public async Task ExecuteReadQueryAsync_ShouldPassTargetDatabaseToConnectionFactory()
    {
        // Arrange
        RecordingReadOnlyQueryValidator validator = new();
        CapturingSqlConnectionFactory connectionFactory = new();
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
        };
        QueryExecutionService service = new(connectionFactory, validator, Options.Create(options));

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteReadQueryAsync("SELECT 1", targetDatabase: "ReportingDb", maxRows: null, timeoutSeconds: null, CancellationToken.None));

        // Assert
        exception.Message.Should().Be("The database should not be touched in this test.");
        validator.WasCalled.Should().BeTrue();
        connectionFactory.TargetDatabase.Should().Be("ReportingDb");
    }

    private sealed class RecordingReadOnlyQueryValidator : IReadOnlyQueryValidator
    {
        public bool WasCalled { get; private set; }

        public void Validate(string sql)
        {
            WasCalled = true;
        }
    }

    private sealed class CapturingSqlConnectionFactory : ISqlConnectionFactory
    {
        public string? TargetDatabase { get; private set; }

        public Task<Microsoft.Data.SqlClient.SqlConnection> OpenConnectionAsync(string? targetDatabase, CancellationToken cancellationToken)
        {
            TargetDatabase = targetDatabase;
            throw new InvalidOperationException("The database should not be touched in this test.");
        }
    }
}
