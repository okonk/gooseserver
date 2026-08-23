using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class InvisibilityTransitionTests : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int ClassId = 1;

    public InvisibilityTransitionTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "invis-transition-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Quest"));
        GameWorld.Settings = new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            MaxAC = 3500, MaxPlayers = 200, MaxNPCs = 15000,
        };
        world = new GameWorld(null);

        var m = new Map { ID = MapId, Name = "Test", Width = 20, Height = 20 };
        m.characters = new ICharacter[(m.Width + 1) * (m.Height + 1)];
        m.tiles = new ITile[(m.Width + 1) * (m.Height + 1)];
        world.MapHandler.Maps[MapId] = m;
        map = m;

        RegisterClass(ClassId, "Test", level: 50);
    }

    private void RegisterClass(int id, string name, int level)
    {
        var cls = new Class { ClassID = id, ClassName = name, ACMultiplier = 1m };
        cls.AddLevel(new ClassLevel { Level = level, BaseStats = new AttributeSet() });

        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.ClassHandler)!;
        classes[id] = cls;
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        foreach (var s in sockets) s.Dispose();
        if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
    }

    private Socket NewUnconnectedSocket()
    {
        var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };
        sockets.Add(s);
        return s;
    }

    private Player NewPlayer()
    {
        var p = new Player(0);
        p.OnLogin();
        p.Inventory = new Inventory(p);
        var klass = new Class { ClassID = ClassId, ClassName = "Test", ACMultiplier = 1m };
        klass.AddLevel(new ClassLevel { Level = 1, ClassID = ClassId, BaseStats = new AttributeSet() });
        p.Class = klass;
        p.BaseStats = new AttributeSet { HP = 100, MP = 100 };
        p.MaxStats = p.BaseStats + new AttributeSet();
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.HairA = 255;
        p.FaceID = 70;
        p.Access = Player.AccessStatus.Normal;
        p.State = Player.States.Ready;
        p.Sock = NewUnconnectedSocket();
        return p;
    }

    private void PlacePlayer(Player p, int x, int y)
    {
        p.Map = map;
        p.MapID = MapId;
        p.MapX = x;
        p.MapY = y;
        map.AddPlayer(p, world);
        map.PlaceCharacter(p);
        map.SetCharacter(p, x, y);
    }

    private NPCTemplate Template(bool seeInvisible = false) => new()
    {
        NPCTemplateID = 1,
        Name = "Test NPC",
        Level = 50,
        ClassID = ClassId,
        BaseStats = new AttributeSet(),
        AggroRange = 5,
        HairA = 255,
        FaceID = 70,
        SeeInvisible = seeInvisible,
    };

    private NPC SpawnNpc(NPCTemplate template) =>
        world.NPCHandler.SpawnNPC(world, MapId, 5, 5, template, shouldRespawn: false);

    private static Buff NewBuff(ICharacter owner, SpellEffect.EffectTypes effectType)
    {
        return new Buff
        {
            Target = owner,
            Caster = owner,
            SpellEffect = new SpellEffect { EffectType = effectType, Duration = 1000 },
        };
    }

    private static string Buffer(Player p) => Encoding.ASCII.GetString(p.SendBuffer.ToArray());

    private static void SeedMoveSpeed(Player p, int speed)
    {
        var q = (PriorityQueue<int, int>)typeof(Player)
            .GetProperty("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(p)!;
        q.Enqueue(speed, speed);
        q.Enqueue(speed, speed);
    }

    [Fact]
    public void Player_TogglingInvisibility_ReceivesSelfCHP()
    {
        var b = NewPlayer();
        PlacePlayer(b, 5, 5);

        var buff = NewBuff(b, SpellEffect.EffectTypes.Invisible);
        b.SendBuffer.Clear();
        b.AddBuff(buff, world);

        string buf = Buffer(b);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,1,70,", buf);

        b.SendBuffer.Clear();
        b.RemoveBuff(buff, world);

        buf = Buffer(b);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,0,70,", buf);
    }

    [Fact]
    public void Player_BecomingInvisible_BroadcastsCHPWithInvisFlagToBystanders()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        PlacePlayer(a, 5, 5);
        PlacePlayer(b, 5, 6);

        var buff = NewBuff(b, SpellEffect.EffectTypes.Invisible);
        a.SendBuffer.Clear();
        b.AddBuff(buff, world);

        string buf = Buffer(a);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,1,70,", buf);
        Assert.DoesNotContain(",255,0,70,", buf);

        a.SendBuffer.Clear();
        b.RemoveBuff(buff, world);

        buf = Buffer(a);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,0,70,", buf);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void Npc_BecomingInvisible_BroadcastsCHPWithInvisFlagToPlayers()
    {
        var p = NewPlayer();
        PlacePlayer(p, 5, 6);
        var npc = SpawnNpc(Template());

        // Spawn sent an MKC carrying the same invis field region; drop it so the
        // asserts only see the transition broadcast.
        p.SendBuffer.Clear();

        var buff = NewBuff(npc, SpellEffect.EffectTypes.Invisible);
        npc.AddBuff(buff, world);

        string buf = Buffer(p);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,1,70,", buf);
        Assert.DoesNotContain(",255,0,70,", buf);

        p.SendBuffer.Clear();
        npc.RemoveBuff(buff, world);

        buf = Buffer(p);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,0,70,", buf);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void Player_BecomingInvisible_ClearsAggroOfNpcsWithoutInvisibilitySight()
    {
        var p = NewPlayer();
        PlacePlayer(p, 5, 6);
        var npc = SpawnNpc(Template(seeInvisible: false));
        Assert.False(npc.CanSeeInvisible);

        npc.AddAggro(p, 1, world);
        Assert.Same(p, npc.AggroTarget);

        p.AddBuff(NewBuff(p, SpellEffect.EffectTypes.Invisible), world);

        Assert.Null(npc.AggroTarget);
    }

    [Fact]
    public void Player_BecomingInvisible_KeepsAggroOfNpcsWithInvisibilitySight()
    {
        var p = NewPlayer();
        PlacePlayer(p, 5, 6);
        var npc = SpawnNpc(Template(seeInvisible: true));
        Assert.True(npc.CanSeeInvisible);

        npc.AddAggro(p, 1, world);
        Assert.Same(p, npc.AggroTarget);

        p.AddBuff(NewBuff(p, SpellEffect.EffectTypes.Invisible), world);

        Assert.Same(p, npc.AggroTarget);
    }

    [Fact]
    public void Player_GainingAndLosingSeeInvisible_SendsSINVSPackets()
    {
        var p = NewPlayer();
        PlacePlayer(p, 5, 5);
        Assert.Equal(Player.AccessStatus.Normal, p.Access);

        var buff = NewBuff(p, SpellEffect.EffectTypes.SeeInvisible);
        p.AddBuff(buff, world);
        Assert.Contains("SINVS1", Buffer(p));
        Assert.DoesNotContain("SINVS0", Buffer(p));

        p.RemoveBuff(buff, world);
        Assert.Contains("SINVS0", Buffer(p));
    }

    [Fact]
    public void GmPlayer_GainingAndLosingSeeInvisible_ReceivesNoSINVSPackets()
    {
        var p = NewPlayer();
        p.Access = Player.AccessStatus.GameMaster;
        PlacePlayer(p, 5, 5);

        var buff = NewBuff(p, SpellEffect.EffectTypes.SeeInvisible);
        p.AddBuff(buff, world);
        p.RemoveBuff(buff, world);

        Assert.DoesNotContain("SINVS", Buffer(p));
    }

    [Fact]
    public void Player_NonInvisibleBuff_SendsNoInvisCHPandNoSINVS()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        PlacePlayer(a, 5, 5);
        PlacePlayer(b, 5, 6);

        var buff = NewBuff(b, SpellEffect.EffectTypes.Buff);
        b.AddBuff(buff, world);
        b.RemoveBuff(buff, world);

        Assert.DoesNotContain("CHP", Buffer(a));
        Assert.DoesNotContain("CHP", Buffer(b));
        Assert.DoesNotContain("SINVS", Buffer(b));
    }

    [Fact]
    public void Player_RemovingOneOfTwoInvisibleBuffs_BroadcastsCHPExactlyOnceOnLastRemoval()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        PlacePlayer(a, 5, 5);
        PlacePlayer(b, 5, 6);

        var first = NewBuff(b, SpellEffect.EffectTypes.Invisible);
        var second = NewBuff(b, SpellEffect.EffectTypes.Invisible);
        b.AddBuff(first, world);
        b.AddBuff(second, world);
        Assert.Equal(2, b.InvisibleBuffCount);

        a.SendBuffer.Clear();
        b.RemoveBuff(first, world);

        // 2 -> 1 is not a transition: no CHP at all, not even a flag-1 one.
        Assert.DoesNotContain("CHP", Buffer(a));

        a.SendBuffer.Clear();
        b.RemoveBuff(second, world);

        string buf = Buffer(a);
        Assert.Equal(1, buf.Split("CHP").Length - 1);
        Assert.Contains(",255,0,70,", buf);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void Player_RenewingInvisibleBuffToNonInvisibleType_NoStaleInvisCHPFromStatBroadcast()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        PlacePlayer(a, 5, 5);
        PlacePlayer(b, 5, 6);
        SeedMoveSpeed(b, 320);

        var seA = new SpellEffect { EffectType = SpellEffect.EffectTypes.Invisible, Duration = 1000 };
        seA.Stats = new AttributeSet { MoveSpeed = 160 };
        b.AddBuff(new Buff { Target = b, Caster = b, SpellEffect = seA }, world);
        Assert.True(b.IsInvisible);

        a.SendBuffer.Clear();

        var seB = new SpellEffect { EffectType = SpellEffect.EffectTypes.Buff, Duration = 1000 };
        seB.Stats = new AttributeSet { MoveSpeed = 120 };
        seB.BuffStacksOver.Add(seA);
        b.AddBuff(new Buff { Target = b, Caster = b, SpellEffect = seB }, world);

        Assert.False(b.IsInvisible);
        Assert.Equal(0, b.InvisibleBuffCount);

        string buf = Buffer(a);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,0,70,", buf);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void Player_InLoadingState_GetsInvisibleBuff_NoPacketsAndNoException()
    {
        var p = NewPlayer();
        p.State = Player.States.LoadingGame;
        p.Map = null;

        var buff = NewBuff(p, SpellEffect.EffectTypes.Invisible);
        p.AddBuff(buff, world);

        Assert.Equal(1, p.InvisibleBuffCount);
        Assert.True(p.IsInvisible);
        Assert.DoesNotContain("CHP", Buffer(p));
        Assert.DoesNotContain("SINVS", Buffer(p));
    }
}
