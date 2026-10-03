using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Goose.Tests;

public class NPCUnreachableTeleportTests : IDisposable
{
    private readonly GooseSettings settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly Map map2;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int MapId2 = 2;
    private const int ClassId = 1;

    public NPCUnreachableTeleportTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "npc-unreach-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Quest"));
        settings = new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            MaxAC = 3500, MaxPlayers = 200, MaxNPCs = 15000,
        };
        world = new GameWorld(settings);

        var m = new Map { ID = MapId, Name = "Test", Width = 20, Height = 20, CanCast = true };
        m.characters = new ICharacter[(m.Width + 1) * (m.Height + 1)];
        m.tiles = new ITile[(m.Width + 1) * (m.Height + 1)];
        world.MapHandler.Maps[MapId] = m;
        map = m;

        var m2 = new Map { ID = MapId2, Name = "Test2", Width = 20, Height = 20, CanCast = true };
        m2.characters = new ICharacter[(m2.Width + 1) * (m2.Height + 1)];
        m2.tiles = new ITile[(m2.Width + 1) * (m2.Height + 1)];
        world.MapHandler.Maps[MapId2] = m2;
        map2 = m2;

        RegisterClass(ClassId, "Test", level: 50);
    }

    private void RegisterClass(int id, string name, int level)
    {
        var cls = new Class { ClassID = id, ClassName = name, ACMultiplier = 1.0 };
        cls.AddLevel(new ClassLevel { Level = level, BaseStats = new AttributeSet() });

        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.ClassHandler)!;
        classes[id] = cls;
    }

    public void Dispose()
    {
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
        p.Inventory = new Inventory(p, world.Settings);
        var klass = new Class { ClassID = ClassId, ClassName = "Test", ACMultiplier = 1.0 };
        klass.AddLevel(new ClassLevel { Level = 1, ClassID = ClassId, BaseStats = new AttributeSet() });
        p.Class = klass;
        p.BaseStats = new AttributeSet { HP = 1_000_000, MP = 100 };
        p.MaxStats = p.BaseStats + new AttributeSet();
        p.CurrentHP = 1_000_000;
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
        PlacePlayer(p, map, MapId, x, y);
    }

    private void PlacePlayer(Player p, Map target, int targetId, int x, int y)
    {
        p.Map = target;
        p.MapID = targetId;
        p.MapX = x;
        p.MapY = y;
        target.AddPlayer(p, world);
        target.PlaceCharacter(p);
        target.SetCharacter(p, x, y);
    }

    private void SealPocket(int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                map.SetTile(x + dx, y + dy, new BlockedTile());
            }
    }

    private static NPCTemplate Template(NPCTemplate.BehaviourTypes behaviour, int id = 1) => new()
    {
        NPCTemplateID = id,
        Name = "Test NPC",
        Level = 50,
        ClassID = ClassId,
        BaseStats = new AttributeSet(),
        AggroRange = 15,
        AttackRange = 1,
        AttackSpeed = 1,
        MoveSpeed = 0,
        Behaviour = behaviour,
        BehaviourTimeout = 60,
        StuckMessage = "STUCKMSG",
        HairA = 255,
        FaceID = 70,
        Allies = new List<NPCTemplate>(),
    };

    private NPC SpawnNpc(NPCTemplate template, int x, int y) =>
        world.NPCHandler.SpawnNPC(world, MapId, x, y, template, shouldRespawn: false)!;

    private static Buff NewRootBuff(ICharacter owner)
    {
        return new Buff
        {
            Target = owner,
            Caster = owner,
            SpellEffect = new SpellEffect { EffectType = SpellEffect.EffectTypes.Root, Duration = 1000 },
        };
    }

    private static string Buffer(Player p) => Encoding.ASCII.GetString(p.SendBuffer.ToArray());

    private static bool NearNpc(Player p, NPC npc, int radius = 2) =>
        Math.Max(Math.Abs(p.MapX - npc.MapX), Math.Abs(p.MapY - npc.MapY)) <= radius;

    [Fact]
    public void UnreachablePlayer_FreshTimer_WarpedImmediatelyAndStuckMessageSent()
    {
        SealPocket(17, 17);
        var npc = SpawnNpc(Template(NPCTemplate.BehaviourTypes.TeleportAggroIfUnreachable), 5, 5);
        var player = NewPlayer();
        PlacePlayer(player, 17, 17);

        player.SendBuffer.Clear();
        npc.AddAggro(player, 10, world);
        npc.HandleAttackEvent(world);

        Assert.True(NearNpc(player, npc));
        Assert.Contains("ATT" + npc.LoginID, Buffer(player));
        Assert.Contains("STUCKMSG", Buffer(player));
    }

    [Fact]
    public void ReachableFarPlayer_FreshTimer_NotWarped()
    {
        var npc = SpawnNpc(Template(NPCTemplate.BehaviourTypes.TeleportAggroIfUnreachable), 5, 5);
        var player = NewPlayer();
        PlacePlayer(player, 17, 5);

        player.SendBuffer.Clear();
        npc.AddAggro(player, 10, world);
        npc.HandleAttackEvent(world);

        Assert.Equal(17, player.MapX);
        Assert.Equal(5, player.MapY);
        Assert.DoesNotContain("STUCKMSG", Buffer(player));
    }

    [Fact]
    public void ReachableFarPlayer_TimerExpired_WarpedByTimerBranch()
    {
        var npc = SpawnNpc(Template(NPCTemplate.BehaviourTypes.TeleportAggroIfUnreachable), 5, 5);
        var player = NewPlayer();
        PlacePlayer(player, 17, 5);

        player.SendBuffer.Clear();
        npc.AddAggro(player, 10, world);
        npc.LastAttackTime = world.TimeNow - 61L * world.TimerFrequency;
        npc.HandleAttackEvent(world);

        Assert.True(NearNpc(player, npc));
        Assert.Contains("ATT" + npc.LoginID, Buffer(player));
        Assert.Contains("STUCKMSG", Buffer(player));
    }

    [Fact]
    public void UnreachableButWithinThreshold_NotWarped()
    {
        SealPocket(7, 7);
        var npc = SpawnNpc(Template(NPCTemplate.BehaviourTypes.TeleportAggroIfUnreachable), 5, 5);
        var player = NewPlayer();
        PlacePlayer(player, 7, 7);

        player.SendBuffer.Clear();
        npc.AddAggro(player, 10, world);
        npc.HandleAttackEvent(world);

        Assert.Equal(7, player.MapX);
        Assert.Equal(7, player.MapY);
        Assert.DoesNotContain("STUCKMSG", Buffer(player));
    }

    [Fact]
    public void Rooted_UnreachablePlayer_NotWarped()
    {
        SealPocket(17, 17);
        var npc = SpawnNpc(Template(NPCTemplate.BehaviourTypes.TeleportAggroIfUnreachable), 5, 5);
        var player = NewPlayer();
        PlacePlayer(player, 17, 17);
        npc.AddBuff(NewRootBuff(npc), world);

        player.SendBuffer.Clear();
        npc.AddAggro(player, 10, world);
        npc.HandleAttackEvent(world);

        Assert.Equal(17, player.MapX);
        Assert.Equal(17, player.MapY);
        Assert.DoesNotContain("STUCKMSG", Buffer(player));
    }

    [Fact]
    public void AggroTargetOnOtherMap_NotWarped()
    {
        SealPocket(17, 17);
        var npc = SpawnNpc(Template(NPCTemplate.BehaviourTypes.TeleportAggroIfUnreachable), 5, 5);
        var player = NewPlayer();
        PlacePlayer(player, map2, MapId2, 17, 17);

        player.SendBuffer.Clear();
        npc.AddAggro(player, 10, world);
        npc.HandleAttackEvent(world);

        Assert.Equal(MapId2, player.MapID);
        Assert.Equal(17, player.MapX);
        Assert.Equal(17, player.MapY);
        Assert.DoesNotContain("STUCKMSG", Buffer(player));
    }

    [Fact]
    public void OldTeleportAggroMode_FreshTimer_NotWarped()
    {
        SealPocket(17, 17);
        var npc = SpawnNpc(Template(NPCTemplate.BehaviourTypes.TeleportAggro), 5, 5);
        var player = NewPlayer();
        PlacePlayer(player, 17, 17);

        player.SendBuffer.Clear();
        npc.AddAggro(player, 10, world);
        npc.HandleAttackEvent(world);

        Assert.Equal(17, player.MapX);
        Assert.Equal(17, player.MapY);
        Assert.DoesNotContain("STUCKMSG", Buffer(player));
    }
}
