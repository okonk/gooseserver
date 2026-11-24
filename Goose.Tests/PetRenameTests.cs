using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class PetRenameTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer owner, Map map) WorldAndOwner()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var owner = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            fixture.AddOnlinePlayer(owner);
            return (fixture, owner, map);
        }

        private static Pet MakePet(TestWorldFixture fixture, Player owner, string name)
        {
            var pet = new Pet
            {
                PetID = 1,
                Name = name,
                Owner = owner,
                Level = 1,
                BaseStats = new AttributeSet { HP = 100 },
                MaxStats = new AttributeSet { HP = 100 },
                Class = fixture.World.ClassHandler.GetClass(0)!,
            };
            owner.Pets.Add(pet);
            return pet;
        }

        private static void StartPetDatabase(TestWorldFixture fixture)
        {
            fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            fixture.World.Database.Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "pets.sql"));
                cmd.ExecuteNonQuery();
            });
        }

        [Fact]
        public void Rename_invalid_name_changes_nothing()
        {
            var (fixture, owner, _) = WorldAndOwner();
            using (fixture)
            {
                var pet = MakePet(fixture, owner, "Rex");

                var result = PetOperations.Rename(owner, pet, "ab", fixture.World);

                Assert.False(result.Ok);
                Assert.Equal("Pet names must be 3-16 letters, with single spaces between words.", result.Message);
                Assert.Equal("Rex", pet.Name);
                Assert.Empty(fixture.World.LogHandler.Pending);
            }
        }

        [Fact]
        public void Rename_live_pet_broadcasts_erase_and_remake_in_place()
        {
            var (fixture, owner, map) = WorldAndOwner();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, owner, "Rex");
                var bystander = fixture.CommandPlayerOn(map, 2, 2, "Bystander");
                fixture.AddOnlinePlayer(bystander);
                map.AddPlayer(bystander, fixture.World);
                StartPetDatabase(fixture);

                Assert.True(PetOperations.TrySpawn(owner, pet, fixture.World).Ok);
                pet.CurrentHP = 1;
                pet.Mode = Pet.Modes.Follow;

                int loginId = pet.LoginID;
                bystander.Sent.Clear();

                var result = PetOperations.Rename(owner, pet, "Barkley", fixture.World);

                Assert.True(result.Ok);
                Assert.Equal("Rex is now known as Barkley.", result.Message);
                Assert.Equal("Barkley", pet.Name);
                Assert.Equal(loginId, pet.LoginID);
                Assert.Equal(1, pet.CurrentHP);
                Assert.Equal(Pet.Modes.Follow, pet.Mode);
                Assert.True(pet.IsAlive);

                string buf = string.Join("\n", bystander.Sent);
                int ercIndex = buf.IndexOf($"ERC{loginId}", StringComparison.Ordinal);
                Assert.True(ercIndex >= 0, "bystander never received the erase packet");
                int mkcIndex = buf.IndexOf($"MKC{loginId},13,Barkley,", StringComparison.Ordinal);
                Assert.True(mkcIndex > ercIndex, "bystander never received the remake packet after the erase");
            }
        }

        [Fact]
        public void Rename_refreshes_open_petinfo_window()
        {
            var (fixture, owner, map) = WorldAndOwner();
            using (fixture)
            {
                map.CanSpawnPets = true;
                var pet = MakePet(fixture, owner, "Rex");
                PetOperations.ShowInfo(owner, pet, fixture.World);
                StartPetDatabase(fixture);
                owner.Sent.Clear();

                Assert.True(PetOperations.Rename(owner, pet, "Barkley", fixture.World).Ok);

                Assert.Contains(owner.Sent, s => s.Contains("Name: Barkley"));
            }
        }

        [Fact]
        public void Rename_writes_petrename_log()
        {
            var (fixture, owner, _) = WorldAndOwner();
            using (fixture)
            {
                var pet = MakePet(fixture, owner, "Rex");
                StartPetDatabase(fixture);

                Assert.True(PetOperations.Rename(owner, pet, "Barkley", fixture.World).Ok);

                var log = Assert.Single(fixture.World.LogHandler.Pending, l => l.Type == Log.Types.PetRename);
                Assert.Equal($"pet {pet.PetID}: Rex -> Barkley", log.Text);
                Assert.Equal(owner.PlayerID, log.PlayerID);
            }
        }
    }
}
