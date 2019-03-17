using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public enum AccessPrivilege
    {
        IgnoreMapRequirements,
        Warp,
        Approach,
        TalkWhileMuted,
        CastSpellsWhileBlocked,
        MuteMap,
        MutePlayer,
        Broadcast,
        Summon,
        Kick,
        Ban,
        SetTitle,
        SetSurname,
        DropBoundItem,
        IgnoreItemRequirements,
        RespawnMap,
        Search,
        SpawnNPC,
        SpawnItem,
        GiveExperience,
        ClassChange,
        ChangeName,
        GMInvisible,
        SetAccess,
        SetConfig,
        Shutdown,
    }

    public static class AccessLevels
    {
        static private Dictionary<Player.AccessStatus, HashSet<AccessPrivilege>> accessPrivileges;

        static AccessLevels()
        {
            accessPrivileges = new Dictionary<Player.AccessStatus, HashSet<AccessPrivilege>>();
            accessPrivileges[Player.AccessStatus.GameMaster] = new HashSet<AccessPrivilege>(Enum.GetValues(typeof(AccessPrivilege)).Cast<AccessPrivilege>());
            accessPrivileges[Player.AccessStatus.Guide] = new HashSet<AccessPrivilege>
            {
                AccessPrivilege.IgnoreMapRequirements, AccessPrivilege.Warp, AccessPrivilege.Approach, AccessPrivilege.TalkWhileMuted, AccessPrivilege.CastSpellsWhileBlocked, AccessPrivilege.MuteMap, AccessPrivilege.MutePlayer, AccessPrivilege.Broadcast,
                AccessPrivilege.Summon, AccessPrivilege.Kick, AccessPrivilege.Ban, AccessPrivilege.SetTitle, AccessPrivilege.SetSurname
            };
            accessPrivileges[Player.AccessStatus.EventMaster] = new HashSet<AccessPrivilege>
            {
                AccessPrivilege.IgnoreMapRequirements, AccessPrivilege.Warp, AccessPrivilege.Approach, AccessPrivilege.TalkWhileMuted, AccessPrivilege.CastSpellsWhileBlocked, AccessPrivilege.MuteMap, AccessPrivilege.MutePlayer, AccessPrivilege.Broadcast
            };
            accessPrivileges[Player.AccessStatus.Helper] = new HashSet<AccessPrivilege> { AccessPrivilege.IgnoreMapRequirements, AccessPrivilege.Warp, AccessPrivilege.Approach, AccessPrivilege.TalkWhileMuted };
            accessPrivileges[Player.AccessStatus.Normal] = new HashSet<AccessPrivilege> { };
            accessPrivileges[Player.AccessStatus.Deleted] = new HashSet<AccessPrivilege> { };
            accessPrivileges[Player.AccessStatus.Banned] = new HashSet<AccessPrivilege> { };
        }

        public static bool HasPrivilege(Player player, AccessPrivilege privilege)
        {
            return accessPrivileges[player.Access].Contains(privilege);
        }
    }
}
