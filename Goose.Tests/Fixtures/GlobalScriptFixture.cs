using System.Reflection;
using Goose.Scripting;

namespace Goose.Tests.Fixtures;

public sealed class GlobalScriptFixture : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;

    /// <summary>Every dimension script, by the relative path the server resolves it at.
    /// Copied to output by Goose.Tests.csproj. Add to BOTH lists together - a script
    /// missing here fails inside OnLoaded, not at compile time.</summary>
    ///
    /// <remarks>All eight dimension scripts ship: the global orchestration, the map entry
    /// gate, the quest reward that grants the unlocked dimension, the spell that
    /// teleports the player between dimensions, the item scripts that roll abyss
    /// suffixes and rarity titles onto dimension equipment, and the rebirth script that
    /// trades banked experience for spirit. The seven entry scripts all live in one
    /// folder, Scripts/Global/Dimensions/; the entry orchestration stays in
    /// Scripts/Global/.</remarks>
    private static readonly (string Source, string Relative)[] ShippedScripts =
    {
        ("Dimensions.csx",           "Scripts/Global/Dimensions.csx"),
        ("DimensionConstants.csx",   "Scripts/Global/Dimensions/DimensionConstants.csx"),
        ("DimensionMap.csx",         "Scripts/Global/Dimensions/DimensionMap.csx"),
        ("DimensionUnlock.csx",      "Scripts/Global/Dimensions/DimensionUnlock.csx"),
        ("DimensionTeleport.csx",    "Scripts/Global/Dimensions/DimensionTeleport.csx"),
        ("DimensionItem.csx",        "Scripts/Global/Dimensions/DimensionItem.csx"),
        ("DimensionSurname.csx",     "Scripts/Global/Dimensions/DimensionSurname.csx"),
        ("DimensionRarity.csx",      "Scripts/Global/Dimensions/DimensionRarity.csx"),
        ("Rebirth.csx",              "Scripts/Global/Dimensions/Rebirth.csx"),
    };

    public string DataDirectory { get; }
    public GameWorld World { get; }

    public GlobalScriptFixture()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "global-script-" + Guid.NewGuid().ToString("N"));
        foreach (var dir in new[] { "Global", "Global/Dimensions" })
            Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", dir));

        GameWorld.Settings = new GooseSettings
        {
            DataPath = DataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
            VendorSlotSize = 30,
            // NPC spawns need a login-id range: GetNewID draws from (MaxPlayers, MaxNPCs]
            // (NPCHandler.cs:244). Same values NPCSpawnRegistrationTests uses.
            MaxPlayers = 200, MaxNPCs = 15000,
        };
        World = new GameWorld(null);

        // Seed classes so NPC spawning works (see ORCHESTRATION NOTE 2).
        SeedClass(0, "Default", 50);
        // Rebirth changes the player to class 1 level 1 (Rebirth.csx), so the destination
        // class has to exist in the fixture too. Real class_info carries 1-5 for class 1
        // and 1-50 for 2-7 — the same asymmetry Dimensions.csx's warden comment calls out.
        SeedClass(1, "Commoner", 5);
        SeedClass(3, "Warrior", 50);
    }

    /// <summary>Installs every shipped dimension script into the temp data dir. Call this
    /// before compiling anything - Dimensions.csx loads the map and quest scripts while it
    /// runs, so a partial install fails at OnLoaded rather than at compile time.</summary>
    public void InstallShippedScripts()
    {
        foreach (var (source, relative) in ShippedScripts)
        {
            var from = Path.Combine(AppContext.BaseDirectory, "DimensionScripts", source);
            if (!File.Exists(from))
                throw new FileNotFoundException(
                    $"{source} is not in the test output. Add its <None Include> to Goose.Tests.csproj.", from);

            File.Copy(from, Path.Combine(DataDirectory, relative), overwrite: true);
        }
    }

    /// <summary>Compiles the real shipped Dimensions.csx, so tests exercise what ships
    /// rather than a paraphrase of it.</summary>
    public Script<IGlobalScript> CompileShipped(string fileName = "Dimensions.csx")
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/" + fileName);
    }

    /// <summary>As CompileShipped, for the map script - Task 5's tests drive it directly.</summary>
    public Script<IMapScript> CompileShippedMapScript(string fileName = "DimensionMap.csx")
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IMapScript>("Scripts/Global/Dimensions/" + fileName);
    }

    /// <summary>Compiles an arbitrary script body, for the one test that needs a variant of
    /// the shipped script (the disabled-mode test).</summary>
    public Script<IGlobalScript> CompileSource(string body, string fileName)
    {
        InstallShippedScripts();
        var relativePath = "Scripts/Global/" + fileName;
        File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
        return World.ScriptHandler.GetScript<IGlobalScript>(relativePath);
    }

    /// <summary>Compiles an arbitrary spell-effect script body from the temp data dir.</summary>
    public Script<ISpellEffectScript> CompileSpellEffectScript(string body, string fileName)
    {
        Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Spell"));
        var relativePath = "Scripts/Spell/" + fileName;
        File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
        return World.ScriptHandler.GetScript<ISpellEffectScript>(relativePath);
    }

    /// <summary>A base map with hand-built tile arrays. Real maps get theirs from
    /// Map.LoadData reading a .map file; clones never call it, so a synthetic base is
    /// enough to exercise the clone path.</summary>
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

    /// <summary>Registers a base spell effect. Real ones come from the spell_effects table
    /// (SpellHandler.cs:29); the clone path only reads the object, so a synthetic one is enough.</summary>
    public SpellEffect AddBaseSpellEffect(int id, string name, Action<SpellEffect> configure = null)
    {
        var effect = new SpellEffect { ID = id, Name = name, MaximumLevelEffected = 99 };
        configure?.Invoke(effect);
        World.SpellHandler.AddSpellEffect(effect);
        return effect;
    }

    /// <summary>A player standing on a map, for tests that drive Player methods end to end.
    /// Player(int) never touches BaseStats/MaxStats (only LoadFromAutoCreate does), and
    /// P.StatusInfo (Packets.cs:370) reads Class.ClassName and MaxStats, so a synthetic
    /// player needs all three set like a real logged-in player would have.</summary>
    public Player PlayerOn(Map map, int x, int y)
    {
        return new Player(0)
        {
            Map = map, MapID = map.ID, MapX = x, MapY = y,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = World.ClassHandler.GetClass(0),
        };
    }

    /// <summary>Player.Send is virtual and returns early on a null socket (Player.cs:2409),
    /// so overriding it is how tests read the server's messages back.</summary>
    public sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new List<string>();
        public override void Send(string data) { this.Sent.Add(data); }
    }

    /// <summary>A logged-in-looking player: Ready state (every script command early-returns
    /// otherwise), an Inventory, and the BaseStats/MaxStats/Class trio PlayerOn already
    /// documents.</summary>
    public CapturingPlayer CommandPlayerOn(Map map, int x, int y, string name = "Tester")
    {
        var player = new CapturingPlayer
        {
            Name = name,
            Map = map, MapID = map.ID, MapX = x, MapY = y,
            State = Player.States.Ready,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = World.ClassHandler.GetClass(0),
        };
        player.Inventory = new Inventory(player);
        return player;
    }

    /// <summary>Makes PlayerHandler.GetPlayer(name) find this player, which is how /givesp
    /// resolves its target (PlayerHandler.cs:129). Not AddPlayer: that indexes by
    /// player.Sock (PlayerHandler.cs:51) and a socketless test player would throw on the
    /// null key. Same reflection approach as SeedClass.</summary>
    public void RegisterOnlinePlayer(Player player)
    {
        var byName = (Dictionary<string, Player>)typeof(PlayerHandler)
            .GetField("nameToPlayer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(World.PlayerHandler)!;
        byName[player.Name.ToLower()] = player;
    }

    /// <summary>Runs a chat-line command the way the server does: EventHandler.AddEvent
    /// parses it against the registered trie and queues an Event, and Update dequeues and
    /// calls Ready (EventHandler.cs:286,:361-371). Going through both is the point - it is
    /// what proves the trailing-space registration actually matches.</summary>
    public bool RunCommand(Player player, string packet)
    {
        if (!World.EventHandler.AddEvent(player, packet)) return false;

        World.EventHandler.Update(World);
        return true;
    }

    /// <summary>Registers a base spell pointing at an already-registered effect.</summary>
    public Spell AddBaseSpell(int id, string name, int effectId, Action<Spell> configure = null)
    {
        var spell = new Spell
        {
            ID = id, Name = name, Description = "",
            SpellEffectID = effectId,
            SpellEffect = World.SpellHandler.GetSpellEffect(effectId),
        };
        configure?.Invoke(spell);
        World.SpellHandler.AddSpell(spell);
        return spell;
    }

    /// <summary>Registers a base item template. Real ones come from item_templates
    /// (ItemHandler.cs:56); the clone path only reads the object.</summary>
    public ItemTemplate AddBaseItemTemplate(int id, string name, ItemTemplate.UseTypes useType,
                                            Action<ItemTemplate> configure = null)
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

    /// <summary>Registers a class with levels 1..maxLevel directly into ClassHandler's private
    /// dictionary. ClassHandler has no public registration path (classes come from the DB), so
    /// this mirrors NPCSpawnRegistrationTests.RegisterClass.</summary>
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

    /// <summary>Removes one level row from a seeded class. Needed by the warden-class validation
    /// test (Task 7): the warden uses class 3 at level 50, and that test must be able to take
    /// the level-50 row away to prove the script rejects the misconfiguration up front.</summary>
    public void RemoveClassLevel(int classId, int level)
    {
        var cls = World.ClassHandler.GetClass(classId);
        if (cls == null) return;

        var levels = (Dictionary<int, ClassLevel>)typeof(Class)
            .GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cls)!;
        levels.Remove(level);
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        if (Directory.Exists(DataDirectory)) Directory.Delete(DataDirectory, recursive: true);
    }
}
