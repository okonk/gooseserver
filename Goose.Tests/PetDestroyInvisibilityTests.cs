using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class PetDestroyInvisibilityTests : IDisposable
{
    private readonly GooseSettings settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int ClassId = 1;

    public PetDestroyInvisibilityTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "invis-petdestroy-" + Guid.NewGuid().ToString("N"));
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
        p.Inventory = new Inventory(p, world.Configuration);
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

    private Pet NewPet()
    {
        var klass = new Class { ClassID = ClassId, ClassName = "Test", ACMultiplier = 1m };
        klass.AddLevel(new ClassLevel { Level = 1, ClassID = ClassId, BaseStats = new AttributeSet() });

        var pet = new Pet
        {
            LoginID = 1001,
            Name = "Pet",
            Class = klass,
            BaseStats = new AttributeSet { HP = 100 },
            MaxStats = new AttributeSet { HP = 100 },
            HairA = 255,
            FaceID = 70,
            State = Player.States.Ready,
        };
        pet.CurrentHP = 100;
        return pet;
    }

    private void PlacePet(Pet pet, int x, int y)
    {
        pet.Map = map;
        pet.MapID = MapId;
        pet.MapX = x;
        pet.MapY = y;
        map.AddPlayer(pet, world);
        map.PlaceCharacter(pet);
        map.SetCharacter(pet, x, y);
    }

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

    [Fact]
    public void Pet_Destroy_WhileInvisible_SendsInvisFlipCHPToBystandersBeforeErase()
    {
        var bystander = NewPlayer();
        PlacePlayer(bystander, 5, 6);

        var pet = NewPet();
        PlacePet(pet, 5, 5);
        pet.AddBuff(NewBuff(pet, SpellEffect.EffectTypes.Invisible), world);
        Assert.True(pet.IsInvisible);

        bystander.SendBuffer.Clear();
        pet.Destroy(world);

        string buf = Buffer(bystander);
        int ercIndex = buf.IndexOf("ERC", StringComparison.Ordinal);
        Assert.True(ercIndex >= 0, "bystander never received the erase packet");

        string before = buf.Substring(0, ercIndex);
        string after = buf.Substring(ercIndex);

        Assert.Contains("CHP", before);
        Assert.Contains(",255,0,70,", before);
        Assert.DoesNotContain("CHP", after);
    }
}
