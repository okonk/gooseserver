using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Quests;
using Goose.Scripting;

public partial class Dimensions
{

    /// <summary>Second pass over the maps Task 3 created: spawn each dimension's clone of
    /// every base-map NPC on that dimension's map copy. Runs after RewireWarps because it
    /// reads base spawns, which nothing else mutates.</summary>
    private void CloneSpawns(GameWorld world)
    {
        // Snapshot the base spawns before spawning anything - each map's npc list grows as we
        // go, and Map.NPCs hands back the live list (Map.cs:618).
        var baseSpawns = world.MapHandler.Maps.Values
            .Where(m => m.ID < Offset)
            .SelectMany(m => m.NPCs.ToList())
            .ToList();

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseSpawns)
            {
                var template = world.NPCHandler.GetNPCTemplate(basic.NPCTemplate.NPCTemplateID + Offset * dim);
                if (template == null) continue;

                // SpawnNPC, NOT new NPC().LoadFromTemplate(...): the latter adds the NPC to its
                // map and to the login-id lookup but not to NPCHandler.npcs, so it would never
                // appear in NPCCount. See Part 1 task 4.
                //
                // shouldRespawn: true - respawning is self-sustaining on the NPC, matching how
                // LoadNPCs creates the base spawns.
                world.NPCHandler.SpawnNPC(world, basic.Map.ID + Offset * dim,
                                          basic.SpawnX, basic.SpawnY, template, shouldRespawn: true);
            }
        }
    }

    /// <summary>Second pass over the templates Task 2 created: repoint every ally at the
    /// same dimension's clone. Ally checks compare template references (NPC.cs:559, :1000),
    /// so a dimension mob allied to a dimension-0 template recognises nothing.
    ///
    /// Separate from CloneTemplates because clone order is dictionary order - an ally's clone
    /// may not exist yet at the moment the template referencing it is built.</summary>
    private void RewireAllies(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.NPCHandler.GetTemplates()
                                       .Where(t => t.NPCTemplateID < Offset).ToList())
            {
                var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
                if (clone == null || basic.Allies == null) continue;

                var allies = new List<NPCTemplate>();
                foreach (var ally in basic.Allies)
                {
                    // An ally with no clone is dropped, not left pointing across dimensions.
                    var dimAlly = world.NPCHandler.GetNPCTemplate(ally.NPCTemplateID + Offset * dim);
                    if (dimAlly != null) allies.Add(dimAlly);
                }

                clone.Allies = allies;
                // Keep the string form consistent - nothing re-parses it after load, but a
                // divergent AlliesString is a trap for anyone debugging from a dump.
                clone.AlliesString = string.Join(" ", allies.Select(a => a.NPCTemplateID));
            }
        }
    }

    private void CloneTemplates(GameWorld world)
    {
        // Snapshot first: AddTemplate mutates the dictionary GetTemplates() enumerates.
        // Only base templates (id < Offset) are cloned - a generated id (warden, or an
        // already-cloned template) must not be treated as base sheet data and cloned again.
        var baseTemplates = world.NPCHandler.GetTemplates()
            .Where(t => t.NPCTemplateID < Offset).ToList();

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var template in baseTemplates)
            {
                int id = template.NPCTemplateID + Offset * dim;

                // AddTemplate overwrites silently. A base id large enough to land on another
                // dimension's slot would quietly replace a generated template. Refuse loudly.
                if (world.NPCHandler.GetNPCTemplate(id) != null)
                    throw new Exception($"Dimension template id {id} (base {template.NPCTemplateID}, dim {dim}) "
                                        + "already exists. Offset is too small for this data set.");

                world.NPCHandler.AddTemplate(ScaleTemplate(template, dim));
            }
        }
    }

    private NPCTemplate ScaleTemplate(NPCTemplate basic, int dim)
    {
        var clone = new NPCTemplate(basic)
        {
            NPCTemplateID = basic.NPCTemplateID + Offset * dim,
            Name = basic.Name + " (" + dim + ")",
            Level = 50,                                   // NPC.java:899
            AttackRange = basic.AttackRange + dim,        // NPC.java:869
            CanBeRooted = false,                          // NPC.java:881
            CanBeStunned = false,
            CanBeSlowed = true,
            AttackSpeed = ScaleAttackSpeed(basic.AttackSpeed, dim),
            MoveSpeed = Math.Max(basic.MoveSpeed - 0.15m * dim, 0.15m),   // NPC.java:907
            WeaponDamage = ScaleDamage(basic.WeaponDamage, dim),
            Experience = ScaleExperience(basic.Experience, basic.Level, dim),
            RespawnTime = ScaleRespawn(basic.RespawnTime, dim),
        };

        clone.BaseStats.HP = ScaleHP(basic.BaseStats.HP, dim);
        clone.BaseStats.HPPercentRegen = basic.BaseStats.HPPercentRegen + 0.004m * (dim + 1);  // NPC.java:879

        Recolour(clone, dim);   // NPC.java:1019
        return clone;
    }

    /// <summary>NPC.java:927</summary>
    private long ScaleHP(long basehp, int dim)
    {
        long hp = (long)((basehp + 100000 * Math.Pow(2, dim)) * Math.Pow(4.7, dim));
        if (dim >= 5 && basehp <= 35000000) hp *= 2;
        return hp;
    }

    /// <summary>NPC.java:936</summary>
    private long ScaleDamage(long baseDamage, int dim)
    {
        long damage = (long)(baseDamage * Math.Pow(4, dim) + 100000 * Math.Max(0, Math.Pow(4, dim) - 3));
        if (dim >= 5 && baseDamage < 10000000) damage *= 20;
        return damage;
    }

    /// <summary>NPC.java:945. The dim>=5 branch raises the value back to 0.7 - faithful, if odd.</summary>
    private decimal ScaleAttackSpeed(decimal attackSpeed, int dim)
    {
        attackSpeed = Math.Max(attackSpeed - 0.175m * dim, 0.2m);
        if (dim >= 5 && attackSpeed > 0.5m) attackSpeed = 0.7m;
        return attackSpeed;
    }

    /// <summary>NPC.java:954</summary>
    private long ScaleExperience(long experience, int level, int dim)
    {
        double multi = Math.Pow(3, Math.Min(4, dim));
        if (dim >= 5) multi *= Math.Pow(2, dim - 4);
        return (long)((experience + level * 100) * multi);
    }

    /// <summary>NPC.java:963. Respawn stops shortening past dimension 4.</summary>
    private int ScaleRespawn(int respawnTime, int dim)
    {
        dim = Math.Min(4, dim);
        return Math.Min((int)(respawnTime * Math.Pow(0.85, dim)), 3600 / (1 + dim));
    }

    /// <summary>One quest per dimension: kill that dimension's boss, unlock the next
    /// dimension. Quest n is offered by dimension n's warden, so the chain walks the player
    /// outward one dimension at a time.</summary>
    private void CreateUnlockChain(GameWorld world)
    {
        ValidateWardenClass(world);

        var rewardScript = world.ScriptHandler.GetScript<IQuestScript>("Scripts/Global/Dimensions/DimensionUnlock.csx");

        for (int dim = 0; dim < DimensionCount; dim++)
        {
            int questId = QuestIdBase + dim;

            // AddQuest overwrites silently, and quest ids are the persistence key for
            // in-flight progress. A collision with a sheet-authored quest must be loud.
            if (world.QuestHandler.Get(questId) != null)
                throw new Exception($"Quest id {questId} already exists. QuestIdBase collides with sheet data.");

            var quest = new Quest
            {
                Id = questId,
                Name = "Abysmal Terror (" + (dim + 1) + ")",
                Description = "Slay the terror that stalks dimension " + dim + ".",
                FailText = "The terror still lives.",
                PassText = "The void yields. Dimension " + (dim + 1) + " is open to you.",
                ShowProgress = true,
                Repeatable = false,
                // Chain: quest n requires quest n-1. Quest 0 is the entry point.
                PrerequisiteQuests = dim == 0 ? new List<int>() : new List<int> { QuestIdBase + dim - 1 },
            };

            quest.Requirements.Add(new QuestRequirement
            {
                // Deterministic - QuestProgress persists keyed on requirement.Id
                // (Player.cs:1020, QuestWindow.cs:268). A counter-assigned id would orphan
                // in-flight kill progress on every restart.
                Id = QuestIdBase + dim * 10,
                Type = RequirementType.Kill,
                // Dimension n's boss is a distinct template id, which is the whole reason the
                // stock Kill requirement is dimension-aware with no engine change.
                Value = BossTemplateId + Offset * dim,
                Value2 = 1,
                KeepRequirement = false,
                Quest = quest,
            });

            quest.Rewards.Add(new QuestReward
            {
                Id = QuestIdBase + dim * 10 + 1,
                Type = RewardType.Script,
                Script = rewardScript,
                // QuestReward has no Quest back-reference (QuestReward.cs:37-45), unlike
                // QuestRequirement, so the reward cannot derive its dimension from its quest.
                // ScriptParams carries it, and one script file serves all six rewards.
                ScriptParams = (dim + 1).ToString(),
            });

            world.QuestHandler.AddQuest(quest);

            CreateWarden(world, dim, quest);
        }
    }

    /// <summary>NPC.LoadFromTemplate does Class.GetLevel(Level).BaseStats with no null check
    /// (NPC.cs:635-636). ClassHandler.GetClass returns null for an unknown id and
    /// Class.GetLevel returns null for a level the class has no row for - class 1 (Commoner)
    /// stops at level 5 while classes 2-7 reach 50. Either mistake would throw halfway through
    /// building the world, so check once, up front, with a message that says what to fix.</summary>
    private void ValidateWardenClass(GameWorld world)
    {
        var wardenClass = world.ClassHandler.GetClass(WardenClassId);
        if (wardenClass == null)
            throw new Exception($"WardenClassId {WardenClassId} does not exist.");

        if (wardenClass.GetLevel(WardenLevel) == null)
            throw new Exception($"Class {WardenClassId} has no level {WardenLevel} row in class_info. "
                                + "Pick a class that reaches WardenLevel, or lower WardenLevel.");
    }

    /// <summary>The quest giver for one dimension. Built from configuration rather than cloned
    /// from a base template - there is no warden in sheet data to clone.
    ///
    /// Deliberately NOT run through ScaleTemplate: scaling an invincible quest giver's HP and
    /// damage is meaningless, and the dimension recolour would fight the configured look.</summary>
    private void CreateWarden(GameWorld world, int dim, Quest quest)
    {
        int templateId = WardenTemplateId + Offset * dim;

        if (world.NPCHandler.GetNPCTemplate(templateId) != null)
            throw new Exception($"Warden template id {templateId} already exists. "
                                + "WardenTemplateId collides with sheet data.");

        var warden = new NPCTemplate
        {
            NPCTemplateID = templateId,
            NPCType = NPCTemplate.Types.Quest,
            Name = WardenName + (dim == 0 ? "" : " (" + dim + ")"),
            Title = WardenTitle,
            Surname = WardenSurname,
            Level = WardenLevel,
            ClassID = WardenClassId,

            CanBeKilled = false,     // maps to npc_templates.invincible (NPCHandler.cs:63)
            CanMove = false,
            CanBeRooted = false,
            CanBeStunned = false,
            CanBeSlowed = false,

            WeaponDamage = 0,
            AggroRange = 0,
            AttackRange = 1,
            AttackSpeed = 1m,
            MoveSpeed = 1m,
            RespawnTime = 0,
            Experience = 0,

            BodyID = WardenBodyID,
            BodyState = WardenBodyState,
            BodyR = WardenBodyR, BodyG = WardenBodyG, BodyB = WardenBodyB, BodyA = WardenBodyA,
            FaceID = WardenFaceID,
            HairID = WardenHairID,
            HairR = WardenHairR, HairG = WardenHairG, HairB = WardenHairB, HairA = WardenHairA,
            EquippedItems = WardenEquippedItems,

            AlliesString = "",
            Allies = new List<NPCTemplate>(),
            Drops = new List<NPCDropInfo>(),
        };

        warden.BaseStats = new AttributeSet { HP = 1000, MP = 0 };

        // Sheet-authored quest_ids are resolved at template-load time (NPCHandler.cs:108),
        // which runs before global scripts - so a script-created quest can never be attached
        // through data. It has to be attached here. NPC.cs:637 aliases template.Quests rather
        // than copying, so attaching before spawning is sufficient.
        warden.Quests.Add(quest);

        world.NPCHandler.AddTemplate(warden);

        // shouldRespawn: false - it cannot be killed, so it never needs to come back.
        if (world.NPCHandler.SpawnNPC(world, WardenMapId + Offset * dim,
                                      WardenX, WardenY, warden, shouldRespawn: false) == null)
        {
            throw new Exception($"Could not spawn the dimension-{dim} warden: map "
                                + (WardenMapId + Offset * dim) + " does not exist.");
        }
    }

    /// <summary>The spirit faucet. One NPC and one repeatable quest, in dimension 0 only.
    ///
    /// Deliberately NOT run through ScaleTemplate, and deliberately not cloned per
    /// dimension: rebirth requires stripping naked and leaves the player at level 1, and
    /// every dimension above 0 has CanPVP forced on (CloneMaps).</summary>
    private void CreateRebirthQuest(GameWorld world)
    {
        var rebirthClass = world.ClassHandler.GetClass(RebirthClassId);
        if (rebirthClass == null)
            throw new Exception($"RebirthClassId {RebirthClassId} does not exist.");
        if (rebirthClass.GetLevel(RebirthLevel) == null)
            throw new Exception($"Class {RebirthClassId} has no level {RebirthLevel} row in class_info.");

        // The destination class, not the keeper's. Rebirth calls ChangeClass(1, 1, ...),
        // which reads Class.GetLevel(1) on class 1 (Player.cs:1358+). class_info carries
        // levels 1-5 for class 1, but a dataset that dropped the row would turn every
        // completed rebirth into an NRE mid-transaction, after the quest was consumed.
        var commoner = world.ClassHandler.GetClass(RebirthDestinationClassId);
        if (commoner == null)
            throw new Exception($"RebirthDestinationClassId {RebirthDestinationClassId} does not exist.");
        if (commoner.GetLevel(RebirthDestinationLevel) == null)
            throw new Exception(
                $"Class {RebirthDestinationClassId} has no level {RebirthDestinationLevel} row in class_info - rebirth would fail mid-transaction.");

        if (ExpPerSpirit <= 0)
            throw new Exception("ExpPerSpirit must be positive - GiveReward divides by it.");

        if (world.QuestHandler.Get(RebirthQuestId) != null)
            throw new Exception($"Quest id {RebirthQuestId} already exists. RebirthQuestId collides with sheet data.");
        if (world.NPCHandler.GetNPCTemplate(RebirthTemplateId) != null)
            throw new Exception($"Rebirth template id {RebirthTemplateId} already exists.");

        // Placement, before anything is registered. NPC.LoadFromTemplate calls
        // Map.PlaceCharacter -> Map.SetCharacter, which simply returns on out-of-range
        // coordinates (Map.cs:643-648) - the NPC would exist, be invisible, and be
        // untargetable, with no error anywhere. IsTileBlocked covers all three failures at
        // once: out of bounds, a blocked or warp tile, and an occupant (Map.cs:417-440).
        // It runs here rather than at spawn time because CreateRebirthQuest is called after
        // CloneSpawns, so every generated NPC - the dimension-0 warden included - is
        // already standing on the map.
        var rebirthMap = world.MapHandler.GetMap(RebirthMapId);
        if (rebirthMap == null)
            throw new Exception($"RebirthMapId {RebirthMapId} does not exist.");
        if (rebirthMap.IsTileBlocked(null, RebirthX, RebirthY))
            throw new Exception(
                $"Rebirth keeper cannot stand at {RebirthMapId}({RebirthX},{RebirthY}): out of bounds, blocked, a warp tile, or occupied.");

        var rebirthScript = world.ScriptHandler.GetScript<IQuestScript>("Scripts/Global/Dimensions/Rebirth.csx");

        var quest = new Quest
        {
            Id = RebirthQuestId,
            Name = "Rebirth",
            Description = "Surrender everything you have earned and return to the\\n"
                        + "beginning. Every " + ExpPerSpirit.ToString("N0") + " experience\\n"
                        + "becomes one spirit. Anything left over is lost.\\n\\n"
                        + "You will be a level 1 commoner, and the dimensions\\n"
                        + "you have opened will demand their experience again.\\n\\n"
                        + "Come to me with nothing equipped.",
            FailText = "You are not ready. Remove everything you wear,\\nand bring more experience.",
            PassText = "You are unmade, and remade.",
            ShowProgress = true,
            Repeatable = true,
        };

        quest.Requirements.Add(new QuestRequirement
        {
            Id = RebirthQuestId + 1,
            Type = RequirementType.NothingEquipped,
            KeepRequirement = false,
            Quest = quest,
        });

        quest.Requirements.Add(new QuestRequirement
        {
            Id = RebirthQuestId + 2,
            Type = RequirementType.Script,
            Script = rebirthScript,
            ScriptParams = ExpPerSpirit.ToString(),
            // KeepRequirement true is load-bearing: TakeRequirements runs before
            // GiveRewards (QuestWindow.cs:341-342), so a consuming requirement would zero
            // the experience the reward has to read. All state change lives in the reward.
            KeepRequirement = true,
            Quest = quest,
        });

        quest.Rewards.Add(new QuestReward
        {
            Id = RebirthQuestId + 11,
            Type = RewardType.Script,
            Script = rebirthScript,
            // QuestReward has no Quest back-reference (QuestReward.cs:37-45), so the rate
            // travels here rather than being read off the requirement.
            ScriptParams = ExpPerSpirit.ToString(),
        });

        world.QuestHandler.AddQuest(quest);

        var keeper = new NPCTemplate
        {
            NPCTemplateID = RebirthTemplateId,
            NPCType = NPCTemplate.Types.Quest,
            Name = RebirthName,
            Title = RebirthTitle,
            Surname = RebirthSurname,
            Level = RebirthLevel,
            ClassID = RebirthClassId,

            CanBeKilled = false,
            CanMove = false,
            CanBeRooted = false,
            CanBeStunned = false,
            CanBeSlowed = false,

            WeaponDamage = 0,
            AggroRange = 0,
            AttackRange = 1,
            AttackSpeed = 1m,
            MoveSpeed = 1m,
            RespawnTime = 0,
            Experience = 0,

            BodyID = RebirthBodyID,
            BodyState = RebirthBodyState,
            BodyR = RebirthBodyR, BodyG = RebirthBodyG, BodyB = RebirthBodyB, BodyA = RebirthBodyA,
            FaceID = RebirthFaceID,
            HairID = RebirthHairID,
            HairR = RebirthHairR, HairG = RebirthHairG, HairB = RebirthHairB, HairA = RebirthHairA,
            EquippedItems = RebirthEquippedItems,

            AlliesString = "",
            Allies = new List<NPCTemplate>(),
            Drops = new List<NPCDropInfo>(),
        };

        keeper.BaseStats = new AttributeSet { HP = 1000, MP = 0 };
        keeper.Quests.Add(quest);

        world.NPCHandler.AddTemplate(keeper);

        var spawned = world.NPCHandler.SpawnNPC(world, RebirthMapId, RebirthX, RebirthY,
                                                keeper, shouldRespawn: false);
        if (spawned == null)
            throw new Exception($"Could not spawn the rebirth keeper: map {RebirthMapId} does not exist.");

        // LoadFromTemplate adds to Map.NPCs and then calls Spawn -> PlaceCharacter
        // (NPC.cs:645-648). PlaceCharacter is the step that silently no-ops out of range,
        // so confirm the keeper actually occupies the tile rather than just being listed.
        if (rebirthMap.GetCharacterAt(RebirthX, RebirthY) != spawned)
        {
            throw new Exception(
                $"Rebirth keeper did not take tile {RebirthMapId}({RebirthX},{RebirthY}) - it would be invisible and untargetable.");
        }
    }

    /// <summary>NPC.java:1019 - darker and more opaque per dimension.</summary>
    private void Recolour(NPCTemplate t, int dim)
    {
        t.HairR = Math.Max(t.HairR - dim * 30, 0);
        t.HairG = Math.Max(t.HairG - dim * 30, 0);
        t.HairB = Math.Max(t.HairB - dim * 30, 0);
        t.HairA = Math.Min(t.HairA + dim * 30, 200);
        t.BodyR = Math.Max(t.BodyR - dim * 30, 0);
        t.BodyG = Math.Max(t.BodyG - dim * 30, 0);
        t.BodyB = Math.Max(t.BodyB - dim * 30, 0);
        t.BodyA = Math.Min(t.BodyA + dim * 30, 200);
    }
}
