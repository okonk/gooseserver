using System.Text.Json;
using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaTestKit : IDisposable
{
    public TestWorldFixture Fixture { get; }
    public GameWorld World => Fixture.World;
    public object Host { get; }
    public string ConfigPath { get; }
    public Map ArenaMap => World.MapHandler.Maps[ArenaMapId];
    public Map LobbyMap => World.MapHandler.Maps[LobbyMapId];

    public const int LobbyMapId = 40001;
    public const int ArenaMapId = 40002;
    public static readonly int[] LobbySpawn = [10, 10];
    public static readonly int[] ArenaRect = [10, 10, 80, 80];
    public static readonly int[] BandNormalIds = [41001, 41002, 41003];
    public const int BandBossId = 41101;

    public ArenaTestKit()
    {
        Fixture = new TestWorldFixture();
        var arenaDir = Path.Combine(Fixture.DataDirectory, "Scripts", "Arena");
        Directory.CreateDirectory(arenaDir);
        ConfigPath = Path.Combine(arenaDir, "arena.json");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(ShippedDataDirectory, "Scripts", "Arena"), "*.csx"))
            File.Copy(file, Path.Combine(arenaDir, Path.GetFileName(file)), overwrite: true);
        File.Copy(Path.Combine(ShippedDataDirectory, "Scripts", "Global", "Arena.csx"),
            Path.Combine(Fixture.DataDirectory, "Scripts", "Global", "Arena.csx"), overwrite: true);

        var mobScript = Fixture.World.ScriptHandler.GetScript<INPCScript>("Scripts/Arena/ArenaMob.csx");
        foreach (var id in BandNormalIds)
            Fixture.AddNpcTemplate(id, "Arena Mob " + id, t => t.Script = mobScript);
        Fixture.AddNpcTemplate(BandBossId, "Arena Boss", t => t.Script = mobScript);
        Fixture.AddBaseMap(LobbyMapId, "Arena Lobby", 100, 100);
        Fixture.AddBaseMap(ArenaMapId, "Arena", 100, 100);

        Fixture.World.CurrencyHandler.Register(new DataCurrency("arena_coin", "Arena Coin", "Arena", null, true));

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(DefaultConfig()));
        File.WriteAllText(Path.Combine(Fixture.DataDirectory, "Scripts", "Global", "ArenaHost.csx"), HostScript);
        Host = Fixture.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/ArenaHost.csx").Object;
        Fixture.Settings.MaxAC = 255;
        Fixture.World.WorldState.Set("arena.open", true);
    }

    public TestWorldFixture.CapturingPlayer LobbyPlayer(int playerId, string name, bool gm = false)
    {
        var player = Fixture.CommandPlayerOn(World.MapHandler.Maps[LobbyMapId], 10, 10, name);
        player.PlayerID = playerId;
        if (gm) player.Access = Player.AccessStatus.GameMaster;
        Fixture.AddOnlinePlayer(player);
        World.MapHandler.Maps[LobbyMapId].Players.Add(player);
        return player;
    }

    public void Enter(Map map, Player player)
    {
        player.State = Player.States.Ready;
        player.Map = map;
        player.MapID = map.ID;
        map.AddPlayer(player, World);
    }

    public Script<IGlobalScript> RegisterController() => World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/Arena.csx");

    public Script<IMapScript> AttachMapScript()
    {
        var script = Compile<IMapScript>("Arena", "ArenaMap.csx");
        World.MapHandler.Maps[ArenaMapId].Script = script;
        return script;
    }

    public long Wallet(Player player) => World.CurrencyHandler.Get("arena_coin")!.GetBalance(player);

    public static object? GetProp(object? obj, string name) => obj?.GetType().GetProperty(name)?.GetValue(obj);

    public static void SetProp(object obj, string name, object? value) => obj.GetType().GetProperty(name)!.SetValue(obj, value);

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
            lobbyMap = LobbyMapId,
            lobbySpawn = LobbySpawn,
            arenaMap = ArenaMapId,
            arenaRect = ArenaRect,
            intervalHours = 3,
            anchorUtc = "00:00",
            announceMinutes = new[] { 15, 10, 5 },
            countdownSeconds = 10,
            breakSeconds = 15,
            roundSeconds = 180,
            timeoutWarningSeconds = 30,
            stragglerCount = 3,
            minPlayerDistance = 3,
            baseCount = 4,
            perPlayer = 2,
            bandSize = 10,
            bands = new[] { new { normals = BandNormalIds, boss = BandBossId } },
            eliteFromRound = 3,
            eliteChance = 0.05,
            eliteChanceGrowth = 0.02,
            eliteChanceMax = 0.30,
            eliteHpMult = 5,
            eliteDmgMult = 2,
            eliteTitle = "Elite",
            eliteTint = new[] { 255, 160, 0, 255 },
            bossEvery = 5,
            bossRoundAddFraction = 0.5,
            bossHpMult = 40,
            bossHpPerPlayer = 0.25,
            bossDmgMult = 3,
            minXpSold = 4000000,
            topXpSold = 3000000000,
            a = 0.75,
            b = 0.75,
            anchorRound = 20,
            anchorMobHp = 12000000,
            anchorMobWeaponDamage = 65000,
            hpGrowth = 1.10,
            dmgGrowth = 1.08,
            currencyId = "arena_coin",
            roundBase = 5,
            roundStep = 1,
            mobBounty = 1,
            eliteBountyMult = 5,
            bossBountyMult = 25,
            killCreditMinShare = 0.05,
            priestClassIds = new[] { 5 },
            priestKillShare = 0.5,
            milestones = new Dictionary<string, long> { ["5"] = 20, ["10"] = 60, ["15"] = 120, ["20"] = 200 },
        };
    }

    private const string HostScript = """
        #load "../Arena/ArenaConfig.csx"
        #load "../Arena/ArenaMath.csx"
        #load "../Arena/ArenaState.csx"
        #load "../Arena/ArenaBoard.csx"
        #load "../Arena/ArenaCore.csx"
        #load "../Arena/ArenaBoss.csx"
        using System;
        using System.Collections.Generic;
        using Goose;
        using Goose.Scripting;

        public class ArenaHost : BaseGlobalScript
        {
            public object Config(GameWorld world) => ArenaConfig.Get(world);
            public void StartRun(GameWorld world, DateTime now) => ArenaCore.StartRun(world, now);
            public void Tick(GameWorld world, DateTime now) => ArenaCore.Tick(world, now);
            public void OnNpcKilled(NPC npc, ICharacter killer, List<Player> credited, GameWorld world) => ArenaCore.OnNpcKilled(npc, killer, credited, world);
            public void Eliminate(GameWorld world, int id, string reason) => ArenaCore.Eliminate(world, id, reason);
            public void EndIfEmpty(GameWorld world, DateTime now) => ArenaCore.EndIfEmpty(world, now);
            public void EndRun(GameWorld world, string reason) => ArenaCore.EndRun(world, reason);
            public void Pay(GameWorld world, int id, long amount, string reason) => ArenaCore.Pay(world, id, amount, reason);
            public bool IsParticipant(GameWorld world, int id) => ArenaCore.IsParticipant(world, id);
            public object Run(GameWorld world) => ArenaState.Load(world);
            public object LastRun(GameWorld world) => ArenaState.LoadLastRun(world);
            public string WeekKey(DateTime utc) => ArenaBoard.WeekKey(utc);
            public object BoardLoad(GameWorld world) => ArenaBoard.Load(world);
            public object NewBoard() => new ArenaBoard();
            public object NewEntry() => new ArenaBoardEntry();
            public void BoardSave(GameWorld world, object board) => ArenaBoard.Save(world, (ArenaBoard)board);
            public void BoardRecord(GameWorld world, object run, DateTime now) => ArenaBoard.Record(world, (ArenaRun)run, now);
            public List<(int Id, double Value)> BoardTop(object board, int kind, bool weekly, string week, int n) => ArenaBoard.Top((ArenaBoard)board, (ArenaBoardKind)kind, weekly, week, n);
            public (int Rank, double Value)? BoardRankOf(object board, int id, int kind, bool weekly, string week) => ArenaBoard.RankOf((ArenaBoard)board, id, (ArenaBoardKind)kind, weekly, week);
            public void SaveRun(GameWorld world, object run) => ArenaState.Save(world, (ArenaRun)run);
            public Player FindPlayer(GameWorld world, int id) => ArenaCore.FindPlayer(world, id);
            public bool Enabled(GameWorld world) => ArenaConfig.Get(world).Enabled(world);
            public double PowerFraction(long xp, object c) => ArenaMath.PowerFraction(xp, (ArenaConfig)c);
            public long EffectiveDamage(long raw, double s, object c) => ArenaMath.EffectiveDamage(raw, s, (ArenaConfig)c);
            public long MobHp(int round, int tier, int alive, object c) => ArenaMath.MobHp(round, (MobTier)tier, alive, (ArenaConfig)c);
            public long MobXp(int round, int tier, object c) => ArenaMath.MobXp(round, (MobTier)tier, (ArenaConfig)c);
            public double MobWeaponDamage(int round, int tier, double s, object c) => ArenaMath.MobWeaponDamage(round, (MobTier)tier, s, (ArenaConfig)c);
            public int WaveCount(int round, int alive, object c) => ArenaMath.WaveCount(round, alive, (ArenaConfig)c);
            public bool IsBossRound(int round, object c) => ArenaMath.IsBossRound(round, (ArenaConfig)c);
            public double EliteChance(int round, object c) => ArenaMath.EliteChance(round, (ArenaConfig)c);
            public object BandFor(int round, object c) => ArenaMath.BandFor(round, (ArenaConfig)c);
            public long RoundPayout(int round, object c) => ArenaMath.RoundPayout(round, (ArenaConfig)c);
            public long Bounty(int tier, object c) => ArenaMath.Bounty((MobTier)tier, (ArenaConfig)c);
            public long MilestoneBonus(int rounds, object c) => ArenaMath.MilestoneBonus(rounds, (ArenaConfig)c);
            public List<int> KillCredit(Dictionary<int, long> dmg, double minShare) => ArenaMath.KillCredit(dmg, minShare);
            public DateTime NextStartUtc(DateTime now, object c) => ArenaMath.NextStartUtc(now, (ArenaConfig)c);
            public object BossStart(string kind, NPC npc, GameWorld world) => ArenaBoss.Start(kind, npc, world, ArenaConfig.Get(world));
            public void BossResolve(object t) => ((ArenaTelegraph)t).Resolve();
            public List<(int X, int Y)> BossTiles(object t) => ((ArenaTelegraph)t).LiveTiles?.Invoke() ?? ((ArenaTelegraph)t).Tiles;
            public List<NPC> BossSummon(NPC npc, GameWorld world) => ArenaBoss.Summon(npc, world, ArenaConfig.Get(world));
            public void BossHold(NPC npc, GameWorld world) => ArenaBoss.HoldTarget(npc, world);
            public bool BossThink(NPC npc, GameWorld world) => ArenaBoss.Think(npc, world, ArenaConfig.Get(world));
            public bool BossBusy(NPC npc, GameWorld world) => ArenaBoss.IsBusy(npc, world);
            public bool BossShielded(NPC npc) => ArenaBoss.IsShielded(npc);
        }

        return typeof(ArenaHost);
        """;

    public static string ShippedDataDirectory
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
}
