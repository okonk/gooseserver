using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Goose.Events;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class InvisibilityBreakTests : IDisposable
{
    private readonly GooseSettings settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;
    private readonly List<Socket> sockets = new();

    private const int MapId = 1;
    private const int ClassId = 1;

    public InvisibilityBreakTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "invis-break-" + Guid.NewGuid().ToString("N"));
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

    private NPC SpawnNpc() => world.NPCHandler.SpawnNPC(world, MapId, 5, 5, new NPCTemplate
    {
        NPCTemplateID = 1,
        Name = "Test NPC",
        Level = 50,
        ClassID = ClassId,
        BaseStats = new AttributeSet(),
        AggroRange = 5,
        HairA = 255,
        FaceID = 70,
    }, shouldRespawn: false);

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

    private static void Swing(Player p, GameWorld world)
    {
        new PlayerAttackEvent { Player = p }.Ready(world);
    }

    [Fact]
    public void InvisiblePlayer_AttackingNpc_BreaksInvisibilityAndKeepsOtherBuffs()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        b.Level = 1;
        PlacePlayer(a, 4, 4);
        PlacePlayer(b, 4, 5);
        var npc = SpawnNpc();

        var root = NewBuff(b, SpellEffect.EffectTypes.Root);
        var invis = NewBuff(b, SpellEffect.EffectTypes.Invisible);
        b.AddBuff(invis, world);
        b.AddBuff(root, world);
        Assert.True(b.IsInvisible);

        a.SendBuffer.Clear();
        Swing(b, world);

        Assert.False(b.IsInvisible);
        Assert.DoesNotContain(b.Buffs, buf => buf.SpellEffect.EffectType == SpellEffect.EffectTypes.Invisible);
        Assert.Contains(root, b.Buffs);

        string buf = Buffer(a);
        Assert.Contains(",255,0,70,", buf);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void VisiblePlayer_AttackingNpc_SendsNoCharacterUpdate()
    {
        var b = NewPlayer();
        b.Level = 1;
        PlacePlayer(b, 4, 5);
        var npc = SpawnNpc();

        b.SendBuffer.Clear();
        Swing(b, world);

        Assert.False(b.IsInvisible);
        string buf = Buffer(b);
        Assert.DoesNotContain("CHP", buf);
    }

    [Fact]
    public void InvisiblePlayer_SwingsAtEmptyTile_BreaksInvisibility()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        PlacePlayer(a, 3, 5);
        PlacePlayer(b, 4, 5);

        b.AddBuff(NewBuff(b, SpellEffect.EffectTypes.Invisible), world);
        Assert.True(b.IsInvisible);

        a.SendBuffer.Clear();
        Swing(b, world);

        Assert.False(b.IsInvisible);
        Assert.DoesNotContain(b.Buffs, buf => buf.SpellEffect.EffectType == SpellEffect.EffectTypes.Invisible);

        string buf = Buffer(a);
        Assert.Contains(",255,0,70,", buf);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void InvisibleNpc_AttackingPlayer_BreaksInvisibility()
    {
        var p = NewPlayer();
        p.Level = 200;
        PlacePlayer(p, 5, 6);
        var npc = SpawnNpc();
        p.SendBuffer.Clear();

        var buff = NewBuff(npc, SpellEffect.EffectTypes.Invisible);
        npc.AddBuff(buff, world);
        Assert.True(npc.IsInvisible);

        p.SendBuffer.Clear();
        npc.Attack(p, world);

        Assert.False(npc.IsInvisible);
        Assert.Empty(npc.Buffs);

        string buf = Buffer(p);
        Assert.Contains("CHP", buf);
        Assert.Contains(",255,0,70,", buf);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void InvisiblePlayer_SuccessfulCast_BreaksInvisibility()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        PlacePlayer(a, 5, 5);
        PlacePlayer(b, 5, 6);

        var root = NewBuff(b, SpellEffect.EffectTypes.Root);
        var invis = NewBuff(b, SpellEffect.EffectTypes.Invisible);
        b.AddBuff(invis, world);
        b.AddBuff(root, world);
        Assert.True(b.IsInvisible);

        var se = new SpellEffect
        {
            EffectType = SpellEffect.EffectTypes.Buff,
            TargetType = SpellEffect.TargetTypes.Target,
            Effected = SpellEffect.SpellEffected.Self,
            Duration = 1000,
        };

        a.SendBuffer.Clear();
        bool result = se.Cast(b, b, world);

        Assert.True(result);
        Assert.False(b.IsInvisible);
        Assert.DoesNotContain(b.Buffs, buf => buf.SpellEffect.EffectType == SpellEffect.EffectTypes.Invisible);
        Assert.Contains(root, b.Buffs);

        string buf = Buffer(a);
        Assert.Equal(1, buf.Split(",255,0,70,").Length - 1);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void CastOnPvpMap_SpellWorksNotInPvp_StaysInvisible()
    {
        map.CanPVP = true;
        var p = NewPlayer();
        PlacePlayer(p, 5, 5);

        p.AddBuff(NewBuff(p, SpellEffect.EffectTypes.Invisible), world);
        Assert.True(p.IsInvisible);

        var se = new SpellEffect
        {
            EffectType = SpellEffect.EffectTypes.Buff,
            TargetType = SpellEffect.TargetTypes.Target,
            Effected = SpellEffect.SpellEffected.Self,
            Duration = 1000,
            WorksInPVP = false,
        };

        bool result = se.Cast(p, p, world);

        Assert.False(result);
        Assert.True(p.IsInvisible);
        Assert.Single(p.Buffs);
    }

    [Fact]
    public void TwoInvisibleStacks_OneAttack_RemovesBoth()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        b.Level = 1;
        PlacePlayer(a, 3, 5);
        PlacePlayer(b, 4, 5);
        var npc = SpawnNpc();

        b.AddBuff(NewBuff(b, SpellEffect.EffectTypes.Invisible), world);
        b.AddBuff(NewBuff(b, SpellEffect.EffectTypes.Invisible), world);
        Assert.Equal(2, b.InvisibleBuffCount);

        a.SendBuffer.Clear();
        Swing(b, world);

        Assert.False(b.IsInvisible);
        Assert.Equal(0, b.InvisibleBuffCount);
        Assert.DoesNotContain(b.Buffs, buf => buf.SpellEffect.EffectType == SpellEffect.EffectTypes.Invisible);

        string buf = Buffer(a);
        Assert.Equal(1, buf.Split(",255,0,70,").Length - 1);
        Assert.DoesNotContain(",255,1,70,", buf);
    }

    [Fact]
    public void InvisiblePlayer_SelfCastInvisibleSpell_EndsInvisibleWithFlipFlop()
    {
        var a = NewPlayer();
        var b = NewPlayer();
        PlacePlayer(a, 5, 5);
        PlacePlayer(b, 5, 6);

        var oldInvis = NewBuff(b, SpellEffect.EffectTypes.Invisible);
        b.AddBuff(oldInvis, world);
        Assert.True(b.IsInvisible);

        var se = new SpellEffect
        {
            EffectType = SpellEffect.EffectTypes.Invisible,
            TargetType = SpellEffect.TargetTypes.Target,
            Effected = SpellEffect.SpellEffected.Self,
            Duration = 1000,
        };

        a.SendBuffer.Clear();
        bool result = se.Cast(b, b, world);

        Assert.True(result);
        Assert.True(b.IsInvisible);
        Assert.Single(b.Buffs);
        Assert.Same(se, b.Buffs[0].SpellEffect);

        string buf = Buffer(a);
        int zero = buf.IndexOf(",255,0,70,");
        int one = buf.IndexOf(",255,1,70,");
        Assert.True(zero >= 0, "expected a 1->0 CHP flip");
        Assert.True(one > zero, "expected the 0->1 flip after the 1->0 flip");
        Assert.Equal(one, buf.LastIndexOf(",255,1,70,"));
        Assert.Equal(zero, buf.LastIndexOf(",255,0,70,"));
    }

    [Fact]
    public void InvisiblePlayer_FailedCast_StillReveals()
    {
        var p = NewPlayer();
        PlacePlayer(p, 5, 6);
        var npc = SpawnNpc();

        p.AddBuff(NewBuff(p, SpellEffect.EffectTypes.Invisible), world);
        Assert.True(p.IsInvisible);

        var se = new SpellEffect
        {
            EffectType = SpellEffect.EffectTypes.Stun,
            TargetType = SpellEffect.TargetTypes.Target,
            Effected = SpellEffect.SpellEffected.NPC,
            Duration = 1000,
            MaximumLevelEffected = 999,
        };

        bool result = se.Cast(p, npc, world);

        Assert.False(result);
        Assert.False(p.IsInvisible);
        Assert.Empty(p.Buffs);
    }
}
