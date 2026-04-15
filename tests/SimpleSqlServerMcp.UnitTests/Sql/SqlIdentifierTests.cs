using SimpleSqlServerMcp.Sql;

namespace SimpleSqlServerMcp.UnitTests.Sql;

public sealed class SqlIdentifierTests
{
    [Fact]
    public void Quote_ShouldWrapIdentifierInBrackets()
    {
        // Arrange

        // Act
        string result = SqlIdentifier.Quote("Users");

        // Assert
        result.Should().Be("[Users]");
    }

    [Fact]
    public void Quote_ShouldEscapeClosingBracket()
    {
        // Arrange

        // Act
        string result = SqlIdentifier.Quote("User]Archive");

        // Assert
        result.Should().Be("[User]]Archive]");
    }

    [Fact]
    public void Quote_ShouldThrow_ForWhitespaceIdentifier()
    {
        // Arrange
        Action action = () => SqlIdentifier.Quote(" ");

        // Act

        // Assert
        action.Should().Throw<ArgumentException>();
    }
}
