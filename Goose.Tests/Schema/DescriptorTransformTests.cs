using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class DescriptorTransformTests
{
    [Fact]
    public void Text_is_escaped_and_quotes_doubled()
    {
        Assert.Equal("'Bob''s Hat'",
            DescriptorTransform.Apply(Col.Text("item_name"), "Bob's Hat"));
    }

    [Fact]
    public void Numbers_pass_through_unquoted()
    {
        Assert.Equal("42", DescriptorTransform.Apply(Col.Int("stack", def: 1), "42"));
    }

    [Fact]
    public void Bool_is_quoted_like_text()
    {
        // Matches existing behaviour: booleans went through EscapeString.
        Assert.Equal("'1'", DescriptorTransform.Apply(Col.Bool("lore", def: false), "1"));
    }

    [Fact]
    public void Enum_name_becomes_its_integer_value()
    {
        Assert.Equal("1", DescriptorTransform.Apply(Col.Enum<Sample>("k"), "Second"));
    }

    private enum Sample { First = 0, Second = 1 }
}
