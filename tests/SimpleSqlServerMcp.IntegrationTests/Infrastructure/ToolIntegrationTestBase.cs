using System.Text.Json;
using ModelContextProtocol.Protocol;
using Xunit.Abstractions;

namespace SimpleSqlServerMcp.IntegrationTests.Infrastructure;

[Collection(SqlServerCollection.Name)]
public abstract class ToolIntegrationTestBase(
    SqlServerCollectionFixture fixture,
    ITestOutputHelper output)
{
    protected SqlServerCollectionFixture Fixture { get; } = fixture;
    protected ITestOutputHelper Output { get; } = output;

    protected Task<IntegrationDatabaseScope> CreateDatabaseScopeAsync(
        string? databaseName = null,
        string? seedSql = null,
        CancellationToken cancellationToken = default)
    {
        return Fixture.CreateDatabaseScopeAsync(databaseName, seedSql, cancellationToken);
    }

    protected Task<McpServerProcessHost> StartReadOnlyHostAsync(
        string? defaultDatabase = null,
        IReadOnlyDictionary<string, string?>? additionalEnvironmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        return McpServerProcessHost.StartAsync(Fixture, mode: "read-only", defaultDatabase, additionalEnvironmentVariables, cancellationToken);
    }

    protected Task<McpServerProcessHost> StartMutableHostAsync(
        string? defaultDatabase = null,
        IReadOnlyDictionary<string, string?>? additionalEnvironmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        return McpServerProcessHost.StartAsync(Fixture, mode: "mutable", defaultDatabase, additionalEnvironmentVariables, cancellationToken);
    }

    protected static JsonElement GetStructuredJson(CallToolResult result)
    {
        if (result.IsError == true)
        {
            string errorText = string.Join(
                Environment.NewLine,
                result.Content.OfType<TextContentBlock>().Select(static block => block.Text));
            throw new Xunit.Sdk.XunitException($"Expected a successful tool result but received an error:{Environment.NewLine}{errorText}");
        }

        if (result.StructuredContent is JsonElement jsonElement)
        {
            return jsonElement;
        }

        result.Content.Should().NotBeNullOrEmpty();
        TextContentBlock textContent = result.Content
            .OfType<TextContentBlock>()
            .Single();

        return JsonDocument.Parse(textContent.Text).RootElement.Clone();
    }

    protected void WriteResultSummary(string toolName, JsonElement structuredContent)
    {
        Output.WriteLine($"Tool '{toolName}' returned: {structuredContent}");
    }
}
