using Goose.Testing;

namespace Goose.IntegrationTests;

public class PetSlotItemScriptTests
{
    private const int ItemId = 901;

    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, Item Item) Setup(string parameters)
    {
        var fixture = new TestWorldFixture(s => { s.PetCountLimit = 5; s.PetSlotsMax = 30; });
        var map = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        map.AddPlayer(player, fixture.World);

        var template = ShippedItemTemplate(fixture);
        var item = new Item();
        item.LoadFromTemplate(template);
        item.ScriptParams = parameters;
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        player.Inventory.AddItem(item, 1, fixture.World);

        return (fixture, player, item);
    }

    private static ItemTemplate ShippedItemTemplate(TestWorldFixture fixture)
    {
        var template = fixture.AddBaseItemTemplate(ItemId, "Pet Slots", ItemTemplate.UseTypes.OneTime,
            t => t.StackSize = 99);
        var shipped = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ItemScripts", "PetSlotItem.csx"));
        template.Script = fixture.CompileItemScript(shipped, "PetSlotItem.csx");
        return template;
    }

    [Fact]
    public void An_item_with_no_parameters_grants_one_slot_and_is_consumed()
    {
        var (fixture, player, item) = Setup("");
        using var _ = fixture;

        player.Inventory.UseConsumable(item, fixture.World);

        Assert.Equal(1, player.PetSlotsBonus);
        Assert.Null(player.Inventory.GetSlot(1));
        Assert.Contains(player.Sent, s => s.Contains(P.ServerMessage("You can now keep up to 6 pets.")));
    }

    [Fact]
    public void Slots_are_clamped_to_the_available_room()
    {
        var (fixture, player, item) = Setup("5");
        using var _ = fixture;
        player.PetSlotsBonus = fixture.World.Settings.PetSlotsMax - fixture.World.Settings.PetCountLimit - 2;

        player.Inventory.UseConsumable(item, fixture.World);

        Assert.Equal(fixture.World.Settings.PetSlotsMax - fixture.World.Settings.PetCountLimit, player.PetSlotsBonus);
        Assert.Equal(fixture.World.Settings.PetSlotsMax, player.MaxPets(fixture.World.Settings));
        Assert.Null(player.Inventory.GetSlot(1));
    }

    [Fact]
    public void An_item_at_max_slots_is_kept()
    {
        var (fixture, player, item) = Setup("5");
        using var _ = fixture;
        player.PetSlotsBonus = fixture.World.Settings.PetSlotsMax - fixture.World.Settings.PetCountLimit;

        player.Inventory.UseConsumable(item, fixture.World);

        Assert.Same(item, player.Inventory.GetSlot(1)!.Item);
        Assert.Equal(fixture.World.Settings.PetSlotsMax - fixture.World.Settings.PetCountLimit, player.PetSlotsBonus);
        Assert.Contains(player.Sent, s => s.Contains(P.ServerMessage("You already have the maximum number of pet slots (30).")));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-3")]
    public void An_item_with_an_invalid_parameter_is_kept(string parameters)
    {
        var (fixture, player, item) = Setup(parameters);
        using var _ = fixture;

        player.Inventory.UseConsumable(item, fixture.World);

        Assert.Same(item, player.Inventory.GetSlot(1)!.Item);
        Assert.Equal(0, player.PetSlotsBonus);
        Assert.Contains(player.Sent, s => s.Contains(P.ServerMessage("This item has no valid pet slots configured.")));
    }
}
