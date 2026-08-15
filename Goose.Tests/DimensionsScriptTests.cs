using Goose.Quests;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionsScriptTests
{
    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Disabled_by_configuration_changes_nothing()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        boss.BaseStats = new AttributeSet { HP = 3704 };
        fixture.World.NPCHandler.AddTemplate(boss);

        var source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "Dimensions.csx"));
        var disabled = source.Replace("public const bool Enabled = true;",
                                      "public const bool Enabled = false;");
        Assert.NotEqual(source, disabled);   // the flag line moved - fix this test, not the script

        fixture.CompileSource(disabled, "DimensionsDisabled.csx").Object.OnLoaded(fixture.World);

        Assert.Single(fixture.World.MapHandler.Maps);
        Assert.Null(fixture.World.MapHandler.GetMap(100001));
        Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(100162));
        Assert.Null(fixture.World.QuestHandler.Get(900000));
        Assert.Equal(0, fixture.World.NPCHandler.NPCCount);
        // No /dimension command either - the whole feature is off, not just the world.
        Assert.False(fixture.World.EventHandler.AddEvent(new Player(0), "/dimension 1"));
    }

    [Fact]
    public void Registers_the_dimension_command()
    {
        using var fixture = Run(f => f.AddBaseMap(1, "Town", width: 100, height: 100));

        // AddEvent returns false when no command matches the packet prefix (EventHandler.cs:286).
        Assert.True(fixture.World.EventHandler.AddEvent(new Player(0), "/dimension 1"));
        Assert.False(fixture.World.EventHandler.AddEvent(new Player(0), "/notacommand"));
    }

    [Fact]
    public void Clones_each_template_once_per_dimension_with_scaled_stats()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40,
                                      WeaponDamage = 365, RespawnTime = 50, Experience = 750,
                                      AttackSpeed = 1.5m, MoveSpeed = 1.5m, AttackRange = 1,
                                      CanBeRooted = true, CanBeStunned = true, CanBeSlowed = false };
            t.BaseStats = new AttributeSet { HP = 3704 };
            f.World.NPCHandler.AddTemplate(t);
        });

        var dim3 = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 3);
        Assert.NotNull(dim3);
        Assert.Equal("Shadow Dog (3)", dim3.Name);

        // NPC.java:927 - (base + 100000*2^dim) * 4.7^dim
        Assert.Equal((long)((3704 + 100000 * Math.Pow(2, 3)) * Math.Pow(4.7, 3)), dim3.BaseStats.HP);
        // NPC.java:936 - base*4^dim + 100000*max(0, 4^dim-3)
        Assert.Equal((long)(365 * Math.Pow(4, 3) + 100000 * Math.Max(0, Math.Pow(4, 3) - 3)), dim3.WeaponDamage);
        // NPC.java:954 - (exp + level*100) * 3^min(4,dim)
        Assert.Equal((long)((750 + 40 * 100) * Math.Pow(3, 3)), dim3.Experience);
        // NPC.java:899 - every dimension mob is level 50
        Assert.Equal(50, dim3.Level);
        // NPC.java:881 - immune to root and stun, but slowable
        Assert.False(dim3.CanBeRooted);
        Assert.False(dim3.CanBeStunned);
        Assert.True(dim3.CanBeSlowed);
        // NPC.java:869 - attack range grows with dimension
        Assert.Equal(1 + 3, dim3.AttackRange);
    }

    [Fact]
    public void Leaves_the_base_template_untouched()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
            t.BaseStats = new AttributeSet { HP = 3704 };
            f.World.NPCHandler.AddTemplate(t);
        });

        var basic = fixture.World.NPCHandler.GetNPCTemplate(162);
        Assert.Equal("Shadow Dog", basic.Name);
        Assert.Equal(3704, basic.BaseStats.HP);
        Assert.Equal(40, basic.Level);
    }

    [Fact]
    public void Allies_point_at_the_same_dimensions_templates()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var dog = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
            var wolf = new NPCTemplate { NPCTemplateID = 163, Name = "Shadow Wolf", Level = 40 };
            dog.BaseStats = new AttributeSet();   // ScaleTemplate reads basic.BaseStats.HP
            wolf.BaseStats = new AttributeSet();
            dog.Allies = new List<NPCTemplate> { wolf };
            wolf.Allies = new List<NPCTemplate> { dog };
            f.World.NPCHandler.AddTemplate(dog);
            f.World.NPCHandler.AddTemplate(wolf);
        });

        var dim3Dog = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 3);
        var dim3Wolf = fixture.World.NPCHandler.GetNPCTemplate(163 + 100000 * 3);

        // Reference identity is what NPC.cs:559 compares, so Same, not Equal.
        Assert.Same(dim3Wolf, Assert.Single(dim3Dog.Allies));
        Assert.Same(dim3Dog, Assert.Single(dim3Wolf.Allies));

        // The base templates keep their own allies.
        Assert.Same(fixture.World.NPCHandler.GetNPCTemplate(163),
                    Assert.Single(fixture.World.NPCHandler.GetNPCTemplate(162).Allies));
    }

    [Fact]
    public void An_ally_with_no_dimension_clone_is_dropped_rather_than_left_pointing_at_dimension_zero()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var dog = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
            dog.BaseStats = new AttributeSet();   // ScaleTemplate reads basic.BaseStats.HP
            // An ally id that resolved at load time but is not in the handler now.
            dog.Allies = new List<NPCTemplate> { new NPCTemplate { NPCTemplateID = 999 } };
            f.World.NPCHandler.AddTemplate(dog);
        });

        Assert.Empty(fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 3).Allies);
    }

    [Fact]
    public void Applies_the_dimension_five_multipliers()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40, WeaponDamage = 365 };
            t.BaseStats = new AttributeSet { HP = 3704 };   // <= 35,000,000, so HP doubles at dim >= 5
            f.World.NPCHandler.AddTemplate(t);
        });

        var dim5 = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 5);

        Assert.Equal((long)((3704 + 100000 * Math.Pow(2, 5)) * Math.Pow(4.7, 5)) * 2, dim5.BaseStats.HP);
        // base < 10,000,000 so damage is multiplied by 20
        Assert.Equal((long)(365 * Math.Pow(4, 5) + 100000 * Math.Max(0, Math.Pow(4, 5) - 3)) * 20, dim5.WeaponDamage);
        // This value exceeds int.MaxValue - it only fits because Part 1 widened the fields.
        Assert.True(dim5.BaseStats.HP > int.MaxValue);
    }

    [Fact]
    public void Clones_each_map_once_per_dimension()
    {
        using var fixture = Run(f => f.AddBaseMap(1, "Town", width: 100, height: 100));

        var dim2 = fixture.World.MapHandler.GetMap(1 + 100000 * 2);

        Assert.NotNull(dim2);
        Assert.Equal("Town (2)", dim2.Name);
        Assert.True(dim2.CanPVP);                     // PVP is forced on in every dimension
        Assert.NotSame(fixture.World.MapHandler.GetMap(1).characters, dim2.characters);
        Assert.NotSame(fixture.World.MapHandler.GetMap(1).tiles, dim2.tiles);
    }

    [Fact]
    public void Warps_point_at_the_same_dimension()
    {
        using var fixture = Run(f =>
        {
            var town = f.AddBaseMap(1, "Town", width: 100, height: 100);
            var cave = f.AddBaseMap(2, "Cave");
            town.SetTile(3, 3, new WarpTile { WarpMap = cave, WarpX = 7, WarpY = 8 });
        });

        var dim2Town = fixture.World.MapHandler.GetMap(1 + 100000 * 2);
        var warp = (WarpTile)dim2Town.GetTile(3, 3);

        Assert.Equal(2 + 100000 * 2, warp.WarpMap.ID);   // the dimension-2 Cave, not the base one
        Assert.Equal(7, warp.WarpX);
        Assert.Equal(8, warp.WarpY);

        // The base map's warp must be untouched.
        var baseWarp = (WarpTile)fixture.World.MapHandler.GetMap(1).GetTile(3, 3);
        Assert.Equal(2, baseWarp.WarpMap.ID);
    }

    [Fact]
    public void Blocked_tiles_are_shared_not_duplicated()
    {
        using var fixture = Run(f =>
        {
            var town = f.AddBaseMap(1, "Town", width: 100, height: 100);
            town.SetTile(4, 4, new BlockedTile());
        });

        // BlockedTile is an empty marker (BlockedTile.cs:8), so sharing the reference is safe.
        Assert.Same(fixture.World.MapHandler.GetMap(1).GetTile(4, 4),
                    fixture.World.MapHandler.GetMap(100001).GetTile(4, 4));
    }

    /// <summary>The clone must not become a way around a key-gated map. requiredItems is
    /// private (Map.cs:64) and enforced by PlayerCanJoin (Map.cs:573), which is why Part 1
    /// added Map.CloneAs rather than leaving the script to rebuild public fields.</summary>
    [Fact]
    public void Clones_keep_the_base_maps_required_items_and_mute_state()
    {
        using var fixture = Run(f =>
        {
            var vault = f.AddBaseMap(1, "Vault", width: 100, height: 100);
            vault.AddRequiredItem(1234);
            vault.Muted = true;
        });

        var dim2 = fixture.World.MapHandler.GetMap(1 + 100000 * 2);

        Assert.Equal(new[] { 1234 }, dim2.RequiredItems);
        Assert.True(dim2.Muted);
    }

    [Fact]
    public void Spawns_the_dimension_template_on_the_dimension_map()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
            t.BaseStats = new AttributeSet { HP = 3704 };
            f.World.NPCHandler.AddTemplate(t);

            f.World.NPCHandler.SpawnNPC(f.World, 1, 50, 50, t, shouldRespawn: true);
        });

        var dim1Map = fixture.World.MapHandler.GetMap(100001);
        // The warden shares this map, so pick out the boss clone by template id.
        var spawned = dim1Map.NPCs.Single(n => n.NPCTemplate.NPCTemplateID == 162 + 100000);

        Assert.Equal(162 + 100000, spawned.NPCTemplate.NPCTemplateID);
        Assert.Equal(50, spawned.SpawnX);
        Assert.Equal(50, spawned.SpawnY);
    }

    /// <summary>The done criteria are stated in NPCCount ("~82,000 NPCs"), so the generated
    /// spawns must actually be registered with the handler. Only SpawnNPC (Part 1 task 4) does
    /// that - NPC.LoadFromTemplate adds to the map and the login-id lookup and nothing else.</summary>
    [Fact]
    public void Generated_spawns_are_registered_with_the_handler()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
            t.BaseStats = new AttributeSet { HP = 3704 };
            f.World.NPCHandler.AddTemplate(t);
            f.World.NPCHandler.SpawnNPC(f.World, 1, 50, 50, t, shouldRespawn: true);
        });

        // 1 base spawn + one per dimension (6) + one warden per quest-bearing dimension (6,
        // dimensions 0-5). The script's types are not visible to the test assembly, so the
        // count is a literal. The warden chain (Task 7) turns the exact count into a floor.
        Assert.True(fixture.World.NPCHandler.NPCCount >= 7);
    }

    private static void SeedBoss(GlobalScriptFixture f)
    {
        f.AddBaseMap(1, "Town", width: 100, height: 100);
        var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        boss.BaseStats = new AttributeSet { HP = 3704 };
        f.World.NPCHandler.AddTemplate(boss);
    }

    [Fact]
    public void Map_ids_do_not_collide_with_existing_maps()
    {
        // A base map already sitting on a generated id must be a loud failure, not a silent
        // overwrite - MapHandler.Maps is a plain dictionary.
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town");
        fixture.AddBaseMap(100001, "Impostor");

        using (fixture)
        {
            var ex = Assert.Throws<Exception>(
                () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
            Assert.Contains("100001", ex.Message);
        }
    }

    [Fact]
    public void Creates_one_unlock_quest_per_dimension()
    {
        using var fixture = Run(SeedBoss);

        for (int dim = 0; dim < 6; dim++)
        {
            var quest = fixture.World.QuestHandler.Get(900000 + dim);
            Assert.NotNull(quest);

            var requirement = quest.Requirements.Single();
            Assert.Equal(RequirementType.Kill, requirement.Type);
            // Dimension n's quest wants dimension n's boss - a distinct template id, which is
            // what makes the stock Kill requirement dimension-aware (Player.cs:1020).
            Assert.Equal(162 + 100000 * dim, requirement.Value);
            Assert.Equal(1, requirement.Value2);

            Assert.Equal(RewardType.Script, quest.Rewards.Single().Type);
        }
    }

    [Fact]
    public void Quest_ids_are_deterministic_across_runs()
    {
        using var first = Run(SeedBoss);
        using var second = Run(SeedBoss);

        Assert.Equal(first.World.QuestHandler.Get(900003).Requirements.Single().Id,
                     second.World.QuestHandler.Get(900003).Requirements.Single().Id);
    }

    [Fact]
    public void Wardens_carry_their_dimensions_quest()
    {
        using var fixture = Run(SeedBoss);

        var warden = fixture.World.NPCHandler.GetTemplates()
            .Single(t => t.NPCType == NPCTemplate.Types.Quest && t.Quests.Any(q => q.Id == 900002));

        Assert.Single(warden.Quests);
    }

    /// <summary>The warden is built from configuration, not from sheet data, so the
    /// configuration is the only thing that decides what players see and whether it can be
    /// killed. A killable quest giver in a forced-PVP dimension is a griefing target.</summary>
    [Fact]
    public void Wardens_use_the_configured_appearance_and_cannot_be_killed()
    {
        using var fixture = Run(SeedBoss);

        var warden = fixture.World.NPCHandler.GetNPCTemplate(800000 + 100000 * 2);

        Assert.NotNull(warden);
        Assert.Equal(NPCTemplate.Types.Quest, warden.NPCType);
        Assert.False(warden.CanBeKilled);
        Assert.False(warden.CanMove);
        Assert.Equal(50, warden.Level);
        Assert.Equal(3, warden.ClassID);

        // Appearance comes from config verbatim - dimension recolouring must NOT be applied.
        Assert.Equal(1, warden.BodyID);
        Assert.Equal(1, warden.FaceID);
        Assert.Equal(1, warden.HairID);
        Assert.Equal(40, warden.BodyR);
        Assert.Equal(20, warden.HairR);
        Assert.Equal("", warden.EquippedItems);
    }

    /// <summary>NPC.LoadFromTemplate dereferences Class.GetLevel(Level) at NPC.cs:636 with no
    /// null check, so a warden on a class with no row at WardenLevel throws mid-spawn and
    /// leaves the world half-built. Class 1 has levels 1-5 only, which is exactly the shape of
    /// this mistake.</summary>
    [Fact]
    public void A_warden_class_with_no_row_at_the_warden_level_is_rejected_up_front()
    {
        var fixture = new GlobalScriptFixture();
        SeedBoss(fixture);
        fixture.RemoveClassLevel(classId: 3, level: 50);

        using (fixture)
        {
            var ex = Assert.Throws<Exception>(
                () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
            Assert.Contains("50", ex.Message);
        }
    }

    [Fact]
    public void Wardens_are_spawned_on_the_start_map_of_every_dimension_that_has_a_quest()
    {
        using var fixture = Run(SeedBoss);

        // Dimensions 0-5 each offer the quest that unlocks the dimension above them. Dimension
        // 6 is the top, so it has no quest and no warden.
        for (int dim = 0; dim < 6; dim++)
        {
            var map = fixture.World.MapHandler.GetMap(1 + 100000 * dim);
            Assert.Contains(map.NPCs, n => n.NPCTemplate.NPCTemplateID == 800000 + 100000 * dim);
        }

        Assert.DoesNotContain(fixture.World.MapHandler.GetMap(1 + 100000 * 6).NPCs,
                              n => n.NPCTemplate.NPCType == NPCTemplate.Types.Quest);
    }

    // ---- Id collisions ---------------------------------------------------------------

    /// <summary>AddTemplate and AddQuest both overwrite silently. Generated ids landing on
    /// sheet-authored rows would replace real content with no diagnostic at all, so every
    /// generated id space gets a preflight check.</summary>
    [Theory]
    [InlineData("npc template", 800000)]      // warden base id
    [InlineData("npc template", 100162)]      // dimension-1 clone of the seeded boss
    public void Generated_npc_template_ids_must_not_already_exist(string _, int id)
    {
        var fixture = new GlobalScriptFixture();
        SeedBoss(fixture);
        fixture.World.NPCHandler.AddTemplate(new NPCTemplate { NPCTemplateID = id, Name = "Impostor" });

        using (fixture)
        {
            var ex = Assert.Throws<Exception>(
                () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
            Assert.Contains(id.ToString(), ex.Message);
        }
    }

    [Fact]
    public void Generated_quest_ids_must_not_already_exist()
    {
        var fixture = new GlobalScriptFixture();
        SeedBoss(fixture);
        fixture.World.QuestHandler.AddQuest(new Quest { Id = 900003, Name = "Sheet-authored" });

        using (fixture)
        {
            var ex = Assert.Throws<Exception>(
                () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
            Assert.Contains("900003", ex.Message);
        }
    }

    /// <summary>Requirement and reward ids are the persistence key for in-flight quest
    /// progress, so a collision there corrupts saved progress rather than just content.</summary>
    [Fact]
    public void Requirement_and_reward_ids_do_not_collide_with_each_other()
    {
        using var fixture = Run(SeedBoss);

        var ids = new List<int>();
        for (int dim = 0; dim < 6; dim++)
        {
            var quest = fixture.World.QuestHandler.Get(900000 + dim);
            ids.AddRange(quest.Requirements.Select(r => r.Id));
            ids.AddRange(quest.Rewards.Select(r => r.Id));
        }

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // ---- The reward actually grants the dimension ------------------------------------

    /// <summary>The chain is only worth anything if completing a quest raises dimension.max
    /// and that survives a save. Nothing above tests the reward script at all.</summary>
    [Fact]
    public void Completing_a_quest_raises_dimension_max()
    {
        using var fixture = Run(SeedBoss);

        var reward = fixture.World.QuestHandler.Get(900002).Rewards.Single();
        var player = new Player(0);

        reward.Script.Object.GiveReward(reward, npc: null, player, fixture.World);

        // Quest index 2 unlocks dimension 3.
        Assert.Equal(3, player.Properties.GetProperty<int>("dimension.max", 0));
    }

    [Fact]
    public void The_reward_raises_but_never_lowers_dimension_max()
    {
        using var fixture = Run(SeedBoss);

        var player = new Player(0);
        player.Properties["dimension.max"] = 5;

        var reward = fixture.World.QuestHandler.Get(900000).Rewards.Single();
        reward.Script.Object.GiveReward(reward, npc: null, player, fixture.World);

        Assert.Equal(5, player.Properties.GetProperty<int>("dimension.max", 0));
    }

    /// <summary>And it must persist - the property is only useful if it comes back after a
    /// restart. This closes the loop with Part 1's player_properties column.</summary>
    [Fact]
    public void A_granted_dimension_survives_a_save_and_reload()
    {
        using var fixture = Run(SeedBoss);

        var reward = fixture.World.QuestHandler.Get(900002).Rewards.Single();
        var player = new Player(0);
        reward.Script.Object.GiveReward(reward, npc: null, player, fixture.World);

        var json = JsonHelper.Serialize(player.Properties.Clone());
        var reloaded = new Player(0);
        reloaded.LoadPropertiesFromColumn(json);

        Assert.Equal(3, reloaded.Properties.GetProperty<int>("dimension.max", 0));
    }
}
