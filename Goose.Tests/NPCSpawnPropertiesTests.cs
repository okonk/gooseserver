using Goose.Testing;

namespace Goose.Tests;

public class NPCSpawnPropertiesTests
{
    private const int MapId = 1;

    private static NPCTemplate Template(bool canMove) => new()
    {
        NPCTemplateID = 1,
        Name = "Test NPC",
        Level = 50,
        ClassID = 0,
        BaseStats = new AttributeSet(),
        CanMove = canMove,
    };

    private static NPC Spawn(TestWorldFixture fixture, bool templateCanMove,
                             PropertiesDictionary? properties)
    {
        fixture.AddBaseMap(MapId, "Town");
        var template = Template(templateCanMove);
        fixture.World.NPCHandler.AddTemplate(template);
        return fixture.World.NPCHandler.SpawnNPC(
            fixture.World, MapId, 5, 5, template, shouldRespawn: true, properties)!;
    }

    [Fact]
    public void canMove_true_overrides_a_stationary_template()
    {
        using var fixture = new TestWorldFixture();

        var npc = Spawn(fixture, templateCanMove: false,
                        new PropertiesDictionary { ["canMove"] = true });

        Assert.True(npc.CanMove);
        Assert.True(npc.Properties.GetProperty<bool>("canMove"));
    }

    [Fact]
    public void canMove_false_overrides_a_movable_template()
    {
        using var fixture = new TestWorldFixture();

        var npc = Spawn(fixture, templateCanMove: true,
                        new PropertiesDictionary { ["canMove"] = false });

        Assert.False(npc.CanMove);
    }

    [Fact]
    public void An_empty_dictionary_keeps_the_template_value()
    {
        using var fixture = new TestWorldFixture();

        var npc = Spawn(fixture, templateCanMove: true, new PropertiesDictionary());

        Assert.True(npc.CanMove);
        Assert.Empty(npc.Properties);
    }

    [Fact]
    public void Omitting_the_properties_argument_keeps_the_template_value()
    {
        using var fixture = new TestWorldFixture();
        fixture.AddBaseMap(MapId, "Town");
        var template = Template(canMove: true);
        fixture.World.NPCHandler.AddTemplate(template);

        var npc = fixture.World.NPCHandler.SpawnNPC(
            fixture.World, MapId, 5, 5, template, shouldRespawn: true)!;

        Assert.True(npc.CanMove);
        Assert.Empty(npc.Properties);
    }

    [Fact]
    public void A_numeric_canMove_throws_rather_than_being_coerced()
    {
        using var fixture = new TestWorldFixture();

        // Accepted risk, recorded in the design doc: the sheet writes its Bool columns as
        // 0/1, so this is the mistake authors actually make, and it stops the load.
        Assert.Throws<InvalidCastException>(() =>
            Spawn(fixture, templateCanMove: false,
                  new PropertiesDictionary { ["canMove"] = 1L }));
    }
}
