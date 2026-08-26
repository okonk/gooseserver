using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class TestFixtureIsolationTests
{
    [Fact]
    public async Task Concurrent_fixtures_with_conflicting_settings_stay_isolated()
    {
        var ta = Task.Run(() => new TestWorldFixture(s =>
        {
            s.InventorySize = 10;
            s.VendorSlotSize = 11;
            s.ExperienceModifier = 2;
        }));
        var tb = Task.Run(() => new TestWorldFixture(s =>
        {
            s.InventorySize = 40;
            s.VendorSlotSize = 41;
            s.ExperienceModifier = 7;
        }));
        var a = await ta;
        var b = await tb;
        using (a)
        using (b)
        {
            Assert.NotSame(a.Settings, b.Settings);
            Assert.NotEqual(a.DataDirectory, b.DataDirectory);

            Assert.Equal(10, a.Settings.InventorySize);
            Assert.Equal(40, b.Settings.InventorySize);
            Assert.Equal(11, a.Settings.VendorSlotSize);
            Assert.Equal(41, b.Settings.VendorSlotSize);
            Assert.Equal(2m, a.World.ExperienceModifier);
            Assert.Equal(7m, b.World.ExperienceModifier);

            Assert.Equal(11, InventorySlots(a));
            Assert.Equal(41, InventorySlots(b));
            Assert.Equal(12, VendorSlots(a));
            Assert.Equal(42, VendorSlots(b));
        }
    }

    [Fact]
    public void Disposing_one_fixture_leaves_the_other_resolving_its_scripts_and_settings()
    {
        var a = new TestWorldFixture(s => s.ExperienceModifier = 5);
        using var b = new TestWorldFixture(s => s.ExperienceModifier = 3);

        WriteGlobalScript(a.DataDirectory, "A.csx", "ScriptA");
        WriteGlobalScript(b.DataDirectory, "B.csx", "ScriptB");
        var scriptA = a.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/A.csx");
        var scriptB = b.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/B.csx");
        Assert.NotSame(scriptA, scriptB);

        a.Dispose();

        Assert.False(Directory.Exists(a.DataDirectory));
        Assert.True(Directory.Exists(b.DataDirectory));

        var reloaded = b.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/B.csx");
        Assert.Same(scriptB, reloaded);
        Assert.Equal(3m, b.Settings.ExperienceModifier);
        Assert.Equal(3m, b.World.ExperienceModifier);

        var map = b.AddBaseMap(1, "Town");
        var player = b.CommandPlayerOn(map, 1, 1);
        Assert.Equal(b.Settings.InventorySize + 1, player.Inventory.GetInventorySlots().Length);
    }

    private static int InventorySlots(TestWorldFixture fixture)
    {
        var map = fixture.AddBaseMap(1, "Town");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        return player.Inventory.GetInventorySlots().Length;
    }

    private static int VendorSlots(TestWorldFixture fixture)
    {
        var map = fixture.AddBaseMap(2, "VendorTown");
        var template = new NPCTemplate
        {
            NPCTemplateID = 55, Name = "Merchant", Level = 1, ClassID = 1,
            BaseStats = new AttributeSet(),
        };
        template.VendorItems = new NPCVendorSlot[fixture.Settings.VendorSlotSize + 1];
        fixture.World.NPCHandler.AddTemplate(template);
        var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, 2, 1, 1, template, shouldRespawn: false);
        return npc.VendorItems!.Length;
    }

    private static void WriteGlobalScript(string dataDirectory, string fileName, string className)
    {
        File.WriteAllText(Path.Combine(dataDirectory, "Scripts", "Global", fileName), $@"
public class {className} : BaseGlobalScript
{{
}}

return typeof({className});
");
    }
}
