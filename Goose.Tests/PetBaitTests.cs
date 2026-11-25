using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class PetBaitTests
    {
        private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Item bait) WorldWithBait()
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var player = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            var template = fixture.AddBaseItemTemplate(541, "Pet Bait", ItemTemplate.UseTypes.OneTime, t =>
            {
                t.StackSize = 10;
                t.SpellEffectChance = 100;
                t.SpellEffect = new SpellEffect { EffectType = SpellEffect.EffectTypes.PetTame };
            });
            var bait = new Item();
            bait.LoadFromTemplate(template);
            player.Inventory.AddItem(bait, 1, fixture.World);
            return (fixture, player, bait);
        }

        private static void AddPet(TestWorldFixture fixture, Player owner, int id)
        {
            owner.Pets.Add(new Pet
            {
                PetID = id,
                Name = "Pet" + id,
                Owner = owner,
                Level = 1,
                BaseStats = new AttributeSet(),
                MaxStats = new AttributeSet(),
                Class = fixture.World.ClassHandler.GetClass(0)!,
            });
        }

        [Fact]
        public void Bait_is_kept_when_pets_are_at_max()
        {
            var (fixture, player, _) = WorldWithBait();
            using (fixture)
            {
                fixture.Settings.PetCountLimit = 2;
                AddPet(fixture, player, 1);
                AddPet(fixture, player, 2);

                player.Inventory.UseConsumable(player.Inventory.GetSlot(1)!.Item, fixture.World);

                Assert.True(player.Inventory.HasItem(541));
                Assert.Contains(player.Sent, s => s.Contains("maximum amount of pets"));
            }
        }

        [Fact]
        public void Bait_is_used_when_pets_are_below_max()
        {
            var (fixture, player, _) = WorldWithBait();
            using (fixture)
            {
                fixture.Settings.PetCountLimit = 2;
                AddPet(fixture, player, 1);

                player.Inventory.UseConsumable(player.Inventory.GetSlot(1)!.Item, fixture.World);

                Assert.False(player.Inventory.HasItem(541));
            }
        }
    }
}
