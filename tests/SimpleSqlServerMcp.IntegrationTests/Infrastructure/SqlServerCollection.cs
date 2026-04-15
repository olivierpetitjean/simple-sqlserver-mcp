namespace SimpleSqlServerMcp.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerCollectionFixture>
{
    public const string Name = "sql-server-container";
}
