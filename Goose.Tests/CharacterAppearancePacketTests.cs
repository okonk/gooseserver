using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests
{
    public class CharacterAppearancePacketTests
    {
        private static (TestWorldFixture World, Player Player) NewPlayer(int bodyId)
        {
            var world = new TestWorldFixture();
            var map = world.AddBaseMap(1, "Town");
            var player = world.CommandPlayerOn(map, 1, 2, "Hax");
            player.LoginID = 7;
            player.Title = "T";
            player.Surname = "S";
            player.Facing = Direction.Right;
            player.MaxStats = new AttributeSet { HP = 100 };
            player.CurrentHP = 50;
            player.CurrentBodyID = bodyId;
            player.BodyR = 10;
            player.BodyG = 20;
            player.BodyB = 30;
            player.BodyA = 40;
            player.BodyState = 1;
            player.HairID = 11;
            player.HairR = 5;
            player.HairG = 6;
            player.HairB = 7;
            player.HairA = 8;
            player.FaceID = 70;
            player.InvisibleBuffCount = 1;
            player.Access = Player.AccessStatus.GameMaster;

            var weaponTemplate = world.AddBaseItemTemplate(200, "Sword", ItemTemplate.UseTypes.Weapon, t =>
            {
                t.GraphicEquipped = 101;
                t.GraphicR = 1;
                t.GraphicG = 2;
                t.GraphicB = 3;
                t.GraphicA = 4;
                t.BodyState = 4;
            });
            var weapon = new Item();
            weapon.LoadFromTemplate(weaponTemplate);
            world.World.ItemHandler.AddAndAssignId(weapon, world.World);
            Assert.True(player.Inventory.AddItem(weapon, 1, world.World));
            Assert.True(player.Inventory.Equip(weapon, world.World));

            var mountTemplate = world.AddBaseItemTemplate(201, "Mount", ItemTemplate.UseTypes.NoUse, t =>
            {
                t.Slot = ItemTemplate.ItemSlots.Mount;
                t.GraphicEquipped = 202;
                t.GraphicR = 5;
                t.GraphicG = 6;
                t.GraphicB = 7;
                t.GraphicA = 8;
            });
            var mount = new Item();
            mount.LoadFromTemplate(mountTemplate);
            world.World.ItemHandler.AddAndAssignId(mount, world.World);
            Assert.True(player.Inventory.AddItem(mount, 1, world.World));
            Assert.True(player.Inventory.Equip(mount, world.World));

            return (world, player);
        }

        private const string LayeredEquipped = "0,*,0,*,0,*,0,*,0,*,101,1,2,3,4,";
        private const string LayeredHairColor = "5,6,7,8,";
        private const string LayeredMount = "202,5,6,7,8,";

        [Fact]
        public void MakeCharacter_layered_body_sends_full_shape_with_weapon_pose()
        {
            var (world, player) = NewPlayer(10001);
            using (world)
            {
                Assert.Equal(
                    "MKC7,1,Hax,T,S,,1,2,2,50,10001,10,20,30,40,4,11," +
                    LayeredEquipped + LayeredHairColor + "1,70,0,1," + LayeredMount,
                    P.MakeCharacter(player));
            }
        }

        [Fact]
        public void MakeCharacter_compact_body_forces_pose_3_and_omits_layered_fields()
        {
            var (world, player) = NewPlayer(10100);
            using (world)
            {
                Assert.Equal("MKC7,1,Hax,T,S,,1,2,2,50,10100,10,20,30,40,3,1,0,1,", P.MakeCharacter(player));
            }
        }

        [Fact]
        public void UpdateCharacter_layered_body_sends_full_shape_with_weapon_pose()
        {
            var (world, player) = NewPlayer(10001);
            using (world)
            {
                Assert.Equal(
                    "CHP7,10001,10,20,30,40,4,11," +
                    LayeredEquipped + LayeredHairColor + "1,70,0," + LayeredMount,
                    P.UpdateCharacter(player));
            }
        }

        [Fact]
        public void UpdateCharacter_compact_body_forces_pose_3_and_omits_layered_fields()
        {
            var (world, player) = NewPlayer(10100);
            using (world)
            {
                Assert.Equal("CHP7,10100,10,20,30,40,3,1,0,", P.UpdateCharacter(player));
            }
        }

        private static NPC NewNPC(int bodyId)
        {
            var npc = new NPC
            {
                LoginID = 5,
                NPCType = NPCTemplate.Types.Monster,
                Name = "Slime",
                Title = "T",
                Surname = "S",
                MapX = 3,
                MapY = 4,
                Facing = Direction.Down,
                MaxStats = new AttributeSet { HP = 200 },
                CurrentBodyID = bodyId,
                BodyR = 10,
                BodyG = 20,
                BodyB = 30,
                BodyA = 40,
                BodyState = 5,
                HairID = 11,
                EquippedItems = "7,1,2,3,4",
                HairR = 5,
                HairG = 6,
                HairB = 7,
                HairA = 8,
                InvisibleBuffCount = 1,
                FaceID = 71,
            };
            npc.CurrentHP = 100;
            return npc;
        }

        [Fact]
        public void MakeNPCCharacter_layered_body_sends_full_shape()
        {
            var npc = NewNPC(10001);

            Assert.Equal(
                "MKC5,2,Slime,T,S,,3,4,3,50,10001,10,20,30,40,5,11,7,1,2,3,4,5,6,7,8,1,71,320,0,0,0,0,0,0",
                P.MakeNPCCharacter(npc));
        }

        [Fact]
        public void MakeNPCCharacter_compact_body_forces_pose_3_and_omits_layered_fields()
        {
            var npc = NewNPC(10100);

            Assert.Equal("MKC5,2,Slime,T,S,,3,4,3,50,10100,10,20,30,40,3,1,320,0,", P.MakeNPCCharacter(npc));
        }

        [Fact]
        public void UpdateNPC_layered_body_sends_full_shape()
        {
            var npc = NewNPC(10001);

            Assert.Equal(
                "CHP5,10001,10,20,30,40,5,11,7,1,2,3,4,5,6,7,8,1,71,320,0,0,0,0,0",
                P.UpdateNPC(npc));
        }

        [Fact]
        public void UpdateNPC_compact_body_forces_pose_3_and_omits_layered_fields()
        {
            var npc = NewNPC(10100);

            Assert.Equal("CHP5,10100,10,20,30,40,3,1,320,", P.UpdateNPC(npc));
        }

        private static Pet NewPet(int bodyId)
        {
            var pet = new Pet
            {
                LoginID = 9,
                Name = "Pet",
                Title = "T",
                Surname = "S",
                MapX = 5,
                MapY = 6,
                Facing = Direction.Left,
                MaxStats = new AttributeSet { HP = 40 },
                CurrentBodyID = bodyId,
                BodyR = 10,
                BodyG = 20,
                BodyB = 30,
                BodyA = 40,
                BodyState = 6,
                HairID = 12,
                EquippedItems = "9,*",
                HairR = 5,
                HairG = 6,
                HairB = 7,
                HairA = 8,
                InvisibleBuffCount = 1,
                FaceID = 72,
            };
            pet.CurrentHP = 20;
            return pet;
        }

        [Fact]
        public void MakePetCharacter_layered_body_sends_full_shape()
        {
            var pet = NewPet(10001);

            Assert.Equal(
                "MKC9,13,Pet,T,S,,5,6,4,50,10001,10,20,30,40,6,12,9,*,5,6,7,8,1,72,320,0,0,0,0,0,0",
                P.MakePetCharacter(pet));
        }

        [Fact]
        public void MakePetCharacter_compact_body_forces_pose_3_and_omits_layered_fields()
        {
            var pet = NewPet(10100);

            Assert.Equal("MKC9,13,Pet,T,S,,5,6,4,50,10100,10,20,30,40,3,1,320,0,", P.MakePetCharacter(pet));
        }

        [Fact]
        public void UpdatePet_layered_body_sends_full_shape()
        {
            var pet = NewPet(10001);

            Assert.Equal(
                "CHP9,10001,10,20,30,40,6,12,9,*,5,6,7,8,1,72,320,0,0,0,0,0",
                P.UpdatePet(pet));
        }

        [Fact]
        public void UpdatePet_compact_body_forces_pose_3_and_omits_layered_fields()
        {
            var pet = NewPet(10100);

            Assert.Equal("CHP9,10100,10,20,30,40,3,1,320,", P.UpdatePet(pet));
        }

        [Theory]
        [InlineData(10001, "CHP0,10001,0,0,0,0,0,0,0,*,0,*,0,*,0,*,0,*,0,*,0,0,0,0,0,0,777,0,*\x1")]
        [InlineData(10100, "CHP0,10100,0,0,0,0,3,0,777,\x1")]
        public void GmHax_uses_matching_body_shape(int bodyId, string expected)
        {
            var fixture = new TestWorldFixture();
            var map = fixture.AddBaseMap(1, "Test");
            var gm = fixture.CommandPlayerOn(map, 1, 2, "Tester");
            gm.Access = Player.AccessStatus.GameMaster;
            gm.CurrentBodyID = bodyId;

            using (fixture)
            {
                Assert.True(fixture.RunCommand(gm, "/gmhax 777"));

                Assert.Contains(gm.Sent, s => s == expected);
            }
        }
    }
}
