using System.Linq;
using Goose;
using Goose.Commands;
using Goose.Scripting;

public class ReloadMapNpcs : BaseGlobalScript
{
    public override void OnLoaded(GameWorld world)
    {
        world.Commands.Register("/reloadmapnpcs", AccessPrivilege.ReloadSQL, "Admin",
            "Reapply NPC template stats to every NPC on your map.", Reload);
    }

    public static void Reload(CommandContext ctx)
    {
        var count = 0;
        foreach (var npc in ctx.Player.Map.NPCs.ToList())
        {
            var template = npc.NPCTemplate;
            var level = npc.Class?.GetLevel(npc.Level);
            if (template == null || level == null) continue;

            npc.AggroRange = template.AggroRange;
            npc.AttackRange = template.AttackRange;
            npc.AttackSpeed = template.AttackSpeed;
            npc.MoveSpeed = template.MoveSpeed;
            npc.CanBeRooted = template.CanBeRooted;
            npc.CanBeSlowed = template.CanBeSlowed;
            npc.CanBeStunned = template.CanBeStunned;
            npc.Experience = template.Experience;
            npc.WeaponDamage = template.WeaponDamage;
            npc.RespawnTime = template.RespawnTime;
            npc.LastRespawnTimeSeconds = template.RespawnTime;

            npc.BaseStats = new AttributeSet() + template.BaseStats;
            var max = new AttributeSet() + npc.BaseStats + level.BaseStats;
            foreach (var buff in npc.Buffs) max += buff.SpellEffect.Stats;
            npc.MaxStats = max;

            if (npc.State == NPC.States.Alive) npc.CurrentHP = npc.MaxHP;
            count++;
        }

        ctx.Send($"Reloaded {count} NPCs on {ctx.Player.Map.Name}.");
    }
}

return typeof(ReloadMapNpcs);
