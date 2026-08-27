namespace Goose.Tests;

public class ItemHandlerGetGoldTests
{
    [Fact]
    public void GetGold_returns_null_when_gold_item_was_never_created()
    {
        var world = new GameWorld(new GooseSettings());

        Assert.Null(world.ItemHandler.GetGold(world));
    }

    [Fact]
    public void GetGold_returns_registered_gold_item()
    {
        var world = new GameWorld(new GooseSettings());
        var gold = new Item { ItemID = world.Settings.ItemIDStartpoint + world.Settings.GoldItemID };
        world.ItemHandler.AddItem(gold, world);

        Assert.Same(gold, world.ItemHandler.GetGold(world));
    }
}
