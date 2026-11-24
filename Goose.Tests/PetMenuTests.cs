using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class PetMenuTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map) WorldAndPlayer()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            return (fixture, player, map);
        }

        private static Pet MakePet(TestWorldFixture fixture, Player owner, int id, string name)
        {
            var pet = new Pet
            {
                PetID = id,
                Name = name,
                Owner = owner,
                Level = 1,
                BaseStats = new AttributeSet(),
                MaxStats = new AttributeSet(),
                Class = fixture.World.ClassHandler.GetClass(0)!,
            };
            owner.Pets.Add(pet);
            return pet;
        }

        private static OptionListWindow OptionListOf(TestWorldFixture.CapturingPlayer player)
        {
            var w = Assert.Single(player.Windows, x => x.Type == Window.WindowTypes.OptionList);
            return (OptionListWindow)w;
        }

        [Fact]
        public void PetLine_alive_non_attack_shows_mode()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Spawn(fixture.World);
                pet.Mode = Pet.Modes.Follow;

                Assert.Equal("Rex  Lv 1  [Out - Follow]", PetMenu.PetLine(pet));
            }
        }

        [Fact]
        public void PetLine_alive_attack_shows_attacking()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Spawn(fixture.World);
                pet.Mode = Pet.Modes.Attack;

                Assert.Equal("Rex  Lv 1  [Out - Attacking]", PetMenu.PetLine(pet));
            }
        }

        [Fact]
        public void PetLine_dead_with_cooldown_shows_remaining_seconds()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.NextRespawnTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30;

                Assert.Matches("^Rex  Lv 1  \\[Cooldown (29|30)s\\]$", PetMenu.PetLine(pet));
            }
        }

        [Fact]
        public void PetLine_dead_without_cooldown_has_no_status()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.NextRespawnTime = 0;

                Assert.Equal("Rex  Lv 1", PetMenu.PetLine(pet));
            }
        }

        [Fact]
        public void Open_title_shows_pet_count_over_max()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.PetCountLimit = 5;
                MakePet(fixture, player, 1, "Rex");
                MakePet(fixture, player, 2, "Fido");

                PetMenu.Open(player, fixture.World);

                Assert.Equal("Pets (2/5)", OptionListOf(player).Title);
            }
        }

        [Fact]
        public void Open_sends_one_line_per_pet()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var rex = MakePet(fixture, player, 1, "Rex");
                var fido = MakePet(fixture, player, 2, "Fido");
                rex.Spawn(fixture.World);
                rex.Mode = Pet.Modes.Defend;
                fido.NextRespawnTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30;

                PetMenu.Open(player, fixture.World);

                Assert.Contains(player.Sent, s => s.StartsWith("WNF") && s.Contains("Rex  Lv 1  [Out - Defend]"));
                Assert.Contains(player.Sent, s => s.StartsWith("WNF") && s.Contains("Fido  Lv 1  [Cooldown ") && s.Contains("s]"));
            }
        }

        [Fact]
        public void Open_with_no_pets_sends_tame_line()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                PetMenu.Open(player, fixture.World);

                Assert.Contains(player.Sent, s => s.StartsWith("WNF") && s.Contains("You have no pets. Tame one!"));
            }
        }

        [Fact]
        public void Open_twice_leaves_exactly_one_non_npc_optionlist_window()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakePet(fixture, player, 1, "Rex");

                PetMenu.Open(player, fixture.World);
                PetMenu.Open(player, fixture.World);

                var open = player.Windows.Count(w => w.Type == Window.WindowTypes.OptionList && w.NPC is null);
                Assert.Equal(1, open);
            }
        }

        [Fact]
        public void Clicking_pet_line_closes_window()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakePet(fixture, player, 1, "Rex");

                PetMenu.Open(player, fixture.World);
                OptionListOf(player).LineClicked(0, 0, player, fixture.World);

                Assert.Empty(player.Windows.Where(w => w.Type == Window.WindowTypes.OptionList && w.NPC is null));
            }
        }
    }
}
