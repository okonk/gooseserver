using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Goose.Tests;

public class NPCMessageTests : IDisposable
{
    private readonly GooseSettings settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int ClassId = 1;

    public NPCMessageTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "npc-message-" + Guid.NewGuid().ToString("N"));
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
        p.BaseStats = new AttributeSet { HP = 10000, MP = 100 };
        p.MaxStats = p.BaseStats + new AttributeSet();
        p.CurrentHP = 10000;
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

    private static NPCTemplate Template(
        string aggroMessage = "", string stuckMessage = "",
        NPCTemplate.BehaviourTypes behaviour = NPCTemplate.BehaviourTypes.DoNothing,
        int id = 1) => new()
    {
        NPCTemplateID = id,
        Name = "Test NPC",
        Level = 50,
        ClassID = ClassId,
        BaseStats = new AttributeSet(),
        AggroRange = 5,
        AttackRange = 1,
        WeaponDamage = 1,
        HairA = 255,
        FaceID = 70,
        Allies = new List<NPCTemplate>(),
        AggroMessage = aggroMessage,
        StuckMessage = stuckMessage,
        Behaviour = behaviour,
        BehaviourTimeout = 1,
    };

    private NPC SpawnNpc(NPCTemplate template, int x, int y) =>
        world.NPCHandler.SpawnNPC(world, MapId, x, y, template, shouldRespawn: false)!;

    private static string Buffer(Player p) => Encoding.ASCII.GetString(p.SendBuffer.ToArray());

    private static void MakeStuck(NPC npc, GameWorld world)
    {
        npc.LastAttackTime = world.TimeNow - 10 * world.TimerFrequency;
    }

    [Fact]
    public void AggroMessage_SentToEveryoneInRange_OnProximityAggro()
    {
        var npc = SpawnNpc(Template(aggroMessage: "Grrr!"), 5, 5);
        var bystander = NewPlayer();
        var target = NewPlayer();
        PlacePlayer(bystander, 5, 4);
        PlacePlayer(target, 5, 6);

        bystander.SendBuffer.Clear();
        target.SendBuffer.Clear();
        npc.AggroIfInRange(target, world);

        Assert.Same(target, npc.AggroTarget);
        Assert.Contains("$7Grrr!", Buffer(bystander));
        Assert.Contains("$7Grrr!", Buffer(target));
    }

    [Fact]
    public void AggroMessage_SentOnDamageAggro()
    {
        var npc = SpawnNpc(Template(aggroMessage: "Who struck me?!"), 5, 5);
        var attacker = NewPlayer();
        PlacePlayer(attacker, 5, 6);
        attacker.SendBuffer.Clear();

        npc.AddAggro(attacker, 10, world);

        Assert.Contains("$7Who struck me?!", Buffer(attacker));
    }

    [Fact]
    public void AggroMessage_NotSentAgain_WhileAlreadyAggroed()
    {
        var npc = SpawnNpc(Template(aggroMessage: "Grrr!"), 5, 5);
        var target = NewPlayer();
        var newcomer = NewPlayer();
        PlacePlayer(target, 5, 6);
        PlacePlayer(newcomer, 5, 7);

        npc.AddAggro(target, 1, world);
        target.SendBuffer.Clear();
        newcomer.SendBuffer.Clear();

        npc.AddAggro(target, 5, world);
        npc.AddAggro(newcomer, 3, world);

        Assert.DoesNotContain("$7Grrr!", Buffer(target));
        Assert.DoesNotContain("$7Grrr!", Buffer(newcomer));
    }

    [Fact]
    public void BlankAggroMessage_SendsNothing()
    {
        var npc = SpawnNpc(Template(), 5, 5);
        var bystander = NewPlayer();
        var target = NewPlayer();
        PlacePlayer(bystander, 5, 4);
        PlacePlayer(target, 5, 6);

        bystander.SendBuffer.Clear();
        npc.AggroIfInRange(target, world);

        Assert.Same(target, npc.AggroTarget);
        Assert.DoesNotContain("$7", Buffer(bystander));
    }

    [Fact]
    public void StuckMessage_TeleportAggro_SentWhenBehaviourFires()
    {
        var npc = SpawnNpc(Template(stuckMessage: "Come here!", behaviour: NPCTemplate.BehaviourTypes.TeleportAggro), 5, 5);
        var target = NewPlayer();
        var bystander = NewPlayer();
        PlacePlayer(target, 5, 10);
        PlacePlayer(bystander, 6, 10);

        npc.AddAggro(target, 1, world);
        MakeStuck(npc, world);
        target.SendBuffer.Clear();
        bystander.SendBuffer.Clear();

        npc.HandleAttackEvent(world);

        Assert.True(Math.Abs(target.MapX - npc.MapX) <= 1 && Math.Abs(target.MapY - npc.MapY) <= 1);
        Assert.Contains("$7Come here!", Buffer(target));
        Assert.Contains("$7Come here!", Buffer(bystander));
    }

    [Fact]
    public void StuckMessage_TeleportToAggro_SentWhenBehaviourFires()
    {
        var npc = SpawnNpc(Template(stuckMessage: "I see you!", behaviour: NPCTemplate.BehaviourTypes.TeleportToAggro), 5, 5);
        var target = NewPlayer();
        var bystander = NewPlayer();
        PlacePlayer(target, 5, 10);
        PlacePlayer(bystander, 6, 10);

        npc.AddAggro(target, 1, world);
        MakeStuck(npc, world);
        target.SendBuffer.Clear();
        bystander.SendBuffer.Clear();

        npc.HandleAttackEvent(world);

        Assert.True(Map.InRange(npc, target));
        Assert.Contains("$7I see you!", Buffer(target));
        Assert.Contains("$7I see you!", Buffer(bystander));
    }

    [Fact]
    public void StuckMessage_NotSent_WhenBehaviourIsDoNothing()
    {
        var npc = SpawnNpc(Template(stuckMessage: "Should not appear"), 5, 5);
        var target = NewPlayer();
        PlacePlayer(target, 5, 10);

        npc.AddAggro(target, 1, world);
        MakeStuck(npc, world);
        target.SendBuffer.Clear();

        npc.HandleAttackEvent(world);

        Assert.DoesNotContain("$7Should not appear", Buffer(target));
    }

    [Fact]
    public void StuckMessage_NotSent_BeforeTimeoutElapses()
    {
        var npc = SpawnNpc(Template(stuckMessage: "Not yet!", behaviour: NPCTemplate.BehaviourTypes.TeleportAggro), 5, 5);
        var target = NewPlayer();
        PlacePlayer(target, 5, 10);

        npc.AddAggro(target, 1, world);
        target.SendBuffer.Clear();

        npc.HandleAttackEvent(world);

        Assert.Equal((5, 10), (target.MapX, target.MapY));
        Assert.DoesNotContain("$7Not yet!", Buffer(target));
    }
}
