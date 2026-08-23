using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class InvisibilityAggroTests : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int ClassId = 1;

    public InvisibilityAggroTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "invis-aggro-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Quest"));
        GameWorld.Settings = new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            MaxAC = 3500, MaxPlayers = 200, MaxNPCs = 15000,
        };
        world = new GameWorld(null);

        var m = new Map { ID = MapId, Name = "Test", Width = 20, Height = 20, CanCast = true };
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

    private static NPCTemplate Template(bool seeInvisible, int id = 1) => new()
    {
        NPCTemplateID = id,
        Name = "Test NPC",
        Level = 50,
        ClassID = ClassId,
        BaseStats = new AttributeSet(),
        AggroRange = 5,
        HairA = 255,
        FaceID = 70,
        SeeInvisible = seeInvisible,
        Allies = new List<NPCTemplate>(),
    };

    private NPC SpawnNpc(NPCTemplate template, int x, int y) =>
        world.NPCHandler.SpawnNPC(world, MapId, x, y, template, shouldRespawn: false);

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

    private static void MakeInvisible(Player p, GameWorld world)
    {
        p.AddBuff(NewBuff(p, SpellEffect.EffectTypes.Invisible), world);
        Assert.True(p.IsInvisible);
    }

    [Fact]
    public void InvisiblePlayer_NpcCantSee_DoesNotAggroOrSendEmote()
    {
        var npc = SpawnNpc(Template(seeInvisible: false), 5, 5);
        var bystander = NewPlayer();
        var target = NewPlayer();
        PlacePlayer(bystander, 5, 4);
        PlacePlayer(target, 5, 6);
        MakeInvisible(target, world);

        bystander.SendBuffer.Clear();
        npc.AggroIfInRange(target, world);

        Assert.Null(npc.AggroTarget);
        Assert.False(npc.AggroTargetToValue.ContainsKey(target));
        Assert.DoesNotContain("EMOT", Buffer(bystander));
    }

    [Fact]
    public void InvisiblePlayer_NpcSeesInvisibleByTemplate_AggrosAndSendsEmote()
    {
        var npc = SpawnNpc(Template(seeInvisible: true), 5, 5);
        var bystander = NewPlayer();
        var target = NewPlayer();
        PlacePlayer(bystander, 5, 4);
        PlacePlayer(target, 5, 6);
        MakeInvisible(target, world);

        bystander.SendBuffer.Clear();
        npc.AggroIfInRange(target, world);

        Assert.Same(target, npc.AggroTarget);
        Assert.Contains("EMOT", Buffer(bystander));
    }

    [Fact]
    public void InvisiblePlayer_NpcWithSeeInvisibleBuff_Aggros()
    {
        var npc = SpawnNpc(Template(seeInvisible: false), 5, 5);
        var target = NewPlayer();
        PlacePlayer(target, 5, 6);
        npc.AddBuff(NewBuff(npc, SpellEffect.EffectTypes.SeeInvisible), world);
        MakeInvisible(target, world);

        npc.AggroIfInRange(target, world);

        Assert.Same(target, npc.AggroTarget);
    }

    [Fact]
    public void VisiblePlayer_NpcCantSeeInvisible_Aggros()
    {
        var npc = SpawnNpc(Template(seeInvisible: false), 5, 5);
        var bystander = NewPlayer();
        var target = NewPlayer();
        PlacePlayer(bystander, 5, 4);
        PlacePlayer(target, 5, 6);

        bystander.SendBuffer.Clear();
        npc.AggroIfInRange(target, world);

        Assert.Same(target, npc.AggroTarget);
        Assert.Contains("EMOT", Buffer(bystander));
    }

    [Fact]
    public void InvisiblePlayer_SeeingNpc_AggroSplashesToNonSeeingAlly()
    {
        var templateA = Template(seeInvisible: true, id: 1);
        var templateB = Template(seeInvisible: false, id: 2);
        templateA.Allies.Add(templateB);

        var a = SpawnNpc(templateA, 5, 5);
        var b = SpawnNpc(templateB, 5, 4);
        var target = NewPlayer();
        PlacePlayer(target, 5, 6);
        MakeInvisible(target, world);

        a.AggroIfInRange(target, world);

        Assert.Same(target, a.AggroTarget);
        Assert.Same(target, b.AggroTarget);
    }
}
