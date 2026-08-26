using Goose;
using Goose.Tests.Fakes;
using Xunit;

namespace Goose.Tests;

public class DataReaderExtensionsTests
{
    private static FakeDbDataReader NewReader(Dictionary<string, object> values) => new(values);

    [Fact]
    public void GetInt32_ReturnsValue()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = 7 });

        Assert.Equal(7, reader.GetInt32("col"));
    }

    [Fact]
    public void GetInt32_OnDBNull_ThrowsInvalidCastException()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = DBNull.Value });

        Assert.Throws<InvalidCastException>(() => reader.GetInt32("col"));
    }

    [Fact]
    public void GetInt32_OnNullCell_ReturnsZero()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = null! });

        Assert.Equal(0, reader.GetInt32("col"));
    }

    [Fact]
    public void GetInt32_OnTextCell_Parses()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = "42" });

        Assert.Equal(42, reader.GetInt32("col"));
    }

    [Fact]
    public void GetString_ReturnsValue()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = "hello" });

        Assert.Equal("hello", reader.GetString("col"));
    }

    [Fact]
    public void GetString_OnDBNull_ReturnsEmptyString()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = DBNull.Value });

        Assert.Equal(string.Empty, reader.GetString("col"));
    }

    [Fact]
    public void GetString_OnNullCell_ReturnsEmptyString()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = null! });

        Assert.Equal(string.Empty, reader.GetString("col"));
    }

    [Fact]
    public void GetInt64_ReturnsValue()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = 42L });

        Assert.Equal(42L, reader.GetInt64("col"));
    }

    [Fact]
    public void GetDecimal_ReturnsValue()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = 4.5m });

        Assert.Equal(4.5m, reader.GetDecimal("col"));
    }

    [Fact]
    public void GetDouble_ReturnsValue()
    {
        var reader = NewReader(new Dictionary<string, object> { ["col"] = 3.5d });

        Assert.Equal(3.5d, reader.GetDouble("col"));
    }
}
