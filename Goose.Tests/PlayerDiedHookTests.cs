using System.Net.Sockets;
using System.Reflection;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class PlayerDiedHookTests : IDisposable
{
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly Map boundMap;
    private readonly Map lobby;
    private readonly List<Socket> sockets = new();

    private const int ClassId = 1;

    public PlayerDiedHookTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "player-died-hook-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Quest"));
        world = new GameWorld(new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            MaxAC = 3500, MaxPlayers = 200, MaxNPCs = 15000, DamageModifier = 1,
        });

        map = AddMap(1);
        boundMap = AddMap(2);
        lobby = AddMap(3);

        var cls = new Class { ClassID = ClassId, ClassName = "Test", ACMultiplier = 1.0 };
        cls.AddLevel(new ClassLevel { Level = 50, BaseStats = new AttributeSet() });
        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.ClassHandler)!;
        classes[ClassId] = cls;
    }

    private Map AddMap(int id)
    {
        var m = new Map { ID = id, Name = "Map " + id, Width = 20, Height = 20, CanCast = true };
        m.characters = new ICharacter[(m.Width + 1) * (m.Height + 1)];
        m.tiles = new ITile[(m.Width + 1) * (m.Height + 1)];
        world.MapHandler.Maps[id] = m;
        return m;
    }

    public void Dispose()
    {
        foreach (var s in sockets) s.Dispose();
        if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
    }

    private Player PlayerAt(int x, int y, long hp)
    {
        var p = new Player(0);
        p.OnLogin();
        p.Inventory = new Inventory(p, world.Settings);
        var klass = new Class { ClassID = ClassId, ClassName = "Test", ACMultiplier = 1.0 };
        klass.AddLevel(new ClassLevel { Level = 1, ClassID = ClassId, BaseStats = new AttributeSet() });
        p.Class = klass;
        p.Level = 1;
        p.BaseStats = new AttributeSet { HP = hp, MP = 100 };
        p.MaxStats = p.BaseStats + new AttributeSet();
        p.CurrentHP = hp;
        p.CurrentMP = 100;
        p.HairA = 255;
        p.FaceID = 70;
        p.Access = Player.AccessStatus.Normal;
        p.State = Player.States.Ready;
        p.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };
        sockets.Add(p.Sock);
        p.BoundMap = boundMap;
        p.BoundX = 5;
        p.BoundY = 5;

        p.Map = map;
        p.MapID = map.ID;
        p.MapX = x;
        p.MapY = y;
        map.AddPlayer(p, world);
        map.PlaceCharacter(p);
        map.SetCharacter(p, x, y);
        return p;
    }

    private NPC NpcAt(int x, int y) => world.NPCHandler.SpawnNPC(world, map.ID, x, y, new NPCTemplate
    {
        NPCTemplateID = 1,
        Name = "Test NPC",
        Level = 50,
        ClassID = ClassId,
        BaseStats = new AttributeSet { HP = 1000 },
        WeaponDamage = 5000,
        AttackRange = 1,
        AggroRange = 5,
        HairA = 255,
        FaceID = 70,
    }, shouldRespawn: false)!;

    private RecordingGlobalScript RegisterGlobalRecorder(string path)
    {
        var recorder = new RecordingGlobalScript();
        var script = ScriptStub.For<IGlobalScript>(recorder);
        script.FilePath = path;
        this.world.ScriptHandler.TryAdd(script);
        return recorder;
    }

    private void DriveMapLoad(Player player)
    {
        var ev = new DoneLoadingMapEvent { Player = player, Ticks = this.world.TimeNow };
        this.world.EventHandler.AddEvent(ev);
        this.world.EventHandler.Update(this.world);
    }

    [Fact]
    public void Death_without_scripts_respawns_at_bind_point()
    {
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        DriveMapLoad(player);

        Assert.Same(boundMap, player.Map);
        Assert.Equal(5, player.MapX);
        Assert.Equal(5, player.MapY);
    }

    [Fact]
    public void Map_override_respawns_at_override_location()
    {
        map.Script = ScriptStub.For<IMapScript>(new OverrideMapScript(lobby, 3, 4));
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        DriveMapLoad(player);

        Assert.Same(lobby, player.Map);
        Assert.Equal(3, player.MapX);
        Assert.Equal(4, player.MapY);
        Assert.Same(boundMap, player.BoundMap);
        Assert.Equal(5, player.BoundX);
        Assert.Equal(5, player.BoundY);
    }

    [Fact]
    public void Map_override_to_death_map_respawns_at_override_location()
    {
        map.Script = ScriptStub.For<IMapScript>(new OverrideMapScript(map, 7, 8));
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        Assert.Same(map, player.Map);
        Assert.Equal(7, player.MapX);
        Assert.Equal(8, player.MapY);
        Assert.Same(boundMap, player.BoundMap);
        Assert.Equal(5, player.BoundX);
        Assert.Equal(5, player.BoundY);
    }

    [Fact]
    public void Map_hook_sees_the_death_map_and_killer()
    {
        var recorder = new RecordingMapScript();
        map.Script = ScriptStub.For<IMapScript>(recorder);
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        Assert.Single(recorder.Dead);
        var (m, playerMap, k) = recorder.Dead[0];
        Assert.Same(map, m);
        Assert.Same(map, playerMap);
        Assert.Same(npc, k);
    }

    [Fact]
    public void Global_hook_receives_player_and_killer()
    {
        var recorder = RegisterGlobalRecorder("Scripts/Global/Record.csx");
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        Assert.Single(recorder.Dead);
        var (p, k) = recorder.Dead[0];
        Assert.Same(player, p);
        Assert.Same(npc, k);
    }

    [Fact]
    public void Throwing_map_hook_falls_back_to_bind_point()
    {
        map.Script = ScriptStub.For<IMapScript>(new ThrowingMapScript());
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        DriveMapLoad(player);

        Assert.Same(boundMap, player.Map);
        Assert.Equal(5, player.MapX);
        Assert.Equal(5, player.MapY);
    }

    [Fact]
    public void Throwing_global_script_does_not_stop_others_or_the_respawn()
    {
        var throwing = ScriptStub.For<IGlobalScript>(new ThrowingGlobalScript());
        throwing.FilePath = "Scripts/Global/Throw.csx";
        this.world.ScriptHandler.TryAdd(throwing);
        var recorder = RegisterGlobalRecorder("Scripts/Global/Record.csx");
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        DriveMapLoad(player);

        Assert.Single(recorder.Dead);
        Assert.Same(boundMap, player.Map);
        Assert.Equal(5, player.MapX);
        Assert.Equal(5, player.MapY);
    }

    [Fact]
    public void Non_lethal_hit_does_not_fire_hooks()
    {
        var mapRecorder = new RecordingMapScript();
        map.Script = ScriptStub.For<IMapScript>(mapRecorder);
        var globalRecorder = RegisterGlobalRecorder("Scripts/Global/Record.csx");
        var player = PlayerAt(5, 6, hp: 100000);
        var npc = NpcAt(5, 5);

        npc.Attack(player, world);

        Assert.Empty(mapRecorder.Dead);
        Assert.Empty(globalRecorder.Dead);
        Assert.Same(map, player.Map);
        Assert.Equal(5, player.MapX);
        Assert.Equal(6, player.MapY);
    }

    private sealed class OverrideMapScript : BaseMapScript
    {
        private readonly Map respawnMap;
        private readonly int respawnX;
        private readonly int respawnY;

        public OverrideMapScript(Map respawnMap, int respawnX, int respawnY)
        {
            this.respawnMap = respawnMap;
            this.respawnX = respawnX;
            this.respawnY = respawnY;
        }

        public override RespawnLocation? OnPlayerDied(Map map, Player player, ICharacter killer, GameWorld world)
            => new(this.respawnMap, this.respawnX, this.respawnY);
    }

    private sealed class RecordingMapScript : BaseMapScript
    {
        public List<(Map Map, Map PlayerMap, ICharacter Killer)> Dead { get; } = [];

        public override RespawnLocation? OnPlayerDied(Map map, Player player, ICharacter killer, GameWorld world)
        {
            this.Dead.Add((map, player.Map, killer));
            return null;
        }
    }

    private sealed class ThrowingMapScript : BaseMapScript
    {
        public override RespawnLocation? OnPlayerDied(Map map, Player player, ICharacter killer, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class RecordingGlobalScript : BaseGlobalScript
    {
        public List<(Player Player, ICharacter Killer)> Dead { get; } = [];

        public override void OnPlayerDied(Player player, ICharacter killer, GameWorld world)
        {
            this.Dead.Add((player, killer));
        }
    }

    private sealed class ThrowingGlobalScript : BaseGlobalScript
    {
        public override void OnPlayerDied(Player player, ICharacter killer, GameWorld world)
            => throw new InvalidOperationException("boom");
    }
}
