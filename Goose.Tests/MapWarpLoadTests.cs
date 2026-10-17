using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Goose.Events;

namespace Goose.Tests;

public class MapWarpLoadTests : IDisposable
{
    private readonly GooseSettings settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int ClassId = 1;

    public MapWarpLoadTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "mwp-mapload-" + Guid.NewGuid().ToString("N"));
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

    private Player NewLoadingPlayer()
    {
        var p = new Player(0);
        p.OnLogin();
        p.Name = "Tester";
        p.Inventory = new Inventory(p, world.Settings);
        p.Class = world.ClassHandler.GetClass(ClassId)!;
        p.Level = 1;
        p.BaseStats = new AttributeSet { HP = 100, MP = 100 };
        p.MaxStats = p.BaseStats + new AttributeSet();
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.HairA = 255;
        p.FaceID = 70;
        p.Access = Player.AccessStatus.Normal;
        p.Map = map;
        p.MapID = MapId;
        p.MapX = 10;
        p.MapY = 10;
        p.State = Player.States.LoadingMap;
        p.Sock = NewUnconnectedSocket();
        return p;
    }

    private static string[] Packets(Player p) =>
        Encoding.ASCII.GetString(p.SendBuffer.ToArray()).Split('\x1');

    private static void DriveMapLoad(GameWorld world, Player p)
    {
        var ev = new DoneLoadingMapEvent { Player = p, Ticks = world.TimeNow };
        world.EventHandler.AddEvent(ev);
        world.EventHandler.Update(world);
    }

    [Fact]
    public void MapLoad_SendsOneMwpPerWarp()
    {
        map.SetTile(2, 3, new WarpTile());
        map.SetTile(15, 20, new WarpTile());

        var p = NewLoadingPlayer();
        DriveMapLoad(world, p);

        var packets = Packets(p);
        var mwps = packets.Where(x => x.StartsWith("MWP")).ToList();
        Assert.Equal(2, mwps.Count);
        Assert.Contains("MWP2,3", mwps);
        Assert.Contains("MWP15,20", mwps);
    }

    [Fact]
    public void MapLoad_SendsWarpsAfterDoneSendingMap()
    {
        map.SetTile(2, 3, new WarpTile());

        var p = NewLoadingPlayer();
        DriveMapLoad(world, p);

        var packets = Packets(p);
        int dsm = packets.IndexOf("DSM");
        Assert.True(dsm >= 0);
        var mwpIndices = packets.Select((x, i) => i).Where(i => packets[i].StartsWith("MWP")).ToList();
        Assert.NotEmpty(mwpIndices);
        Assert.All(mwpIndices, i => Assert.True(i > dsm));
    }

    [Fact]
    public void MapLoad_MapWithoutWarps_SendsNoMwp()
    {
        var p = NewLoadingPlayer();
        DriveMapLoad(world, p);

        Assert.DoesNotContain(Packets(p), x => x.StartsWith("MWP"));
    }
}
