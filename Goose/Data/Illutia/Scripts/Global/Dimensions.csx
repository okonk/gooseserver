#load "Dimensions/DimensionConstants.csx"
#load "Dimensions/DimensionHelpers.csx"
#load "Dimensions/Npcs.csx"
#load "Dimensions/Maps.csx"
#load "Dimensions/Items.csx"
#load "Dimensions/Spells.csx"
#load "Dimensions/Commands.csx"
#load "Dimensions/SpiritCurrency.csx"
using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Events;
using Goose.Quests;
using Goose.Scripting;

public partial class Dimensions : BaseGlobalScript
{
    // ---- Configuration -------------------------------------------------
    public const bool Enabled = true;

    /// <summary>Dimensions above 0. Abyss shipped 6.</summary>
    public const int DimensionCount = 6;

    /// <summary>Single definition in DimensionConstants.csx. Dimension n's copy of
    /// anything lives at baseId + Offset*n. Must exceed every base id: Illutia map ids
    /// reach 10044, so 10000 is too small.</summary>
    public const int Offset = DimensionConstants.Offset;

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

    /// <summary>Single definition in DimensionConstants.csx. Where rebirth *leaves* the
    /// player, as opposed to what the keeper looks like. Read by Rebirth.csx through
    /// DimensionConstants; CreateRebirthQuest's preflight checks the same pair against
    /// class_info.</summary>
    public const int RebirthDestinationClassId = DimensionConstants.RebirthDestinationClassId;
    public const int RebirthDestinationLevel = DimensionConstants.RebirthDestinationLevel;

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

    /// <summary>Single definition in DimensionConstants.csx. Generated ItemModifier ids.
    /// item_surnames/item_titles are sheet data with small ids; these sit far above so a
    /// new sheet row can never collide. The two dictionaries are separate
    /// (ItemHandler.cs:20,21), so the ranges only need to be distinct from sheet ids, not
    /// from each other.</summary>
    public const int SurnameIdBase = DimensionConstants.SurnameIdBase;
    public const int TitleIdBase = DimensionConstants.TitleIdBase;

    /// <summary>Single definition in DimensionConstants.csx. Registry id for the spirit
    /// currency. Dimension items are priced in it; their Value is already the spirit
    /// price (x3^dim, see CloneItemTemplates).</summary>
    public const string SpiritCurrencyId = DimensionConstants.SpiritCurrencyId;

    /// <summary>Reroll cost is ResetItemCostBase^dim: 3/9/27/81/243/729 spirit
    /// (ResetItemEvent.java:30).</summary>
    public const int ResetItemCostBase = 3;

    /// <summary>Single definition in DimensionConstants.csx. PlayerProperties key
    /// holding the player's unlocked maximum dimension.</summary>
    public const string MaxDimensionProperty = DimensionConstants.MaxDimensionProperty;

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
}

return typeof(Dimensions);
