using System.Reflection;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class InvisibilityCounterTests : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;
    private readonly string dataDirectory;
    private readonly GameWorld world;
    private readonly Map map;

    private const int MapId = 1;
    private const int ClassId = 1;

    public InvisibilityCounterTests()
    {
        dataDirectory = Path.Combine(Path.GetTempPath(), "invis-counter-" + Guid.NewGuid().ToString("N"));
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
        if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
    }

    private static Player NewPlayer(Map map)
    {
        var p = new Player(0);
        p.Inventory = new Inventory(p);
        var klass = new Class { ClassID = 1, ClassName = "Test", ACMultiplier = 1m };
        klass.AddLevel(new ClassLevel { Level = 1, ClassID = 1, BaseStats = new AttributeSet() });
        p.Class = klass;
        p.BaseStats = new AttributeSet { HP = 100, MP = 100 };
        p.MaxStats = p.BaseStats + new AttributeSet();
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.HairA = 255;
        p.FaceID = 70;
        p.State = Player.States.Ready;
        p.Map = map;
        return p;
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

    [Fact]
    public void Player_AddingAndRemovingInvisibleBuff_FlipsIsInvisibleAndPacketField()
    {
        var p = NewPlayer(map);
        var buff = NewBuff(p, SpellEffect.EffectTypes.Invisible);

        p.AddBuff(buff, world);
        Assert.True(p.IsInvisible);
        Assert.Contains(",1,70,", P.UpdateCharacter(p));

        p.RemoveBuff(buff, world);
        Assert.False(p.IsInvisible);
        Assert.Contains(",0,70,", P.UpdateCharacter(p));
    }

    [Fact]
    public void Npc_AddingAndRemovingInvisibleBuff_FlipsIsInvisibleAndPacketField()
    {
        var npc = SpawnNpc(Template());
        var buff = NewBuff(npc, SpellEffect.EffectTypes.Invisible);

        npc.AddBuff(buff, world);
        Assert.True(npc.IsInvisible);
        Assert.Contains(",1,70,", P.UpdateNPC(npc));

        npc.RemoveBuff(buff, world);
        Assert.False(npc.IsInvisible);
        Assert.Contains(",0,70,", P.UpdateNPC(npc));
    }

    [Fact]
    public void Player_TwoInvisibleBuffs_StayInvisibleUntilBothRemoved()
    {
        var p = NewPlayer(map);
        var a = NewBuff(p, SpellEffect.EffectTypes.Invisible);
        var b = NewBuff(p, SpellEffect.EffectTypes.Invisible);

        p.AddBuff(a, world);
        p.AddBuff(b, world);
        Assert.True(p.IsInvisible);

        p.RemoveBuff(a, world);
        Assert.True(p.IsInvisible);

        p.RemoveBuff(b, world);
        Assert.False(p.IsInvisible);
    }

    [Fact]
    public void Player_NonInvisibleBuff_DoesNotTouchInvisCounters()
    {
        var p = NewPlayer(map);
        p.AddBuff(NewBuff(p, SpellEffect.EffectTypes.Buff), world);

        Assert.False(p.IsInvisible);
        Assert.Equal(0, p.InvisibleBuffCount);
        Assert.Equal(0, p.SeeInvisibleBuffCount);
        Assert.Contains(",0,70,", P.UpdateCharacter(p));
    }

    [Fact]
    public void Player_RenewInvisibleBuffToNonInvisibleType_ClearsInvisibility()
    {
        var p = NewPlayer(map);
        var seA = new SpellEffect { EffectType = SpellEffect.EffectTypes.Invisible, Duration = 1000 };
        p.AddBuff(new Buff { Target = p, Caster = p, SpellEffect = seA }, world);
        Assert.True(p.IsInvisible);

        var seB = new SpellEffect { EffectType = SpellEffect.EffectTypes.Buff, Duration = 1000 };
        seB.BuffStacksOver.Add(seA);
        p.AddBuff(new Buff { Target = p, Caster = p, SpellEffect = seB }, world);

        Assert.False(p.IsInvisible);
        Assert.Equal(0, p.InvisibleBuffCount);
    }

    [Fact]
    public void Player_DoubleRemoveDoesNotDriveCounterNegative()
    {
        var p = NewPlayer(map);
        var a = NewBuff(p, SpellEffect.EffectTypes.Invisible);

        p.AddBuff(a, world);
        p.RemoveBuff(a, world);
        p.RemoveBuff(a, world);

        var b = NewBuff(p, SpellEffect.EffectTypes.Invisible);
        p.AddBuff(b, world);
        Assert.True(p.IsInvisible);

        p.RemoveBuff(b, world);
        Assert.False(p.IsInvisible);
    }

    [Fact]
    public void Npc_TemplateSeeInvisible_IsVisibleWithNoBuffs()
    {
        var npc = SpawnNpc(Template(seeInvisible: true));
        Assert.True(npc.CanSeeInvisible);
    }

    [Fact]
    public void Npc_SeeInvisibleBuff_TogglesCanSeeInvisible()
    {
        var npc = SpawnNpc(Template(seeInvisible: false));
        Assert.False(npc.CanSeeInvisible);

        var buff = NewBuff(npc, SpellEffect.EffectTypes.SeeInvisible);
        npc.AddBuff(buff, world);
        Assert.True(npc.CanSeeInvisible);

        npc.RemoveBuff(buff, world);
        Assert.False(npc.CanSeeInvisible);
    }

    [Fact]
    public void Npc_RenewInvisibleBuffToNonInvisibleType_ClearsInvisibility()
    {
        var npc = SpawnNpc(Template());
        var seA = new SpellEffect { EffectType = SpellEffect.EffectTypes.Invisible, Duration = 1000 };
        npc.AddBuff(new Buff { Target = npc, Caster = npc, SpellEffect = seA }, world);
        Assert.True(npc.IsInvisible);

        var seB = new SpellEffect { EffectType = SpellEffect.EffectTypes.Buff, Duration = 1000 };
        seB.BuffStacksOver.Add(seA);
        npc.AddBuff(new Buff { Target = npc, Caster = npc, SpellEffect = seB }, world);

        Assert.False(npc.IsInvisible);
        Assert.Equal(0, npc.InvisibleBuffCount);
    }

    [Fact]
    public void Npc_RenewSeeInvisibleBuffToNonSeeInvisibleType_ClearsCanSeeInvisible()
    {
        var npc = SpawnNpc(Template(seeInvisible: false));
        var seA = new SpellEffect { EffectType = SpellEffect.EffectTypes.SeeInvisible, Duration = 1000 };
        npc.AddBuff(new Buff { Target = npc, Caster = npc, SpellEffect = seA }, world);
        Assert.True(npc.CanSeeInvisible);

        var seB = new SpellEffect { EffectType = SpellEffect.EffectTypes.Buff, Duration = 1000 };
        seB.BuffStacksOver.Add(seA);
        npc.AddBuff(new Buff { Target = npc, Caster = npc, SpellEffect = seB }, world);

        Assert.False(npc.CanSeeInvisible);
        Assert.Equal(0, npc.SeeInvisibleBuffCount);
    }

    [Fact]
    public void Npc_DoubleRemoveDoesNotDriveCounterNegative()
    {
        var npc = SpawnNpc(Template());
        var a = NewBuff(npc, SpellEffect.EffectTypes.Invisible);

        npc.AddBuff(a, world);
        npc.RemoveBuff(a, world);
        npc.RemoveBuff(a, world);

        var b = NewBuff(npc, SpellEffect.EffectTypes.Invisible);
        npc.AddBuff(b, world);
        Assert.True(npc.IsInvisible);

        npc.RemoveBuff(b, world);
        Assert.False(npc.IsInvisible);
    }

    [Fact]
    public void NpcTemplate_SeeInvisible_DefaultsFalse_AndCopiesInCopyCtor_AndLoadsOnSpawn()
    {
        Assert.False(new NPCTemplate().SeeInvisible);

        var template = Template(seeInvisible: true);
        Assert.True(new NPCTemplate(template).SeeInvisible);

        var npc = SpawnNpc(template);
        Assert.True(npc.CanSeeInvisible);
    }

    [Fact]
    public void Pet_AddingInvisibleBuff_FlipsIsInvisibleAndPacketField()
    {
        var klass = new Class { ClassID = 1, ClassName = "Test", ACMultiplier = 1m };
        klass.AddLevel(new ClassLevel { Level = 1, ClassID = 1, BaseStats = new AttributeSet() });

        var pet = new Pet
        {
            LoginID = 9,
            Name = "Pet",
            Class = klass,
            BaseStats = new AttributeSet { HP = 100 },
            MaxStats = new AttributeSet { HP = 100 },
            HairA = 255,
            FaceID = 70,
            State = Player.States.Ready,
            Map = map,
        };
        pet.CurrentHP = 100;

        var buff = NewBuff(pet, SpellEffect.EffectTypes.Invisible);
        pet.AddBuff(buff, world);
        Assert.True(pet.IsInvisible);
        Assert.Contains(",1,70,", P.UpdatePet(pet));
    }
}
