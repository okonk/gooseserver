using Goose;
using Goose.Testing;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>In GameWorldSettingsCollection: the production paths these tests
/// exercise read the process-global GameWorld.Settings (shipped defaults until the
/// per-world migration completes), so they must not run in parallel with classes
/// that mutate that static.</summary>
[Collection(GameWorldSettingsCollection.Name)]
public class PlayerEconomyOverloadTests
{

    /// <summary>Rebirth must not shave the settings loss percent. ChangeClass banks
    /// Experience into ExperienceSold (Player.cs:1368) and multiplies the result by
    /// (1 - loss) at Player.cs:1370; an explicit 0 has to skip that entirely.</summary>
    [Fact]
    public void ChangeClass_with_explicit_zero_loss_banks_the_full_experience()
    {
        using var fixture = new TestWorldFixture();
        fixture.Settings.ChangeClassExperienceLossPercent = 0.07;
        var map = fixture.AddBaseMap(9100, "Overload Map");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 1_000_000;
        player.ExperienceSold = 0;
        player.Spellbook = new Spellbook(player);

        player.ChangeClass(1, 1, fixture.World, 0d);

        Assert.Equal(1_000_000, player.ExperienceSold);
    }

    [Fact]
    public void ChangeClass_three_arg_overload_still_applies_the_settings_loss()
    {
        using var fixture = new TestWorldFixture();
        fixture.Settings.ChangeClassExperienceLossPercent = 0.07;
        var map = fixture.AddBaseMap(9101, "Overload Map 2");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 1_000_000;
        player.ExperienceSold = 0;
        player.Spellbook = new Spellbook(player);

        player.ChangeClass(1, 1, fixture.World);

        // 1.0d - 0.07 is 0.92999999...334 in double, so the (long) truncation lands on
        // 929999, exactly as the pre-overload code did. Asserting this proves the moved
        // body is bit-for-bit identical, not "approximately 93%".
        Assert.Equal(929_999, player.ExperienceSold);
    }

    /// <summary>Purchased experience must arrive un-multiplied. AddExperience scales by
    /// world.ExperienceModifier on a branch selected by ExperienceModifierLimit
    /// (Player.cs:1662-1671), which script cannot invert reliably.</summary>
    [Theory]
    [InlineData(0L)]            // no limit configured -> full-modifier branch
    [InlineData(1_000L)]        // player is past the limit -> reduced-modifier branch
    public void AddExperience_without_modifiers_grants_the_exact_amount(long modifierLimit)
    {
        using var fixture = new TestWorldFixture();
        fixture.Settings.ExperienceCap = 0;
        fixture.Settings.ExperienceModifier = 2;
        fixture.Settings.ExperienceModifierLimit = (int)modifierLimit; // Settings field is int
        fixture.World.ExperienceModifier = 2;

        var map = fixture.AddBaseMap(9102 + (int)modifierLimit, "Overload Map 3");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 50_000;

        player.AddExperience(25_000_000, fixture.World, Player.ExperienceMessage.None, applyModifiers: false);

        Assert.Equal(25_050_000, player.Experience);
    }

    [Fact]
    public void AddExperience_three_arg_overload_still_applies_the_modifier()
    {
        using var fixture = new TestWorldFixture();
        fixture.Settings.ExperienceCap = 0;
        fixture.Settings.ExperienceModifier = 2;
        fixture.Settings.ExperienceModifierLimit = 0;
        fixture.World.ExperienceModifier = 2;

        var map = fixture.AddBaseMap(9105, "Overload Map 4");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 0;

        player.AddExperience(1_000, fixture.World, Player.ExperienceMessage.None);

        Assert.Equal(2_000, player.Experience);
    }
}
