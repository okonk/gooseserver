using Goose.Events;
using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestIconVisibilityTests : IDisposable
{
    private readonly TestWorldFixture world;

    public QuestIconVisibilityTests()
    {
        world = new TestWorldFixture();
    }

    public void Dispose() => world.Dispose();

    private Map NewMap() => world.AddBaseMap(1, "Town", 40, 40);

    private NPCTemplate NewTemplate(Quest? quest = null) => new()
    {
        NPCTemplateID = 1,
        Name = "Test NPC",
        Level = 1,
        ClassID = 0,
        BaseStats = new AttributeSet(),
        Quests = quest is null ? new List<Quest>() : new List<Quest> { quest },
    };

    private NPC SpawnNpc(int x, int y, Quest? quest = null)
        => world.World.NPCHandler.SpawnNPC(world.World, 1, x, y, NewTemplate(quest), false)!;

    private static void AssertMkcPrecedesChi(List<string> sent, int npcLoginId)
    {
        string mkc = "MKC" + npcLoginId + ",";
        string chi = "CHI" + npcLoginId + ",";
        int mkcIndex = sent.FindIndex(s => s.StartsWith(mkc));
        int chiIndex = sent.FindIndex(s => s.StartsWith(chi));
        Assert.True(mkcIndex >= 0, "no MKC packet for npc " + npcLoginId);
        Assert.True(chiIndex >= 0, "no CHI packet for npc " + npcLoginId);
        Assert.True(mkcIndex < chiIndex, "MKC at index " + mkcIndex + " must precede CHI at index " + chiIndex);
    }

    [Fact]
    public void InitialMapLoad_SendsIconImmediatelyAfterNpcPublication()
    {
        var map = NewMap();
        var npc = SpawnNpc(10, 10);

        var player = world.CommandPlayerOn(map, 10, 10, "Hero");
        player.State = Player.States.LoadingMap;

        var ev = new DoneLoadingMapEvent { Player = player, Ticks = world.World.TimeNow };
        world.World.EventHandler.AddEvent(ev);
        world.World.EventHandler.Update(world.World);

        AssertMkcPrecedesChi(player.Sent, npc.LoginID);
    }

    [Fact]
    public void PlayerMoveIntoNpcRange_SendsIconImmediatelyAfterNpcPublication()
    {
        var map = NewMap();
        var npc = SpawnNpc(30, 30);

        var player = world.CommandPlayerOn(map, 1, 1, "Hero");
        player.MoveTo(world.World, 30, 30);

        AssertMkcPrecedesChi(player.Sent, npc.LoginID);
    }

    [Fact]
    public void NpcSpawnIntoPlayerRange_SendsIconImmediatelyAfterNpcPublication()
    {
        var map = NewMap();
        var player = world.CommandPlayerOn(map, 10, 10, "Hero");
        map.AddPlayer(player, world.World);

        var npc = SpawnNpc(10, 10);

        AssertMkcPrecedesChi(player.Sent, npc.LoginID);
    }

    [Fact]
    public void NpcMoveIntoPlayerRange_SendsIconImmediatelyAfterNpcPublication()
    {
        var map = NewMap();
        var player = world.CommandPlayerOn(map, 1, 1, "Hero");
        map.AddPlayer(player, world.World);

        var npc = SpawnNpc(30, 30);
        npc.MoveTo(world.World, 1, 1);

        AssertMkcPrecedesChi(player.Sent, npc.LoginID);
    }

    [Fact]
    public void SameMapWarp_SendsIconImmediatelyAfterNpcPublication()
    {
        var map = NewMap();
        var npc = SpawnNpc(10, 10);

        var player = world.CommandPlayerOn(map, 1, 1, "Hero");
        player.WarpTo(world.World, map, 10, 10);

        AssertMkcPrecedesChi(player.Sent, npc.LoginID);
    }

    [Fact]
    public void InitialMapLoad_ScriptedRequirementReadingMap_ResolvesAgainstNewMap()
    {
        var destination = world.AddBaseMap(2, "Destination", 40, 40);

        var script = world.CompileQuestScript(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => player.Map is not null && player.Map.ID == 2;
}
return typeof(T);
");
        var quest = new Quest { Id = 1, Name = "Q", Description = "d" };
        quest.Requirements.Add(new QuestRequirement
        {
            Id = 1, Type = RequirementType.Script, Script = script, Quest = quest,
        });

        var npc = world.World.NPCHandler.SpawnNPC(world.World, 2, 10, 10, NewTemplate(quest), false)!;

        var player = world.CommandPlayerOn(destination, 10, 10, "Hero");
        // Simulate the post-teleport state: removed from the old map, Map is null.
        player.Map = null!;
        player.MapID = destination.ID;
        player.QuestsStarted.Add(quest);
        player.State = Player.States.LoadingMap;

        var ev = new DoneLoadingMapEvent { Player = player, Ticks = world.World.TimeNow };
        world.World.EventHandler.AddEvent(ev);
        world.World.EventHandler.Update(world.World);

        var s = world.Settings;
        Assert.Contains(player.Sent, pkt =>
            pkt.StartsWith($"CHI{npc.LoginID},{s.QuestReadyIconSheet},{s.QuestReadyIconGraphic}"));
    }

    [Fact]
    public void SameNpc_SendsDifferentIconPayloadsToDifferentViewers()
    {
        var map = NewMap();
        var quest = new Quest { Id = 1, Name = "Q", Description = "d", MinLevel = 5 };

        var ready = world.CommandPlayerOn(map, 10, 10, "Ready");
        ready.Level = 5;
        ready.QuestsStarted.Add(quest);
        map.AddPlayer(ready, world.World);

        var plain = world.CommandPlayerOn(map, 10, 11, "Plain");
        map.AddPlayer(plain, world.World);

        var npc = SpawnNpc(10, 10, quest);

        AssertMkcPrecedesChi(ready.Sent, npc.LoginID);
        AssertMkcPrecedesChi(plain.Sent, npc.LoginID);

        var s = world.Settings;
        Assert.Contains(ready.Sent, pkt =>
            pkt.StartsWith($"CHI{npc.LoginID},{s.QuestReadyIconSheet},{s.QuestReadyIconGraphic}"));
        Assert.Contains(plain.Sent, pkt => pkt.StartsWith($"CHI{npc.LoginID},0,0"));
    }
}
