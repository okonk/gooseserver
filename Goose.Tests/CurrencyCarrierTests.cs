using Goose;
using Xunit;

namespace Goose.Tests;

public class CurrencyCarrierTests
{
    /// <summary>Null, not "gold". An unset item must be able to inherit its vendor's
    /// currency - a credit dealer's stock has no per-item override.</summary>
    [Fact]
    public void ItemTemplate_DefaultsToNullCurrency()
    {
        Assert.Null(new ItemTemplate().CurrencyId);
    }

    /// <summary>The copy constructor is how Dimensions.csx builds every clone. A field it
    /// forgets is a field the clones silently lose.</summary>
    [Fact]
    public void ItemTemplate_CopyConstructorCarriesCurrency()
    {
        var basic = new ItemTemplate { ID = 5, Name = "Sword", BaseStats = new AttributeSet(), CurrencyId = "spirit" };

        Assert.Equal("spirit", new ItemTemplate(basic).CurrencyId);
    }

    [Fact]
    public void NPCTemplate_DefaultsToNullCurrency()
    {
        Assert.Null(new NPCTemplate().CurrencyId);
    }

    [Fact]
    public void NPCTemplate_CopyConstructorCarriesCurrency()
    {
        var basic = new NPCTemplate { NPCTemplateID = 7, Name = "Merchant", CurrencyId = Currency.Credits };

        Assert.Equal(Currency.Credits, new NPCTemplate(basic).CurrencyId);
    }

    [Fact]
    public void NPC_ReadsCurrencyFromItsTemplate()
    {
        var npc = new NPC { NPCTemplate = new NPCTemplate { CurrencyId = Currency.Credits } };

        Assert.Equal(Currency.Credits, npc.CurrencyId);
    }
}
