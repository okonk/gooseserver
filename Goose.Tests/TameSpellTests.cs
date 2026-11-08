using Goose.Testing;

namespace Goose.Tests;

public sealed class TameSpellTests
{
    [Fact]
    public void Untameable_npc_refuses_with_a_notice_and_no_pet()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var template = fixture.AddNpcTemplate(1, "Arena Ghast", t => t.MoveSpeed = 1);
        var npc = fixture.SpawnNpc(map, 2, 1, template, properties: new PropertiesDictionary { ["untameable"] = true });
        player.Sent.Clear();

        Assert.False(new SpellEffect().CastTameSpell(player, npc, fixture.World));

        Assert.Empty(player.Pets);
        Assert.Contains(player.Sent, p => p.Contains("Arena Ghast can't be tamed."));
    }
}
