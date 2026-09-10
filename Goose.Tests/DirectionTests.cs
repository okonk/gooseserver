namespace Goose.Tests;

public class DirectionTests
{
    [Theory]
    [InlineData(Direction.Up, 1)]
    [InlineData(Direction.Right, 2)]
    [InlineData(Direction.Down, 3)]
    [InlineData(Direction.Left, 4)]
    public void Values_match_server_protocol(Direction direction, int value)
    {
        Assert.Equal(value, (int)direction);
    }
}
