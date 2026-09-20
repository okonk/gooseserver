using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Goose;
using Xunit;

namespace Goose.Tests
{
    public class SpellCooldownPacketTests
    {
        private static Socket NewUnconnectedSocket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };
        }

        private static (Player p, GameWorld world) NewCaster()
        {
            var world = new GameWorld(new GooseSettings { SpellbookSize = 90 });
            var p = new Player(0);
            p.OnLogin();
            p.Sock = NewUnconnectedSocket();
            p.Class = new Class { ClassID = 1, ClassName = "Test" };
            p.Spellbook = new Spellbook(p, world.Settings);
            p.Map = new Map();
            p.BaseStats = new AttributeSet { HP = 100, MP = 100 };
            p.MaxStats = p.BaseStats + new AttributeSet();
            p.CurrentHP = 100;
            p.CurrentMP = 100;
            return (p, world);
        }

        private static Spell NewSpell()
        {
            return new Spell
            {
                ID = 1,
                Name = "Test",
                Aether = 10000,
                Target = Spell.SpellTargets.Self,
                SpellEffect = new SpellEffect(),
            };
        }

        private static long? CdrRemainingMs(Player p)
        {
            var payload = Encoding.Latin1.GetString(p.SendBuffer.ToArray());
            var packet = payload.Split('\x1').FirstOrDefault(s => s.StartsWith("CDR"));
            if (packet is null) return null;

            return long.Parse(packet.Substring(3).Split(',')[1]);
        }

        private static long? LastSssRemainingMs(Player p)
        {
            var payload = Encoding.Latin1.GetString(p.SendBuffer.ToArray());
            var packet = payload.Split('\x1').LastOrDefault(s => s.StartsWith("SSS"));
            if (packet is null) return null;

            return long.Parse(packet.Substring(3).Split(',')[^1]);
        }

        [Fact]
        public void SpellCooldownRemaining_FormatsSlotAndRemainingMs()
        {
            Assert.Equal("CDR3,1500", P.SpellCooldownRemaining(3, 1500));
            Assert.Equal("CDR1,0", P.SpellCooldownRemaining(1, 0));
        }

        [Fact]
        public void SpellSlot_SendsFullCooldownEvenOverOneHour()
        {
            var spell = NewSpell();
            spell.Aether = (long)TimeSpan.FromHours(2).TotalMilliseconds;

            var packet = P.SpellSlot(spell, 1, 0, 0);

            Assert.EndsWith("," + spell.Aether + ",0", packet);
        }

        [Fact]
        public void SpellSlot_AppendsRemainingMs()
        {
            var packet = P.SpellSlot(NewSpell(), 1, 0, 1500);

            Assert.EndsWith(",10000,1500", packet);
        }

        [Fact]
        public void SendSlot_NeverCast_SendsZeroRemaining()
        {
            var (p, world) = NewCaster();
            p.Spellbook.AddSpell(NewSpell(), world);

            p.Spellbook.SendSlot(1, world);

            Assert.Equal(0, LastSssRemainingMs(p));
        }

        [Fact]
        public void SendSlot_WhileOnCooldown_SendsRemainingNotTotal()
        {
            var (p, world) = NewCaster();
            p.Spellbook.AddSpell(NewSpell(), world);
            p.Spellbook.SetSlotLastCast(1, world.TimeNow - (long)(4.0 * world.TimerFrequency));

            p.Spellbook.SendSlot(1, world);

            var remaining = LastSssRemainingMs(p);
            Assert.NotNull(remaining);
            Assert.InRange(remaining!.Value, 5950, 6000);
        }

        [Fact]
        public void CastSpell_WhenOnCooldown_SendsRemainingCooldown()
        {
            var (p, world) = NewCaster();
            p.Spellbook.AddSpell(NewSpell(), world);

            p.Spellbook.SetSlotLastCast(1, world.TimeNow - (long)(4.0 * world.TimerFrequency));

            p.CastSpell(1, p, world);

            var remaining = CdrRemainingMs(p);
            Assert.NotNull(remaining);
            Assert.InRange(remaining!.Value, 5950, 6000);
        }

        [Fact]
        public void CastSpell_WhenFizzles_SendsZeroRemainingCooldown()
        {
            var (p, world) = NewCaster();
            var spell = NewSpell();
            spell.MPStaticCost = 50;
            p.Spellbook.AddSpell(spell, world);
            p.CurrentMP = 0;

            p.CastSpell(1, p, world);

            var payload = Encoding.Latin1.GetString(p.SendBuffer.ToArray());
            Assert.Contains(",60,Fizzle", payload);
            Assert.Equal(0, CdrRemainingMs(p));
        }

        [Fact]
        public void CastSpell_WhenFizzles_SendsCooldownOnlyToCaster()
        {
            var (p, world) = NewCaster();
            var spell = NewSpell();
            spell.MPStaticCost = 50;
            p.Spellbook.AddSpell(spell, world);
            p.CurrentMP = 0;

            var nearby = new Player(1);
            nearby.OnLogin();
            nearby.Sock = NewUnconnectedSocket();
            nearby.MapX = p.MapX;
            nearby.MapY = p.MapY;
            p.Map.AddPlayer(p, world);
            p.Map.AddPlayer(nearby, world);

            p.CastSpell(1, p, world);

            var casterPayload = Encoding.Latin1.GetString(p.SendBuffer.ToArray());
            var nearbyPayload = Encoding.Latin1.GetString(nearby.SendBuffer.ToArray());

            Assert.Contains(",60,Fizzle", casterPayload);
            Assert.Contains(",60,Fizzle", nearbyPayload);
            Assert.DoesNotContain("CDR", nearbyPayload);
        }
    }
}
