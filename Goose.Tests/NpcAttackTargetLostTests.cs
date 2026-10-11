using System.Net.Sockets;
using System.Reflection;

namespace Goose.Tests;

public class NpcAttackTargetLostTests : IDisposable
{
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly Map boundMap;
    private readonly List<Socket> sockets = new();

    private const int ClassId = 1;

    public NpcAttackTargetLostTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "npc-target-lost-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dataDirectory, "Scripts", "Quest"));
        world = new GameWorld(new GooseSettings
        {
            DataPath = dataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            MaxAC = 3500, MaxPlayers = 200, MaxNPCs = 15000, DamageModifier = 1,
        });

        map = AddMap(1);
        boundMap = AddMap(2);

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

    [Fact]
    public void Killing_blow_does_not_cast_on_attack_procs_at_the_warped_player()
    {
        var player = PlayerAt(5, 6, hp: 10);
        var npc = NpcAt(5, 5);
        var chomp = new SpellEffect { EffectType = SpellEffect.EffectTypes.Formula, Effected = SpellEffect.SpellEffected.NPCPlayer, HPFormula = "-1" };
        npc.AddBuff(new Buff
        {
            Target = npc,
            Caster = npc,
            SpellEffect = new SpellEffect
            {
                EffectType = SpellEffect.EffectTypes.OnAttack,
                Duration = 1000,
                OnMeleeAttackSpell = chomp,
                OnMeleeAttackSpellChance = 100,
            },
        }, world);

        npc.Attack(player, world);

        Assert.NotSame(map, player.Map);
    }

    [Fact]
    public void Handle_attack_event_without_an_aggro_target_does_nothing()
    {
        var npc = NpcAt(5, 5);

        npc.HandleAttackEvent(world);

        Assert.Null(npc.AggroTarget);
    }
}
