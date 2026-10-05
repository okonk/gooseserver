using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests;

public class GMAddHPScriptTests
{
    [Fact]
    public void Added_hp_stacks_and_is_removed_when_the_npc_dies()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = new NPCTemplate
        {
            NPCTemplateID = 1,
            Name = "Test NPC",
            Level = 5,
            ClassID = 1,
            CanBeKilled = true,
            RespawnTime = 10,
            BaseStats = new AttributeSet { HP = 100, Strength = 25 },
            Drops = [],
        };
        var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, map.ID, 3, 3, template, true)!;
        map.Players.Add(player);
        npc.CurrentHP = 50;
        var scriptBody = File.ReadAllText(Path.Combine(FindIllutiaDataDirectory(), "Scripts", "Spell", "GMAddHP.csx"));
        var effect = new SpellEffect
        {
            ID = 299,
            Name = "GMAddHP",
            EffectType = SpellEffect.EffectTypes.Script,
            Stats = new AttributeSet { HP = 1_000_000, Strength = 9 },
            Script = fixture.CompileSpellEffectScript(scriptBody, "GMAddHP.csx"),
        };

        Assert.True(effect.Script.Object.Cast(effect, player, npc, fixture.World));
        Assert.True(effect.Script.Object.Cast(effect, player, npc, fixture.World));
        Assert.Equal(2_000_100, npc.MaxHP);
        Assert.Equal(2_000_050, npc.CurrentHP);
        var hpBuff = Assert.Single(npc.Buffs);
        Assert.Equal(2_000_000, hpBuff.SpellEffect.Stats.HP);
        Assert.Equal(0, hpBuff.SpellEffect.Stats.Strength);
        Assert.Equal(25, npc.MaxStats.Strength);
        Assert.Empty(player.Sent);

        npc.Attacked(player, npc.CurrentHP, fixture.World);

        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.Equal(100, npc.MaxHP);
        Assert.Equal(0, npc.CurrentHP);
        Assert.Equal(25, npc.MaxStats.Strength);
        Assert.Empty(npc.Buffs);

        npc.Spawn(fixture.World);

        Assert.Equal(100, npc.MaxHP);
        Assert.Equal(100, npc.CurrentHP);
    }

    [Fact]
    public void Remove_hp_repairs_only_untracked_hp_and_never_crosses_the_normal_maximum()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = new NPCTemplate
        {
            NPCTemplateID = 1,
            Name = "Test NPC",
            Level = 5,
            ClassID = 1,
            CanBeKilled = true,
            BaseStats = new AttributeSet { HP = 100 },
            Drops = [],
        };
        var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, map.ID, 3, 3, template, true)!;
        map.Players.Add(player);
        var trackedEffect = new SpellEffect { Stats = new AttributeSet { HP = 500 } };
        npc.Buffs.Add(new Buff { Caster = player, Target = npc, SpellEffect = trackedEffect });
        npc.MaxStats.HP += trackedEffect.Stats.HP;
        npc.MaxStats.HP += 2_000_000;
        npc.CurrentHP = 1_500_000;
        var scriptBody = File.ReadAllText(Path.Combine(FindIllutiaDataDirectory(), "Scripts", "Spell", "GMRemoveHP.csx"));
        var effect = new SpellEffect
        {
            ID = 300,
            Name = "GMRemoveHP",
            EffectType = SpellEffect.EffectTypes.Script,
            Stats = new AttributeSet { HP = 1_000_000 },
            Script = fixture.CompileSpellEffectScript(scriptBody, "GMRemoveHP.csx"),
        };

        Assert.True(effect.Script.Object.Cast(effect, player, npc, fixture.World));
        Assert.Equal(1_000_600, npc.MaxHP);
        Assert.Equal(1_000_600, npc.CurrentHP);
        Assert.True(effect.Script.Object.Cast(effect, player, npc, fixture.World));
        Assert.Equal(600, npc.MaxHP);
        Assert.Equal(600, npc.CurrentHP);
        Assert.Empty(player.Sent);

        Assert.False(effect.Script.Object.Cast(effect, player, npc, fixture.World));
        Assert.Equal(600, npc.MaxHP);
        Assert.Equal(600, npc.CurrentHP);
    }

    private static string FindIllutiaDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory.FullName, "Data", "Illutia"),
                Path.Combine(directory.FullName, "Goose", "Data", "Illutia"),
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Data/Illutia from " + AppContext.BaseDirectory);
    }
}
