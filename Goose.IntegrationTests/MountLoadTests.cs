using System.Data.SQLite;
using Xunit;

namespace Goose.IntegrationTests;

public class MountLoadTests : PlayerFirstSaveTestBase
{
    public MountLoadTests() : base(["players", "banks"]) { }

    [Fact]
    public void Loading_a_saved_mount_equips_it_dismounted_without_the_speed_buff()
    {
        var effect = new SpellEffect
        {
            ID = 259,
            Name = "Mount Speed II",
            EffectType = SpellEffect.EffectTypes.Buff,
            Stats = new AttributeSet { MoveSpeed = 128 },
        };
        world.SpellHandler.AddSpellEffect(effect);
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 651,
            Name = "Tank",
            Slot = ItemTemplate.ItemSlots.Mount,
            SpellEffect = effect,
            BaseStats = new AttributeSet(),
        });

        var player = MakePlayer();
        // MakePlayer sets BaseStats but not MaxStats or Class; AddStats dereferences
        // both (Player.cs:155,1653; Packets.cs:420) and the equipped loop calls AddStats.
        player.MaxStats = new AttributeSet();
        player.Class = new Class { ClassID = 1 };
        player.BaseStats.MoveSpeed = 320;
        // Seed the move-speed queue the way LoadFromReader does (Player.cs:825-826);
        // MakePlayer does not go through LoadFromReader.
        var queue = (PriorityQueue<int, int>)typeof(Player)
            .GetProperty("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(player)!;
        queue.Enqueue(320, 320);

        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((14, SlotJson(651)))));

        player.Inventory.Load(world);

        var mount = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount);
        Assert.NotNull(mount);
        Assert.Equal(651, mount!.Item.TemplateID);
        Assert.False(player.Mounted);
        Assert.Empty(player.Buffs);
        // 128 if the mount buff were (wrongly) applied: the queue is a min-heap, so
        // the mount speed would win over the 320 base.
        Assert.Equal(320, player.CalculateMoveSpeed());
    }

    private void Insert(string sql, (string name, object value)? arg = null)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (arg is not null)
                cmd.Parameters.Add(new SQLiteParameter(arg.Value.name, arg.Value.value));
            cmd.ExecuteNonQuery();
        });
    }

    private static string SlotJson(int templateId) =>
        JsonHelper.Serialize(new ItemSlot
        {
            Item = new Item { TemplateID = templateId, BaseStats = new AttributeSet() },
            Stack = 1,
        });

    private static string FullArray(params (int index, string json)[] slots)
    {
        // EquippedSize is 20 in PlayerFirstSaveTestBase; the equipped array is size+1.
        var entries = new string[21];
        foreach (var (index, json) in slots)
            entries[index] = json;
        return "[" + string.Join(",", entries.Select(e => e ?? "null")) + "]";
    }
}
