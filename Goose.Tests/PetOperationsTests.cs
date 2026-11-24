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

        [Fact]
        public void IsMaxLevel_true_when_level_experience_is_zero()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 1, "Rex");

                Assert.True(PetOperations.IsMaxLevel(pet));
            }
        }

        [Fact]
        public void IsMaxLevel_false_when_level_experience_is_nonzero()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.World.ClassHandler.GetClass(0)!.GetLevel(1)!.Experience = 100;
                var pet = MakePet(fixture, player, 1, "Rex");

                Assert.False(PetOperations.IsMaxLevel(pet));
            }
        }

        [Fact]
        public void IsMaxLevel_false_when_level_is_missing()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Level = 999;

                Assert.False(PetOperations.IsMaxLevel(pet));
            }
        }

        [Fact]
        public void MaxAffordableVita_agrees_with_buy_loop_around_cost_boundaries()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.PetVitaCost = 100;
                fixture.Settings.PetVitaBuyAmount = 10;
                fixture.Settings.IncreasePetVitaBuyCost = 10;

                foreach (long exp in new[] { 99L, 100, 119, 359, 1000 })
                {
                    var pet = MakePet(fixture, player, 1, "Rex");
                    pet.Experience = exp;

                    long max = PetOperations.MaxAffordableVita(pet, fixture.Settings);

                    long simExp = exp, simHp = 0, simCost = 0, simCount = 0;
                    while (true)
                    {
                        long cost = Utils.MultiplyAndTruncate(100, (simHp / 10) * 0.2 + 1);
                        if (simExp < cost) break;
                        simExp -= cost;
                        simHp += 10;
                        simCost += cost;
                        simCount++;
                    }

                    Assert.Equal(simCount, max);

                    var r = PetOperations.BuyVita(pet, max, fixture.World);
                    if (max == 0)
                    {
                        Assert.False(r.Ok);
                        Assert.Equal("Rex doesn't have enough experience.", r.Message);
                        Assert.Equal(exp, pet.Experience);
                        Assert.Equal(0, pet.BaseStats.HP);
                    }
                    else
                    {
                        Assert.True(r.Ok);
                        Assert.Equal("Bought " + max * 10 + " hp for " + simCost + " experience.", r.Message);
                        Assert.Equal(simExp, pet.Experience);
                        Assert.Equal(simHp, pet.BaseStats.HP);
                        Assert.True(pet.Experience < PetOperations.NextVitaCost(pet, fixture.Settings));
                    }
                }
            }
        }

        [Fact]
        public void MaxAffordableDamage_agrees_with_buy_loop_around_cost_boundaries()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.PetDamageCost = 100;
                fixture.Settings.PetDamageBuyAmount = 5;
                fixture.Settings.IncreasePetDamageBuyCost = 10;

                foreach (long exp in new[] { 99L, 100, 109, 219, 1000 })
                {
                    var pet = MakePet(fixture, player, 1, "Rex");
                    pet.Experience = exp;

                    long max = PetOperations.MaxAffordableDamage(pet, fixture.Settings);

                    long simExp = exp, simDmg = 0, simCost = 0, simCount = 0;
                    while (true)
                    {
                        long cost = Utils.MultiplyAndTruncate(100, (simDmg / 10) * 0.2 + 1);
                        if (simExp < cost) break;
                        simExp -= cost;
                        simDmg += 5;
                        simCost += cost;
                        simCount++;
                    }

                    Assert.Equal(simCount, max);

                    var r = PetOperations.BuyDamage(pet, max, fixture.World);
                    if (max == 0)
                    {
                        Assert.False(r.Ok);
                        Assert.Equal("Rex doesn't have enough experience.", r.Message);
                        Assert.Equal(exp, pet.Experience);
                        Assert.Equal(0, pet.WeaponDamage);
                    }
                    else
                    {
                        Assert.True(r.Ok);
                        Assert.Equal("Bought " + max * 5 + " damage for " + simCost + " experience.", r.Message);
                        Assert.Equal(simExp, pet.Experience);
                        Assert.Equal(simDmg, pet.WeaponDamage);
                        Assert.True(pet.Experience < PetOperations.NextDamageCost(pet, fixture.Settings));
                    }
                }
            }
        }

        [Fact]
        public void NotMaxLevel_zero_affordable_and_buys_refused()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.World.ClassHandler.GetClass(0)!.GetLevel(1)!.Experience = 100;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 5000;

                Assert.Equal(0, PetOperations.MaxAffordableVita(pet, fixture.Settings));
                Assert.Equal(0, PetOperations.MaxAffordableDamage(pet, fixture.Settings));

                var v = PetOperations.BuyVita(pet, 5, fixture.World);
                Assert.False(v.Ok);
                Assert.Equal("Rex must be max level to buy stats.", v.Message);

                var d = PetOperations.BuyDamage(pet, 5, fixture.World);
                Assert.False(d.Ok);
                Assert.Equal("Rex must be max level to buy stats.", d.Message);

                Assert.Equal(5000, pet.Experience);
                Assert.Equal(0, pet.BaseStats.HP);
                Assert.Equal(0, pet.WeaponDamage);
            }
        }

        [Fact]
        public void ZeroVitaCost_hits_max_affordable_cap()
        {
            var fixture = new TestWorldFixture(s => s.PetVitaCost = 0);
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            using (fixture)
            {
                fixture.Settings.IncreasePetVitaBuyCost = 10;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 1000;

                Assert.Equal(PetOperations.MaxAffordableBuyCap, PetOperations.MaxAffordableVita(pet, fixture.Settings));
            }
        }

        [Fact]
        public void ZeroDamageCost_hits_max_affordable_cap()
        {
            var fixture = new TestWorldFixture(s => s.PetDamageCost = 0);
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            using (fixture)
            {
                fixture.Settings.IncreasePetDamageBuyCost = 10;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 1000;

                Assert.Equal(PetOperations.MaxAffordableBuyCap, PetOperations.MaxAffordableDamage(pet, fixture.Settings));
            }
        }

        [Fact]
        public void PetVita_command_sends_max_level_message()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.World.ClassHandler.GetClass(0)!.GetLevel(1)!.Experience = 100;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 500;

                Assert.True(fixture.RunCommand(player, "/petvita 1"));

                Assert.Contains(player.Sent, s => s.Contains("Rex must be max level to buy stats."));
                Assert.Equal(500, pet.Experience);
            }
        }

        [Fact]
        public void PetVita_command_sends_not_enough_experience_message()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.Settings.PetVitaCost = 100;
                fixture.Settings.IncreasePetVitaBuyCost = 10;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 50;

                Assert.True(fixture.RunCommand(player, "/petvita 1"));

                Assert.Contains(player.Sent, s => s.Contains("Rex doesn't have enough experience."));
                Assert.Equal(50, pet.Experience);
            }
        }

        [Fact]
        public void PetDamage_command_sends_max_level_message()
        {
            var (fixture, player, _) = WorldAndPlayer();
            using (fixture)
            {
                fixture.World.ClassHandler.GetClass(0)!.GetLevel(1)!.Experience = 100;
                var pet = MakePet(fixture, player, 1, "Rex");
                pet.Experience = 500;

                Assert.True(fixture.RunCommand(player, "/petdamage 1"));

                Assert.Contains(player.Sent, s => s.Contains("Rex must be max level to buy stats."));
                Assert.Equal(500, pet.Experience);
            }
        }
    }
}
