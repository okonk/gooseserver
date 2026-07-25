using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using Goose.Events;

namespace Goose
{
    /**
     * EventHandler, does events at the specified time
     *
     */
    public class EventHandler
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /**
         * SortedList acts like a priority queue
         *
         */
        SortedList<long, Event> events;

        /**
         * StringToEvent, converts a string to an event creator delegate
         *
         */
        Dictionary<string, CommandDefinition> stringToEvent;
        public delegate Event CreateEvent(Player player, Object data);

        /**
         * CommandDefinition, a dispatch table entry
         *
         * RequiredPrivilege is null for commands any player may use. Anything else is
         * refused by AddEvent unless the caller holds that privilege.
         *
         */
        private sealed class CommandDefinition
        {
            public CreateEvent Create;
            public AccessPrivilege? RequiredPrivilege;
        }

        private static CommandDefinition Open(CreateEvent create)
        {
            return new CommandDefinition { Create = create, RequiredPrivilege = null };
        }

        private static CommandDefinition Restricted(CreateEvent create, AccessPrivilege privilege)
        {
            return new CommandDefinition { Create = create, RequiredPrivilege = privilege };
        }

        /**
         * Constructor, constructs sortedlist
         *
         */
        public EventHandler()
        {
            this.events = new SortedList<long, Event>();
            // Every entry states its access requirement, either Open or Restricted. The
            // dispatcher used to hold bare delegates and perform no authorization at all,
            // leaving every check to the individual handler - which is how /hax shipped
            // ungated. Requiring the decision here means a newly added command cannot
            // silently default to being reachable by anyone.
            //
            // Handlers keep their own checks as defence in depth, and commands whose
            // requirement varies by argument (/toggle, /custom, /givecredits) are Open
            // here and enforce the finer rule themselves.
            this.stringToEvent = new Dictionary<string, CommandDefinition>
            {
                { "LOGIN", Open(LoginEvent.Create) },
                { "LCNT", Open(LoginContinuedEvent.Create) },
                { "DLM", Open(DoneLoadingMapEvent.Create) },
                { ";", Open(ChatEvent.Create) },
                { "M1", Open(MoveEvent.Create) },
                { "M2", Open(MoveEvent.Create) },
                { "M3", Open(MoveEvent.Create) },
                { "M4", Open(MoveEvent.Create) },
                { "F1", Open(FacingEvent.Create) },
                { "F2", Open(FacingEvent.Create) },
                { "F3", Open(FacingEvent.Create) },
                { "F4", Open(FacingEvent.Create) },
                { "/tell ", Open(TellEvent.Create) },
                { "/who", Open(WhoEvent.Create) },
                { "/summon ", Restricted(SummonEvent.Create, AccessPrivilege.Summon) },
                { "/warp ", Restricted(WarpEvent.Create, AccessPrivilege.Warp) },
                { "/approach ", Restricted(ApproachEvent.Create, AccessPrivilege.Approach) },
                { "CHANGE", Open(InventoryChangeSlotEvent.Create) },
                { "SPLIT", Open(InventorySplitEvent.Create) },
                { "USE", Open(InventoryUseEvent.Create) },
                { "GET", Open(PickupItemEvent.Create) },
                { "DRP", Open(PlayerDropItemEvent.Create) },
                { "/dropgold ", Open(PlayerDropGoldEvent.Create) },
                { "ATT", Open(PlayerAttackEvent.Create) },
                { "PONG", Open(PlayerPongEvent.Create) },
                { "/shutdown", Restricted(ShutdownCommandEvent.Create, AccessPrivilege.Shutdown) },
                { "/location", Open(LocationEvent.Create) },
                { "RPU", Open(RefreshPositionEvent.Create) },
                { "/refresh", Open(RefreshPositionEvent.Create) },
                { "CAST", Open(PlayerCastSpellEvent.Create) },
                { "/getitem ", Restricted(GetItemCommandEvent.Create, AccessPrivilege.SpawnItem) },
                { "/hax ", Restricted(HaxCommandEvent.Create, AccessPrivilege.Debug) },
                { "/gmhax ", Restricted(GMHaxCommandEvent.Create, AccessPrivilege.Debug) },
                { "/togglegroup", Open(ToggleGroupCommandEvent.Create) },
                { "/group ", Open(GroupChatEvent.Create) },
                { "/invite ", Open(GroupAddEvent.Create) },
                { "/groupadd ", Open(GroupAddEvent.Create) },
                { "/disband", Open(GroupRemoveEvent.Create) },
                { "/groupremove", Open(GroupRemoveEvent.Create) },
                { "RC", Open(PlayerRightClickEvent.Create) },
                { "WBC", Open(WindowButtonClickEvent.Create) },
                { "VPI", Open(VendorPurchaseInventoryEvent.Create) },
                { "VSI", Open(VendorSellInventoryEvent.Create) },
                { "/ban ", Restricted(BanCommandEvent.Create, AccessPrivilege.Ban) },
                { "/kick ", Restricted(KickCommandEvent.Create, AccessPrivilege.Kick) },
                { "/shout ", Open(ShoutCommandEvent.Create) },
                { "/auction ", Open(AuctionCommandEvent.Create) },
                { "/random", Open(RandomCommandEvent.Create) },
                { "/broadcast ", Restricted(BroadcastCommandEvent.Create, AccessPrivilege.Broadcast) },
                { "EMOT", Open(EmoteEvent.Create) },
                { "/buyvita", Open(BuyVitaCommandEvent.Create) },
                { "/buymana", Open(BuyManaCommandEvent.Create) },
                { "DITM", Open(DestroyItemEvent.Create) },
                { "DSPL", Open(DestroySpellEvent.Create) },
                { "SWAP", Open(SpellbookSwapEvent.Create) },
                { "OCB", Open(OpenCombineBagEvent.Create) },
                { "ITW", Open(InventoryToWindowEvent.Create) },
                { "WTI", Open(WindowToInventoryEvent.Create) },
                { "/charinfo", Open(CharacterInfoCommandEvent.Create) },
                { "/guildcreate ", Open(GuildCreateCommandEvent.Create) },
                { "/guildadd ", Open(GuildAddCommandEvent.Create) },
                { "/guildremove", Open(GuildRemoveCommandEvent.Create) },
                { "/guildmotd", Open(GuildMotdCommandEvent.Create) },
                { "/guildowner ", Open(GuildOwnerCommandEvent.Create) },
                { "/guildofficer ", Open(GuildOfficerCommandEvent.Create) },
                { "/guild ", Open(GuildChatCommandEvent.Create) },
                { "/rank", Open(RankCommandEvent.Create) },
                { "/setconfig ", Restricted(SetConfigCommandEvent.Create, AccessPrivilege.SetConfig) },
                { "/saveconfig", Restricted(SaveConfigCommandEvent.Create, AccessPrivilege.SetConfig) },
                { "/respawnmap", Restricted(RespawnMapCommandEvent.Create, AccessPrivilege.RespawnMap) },
                { "/changepassword ", Open(ChangePasswordCommandEvent.Create) },
                { "KBUF", Open(KillBuffEvent.Create) },
                { "/toggle ", Open(ToggleCommandEvent.Create) },
                { "/aether ", Open(AetherCommandEvent.Create) },
                { "/petlist", Open(PetListCommandEvent.Create) },
                { "/petspawn ", Open(PetSpawnCommandEvent.Create) },
                { "/petinfo ", Open(PetInfoCommandEvent.Create) },
                { "/petdamage ", Open(PetDamageCommandEvent.Create) },
                { "/petvita ", Open(PetVitaCommandEvent.Create) },
                { "/petdelete ", Open(PetDeleteCommandEvent.Create) },
                { "/unban ", Restricted(UnbanCommandEvent.Create, AccessPrivilege.Ban) },
                { "/checkname ", Restricted(CheckNameCommandEvent.Create, AccessPrivilege.ChangeName) },
                { "/changeclass ", Restricted(ChangeClassCommandEvent.Create, AccessPrivilege.ClassChange) },
                { "/changename ", Restricted(ChangeNameCommandEvent.Create, AccessPrivilege.ChangeName) },
                { "/giveexperience ", Restricted(GiveExperienceCommandEvent.Create, AccessPrivilege.GiveExperience) },
                { "/credits", Open(CreditsCommandEvent.Create) },
                { "/playtime", Open(PlaytimeCommandEvent.Create) },
                { "/settitle ", Restricted(SetTitleCommandEvent.Create, AccessPrivilege.SetTitle) },
                { "/setsurname ", Restricted(SetSurnameCommandEvent.Create, AccessPrivilege.SetSurname) },
                { "/givecredits ", Open(GiveCreditsCommandEvent.Create) },
                { "/hairdye", Open(HairdyeCommandEvent.Create) },
                { "SBN", Open(SpellbookNextEvent.Create) },
                { "SBB", Open(SpellbookBackEvent.Create) },
                { "LC", Open(PlayerLeftClickEvent.Create) },
                { "/spawnnpc ", Restricted(SpawnNPCCommandEvent.Create, AccessPrivilege.SpawnNPC) },
                { "/search ", Restricted(SearchCommandEvent.Create, AccessPrivilege.Search) },
                { "WTW", Open(WindowToWindowEvent.Create) },
                { "/custom", Open(CustomCommandEvent.Create) },
                { "SID", Open(SpellInfoEvent.Create) },
                { "/mutemap", Restricted(MuteMapEvent.Create, AccessPrivilege.MuteMap) },
                { "/setaccess", Restricted(SetAccessCommandEvent.Create, AccessPrivilege.SetAccess) },
                { "/macrocheck ", Restricted(MacroCheckCommandEvent.Create, AccessPrivilege.MacroCheck) },
                { "/mc ", Open(MacroConfirmCommandEvent.Create) },
                { "/reloadscripts", Restricted(ReloadScriptsCommandEvent.Create, AccessPrivilege.ReloadScripts) },
                { "/reloadsql", Restricted(ReloadSqlCommandEvent.Create, AccessPrivilege.ReloadSQL) },
                { "/updatesql", Restricted(UpdateSqlCommandEvent.Create, AccessPrivilege.ReloadSQL) },
                { "/placespawn", Restricted(PlaceSpawnCommandEvent.Create, AccessPrivilege.PlaceSpawn) },
                { "/playerinfo ", Restricted(PlayerInfoCommandEvent.Create, AccessPrivilege.PlayerInfoCheck) },
                { "/setpassword ", Restricted(GMSetPasswordCommandEvent.Create, AccessPrivilege.SetPassword) }
            };
        }

        /**
         * RegisterEvent, registers a command any player may use
         *
         * Used by global scripts. Kept unprivileged to match how scripts already rely on
         * it; use the overload below for anything that should be restricted.
         *
         */
        public void RegisterEvent(string key, CreateEvent action)
        {
            this.stringToEvent[key] = Open(action);
        }

        public void RegisterEvent(string key, CreateEvent action, AccessPrivilege privilege)
        {
            this.stringToEvent[key] = Restricted(action, privilege);
        }

        /**
         * AddEvent, creates Event object from packet and adds it to events
         *
         * This function is pretty sexy, not sure if it's a very good way of doing it though
         * What it does is searches our stringtToEvent dictionary and sees if any of the keys
         * match with the start of the packet.
         *
         * The stringToEvent dictionary holds a delegate which calls the static member of the Event class
         * which creates a new object of that event type and returns it.
         *
         * If we find a matching packet we return true.
         * If we don't find a match returns false.
         *
         */
        public bool AddEvent(Player player, string packet)
        {
            foreach (var entry in this.stringToEvent)
            {
                if (!packet.StartsWith(entry.Key)) continue;

                CommandDefinition definition = entry.Value;

                if (definition.RequiredPrivilege.HasValue &&
                    (player == null || !AccessLevels.HasPrivilege(player, definition.RequiredPrivilege.Value)))
                {
                    // Matched but refused. Swallowed rather than answered so an
                    // unprivileged player cannot probe which commands exist.
                    log.Debug("Refused {0} for {1}: missing {2}.",
                        entry.Key, player?.Name ?? "unknown", definition.RequiredPrivilege.Value);

                    return true;
                }

                Event e = definition.Create(player, packet);
                this.AddEvent(e);
                return true;
            }

            return false;
        }

        /**
         * AddEvent, adds the Event object to events
         *
         */
        public void AddEvent(Event e)
        {
            if (this.events.ContainsKey(e.Ticks))
            {
                e.Ticks++;
                this.AddEvent(e);
            }
            else
            {
                this.events[e.Ticks] = e;
            }
        }

        /**
         * RemoveEvent, removes event from event handler
         *
         */
        public void RemoveEvent(Event e)
        {
            this.events.Remove(e.Ticks);
        }

        /**
         * Update, loops through list doing events if they need to be done
         *
         */
        public void Update(GameWorld world)
        {
            long now = world.TimeNow;
            int index;

            var readyEvents = (from e in this.events
                              where e.Key <= now
                              select e.Value).ToList<Event>();

            for (int i = 0; i < readyEvents.Count; i++)
            {
                Event ev = readyEvents[i];

                try
                {
                    ev?.Ready(world);
                }
                catch (Exception e)
                {
                    // An exception here used to unwind the whole game loop and restart the
                    // world. Contain it to the offending event so one client cannot take
                    // the server down. The event is still removed below.
                    log.Error(e, "Unhandled exception in {0} for player {1}",
                        ev?.GetType().Name ?? "null event",
                        ev?.Player?.Name ?? "none");
                }

                if ((index = this.events.IndexOfValue(ev)) < readyEvents.Count && index > -1)
                {
                    this.events.RemoveAt(index);
                }
            }
        }
    }
}
