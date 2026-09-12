using System.Data.SQLite;
using Goose.Testing;

namespace Goose.Tests;

public class NPCSpawnPropertiesLoadTests
{
    private const int MapId = 1;

    private static TestWorldFixture WorldWithSpawnRow(string? propertiesCell)
    {
        var fixture = new TestWorldFixture();
        fixture.AddBaseMap(MapId, "Town");
        fixture.World.NPCHandler.AddTemplate(new NPCTemplate
        {
            NPCTemplateID = 1, Name = "Test NPC", Level = 50, ClassID = 0,
            BaseStats = new AttributeSet(), CanMove = false,
        });

        fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
        fixture.World.Database.Execute(conn =>
        {
            using (var schema = conn.CreateCommand())
            {
                schema.CommandText = File.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "sql", "npcs.sql"));
                schema.ExecuteNonQuery();
            }

            using var insert = conn.CreateCommand();
            insert.CommandText =
                "INSERT INTO npc_spawns (npc_id, map_id, map_x, map_y, properties) " +
                "VALUES (1, " + MapId + ", 5, 5, @p)";
            insert.Parameters.Add(new SQLiteParameter("@p", propertiesCell));
            insert.ExecuteNonQuery();
        });

        return fixture;
    }

    private static NPC OnlyNpc(TestWorldFixture fixture) =>
        Assert.Single(fixture.World.MapHandler.GetMap(MapId)!.NPCs);

    [Fact]
    public void A_spawn_row_cell_reaches_the_loaded_npc()
    {
        using var fixture = WorldWithSpawnRow("{\"canMove\":true}");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        var npc = OnlyNpc(fixture);
        Assert.True(npc.CanMove);
        Assert.True(npc.Properties.GetProperty<bool>("canMove"));
    }

    [Fact]
    public void A_blank_cell_keeps_the_template_value()
    {
        using var fixture = WorldWithSpawnRow("");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        var npc = OnlyNpc(fixture);
        Assert.False(npc.CanMove);
        Assert.Empty(npc.Properties);
    }

    [Fact]
    public void An_unreadable_cell_still_spawns_the_npc_and_names_the_row()
    {
        using var log = new CapturingLog();
        using var fixture = WorldWithSpawnRow("{\"canMove\":");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        var npc = OnlyNpc(fixture);
        Assert.False(npc.CanMove);
        Assert.Empty(npc.Properties);
        Assert.Contains(log.Messages, m =>
            m.Contains("npc_spawns") && m.Contains(MapId.ToString()) && m.Contains("properties"));
    }

    [Fact]
    public void A_json_root_that_is_not_an_object_is_unreadable_rather_than_fatal()
    {
        using var fixture = WorldWithSpawnRow("[]");

        fixture.World.NPCHandler.LoadNPCs(fixture.World);

        Assert.False(OnlyNpc(fixture).CanMove);
    }
}
