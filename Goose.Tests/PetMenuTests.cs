using System.Globalization;
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

        private static List<string> LinesOf(TestWorldFixture.CapturingPlayer player, OptionListWindow w)
        {
            var prefix = $"WNF{w.ID},";
            var lines = new Dictionary<int, string>();
            foreach (var s in player.Sent.Where(x => x.StartsWith(prefix)))
            {
                var rest = s.Substring(prefix.Length);
                int lineNo = int.Parse(rest.Split(',')[0], CultureInfo.InvariantCulture);
                int lineStart = rest.IndexOf(',') + 1;
                string text = rest.Substring(lineStart).Split('|')[0];
                lines[lineNo] = text;
            }
            return lines.OrderBy(x => x.Key).Select(x => x.Value).ToList();
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
        public void Clicking_pet_line_opens_pet_window()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.IncreasePetVitaBuyCost = 100000;
                fixture.Settings.IncreasePetDamageBuyCost = 100000;
                MakePet(fixture, player, 1, "Rex");

                PetMenu.Open(player, fixture.World);
                OptionListOf(player).LineClicked(0, 0, player, fixture.World);

                Assert.Equal("Rex", OptionListOf(player).Title);
            }
        }

        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Map map, Pet pet, OptionListWindow window) OpenPetWindow()
        {
            var (fixture, player, map) = WorldAndPlayer();
            map.CanSpawnPets = true;
            fixture.Settings.IncreasePetVitaBuyCost = 100000;
            fixture.Settings.IncreasePetDamageBuyCost = 100000;
            fixture.World.ClassHandler.GetClass(0)!.GetLevel(1)!.Experience = 100;
            var pet = MakePet(fixture, player, 1, "Rex");
            PetMenu.Open(player, fixture.World);
            OptionListOf(player).LineClicked(0, 0, player, fixture.World);
            var window = OptionListOf(player);
            return (fixture, player, map, pet, window);
        }

        [Fact]
        public void OpenPet_sends_eight_lines_with_expected_labels()
        {
            var (fixture, player, _, _, window) = OpenPetWindow();
            using (fixture)
            {
                Assert.Equal("Rex", window.Title);
                Assert.Equal(
                    ["Details", "Spawn", "Mode: Neutral", "Mode: Follow", "Mode: Defend",
                     "Buy HP (max level only)", "Buy Damage (max level only)", "Release"],
                    LinesOf(player, window));
            }
        }

        [Fact]
        public void OpenPet_max_level_shows_next_cost_labels()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                fixture.Settings.IncreasePetVitaBuyCost = 100000;
                fixture.Settings.IncreasePetDamageBuyCost = 100000;
                fixture.Settings.PetVitaCost = 100;
                fixture.Settings.PetDamageCost = 200;
                MakePet(fixture, player, 1, "Rex");

                PetMenu.Open(player, fixture.World);
                OptionListOf(player).LineClicked(0, 0, player, fixture.World);

                var lines = LinesOf(player, OptionListOf(player));
                Assert.Equal("Buy HP (next: 100 exp)", lines[5]);
                Assert.Equal("Buy Damage (next: 200 exp)", lines[6]);
            }
        }

        [Fact]
        public void OpenPet_spawn_label_flips_to_despawn_after_click()
        {
            var (fixture, player, _, pet, window) = OpenPetWindow();
            using (fixture)
            {
                window.LineClicked(1, 0, player, fixture.World);

                Assert.True(pet.IsAlive);
                Assert.Contains(player.Sent, s => s.StartsWith("NTF0,") && s.Contains("Rex has been summoned."));
                var reopened = OptionListOf(player);
                Assert.Equal("Despawn", LinesOf(player, reopened)[1]);
            }
        }

        [Fact]
        public void OpenPet_mode_on_non_spawned_pet_notices_and_leaves_mode_unchanged()
        {
            var (fixture, player, _, pet, window) = OpenPetWindow();
            using (fixture)
            {
                pet.Mode = Pet.Modes.Defend;

                window.LineClicked(2, 0, player, fixture.World);

                Assert.False(pet.IsAlive);
                Assert.Equal(Pet.Modes.Defend, pet.Mode);
                Assert.Contains(player.Sent, s => s.StartsWith("NTF0,") && s.Contains("isn't out"));
            }
        }

        [Fact]
        public void OpenPet_mode_marker_only_on_current_mode_when_spawned()
        {
            var (fixture, player, _, pet, window) = OpenPetWindow();
            using (fixture)
            {
                pet.Spawn(fixture.World);
                pet.Mode = Pet.Modes.Follow;

                PetMenu.Open(player, fixture.World);
                OptionListOf(player).LineClicked(0, 0, player, fixture.World);

                var lines = LinesOf(player, OptionListOf(player));
                Assert.False(lines[2].StartsWith("* "));
                Assert.StartsWith("* ", lines[3]);
                Assert.False(lines[4].StartsWith("* "));
            }
        }

        [Fact]
        public void OpenPet_stale_id_notices_and_reopens_root()
        {
            var (fixture, player, _, pet, window) = OpenPetWindow();
            using (fixture)
            {
                player.Pets.Remove(pet);

                window.LineClicked(1, 0, player, fixture.World);

                Assert.False(pet.IsAlive);
                Assert.Contains(player.Sent, s => s.StartsWith("NTF0,") && s.Contains("That pet is no longer yours."));
                Assert.StartsWith("Pets (", OptionListOf(player).Title);
            }
        }

        [Fact]
        public void OpenPet_back_reopens_root()
        {
            var (fixture, player, _, _, window) = OpenPetWindow();
            using (fixture)
            {
                window.Clicked(Window.ButtonTypes.Back, 0, 0, 0, player, fixture.World);

                Assert.StartsWith("Pets (", OptionListOf(player).Title);
            }
        }
    }
}
