using Goose.Events;
using Goose.Testing;

namespace Goose.Tests;

public class PartyMemberBuffSyncTests
{
    private sealed class GroupedPlayers : IDisposable
    {
        public TestWorldFixture Fixture { get; }
        public Map Map { get; }
        public TestWorldFixture.CapturingPlayer Target { get; }
        public TestWorldFixture.CapturingPlayer Viewer { get; }
        public Group Group { get; }

        public GroupedPlayers()
        {
            this.Fixture = new TestWorldFixture(s => { s.PartyWindowMax = 4; s.BuffBarVisibleSize = 3; });
            this.Map = this.Fixture.AddBaseMap(1, "m");
            this.Target = this.Fixture.CommandPlayerOn(this.Map, 5, 5, "Target");
            this.Viewer = this.Fixture.CommandPlayerOn(this.Map, 6, 5, "Viewer");
            this.Target.LoginID = 101;
            this.Viewer.LoginID = 102;
            this.Map.AddPlayer(this.Target, this.Fixture.World);
            this.Map.AddPlayer(this.Viewer, this.Fixture.World);
            this.Group = new Group();
            this.Group.AddPlayer(this.Target, this.Fixture.World, this.Target);
            this.Group.AddPlayer(this.Viewer, this.Fixture.World, this.Target);
            this.Target.Sent.Clear();
            this.Viewer.Sent.Clear();
        }

        public void Dispose() => this.Fixture.Dispose();
    }

    private static List<string> PartyDeltas(List<string> sent)
        => sent.Where(s => s.StartsWith("PBA") || s.StartsWith("PBR") || s.StartsWith("PBC")).ToList();

    [Fact]
    public void SendBuffSnapshot_EmitsPBCBeforeOrderedPBAs()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var charm = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var speed = ctx.Fixture.AddBaseSpellEffect(6, "Speed",
            e => { e.Duration = 120; e.BuffGraphic = 8; e.BuffGraphicFile = 12; });
        ctx.Target.Buffs.Add(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = charm });
        ctx.Target.Buffs.Add(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = speed, TimeCast = world.TimeNow });

        ctx.Viewer.Sent.Clear();
        ctx.Group.SendBuffSnapshot(ctx.Viewer, ctx.Target, world);

        Assert.Equal(3, ctx.Viewer.Sent.Count);
        Assert.Equal("PBC101" + "\x01", ctx.Viewer.Sent[0]);
        Assert.Equal("PBA101,5,7,11,0,0,Charm" + "\x01", ctx.Viewer.Sent[1]);
        var parts = ctx.Viewer.Sent[2].TrimEnd('\x01').Split(',');
        Assert.Equal("PBA101", parts[0]);
        Assert.Equal("6", parts[1]);
        Assert.InRange(long.Parse(parts[4]), 119000, 120000);
        Assert.Equal("120000", parts[5]);
        Assert.Equal("Speed", parts[6]);
    }

    [Fact]
    public void SendBuffSnapshot_EmptyBuffs_EmitsOnlyPBC()
    {
        using var ctx = new GroupedPlayers();

        ctx.Viewer.Sent.Clear();
        ctx.Group.SendBuffSnapshot(ctx.Viewer, ctx.Target, ctx.Fixture.World);

        Assert.Equal(["PBC101" + "\x01"], ctx.Viewer.Sent);
    }

    [Fact]
    public void SendBuffSnapshot_ItemBuffs_ExcludedRegardlessOfShowItemBuffs()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var itemEffect = ctx.Fixture.AddBaseSpellEffect(9, "Item Effect",
            e => { e.Duration = 0; e.BuffGraphic = 1; e.BuffGraphicFile = 2; });
        ctx.Target.Buffs.Add(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = itemEffect, ItemBuff = true });

        ctx.Viewer.Sent.Clear();
        ctx.Group.SendBuffSnapshot(ctx.Viewer, ctx.Target, world);
        Assert.Equal(["PBC101" + "\x01"], ctx.Viewer.Sent);

        ctx.Target.ToggleSettings = Player.ToggleSetting.ItemBuffs;
        ctx.Viewer.Sent.Clear();
        ctx.Group.SendBuffSnapshot(ctx.Viewer, ctx.Target, world);
        Assert.Equal(["PBC101" + "\x01"], ctx.Viewer.Sent);
    }

    [Fact]
    public void AddBuff_PublishesOnePBA()
    {
        using var ctx = new GroupedPlayers();
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var buff = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect };

        ctx.Target.AddBuff(buff, ctx.Fixture.World);

        Assert.Equal(["PBA101,5,7,11,0,0,Charm" + "\x01"], PartyDeltas(ctx.Viewer.Sent));
        Assert.Empty(PartyDeltas(ctx.Target.Sent));
    }

    [Fact]
    public void AddBuff_RefreshbarFalse_PublishesPBAButNoBUF()
    {
        using var ctx = new GroupedPlayers();
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var buff = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect };

        ctx.Target.AddBuff(buff, ctx.Fixture.World, refreshbar: false);

        Assert.Equal(["PBA101,5,7,11,0,0,Charm" + "\x01"], PartyDeltas(ctx.Viewer.Sent));
        Assert.DoesNotContain(ctx.Target.Sent, s => s.StartsWith("BUF"));
    }

    [Fact]
    public void AddBuff_SameIdRenewal_PublishesOnePBAWithResetRemaining()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 120; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var first = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect,
            TimeCast = world.TimeNow - 60 * world.TimerFrequency };
        ctx.Target.AddBuff(first, world);
        ctx.Viewer.Sent.Clear();

        var second = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect,
            TimeCast = world.TimeNow };
        ctx.Target.AddBuff(second, world);

        var deltas = PartyDeltas(ctx.Viewer.Sent);
        Assert.Single(deltas);
        var parts = deltas[0].TrimEnd('\x01').Split(',');
        Assert.Equal("PBA101", parts[0]);
        Assert.Equal("5", parts[1]);
        Assert.InRange(long.Parse(parts[4]), 119000, 120000);
        Assert.Equal("120000", parts[5]);
    }

    [Fact]
    public void AddBuff_SameIdRenewal_RefreshbarFalse_PublishesPBAButNoBUF()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 120; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var first = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect,
            TimeCast = world.TimeNow - 60 * world.TimerFrequency };
        ctx.Target.AddBuff(first, world);
        ctx.Viewer.Sent.Clear();
        ctx.Target.Sent.Clear();

        var second = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect,
            TimeCast = world.TimeNow };
        ctx.Target.AddBuff(second, world, refreshbar: false);

        Assert.Single(PartyDeltas(ctx.Viewer.Sent));
        Assert.DoesNotContain(ctx.Viewer.Sent, s => s.StartsWith("PBR") || s.StartsWith("PBC"));
        Assert.DoesNotContain(ctx.Target.Sent, s => s.StartsWith("BUF"));
    }

    [Fact]
    public void AddBuff_StackingUpgrade_PublishesPBRThenPBA()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var minor = ctx.Fixture.AddBaseSpellEffect(5, "Minor Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var major = ctx.Fixture.AddBaseSpellEffect(6, "Major Charm",
            e => { e.Duration = 0; e.BuffGraphic = 8; e.BuffGraphicFile = 12; });
        major.BuffStacksOver.Add(minor);
        ctx.Target.AddBuff(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = minor }, world);
        ctx.Viewer.Sent.Clear();

        ctx.Target.AddBuff(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = major }, world);

        Assert.Equal(["PBR101,5" + "\x01", "PBA101,6,8,12,0,0,Major Charm" + "\x01"],
            PartyDeltas(ctx.Viewer.Sent));
    }

    [Fact]
    public void AddBuff_RejectedNonStacking_PublishesNothing()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var minor = ctx.Fixture.AddBaseSpellEffect(5, "Minor Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var major = ctx.Fixture.AddBaseSpellEffect(6, "Major Charm",
            e => { e.Duration = 0; e.BuffGraphic = 8; e.BuffGraphicFile = 12; });
        major.BuffDoesntStackOver.Add(minor);
        ctx.Target.AddBuff(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = minor }, world);
        ctx.Viewer.Sent.Clear();

        ctx.Target.AddBuff(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = major }, world);

        Assert.Empty(PartyDeltas(ctx.Viewer.Sent));
        Assert.DoesNotContain(ctx.Target.Buffs, b => b.SpellEffect == major);
    }

    [Fact]
    public void RemoveBuff_PublishesOnePBR_DuplicateRemovalPublishesNothing()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var buff = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect };
        ctx.Target.AddBuff(buff, world);
        ctx.Viewer.Sent.Clear();

        ctx.Target.RemoveBuff(buff, world);
        Assert.Equal(["PBR101,5" + "\x01"], PartyDeltas(ctx.Viewer.Sent));

        ctx.Viewer.Sent.Clear();
        ctx.Target.RemoveBuff(buff, world);
        Assert.Empty(PartyDeltas(ctx.Viewer.Sent));
    }

    [Fact]
    public void BuffExpireEvent_ExpiredBuff_PublishesPBR()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 120; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var buff = new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect,
            TimeCast = world.TimeNow - 130 * world.TimerFrequency };
        ctx.Target.AddBuff(buff, world);
        ctx.Viewer.Sent.Clear();

        var ev = new BuffExpireEvent { Ticks = 0, Player = ctx.Target, Data = buff };
        buff.BuffExpireEvent = ev;
        world.EventHandler.AddEvent(ev);
        world.EventHandler.Update(world);

        Assert.Equal(["PBR101,5" + "\x01"], PartyDeltas(ctx.Viewer.Sent));
        Assert.DoesNotContain(ctx.Target.Buffs, b => b.SpellEffect == effect);
    }

    [Fact]
    public void GroupRemovePlayer_ClearsRoster_NoLaterDeltas()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var effectA = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
        var effectB = ctx.Fixture.AddBaseSpellEffect(6, "Speed",
            e => { e.Duration = 0; e.BuffGraphic = 8; e.BuffGraphicFile = 12; });

        ctx.Group.RemovePlayer(ctx.Target, world, false, ctx.Viewer);
        Assert.Null(ctx.Target.Group);
        Assert.DoesNotContain(ctx.Group.Players, p => p == ctx.Target);
        Assert.Null(ctx.Viewer.Group);
        ctx.Viewer.Sent.Clear();
        ctx.Target.Sent.Clear();

        ctx.Target.AddBuff(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effectA }, world);
        Assert.Empty(PartyDeltas(ctx.Viewer.Sent));

        ctx.Viewer.AddBuff(new Buff { Caster = ctx.Viewer, Target = ctx.Viewer, SpellEffect = effectB }, world);
        Assert.Empty(PartyDeltas(ctx.Target.Sent));
    }

    [Fact]
    public void AddBuff_ViewerOutOfRange_PublishesNothing()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        var far = ctx.Fixture.CommandPlayerOn(ctx.Map, 5, 40, "Far");
        far.LoginID = 103;
        ctx.Map.AddPlayer(far, world);
        ctx.Group.AddPlayer(far, world, ctx.Target);
        far.Sent.Clear();
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });

        ctx.Target.AddBuff(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect }, world);

        Assert.Empty(PartyDeltas(far.Sent));
    }

    [Fact]
    public void AddBuff_TargetGMInvisible_PublishesNothing()
    {
        using var ctx = new GroupedPlayers();
        var world = ctx.Fixture.World;
        ctx.Target.Access = Player.AccessStatus.GameMaster;
        ctx.Viewer.Sent.Clear();
        var effect = ctx.Fixture.AddBaseSpellEffect(5, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });

        ctx.Target.AddBuff(new Buff { Caster = ctx.Target, Target = ctx.Target, SpellEffect = effect }, world);

        Assert.Empty(PartyDeltas(ctx.Viewer.Sent));
    }
}
