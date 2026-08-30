using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class Part2PetsTests
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

        [Fact]
        public void PetList_lists_each_pet()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var rex = MakePet(fixture, player, 1, "Rex");
                rex.Level = 3;
                var fido = MakePet(fixture, player, 2, "Fido");

                Assert.True(fixture.RunCommand(player, "/petlist"));

                Assert.Contains(player.Sent, s => s.Contains("Listing Pets: <ID> <Name> <Level>"));
                Assert.Contains(player.Sent, s => s.Contains("1 Rex 3"));
                Assert.Contains(player.Sent, s => s.Contains("2 Fido 1"));
            }
        }

        [Fact]
        public void PetSpawn_spawns_matching_pet_on_map()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 5, "Rex");

                Assert.True(fixture.RunCommand(player, "/petspawn 5"));

                Assert.True(pet.IsAlive);
                Assert.Same(map, pet.Map);
                Assert.DoesNotContain(player.Sent, s => s.Contains("Couldn't find pet matching ID."));
            }
        }

        [Fact]
        public void PetSpawn_disabled_map_is_refused()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = false;
                MakePet(fixture, player, 5, "Rex");

                Assert.True(fixture.RunCommand(player, "/petspawn 5"));

                Assert.Contains(player.Sent, s => s.Contains("Pets are disabled in this map."));
            }
        }

        [Fact]
        public void PetSpawn_bad_id_sends_usage()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;

                Assert.True(fixture.RunCommand(player, "/petspawn abc"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /petspawn <id>"));
            }
        }

        [Fact]
        public void PetSpawn_extra_tokens_are_ignored()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 5, "Rex");

                Assert.True(fixture.RunCommand(player, "/petspawn 5 junk"));

                Assert.True(pet.IsAlive);
                Assert.Same(map, pet.Map);
            }
        }

        [Fact]
        public void PetInfo_opens_window_once_and_refreshes()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 5, "Rex");

                Assert.True(fixture.RunCommand(player, "/petinfo 5"));

                var windows = player.Windows.Where(w => w.Type == Window.WindowTypes.PetInfo).ToList();
                Assert.Single(windows);
                Assert.Same(pet, windows[0].Data);
                Assert.Equal("Pet Info For ID 5", windows[0].Title);

                Assert.True(fixture.RunCommand(player, "/petinfo 5"));

                Assert.Equal(1, player.Windows.Count(w => w.Type == Window.WindowTypes.PetInfo));
            }
        }

        [Fact]
        public void PetInfo_bad_id_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/petinfo abc"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /petinfo <id>"));
            }
        }

        [Fact]
        public void PetInfo_extra_tokens_are_ignored()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 5, "Rex");

                Assert.True(fixture.RunCommand(player, "/petinfo 5 junk"));

                var windows = player.Windows.Where(w => w.Type == Window.WindowTypes.PetInfo).ToList();
                Assert.Single(windows);
                Assert.Same(pet, windows[0].Data);
            }
        }

        [Fact]
        public void PetDamage_binds_both_arguments()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.PetDamageCost = 100;
                fixture.Settings.PetDamageBuyAmount = 5;
                fixture.Settings.IncreasePetDamageBuyCost = 10;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 250;

                Assert.True(fixture.RunCommand(player, "/petdamage 1 2"));

                Assert.Contains(player.Sent, s => s.Contains("Bought 10 damage for 200 experience."));
                Assert.Equal(10, pet.WeaponDamage);
                Assert.Equal(50, pet.Experience);
                Assert.Equal(200, pet.ExperienceSold);
            }
        }

        [Fact]
        public void PetDamage_defaults_buys_to_one()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.PetDamageCost = 100;
                fixture.Settings.PetDamageBuyAmount = 5;
                fixture.Settings.IncreasePetDamageBuyCost = 10;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 250;

                Assert.True(fixture.RunCommand(player, "/petdamage 1"));

                Assert.Contains(player.Sent, s => s.Contains("Bought 5 damage for 100 experience."));
                Assert.Equal(5, pet.WeaponDamage);
                Assert.Equal(150, pet.Experience);
            }
        }

        [Fact]
        public void PetDamage_bad_token_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakePet(fixture, player, 1, "Rex");

                Assert.True(fixture.RunCommand(player, "/petdamage abc"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /petdamage <petid> [buys]"));
            }
        }

        [Fact]
        public void PetDamage_nonpositive_literals_are_rejected()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakePet(fixture, player, 1, "Rex");

                Assert.True(fixture.RunCommand(player, "/petdamage 1 -1"));
                Assert.Contains(player.Sent, s => s.Contains("Invalid buy amount."));

                player.Sent.Clear();
                Assert.True(fixture.RunCommand(player, "/petdamage 0"));
                Assert.Contains(player.Sent, s => s.Contains("Invalid pet id."));
            }
        }

        [Fact]
        public void PetVita_buys_hit_points()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.PetVitaCost = 100;
                fixture.Settings.PetVitaBuyAmount = 10;
                fixture.Settings.IncreasePetVitaBuyCost = 10;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 250;

                Assert.True(fixture.RunCommand(player, "/petvita 1 2"));

                Assert.Contains(player.Sent, s => s.Contains("Bought 20 hp for 220 experience."));
                Assert.Equal(20, pet.BaseStats.HP);
                Assert.Equal(30, pet.Experience);
                Assert.Equal(220, pet.ExperienceSold);
            }
        }

        [Fact]
        public void PetVita_bad_token_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakePet(fixture, player, 1, "Rex");

                Assert.True(fixture.RunCommand(player, "/petvita abc"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /petvita <petid> [buys]"));
            }
        }

        [Fact]
        public void PetDelete_removes_pet_and_marks_deleted()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
                fixture.World.Database.Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "CREATE TABLE pets (pet_id INT PRIMARY KEY, owner_id INT NOT NULL);";
                    cmd.ExecuteNonQuery();
                });

                var pet = MakePet(fixture, player, 5, "Rex");
                pet.AutoCreatedNotSaved = false;

                Assert.True(fixture.RunCommand(player, "/petdelete 5"));

                Assert.Empty(player.Pets);
                Assert.True(pet.Delete);
            }
        }

        [Fact]
        public void PetDelete_bad_id_sends_usage()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                Assert.True(fixture.RunCommand(player, "/petdelete abc"));

                Assert.Contains(player.Sent, s => s.Contains("Usage: /petdelete <id>"));
            }
        }
    }
}
