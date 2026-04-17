using Microsoft.Extensions.Options;
using SimpleSqlServerMcp.Configuration;
using SimpleSqlServerMcp.Security;

namespace SimpleSqlServerMcp.UnitTests.Security;

public sealed class SqlPasswordResolverTests
{
    [Fact]
    public void ResolvePassword_ShouldReturnNull_WhenIntegratedSecurityIsEnabled()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = true,
            Password = "Secret123!",
            PasswordSecretName = "SimpleSqlServerMcp/SqlPassword/test",
        };
        FakeWindowsCredentialReader credentialReader = new("StoredSecret!");
        SqlPasswordResolver resolver = CreateResolver(options, new FakePlatformInfo(isWindows: true), credentialReader);

        // Act
        string? password = resolver.ResolvePassword();

        // Assert
        password.Should().BeNull();
        credentialReader.LastRequestedTarget.Should().BeNull();
    }

    [Fact]
    public void ResolvePassword_ShouldReturnInlinePassword_WhenNoSecretNameIsConfigured()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = false,
            Username = "sa",
            Password = "Secret123!",
        };
        SqlPasswordResolver resolver = CreateResolver(options, new FakePlatformInfo(isWindows: true), new FakeWindowsCredentialReader("StoredSecret!"));

        // Act
        string? password = resolver.ResolvePassword();

        // Assert
        password.Should().Be("Secret123!");
    }

    [Fact]
    public void ResolvePassword_ShouldPreferWindowsCredentialManagerSecret_WhenSecretNameIsConfigured()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = false,
            Username = "sa",
            Password = "InlineSecret!",
            PasswordSecretName = "SimpleSqlServerMcp/SqlPassword/test",
        };
        FakeWindowsCredentialReader credentialReader = new("StoredSecret!");
        SqlPasswordResolver resolver = CreateResolver(options, new FakePlatformInfo(isWindows: true), credentialReader);

        // Act
        string? password = resolver.ResolvePassword();

        // Assert
        password.Should().Be("StoredSecret!");
        credentialReader.LastRequestedTarget.Should().Be("SimpleSqlServerMcp/SqlPassword/test");
    }

    [Fact]
    public void ResolvePassword_ShouldThrow_WhenSecretNameIsConfiguredOnNonWindows()
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
        SqlPasswordResolver resolver = CreateResolver(options, new FakePlatformInfo(isWindows: false), new FakeWindowsCredentialReader("StoredSecret!"));

        // Act
        Action act = () => _ = resolver.ResolvePassword();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SQLSERVER_PASSWORD_SECRET_NAME*only on Windows*");
    }

    [Fact]
    public void ResolvePassword_ShouldThrow_WhenCredentialIsMissing()
    {
        // Arrange
        SqlServerMcpOptions options = new()
        {
            Host = ".",
            Database = "master",
            IntegratedSecurity = false,
            Username = "sa",
            PasswordSecretName = "SimpleSqlServerMcp/SqlPassword/missing",
        };
        SqlPasswordResolver resolver = CreateResolver(options, new FakePlatformInfo(isWindows: true), new FakeWindowsCredentialReader(secret: null));

        // Act
        Action act = () => _ = resolver.ResolvePassword();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SimpleSqlServerMcp/SqlPassword/missing*");
    }

    private static SqlPasswordResolver CreateResolver(
        SqlServerMcpOptions options,
        IPlatformInfo platformInfo,
        IWindowsCredentialReader credentialReader)
    {
        return new SqlPasswordResolver(
            Options.Create(options),
            platformInfo,
            credentialReader);
    }

    private sealed class FakePlatformInfo(bool isWindows) : IPlatformInfo
    {
        public bool IsWindows() => isWindows;
    }

    private sealed class FakeWindowsCredentialReader(string? secret) : IWindowsCredentialReader
    {
        public string? LastRequestedTarget { get; private set; }

        public string? ReadGenericCredential(string targetName)
        {
            LastRequestedTarget = targetName;
            return secret;
        }
    }
}
