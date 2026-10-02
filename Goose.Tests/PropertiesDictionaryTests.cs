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

    [Fact]
    public void String_lists_survive_the_json_round_trip()
    {
        var props = new PropertiesDictionary { ["titles"] = new List<string> { "Lord", "Lady" } };

        var restored = JsonHelper.Deserialize<PropertiesDictionary>(JsonHelper.Serialize(props))!;

        Assert.Equal(new List<string> { "Lord", "Lady" }, restored.GetProperty<List<string>>("titles"));
    }

    [Fact]
    public void Object_lists_convert_to_the_requested_element_type()
    {
        var props = new PropertiesDictionary { ["counts"] = new List<object?> { 1L, 2L } };

        Assert.Equal(new List<int> { 1, 2 }, props.GetProperty<List<int>>("counts"));
    }

    [Fact]
    public void A_string_is_not_read_as_a_list_of_characters()
    {
        var props = new PropertiesDictionary { ["name"] = "abyss" };

        Assert.Throws<InvalidCastException>(() => props.GetProperty<List<string>>("name"));
    }

    [Fact]
    public void The_returned_list_is_a_copy_of_the_stored_one()
    {
        var stored = new List<string> { "Lord" };
        var props = new PropertiesDictionary { ["titles"] = stored };

        var read = props.GetProperty<List<string>>("titles");
        read.Add("Mutated");

        Assert.Single(stored);
        Assert.Single(props.GetProperty<List<string>>("titles"));
    }
}
