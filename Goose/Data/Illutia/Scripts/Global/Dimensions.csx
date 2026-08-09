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

    /// <summary>Quest ids are deterministic: QuestProgress persists keyed on
    /// requirement.Id (Player.cs:1020 / QuestWindow.cs:268), so a counter-assigned id
    /// would orphan in-flight kill progress on restart.</summary>
    public const int QuestIdBase = 900000;

    public const string MaxDimensionProperty = "dimension.max";

    public override void OnLoaded(GameWorld world)
    {
        if (!Enabled) return;

        CloneTemplates(world);
        RewireAllies(world);
        CloneMaps(world);
        RewireWarps(world);
        CloneSpawns(world);
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
                // Entry gates scale by (dim*5)^2
                clone.MinExperience = basic.MinExperience * (dim * 5) * (dim * 5);
                clone.MaxExperience = basic.MaxExperience * (dim * 5) * (dim * 5);
                clone.Script = mapScript;                 // replaces, not composes
                clone.ScriptParams = dim.ToString();      // DimensionMap reads its dimension from here

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
        var baseTemplates = world.NPCHandler.GetTemplates().ToList();

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

return typeof(Dimensions);
