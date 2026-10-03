using Goose;
using Goose.Testing;

namespace Goose.Tests;

public class CommunityChestValidationTests
{
    private const int ChestId = 77;
    private const int Sword = 100;
    private const int BopItem = 101;
    private const int LoreItem = 102;
    private const int ScriptedItem = 103;

    private const string DimensionRefusalBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseItemScript
        {
            public override string? CanPickup(Player player, Item item, GameWorld world)
            {
                return "Not in this dimension.";
            }
        }

        return typeof(T);
        """;

    private const string ThrowingBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseItemScript
        {
            public override string? CanPickup(Player player, Item item, GameWorld world)
            {
                throw new Exception("boom");
            }
        }

        return typeof(T);
        """;

    private static Item LoadItem(TestWorldFixture fixture, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(templateId)!);
        return item;
    }

    private static NPC SpawnChest(TestWorldFixture fixture, int templateId)
    {
        var template = new NPCTemplate
        {
            NPCTemplateID = templateId, Name = "Chest " + templateId, Level = 50, ClassID = 0,
            BaseStats = new AttributeSet(),
        };
        fixture.World.NPCHandler.AddTemplate(template);
        return fixture.World.NPCHandler.SpawnNPC(
            fixture.World, 1, 5, 5, template, shouldRespawn: true,
            new PropertiesDictionary { ["communityChest"] = true })!;
    }

    private static (TestWorldFixture Fixture, Map Map, NPC Chest) Setup()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town");
        var chest = SpawnChest(fixture, ChestId);
        return (fixture, map, chest);
    }

    private static TestWorldFixture.CapturingPlayer OnlinePlayer(TestWorldFixture fixture, Map map, string name)
    {
        var player = fixture.CommandPlayerOn(map, 5, 5, name);
        player.Bank = new PlayerBank();
        player.Windows = new List<Window>();
        fixture.AddOnlinePlayer(player);
        return player;
    }

    private static bool MessageSent(TestWorldFixture.CapturingPlayer player, string text) =>
        player.Sent.Any(p => p.StartsWith(P.ServerMessage(text)));

    [Fact]
    public void DepositBound_Refused()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];

        var bound = LoadItem(fixture, Sword);
        bound.IsBound = true;
        a.Inventory.SetSlot(1, new ItemSlot { Item = bound });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "ITW1," + win.ID + ",3"));

        Assert.NotNull(a.Inventory.GetSlot(1));
        Assert.Null(fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId).GetSlot(3));
        Assert.True(MessageSent(a, "That item is bound to you."));
    }

    [Fact]
    public void DepositBindOnPickup_Refused()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(BopItem, "Bop", ItemTemplate.UseTypes.NoUse, t => t.IsBindOnPickup = true);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];

        a.Inventory.SetSlot(1, new ItemSlot { Item = LoadItem(fixture, BopItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "ITW1," + win.ID + ",3"));

        Assert.NotNull(a.Inventory.GetSlot(1));
        Assert.Null(fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId).GetSlot(3));
        Assert.True(MessageSent(a, "That item is bound to you."));
    }

    [Fact]
    public void DepositBound_OntoOccupiedChestSlot_Refused()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        var bound = LoadItem(fixture, Sword);
        bound.IsBound = true;
        a.Inventory.SetSlot(1, new ItemSlot { Item = bound });
        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "ITW1," + win.ID + ",3"));

        Assert.NotNull(a.Inventory.GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.True(MessageSent(a, "That item is bound to you."));
    }

    [Fact]
    public void WithdrawBound_Refused()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        var bound = LoadItem(fixture, Sword);
        bound.IsBound = true;
        container.SetSlot(3, new ItemSlot { Item = bound });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.Null(a.Inventory.GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.True(MessageSent(a, "That item is bound."));
    }

    [Fact]
    public void WithdrawUnboundBindOnPickup_Refused()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(BopItem, "Bop", ItemTemplate.UseTypes.NoUse, t => t.IsBindOnPickup = true);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, BopItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.Null(a.Inventory.GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.True(MessageSent(a, "That item is bound to you."));
    }

    [Fact]
    public void WithdrawLore_RefusedWhenHeldInInventory()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(LoreItem, "LoreSword", ItemTemplate.UseTypes.NoUse, t => t.IsLore = true);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, LoreItem) });
        a.Inventory.SetSlot(5, new ItemSlot { Item = LoadItem(fixture, LoreItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.Null(a.Inventory.GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.True(MessageSent(a, "Already have LORE item LoreSword."));
    }

    [Fact]
    public void WithdrawLore_RefusedWhenHeldInBank()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(LoreItem, "LoreSword", ItemTemplate.UseTypes.NoUse, t => t.IsLore = true);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, LoreItem) });
        a.NumberOfBankPages = 1;
        a.Bank.GetOrCreateContainer(a, 1, 30).SetSlot(1, new ItemSlot { Item = LoadItem(fixture, LoreItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.Null(a.Inventory.GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.True(MessageSent(a, "Already have LORE item LoreSword."));
    }

    [Fact]
    public void WithdrawLore_AllowedWhenNotHeld()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(LoreItem, "LoreSword", ItemTemplate.UseTypes.NoUse, t => t.IsLore = true);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, LoreItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.NotNull(a.Inventory.GetSlot(1));
        Assert.Null(container.GetSlot(3));
    }

    [Fact]
    public void WithdrawLore_SwappingOntoJunk_Refused()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(LoreItem, "LoreSword", ItemTemplate.UseTypes.NoUse, t => t.IsLore = true);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, LoreItem) });
        a.Inventory.SetSlot(1, new ItemSlot { Item = LoadItem(fixture, Sword) });
        a.Inventory.SetSlot(5, new ItemSlot { Item = LoadItem(fixture, LoreItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.NotNull(container.GetSlot(3));
        Assert.Equal("Sword", a.Inventory.GetSlot(1)!.Item.Name);
        Assert.True(MessageSent(a, "Already have LORE item LoreSword."));
    }

    [Fact]
    public void WithdrawDimensionRefusal_ForwardsScriptMessage()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(ScriptedItem, "Scripted", ItemTemplate.UseTypes.NoUse,
            t => t.Script = fixture.CompileItemScript(DimensionRefusalBody, "Dim.csx"));
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, ScriptedItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.Null(a.Inventory.GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.True(MessageSent(a, "Not in this dimension."));
    }

    [Fact]
    public void Withdraw_ScriptThrows_RefusesClosed()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(ScriptedItem, "Scripted", ItemTemplate.UseTypes.NoUse,
            t => t.Script = fixture.CompileItemScript(ThrowingBody, "Throw.csx"));
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = a.Windows[0];
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, ScriptedItem) });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + win.ID + ",3,1"));

        Assert.Null(a.Inventory.GetSlot(1));
        Assert.NotNull(container.GetSlot(3));
        Assert.True(MessageSent(a, "You cannot pick that up right now."));
    }

    [Fact]
    public void CombineToChest_RulesRun()
    {
        var (fixture, map, chest) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        CombineBagWindow.Open(fixture.World, a);
        var chestWin = a.Windows.OfType<CommunityChestWindow>().Single();
        var bagWin = a.Windows.OfType<CombineBagWindow>().Single();
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestId);

        var bound = LoadItem(fixture, Sword);
        bound.IsBound = true;
        a.Inventory.GetCombineBagContainer().SetSlot(1, new ItemSlot { Item = bound });
        a.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTW" + bagWin.ID + ",1," + chestWin.ID + ",3"));

        Assert.NotNull(a.Inventory.GetCombineBagContainer().GetSlot(1));
        Assert.Null(container.GetSlot(3));
        Assert.True(MessageSent(a, "That item is bound to you."));
    }
}
