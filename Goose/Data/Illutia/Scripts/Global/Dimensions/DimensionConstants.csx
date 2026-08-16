using System;

/// <summary>Compile-time constants shared by every dimension script. Loaded with
/// #load, which merges this file's declarations into each host script's compilation
/// - one definition, no cross-file drift.
///
/// Rules: declarations only (a #loaded file is not an entry script, so no top-level
/// return), and const only. Each host script's compilation gets its own copy of this
/// class (pinned by ScriptLoadDirectiveTests), so a mutable static here would not be
/// shared across scripts - it would just be a trap.</summary>
public static class DimensionConstants
{
    /// <summary>Dimension n's copy of anything lives at baseId + Offset*n.
    /// Must exceed every base id: Illutia map ids reach 10044, so 10000 is too small.</summary>
    public const int Offset = 100000;

    /// <summary>Generated item_surnames ids. item_surnames/item_titles are sheet data
    /// with small ids; these sit far above so a new sheet row can never collide. The
    /// two dictionaries are separate (ItemHandler.cs:20,21), so the ranges only need
    /// to be distinct from sheet ids, not from each other.</summary>
    public const int SurnameIdBase = 900000;

    /// <summary>Generated item_titles ids: Legendary, Stunted.</summary>
    public const int TitleIdBase = 900100;

    /// <summary>PlayerProperties key holding the player's unlocked maximum dimension.</summary>
    public const string MaxDimensionProperty = "dimension.max";

    /// <summary>Registry id for the spirit currency. Dimension items are priced in it;
    /// their Value is already the spirit price (x3^dim, see CloneItemTemplates).</summary>
    public const string SpiritCurrencyId = "spirit";

    /// <summary>Where rebirth leaves the player. Rebirth.csx reads these; Dimensions.csx
    /// prefights the same pair against class_info.</summary>
    public const int RebirthDestinationClassId = 1;   // Commoner
    public const int RebirthDestinationLevel = 1;
}
