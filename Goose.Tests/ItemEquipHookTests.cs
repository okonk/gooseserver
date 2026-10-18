using Goose.Testing;
using static Goose.Inventory;

namespace Goose.Tests;

public class ItemEquipHookTests
{
    private const int OldHelm = 100;
    private const int NewHelm = 101;
    private const int ShieldId = 102;
    private const int TwoHanderId = 103;

    private const string RecordingBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseItemScript
        {
            private static int HelmOf(Player player)
            {
                ItemSlot? helm = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Head);
                return helm is null ? 0 : helm.Item.TemplateID;
            }

            public override void OnEquipEvent(Player player, Item item, GameWorld world)
            {
                world.Send(player, P.ServerMessage("EQUIP:" + item.TemplateID + ":HELM=" + HelmOf(player)));
            }

            public override void OnUnequipEvent(Player player, Item item, GameWorld world)
            {
                world.Send(player, P.ServerMessage("UNEQUIP:" + item.TemplateID + ":HELM=" + HelmOf(player)));
            }
        }

        return typeof(T);
        """;

    private const string ThrowingBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseItemScript
        {
            public override void OnEquipEvent(Player player, Item item, GameWorld world)
            {
                throw new Exception("boom");
            }
        }

        return typeof(T);
        """;

    private static (TestWorldFixture World, TestWorldFixture.CapturingPlayer Player) NewPlayer()
    {
        var world = new TestWorldFixture();
        var map = world.AddBaseMap(1, "Town");
        var player = world.CommandPlayerOn(map, 1, 2, "Hax");
        return (world, player);
    }

    private static Item Give(TestWorldFixture world, Player player, int templateId,
                             ItemTemplate.UseTypes useType, ItemTemplate.ItemSlots slot,
                             string scriptBody, string scriptFile)
    {
        var template = world.AddBaseItemTemplate(templateId, "Item " + templateId, useType, t =>
        {
            t.Slot = slot;
            t.Script = world.CompileItemScript(scriptBody, scriptFile + ".csx");
        });
        var item = new Item();
        item.LoadFromTemplate(template);
        world.World.ItemHandler.AddAndAssignId(item, world.World);
        Assert.True(player.Inventory.AddItem(item, 1, world.World));
        return item;
    }

    private static List<string> HookMessages(TestWorldFixture.CapturingPlayer player) =>
        player.Sent.Where(p => p.StartsWith("$7EQUIP:") || p.StartsWith("$7UNEQUIP:"))
            .Select(p => p.Substring(2))
            .ToList();

    [Fact]
    public void Equip_calls_OnEquipEvent_once()
    {
        var (world, player) = NewPlayer();
        using (world)
        {
            var item = Give(world, player, OldHelm, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Helmet, RecordingBody, "Helm" + OldHelm);
            player.Sent.Clear();

            Assert.True(player.Inventory.Equip(item, world.World));

            Assert.Equal(new[] { "EQUIP:" + OldHelm + ":HELM=" + OldHelm }, HookMessages(player));
        }
    }

    [Fact]
    public void Unequip_calls_OnUnequipEvent_once()
    {
        var (world, player) = NewPlayer();
        using (world)
        {
            var item = Give(world, player, OldHelm, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Helmet, RecordingBody, "Helm" + OldHelm);
            Assert.True(player.Inventory.Equip(item, world.World));
            player.Sent.Clear();

            Assert.True(player.Inventory.Unequip(EquipSlots.Head, world.World));

            Assert.Equal(new[] { "UNEQUIP:" + OldHelm + ":HELM=0" }, HookMessages(player));
        }
    }

    [Fact]
    public void Unequip_of_empty_slot_calls_nothing()
    {
        var (world, player) = NewPlayer();
        using (world)
        {
            Give(world, player, OldHelm, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Helmet, RecordingBody, "Helm" + OldHelm);
            player.Sent.Clear();

            Assert.True(player.Inventory.Unequip(EquipSlots.Head, world.World));

            Assert.Empty(HookMessages(player));
        }
    }

    [Fact]
    public void Swapping_helms_calls_unequip_then_equip_both_seeing_the_new_helm()
    {
        var (world, player) = NewPlayer();
        using (world)
        {
            var oldHelm = Give(world, player, OldHelm, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Helmet, RecordingBody, "Helm" + OldHelm);
            Assert.True(player.Inventory.Equip(oldHelm, world.World));

            var newHelm = Give(world, player, NewHelm, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Helmet, RecordingBody, "Helm" + NewHelm);
            player.Sent.Clear();

            Assert.True(player.Inventory.Equip(newHelm, world.World));

            Assert.Equal(new[]
            {
                "UNEQUIP:" + OldHelm + ":HELM=" + NewHelm,
                "EQUIP:" + NewHelm + ":HELM=" + NewHelm,
            }, HookMessages(player));
        }
    }

    [Fact]
    public void Two_hander_over_shield_calls_shield_unequip_and_weapon_equip()
    {
        var (world, player) = NewPlayer();
        using (world)
        {
            var shield = Give(world, player, ShieldId, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Shield, RecordingBody, "Shield" + ShieldId);
            Assert.True(player.Inventory.Equip(shield, world.World));

            var twoHander = Give(world, player, TwoHanderId, ItemTemplate.UseTypes.Weapon,
                ItemTemplate.ItemSlots.TwoHanded, RecordingBody, "Weapon" + TwoHanderId);
            player.Sent.Clear();

            Assert.True(player.Inventory.Equip(twoHander, world.World));

            Assert.Equal(new[]
            {
                "UNEQUIP:" + ShieldId + ":HELM=0",
                "EQUIP:" + TwoHanderId + ":HELM=0",
            }, HookMessages(player));
        }
    }

    [Fact]
    public void Failed_equip_still_flushes_the_unequips_that_happened()
    {
        var (world, player) = NewPlayer();
        using (world)
        {
            var shield = Give(world, player, ShieldId, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Shield, RecordingBody, "Shield" + ShieldId);
            Assert.True(player.Inventory.Equip(shield, world.World));

            var template = world.AddBaseItemTemplate(TwoHanderId, "Item " + TwoHanderId,
                ItemTemplate.UseTypes.Weapon, t => t.Slot = ItemTemplate.ItemSlots.TwoHanded);
            var twoHander = new Item();
            twoHander.LoadFromTemplate(template);
            world.World.ItemHandler.AddAndAssignId(twoHander, world.World);
            player.Sent.Clear();

            Assert.False(player.Inventory.Equip(twoHander, world.World));

            Assert.Equal(new[] { "UNEQUIP:" + ShieldId + ":HELM=0" }, HookMessages(player));
        }
    }

    [Fact]
    public void Throwing_item_script_does_not_block_equip()
    {
        var (world, player) = NewPlayer();
        using (world)
        {
            var item = Give(world, player, OldHelm, ItemTemplate.UseTypes.Armor,
                ItemTemplate.ItemSlots.Helmet, ThrowingBody, "Throw" + OldHelm);

            var exception = Record.Exception(() =>
                Assert.True(player.Inventory.Equip(item, world.World)));

            Assert.Null(exception);
            Assert.Same(item, player.Inventory.GetEquippedSlot(EquipSlots.Head)!.Item);
        }
    }
}
