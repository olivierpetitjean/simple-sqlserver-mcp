using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Safety;
using SimpleSqlServerMcp.Services;
using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.UnitTests.Services;

public sealed class TransactionExecutionServiceTests
{
    [Fact]
    public async Task ExecuteTransactionAsync_ShouldFailBeforeTouchingDatabase_WhenModeIsNotMutable()
    {
        // Arrange
        RecordingMutableQueryValidator validator = new("UPDATE");
        ThrowingSqlConnectionFactory connectionFactory = new();
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            Mode = QueryExecutionMode.ReadOnly,
        };
        TransactionExecutionService service = new(connectionFactory, validator, Options.Create(options));

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteTransactionAsync(["UPDATE dbo.Users SET Name = 'x'"], targetDatabase: "AppDb", isolationLevel: null, timeoutSeconds: null, CancellationToken.None));

        // Assert
        exception.Message.Should().ContainEquivalentOf("not set to mutable");
        validator.Calls.Should().Be(0);
        connectionFactory.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteTransactionAsync_ShouldFailBeforeTouchingDatabase_WhenStatementTypeIsNotTransactionSafe()
    {
        // Arrange
        RecordingMutableQueryValidator validator = new("CREATE DATABASE");
        ThrowingSqlConnectionFactory connectionFactory = new();
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            Mode = QueryExecutionMode.Mutable,
        };
        TransactionExecutionService service = new(connectionFactory, validator, Options.Create(options));

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteTransactionAsync(["CREATE DATABASE [Demo]"], targetDatabase: "AppDb", isolationLevel: null, timeoutSeconds: null, CancellationToken.None));

        // Assert
        exception.Message.Should().Contain("does not support");
        exception.Message.Should().Contain("CREATE DATABASE");
        validator.Calls.Should().Be(1);
        connectionFactory.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteTransactionAsync_ShouldPassTargetDatabaseToConnectionFactory_WhenStatementsAreValid()
    {
        // Arrange
        RecordingMutableQueryValidator validator = new("UPDATE");
        ThrowingSqlConnectionFactory connectionFactory = new();
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            Mode = QueryExecutionMode.Mutable,
        };
        TransactionExecutionService service = new(connectionFactory, validator, Options.Create(options));

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteTransactionAsync(["UPDATE dbo.Users SET Name = 'x'"], targetDatabase: "AppDb", isolationLevel: null, timeoutSeconds: null, CancellationToken.None));

        // Assert
        exception.Message.Should().Be("The database should not be touched in this test.");
        validator.Calls.Should().Be(1);
        connectionFactory.WasCalled.Should().BeTrue();
        connectionFactory.TargetDatabase.Should().Be("AppDb");
    }

    [Fact]
    public async Task ExecuteTransactionAsync_ShouldRejectUnsupportedIsolationLevelBeforeTouchingDatabase()
    {
        // Arrange
        RecordingMutableQueryValidator validator = new("UPDATE");
        ThrowingSqlConnectionFactory connectionFactory = new();
        SqlServerMcpOptions options = new()
        {
            Host = "localhost",
            Database = "master",
            IntegratedSecurity = true,
            Mode = QueryExecutionMode.Mutable,
        };
        TransactionExecutionService service = new(connectionFactory, validator, Options.Create(options));

        // Act
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteTransactionAsync(["UPDATE dbo.Users SET Name = 'x'"], targetDatabase: "AppDb", isolationLevel: "banana", timeoutSeconds: null, CancellationToken.None));

        // Assert
        exception.Message.Should().Contain("Unsupported isolationLevel");
        validator.Calls.Should().Be(1);
        connectionFactory.WasCalled.Should().BeFalse();
    }

    [Fact]
    public void BuildFailureMessage_ShouldDescribeFailedStatement_WhenAStatementFailsBeforeCommit()
    {
        // Arrange
        string[] statementTypes = ["UPDATE", "INSERT", "DELETE"];

        // Act
        string message = TransactionExecutionService.BuildFailureMessage(
            statementTypes,
            completedStatementCount: 1,
            failureMessage: "duplicate key");

        // Assert
        message.Should().Be("Transaction rolled back after statement 2 (INSERT) failed: duplicate key");
    }

    [Fact]
    public void BuildFailureMessage_ShouldDescribeCommitFailure_WhenAllStatementsCompleted()
    {
        // Arrange
        string[] statementTypes = ["UPDATE", "INSERT"];

        // Act
        string message = TransactionExecutionService.BuildFailureMessage(
            statementTypes,
            completedStatementCount: 2,
            failureMessage: "commit transport error");

        // Assert
        message.Should().Be("Transaction rolled back after commit failed: commit transport error");
    }

    private sealed class RecordingMutableQueryValidator(string statementType) : IMutableQueryValidator
    {
        public int Calls { get; private set; }

        public string Validate(string sql)
        {
            Calls++;
            return statementType;
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
