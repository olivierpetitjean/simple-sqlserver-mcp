namespace SimpleSqlServerMcp.Models;

internal sealed class DatabaseInfo
{
    public required string Name { get; init; }

    public string? State { get; init; }

    public int CompatibilityLevel { get; init; }
}
