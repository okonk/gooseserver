using Goose.Testing;

namespace Goose.Tests;

public class NpcDespawnTests
{
    [Fact]
    public void Killing_a_non_respawning_npc_releases_its_id_and_registry_entry()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var template = fixture.AddNpcTemplate(1, "Temp");
        var npc = fixture.SpawnNpc(map, 5, 5, template, shouldRespawn: false);

        int countBefore = fixture.World.NPCHandler.NPCCount;
        var player = fixture.CommandPlayerOn(map, 6, 6);
        map.AddPlayer(player, fixture.World);
        map.SetCharacter(player, 6, 6);

        npc.Attacked(player, npc.CurrentHP, fixture.World);

        Assert.False(fixture.World.NPCHandler.IsRegistered(npc.LoginID));
        Assert.Equal(countBefore - 1, fixture.World.NPCHandler.NPCCount);
        Assert.DoesNotContain(npc, map.NPCs);
    }

    [Fact]
    public void Killing_a_respawning_npc_keeps_it_registered()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var template = fixture.AddNpcTemplate(1, "Temp");
        var npc = fixture.SpawnNpc(map, 5, 5, template, shouldRespawn: true);

        int countBefore = fixture.World.NPCHandler.NPCCount;
        var player = fixture.CommandPlayerOn(map, 6, 6);
        map.AddPlayer(player, fixture.World);
        map.SetCharacter(player, 6, 6);

        npc.Attacked(player, npc.CurrentHP, fixture.World);

        Assert.True(fixture.World.NPCHandler.IsRegistered(npc.LoginID));
        Assert.Equal(countBefore, fixture.World.NPCHandler.NPCCount);
    }

    [Fact]
    public void Despawn_removes_npc_from_map_tile_and_registries()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var template = fixture.AddNpcTemplate(1, "Temp");
        var npc = fixture.SpawnNpc(map, 5, 5, template);

        var player = fixture.CommandPlayerOn(map, 6, 6);
        map.AddPlayer(player, fixture.World);
        map.SetCharacter(player, 6, 6);

        npc.Despawn(fixture.World);

        Assert.Null(map.GetCharacterAt(5, 5));
        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.False(fixture.World.NPCHandler.IsRegistered(npc.LoginID));
        Assert.DoesNotContain(npc, map.NPCs);
        Assert.Contains("ERC" + npc.LoginID + "\x01", player.Sent);
    }

    [Fact]
    public void Despawn_twice_is_a_noop()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var template = fixture.AddNpcTemplate(1, "Temp");
        var npc = fixture.SpawnNpc(map, 5, 5, template);

        var player = fixture.CommandPlayerOn(map, 6, 6);
        map.AddPlayer(player, fixture.World);
        map.SetCharacter(player, 6, 6);

        npc.Despawn(fixture.World);
        int sentBefore = player.Sent.Count;

        npc.Despawn(fixture.World);

        Assert.Equal(sentBefore, player.Sent.Count);
    }

    [Fact]
    public void Releasing_twice_does_not_touch_other_npcs()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var a = fixture.SpawnNpc(map, 5, 5, fixture.AddNpcTemplate(1, "A"));
        var b = fixture.SpawnNpc(map, 7, 7, fixture.AddNpcTemplate(2, "B"));

        int countBefore = fixture.World.NPCHandler.NPCCount;
        a.Despawn(fixture.World);
        fixture.World.NPCHandler.Release(a);

        Assert.True(fixture.World.NPCHandler.IsRegistered(b.LoginID));
        Assert.Equal(countBefore - 1, fixture.World.NPCHandler.NPCCount);
    }

    [Fact]
    public void Despawn_removes_buffs()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var template = fixture.AddNpcTemplate(1, "Temp");
        var npc = fixture.SpawnNpc(map, 5, 5, template);

        var effect = fixture.AddBaseSpellEffect(1, "Stun",
            e => { e.EffectType = SpellEffect.EffectTypes.Stun; e.Duration = 0; });
        var buff = new Buff { Target = npc, Caster = npc, SpellEffect = effect };
        npc.AddBuff(buff, fixture.World);

        npc.Despawn(fixture.World);

        Assert.Empty(npc.Buffs);
    }
}
