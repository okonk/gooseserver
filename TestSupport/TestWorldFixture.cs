using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Goose.Scripting;

namespace Goose.Testing;

public class TestWorldFixture : IDisposable
{
    public string DataDirectory { get; }
    public GooseSettings Settings { get; }
    public GameWorld World { get; }

    public TestWorldFixture(Action<GooseSettings>? configure = null)
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "test-world-" + Guid.NewGuid().ToString("N"));
        foreach (var dir in new[] { "Global", "Global/Dimensions" })
            Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", dir));

        Settings = new GooseSettings
        {
            DataPath = DataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            VendorSlotSize = 30,
            // NPC spawns need a login-id range: GetNewID draws from (MaxPlayers, MaxNPCs]
            // (NPCHandler.cs:244). Same values NPCSpawnRegistrationTests uses.
            MaxPlayers = 200, MaxNPCs = 15000,
        };
        configure?.Invoke(Settings);
        World = new GameWorld(Settings);

        SeedClass(0, "Default", 50);
        SeedClass(1, "Commoner", 5);
        SeedClass(3, "Warrior", 50);
    }

    public Script<ISpellEffectScript> CompileSpellEffectScript(string body, string fileName)
    {
        Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Spell"));
        var relativePath = "Scripts/Spell/" + fileName;
        File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
        return World.ScriptHandler.GetScript<ISpellEffectScript>(relativePath);
    }

    public Map AddBaseMap(int id, string name, int width = 10, int height = 10)
    {
        var map = new Map
        {
            ID = id, Name = name, FileName = "Map" + id + ".map",
            Width = width, Height = height,
            tiles = new ITile[(width + 1) * (height + 1)],
            characters = new ICharacter[(width + 1) * (height + 1)],
        };
        World.MapHandler.Maps[id] = map;
        return map;
    }

    public SpellEffect AddBaseSpellEffect(int id, string name, Action<SpellEffect>? configure = null)
    {
        var effect = new SpellEffect { ID = id, Name = name, MaximumLevelEffected = 99 };
        configure?.Invoke(effect);
        World.SpellHandler.AddSpellEffect(effect);
        return effect;
    }

    public Player PlayerOn(Map map, int x, int y)
    {
        return new Player(0)
        {
            Map = map, MapID = map.ID, MapX = x, MapY = y,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = World.ClassHandler.GetClass(0)!,
        };
    }

    public sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new List<string>();
        public override bool Send(string data) { this.Sent.Add(data); return true; }
    }

    public CapturingPlayer CommandPlayerOn(Map map, int x, int y, string name = "Tester")
    {
        var player = new CapturingPlayer
        {
            Name = name,
            Map = map, MapID = map.ID, MapX = x, MapY = y,
            State = Player.States.Ready,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = World.ClassHandler.GetClass(0)!,
        };
        player.Inventory = new Inventory(player, this.Settings);
        return player;
    }

    public void RegisterOnlinePlayer(Player player)
    {
        var byName = (Dictionary<string, Player>)typeof(PlayerHandler)
            .GetField("nameToPlayer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(World.PlayerHandler)!;
        byName[player.Name.ToLower()] = player;
    }

    public void RegisterDatabasePlayer(Player player)
    {
        var byName = (Dictionary<string, Player>)typeof(PlayerHandler)
            .GetField("allNameToPlayer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(World.PlayerHandler)!;
        byName[player.Name.ToLower()] = player;
    }

    public void AddOnlinePlayer(Player player)
    {
        // AddPlayer keys sockToPlayer by Sock, so a dummy socket is required.
        player.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        World.PlayerHandler.AddPlayer(player, World);
    }

    public bool RunCommand(Player player, string packet)
    {
        if (!World.EventHandler.AddEvent(player, packet)) return false;

        World.EventHandler.Update(World);
        return true;
    }

    public Spell AddBaseSpell(int id, string name, int effectId, Action<Spell>? configure = null)
    {
        var spell = new Spell
        {
            ID = id, Name = name, Description = "",
            SpellEffectID = effectId,
            SpellEffect = World.SpellHandler.GetSpellEffect(effectId)!,
        };
        configure?.Invoke(spell);
        World.SpellHandler.AddSpell(spell);
        return spell;
    }

    public ItemTemplate AddBaseItemTemplate(int id, string name, ItemTemplate.UseTypes useType,
                                            Action<ItemTemplate>? configure = null)
    {
        var template = new ItemTemplate
        {
            ID = id, Name = name, Description = "A " + name, UseType = useType,
            Slot = ItemTemplate.ItemSlots.OneHanded, BaseStats = new AttributeSet(),
            GraphicR = 255, GraphicG = 255, GraphicB = 255, GraphicA = 100,
            StackSize = 1, ScriptParams = "",
        };
        configure?.Invoke(template);
        World.ItemHandler.AddTemplate(template);
        return template;
    }

    public void SeedClass(int classId, string name, int maxLevel)
    {
        var cls = new Class { ClassID = classId, ClassName = name, ACMultiplier = 1m };
        for (int level = 1; level <= maxLevel; level++)
            // Spells must be a real list: Player.ChangeClass iterates GetLevel(n).Spells.
            cls.AddLevel(new ClassLevel { Level = level, BaseStats = new AttributeSet(), Spells = new List<Spell>() });

        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(World.ClassHandler)!;
        classes[classId] = cls;
    }

    public void SeedClassLevels(int classId, string name, int[] levels)
    {
        var cls = new Class { ClassID = classId, ClassName = name, ACMultiplier = 1m };
        foreach (int level in levels)
            cls.AddLevel(new ClassLevel { Level = level, BaseStats = new AttributeSet(), Spells = new List<Spell>() });

        var classes = (Dictionary<int, Class>)typeof(ClassHandler)
            .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(World.ClassHandler)!;
        classes[classId] = cls;
    }

    public void Dispose()
    {
        if (Directory.Exists(DataDirectory)) Directory.Delete(DataDirectory, recursive: true);
    }
}
