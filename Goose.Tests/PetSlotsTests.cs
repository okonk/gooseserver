using Xunit;

namespace Goose.Tests;

public sealed class PetSlotsTests
{
    [Fact]
    public void MaxPets_is_base_when_no_bonus_key()
    {
        var player = new Player(0);
        var settings = new GooseSettings { PetCountLimit = 5, PetSlotsMax = 30 };

        Assert.Equal(5, player.MaxPets(settings));
    }

    [Fact]
    public void MaxPets_adds_slot_bonus()
    {
        var player = new Player(0) { PetSlotsBonus = 2 };
        var settings = new GooseSettings { PetCountLimit = 5, PetSlotsMax = 30 };

        Assert.Equal(7, player.MaxPets(settings));
    }

    [Fact]
    public void MaxPets_clamps_at_pet_slots_max()
    {
        var player = new Player(0) { PetSlotsBonus = 40 };
        var settings = new GooseSettings { PetCountLimit = 5, PetSlotsMax = 30 };

        Assert.Equal(30, player.MaxPets(settings));
    }
}
