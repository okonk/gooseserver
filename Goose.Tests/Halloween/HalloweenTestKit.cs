using System.Text.Json;
using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Halloween;

public sealed class HalloweenTestKit : IDisposable
{
    public TestWorldFixture Fixture { get; }
    public GameWorld World => Fixture.World;
    public object Host { get; }
    public string ConfigPath { get; }

    public int CandyId { get; }
    public int PartyCostumeId { get; }
    public int[] MaskIds { get; }
    public int RitualCandleId { get; }
    public int StalkerId { get; }
    public int GroundskeeperId { get; }
    public int WailingGhostId { get; }
    public int FlickeringGhostId { get; }
    public int PinkPhantomId { get; }
    public int BloodyGhostId { get; }
    public int RestlessBonesId { get; }

    public HalloweenTestKit()
    {
        Fixture = new TestWorldFixture();
        var halloweenDir = Path.Combine(Fixture.DataDirectory, "Scripts", "Global", "Halloween");
        Directory.CreateDirectory(halloweenDir);
        ConfigPath = Path.Combine(halloweenDir, "halloween.json");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(ShippedDataDirectory, "Scripts", "Global", "Halloween"), "*.csx"))
            File.Copy(file, Path.Combine(halloweenDir, Path.GetFileName(file)), overwrite: true);

        CandyId = 90001;
        PartyCostumeId = 90002;
        MaskIds = [90003, 90004, 90005, 90006, 90007];
        RitualCandleId = 90008;
        StalkerId = 91001;
        GroundskeeperId = 91002;
        WailingGhostId = 91003;
        FlickeringGhostId = 91004;
        PinkPhantomId = 91005;
        BloodyGhostId = 91006;
        RestlessBonesId = 91007;

        Fixture.AddBaseItemTemplate(CandyId, "Candy", ItemTemplate.UseTypes.OneTime,
            t => t.Slot = ItemTemplate.ItemSlots.Misc);
        Fixture.AddBaseItemTemplate(PartyCostumeId, "Party Costume", ItemTemplate.UseTypes.Armor,
            t => t.Slot = ItemTemplate.ItemSlots.Helmet);
        foreach (var maskId in MaskIds)
            Fixture.AddBaseItemTemplate(maskId, "Mask " + maskId, ItemTemplate.UseTypes.Armor,
                t => t.Slot = ItemTemplate.ItemSlots.Helmet);
        Fixture.AddBaseItemTemplate(RitualCandleId, "Ritual Candle", ItemTemplate.UseTypes.NoUse,
            t => t.Slot = ItemTemplate.ItemSlots.Misc);

        Fixture.AddNpcTemplate(StalkerId, "Stalker");
        Fixture.AddNpcTemplate(GroundskeeperId, "Groundskeeper");
        Fixture.AddNpcTemplate(WailingGhostId, "Wailing Ghost");
        Fixture.AddNpcTemplate(FlickeringGhostId, "Flickering Ghost");
        Fixture.AddNpcTemplate(PinkPhantomId, "Pink Phantom");
        Fixture.AddNpcTemplate(BloodyGhostId, "Bloody Ghost");
        Fixture.AddNpcTemplate(RestlessBonesId, "Restless Bones");

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(DefaultConfig()));
        File.WriteAllText(Path.Combine(Fixture.DataDirectory, "Scripts", "Global", "HalloweenHost.csx"), HostScript);
        Host = Fixture.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/HalloweenHost.csx").Object;
    }

    public void SetConfig(Action<JsonObject> mutate)
    {
        var json = JsonNode.Parse(File.ReadAllText(ConfigPath))!.AsObject();
        mutate(json);
        File.WriteAllText(ConfigPath, json.ToJsonString());
        File.SetLastWriteTimeUtc(ConfigPath, File.GetLastWriteTimeUtc(ConfigPath).AddSeconds(1));
    }

    public Script<T> Compile<T>(string folder, string file) where T : class
    {
        var targetDir = Path.Combine(Fixture.DataDirectory, "Scripts", folder);
        Directory.CreateDirectory(targetDir);
        File.Copy(Path.Combine(ShippedDataDirectory, "Scripts", folder, file),
            Path.Combine(targetDir, file), overwrite: true);
        return Fixture.World.ScriptHandler.GetScript<T>("Scripts/" + folder + "/" + file);
    }

    public static object? Call(object scriptObject, string method, params object?[] args)
        => scriptObject.GetType().GetMethod(method)!.Invoke(scriptObject, args);

    public void Dispose() => Fixture.Dispose();

    private object DefaultConfig()
    {
        return new
        {
            maps = new { house = 48, basement = 39, grove = 38,
                         groveExit = new[] { 38, 37 }, foyerSpawn = new[] { 43, 53 }, basementArrival = new[] { 0, 0 } },
            xpBonus = 0.5,
            items = new { candy = CandyId, partyCostume = PartyCostumeId, masks = MaskIds, ritualCandle = RitualCandleId },
            npcs = new { stalker = StalkerId, groundskeeper = GroundskeeperId, wailingGhost = WailingGhostId,
                         flickeringGhost = FlickeringGhostId, pinkPhantom = PinkPhantomId, bloodyGhost = BloodyGhostId,
                         restlessBones = RestlessBonesId },
            candy = new { levelOnly = 50, baseChance = 0.25, baseAmount = new[] { 1, 1 },
                          byTemplate = new Dictionary<int, int[]> { [WailingGhostId] = new[] { 1, 3 } }, stalkerSurvived = 10 },
            rooms = new[]
            {
                new { name = "Foyer", rect = new[] { 0, 0, 5, 5 }, weight = 0, table = "house" },
                new { name = "Master Bedroom", rect = new[] { 10, 10, 4, 4 }, weight = 3, table = "master" },
                new { name = "Study", rect = new[] { 20, 20, 4, 4 }, weight = 1, table = "house" },
            },
            ambush = new
            {
                baseChance = 0.006, basementWeight = 2.5, defaultWeight = 1.0,
                cooldownSeconds = 40, basementCooldownSeconds = 30, fakeCooldownSeconds = 15,
                groupCooldownRadius = 8, packRadius = 6, packMax = 5,
                despawnSeconds = 90, maxLivePerMap = 15, maxPhantomsHouse = 1, maxPhantomsBasement = 2,
                tables = new Dictionary<string, object>
                {
                    ["house"] = new { fake = 50, ghosts = 45, phantom = 3, stalker = 2 },
                    ["master"] = new { fake = 40, ghosts = 35, phantom = 22, stalker = 3 },
                    ["basement"] = new { fake = 20, bones = 45, bloody = 25, phantom = 8, stalker = 2 },
                },
                packs = new Dictionary<string, object>
                {
                    ["wailing"] = 2, ["flickering"] = 3,
                    ["bones"] = new[] { 3, 4 }, ["bloody"] = new[] { 1, 2 },
                },
                candy = new Dictionary<string, int[]>
                {
                    ["ghost"] = new[] { 3, 5 }, ["bones"] = new[] { 3, 5 },
                    ["bloody"] = new[] { 3, 5 }, ["phantom"] = new[] { 15, 25 },
                },
                fakeScares = new Dictionary<string, string[]>
                {
                    ["house"] = ["The portrait's eyes follow you…", "A door slams upstairs."],
                    ["basement"] = ["Something drips in the dark."],
                    ["directional"] = ["Footsteps behind you."],
                },
            },
            stalker = new { graceSeconds = 300, maxLive = 3, spawnBehind = 4,
                             chainsawMinMinutes = 20, chainsawMaxMinutes = 40, chainsawMinPlayers = 3,
                             chainsawDelaySeconds = 5, costumeGraceSeconds = 10 },
            pentagram = new { points = new[] { new[] { 67, 4 }, new[] { 58, 10 }, new[] { 76, 10 },
                                               new[] { 62, 21 }, new[] { 72, 21 } },
                              centre = new[] { 67, 13 } },
            invasion = new { towns = new int[0], templates = new int[0], count = 20, everyMinutes = 30, lifetimeMinutes = 15 },
            effects = new { weeperCap = 0, groundskeeperCurve = 0, ghostSnareOnHit = 0 },
            weeper = new { capPerHit = 100000 },
            groundskeeper = new { bloodyDamageMultiplier = 0.25,
                                   curve = new { floor = 10000, band = 70000, exponent = 0.4 } },
        };
    }

    private const string HostScript = """
        #load "Halloween/HalloweenConfig.csx"
        #load "Halloween/HalloweenCommon.csx"
        using System;
        using Goose;
        using Goose.Scripting;

        public class HalloweenHost : BaseGlobalScript
        {
            public object Config(GameWorld world) => HalloweenConfig.Get(world);
            public bool Active(GameWorld world) => Halloween.Active(world);
            public void SetActive(GameWorld world, bool on) => Halloween.SetActive(world, on);
            public bool Tier(GameWorld world, int n) => Halloween.Tier(world, n);
            public void SetTier(GameWorld world, int n, bool on) => Halloween.SetTier(world, n, on);
            public bool IsHouse(GameWorld world, Map map) => Halloween.IsHouse(map, HalloweenConfig.Get(world));
            public bool IsBasement(GameWorld world, Map map) => Halloween.IsBasement(map, HalloweenConfig.Get(world));
            public bool IsEventMap(GameWorld world, Map map) => Halloween.IsEventMap(map, HalloweenConfig.Get(world));
            public bool IsInCostume(GameWorld world, Player player) => Halloween.IsInCostume(player, HalloweenConfig.Get(world));
            public string? RoomAt(GameWorld world, int x, int y) => Halloween.RoomAt(HalloweenConfig.Get(world), x, y)?.Name;
            public double Weight(GameWorld world, int x, int y) => Halloween.Weight(HalloweenConfig.Get(world), x, y);
            public string Table(GameWorld world, int x, int y) => Halloween.Table(HalloweenConfig.Get(world), x, y);
            public long GetState(Player player, string key) => Halloween.GetState(player, key);
            public void SetState(Player player, string key, long value) => Halloween.SetState(player, key, value);
            public void Feed(GameWorld world, Player player, string text) => Halloween.Feed(world, player, text);
            public long Ticks(GameWorld world, double seconds) => Halloween.Ticks(world, seconds);
        }

        return typeof(HalloweenHost);
        """;

    private static string ShippedDataDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "Goose", "Data", "Illutia");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Goose/Data/Illutia not found");
        }
    }

    public sealed class FixedRandom : Random
    {
        private readonly Queue<double> doubles = new();
        private readonly Queue<int> ints = new();

        public FixedRandom(IEnumerable<double> nextDouble, IEnumerable<int>? next = null)
        {
            foreach (var value in nextDouble) doubles.Enqueue(value);
            if (next is not null)
                foreach (var value in next) ints.Enqueue(value);
        }

        public override double NextDouble() => doubles.Count > 0 ? doubles.Dequeue() : 0.0;
        public override int Next() => ints.Count > 0 ? ints.Dequeue() : 0;
        public override int Next(int maxValue) => Next() % maxValue;
        public override int Next(int minValue, int maxValue) => minValue + (int)(NextDouble() * (maxValue - minValue));
    }
}
