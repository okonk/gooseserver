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
    // Ships disabled. Flip to true once the world is verified to clone cleanly.
    public const bool Enabled = false;

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
    }
}

return typeof(Dimensions);
