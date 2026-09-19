using System;
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class InvestigateWindow : Window
{
    private readonly NPC npc;
    private readonly List<List<string>> pages;
    private int pageNumber = 0;

    public override string Title => "Investigate: " + npc.Name;

    public override string Buttons
        => "0,1," + (pageNumber == 0 ? 0 : 1) + "," + (pageNumber < pages.Count - 1 ? 1 : 0) + ",0";

    public InvestigateWindow(GameWorld world, Player player, NPC npc)
    {
        this.npc = npc;
        this.ID = ++player.LastWindowID;
        this.Frame = WindowFrames.GenericInfo;
        this.Type = WindowTypes.Generic;
        this.NPC = npc;
        this.pages = BuildPages(npc);

        this.SendCreate(player, world);
    }

    public static void Open(GameWorld world, Player player, NPC npc)
    {
        var existing = player.Windows.FirstOrDefault(w => w.Type == WindowTypes.Generic && w.NPC == npc);
        if (existing is InvestigateWindow window)
        {
            window.pageNumber = 0;
            window.SendCreate(player, world);
            return;
        }

        player.Windows.Add(new InvestigateWindow(world, player, npc));
    }

    private static List<List<string>> BuildPages(NPC npc)
    {
        string yn(bool v) => v ? "Yes" : "No";
        string behaviour = npc.NPCTemplate.Behaviour switch
        {
            NPCTemplate.BehaviourTypes.TeleportAggro => "Teleport on aggro",
            NPCTemplate.BehaviourTypes.TeleportToAggro => "Teleport to aggro",
            _ => "None",
        };

        List<string> page1 = new List<string>
        {
            "Max HP: " + npc.MaxHP,
            "Level: " + npc.Level,
            "Class: " + (npc.Class?.ClassName ?? "?"),
            "Damage: " + npc.WeaponDamage,
            "AC: " + npc.MaxStats.AC,
            "Armor pierce: " + npc.ArmorPierce,
            "Aggro range: " + npc.AggroRange,
            "Attack range: " + npc.AttackRange,
            "Attack speed: " + Math.Round(npc.AttackSpeed, 2),
            "Move speed: " + Math.Round(npc.MoveSpeed, 2),
        };

        List<string> page2 = new List<string>
        {
            "HP regen: " + Math.Round(npc.MaxStats.HPPercentRegen * 100, 0) + "% +" + npc.MaxStats.HPStaticRegen,
            "Experience: " + npc.Experience,
            "See invisible: " + yn(npc.CanSeeInvisible),
            "CC: stun " + yn(npc.CanBeStunned) + " root " + yn(npc.CanBeRooted) + " slow " + yn(npc.CanBeSlowed),
            "Behaviour: " + behaviour,
            "Respawn: " + npc.RespawnTime + "s",
        };

        return new List<List<string>> { page1, page2 };
    }

    public override void Populate(Player player, GameWorld world)
    {
        int lineno = 1;
        foreach (var line in pages[pageNumber])
            world.Send(player, P.WindowTextLine(this.ID, lineno++, line));
    }

    public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
    {
        switch (buttonid)
        {
            case ButtonTypes.Exit:
            case ButtonTypes.Close:
                player.Windows.Remove(this);
                break;
            case ButtonTypes.Next:
                if (pageNumber < pages.Count - 1)
                {
                    pageNumber++;
                    this.SendCreate(player, world);
                }
                break;
            case ButtonTypes.Back:
                if (pageNumber > 0)
                {
                    pageNumber--;
                    this.SendCreate(player, world);
                }
                break;
            default:
                player.Windows.Remove(this);
                break;
        }
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
