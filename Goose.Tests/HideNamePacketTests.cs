using Goose;
using Xunit;

namespace Goose.Tests
{
    public class HideNamePacketTests
    {
        // FaceID is 70 in the helpers; Invisible=0. HideName sits between them: ,<invis>,<hide>,70,
        [Fact]
        public void MakeNPCCharacter_HideName1_PinsFieldAfterInvisible()
        {
            var npc = new NPC { LoginID = 5, Name = "NPC", MaxStats = new AttributeSet { HP = 100 },
                                HairA = 255, FaceID = 70, HideName = true };
            npc.CurrentHP = 100;
            Assert.Contains(",0,1,70,", P.MakeNPCCharacter(npc));   // invis=0, hide=1, face=70
        }

        [Fact]
        public void MakeNPCCharacter_HideName0_PinsFieldAfterInvisible()
        {
            var npc = new NPC { LoginID = 5, Name = "NPC", MaxStats = new AttributeSet { HP = 100 },
                                HairA = 255, FaceID = 70, HideName = false };
            npc.CurrentHP = 100;
            Assert.Contains(",0,0,70,", P.MakeNPCCharacter(npc));
        }

        [Fact]
        public void MakeCharacter_PlayersAlwaysEmitHideName0()
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
            Assert.Contains(",0,0,70,", P.MakeCharacter(p));
        }
    }
}
