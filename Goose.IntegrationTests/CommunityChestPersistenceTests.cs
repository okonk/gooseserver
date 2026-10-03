namespace Goose.IntegrationTests;

public class CommunityChestPersistenceTests : PlayerFirstSaveTestBase
{
    private const int Chest = 77;
    private const int BankNpc = 78;
    private const int Sword = 100;

    public CommunityChestPersistenceTests()
        : base(["players", "banks", "pets", "guilds", "world_state"],
               ["quest_requirements", "quest_rewards"])
    {
        settings.BankSlotsPerPage = 30;
        settings.CommunityChestPages = 3;
    }

    private static void RegisterSword(GameWorld world)
    {
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = Sword,
            Name = "Sword",
            Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
        });
    }

    private static Item SwordItem(GameWorld world, int itemId)
    {
        var item = new Item();
        item.LoadFromTemplate(world.ItemHandler.GetTemplate(Sword)!);
        item.ItemID = itemId;
        return item;
    }

    private string? WorldStateValue(string key)
    {
        return world.Database.Execute<string?>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT value FROM world_state WHERE key=@key;";
            cmd.Parameters.AddWithValue("@key", key);
            return cmd.ExecuteScalar() as string;
        });
    }

    private string? SingleValue(string sql)
    {
        return world.Database.Execute<string?>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar() as string;
        });
    }

    [Fact]
    public void ChestSurvivesWorldRestart()
    {
        RegisterSword(world);

        var container = world.ChestHandler.GetOrCreateContainer(world, Chest);
        container.SetSlot(3, new ItemSlot { Item = SwordItem(world, 9001) });
        world.WorldState.Save(world);
        world.Database.Execute(conn => { });

        var second = new GameWorld(this.settings);
        try
        {
            second.Database.Start(this.dbPath);
            RegisterSword(second);
            second.WorldState.Load(second.Database);
            second.ChestHandler.Load(second);

            var slot = second.ChestHandler.GetOrCreateContainer(second, Chest).GetSlot(3);
            Assert.NotNull(slot);
            Assert.Equal(Sword, slot!.Item.TemplateID);
            Assert.Contains(second.ItemHandler.GetItems(), i => i.ItemID == 9001);
        }
        finally
        {
            second.Database.Stop();
        }
    }

    [Fact]
    public void IndependentSaveCycles_EventuallyPersistTransfer()
    {
        RegisterSword(world);

        var container = world.ChestHandler.GetOrCreateContainer(world, Chest);
        container.SetSlot(3, new ItemSlot { Item = SwordItem(world, 9001) });
        world.WorldState.Save(world);
        world.Database.Execute(conn => { });

        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;
        player.Windows = new List<Window>();
        player.SaveToDatabase(world);
        world.Database.Execute(conn => { });

        string chestBefore = WorldStateValue("chest:" + Chest)!;
        string invBefore = SingleValue("SELECT serialized_data FROM inventory WHERE player_id=1")!;

        var map = new Map { ID = 1, Name = "Town", Width = 10, Height = 10 };
        player.Map = map;
        player.MapID = map.ID;
        player.MapX = 5;
        player.MapY = 5;
        var chestNpc = new NPC { NPCTemplateID = Chest, Map = map, MapID = map.ID, MapX = 5, MapY = 5 };

        CommunityChestWindow.Open(world, player, chestNpc);
        var chestWin = (CommunityChestWindow)player.Windows[0];
        chestWin.WindowToInventory(player, 3, 1, world);
        world.Database.Execute(conn => { });

        Assert.Null(container.GetSlot(3));
        Assert.NotNull(player.Inventory.GetSlot(1));
        Assert.Equal(chestBefore, WorldStateValue("chest:" + Chest));
        Assert.Equal(invBefore, SingleValue("SELECT serialized_data FROM inventory WHERE player_id=1"));

        // Chest and player rows are saved by independent timers, so between the two saves the persisted
        // rows can disagree (here the item is in neither row); eventual consistency, not crash atomicity.
        world.WorldState.Save(world);
        world.Database.Execute(conn => { });
        string chestAfter = WorldStateValue("chest:" + Chest)!;
        Assert.NotEqual(chestBefore, chestAfter);
        Assert.Equal(invBefore, SingleValue("SELECT serialized_data FROM inventory WHERE player_id=1"));

        player.SaveToDatabase(world);
        world.Database.Execute(conn => { });
        string invAfter = SingleValue("SELECT serialized_data FROM inventory WHERE player_id=1")!;
        Assert.NotEqual(invBefore, invAfter);

        var second = new GameWorld(this.settings);
        try
        {
            second.Database.Start(this.dbPath);
            RegisterSword(second);
            second.WorldState.Load(second.Database);
            second.ChestHandler.Load(second);

            Assert.Null(second.ChestHandler.GetOrCreateContainer(second, Chest).GetSlot(3));

            var invSlots = JsonHelper.Deserialize<ItemSlot[]>(invAfter)!;
            Assert.NotNull(invSlots[1]);
            Assert.Equal(Sword, invSlots[1]!.Item.TemplateID);
        }
        finally
        {
            second.Database.Stop();
        }
    }

    [Fact]
    public void BankToChest_PersistsOnExistingTimers()
    {
        RegisterSword(world);

        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;
        player.Windows = new List<Window>();
        player.NumberOfBankPages = 1;
        player.Bank.GetOrCreateContainer(player, BankNpc, this.settings.BankSlotsPerPage)
            .SetSlot(1, new ItemSlot { Item = SwordItem(world, 9002) });
        player.SaveToDatabase(world);
        world.Database.Execute(conn => { });

        var container = world.ChestHandler.GetOrCreateContainer(world, Chest);

        var map = new Map { ID = 1, Name = "Town", Width = 10, Height = 10 };
        player.Map = map;
        player.MapID = map.ID;
        player.MapX = 5;
        player.MapY = 5;
        var bankNpc = new NPC { NPCTemplateID = BankNpc, Map = map, MapID = map.ID, MapX = 5, MapY = 5 };
        var chestNpc = new NPC { NPCTemplateID = Chest, Map = map, MapID = map.ID, MapX = 5, MapY = 5 };

        BankWindow.Open(world, player, bankNpc);
        CommunityChestWindow.Open(world, player, chestNpc);
        var bankWin = player.Windows.OfType<BankWindow>().Single();
        var chestWin = player.Windows.OfType<CommunityChestWindow>().Single();

        string bankSql = "SELECT serialized_data FROM bank_items WHERE npc_id=" + BankNpc + " AND player_id=1";
        string bankBefore = SingleValue(bankSql)!;
        string? chestBefore = WorldStateValue("chest:" + Chest);
        Assert.Null(chestBefore);

        ItemContainerWindow.WindowToWindow(player, bankWin, 1, chestWin, 3, world);
        world.Database.Execute(conn => { });

        Assert.Null(player.Bank.Containers[BankNpc].GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.Equal(bankBefore, SingleValue(bankSql));
        Assert.Equal(chestBefore, WorldStateValue("chest:" + Chest));

        world.WorldState.Save(world);
        world.Database.Execute(conn => { });
        string chestAfter = WorldStateValue("chest:" + Chest)!;
        Assert.NotEqual(chestBefore, chestAfter);
        Assert.Equal(bankBefore, SingleValue(bankSql));

        player.SaveToDatabase(world);
        world.Database.Execute(conn => { });
        string bankAfter = SingleValue(bankSql)!;
        Assert.NotEqual(bankBefore, bankAfter);

        var second = new GameWorld(this.settings);
        try
        {
            second.Database.Start(this.dbPath);
            RegisterSword(second);
            second.WorldState.Load(second.Database);
            second.ChestHandler.Load(second);

            var slot = second.ChestHandler.GetOrCreateContainer(second, Chest).GetSlot(3);
            Assert.NotNull(slot);
            Assert.Equal(Sword, slot!.Item.TemplateID);
            Assert.Contains(second.ItemHandler.GetItems(), i => i.ItemID == 9002);

            var bankSlots = JsonHelper.Deserialize<ItemSlot[]>(bankAfter)!;
            Assert.Null(bankSlots[1]);
        }
        finally
        {
            second.Database.Stop();
        }
    }
}
