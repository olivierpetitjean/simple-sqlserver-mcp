using SimpleSqlServerMcp.WindowsInstaller;

namespace SimpleSqlServerMcp.WindowsInstaller.Tests;

public sealed class CodexConfigPathResolverTests
{
    [Fact]
    public void ResolveDefault_uses_windows_codex_path()
    {
        var path = CodexConfigPathResolver.ResolveDefault(@"C:\Users\Dev");

        path.Should().Be(@"C:\Users\Dev\.codex\config.toml");
    }
}
