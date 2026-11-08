using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaSpellsTests : IDisposable
{
    private readonly ArenaTestKit kit;
    private readonly Map map;
    private readonly Script<INPCScript> mobScript;
    private int nextSpellId = 900;

    public ArenaSpellsTests()
    {
        kit = new ArenaTestKit();
        kit.Fixture.Settings.MaxAC = 255;
        map = kit.Fixture.AddBaseMap(2, "Spell Test", 40, 40);
        mobScript = kit.Compile<INPCScript>("Arena", "ArenaMob.csx");
        AddSpell("Fearsome Lash", SpellEffect.TargetTypes.LineFront, 3);
        AddSpell("Arcane Blast", SpellEffect.TargetTypes.Target, 0);
        AddSpell("Restore Health", SpellEffect.TargetTypes.Target, 0);
        kit.SetConfig(j => j["mobSpells"] = System.Text.Json.Nodes.JsonNode.Parse("""
            {
                "Fearsome Lash": { "damage": 2.0, "cooldown": 10 },
                "Arcane Blast": { "damage": 1.5, "cooldown": 6, "range": 5 },
                "Restore Health": { "heal": 0.05, "healBelow": 0.8, "cooldown": 10, "range": 6 }
            }
            """));
    }

    public void Dispose() => kit.Dispose();

    private void AddSpell(string name, SpellEffect.TargetTypes shape, int size)
    {
        var effect = new SpellEffect { ID = nextSpellId, Name = name, TargetType = shape, TargetSize = size, Animation = 815008 };
        kit.World.SpellHandler.AddSpellEffect(effect);
        kit.World.SpellHandler.AddSpell(new Spell { ID = nextSpellId++, Name = name, SpellEffect = effect });
    }

    private NPC Mob(int id, int x, int y, string spells)
    {
        var template = kit.Fixture.AddNpcTemplate(id, "Arena Mob " + id, t =>
        {
            t.Script = mobScript;
            t.ScriptParams = "spells=" + spells;
        });
        return kit.Fixture.SpawnNpc(map, x, y, template, false, new PropertiesDictionary
        {
            ["arena.run"] = "r1",
            ["arena.round"] = 20,
            ["arena.tier"] = 0,
            ["arena.alive"] = 4,
        });
    }

    private TestWorldFixture.CapturingPlayer Player(int id, int x, int y)
    {
        var player = kit.Fixture.CommandPlayerOn(map, x, y, "P" + id);
        player.PlayerID = id;
        player.ExperienceSold = 3_000_000_000;
        player.MaxStats.HP = 1_000_000_000;
        player.CurrentHP = 1_000_000_000;
        kit.Fixture.AddOnlinePlayer(player);
        map.Players.Add(player);
        map.SetCharacter(player, x, y);
        return player;
    }

    [Fact]
    public void Fearsome_lash_turns_to_hit_players_up_to_three_tiles_in_a_line_for_scaled_damage()
    {
        var rogue = Mob(6001, 10, 10, "Fearsome Lash");
        var inLine = Player(70001, 10, 13);
        var offLine = Player(70002, 11, 12);
        rogue.AddAggro(offLine, 1, kit.World);

        mobScript.Object.OnAttackEvent(rogue, kit.World);

        Assert.Equal(Direction.Down, rogue.Facing);
        long expected = (long)rogue.MitigateMelee(inLine, 65_000 * 2.0, kit.World);
        Assert.Equal(1_000_000_000 - expected, inLine.CurrentHP);
        Assert.Equal(1_000_000_000, offLine.CurrentHP);
        Assert.Contains(inLine.Sent, p => p.Contains("815008"));
    }

    [Fact]
    public void Spell_on_cooldown_falls_back_to_melee()
    {
        var rogue = Mob(6002, 10, 10, "Fearsome Lash");
        var player = Player(70003, 10, 11);
        rogue.AddAggro(player, 1, kit.World);
        mobScript.Object.OnAttackEvent(rogue, kit.World);
        long afterLash = player.CurrentHP;
        player.Sent.Clear();

        mobScript.Object.OnAttackEvent(rogue, kit.World);

        Assert.True(player.CurrentHP < afterLash);
        Assert.DoesNotContain(player.Sent, p => p.Contains("815008"));
    }

    [Fact]
    public void Targeted_spell_needs_the_target_in_range()
    {
        var magus = Mob(6003, 10, 10, "Arcane Blast");
        var far = Player(70004, 10, 20);
        magus.AddAggro(far, 1, kit.World);

        mobScript.Object.OnAttackEvent(magus, kit.World);
        Assert.Equal(1_000_000_000, far.CurrentHP);

        far.WarpTo(kit.World, map, 10, 14);
        mobScript.Object.OnAttackEvent(magus, kit.World);
        Assert.True(far.CurrentHP < 1_000_000_000);
    }

    [Fact]
    public void Priest_heals_the_most_injured_ally_in_range_and_ignores_healthy_ones()
    {
        var priest = Mob(6004, 10, 10, "Restore Health");
        var hurt = Mob(6005, 12, 10, "");
        var healthy = Mob(6006, 13, 10, "");
        var player = Player(70005, 20, 20);
        priest.AddAggro(player, 1, kit.World);
        hurt.CurrentHP = hurt.MaxHP / 2;
        healthy.CurrentHP = healthy.MaxHP * 9 / 10;
        long healthyBefore = healthy.CurrentHP;

        mobScript.Object.OnAttackEvent(priest, kit.World);

        Assert.Equal(hurt.MaxHP / 2 + hurt.MaxHP / 20, hurt.CurrentHP);
        Assert.Equal(healthyBefore, healthy.CurrentHP);
    }
}
