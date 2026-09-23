using Goose.Testing;

namespace Goose.IntegrationTests;

public class HairDyePotionScriptTests
{
    private const int PotionId = 900;

    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, Item Potion) Setup(string parameters)
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        map.AddPlayer(player, fixture.World);

        var template = ShippedPotionTemplate(fixture);
        var potion = new Item();
        potion.LoadFromTemplate(template);
        potion.ScriptParams = parameters;
        fixture.World.ItemHandler.AddAndAssignId(potion, fixture.World);
        player.Inventory.AddItem(potion, 1, fixture.World);

        return (fixture, player, potion);
    }

    private static ItemTemplate ShippedPotionTemplate(TestWorldFixture fixture)
    {
        var template = fixture.AddBaseItemTemplate(PotionId, "Hair Dye", ItemTemplate.UseTypes.OneTime,
            t => t.StackSize = 99);
        var shipped = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ItemScripts", "HairDyePotion.csx"));
        template.Script = fixture.CompileItemScript(shipped, "HairDyePotion.csx");
        return template;
    }

    private static ItemSlot? FindPotion(Player player, string name)
    {
        for (int i = 1; i <= 30; i++)
        {
            var slot = player.Inventory.GetSlot(i);
            if (slot is not null && slot.Item.Name == name) return slot;
        }

        return null;
    }

    [Fact]
    public void A_potion_bought_with_hairdye_create_dyes_the_hair()
    {
        var fixture = new TestWorldFixture(s => { s.HairdyeCommandCost = 5000; s.HairDyePotionId = PotionId; });
        using var _ = fixture;
        var map = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        map.AddPlayer(player, fixture.World);
        ShippedPotionTemplate(fixture);
        player.Gold = 5000;

        fixture.RunCommand(player, "/hairdye create 9 8 7 200 Crimson");

        var potion = FindPotion(player, "Crimson");
        Assert.NotNull(potion);
        Assert.Equal(5, potion!.Stack);

        player.Inventory.UseConsumable(potion.Item, fixture.World);

        Assert.Equal(9, player.HairR);
        Assert.Equal(8, player.HairG);
        Assert.Equal(7, player.HairB);
        Assert.Equal(200, player.HairA);
        Assert.Equal(4, FindPotion(player, "Crimson")!.Stack);
    }

    [Fact]
    public void Using_a_potion_dyes_the_hair_and_consumes_it()
    {
        var (fixture, player, potion) = Setup("12,34,56,78");
        using var _ = fixture;

        player.Inventory.UseConsumable(potion, fixture.World);

        Assert.Equal(12, player.HairR);
        Assert.Equal(34, player.HairG);
        Assert.Equal(56, player.HairB);
        Assert.Equal(78, player.HairA);
        Assert.Contains(player.Sent, s => s.Contains(P.UpdateCharacter(player)));
        Assert.Null(player.Inventory.GetSlot(1));
    }

    [Fact]
    public void The_dye_reaches_players_in_range()
    {
        var (fixture, player, potion) = Setup("1,2,3,4");
        using var _ = fixture;

        var watcher = fixture.CommandPlayerOn(player.Map, 6, 5, "Watcher");
        player.Map.AddPlayer(watcher, fixture.World);

        player.Inventory.UseConsumable(potion, fixture.World);

        Assert.Contains(watcher.Sent, s => s.Contains(P.UpdateCharacter(player)));
    }

    [Fact]
    public void A_potion_with_a_damaged_colour_is_kept()
    {
        var (fixture, player, potion) = Setup("255,0,0");
        using var _ = fixture;

        player.Inventory.UseConsumable(potion, fixture.World);

        Assert.Same(potion, player.Inventory.GetSlot(1)!.Item);
        Assert.Equal(0, player.HairA);
        Assert.DoesNotContain(player.Sent, s => s.Contains(P.UpdateCharacter(player)));
    }

    [Fact]
    public void An_out_of_range_colour_is_kept()
    {
        var (fixture, player, potion) = Setup("256,0,0,255");
        using var _ = fixture;

        player.Inventory.UseConsumable(potion, fixture.World);

        Assert.Same(potion, player.Inventory.GetSlot(1)!.Item);
        Assert.Equal(0, player.HairR);
    }
}
