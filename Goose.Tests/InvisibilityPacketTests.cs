using Goose;
using Xunit;

namespace Goose.Tests
{
    public class InvisibilityPacketTests
    {
        private const string InvisField = ",0,70,";

        [Fact]
        public void SeeInvisible_WhenCanSee_ProducesSINVS1()
        {
            Assert.Equal("SINVS1", P.SeeInvisible(true));
        }

        [Fact]
        public void SeeInvisible_WhenCannotSee_ProducesSINVS0()
        {
            Assert.Equal("SINVS0", P.SeeInvisible(false));
        }

        private static Player NewPlayer()
        {
            var p = new Player(0);
            p.Inventory = new Inventory(p, new GooseSettings
            {
                InventorySize = 30, EquippedSize = 14, CombineBagSize = 10,
            });
            var klass = new Class { ClassID = 1, ClassName = "Test" };
            klass.AddLevel(new ClassLevel { Level = 1, ClassID = 1, BaseStats = new AttributeSet() });
            p.Class = klass;
            p.BaseStats = new AttributeSet { HP = 100, MP = 100 };
            p.MaxStats = p.BaseStats + new AttributeSet();
            p.CurrentHP = 100;
            p.CurrentMP = 100;
            p.State = Player.States.Ready;
            p.HairA = 255;
            p.FaceID = 70;
            return p;
        }

        [Fact]
        public void MakeCharacter_PinsInvisFieldBeforeFaceID()
        {
            var packet = P.MakeCharacter(NewPlayer());

            Assert.StartsWith("MKC", packet);
            Assert.Contains(InvisField, packet);
            Assert.Contains(",255,0,70,", packet);
        }

        [Fact]
        public void UpdateCharacter_PinsInvisFieldBeforeFaceID()
        {
            var packet = P.UpdateCharacter(NewPlayer());

            Assert.StartsWith("CHP", packet);
            Assert.Contains(InvisField, packet);
        }

        private static Pet NewPet()
        {
            var pet = new Pet
            {
                LoginID = 9,
                Name = "Pet",
                MaxStats = new AttributeSet { HP = 100 },
                HairA = 255,
                FaceID = 70
            };
            pet.CurrentHP = 100;
            return pet;
        }

        [Fact]
        public void MakePetCharacter_PinsInvisFieldBeforeFaceID()
        {
            var packet = P.MakePetCharacter(NewPet());

            Assert.StartsWith("MKC", packet);
            Assert.Contains(InvisField, packet);
        }

        [Fact]
        public void UpdatePet_PinsInvisFieldBeforeFaceID()
        {
            var packet = P.UpdatePet(NewPet());

            Assert.StartsWith("CHP", packet);
            Assert.Contains(InvisField, packet);
        }

        private static NPC NewNPC()
        {
            var npc = new NPC
            {
                LoginID = 5,
                Name = "NPC",
                MaxStats = new AttributeSet { HP = 100 },
                HairA = 255,
                FaceID = 70
            };
            npc.CurrentHP = 100;
            return npc;
        }

        [Fact]
        public void MakeNPCCharacter_PinsInvisFieldBeforeFaceID()
        {
            var packet = P.MakeNPCCharacter(NewNPC());

            Assert.StartsWith("MKC", packet);
            Assert.Contains(InvisField, packet);
        }

        [Fact]
        public void UpdateNPC_PinsInvisFieldBeforeFaceID()
        {
            var packet = P.UpdateNPC(NewNPC());

            Assert.StartsWith("CHP", packet);
            Assert.Contains(InvisField, packet);
        }
    }
}
