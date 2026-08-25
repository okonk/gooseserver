using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Goose.Events;

namespace Goose.Tests;

public class InvisibilityMapLoadTests : IDisposable
{
    private readonly GooseSettings settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int ClassId = 1;

    public InvisibilityMapLoadTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "invis-mapload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Quest"));
        settings = new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            MaxAC = 3500, MaxPlayers = 200, MaxNPCs = 15000,
        };
        world = new GameWorld(settings);

        var m = new Map { ID = MapId, Name = "Test", Width = 20, Height = 20 };
        m.characters = new ICharacter[(m.Width + 1) * (m.Height + 1)];
        m.tiles = new ITile[(m.Width + 1) * (m.Height + 1)];
        world.MapHandler.Maps[MapId] = m;
        map = m;

        RegisterClass(ClassId, "Test", level: 1);
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
        foreach (var s in sockets) s.Dispose();
        if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
    }

    private Socket NewUnconnectedSocket()
    {
        var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };
        sockets.Add(s);
        return s;
    }

    private Player NewLoadingPlayer()
    {
        var p = new Player(0);
        p.OnLogin();
        p.Name = "Tester";
        p.Inventory = new Inventory(p, world.Settings);
        p.Class = world.ClassHandler.GetClass(ClassId);
        p.Level = 1;
        p.BaseStats = new AttributeSet { HP = 100, MP = 100 };
        p.MaxStats = p.BaseStats + new AttributeSet();
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.HairA = 255;
        p.FaceID = 70;
        p.Access = Player.AccessStatus.Normal;
        // Map ref is needed by AddBuff's range lookup; the event reassigns it and
        // does the actual placement itself.
        p.Map = map;
        p.MapID = MapId;
        p.MapX = 10;
        p.MapY = 10;
        p.State = Player.States.LoadingMap;
        p.Sock = NewUnconnectedSocket();
        return p;
    }

    private static Buff NewSeeInvisibleBuff(Player owner) => new()
    {
        Target = owner,
        Caster = owner,
        SpellEffect = new SpellEffect { EffectType = SpellEffect.EffectTypes.SeeInvisible, Duration = 1000 },
    };

    private static string Buffer(Player p) => Encoding.ASCII.GetString(p.SendBuffer.ToArray());

    private static void DriveMapLoad(GameWorld world, Player p)
    {
        var ev = new DoneLoadingMapEvent { Player = p, Ticks = world.TimeNow };
        world.EventHandler.AddEvent(ev);
        world.EventHandler.Update(world);
    }

    [Fact]
    public void MapLoad_GmPlayer_SendsSINVS1()
    {
        var p = NewLoadingPlayer();
        p.Access = Player.AccessStatus.GameMaster;

        DriveMapLoad(world, p);

        Assert.Contains("SINVS1", Buffer(p));
    }

    [Fact]
    public void MapLoad_PlayerWithSeeInvisibleBuff_SendsSINVS1()
    {
        var p = NewLoadingPlayer();
        p.AddBuff(NewSeeInvisibleBuff(p), world);

        DriveMapLoad(world, p);

        Assert.Contains("SINVS1", Buffer(p));
    }

    [Fact]
    public void MapLoad_PlayerWithoutSeeInvisible_SendsExplicitSINVS0()
    {
        var p = NewLoadingPlayer();

        DriveMapLoad(world, p);

        string buf = Buffer(p);
        Assert.Contains("SINVS0", buf);
        Assert.DoesNotContain("SINVS1", buf);
    }
}
