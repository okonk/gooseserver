using Goose;
using Goose.Testing;

namespace Goose.Tests;

public class CommunityChestSyncTests
{
    private const int ChestAId = 77;
    private const int ChestBId = 78;
    private const int BankerId = 79;
    private const int Sword = 100;
    private const int Axe = 101;
    private const int Potion = 200;

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

    private static NPC SpawnBanker(TestWorldFixture fixture, int templateId)
    {
        var template = new NPCTemplate
        {
            NPCTemplateID = templateId, Name = "Banker " + templateId, Level = 50, ClassID = 0,
            NPCType = NPCTemplate.Types.Banker,
            BaseStats = new AttributeSet(),
        };
        fixture.World.NPCHandler.AddTemplate(template);
        return fixture.World.NPCHandler.SpawnNPC(
            fixture.World, 1, 5, 5, template, shouldRespawn: true)!;
    }

    private static (TestWorldFixture Fixture, Map Map, NPC ChestA, NPC ChestB) Setup()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town");
        var chestA = SpawnChest(fixture, ChestAId);
        var chestB = SpawnChest(fixture, ChestBId);
        return (fixture, map, chestA, chestB);
    }

    private static TestWorldFixture.CapturingPlayer OnlinePlayer(TestWorldFixture fixture, Map map, string name)
    {
        var player = fixture.CommandPlayerOn(map, 5, 5, name);
        player.Windows = new List<Window>();
        fixture.AddOnlinePlayer(player);
        return player;
    }

    private static CommunityChestWindow ChestWindow(Player player) =>
        (CommunityChestWindow)player.Windows.OfType<CommunityChestWindow>().Single();

    private static List<string> SlotPackets(TestWorldFixture.CapturingPlayer player, int windowId)
    {
        string gws = "GWS" + windowId + "|";
        string gwc = "GWC" + windowId + ",";
        return player.Sent.Where(p => p.StartsWith(gws) || p.StartsWith(gwc)).ToList();
    }

    [Fact]
    public void Withdraw_PushesOnePacketToActorAndViewer()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        var b = OnlinePlayer(fixture, map, "B");

        CommunityChestWindow.Open(fixture.World, a, chestA);
        CommunityChestWindow.Open(fixture.World, b, chestA);
        var aWin = a.Windows[0];
        var bWin = b.Windows[0];

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        a.Sent.Clear();
        b.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "WTI" + aWin.ID + ",3,1"));

        var aPackets = SlotPackets(a, aWin.ID);
        var bPackets = SlotPackets(b, bWin.ID);
        Assert.Single(aPackets);
        Assert.StartsWith("GWC" + aWin.ID + ",3", aPackets[0]);
        Assert.Single(bPackets);
        Assert.StartsWith("GWC" + bWin.ID + ",3", bPackets[0]);
        Assert.Null(container.GetSlot(3));
        Assert.NotNull(a.Inventory.GetSlot(1));
    }

    [Fact]
    public void StackMerge_StillPushes()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Potion, "Potion", ItemTemplate.UseTypes.NoUse, t => t.StackSize = 5);
        var a = OnlinePlayer(fixture, map, "A");
        var b = OnlinePlayer(fixture, map, "B");

        CommunityChestWindow.Open(fixture.World, a, chestA);
        CommunityChestWindow.Open(fixture.World, b, chestA);
        var bWin = b.Windows[0];

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Potion), Stack = 1 });
        a.Inventory.SetSlot(1, new ItemSlot { Item = LoadItem(fixture, Potion), Stack = 1 });
        a.Sent.Clear();
        b.Sent.Clear();

        Assert.True(fixture.RunCommand(a, "ITW1," + a.Windows[0].ID + ",3"));

        var bPackets = SlotPackets(b, bWin.ID);
        Assert.Single(bPackets);
        Assert.StartsWith("GWS" + bWin.ID + "|3|", bPackets[0]);
        Assert.Equal(2, container.GetSlot(3)!.Stack);
    }

    [Fact]
    public void Navigation_ChangesPageAndRendersVisibleSlots()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(Axe, "Axe", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chestA);
        var win = ChestWindow(a);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        container.SetSlot(fixture.Settings.BankSlotsPerPage + 3, new ItemSlot { Item = LoadItem(fixture, Axe) });

        a.Sent.Clear();
        Assert.True(fixture.RunCommand(a, "WBC4," + win.ID + ",0,0,0"));
        Assert.Equal(2, win.CurrentPage);
        Assert.Equal("Community Chest Page 2/3", win.Title);
        Assert.Equal("0,1,1,1,0", win.Buttons);
        Assert.Contains(a.Sent, p => p.StartsWith("MKW" + win.ID + ",30,Community Chest Page 2/3"));
        Assert.Equal(1, a.Sent.Count(p => p.StartsWith("GWS" + win.ID + "|3|") && p.Contains("Axe")));
        Assert.Equal(0, a.Sent.Count(p => p.StartsWith("GWS" + win.ID + "|") && p.Contains("Sword")));

        a.Sent.Clear();
        Assert.True(fixture.RunCommand(a, "WBC3," + win.ID + ",0,0,0"));
        Assert.Equal(1, win.CurrentPage);
        Assert.Equal("0,1,0,1,0", win.Buttons);
        Assert.Equal(1, a.Sent.Count(p => p.StartsWith("GWS" + win.ID + "|3|") && p.Contains("Sword")));
        Assert.Equal(0, a.Sent.Count(p => p.StartsWith("GWS" + win.ID + "|") && p.Contains("Axe")));

        a.Sent.Clear();
        Assert.True(fixture.RunCommand(a, "WBC3," + win.ID + ",0,0,0"));
        Assert.Equal(1, win.CurrentPage);
        Assert.DoesNotContain(a.Sent, p => p.StartsWith("MKW"));

        Assert.True(fixture.RunCommand(a, "WBC4," + win.ID + ",0,0,0"));
        Assert.True(fixture.RunCommand(a, "WBC4," + win.ID + ",0,0,0"));
        Assert.Equal(3, win.CurrentPage);
        a.Sent.Clear();
        Assert.True(fixture.RunCommand(a, "WBC4," + win.ID + ",0,0,0"));
        Assert.Equal(3, win.CurrentPage);
        Assert.DoesNotContain(a.Sent, p => p.StartsWith("MKW"));
    }

    [Fact]
    public void PageTwoViewer_GetsNothingForPageOneChange()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        var b = OnlinePlayer(fixture, map, "B");

        CommunityChestWindow.Open(fixture.World, a, chestA);
        CommunityChestWindow.Open(fixture.World, b, chestA);
        var aWin = a.Windows[0];
        var bWin = ChestWindow(b);

        Assert.True(fixture.RunCommand(b, "WBC4," + bWin.ID + ",0,0,0"));
        Assert.Equal(2, bWin.CurrentPage);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        a.Sent.Clear();
        b.Sent.Clear();
        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });

        Assert.Empty(b.Sent);
        Assert.Single(SlotPackets(a, aWin.ID));
    }

    [Fact]
    public void PageTwoChange_UpdatesOnlyPageTwoAtRelativeSlot()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Axe, "Axe", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        var b = OnlinePlayer(fixture, map, "B");

        CommunityChestWindow.Open(fixture.World, a, chestA);
        CommunityChestWindow.Open(fixture.World, b, chestA);
        var aWin = a.Windows[0];
        var bWin = ChestWindow(b);

        Assert.True(fixture.RunCommand(b, "WBC4," + bWin.ID + ",0,0,0"));

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        a.Sent.Clear();
        b.Sent.Clear();
        container.SetSlot(fixture.Settings.BankSlotsPerPage + 3, new ItemSlot { Item = LoadItem(fixture, Axe) });

        Assert.Empty(a.Sent);
        var bPackets = SlotPackets(b, bWin.ID);
        Assert.Single(bPackets);
        Assert.StartsWith("GWS" + bWin.ID + "|3|", bPackets[0]);
        Assert.Contains("Axe", bPackets[0]);
    }

    [Fact]
    public void ViewerWindowClosed_RemovesImmediately()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        var b = OnlinePlayer(fixture, map, "B");

        CommunityChestWindow.Open(fixture.World, a, chestA);
        CommunityChestWindow.Open(fixture.World, b, chestA);
        var bWin = b.Windows[0];

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        Assert.Equal(2, fixture.World.ChestHandler.Viewers[container].Count);

        bWin.Clicked(Window.ButtonTypes.Exit, 0, 0, 0, b, fixture.World);
        Assert.DoesNotContain(b.Windows, w => w == bWin);
        var remaining = Assert.Single(fixture.World.ChestHandler.Viewers[container]);
        Assert.Same(a, remaining.Player);

        b.Sent.Clear();
        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        Assert.Empty(b.Sent);
    }

    [Fact]
    public void RepeatedOpen_DoesNotGrowViewerRegistry()
    {
        var (fixture, map, chestA, _) = Setup();
        var a = OnlinePlayer(fixture, map, "A");

        for (int i = 0; i < 100; i++)
            CommunityChestWindow.Open(fixture.World, a, chestA);

        Assert.Single(a.Windows);
        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        var tuple = Assert.Single(fixture.World.ChestHandler.Viewers[container]);
        Assert.Same(a.Windows[0], tuple.Window);
    }

    [Fact]
    public void Logout_PrunesViewer()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        fixture.World.ChestHandler.Load(fixture.World);
        var a = OnlinePlayer(fixture, map, "A");
        var b = OnlinePlayer(fixture, map, "B");

        CommunityChestWindow.Open(fixture.World, a, chestA);
        CommunityChestWindow.Open(fixture.World, b, chestA);

        var container = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        fixture.World.PlayerHandler.RemovePlayer(b);
        Assert.DoesNotContain(fixture.World.ChestHandler.Viewers[container], v => v.Player == b);

        b.Sent.Clear();
        container.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        Assert.Empty(b.Sent);
    }

    [Fact]
    public void BankAndChestCoexist()
    {
        var (fixture, map, chestA, _) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");
        a.Bank = new PlayerBank();
        a.NumberOfBankPages = 1;
        var bankNpc = SpawnBanker(fixture, BankerId);

        BankWindow.Open(fixture.World, a, bankNpc);
        CommunityChestWindow.Open(fixture.World, a, chestA);
        Assert.Equal(2, a.Windows.Count);
        var bankWin = a.Windows.Single(w => w.Type == Window.WindowTypes.Bank);
        var chestWin = a.Windows.Single(w => w.Type == Window.WindowTypes.CommunityChest);

        var chestContainer = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        a.Sent.Clear();
        chestContainer.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        Assert.Equal(1, a.Sent.Count(p => p.StartsWith("GWS" + chestWin.ID + "|")));
        Assert.DoesNotContain(a.Sent, p => p.StartsWith("SBS"));

        a.Inventory.SetSlot(1, new ItemSlot { Item = LoadItem(fixture, Sword) });
        a.Sent.Clear();
        Assert.True(fixture.RunCommand(a, "ITW1," + bankWin.ID + ",3"));
        Assert.Equal(1, a.Sent.Count(p => p.StartsWith("SBS")));
        Assert.DoesNotContain(a.Sent, p => p.StartsWith("GWS"));
    }

    [Fact]
    public void OpenSecondChest_ReplacesFirst()
    {
        var (fixture, map, chestA, chestB) = Setup();
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");

        CommunityChestWindow.Open(fixture.World, a, chestA);
        var winA = a.Windows[0];
        CommunityChestWindow.Open(fixture.World, a, chestB);
        Assert.DoesNotContain(a.Windows, w => w == winA);

        var containerA = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestAId);
        var containerB = fixture.World.ChestHandler.GetOrCreateContainer(fixture.World, ChestBId);
        Assert.False(fixture.World.ChestHandler.Viewers.ContainsKey(containerA));
        var tuple = Assert.Single(fixture.World.ChestHandler.Viewers[containerB]);
        var winB = a.Windows[0];
        Assert.Same(winB, tuple.Window);

        a.Sent.Clear();
        containerB.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        Assert.Single(SlotPackets(a, winB.ID));

        a.Sent.Clear();
        containerA.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, Sword) });
        Assert.Empty(a.Sent);
    }

    [Fact]
    public void OutOfRangeAbsoluteSlot_Refused()
    {
        var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        var map = fixture.AddBaseMap(1, "Town");
        var chest = SpawnChest(fixture, ChestAId);
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var a = OnlinePlayer(fixture, map, "A");

        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = ChestWindow(a);
        win.CurrentPage = 2;

        a.Inventory.SetSlot(1, new ItemSlot { Item = LoadItem(fixture, Sword) });
        a.Sent.Clear();
        Assert.True(fixture.RunCommand(a, "ITW1," + win.ID + ",5"));

        Assert.NotNull(a.Inventory.GetSlot(1));
        Assert.Empty(SlotPackets(a, win.ID));
    }

    [Fact]
    public void OverflowPagesReachable()
    {
        var fixture = new TestWorldFixture(s => s.CommunityChestPages = 1);
        fixture.AddBaseItemTemplate(Sword, "Sword", ItemTemplate.UseTypes.NoUse);
        var map = fixture.AddBaseMap(1, "Town");
        var chest = SpawnChest(fixture, ChestAId);

        var blob = new ItemSlot[40];
        blob[39] = new ItemSlot { Item = new Item { ItemID = 9001, TemplateID = Sword, Name = "Sword" } };
        fixture.World.WorldState.LoadRows(new[]
        {
            new KeyValuePair<string, string>("chest:" + ChestAId, JsonHelper.Serialize(blob)),
        });
        fixture.World.ChestHandler.Load(fixture.World);

        var a = OnlinePlayer(fixture, map, "A");
        CommunityChestWindow.Open(fixture.World, a, chest);
        var win = ChestWindow(a);
        Assert.Equal(2, win.MaxPages);

        a.Sent.Clear();
        Assert.True(fixture.RunCommand(a, "WBC4," + win.ID + ",0,0,0"));
        Assert.Equal(2, win.CurrentPage);
        Assert.Equal(1, a.Sent.Count(p => p.StartsWith("GWS" + win.ID + "|9|") && p.Contains("Sword")));
    }
}
