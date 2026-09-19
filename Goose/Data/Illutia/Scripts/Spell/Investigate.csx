using System;
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class InvestigateWindow : Window
{
    private readonly NPC npc;
    private List<string> lines;

    public override string Title => "Investigate: " + npc.Name;

    public override string Buttons => "0,0,0,0,0";

    public InvestigateWindow(GameWorld world, Player player, NPC npc)
    {
        this.npc = npc;
        this.ID = ++player.LastWindowID;
        this.Frame = WindowFrames.GenericInfo;
        this.Type = WindowTypes.Generic;
        this.NPC = npc;
        this.lines = BuildLines(npc, player);

        this.SendCreate(player, world);
    }

    public static void Open(GameWorld world, Player player, NPC npc)
    {
        var existing = player.Windows.FirstOrDefault(w => w.Type == WindowTypes.Generic && w.NPC == npc);
        if (existing is InvestigateWindow window)
        {
            window.lines = BuildLines(npc, player);
            window.SendCreate(player, world);
            return;
        }

        player.Windows.Add(new InvestigateWindow(world, player, npc));
    }

    // The client's info window is 140px tall with an 11.18px row pitch from y=22, so it shows
    // 10 rows and anything past that is drawn off the frame.
    private static List<string> BuildLines(NPC npc, Player player)
    {
        var lines = new List<string>
        {
            $"Level: {npc.Level}   Class: {npc.Class?.ClassName ?? "?"}",
            $"HP: {npc.MaxHP:N0}   AC: {npc.MaxStats.AC:N0}   Regen: {npc.MaxStats.HPPercentRegen * 100:0.##}% +{npc.MaxStats.HPStaticRegen:N0}",
            $"Damage: {npc.WeaponDamage:N0}   Armor Pierce: {npc.ArmorPierce:N0}",
            $"Attack Speed: {Math.Round(npc.AttackSpeed, 2)}   Move Speed: {Math.Round(npc.MoveSpeed, 2)}",
            $"Attack Range: {npc.AttackRange}   Aggro Range: {npc.AggroRange}",
            $"Experience: {npc.Experience:N0}   Respawn: {Respawn(npc)}",
            $"Tame Chance: {TameChance(npc, player)}",
        };

        switch (npc.NPCTemplate.Behaviour)
        {
            case NPCTemplate.BehaviourTypes.TeleportAggro:
                lines.Add("On aggro: pulls you to it");
                break;
            case NPCTemplate.BehaviourTypes.TeleportToAggro:
                lines.Add("On aggro: teleports to you");
                break;
        }

        lines.Add("Sees Invisible: " + (npc.CanSeeInvisible ? "Yes" : "No"));
        lines.Add(CrowdControlLine(npc));

        return lines;
    }

    private static string Respawn(NPC npc)
    {
        if (npc.RespawnTime <= 0) return "never";
        return Utils.FormatDuration(npc.RespawnTime * 1000L).Trim();
    }

    // Mirrors SpellEffect.CastTameSpell: the tamer's base HP and MP plus their class level's,
    // over the target's max HP, and the spell refuses stationary or invincible targets.
    private static string TameChance(NPC npc, Player player)
    {
        if (!npc.CanBeKilled || npc.MoveSpeed == 0) return "not tameable";
        if (npc.MaxHP <= 0) return "100.00%";

        var classLevel = player.Class?.GetLevel(player.Level);
        long tamer = player.BaseStats.HP + (classLevel?.BaseStats.HP ?? 0) +
                     player.BaseStats.MP + (classLevel?.BaseStats.MP ?? 0);

        return Math.Min(100.0, (double)tamer / npc.MaxHP * 100).ToString("F2") + "%";
    }

    private static string CrowdControlLine(NPC npc)
    {
        var vulnerable = new List<string>();
        var immune = new List<string>();

        (npc.CanBeStunned ? vulnerable : immune).Add("stun");
        (npc.CanBeRooted ? vulnerable : immune).Add("root");
        (npc.CanBeSlowed ? vulnerable : immune).Add("slow");

        string affected = string.Join(", ", vulnerable);
        string resistant = string.Join(", ", immune);

        if (vulnerable.Count == 0) return "Immune to " + resistant;
        if (immune.Count == 0) return "Vulnerable to: " + affected;
        return "Vulnerable to: " + affected + " (immune to " + resistant + ")";
    }

    public override void Populate(Player player, GameWorld world)
    {
        int lineno = 1;
        foreach (var line in lines)
            world.Send(player, P.WindowTextLine(this.ID, lineno++, line));
    }

    public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
    {
        player.Windows.Remove(this);
    }
}

public class InvestigateSpell : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
    {
        if (caster is not Player player) return false;
        if (target is not NPC npc)
        {
            world.Send(player, P.ServerMessage("Investigate only works on NPCs."));
            return false;
        }

        InvestigateWindow.Open(world, player, npc);
        return true;
    }

    public override IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        return new[] { "Reveals a creature's combat stats in a window" };
    }
}

return typeof(InvestigateSpell);
