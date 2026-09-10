using System.Data.SQLite;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Goose;
using Goose.Events;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class DoubleBehaviorTests
{
    [Fact]
    public void RollChance_0_5_lands_in_50_percent_band()
    {
        using var fixture = new TestWorldFixture();

        const int rolls = 20000;
        int hits = 0;
        for (int i = 0; i < rolls; i++)
            if (fixture.World.RollChance(0.5)) hits++;

        Assert.InRange(hits / (double)rolls, 0.48, 0.52);
    }

    [Theory]
    [InlineData(1, 125)]
    [InlineData(2, 157)]
    [InlineData(3, 196)]
    [InlineData(4, 245)]
    [InlineData(5, 306)]
    public void Multiplier_ladder_pins_double_quantizer(int dim, long expected)
    {
        var stats = new AttributeSet { HP = 100, MP = 100 };

        var scaled = stats * Math.Pow(1.25, dim);

        Assert.Equal(expected, scaled.HP);
        Assert.Equal(expected, scaled.MP);
    }

    [Theory]
    [InlineData(10.5, "10")]
    [InlineData(2.5, "2")]
    public void Snare_F0_midpoint_rounds_half_to_even(double snarePercent, string expected)
    {
        using var fixture = new TestWorldFixture();
        var effect = new SpellEffect
        {
            EffectType = SpellEffect.EffectTypes.Permanent,
            SnarePercent = snarePercent,
        };

        var lines = effect.GetItemDescription(fixture.World).ToList();

        Assert.Equal("Permanently Decrease Move Speed by " + expected + "%", lines.Single());
    }

    [Fact]
    public void Percentage_F0_midpoint_rounds_half_to_even()
    {
        using var fixture = new TestWorldFixture();
        var effect = new SpellEffect
        {
            EffectType = SpellEffect.EffectTypes.Permanent,
            Stats = new AttributeSet { Haste = 0.105 },
        };

        var lines = effect.GetItemDescription(fixture.World).ToList();

        Assert.Equal("Permanently Increase Melee Attack Speed by 10%", lines.Single());
    }

    [Fact]
    public void MathRound_aether_wait_2_135_rounds_to_2_13()
    {
        Assert.Equal(2.13, Math.Round(2.135, 2));
    }

    [Fact]
    public void Wire_int_cast_truncates_spell_effect_chance()
    {
        using var fixture = new TestWorldFixture();
        var template = fixture.AddBaseItemTemplate(1, "Potion", ItemTemplate.UseTypes.OneTime,
            t => t.SpellEffectChance = 55.7);
        var item = new Item();
        item.LoadFromTemplate(template);

        var fields = P.ItemSlot(item, fixture.World, 1, 1).Split('|');

        Assert.Equal("55", fields[^9]);
    }

    [Fact]
    public void CalculateMoveSpeed_returns_int()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.PlayerOn(fixture.AddBaseMap(1, "Town"), 1, 1);

        int speed = player.CalculateMoveSpeed();

        Assert.Equal(0, speed);
    }

    [Theory]
    [InlineData(10, 15)]
    [InlineData(11, 15)]
    [InlineData(7, 15)]
    [InlineData(1, 0)]
    [InlineData(4, 25)]
    [InlineData(3, 100)]
    public void Npc_move_tick_pins_binary_product(int moveSpeed, int snarePercent)
    {
        using var fixture = new TestWorldFixture();
        var npc = new NPC
        {
            MoveSpeed = moveSpeed,
            Buffs = [SnareBuff(snarePercent)],
        };

        // Event() seeds Ticks with the current timestamp, so the interval is only
        // observable bracketed between two TimeNow reads.
        long before = fixture.World.TimeNow;
        npc.AddMoveEvent(fixture.World);
        long after = fixture.World.TimeNow;
        long expected = ExpectedTicks(fixture.World.TimerFrequency, moveSpeed, snarePercent);

        Assert.InRange(npc.MoveEvent!.Ticks, before + expected, after + expected);
    }

    [Theory]
    [InlineData(11, 15)]
    [InlineData(7, 15)]
    [InlineData(10, 15)]
    public void Npc_attack_tick_pins_binary_product(int attackSpeed, int snarePercent)
    {
        using var fixture = new TestWorldFixture();
        var npc = new NPC
        {
            AttackSpeed = attackSpeed,
            AttackRange = 5,
            AggroTarget = fixture.PlayerOn(fixture.AddBaseMap(1, "Town"), 1, 1),
            Buffs = [SnareBuff(snarePercent)],
        };

        long before = fixture.World.TimeNow;
        npc.AddAttackEvent(fixture.World);
        long after = fixture.World.TimeNow;
        long expected = ExpectedTicks(fixture.World.TimerFrequency, attackSpeed, snarePercent);

        Assert.InRange(npc.AttackEvent!.Ticks, before + expected, after + expected);
    }

    [Fact]
    public void BuyVita_price_pins_integer_division_and_long_truncation()
    {
        using var fixture = new TestWorldFixture(s =>
        {
            s.IncreaseVitaBuyAmount = 100;
            s.VitaBuyAmount = 50;
        });
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "Town"), 1, 1, "Buyer");
        player.Level = 1;
        player.Experience = 10000;
        player.BaseStats.HP = 1001;
        fixture.World.ClassHandler.GetClass(0)!.VitaCost = 1000;

        Assert.True(fixture.RunCommand(player, "/buyvita"));

        Assert.Equal(7000, player.Experience);
        Assert.Equal(3000, player.ExperienceSold);
        Assert.Equal(1051, player.BaseStats.HP);
        Assert.Contains(player.Sent, m => m.Contains("Bought 50 hp for 3000 experience."));
    }

    [Fact]
    public void ExperienceModifier_pins_integer_division_of_player_count()
    {
        using var fixture = new TestWorldFixture(s =>
        {
            s.IdleTimeout = 10;
            s.PlayerCountExperienceModifierInterval = 3;
            s.PlayerCountExperienceModifier = 1.5;
        });
        var map = fixture.AddBaseMap(1, "Town");

        // Four distinct loopback IPs: 127/8 is loopback on Linux, so the event's
        // per-IP set sees four players, not one.
        var peers = new List<LoopbackPeer>();
        for (int i = 1; i <= 4; i++)
        {
            var peer = new LoopbackPeer("127.0.0." + i);
            peers.Add(peer);
            var player = fixture.CommandPlayerOn(map, 1, 1, "P" + i);
            player.Sock = peer.Client;
            player.LastActive = fixture.World.TimeNow;
            fixture.World.PlayerHandler.AddPlayer(player, fixture.World);
        }

        try
        {
            new PlayerCountExperienceModifierUpdateEvent().Ready(fixture.World);

            Assert.Equal(2.5, fixture.World.ExperienceModifier);
        }
        finally
        {
            foreach (var peer in peers) peer.Dispose();
        }
    }

    [Theory]
    [InlineData(1e-7, "1E-07")]
    [InlineData(1e21, "1E+21")]
    public void Player_save_query_text_is_valid_sql_for_extreme_finite_values(double aetherThreshold, string expectedText)
    {
        var player = new Player(0) { AetherThreshold = aetherThreshold, BaseStats = new AttributeSet() };

        var update = player.BuildUpdateQuery();
        var insert = player.BuildInsertQuery();

        var marker = "aether_threshold=";
        var start = update.IndexOf(marker) + marker.Length;
        var value = update.Substring(start, update.IndexOf(',', start) - start);
        Assert.Equal(expectedText, value);
        Assert.Equal(aetherThreshold, double.Parse(value, CultureInfo.InvariantCulture));

        Assert.Contains(expectedText, insert);
        Assert.DoesNotContain("NaN", update);
        Assert.DoesNotContain("∞", update);
        Assert.DoesNotContain("NaN", insert);
        Assert.DoesNotContain("∞", insert);
    }

    [Fact]
    public void Pet_save_round_trips_extreme_finite_regen_values()
    {
        using var conn = new SQLiteConnection("Data Source=:memory:;Version=3;");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "pets.sql"));
            cmd.ExecuteNonQuery();
        }

        var pet = new Pet
        {
            PetID = 1,
            Name = "Pet",
            Title = "",
            Surname = "",
            Class = new Class { ClassID = 1 },
            Owner = new Player(0) { PlayerID = 5 },
            BaseStats = new AttributeSet { HPPercentRegen = 1e-7, MPPercentRegen = 1e21 },
            AutoCreatedNotSaved = true,
        };
        pet.BuildSave()(conn);

        double hpRegen, mpRegen;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT hp_percent_regen, mp_percent_regen FROM pets WHERE pet_id=1";
            using var reader = cmd.ExecuteReader();
            reader.Read();
            hpRegen = Convert.ToDouble(reader[0]);
            mpRegen = Convert.ToDouble(reader[1]);
        }

        Assert.Equal(1e-7, hpRegen);
        Assert.Equal(1e21, mpRegen);
    }

    private static Buff SnareBuff(int percent) => new()
    {
        SpellEffect = new SpellEffect
        {
            EffectType = SpellEffect.EffectTypes.Snare,
            SnarePercent = percent,
        },
    };

    // Hardcoded ticks are the exact double product (11.0 * 1.15 == 12.649999999999999),
    // one below decimal arithmetic's 12650000000 at 1e9 Hz; fallback repeats the product.
    private static long ExpectedTicks(long freq, int speed, int snarePercent) => (speed, snarePercent) switch
    {
        (10, 15) => 115L * freq / 10,
        (1, 0) => freq,
        (4, 25) => 5 * freq,
        (3, 100) => 6 * freq,
        (11, 15) => freq switch
        {
            1_000_000_000 => 12_649_999_999L,
            10_000_000 => 126_499_999L,
            _ => (long)(11.0 * (1 + 15.0 / 100.0) * freq),
        },
        (7, 15) => freq switch
        {
            1_000_000_000 => 8_049_999_999L,
            10_000_000 => 80_499_999L,
            _ => (long)(7.0 * (1 + 15.0 / 100.0) * freq),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(speed)),
    };

    private sealed class LoopbackPeer : IDisposable
    {
        public Socket Client { get; }
        private readonly Socket listener;
        private readonly Socket accepted;

        public LoopbackPeer(string ip)
        {
            this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.listener.Bind(new IPEndPoint(IPAddress.Parse(ip), 0));
            this.listener.Listen(1);
            this.Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Client.Connect(this.listener.LocalEndPoint!);
            this.accepted = this.listener.Accept();
        }

        public void Dispose()
        {
            this.Client.Dispose();
            this.accepted.Dispose();
            this.listener.Dispose();
        }
    }
}
