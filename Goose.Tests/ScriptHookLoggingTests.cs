using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests;

// CapturingLog swaps the global NLog configuration; DisableParallelization keeps
// this class from running concurrently with any other test class.
[CollectionDefinition("NLog", DisableParallelization = true)]
public class NLogCollection { }

[Collection("NLog")]
public class ScriptHookLoggingTests
{
    [Fact]
    public void Map_AddPlayer_WithThrowingOnPlayerEntered_LogsMapAndPlayerIds()
    {
        using var fixture = new TestWorldFixture();
        using var log = new CapturingLog();
        var map = fixture.AddBaseMap(7, "HookMap");
        map.Script = ScriptStub.For<IMapScript>(new ThrowingEnteredScript());
        var player = fixture.PlayerOn(map, 1, 1);
        player.Name = "HookPlayer";
        player.LoginID = 42;

        map.AddPlayer(player, fixture.World);

        Assert.Contains(log.Messages, m =>
            m.Contains("HookMap") && m.Contains("7") &&
            m.Contains("HookPlayer") && m.Contains("42"));
    }

    [Fact]
    public void Npc_Attacked_WithThrowingOnAttackedEvent_LogsNpcNameAndTemplateId()
    {
        using var fixture = new TestWorldFixture();
        using var log = new CapturingLog();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = new NPCTemplate
        {
            NPCTemplateID = 100162,
            Name = "HookNpc",
            Level = 5,
            ClassID = 1,
            CanBeKilled = true,
            BaseStats = new AttributeSet { HP = 100 },
        };
        var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 3, 3, template, false)!;
        npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new ThrowingAttackedScript());

        npc.Attacked(player, 10, fixture.World);

        Assert.Equal(90, npc.CurrentHP);
        Assert.Contains(log.Messages, m =>
            m.Contains("HookNpc") && m.Contains("100162"));
    }

    [Fact]
    public void Npc_Killed_WithThrowingOnKilledEvent_MapHookStillRuns()
    {
        using var fixture = new TestWorldFixture();
        using var log = new CapturingLog();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = new NPCTemplate
        {
            NPCTemplateID = 100162,
            Name = "HookNpc",
            Level = 5,
            ClassID = 1,
            CanBeKilled = true,
            BaseStats = new AttributeSet { HP = 100 },
        };
        var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 3, 3, template, false)!;
        npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new ThrowingKilledScript());
        var mapScript = new RecordingMapScript();
        map.Script = ScriptStub.For<IMapScript>(mapScript);

        npc.Attacked(player, 100, fixture.World);

        Assert.Equal(0, npc.CurrentHP);
        Assert.Equal(1, mapScript.OnNPCKilledEventCalls);
        Assert.Contains(log.Messages, m =>
            m.Contains("NPC OnKilledEvent") && m.Contains("HookNpc") && m.Contains("100162"));
    }

    [Fact]
    public void UseConsumable_ThrowingScript_DoesNotConsumeItem()
    {
        using var fixture = new TestWorldFixture();
        using var log = new CapturingLog();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = fixture.AddBaseItemTemplate(501, "HookPotion", ItemTemplate.UseTypes.OneTime,
            t =>
            {
                t.SpellEffectID = 0;
                t.SpellEffect = null;
                t.Script = ScriptStub.For<IItemScript>(new ThrowingUseScript());
            });
        var item = new Item();
        item.LoadFromTemplate(template);
        player.Inventory.AddItem(item, 1, fixture.World);

        player.Inventory.UseConsumable(item, fixture.World);

        Assert.True(player.Inventory.HasItem(501));
        Assert.Contains(log.Messages, m =>
            m.Contains("HookPotion") && m.Contains("501"));
    }

    [Fact]
    public void UseConsumable_ScriptReturningTrue_StillConsumes()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = fixture.AddBaseItemTemplate(502, "HookPotion", ItemTemplate.UseTypes.OneTime,
            t => t.Script = ScriptStub.For<IItemScript>(new ConsumeUseScript()));
        var item = new Item();
        item.LoadFromTemplate(template);
        player.Inventory.AddItem(item, 1, fixture.World);

        player.Inventory.UseConsumable(item, fixture.World);

        Assert.False(player.Inventory.HasItem(502));
    }

    [Fact]
    public void UseConsumable_ScriptReturningFalse_KeepsItem()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = fixture.AddBaseItemTemplate(503, "HookPotion", ItemTemplate.UseTypes.OneTime,
            t => t.Script = ScriptStub.For<IItemScript>(new KeepUseScript()));
        var item = new Item();
        item.LoadFromTemplate(template);
        player.Inventory.AddItem(item, 1, fixture.World);

        player.Inventory.UseConsumable(item, fixture.World);

        Assert.True(player.Inventory.HasItem(503));
    }

    [Fact]
    public void Player_AddBuff_ThrowingOnBuffAdded_BuffAppliedAndLogged()
    {
        using var fixture = new TestWorldFixture();
        using var log = new CapturingLog();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(7, "HookEffect",
            e =>
            {
                e.Stats = new AttributeSet { Strength = 3 };
                e.Script = ScriptStub.For<ISpellEffectScript>(new ThrowingBuffAddedScript());
            });
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };

        player.AddBuff(buff, fixture.World);

        Assert.Contains(buff, player.Buffs);
        Assert.Equal(3, player.MaxStats.Strength);
        Assert.Contains(log.Messages, m =>
            m.Contains("HookEffect") && m.Contains("7"));
    }

    [Fact]
    public void Player_RemoveBuff_ThrowingOnBuffRemoved_Logged()
    {
        using var fixture = new TestWorldFixture();
        using var log = new CapturingLog();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(8, "HookEffect",
            e => e.Script = ScriptStub.For<ISpellEffectScript>(new ThrowingBuffRemovedScript()));
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };
        player.AddBuff(buff, fixture.World);

        player.RemoveBuff(buff, fixture.World);

        Assert.DoesNotContain(buff, player.Buffs);
        Assert.Contains(log.Messages, m =>
            m.Contains("HookEffect") && m.Contains("8"));
    }

    [Fact]
    public void ResolveAllies_BadIds_KeepsValidOnesAndLogsBadIds()
    {
        using var log = new CapturingLog();
        var handler = new NPCHandler();
        handler.AddTemplate(new NPCTemplate { NPCTemplateID = 100, Name = "AllyNpc", BaseStats = new AttributeSet() });
        handler.AddTemplate(new NPCTemplate { NPCTemplateID = 7, Name = "Ally7", BaseStats = new AttributeSet() });
        handler.AddTemplate(new NPCTemplate { NPCTemplateID = 1, Name = "Ally1", BaseStats = new AttributeSet() });
        var npc = new NPCTemplate { NPCTemplateID = 100, Name = "AllyNpc", BaseStats = new AttributeSet() };

        var allies = NPCHandler.ResolveAllies(npc, "999 7 1", handler);

        Assert.Equal(2, allies.Count);
        Assert.Contains(allies, a => a.NPCTemplateID == 7);
        Assert.Contains(allies, a => a.NPCTemplateID == 1);
        Assert.Contains(log.Messages, m =>
            m.Contains("bad ally template id 999") && m.Contains("AllyNpc") && m.Contains("100"));
    }

    [Fact]
    public void ResolveAllies_NonNumeric_LogsErrorAndReturnsEmpty()
    {
        using var log = new CapturingLog();
        var handler = new NPCHandler();
        var npc = new NPCTemplate { NPCTemplateID = 100, Name = "AllyNpc", BaseStats = new AttributeSet() };

        var allies = NPCHandler.ResolveAllies(npc, "notanumber", handler);

        Assert.Empty(allies);
        Assert.Contains(log.Messages, m =>
            m.Contains("failed parsing allies") && m.Contains("notanumber") && m.Contains("100"));
    }

    [Fact]
    public void LoadFromAutoCreate_BadStartingItemId_LogsWarning()
    {
        using var fixture = new TestWorldFixture(s =>
        {
            s.StartingMapID = 1;
            s.StartingItems = "999 123";
        });
        using var log = new CapturingLog();
        fixture.AddBaseMap(1, "Spawn");
        fixture.AddBaseItemTemplate(123, "HookItem", ItemTemplate.UseTypes.NoUse);
        var player = new Player(0);

        bool ok = player.LoadFromAutoCreate("Newbie", "pass", fixture.World);

        Assert.True(ok);
        Assert.True(player.Inventory.HasItem(123));
        Assert.Contains(log.Messages, m =>
            m.Contains("bad starting item id 999") && m.Contains("Newbie"));
    }

    private sealed class ThrowingUseScript : BaseItemScript
    {
        public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class ConsumeUseScript : BaseItemScript
    {
        public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world) => true;
    }

    private sealed class KeepUseScript : BaseItemScript
    {
        public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world) => false;
    }

    private sealed class ThrowingBuffAddedScript : BaseSpellEffectScript
    {
        public override void OnBuffAdded(Buff buff, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class ThrowingBuffRemovedScript : BaseSpellEffectScript
    {
        public override void OnBuffRemoved(Buff buff, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class ThrowingEnteredScript : BaseMapScript
    {
        public override void OnPlayerEntered(Map map, Player player, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class ThrowingAttackedScript : BaseNPCScript
    {
        public override void OnAttackedEvent(NPC npc, ICharacter attacker, long damage, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class ThrowingKilledScript : BaseNPCScript
    {
        public override void OnKilledEvent(NPC npc, ICharacter killer, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class RecordingMapScript : BaseMapScript
    {
        public int OnNPCKilledEventCalls { get; private set; }

        public override void OnNPCKilledEvent(Map map, NPC npc, ICharacter killer, GameWorld world)
            => this.OnNPCKilledEventCalls++;
    }
}
