using Goose.Testing;
using System.Data.SQLite;

namespace Goose.IntegrationTests;

public class GroundZoneLoadTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "groundzone-load-" + Guid.NewGuid().ToString("N") + ".db");

    private readonly GameWorld world;

    public GroundZoneLoadTests()
    {
        var settings = new GooseSettings
        {
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        world = new GameWorld(settings, new GameServer(settings));
        world.Database.Start(dbPath);
        world.Database.Execute(conn => RunSql(conn, SchemaDdl.ForAll("spell_effects", "spells")));
    }

    private void Insert(string sql) => world.Database.Execute(conn => RunSql(conn, sql));

    private void Load()
    {
        world.SpellHandler.LoadSpellEffects(world);
        world.SpellHandler.LoadSpells(world);
    }

    [Fact]
    public void Reads_the_ground_zone_columns()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_tick_ms, zone_buff_effect_id, zone_overlaps) " +
               "VALUES (1, 'Fire Patch', 6, 24, 8, 5, 1, 1500, 0, 1)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id, ground_targetable) " +
               "VALUES (1, 'Fire Patch', 0, 1, 1, 1)");

        Load();

        var effect = world.SpellHandler.GetSpellEffect(1)!;
        Assert.Equal(SpellEffect.EffectTypes.GroundZone, effect.EffectType);
        Assert.Equal(1500, effect.ZoneTickMs);
        Assert.Equal(0, effect.ZoneBuffEffectID);
        Assert.True(effect.ZoneOverlaps);
        Assert.True(world.SpellHandler.GetSpell(1)!.GroundTargetable);
    }

    [Fact]
    public void Defaults_apply_when_the_columns_are_omitted()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type) " +
               "VALUES (2, 'Plain', 2, 0)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id) " +
               "VALUES (2, 'Plain', 0, 1, 2)");

        Load();

        var effect = world.SpellHandler.GetSpellEffect(2)!;
        Assert.Equal(1000, effect.ZoneTickMs);
        Assert.False(effect.ZoneOverlaps);
        Assert.False(world.SpellHandler.GetSpell(2)!.GroundTargetable);
    }

    [Fact]
    public void Skips_invalid_zones_and_the_spells_that_use_them()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size) VALUES (3, 'Random Zone', 6, 24, 8, 4, 1)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size) VALUES (4, 'Good Zone', 6, 24, 8, 5, 1)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id) " +
               "VALUES (3, 'Random Zone', 0, 1, 3)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id) " +
               "VALUES (4, 'Group Zone', 2, 1, 4)");

        Load();

        Assert.Null(world.SpellHandler.GetSpellEffect(3));
        Assert.Null(world.SpellHandler.GetSpell(3));
        Assert.NotNull(world.SpellHandler.GetSpellEffect(4));
        Assert.Null(world.SpellHandler.GetSpell(4));
    }

    [Fact]
    public void Reload_drops_spells_whose_effect_becomes_invalid()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size) VALUES (3, 'Zone', 6, 24, 8, 5, 1)");
        Insert("INSERT INTO spells (spell_id, spell_name, spell_target, spellbook_graphic, spell_effect_id) " +
               "VALUES (3, 'Zone', 0, 1, 3)");

        Load();

        Assert.NotNull(world.SpellHandler.GetSpell(3));

        Insert("UPDATE spell_effects SET target_type = 4 WHERE spell_effect_id = 3");

        Load();

        Assert.Null(world.SpellHandler.GetSpellEffect(3));
        Assert.Null(world.SpellHandler.GetSpell(3));
    }

    [Fact]
    public void Links_zone_buffs_and_drops_zones_with_a_bad_one()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, effect_duration) " +
               "VALUES (10, 'Ward', 6, 1, 30)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, effect_duration) " +
               "VALUES (11, 'Stun', 6, 7, 3)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_buff_effect_id) VALUES (12, 'Sanctuary', 6, 24, 8, 5, 1, 10)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_buff_effect_id) VALUES (13, 'Stun Field', 6, 24, 8, 5, 1, 11)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_buff_effect_id) VALUES (14, 'Ghost Field', 6, 24, 8, 5, 1, 404)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type) " +
               "VALUES (15, 'Bolt', 6, 0)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_buff_effect_id) VALUES (16, 'Bolt Field', 6, 24, 8, 5, 1, 15)");

        Load();

        Assert.Same(world.SpellHandler.GetSpellEffect(10), world.SpellHandler.GetSpellEffect(12)!.ZoneBuffEffect);
        Assert.Same(world.SpellHandler.GetSpellEffect(11), world.SpellHandler.GetSpellEffect(13)!.ZoneBuffEffect);
        Assert.Null(world.SpellHandler.GetSpellEffect(14));
        Assert.Null(world.SpellHandler.GetSpellEffect(16));
    }

    private static void RunSql(SQLiteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        world.Database.Stop();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
