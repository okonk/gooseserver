namespace Goose.Tests;

public class PropertiesDictionaryTests
{
    [Fact]
    public void Round_trips_through_JsonHelper_preserving_types()
    {
        var props = new PropertiesDictionary { ["dimension.max"] = 3, ["name"] = "abyss", ["on"] = true };

        var restored = JsonHelper.Deserialize<PropertiesDictionary>(JsonHelper.Serialize(props))!;

        // JSON integers come back as long; GetProperty<int> must narrow them.
        Assert.Equal(3, restored.GetProperty<int>("dimension.max"));
        Assert.Equal("abyss", restored.GetProperty<string>("name"));
        Assert.True(restored.GetProperty<bool>("on"));
    }

    [Fact]
    public void Missing_keys_use_the_default_or_throw()
    {
        var props = new PropertiesDictionary();

        Assert.Equal(0, props.GetProperty<int>("dimension.max", 0));
        Assert.False(props.TryGetProperty<int>("dimension.max", out _));
        Assert.Throws<KeyNotFoundException>(() => props.GetProperty<int>("dimension.max"));
    }

    [Fact]
    public void Clone_is_a_shallow_snapshot()
    {
        var props = new PropertiesDictionary { ["a"] = 1 };
        var copy = props.Clone();
        props["b"] = 2;

        Assert.Equal(1, copy.GetProperty<int>("a"));
        Assert.False(copy.ContainsKey("b"));
    }
}
