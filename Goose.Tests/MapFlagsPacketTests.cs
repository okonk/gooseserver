using Goose;
using Xunit;

namespace Goose.Tests
{
    public class MapFlagsPacketTests
    {
        [Fact]
        public void SendMapFlags_NormalMap_SendsAllEnabledDefaults()
        {
            var map = new Map { CanPVP = false, CanUseItems = true, CanCast = true };
            Assert.Equal("MFL0,1,1", P.SendMapFlags(map));
        }

        [Fact]
        public void SendMapFlags_PvpArena_SendsPvpEnabled()
        {
            var map = new Map { CanPVP = true, CanUseItems = true, CanCast = true };
            Assert.Equal("MFL1,1,1", P.SendMapFlags(map));
        }

        [Fact]
        public void SendMapFlags_RestrictedMap_SendsItemsAndCastDisabled()
        {
            var map = new Map { CanPVP = false, CanUseItems = false, CanCast = false };
            Assert.Equal("MFL0,0,0", P.SendMapFlags(map));
        }

        [Fact]
        public void MakePetCharacter_ReportsPetType()
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

            Assert.StartsWith("MKC9,13,", P.MakePetCharacter(pet));
        }
    }
}
