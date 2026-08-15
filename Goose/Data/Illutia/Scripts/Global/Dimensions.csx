using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Events;
using Goose.Quests;
using Goose.Scripting;

public class Dimensions : BaseGlobalScript
{
    // ---- Configuration -------------------------------------------------
    public const bool Enabled = true;

    /// <summary>Dimensions above 0. Abyss shipped 6.</summary>
    public const int DimensionCount = 6;

    /// <summary>Dimension n's copy of anything lives at baseId + Offset*n.
    /// Must exceed every base id: Illutia map ids reach 10044, so 10000 is too small.</summary>
    public const int Offset = 100000;

    /// <summary>Map /dimension n warps to.</summary>
    public const int StartMapId = 1;

    /// <summary>NPC template gating each dimension.</summary>
    public const int BossTemplateId = 162;

    /// <summary>Map.java:251-260. A flat floor, not a scale - it discards the base map's
    /// value entirely. Most maps carry MinExperience = 0, so without this the top two
    /// dimensions have no experience gate at all and dimension.max is the sole barrier.</summary>
    public const long Dim5MinExperience = 100_000_000_000;
    public const long Dim6MinExperience = 500_000_000_000;

    // ---- Warden ---------------------------------------------------------
    // The quest giver. It does not exist in sheet data, so everything about it is
    // configured here. One template per dimension at WardenTemplateId + Offset*dim.

    /// <summary>Base id for the generated warden templates. Must not collide with a
    /// sheet-authored npc_id - the script checks and refuses to overwrite.</summary>
    public const int WardenTemplateId = 800000;

    public const string WardenName = "Warden of the Void";
    public const string WardenTitle = "";
    public const string WardenSurname = "";

    /// <summary>Any class works as long as it has a row for WardenLevel. class_info only
    /// carries levels 1-5 for class 1 (Commoner); classes 2-7 carry 1-50. Level 50 on
    /// class 1 makes Class.GetLevel return null and NPC.LoadFromTemplate throws at
    /// NPC.cs:636. The script validates this at startup rather than at spawn time.</summary>
    public const int WardenClassId = 3;      // Warrior
    public const int WardenLevel = 50;

    /// <summary>Appearance. These are the same fields npc_templates carries, so anything
    /// legal for a sheet-authored NPC is legal here.</summary>
    public const int WardenBodyID = 1;
    public const int WardenBodyState = 0;
    public const int WardenBodyR = 40;
    public const int WardenBodyG = 0;
    public const int WardenBodyB = 60;
    public const int WardenBodyA = 200;
    public const int WardenFaceID = 1;
    public const int WardenHairID = 1;
    public const int WardenHairR = 20;
    public const int WardenHairG = 0;
    public const int WardenHairB = 40;
    public const int WardenHairA = 200;

    /// <summary>MKC-string fragment, exactly as npc_templates.equipped_items
    /// (NPCHandler.cs:65, rendered at Packets.cs:161). Empty for no visible equipment.</summary>
    public const string WardenEquippedItems = "";

    /// <summary>Quest-giver placement, per dimension, on that dimension's start map.</summary>
    public const int WardenMapId = StartMapId;
    public const int WardenX = 50;
    public const int WardenY = 50;

    // ---- Rebirth --------------------------------------------------------
    // The spirit faucet: a repeatable quest converting banked experience into spirit and
    // resetting the character. Script-created for the same reason the warden is - the
    // dimensions feature stays self-contained, and Enabled = false leaves nothing behind.

    /// <summary>Clear of WardenTemplateId (800000 + Offset*6 = 1,400,000 is the warden's
    /// top id, but the wardens occupy 800000, 900000, ... so 810000 is unused).</summary>
    public const int RebirthTemplateId = 810000;

    /// <summary>Clear of QuestIdBase's range: quests 900000-900005, requirement and reward
    /// ids 900000 + n*10 + k, topping out at 900051.</summary>
    public const int RebirthQuestId = 910000;

    /// <summary>Experience per spirit. floor(total / ExpPerSpirit) is minted; the
    /// remainder is destroyed, faithful to RebirthEvent.java:47.</summary>
    public const long ExpPerSpirit = 100_000_000;

    public const string RebirthName = "Keeper of Rebirth";
    public const string RebirthTitle = "";
    public const string RebirthSurname = "";
    public const int RebirthClassId = 3;      // must have a class_info row at RebirthLevel
    public const int RebirthLevel = 50;

    /// <summary>Where rebirth *leaves* the player, as opposed to what the keeper looks
    /// like. Only used by CreateRebirthQuest's preflight: Rebirth.csx compiles separately
    /// and cannot read these, so it hardcodes the same 1 and 1 - keep the two in step.</summary>
    public const int RebirthDestinationClassId = 1;   // Commoner
    public const int RebirthDestinationLevel = 1;

    public const int RebirthBodyID = 1;
    public const int RebirthBodyState = 0;
    public const int RebirthBodyR = 40;
    public const int RebirthBodyG = 0;
    public const int RebirthBodyB = 60;
    public const int RebirthBodyA = 200;
    public const int RebirthFaceID = 1;
    public const int RebirthHairID = 1;
    public const int RebirthHairR = 20;
    public const int RebirthHairG = 0;
    public const int RebirthHairB = 40;
    public const int RebirthHairA = 200;
    public const string RebirthEquippedItems = "";

    /// <summary>Dimension 0 only, beside the dimension-0 warden. Map 1 is StartMapId, the
    /// map /dimension already warps to, so a player who can reach a warden can reach the
    /// keeper without a second landmark.
    ///
    /// Verified against Data/Illutia/Maps/Map1.map: the map is 286x194, and (52,50) carries
    /// no blocked flag (bit 2 of the tile flags, Map.cs:471-475). It is two tiles east of
    /// WardenX/WardenY (50,50), so the two generated NPCs cannot collide. Warp tiles and
    /// sheet NPC spawns come from the database rather than the .map file, so
    /// CreateRebirthQuest re-checks the tile at load time instead of trusting this.</summary>
    public const int RebirthMapId = StartMapId;
    public const int RebirthX = 52;
    public const int RebirthY = 50;

    /// <summary>Quest ids are deterministic: QuestProgress persists keyed on
    /// requirement.Id (Player.cs:1020 / QuestWindow.cs:268), so a counter-assigned id
    /// would orphan in-flight kill progress on restart.</summary>
    public const int QuestIdBase = 900000;

    /// <summary>Generated ItemModifier ids. item_surnames/item_titles are sheet data with
    /// small ids; these sit far above so a new sheet row can never collide. The two
    /// dictionaries are separate (ItemHandler.cs:20,21), so the ranges only need to be
    /// distinct from sheet ids, not from each other.</summary>
    public const int SurnameIdBase = 900000;
    public const int TitleIdBase = 900100;

    /// <summary>Registry id for the spirit currency. Dimension items are priced in it;
    /// their Value is already the spirit price (x3^dim, see CloneItemTemplates).</summary>
    public const string SpiritCurrencyId = "spirit";

    /// <summary>Reroll cost is ResetItemCostBase^dim: 3/9/27/81/243/729 spirit
    /// (ResetItemEvent.java:30).</summary>
    public const int ResetItemCostBase = 3;

    public const string MaxDimensionProperty = "dimension.max";

    /// <summary>BuyGoldCommandEvent.java:47 - 1 spirit buys a million gold.</summary>
    public const long GoldPerSpirit = 1_000_000;

    /// <summary>BuyExperienceCommandEvent.java:52. Deliberately below ExpPerSpirit: the
    /// round trip is lossy by 4x, which is what keeps rebirth a net sink.</summary>
    public const long ExpPerSpiritPurchase = 25_000_000;

    /// <summary>Ceiling on a single wallet. BaseStats.SP is a long, so this is not the
    /// type's limit - it is a sanity bound well above anything the faucet can produce
    /// (a trillion spirit is 10^20 experience through rebirth), placed so a transfer
    /// cannot silently wrap a wallet negative and so a bug in the faucet is visible as a
    /// refusal rather than as a corrupted balance.</summary>
    public const long MaxSpiritBalance = 1_000_000_000_000L;

    /// <summary>Shared by all four commands. Returns false for a missing, unparseable,
    /// zero or negative amount - each command prints its own usage line, so this does not
    /// message.</summary>
    public static bool TryParseAmount(string[] tokens, int index, out long amount)
    {
        amount = 0;
        if (tokens.Length <= index) return false;
        if (!long.TryParse(tokens[index], out amount)) return false;

        return amount > 0;
    }

    public override void OnLoaded(GameWorld world)
    {
        if (!Enabled) return;

        CloneTemplates(world);
        RewireAllies(world);
        CloneMaps(world);
        RewireWarps(world);
        CloneSpawns(world);
        CreateUnlockChain(world);
        CreateRebirthQuest(world);

        PreflightSpellIds(world);
        CloneSpellEffects(world);
        RewireSpellEffects(world);
        CloneSpells(world);
        RewriteTeleportEffects(world);

        // Generated surnames/titles are sheet data in ItemHandler; register the abyss ones
        // before the item clones exist so the per-dimension item script (Task 6) can roll them.
        RegisterModifiers(world);

        // Dimension items are priced in spirit, so the currency must exist before
        // CloneItemTemplates stamps it onto the clones. The guard turns a registration
        // failure into a load-time error rather than a till-time one.
        world.CurrencyHandler.Register(new SpiritCurrency());
        if (world.CurrencyHandler.Get(SpiritCurrencyId) == null)
            throw new Exception($"Currency '{SpiritCurrencyId}' failed to register.");

        // After the spell passes: tome clones point at dimension spells, which must exist
        // to be pointed at. Before RepointDrops (Task 7), which needs the item clones.
        CloneItemTemplates(world);
        RepointDrops(world);
        RepointVendorStock(world);

        world.EventHandler.RegisterEvent("/dimension ", DimensionCommandEvent.Create);
        world.EventHandler.RegisterEvent("/resetitem ", ResetItemCommandEvent.Create);
        world.EventHandler.RegisterEvent("/buygold ", BuyGoldCommandEvent.Create);
        world.EventHandler.RegisterEvent("/buyexperience ", BuyExperienceCommandEvent.Create);
        world.EventHandler.RegisterEvent("/givesp ", GiveSpiritCommandEvent.Create);
    }

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

    /// <summary>Second pass over the maps Task 3 created: repoint every warp at the same
    /// dimension's clone. tiles are shared between base and clone (Map.CloneAs is a shallow
    /// copy), so each rewired warp must be a NEW WarpTile - mutating the shared one would
    /// retarget the base map too.</summary>
    private void RewireWarps(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.MapHandler.Maps.Values.Where(m => m.ID < Offset).ToList())
            {
                var clone = world.MapHandler.GetMap(basic.ID + Offset * dim);

                for (int i = 0; i < clone.tiles.Length; i++)
                {
                    var warp = clone.tiles[i] as WarpTile;
                    if (warp == null) continue;

                    var target = warp.WarpMap == null
                        ? null
                        : world.MapHandler.GetMap(warp.WarpMap.ID + Offset * dim);

                    // A warp whose target has no clone stays pointed at the base map - it is
                    // an exit from the dimension rather than a broken link.
                    clone.tiles[i] = new WarpTile
                    {
                        WarpMap = target ?? warp.WarpMap,
                        WarpX = warp.WarpX,
                        WarpY = warp.WarpY,
                    };
                }
            }
        }
    }

    /// <summary>Map.java:251-260. Dimensions 1-4 scale; 5 and 6 take a flat floor that
    /// ignores the base value.</summary>
    private long MinExperienceFor(long baseMin, int dim)
    {
        if (dim == 5) return Dim5MinExperience;
        if (dim >= 6) return Dim6MinExperience;

        return baseMin * (dim * 5) * (dim * 5);
    }

    private void CloneMaps(GameWorld world)
    {
        var baseMaps = world.MapHandler.Maps.Values.ToList();
        var mapScript = world.ScriptHandler.GetScript<IMapScript>("Scripts/Map/DimensionMap.csx");

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseMaps)
            {
                int id = basic.ID + Offset * dim;

                // MapHandler.Maps is a plain dictionary - a collision would silently replace a
                // real map and strand whoever is standing on it.
                if (world.MapHandler.GetMap(id) != null)
                    throw new Exception($"Dimension map id {id} (base {basic.ID}, dim {dim}) already exists. "
                                        + "Offset is too small for this data set.");

                // Map.CloneAs (Part 1 task 5) carries everything across, including the private
                // requiredItems list and Muted. Rebuilding public fields here instead would
                // drop item-gated entry on every dimension copy of a key-gated map.
                var clone = basic.CloneAs(id, basic.Name + " (" + dim + ")");

                clone.CanPVP = true;                      // forced on in every dimension
                // Entry gates scale by (dim*5)^2; dimensions 5-6 take a flat floor instead
                clone.MinExperience = MinExperienceFor(basic.MinExperience, dim);
                clone.MaxExperience = basic.MaxExperience * (dim * 5) * (dim * 5);
                clone.Script = mapScript;                 // replaces, not composes - DimensionMap forwards to the base script itself
                // ScriptParams passes through untouched so a delegated base script reads the
                // params it was written against. DimensionMap takes its dimension from the
                // map id, which already encodes it.
                clone.ScriptParams = basic.ScriptParams;

                world.MapHandler.Maps[clone.ID] = clone;

                // MapHandler.LoadMaps:78 schedules one of these per map; clones need it too
                // or dropped items never sweep off the ground.
                Event sweep = new ClearMapItemsEvent();
                sweep.Ticks += world.TimerFrequency * GameWorld.Settings.ItemGroundSweepTime;
                sweep.Data = clone;
                world.EventHandler.AddEvent(sweep);
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

        var rewardScript = world.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/DimensionUnlock.csx");

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

        var rebirthScript = world.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/Rebirth.csx");

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

    // ---- Item pass --------------------------------------------------------

    /// <summary>Abyss suffix names, in the band order of Item.java:363-387.</summary>
    private static readonly string[] SurnameNames =
    {
        "of Vita Regen", "of Mana Regen", "of Criticality",
        "of Spell Damage", "of Reduction", "of Speed",
    };

    /// <summary>Registers the eight dimension modifiers. All at Chance 0: RollModifier
    /// (ItemHandler.cs:270) sizes each modifier's selection range as (int)(Chance * 100),
    /// so zero yields an empty range and these can never land on dimension-0 loot. The
    /// dimension script selects them explicitly by id.</summary>
    private void RegisterModifiers(GameWorld world)
    {
        var surnameScript = world.ScriptHandler.GetScript<IItemModifierScript>(
            "Scripts/Item/DimensionSurname.csx");

        for (int i = 0; i < SurnameNames.Length; i++)
        {
            world.ItemHandler.AddSurname(new ItemModifier
            {
                Id = SurnameIdBase + i,
                Name = SurnameNames[i],
                Chance = 0,
                Slot = ItemTemplate.ItemSlots.Misc,   // ModifierAppliesToItem treats Misc as "any slot"
                Script = surnameScript,
                ScriptParams = i.ToString(),
            });
        }

        var rarityScript = world.ScriptHandler.GetScript<IItemModifierScript>(
            "Scripts/Item/DimensionRarity.csx");

        world.ItemHandler.AddTitle(new ItemModifier
        {
            Id = TitleIdBase, Name = "Legendary", Chance = 0,
            Slot = ItemTemplate.ItemSlots.Misc,
            Script = rarityScript, ScriptParams = "1.25",
        });
        world.ItemHandler.AddTitle(new ItemModifier
        {
            Id = TitleIdBase + 1, Name = "Stunted", Chance = 0,
            Slot = ItemTemplate.ItemSlots.Misc,
            Script = rarityScript, ScriptParams = "0.5",
        });
    }

    /// <summary>Equipment and spell tomes get a copy per dimension. Consumables never scale
    /// in abyss (Item.java:404); money and NoUse items have nothing to scale.</summary>
    private bool ShouldClone(ItemTemplate t)
    {
        return t.UseType == ItemTemplate.UseTypes.Armor
            || t.UseType == ItemTemplate.UseTypes.Weapon
            || (t.UseType == ItemTemplate.UseTypes.Scroll && t.LearnSpellID > 0);
    }

    private void CloneItemTemplates(GameWorld world)
    {
        // Snapshot first: AddTemplate mutates the dictionary GetTemplates() enumerates
        // (ItemHandler.cs:42 hands back the live values collection).
        var baseTemplates = world.ItemHandler.GetTemplates()
            .Where(t => t.ID < Offset && ShouldClone(t)).ToList();

        // One shared script for every clone - ScriptHandler caches by path
        // (ScriptHandler.cs:24), and DimensionItem recovers its dimension from each
        // item, so a single stateless instance serves all of them.
        var itemScript = world.ScriptHandler.GetScript<IItemScript>("Scripts/Item/DimensionItem.csx");

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseTemplates)
            {
                int id = basic.ID + Offset * dim;

                // AddTemplate overwrites silently, so a collision would quietly replace a
                // real item and change what every stored Item with that id resolves to.
                if (world.ItemHandler.GetTemplate(id) != null)
                    throw new Exception($"Dimension item template id {id} (base {basic.ID}, dim {dim}) "
                                        + "already exists. Offset is too small for this data set.");

                world.ItemHandler.AddTemplate(ScaleItemTemplate(world, basic, dim, itemScript));
            }
        }
    }

    private ItemTemplate ScaleItemTemplate(GameWorld world, ItemTemplate basic, int dim, Script<IItemScript> itemScript)
    {
        var clone = new ItemTemplate(basic)
        {
            ID = basic.ID + Offset * dim,
            Name = DimensionPrefixes[dim] + basic.Name,
            Description = "Abyss (" + dim + ") " + basic.Description,

            // Replaces the base script rather than composing with it - DimensionItem.csx
            // forwards to the base template's script itself, so nothing is lost.
            Script = itemScript,

            // Item.java:441-444
            GraphicR = Math.Max(basic.GraphicR - 30 * dim, 0),
            GraphicG = Math.Max(basic.GraphicG - 30 * dim, 0),
            GraphicB = Math.Max(basic.GraphicB - 30 * dim, 0),
            GraphicA = Math.Min(basic.GraphicA + 30 * dim, 200),

            // Item.java:445. This is the spirit price. CurrencyId stamps the clones as
            // spirit-priced (below), and CurrencyHandler.Resolve makes that override win
            // at every vendor, so this value is never read as gold.
            Value = (long)(basic.Value * Math.Pow(3, dim)),

            // Dimension items are priced in spirit wherever they are traded. The currency
            // is registered just above in OnLoaded, so stamping can validate against it.
            CurrencyId = SpiritCurrencyId,

            // Item.java:225-260 - dimension gear is freely tradeable.
            IsLore = false,
            IsBindOnPickup = false,
            IsBindOnEquip = false,
        };

        // Equipment only. AttributeSet.java:380-382 returns an empty set for anything that
        // is not equipment, so a tome must not pick up AC, attributes, HP/MP, resistances
        // or melee damage. Most of it would be inert on a consumable, but the generated
        // data would still be wrong - and it renders in the item window (Packets.cs:443).
        if (basic.UseType == ItemTemplate.UseTypes.Armor || basic.UseType == ItemTemplate.UseTypes.Weapon)
            clone.BaseStats += DimensionStats(basic, dim);

        // Spell tomes: teach the dimension's copy of the spell, and become consumables so
        // DimensionItem.csx can implement the upgrade rule. Inventory.cs:277 learns Scroll
        // items directly with no script hook; Inventory.cs:423 gives OneTime items one.
        //
        // A spell with no dimension clone (PreflightSpellIds can skip ids) keeps its base
        // id and stays a plain Scroll - a tome pointing at a nonexistent spell would fail
        // silently at Spellbook.cs:203.
        if (basic.UseType == ItemTemplate.UseTypes.Scroll
            && world.SpellHandler.GetSpell(basic.LearnSpellID + Offset * dim) != null)
        {
            clone.UseType = ItemTemplate.UseTypes.OneTime;
            clone.LearnSpellID = basic.LearnSpellID + Offset * dim;
        }

        return clone;
    }

    /// <summary>Points each dimension NPC's drops at that dimension's item templates.
    /// Items with no clone - gold, consumables, quest tokens - keep the base template.
    ///
    /// Every entry is a NEW NPCDropInfo: NPCTemplate's copy constructor copies the list but
    /// shares its elements (NPCTemplate.cs:251), so mutating one in place would retarget the
    /// base template's drop table and every other dimension's along with it.</summary>
    private void RepointDrops(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.NPCHandler.GetTemplates()
                                       .Where(t => t.NPCTemplateID < Offset).ToList())
            {
                var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
                if (clone == null || basic.Drops == null) continue;

                var drops = new List<NPCDropInfo>();
                foreach (var drop in basic.Drops)
                {
                    var dimTemplate = world.ItemHandler.GetTemplate(drop.ItemTemplate.ID + Offset * dim);

                    drops.Add(new NPCDropInfo
                    {
                        ItemTemplate = dimTemplate ?? drop.ItemTemplate,
                        DropRate = drop.DropRate,
                        Stack = drop.Stack,
                    });
                }

                clone.Drops = drops;
            }
        }
    }

    /// <summary>Point each dimension vendor's stock at that dimension's item clones.
    ///
    /// New array AND new slot objects, never an in-place edit: NPCTemplate's copy
    /// constructor shares VendorItems with the base template (NPCTemplate.cs:254), so
    /// mutating either would rewrite dimension 0's shops. Same rule as RepointDrops.
    ///
    /// No vendor-side CurrencyId is set. The clones carry CurrencyId = "spirit" on the
    /// item, and Resolve puts the item override above the vendor (CurrencyHandler.cs:41),
    /// so repointed gear sells for spirit while unrepointed consumables stay gold.</summary>
    private void RepointVendorStock(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.NPCHandler.GetTemplates()
                                       .Where(t => t.NPCTemplateID < Offset).ToList())
            {
                var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
                if (clone == null || basic.VendorItems == null) continue;

                var slots = new NPCVendorSlot[basic.VendorItems.Length];
                for (int i = 0; i < basic.VendorItems.Length; i++)
                {
                    var slot = basic.VendorItems[i];
                    if (slot == null) continue;

                    var dimTemplate = slot.ItemTemplate == null
                        ? null
                        : world.ItemHandler.GetTemplate(slot.ItemTemplate.ID + Offset * dim);

                    slots[i] = new NPCVendorSlot
                    {
                        Slot = slot.Slot,
                        ItemTemplate = dimTemplate ?? slot.ItemTemplate,
                        Stack = slot.Stack,
                        CanSeeStats = slot.CanSeeStats,
                    };
                }

                clone.VendorItems = slots;
            }
        }
    }

    /// <summary>AttributeSet.java:376, with itemType 0 - the flat per-dimension bonus only.
    /// The six suffix-specific terms live in DimensionSurname.csx, applied at roll time.
    ///
    /// Callers must apply this to equipment only: abyss returns an empty set for every
    /// other use type (AttributeSet.java:380-382). ScaleItemTemplate holds that guard.
    ///
    /// Baking this into the template rather than adding it per item is equivalent: abyss
    /// computes (template + item + dimensionDefault) * StatMultiplier (Item.java:459), and
    /// goose computes (template + item) * StatMultiplier (Item.cs:247), so folding it into
    /// the template leaves Legendary/Stunted multiplying the same total.</summary>
    private AttributeSet DimensionStats(ItemTemplate basic, int dim)
    {
        var a1 = basic.BaseStats;
        double tier = Tier(basic);
        double half = 0.5 * dim;

        return new AttributeSet
        {
            AC = (int)(a1.AC * half + 10 * dim * tier),
            AirResist = (int)(a1.AirResist * half + 10 * dim * tier),
            EarthResist = (int)(a1.EarthResist * half + 10 * dim * tier),
            FireResist = (int)(a1.FireResist * half + 10 * dim * tier),
            WaterResist = (int)(a1.WaterResist * half + 10 * dim * tier),
            SpiritResist = (int)(a1.SpiritResist * half + 10 * dim * tier),
            Dexterity = (int)(a1.Dexterity * half + 15 * dim * tier),
            Stamina = (int)(a1.Stamina * half + 100 * dim * tier),
            Intelligence = (int)(a1.Intelligence * half + 100 * dim * tier),
            Strength = (int)(a1.Strength * half + 100 * dim * tier),

            HP = (long)(a1.HP * dim + Math.Pow(10 * dim, 4) * tier),
            MP = (long)(a1.MP * dim + Math.Pow(10 * dim, 4) * tier),

            DamageReduction = a1.DamageReduction * (decimal)half,
            Haste = a1.Haste * (decimal)half,
            SpellCrit = a1.SpellCrit * (decimal)half,
            SpellDamage = a1.SpellDamage * (decimal)half,
            HPPercentRegen = a1.HPPercentRegen * (decimal)half,
            MPPercentRegen = a1.MPPercentRegen * (decimal)half,
            HPStaticRegen = (int)(a1.HPStaticRegen * half),
            MPStaticRegen = (int)(a1.MPStaticRegen * half),

            // AttributeSet.java:433 casts the whole term to int. Ported faithfully, cast
            // included: the flat 10*dim*tier term dominates, and any base MeleeDamage
            // product below 1.0 truncates to nothing. MeleeDamage is a fraction on both
            // servers - damage *= (1 + MeleeDamage) at Player.java:316 and Player.cs:1616 -
            // so this is a very large bonus by design. User decision, 2026-08-10.
            MeleeDamage = (int)((double)a1.MeleeDamage * dim + 10 * dim * tier),
        };
    }

    /// <summary>AttributeSet.java:405-419. Abyss's top tier (1.5) keys off an SP-priced
    /// template; goose has no SP value, so that tier has no equivalent and is dropped.
    /// Computed from the BASE template - the clone's value is already scaled by 3^dim and
    /// would put every clone in the top tier.</summary>
    private double Tier(ItemTemplate basic)
    {
        if (basic.Value >= 10000000) return 1.0;
        if (basic.MinExperience > 0) return 0.75;
        if (basic.MinLevel == 50) return 0.5;
        return 0.25;
    }

    // ---- Spell pass -------------------------------------------------------

    /// <summary>Validates every id the spell pass depends on BEFORE anything is registered.
    ///
    /// Two reasons this is a preflight rather than a check inside each clone loop. First, a bad
    /// id found halfway through leaves the handler half-mutated - thousands of generated effects
    /// registered, spells not, cross-references unwired - which is a worse thing to hand someone
    /// than a clean refusal. Second, "base" means ID &lt; Offset everywhere downstream
    /// (RewireSpellEffects filters on exactly that), so a base id at or above the offset is not
    /// merely a collision risk: it would be cloned here and then skipped during rewiring, and
    /// the result would be a spell that exists but never stacks or resolves correctly.</summary>
    private void PreflightSpellIds(GameWorld world)
    {
        var effects = world.SpellHandler.GetSpellEffects().ToList();
        var spells = world.SpellHandler.GetSpells().ToList();

        foreach (var effect in effects)
            if (effect.ID < 0 || effect.ID >= Offset)
                throw new Exception($"Spell effect id {effect.ID} is outside the base range "
                    + $"0..{Offset - 1}. Dimension cloning keys on id + {Offset} * dimension, so "
                    + "every sheet id must fit below the offset. Raise Offset or fix the data.");

        foreach (var spell in spells)
            if (spell.ID < 0 || spell.ID >= Offset)
                throw new Exception($"Spell id {spell.ID} is outside the base range "
                    + $"0..{Offset - 1}. Dimension cloning keys on id + {Offset} * dimension, so "
                    + "every sheet id must fit below the offset. Raise Offset or fix the data.");

        // Backstop. Unreachable once the range checks above pass - id + Offset*dim is injective
        // over 0..Offset-1 x 1..DimensionCount - but AddSpell/AddSpellEffect overwrite silently,
        // so the failure this would catch is a real spell vanishing without a trace.
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var effect in effects)
                if (world.SpellHandler.GetSpellEffect(effect.ID + Offset * dim) != null)
                    throw new Exception($"Dimension spell effect id {effect.ID + Offset * dim} "
                        + $"(base {effect.ID}, dim {dim}) already exists.");

            foreach (var spell in spells)
                if (world.SpellHandler.GetSpell(spell.ID + Offset * dim) != null)
                    throw new Exception($"Dimension spell id {spell.ID + Offset * dim} "
                        + $"(base {spell.ID}, dim {dim}) already exists.");
        }
    }

    /// <summary>Dimension name prefixes. SpellHandler.java:235.</summary>
    private static readonly string[] DimensionPrefixes =
    {
        "", "Powerful ", "Super Powerful ", "Supreme ", "Omnipotent ", "Almighty ", "Godly ",
    };

    private string PrefixFor(int dim)
    {
        return dim >= 0 && dim < DimensionPrefixes.Length ? DimensionPrefixes[dim] : "";
    }

    /// <summary>SpellHandler.java:226.</summary>
    private string DescriptionPrefixFor(int dim)
    {
        return dim > 0 ? "Abyss (" + dim + ") " : "";
    }

    /// <summary>Step 1 of the spell pass: one scaled copy of every effect per dimension.
    /// Cross-references are left pointing at dimension-0 effects here and rewired by
    /// RewireSpellEffects - clone order is dictionary order, so a referenced effect's clone
    /// may not exist yet at the moment this runs.</summary>
    private void CloneSpellEffects(GameWorld world)
    {
        // Snapshot: AddSpellEffect mutates the dictionary GetSpellEffects() enumerates.
        var baseEffects = world.SpellHandler.GetSpellEffects().ToList();

        // No collision guard here - PreflightSpellIds already proved every id in this loop is
        // free, before the first registration. Do not re-add one: a throw from inside this loop
        // is exactly the half-mutated handler the preflight exists to avoid.
        for (int dim = 1; dim <= DimensionCount; dim++)
            foreach (var basic in baseEffects)
                world.SpellHandler.AddSpellEffect(ScaleSpellEffect(basic, dim));
    }

    /// <summary>Step 2 of the spell pass. Two jobs, both of which need every clone to exist:
    /// repoint each clone's melee-reaction references at its own dimension, and build the
    /// dimension ladder on the buff stacking lists.</summary>
    private void RewireSpellEffects(GameWorld world)
    {
        var baseEffects = world.SpellHandler.GetSpellEffects()
                               .Where(e => e.ID < Offset).ToList();

        // Snapshot the base lists before touching anything: the dim-0 pass below rewrites the
        // base effect's own BuffDoesntStackOver, and later dimensions must still read the
        // original list.
        var baseStacksOver = baseEffects.ToDictionary(e => e.ID, e => e.BuffStacksOver.ToList());
        var baseDoesntStackOver = baseEffects.ToDictionary(e => e.ID, e => e.BuffDoesntStackOver.ToList());

        // Melee reactions: dimension copies only. The base effect keeps what it loaded.
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseEffects)
            {
                var clone = world.SpellHandler.GetSpellEffect(basic.ID + Offset * dim);
                if (clone == null) continue;

                // A reference with no clone is dropped, not left pointing across dimensions -
                // same rule RewireAllies applies to NPC allies.
                clone.OnMeleeAttackSpell = world.SpellHandler.GetSpellEffect(
                    basic.OnMeleeAttackSpellID + Offset * dim);
                clone.OnMeleeAttackSpellID = clone.OnMeleeAttackSpell == null
                    ? 0 : clone.OnMeleeAttackSpell.ID;

                clone.OnMeleeHitSpell = world.SpellHandler.GetSpellEffect(
                    basic.OnMeleeHitSpellID + Offset * dim);
                clone.OnMeleeHitSpellID = clone.OnMeleeHitSpell == null
                    ? 0 : clone.OnMeleeHitSpell.ID;
            }
        }

        // The stacking ladder covers dimension 0 as well.
        for (int dim = 0; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseEffects)
            {
                var effect = world.SpellHandler.GetSpellEffect(basic.ID + Offset * dim);
                if (effect == null) continue;

                // Everything this effect supersedes, plus itself. Split by dimension: copies at
                // or below this one are stacked over, copies above it refuse the cast. Splitting
                // the SAME set both ways is what guarantees no copy lands in neither list.
                var superseded = baseStacksOver[basic.ID].Concat(new[] { basic }).ToList();

                var stacks = new List<SpellEffect>();
                foreach (var entry in superseded)
                    for (int k = 0; k <= dim; k++)
                        AddEffectIfPresent(world, stacks, entry.ID + Offset * k);

                var doesnt = new List<SpellEffect>();

                // Explicit "never stacks" entries lose at every dimension, both directions.
                foreach (var entry in baseDoesntStackOver[basic.ID])
                    for (int k = 0; k <= DimensionCount; k++)
                        AddEffectIfPresent(world, doesnt, entry.ID + Offset * k);

                // And the upper half of the ladder. This covers the whole superseded set, not
                // just the effect itself: a dim-3 Bless meeting a dim-5 MINOR Bless is in neither
                // list otherwise, and both stat blocks apply at once.
                foreach (var entry in superseded)
                    for (int k = dim + 1; k <= DimensionCount; k++)
                        AddEffectIfPresent(world, doesnt, entry.ID + Offset * k);

                effect.BuffStacksOver = stacks;
                effect.BuffDoesntStackOver = doesnt;

                // Keep the string forms consistent. Nothing re-parses them after load, but a
                // divergent string is a trap for anyone debugging from a dump - same reasoning
                // as AlliesString in RewireAllies.
                effect.BuffStacksOverString = string.Join(" ", stacks.Select(e => e.ID));
                effect.BuffDoesntStackOverString = string.Join(" ", doesnt.Select(e => e.ID));
            }
        }
    }

    private void AddEffectIfPresent(GameWorld world, List<SpellEffect> into, int id)
    {
        var effect = world.SpellHandler.GetSpellEffect(id);
        if (effect != null && !into.Contains(effect)) into.Add(effect);
    }

    /// <summary>Step 3 of the spell pass. Runs after RewireSpellEffects so every effect clone
    /// exists and is fully wired before a spell points at one.</summary>
    private void CloneSpells(GameWorld world)
    {
        var baseSpells = world.SpellHandler.GetSpells().ToList();

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseSpells)
            {
                int id = basic.ID + Offset * dim;

                if (world.SpellHandler.GetSpell(id) != null)
                    throw new Exception($"Dimension spell id {id} (base {basic.ID}, dim {dim}) "
                                        + "already exists. Offset is too small for this data set.");

                var effect = world.SpellHandler.GetSpellEffect(basic.SpellEffectID + Offset * dim);

                // LoadSpells drops a spell whose effect is missing (SpellHandler.cs:250); do
                // the same rather than registering a spell that cannot be cast.
                if (effect == null) continue;

                world.SpellHandler.AddSpell(new Spell(basic)
                {
                    ID = id,
                    Name = PrefixFor(dim) + basic.Name,
                    Description = DescriptionPrefixFor(dim) + basic.Description,
                    Aether = (long)(basic.Aether * Math.Pow(0.9, dim)),          // SpellHandler.java:279
                    HPStaticCost = (int)(basic.HPStaticCost * Math.Pow(3, dim)), // :280
                    MPStaticCost = (int)(basic.MPStaticCost * Math.Pow(3, dim)), // :281
                    SpellEffectID = effect.ID,
                    SpellEffect = effect,
                });
            }
        }
    }

    /// <summary>Step 4 of the spell pass, and the last thing the spell work does. Every
    /// teleport effect - dimension 0 included - becomes a script effect so its destination
    /// resolves in the caster's dimension.
    ///
    /// Dimension 0 is deliberate: class level-up spells stay at dimension 0, so that copy is
    /// the teleport every player actually holds. Skipping it would leave a way out of any
    /// dimension.
    ///
    /// Runs after CloneSpellEffects so the clones were still Teleport-typed when they were
    /// copied, and one pass here converts base and clones together.</summary>
    private void RewriteTeleportEffects(GameWorld world)
    {
        var script = world.ScriptHandler.GetScript<ISpellEffectScript>("Scripts/Spell/DimensionTeleport.csx");

        foreach (var effect in world.SpellHandler.GetSpellEffects().ToList())
        {
            if (effect.EffectType != SpellEffect.EffectTypes.Teleport) continue;

            effect.EffectType = SpellEffect.EffectTypes.Script;
            effect.Script = script;
            effect.ScriptParams = Offset.ToString();
        }
    }

    /// <summary>SpellHandler.java:288-330, applied in abyss's order - the formula wrap reads
    /// TargetType before the shape morph rewrites it.</summary>
    private SpellEffect ScaleSpellEffect(SpellEffect basic, int dim)
    {
        var clone = new SpellEffect(basic)
        {
            ID = basic.ID + Offset * dim,
            Name = PrefixFor(dim) + basic.Name,
            Duration = (long)(basic.Duration * Math.Pow(1.15, dim)),
            TargetSize = basic.TargetSize + dim,
        };

        // SpellHandler.java:290-294
        clone.MinimumLevelEffected =
            (basic.EffectType == SpellEffect.EffectTypes.Buff ||
             basic.EffectType == SpellEffect.EffectTypes.Permanent) ? 50 : 1;

        // SpellHandler.java:298
        if (basic.TauntAggro > 0)
            clone.TauntAggro = (long)(basic.TauntAggro * Math.Pow(3, dim) + 100000 * Math.Pow(20, dim));

        ScaleBuffStats(clone.Stats, dim);

        // SpellHandler.java:307-308, then :310-328. Order matters: targetScale comes from the
        // ORIGINAL target type, before the morph below rewrites it.
        clone.HPFormula = ScaleFormula(basic.HPFormula, basic.TargetType, dim);
        clone.MPFormula = ScaleFormula(basic.MPFormula, basic.TargetType, dim);

        MorphTargetShape(clone, basic.TargetType, basic.TargetSize, dim);

        return clone;
    }

    /// <summary>AttributeSet.java:347. The set is already a clone (SpellEffect copy
    /// constructor), so fields abyss omits keep their base value instead of being zeroed -
    /// notably MoveSpeed and SP. Deliberate deviation, see the design doc.</summary>
    private void ScaleBuffStats(AttributeSet stats, int dim)
    {
        decimal linear = 1m + 0.5m * dim;

        stats.HP = stats.HP * (dim + 1) * (dim + 1);
        stats.MP = stats.MP * (dim + 1) * (dim + 1);

        stats.HPStaticRegen = (int)(stats.HPStaticRegen * Math.Pow(4, dim));
        stats.MPStaticRegen = (int)(stats.MPStaticRegen * Math.Pow(4, dim));

        stats.AC = (int)(stats.AC * linear);
        stats.DamageReduction *= linear;
        stats.Haste *= linear;
        stats.HPPercentRegen *= linear;
        stats.MPPercentRegen *= linear;
        stats.MeleeCrit *= linear;
        stats.MeleeDamage *= linear;
        stats.SpellCrit *= linear;
        stats.SpellDamage *= linear;

        stats.FireResist *= dim;
        stats.AirResist *= dim;
        stats.EarthResist *= dim;
        stats.WaterResist *= dim;
        stats.SpiritResist *= dim;
        stats.Strength *= dim;
        stats.Stamina *= dim;
        stats.Intelligence *= dim;
        stats.Dexterity *= dim;
    }

    /// <summary>SpellHandler.java:260. Single-target spells get an extra 1.15.
    ///
    /// InvariantCulture is required: ParseFormula reads literals with Convert.ToDecimal and no
    /// format provider (SpellEffect.cs:1311), and shipped sheet data already uses '.' as the
    /// separator ("0.10 * %ccmp"), so '.' is the convention the parser is fed everywhere.</summary>
    private string ScaleFormula(string formula, SpellEffect.TargetTypes targetType, int dim)
    {
        if (string.IsNullOrEmpty(formula)) return formula;

        double targetScale = targetType == SpellEffect.TargetTypes.Target ? 1.15 : 1.0;
        double multiplier = targetScale * Math.Pow(1.25, dim);

        return "(" + formula + ") * "
               + multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>SpellHandler.java:310-328. Small shapes grow into bigger ones.</summary>
    private void MorphTargetShape(SpellEffect clone, SpellEffect.TargetTypes targetType, int baseSize, int dim)
    {
        if (targetType == SpellEffect.TargetTypes.Cross || targetType == SpellEffect.TargetTypes.Plus)
        {
            clone.TargetSize = dim;
            clone.TargetType = SpellEffect.TargetTypes.Area;
        }
        else if (targetType == SpellEffect.TargetTypes.LineFront)
        {
            clone.TargetSize = baseSize == 3 ? dim + 1 : dim;
            clone.TargetType = baseSize <= 1
                ? SpellEffect.TargetTypes.Plus
                : SpellEffect.TargetTypes.TriangleFront;
        }
    }
}

/// <summary>Handles "/dimension <n>". Registered with a trailing space so the command
/// trie matches it as a longest-prefix, exactly like "/tell " and "/warp "
/// (EventHandler.cs:123).</summary>
public class DimensionCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new DimensionCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        int dim;
        if (tokens.Length < 2 || !int.TryParse(tokens[1], out dim) || dim < 0 || dim > Dimensions.DimensionCount)
        {
            world.Send(this.Player, P.ServerMessage("/dimension <0-" + Dimensions.DimensionCount + ">"));
            return;
        }

        int max = this.Player.Properties.GetProperty<int>(Dimensions.MaxDimensionProperty, 0);
        if (dim > max)
        {
            world.Send(this.Player, P.ServerMessage(
                "The void has rejected you. You have a maximum dimension of " + max + "."));
            return;
        }

        var target = world.MapHandler.GetMap(Dimensions.StartMapId + Dimensions.Offset * dim);
        if (target == null)
        {
            world.Send(this.Player, P.ServerMessage("That dimension does not exist."));
            return;
        }

        // PlayerCanJoin, then WarpTo. Player.WarpTo (Player.cs:1234) does no gating of its
        // own - MoveEvent (:123), SpellEffect (:831) and DimensionTeleport.csx (:61) each
        // call PlayerCanJoin first, and this command has to as well or every map-level
        // gate in this feature (MinLevel, Min/MaxExperience, required items, and
        // DimensionMap.csx's own hook) is bypassed by the one route players actually use.
        //
        // PlayerCanJoin sends its own refusal, so there is nothing to say here.
        if (!target.PlayerCanJoin(this.Player, world)) return;

        this.Player.WarpTo(world, target, Dimensions.WardenX, Dimensions.WardenY);
    }
}

/// <summary>Handles "/resetitem &lt;n&gt;": rerolls one dimension equipment item's suffix
/// and rarity for ResetItemCostBase^dim spirit. Registered with a trailing space so the
/// command trie matches it as a longest-prefix, exactly like "/dimension ".</summary>
public class ResetItemCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new ResetItemCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        int slotId;
        if (tokens.Length < 2 || !int.TryParse(tokens[1], out slotId) ||
            slotId < 1 || slotId > GameWorld.Settings.InventorySize)
        {
            world.Send(this.Player, P.ServerMessage(
                "/resetitem <1-" + GameWorld.Settings.InventorySize + "> - rerolls a dimension item's suffix."));
            return;
        }

        var slot = this.Player.Inventory.GetSlot(slotId);
        if (slot == null || slot.Item == null)
        {
            world.Send(this.Player, P.ServerMessage("No item exists at that inventory slot."));
            return;
        }

        var item = slot.Item;

        // One Item object backs the whole stack (ItemSlot.cs:17-19), so rerolling a stack
        // of two would rewrite both for one charge. Refuse rather than split.
        if (slot.Stack != 1)
        {
            world.Send(this.Player, P.ServerMessage("Only a single item can be reset, not a stack."));
            return;
        }

        // Three separate questions, and all three have to be asked. The division alone
        // says nothing: a sheet-authored template with an id above Offset would divide to a
        // plausible-looking dimension, be priced with Math.Pow against a dimension that may
        // not exist, and be handed to a reroll hook that knows nothing about it.
        int dim = item.TemplateID / Dimensions.Offset;
        if (dim < 1 || dim > Dimensions.DimensionCount)
        {
            world.Send(this.Player, P.ServerMessage("Only items from a higher plane can be reset."));
            return;
        }

        // CloneItemTemplates registers each clone at baseId + Offset*dim over a base that
        // exists, and stamps the dimension script onto it. All three must hold, or this is
        // not a generated clone and does not belong here.
        var registered = world.ItemHandler.GetTemplate(item.TemplateID);
        if (registered == null || registered != item.Template ||
            world.ItemHandler.GetTemplate(item.TemplateID % Dimensions.Offset) == null ||
            registered.Script == null)
        {
            world.Send(this.Player, P.ServerMessage("Only items from a higher plane can be reset."));
            return;
        }

        // Dimension tomes are Scroll consumables; nothing but gear carries modifiers.
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
        {
            world.Send(this.Player, P.ServerMessage("Only weapons and armor can be reset."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long cost = (long)Math.Pow(Dimensions.ResetItemCostBase, dim);

        // The balance check is the guard, not a nicety: Part 5 established that Remove
        // does not itself refuse an overdraft.
        long before = spirit.GetBalance(this.Player);
        if (before < cost)
        {
            world.Send(this.Player, P.ServerMessage(
                "Not enough " + spirit.Name + " to reset this item. (" + cost + ")"));
            return;
        }

        world.ItemHandler.RerollModifiers(item, world);
        spirit.Remove(this.Player, cost, world);

        this.Player.Inventory.SendSlot(slotId, world);
        world.Send(this.Player, P.ServerMessage(
            "You spend " + cost + " " + spirit.Name + " to remake " + item.Name + "."));

        // Its own log type, not CreatedCustom: that is the GM item-creation log, and
        // folding a paid player reroll into it makes both unqueryable. otherid carries the
        // item's id so a reroll can be joined to the item it rewrote.
        world.LogHandler.Log(Log.Types.ResetItem, this.Player,
            "ResetItem: template " + item.TemplateID + " dim " + dim
            + " cost " + cost + " " + spirit.ShortName
            + " balance " + before + " -> " + (before - cost),
            item.ItemID);
    }
}

/// <summary>Handles "/buygold &lt;amount&gt;": trades spirit for gold at GoldPerSpirit
/// each. Registered with a trailing space so the command trie matches it as a
/// longest-prefix, exactly like "/dimension ".</summary>
public class BuyGoldCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new BuyGoldCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (!Dimensions.TryParseAmount(tokens, 1, out amount))
        {
            world.Send(this.Player, P.ServerMessage(
                "/buygold <amount> - trades spirit for gold at "
                + Dimensions.GoldPerSpirit.ToString("N0") + " each."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        var gold = world.CurrencyHandler.Get(Currency.Gold);   // no CurrencyHandler.Gold property
        if (spirit == null || gold == null) return;

        // Before the balance check: a wrapped product would pass any check made after it.
        if (amount > long.MaxValue / Dimensions.GoldPerSpirit)
        {
            world.Send(this.Player, P.ServerMessage("That is more gold than exists."));
            return;
        }

        long before = spirit.GetBalance(this.Player);
        if (before < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        long granted = amount * Dimensions.GoldPerSpirit;

        spirit.Remove(this.Player, amount, world);
        gold.Add(this.Player, granted, world);

        world.Send(this.Player, P.ServerMessage(
            "You trade " + amount + " " + spirit.Name + " for " + granted.ToString("N0") + " gold."));
        world.LogHandler.Log(Log.Types.BuyGold, this.Player,
            "BuyGold: " + amount + " " + spirit.ShortName + " -> " + granted + " gold"
            + ", spirit " + before + " -> " + (before - amount));
    }
}

/// <summary>Handles "/buyexperience &lt;amount&gt;": buys experience at
/// ExpPerSpiritPurchase each, unmodified by the world's experience modifier. Registered
/// with a trailing space so the command trie matches it as a longest-prefix, exactly like
/// "/dimension ".</summary>
public class BuyExperienceCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new BuyExperienceCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (!Dimensions.TryParseAmount(tokens, 1, out amount))
        {
            world.Send(this.Player, P.ServerMessage(
                "/buyexperience <amount> - buys experience at "
                + Dimensions.ExpPerSpiritPurchase.ToString("N0") + " each."));
            return;
        }

        if (this.Player.ClassID == 1)
        {
            world.Send(this.Player, P.ServerMessage("Choose a class before you buy experience."));
            return;
        }

        if (amount > long.MaxValue / Dimensions.ExpPerSpiritPurchase)
        {
            world.Send(this.Player, P.ServerMessage("That is more experience than exists."));
            return;
        }

        long granted = amount * Dimensions.ExpPerSpiritPurchase;
        long total = this.Player.Experience + this.Player.ExperienceSold;

        // Prospective, not current. AddExperience early-returns when the CURRENT total is
        // over the cap (Player.cs:1653-1660), so checking the same condition here only
        // catches players who are already past it - a player one experience under the cap
        // passes, buys, and lands 24,999,999 above a ceiling the server is meant to
        // enforce. Test what the purchase would produce.
        if (GameWorld.Settings.ExperienceCap > 0 && total + granted > GameWorld.Settings.ExperienceCap)
        {
            long affordable = (GameWorld.Settings.ExperienceCap - total) / Dimensions.ExpPerSpiritPurchase;
            world.Send(this.Player, P.ServerMessage(affordable > 0
                ? "That would carry you past the experience cap. You can buy at most " + affordable + "."
                : "You have reached the experience cap."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long before = spirit.GetBalance(this.Player);
        if (before < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        spirit.Remove(this.Player, amount, world);
        this.Player.AddExperience(granted, world, Player.ExperienceMessage.Normal, applyModifiers: false);

        world.Send(this.Player, P.ServerMessage(
            "You spend " + amount + " " + spirit.Name + " to gain " + granted.ToString("N0") + " experience."));
        world.LogHandler.Log(Log.Types.BuyExperience, this.Player,
            "BuyExperience: " + amount + " " + spirit.ShortName + " -> " + granted + " exp"
            + ", spirit " + before + " -> " + (before - amount));
    }
}

/// <summary>Handles "/givesp &lt;player&gt; &lt;amount&gt;": transfers spirit between two
/// online players. Registered with a trailing space so the command trie matches it as a
/// longest-prefix, exactly like "/dimension ".</summary>
public class GiveSpiritCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new GiveSpiritCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (tokens.Length < 3 || !Dimensions.TryParseAmount(tokens, 2, out amount))
        {
            world.Send(this.Player, P.ServerMessage("/givesp <player> <amount>"));
            return;
        }

        var target = world.PlayerHandler.GetPlayer(tokens[1]);
        if (target == null || target.State != Player.States.Ready)
        {
            world.Send(this.Player, P.ServerMessage(tokens[1] + " is not online."));
            return;
        }

        if (target == this.Player)
        {
            world.Send(this.Player, P.ServerMessage("You cannot give spirit to yourself."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long senderBefore = spirit.GetBalance(this.Player);
        if (senderBefore < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        // The recipient side, checked before either wallet moves. BaseStats.SP is a long,
        // so a transfer into a large enough wallet wraps negative; MaxSpiritBalance keeps
        // the refusal well short of that and makes a faucet bug visible as a refusal
        // rather than as a corrupted balance.
        long targetBefore = spirit.GetBalance(target);
        if (targetBefore > Dimensions.MaxSpiritBalance - amount)
        {
            world.Send(this.Player, P.ServerMessage(target.Name + " cannot hold that much " + spirit.Name + "."));
            return;
        }

        spirit.Remove(this.Player, amount, world);
        spirit.Add(target, amount, world);

        world.Send(this.Player, P.ServerMessage(
            "You give " + amount + " " + spirit.Name + " to " + target.Name + "."));
        world.Send(target, P.ServerMessage(
            this.Player.Name + " gives you " + amount + " " + spirit.Name + "."));

        // One entry per side, each naming the counterparty in otherid and carrying its own
        // before/after. Two rows rather than one because logs are queried per player.
        world.LogHandler.Log(Log.Types.GiveSpirit, this.Player,
            "GiveSpirit: sent " + amount + " " + spirit.ShortName + " to " + target.Name
            + ", balance " + senderBefore + " -> " + (senderBefore - amount),
            target.PlayerID);
        world.LogHandler.Log(Log.Types.GiveSpirit, target,
            "GiveSpirit: received " + amount + " " + spirit.ShortName + " from " + this.Player.Name
            + ", balance " + targetBefore + " -> " + (targetBefore + amount),
            this.Player.PlayerID);
    }
}

/// <summary>Spirit, the dimension currency. The wallet is BaseStats.SP, which already
/// persists as players.player_sp, so no schema change is needed.
///
/// MaxStats.SP is separate accounting from BaseStats.SP: MaxSP reads MaxStats
/// (Player.cs:210), and CurrentSP's setter clamps to MaxSP (Player.cs:185). So a balance
/// change moves both, and CurrentSP is topped up afterwards - regen is zeroed in
/// GooseSettings.json, so nothing else ever moves it.
///
/// Gear granting SP raises MaxStats only, so it cannot inflate the balance. It can make
/// MaxSP exceed the balance, which is cosmetic in the client's SP bar.</summary>
public class SpiritCurrency : ICurrency
{
    public string Id { get { return Dimensions.SpiritCurrencyId; } }
    public string Name { get { return "spirit"; } }
    public string ShortName { get { return "sp"; } }

    public long GetBalance(Player player) { return player.BaseStats.SP; }

    public long GetBuyPrice(ItemTemplate template, int stack) { return template.Value * stack; }

    /// <summary>Half value, and a refusal for worthless items - a dimension clone of a
    /// zero-value base item (0 x 3^dim = 0) must be refused like gold refuses it, or the
    /// vendor would take the item and pay nothing.</summary>
    public long GetSellPrice(Item item, int stack)
    {
        if (item.Value == 0) return -1;
        return stack * item.Value / 2;
    }

    public void Add(Player player, long amount, GameWorld world)
    {
        player.BaseStats.SP += amount;

        var delta = new AttributeSet();
        delta.SP = amount;
        player.AddStats(delta, world);        // raises MaxStats.SP and sends StatusInfo

        player.CurrentSP = player.MaxSP;      // setter clamps, so this must follow AddStats
        world.Send(player, P.StatusInfo(player));
    }

    public void Remove(Player player, long amount, GameWorld world)
    {
        player.BaseStats.SP -= amount;

        var delta = new AttributeSet();
        delta.SP = amount;
        player.RemoveStats(delta, world);

        player.CurrentSP = player.MaxSP;
        world.Send(player, P.StatusInfo(player));
    }
}

return typeof(Dimensions);
