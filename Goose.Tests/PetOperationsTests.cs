using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class PetOperationsTests
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
        public void Find_returns_matching_pet()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var rex = MakePet(fixture, player, 1, "Rex");
                MakePet(fixture, player, 2, "Fido");

                Assert.Same(rex, PetOperations.Find(player, 1));
            }
        }

        [Fact]
        public void Find_returns_null_when_absent()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                MakePet(fixture, player, 1, "Rex");

                Assert.Null(PetOperations.Find(player, 99));
            }
        }

        [Fact]
        public void ActiveCount_counts_only_alive_pets()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var alive = MakePet(fixture, player, 1, "Rex");
                var dead = MakePet(fixture, player, 2, "Fido");

                Assert.Equal(0, PetOperations.ActiveCount(player));

                alive.Spawn(fixture.World);

                Assert.Equal(1, PetOperations.ActiveCount(player));
                Assert.False(dead.IsAlive);
            }
        }

        [Fact]
        public void TrySpawn_disabled_map_is_refused()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = false;
                var pet = MakePet(fixture, player, 1, "Rex");

                var r = PetOperations.TrySpawn(player, pet, fixture.World);

                Assert.False(r.Ok);
                Assert.Equal("Pets are disabled in this map.", r.Message);
                Assert.False(pet.IsAlive);
            }
        }

        [Fact]
        public void TrySpawn_already_out_is_refused()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Spawn(fixture.World);

                var r = PetOperations.TrySpawn(player, pet, fixture.World);

                Assert.False(r.Ok);
                Assert.Equal("That pet is already spawned.", r.Message);
            }
        }

        [Fact]
        public void TrySpawn_cooldown_is_refused_with_wait_message()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");
                long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120;
                pet.NextRespawnTime = expiry;

                var r = PetOperations.TrySpawn(player, pet, fixture.World);

                Assert.False(r.Ok);
                Assert.Contains("seconds to spawn this pet.", r.Message);
                Assert.StartsWith("You must wait ", r.Message);
                Assert.False(pet.IsAlive);
            }
        }

        [Fact]
        public void TrySpawn_at_active_limit_is_refused()
        {
            var fixture = new TestWorldFixture(s => s.ActivePetLimit = 2);
            var map = fixture.AddBaseMap(1, "Test");
            map.CanSpawnPets = true;
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            using (fixture)
            {
                var first = MakePet(fixture, player, 1, "Rex");
                var second = MakePet(fixture, player, 2, "Fido");
                var third = MakePet(fixture, player, 3, "Buddy");

                Assert.True(PetOperations.TrySpawn(player, first, fixture.World).Ok);
                Assert.True(PetOperations.TrySpawn(player, second, fixture.World).Ok);

                var r = PetOperations.TrySpawn(player, third, fixture.World);

                Assert.False(r.Ok);
                Assert.Contains("2 pets", r.Message);
                Assert.False(third.IsAlive);
                Assert.True(first.IsAlive);
                Assert.True(second.IsAlive);
            }
        }

        [Fact]
        public void TrySpawn_dead_pets_do_not_count_toward_limit()
        {
            var fixture = new TestWorldFixture(s => s.ActivePetLimit = 2);
            var map = fixture.AddBaseMap(1, "Test");
            map.CanSpawnPets = true;
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            using (fixture)
            {
                var first = MakePet(fixture, player, 1, "Rex");
                for (int i = 2; i <= 5; i++)
                    MakePet(fixture, player, i, "Pet" + i);

                Assert.True(PetOperations.TrySpawn(player, first, fixture.World).Ok);

                var r = PetOperations.TrySpawn(player, player.Pets[1], fixture.World);

                Assert.True(r.Ok);
                Assert.True(player.Pets[1].IsAlive);
                Assert.Equal(2, PetOperations.ActiveCount(player));
            }
        }

        [Fact]
        public void TrySpawn_success_spawns_pet_on_owner_map()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");

                var r = PetOperations.TrySpawn(player, pet, fixture.World);

                Assert.True(r.Ok);
                Assert.Equal("Rex has been summoned.", r.Message);
                Assert.True(pet.IsAlive);
                Assert.Same(map, pet.Map);
            }
        }

        [Fact]
        public void TryDespawn_alive_pet_destroys_and_sets_cooldown()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
                fixture.World.Database.Execute(conn =>
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "pets.sql"));
                    cmd.ExecuteNonQuery();
                });

                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Spawn(fixture.World);
                long before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var r = PetOperations.TryDespawn(pet, fixture.World);

                Assert.True(r.Ok);
                Assert.Equal("Rex has been dismissed.", r.Message);
                Assert.False(pet.IsAlive);
                Assert.InRange(pet.NextRespawnTime, before + 118, before + 122);
            }
        }

        [Fact]
        public void TryDespawn_not_alive_is_refused_and_leaves_cooldown_unchanged()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.NextRespawnTime = 0;

                var r = PetOperations.TryDespawn(pet, fixture.World);

                Assert.False(r.Ok);
                Assert.Equal("Rex isn't out.", r.Message);
                Assert.Equal(0, pet.NextRespawnTime);
            }
        }

        [Fact]
        public void SetMode_not_alive_is_refused_and_mode_unchanged()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Mode = Pet.Modes.Defend;

                var r = PetOperations.SetMode(pet, Pet.Modes.Follow);

                Assert.False(r.Ok);
                Assert.Equal("Rex isn't out.", r.Message);
                Assert.Equal(Pet.Modes.Defend, pet.Mode);
            }
        }

        [Fact]
        public void SetMode_alive_clears_target_and_sets_mode()
        {
            var (fixture, player, map) = WorldAndPlayer();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Spawn(fixture.World);
                pet.Mode = Pet.Modes.Defend;
                pet.Target = player;

                var r = PetOperations.SetMode(pet, Pet.Modes.Follow);

                Assert.True(r.Ok);
                Assert.Equal("Rex is now in Follow mode.", r.Message);
                Assert.Equal(Pet.Modes.Follow, pet.Mode);
                Assert.Null(pet.Target);
            }
        }

        [Fact]
        public void SetMode_attack_is_a_programmer_error()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 1, "Rex");

                Assert.Throws<ArgumentOutOfRangeException>(() => PetOperations.SetMode(pet, Pet.Modes.Attack));
            }
        }

        [Fact]
        public void PetSpawn_command_at_active_limit_sends_limit_message()
        {
            var fixture = new TestWorldFixture(s => s.ActivePetLimit = 2);
            var map = fixture.AddBaseMap(1, "Test");
            map.CanSpawnPets = true;
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            using (fixture)
            {
                var first = MakePet(fixture, player, 1, "Rex");
                var second = MakePet(fixture, player, 2, "Fido");
                var third = MakePet(fixture, player, 3, "Buddy");
                first.Spawn(fixture.World);
                second.Spawn(fixture.World);

                Assert.True(fixture.RunCommand(player, "/petspawn 3"));

                Assert.Contains(player.Sent, s => s.Contains("You can only have 2 pets out at once."));
                Assert.False(third.IsAlive);
            }
        }
    }
}
