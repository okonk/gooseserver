using Goose;
using Goose.Testing;

namespace Goose.Tests;

public class ItemContainerWindowSeamTests
{
    private const int StackableTemplate = 700;
    private const int OtherTemplate = 701;
    private const int BankNpcId = 100;

    private class SeamWindow : ItemContainerWindow
    {
        public List<(int index, ItemSlot? prior, ItemSlot? next)> Events { get; } = [];
        public List<int> SentSlots { get; } = [];

        public SeamWindow(ItemContainer container)
        {
            this.ItemContainer = container;
            container.SlotChanged += (index, prior, next) => this.Events.Add((index, prior, next));
        }

        public override void SendSlot(int slotIndex, Player player, GameWorld world)
        {
            this.SentSlots.Add(slotIndex);
        }
    }

    private class NoDepositWindow : SeamWindow
    {
        public NoDepositWindow(ItemContainer container) : base(container) { }

        public override bool CanDeposit(Player player, ItemSlot? incoming, GameWorld world) => false;
    }

    private class NoWithdrawWindow : SeamWindow
    {
        public NoWithdrawWindow(ItemContainer container) : base(container) { }

        public override bool CanWithdraw(Player player, ItemSlot? outgoing, GameWorld world) => false;
    }

    private static ItemSlot MakeSlot(TestWorldFixture fixture, int templateId, long stack = 1)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(templateId)!);
        return new ItemSlot { Item = item, Stack = stack };
    }

    private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, BankWindow bank) BankWorld()
    {
        var fixture = new TestWorldFixture();
        fixture.AddBaseItemTemplate(StackableTemplate, "Potion", ItemTemplate.UseTypes.NoUse,
            t => t.StackSize = 100);
        fixture.AddBaseItemTemplate(OtherTemplate, "Axe", ItemTemplate.UseTypes.NoUse);

        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        player.NumberOfBankPages = 1;
        player.Bank = new PlayerBank();

        var npc = new NPC { Map = map, MapID = map.ID, MapX = 5, MapY = 5, NPCTemplateID = BankNpcId };
        BankWindow.Open(fixture.World, player, npc);
        var bank = (BankWindow)player.Windows.Single(w => w.Type == Window.WindowTypes.Bank);

        return (fixture, player, bank);
    }

    [Fact]
    public void BankDrag_StillSendsWindowSlotDirectly()
    {
        var (fixture, player, bank) = BankWorld();
        using (fixture)
        {
            player.Inventory.SetSlot(1, MakeSlot(fixture, StackableTemplate));

            fixture.RunCommand(player, "ITW" + "1," + bank.ID + ",5");

            Assert.NotNull(player.Bank.Containers[BankNpcId].GetSlot(5));
            Assert.Null(player.Inventory.GetSlot(1));
            Assert.Contains(player.Sent, s => s.StartsWith("SBS5|"));
        }
    }

    [Fact]
    public void RefusedDeposit_LeavesSlotsUntouched()
    {
        var (fixture, player, _) = BankWorld();
        using (fixture)
        {
            var container = new ItemContainer(10);
            var window = new NoDepositWindow(container);

            player.Inventory.SetSlot(1, MakeSlot(fixture, StackableTemplate));

            window.InventoryToWindow(player, 1, 3, fixture.World);

            Assert.Null(container.GetSlot(3));
            Assert.NotNull(player.Inventory.GetSlot(1));
            Assert.Empty(window.SentSlots);
        }
    }

    [Fact]
    public void StackMerge_AnnouncesInPlaceMutationOnce()
    {
        var (fixture, player, _) = BankWorld();
        using (fixture)
        {
            var container = new ItemContainer(10);
            var window = new SeamWindow(container);
            var existing = MakeSlot(fixture, StackableTemplate, 3);
            container.SetSlot(1, existing);
            window.Events.Clear();

            player.Inventory.SetSlot(1, MakeSlot(fixture, StackableTemplate, 2));

            window.InventoryToWindow(player, 1, 1, fixture.World);

            Assert.Same(existing, container.GetSlot(1));
            Assert.Equal(5, existing.Stack);
            Assert.Null(player.Inventory.GetSlot(1));
            Assert.Single(window.Events);
            var (index, prior, next) = window.Events[0];
            Assert.Equal(1, index);
            Assert.Same(existing, next);
            Assert.Equal(5, next!.Stack);
        }
    }

    [Fact]
    public void OccupiedTarget_WithdrawDirectionValidated()
    {
        var (fixture, player, _) = BankWorld();
        using (fixture)
        {
            var container = new ItemContainer(10);
            var window = new NoWithdrawWindow(container);
            var occupant = MakeSlot(fixture, OtherTemplate);
            container.SetSlot(1, occupant);
            window.Events.Clear();

            var incoming = MakeSlot(fixture, StackableTemplate);
            player.Inventory.SetSlot(1, incoming);

            window.InventoryToWindow(player, 1, 1, fixture.World);

            Assert.Same(occupant, container.GetSlot(1));
            Assert.Same(incoming, player.Inventory.GetSlot(1));
            Assert.Empty(window.SentSlots);
            Assert.Empty(window.Events);
        }
    }
}
