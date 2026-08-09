using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionsScriptTests
{
    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Disabled_by_configuration_changes_nothing()
    {
        // Dimensions.Enabled is false in the shipped script until the feature is switched on.
        using var fixture = Run(f => f.AddBaseMap(1, "Town"));

        Assert.Single(fixture.World.MapHandler.Maps);
        Assert.Null(fixture.World.MapHandler.GetMap(100001));
    }
}
