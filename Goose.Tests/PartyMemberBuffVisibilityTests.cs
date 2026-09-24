using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class PartyMemberBuffVisibilityTests
{
    private sealed class Ctx : IDisposable
    {
        public TestWorldFixture Fixture { get; }
        public Map Map { get; }
        public Group Group { get; }
        public TestWorldFixture.CapturingPlayer Target { get; }
        public TestWorldFixture.CapturingPlayer Viewer { get; }

        public Ctx(int viewerX = 6, bool grouped = true)
        {
            this.Fixture = new TestWorldFixture(s => s.PartyWindowMax = 10);
            this.Map = this.Fixture.AddBaseMap(1, "m", 60, 40);
            this.Target = this.Fixture.CommandPlayerOn(this.Map, 5, 5, "Target");
            this.Viewer = this.Fixture.CommandPlayerOn(this.Map, viewerX, 5, "Viewer");
            this.Target.LoginID = 101;
            this.Viewer.LoginID = 102;
            this.Map.AddPlayer(this.Target, this.Fixture.World);
            this.Map.AddPlayer(this.Viewer, this.Fixture.World);
            this.Group = new Group();
            this.Group.AddPlayer(this.Target, this.Fixture.World, this.Target);
            if (grouped)
            {
                this.Group.AddPlayer(this.Viewer, this.Fixture.World, this.Target);
            }
            var charm = this.Fixture.AddBaseSpellEffect(5, "Charm",
                e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
            this.Target.Buffs.Add(new Buff { Caster = this.Target, Target = this.Target, SpellEffect = charm });
            this.Target.Sent.Clear();
            this.Viewer.Sent.Clear();
        }

        public void Dispose() => this.Fixture.Dispose();
    }

    private static List<string> PartyPackets(List<string> sent)
        => sent.Where(s => s.StartsWith("PBA") || s.StartsWith("PBR") || s.StartsWith("PBC")).ToList();

    private static int IndexOf(List<string> sent, string prefix)
    {
        int i = sent.FindIndex(s => s.StartsWith(prefix));
        Assert.True(i >= 0, "missing packet " + prefix);
        return i;
    }

    private static void AssertOrder(List<string> sent, params string[] prefixes)
    {
        int prev = -1;
        foreach (var prefix in prefixes)
        {
            int i = IndexOf(sent, prefix);
            Assert.True(i > prev, $"{prefix} (index {i}) must come after index {prev}");
            prev = i;
        }
    }

    [Fact]
    public void GroupAdd_InRange_GUDPrecedesBilateralSnapshots()
    {
        using var ctx = new Ctx(viewerX: 6, grouped: false);
        var world = ctx.Fixture.World;
        ctx.Target.Sent.Clear();
        ctx.Viewer.Sent.Clear();

        ctx.Group.AddPlayer(ctx.Viewer, world, ctx.Target);

        AssertOrder(ctx.Viewer.Sent, "GUD1,101,", "PBC101", "PBA101,");
        AssertOrder(ctx.Target.Sent, "GUD1,102,", "PBC102");
    }

    [Fact]
    public void GroupAdd_OutOfRange_NoSnapshot()
    {
        using var ctx = new Ctx(viewerX: 40, grouped: false);
        var world = ctx.Fixture.World;
        ctx.Target.Sent.Clear();
        ctx.Viewer.Sent.Clear();

        ctx.Group.AddPlayer(ctx.Viewer, world, ctx.Target);

        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        Assert.Empty(PartyPackets(ctx.Target.Sent));
        Assert.Contains(ctx.Viewer.Sent, s => s.StartsWith("GUD1,101,"));
        Assert.Contains(ctx.Target.Sent, s => s.StartsWith("GUD1,102,"));
    }

    [Fact]
    public void GroupAdd_DifferentMap_NoSnapshot()
    {
        using var ctx = new Ctx(viewerX: 40, grouped: false);
        var world = ctx.Fixture.World;
        var other = ctx.Fixture.AddBaseMap(3, "other", 60, 40);
        ctx.Viewer.Map = other;
        ctx.Viewer.MapID = other.ID;
        ctx.Viewer.MapX = 5;
        ctx.Viewer.MapY = 5;
        other.AddPlayer(ctx.Viewer, world);
        ctx.Target.Sent.Clear();
        ctx.Viewer.Sent.Clear();

        ctx.Group.AddPlayer(ctx.Viewer, world, ctx.Target);

        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        Assert.Empty(PartyPackets(ctx.Target.Sent));
    }

    [Fact]
    public void MoveIntoRange_MKCPrecedesPBCAndPBA()
    {
        using var ctx = new Ctx(viewerX: 40);

        ctx.Viewer.MoveTo(ctx.Fixture.World, 6, 5);

        AssertOrder(ctx.Viewer.Sent, "MKC101,", "PBC101", "PBA101,");
        AssertOrder(ctx.Target.Sent, "MKC102,", "PBC102");
    }

    [Fact]
    public void MoveOutOfRange_ERCAndNoPartyPacket()
    {
        using var ctx = new Ctx();

        ctx.Viewer.MoveTo(ctx.Fixture.World, 40, 5);

        Assert.Contains(ctx.Viewer.Sent, s => s.StartsWith("ERC101"));
        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        Assert.Empty(PartyPackets(ctx.Target.Sent));
    }

    [Fact]
    public void MoveReentry_FreshPBCRepairsStaleStateBeforeUpserts()
    {
        using var ctx = new Ctx();
        var world = ctx.Fixture.World;

        ctx.Viewer.MoveTo(world, 40, 5);
        ctx.Viewer.MoveTo(world, 6, 5);

        AssertOrder(ctx.Viewer.Sent, "ERC101", "MKC101,", "PBC101", "PBA101,");
        AssertOrder(ctx.Target.Sent, "ERC102", "MKC102,", "PBC102");
    }

    [Fact]
    public void MoveTo_ExactRangeBoundary_NoSnapshot()
    {
        using var ctx = new Ctx(viewerX: 40);

        ctx.Viewer.MoveTo(ctx.Fixture.World, 29, 5);

        Assert.DoesNotContain(ctx.Viewer.Sent, s => s.StartsWith("MKC101,"));
        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        Assert.Empty(PartyPackets(ctx.Target.Sent));
    }

    [Fact]
    public void MoveIntoRange_GMInvisibleTarget_ViewerReceivesNoCharacterNoSnapshot()
    {
        using var ctx = new Ctx(viewerX: 40);
        ctx.Target.Access = Player.AccessStatus.GameMaster;

        ctx.Viewer.MoveTo(ctx.Fixture.World, 6, 5);

        Assert.DoesNotContain(ctx.Viewer.Sent, s => s.StartsWith("MKC101,"));
        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        AssertOrder(ctx.Target.Sent, "MKC102,", "PBC102");
    }

    [Fact]
    public void MoveIntoRange_RecipientLoading_NoSnapshot()
    {
        using var ctx = new Ctx(viewerX: 40);
        var world = ctx.Fixture.World;
        ctx.Viewer.Map = null!;
        ctx.Viewer.State = Player.States.LoadingMap;
        ctx.Map.RemovePlayer(ctx.Viewer, world);

        ctx.Target.MoveTo(world, 40, 5);

        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        Assert.Empty(PartyPackets(ctx.Target.Sent));
    }

    [Fact]
    public void MoveIntoRange_UnrelatedViewer_NoSnapshot()
    {
        using var ctx = new Ctx(viewerX: 40, grouped: false);

        ctx.Viewer.MoveTo(ctx.Fixture.World, 6, 5);

        AssertOrder(ctx.Viewer.Sent, "MKC101,");
        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        Assert.Empty(PartyPackets(ctx.Target.Sent));
    }

    [Fact]
    public void SameMapWarp_EraseThenMKCThenSnapshot()
    {
        using var ctx = new Ctx();

        ctx.Viewer.WarpTo(ctx.Fixture.World, ctx.Map, 15, 5);

        AssertOrder(ctx.Viewer.Sent, "ERC101", "MKC101,", "PBC101", "PBA101,");
        AssertOrder(ctx.Target.Sent, "ERC102", "MKC102,", "PBC102");
    }

    [Fact]
    public void SameMapWarp_OutOfRange_NoSnapshot()
    {
        using var ctx = new Ctx();

        ctx.Viewer.WarpTo(ctx.Fixture.World, ctx.Map, 40, 5);

        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
        Assert.Empty(PartyPackets(ctx.Target.Sent));
    }

    private sealed class LoadCtx : IDisposable
    {
        public TestWorldFixture Fixture { get; }
        public Map Destination { get; }
        public Group Group { get; }
        public TestWorldFixture.CapturingPlayer Peer { get; }
        public TestWorldFixture.CapturingPlayer Loader { get; }

        public LoadCtx(bool loaderGMInvisible = false)
        {
            this.Fixture = new TestWorldFixture(s => s.PartyWindowMax = 10);
            var world = this.Fixture.World;
            this.Destination = this.Fixture.AddBaseMap(2, "dest", 60, 40);
            this.Peer = this.Fixture.CommandPlayerOn(this.Destination, 10, 10, "Peer");
            this.Peer.LoginID = 201;
            this.Loader = this.Fixture.CommandPlayerOn(this.Destination, 11, 10, "Loader");
            this.Loader.LoginID = 202;
            if (loaderGMInvisible)
            {
                this.Loader.Access = Player.AccessStatus.GameMaster;
            }
            this.Destination.AddPlayer(this.Peer, world);
            this.Group = new Group();
            this.Group.AddPlayer(this.Peer, world, this.Peer);
            this.Group.AddPlayer(this.Loader, world, this.Peer);
            var charm = this.Fixture.AddBaseSpellEffect(5, "Charm",
                e => { e.Duration = 0; e.BuffGraphic = 7; e.BuffGraphicFile = 11; });
            var speed = this.Fixture.AddBaseSpellEffect(6, "Speed",
                e => { e.Duration = 0; e.BuffGraphic = 8; e.BuffGraphicFile = 12; });
            this.Loader.Buffs.Add(new Buff { Caster = this.Loader, Target = this.Loader, SpellEffect = charm });
            this.Peer.Buffs.Add(new Buff { Caster = this.Peer, Target = this.Peer, SpellEffect = speed });
            this.Loader.State = Player.States.LoadingMap;
            this.Loader.Map = null!;
            this.Loader.Sent.Clear();
            this.Peer.Sent.Clear();
        }

        public void DriveDoneLoading()
        {
            var world = this.Fixture.World;
            world.EventHandler.AddEvent(new DoneLoadingMapEvent { Player = this.Loader, Ticks = world.TimeNow });
            world.EventHandler.Update(world);
        }

        public void Dispose() => this.Fixture.Dispose();
    }

    [Fact]
    public void DoneLoadingMap_RosterPrecedesSnapshots_PeersReceiveTargetSnapshot()
    {
        using var ctx = new LoadCtx();

        ctx.DriveDoneLoading();

        AssertOrder(ctx.Loader.Sent, "GUD1,201,", "PBC201", "PBA201,");
        AssertOrder(ctx.Peer.Sent, "MKC202,", "PBC202", "PBA202,");
    }

    [Fact]
    public void DoneLoadingMap_GMInvisibleLoader_PeersReceiveNoCharacterNoSnapshot()
    {
        using var ctx = new LoadCtx(loaderGMInvisible: true);

        ctx.DriveDoneLoading();

        Assert.DoesNotContain(ctx.Peer.Sent, s => s.StartsWith("MKC202,"));
        Assert.Empty(PartyPackets(ctx.Peer.Sent));
    }

    private static TestWorldFixture.CapturingPlayer NewGm(TestWorldFixture fixture, Map map)
    {
        var gm = fixture.CommandPlayerOn(map, 50, 5, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        return gm;
    }

    [Fact]
    public void ChangeName_RepublishesCharacterThenSnapshotToInGroupViewer()
    {
        using var ctx = new Ctx();
        var fixture = ctx.Fixture;
        fixture.RegisterOnlinePlayer(ctx.Target);
        fixture.RegisterDatabasePlayer(ctx.Target);
        var gm = NewGm(fixture, ctx.Map);

        Assert.True(fixture.RunCommand(gm, "/changename Target Newname"));

        Assert.Equal("Newname", ctx.Target.Name);
        AssertOrder(ctx.Viewer.Sent, "ERC101", "MKC101,", "PBC101", "PBA101,");
    }

    [Fact]
    public void SetTitle_RepublishesCharacterThenSnapshotToInGroupViewer()
    {
        using var ctx = new Ctx();
        var fixture = ctx.Fixture;
        fixture.RegisterOnlinePlayer(ctx.Target);
        fixture.RegisterDatabasePlayer(ctx.Target);
        var gm = NewGm(fixture, ctx.Map);

        Assert.True(fixture.RunCommand(gm, "/settitle Target Lord of the Vast"));

        Assert.Equal("Lord of the Vast", ctx.Target.Title);
        AssertOrder(ctx.Viewer.Sent, "ERC101", "MKC101,", "PBC101", "PBA101,");
    }

    [Fact]
    public void SetSurname_RepublishesCharacterThenSnapshotToInGroupViewer()
    {
        using var ctx = new Ctx();
        var fixture = ctx.Fixture;
        fixture.RegisterOnlinePlayer(ctx.Target);
        fixture.RegisterDatabasePlayer(ctx.Target);
        var gm = NewGm(fixture, ctx.Map);

        Assert.True(fixture.RunCommand(gm, "/setsurname Target Smith"));

        Assert.Equal("Smith", ctx.Target.Surname);
        AssertOrder(ctx.Viewer.Sent, "ERC101", "MKC101,", "PBC101", "PBA101,");
    }

    [Fact]
    public void SetTitle_GMInvisibleTarget_ViewerReceivesNoSnapshot()
    {
        using var ctx = new Ctx();
        var fixture = ctx.Fixture;
        ctx.Target.Access = Player.AccessStatus.GameMaster;
        fixture.RegisterOnlinePlayer(ctx.Target);
        fixture.RegisterDatabasePlayer(ctx.Target);
        var gm = NewGm(fixture, ctx.Map);

        Assert.True(fixture.RunCommand(gm, "/settitle Target Lord"));

        Assert.Empty(PartyPackets(ctx.Viewer.Sent));
    }
}
