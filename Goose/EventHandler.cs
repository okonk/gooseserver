using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
         * Cached delegates for event type instantiation.
         * Avoids Activator.CreateInstance (reflection) on the per-packet hot path.
         * Populated lazily on first use, safe because all events are registered at startup.
         */
        private static readonly ConcurrentDictionary<Type, Func<Event>> _eventFactories = new();

        private static Func<Event> GetOrCreateFactory(Type eventType)
        {
            return _eventFactories.GetOrAdd(eventType, (type) =>
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                return (() => (Event)ctor.Invoke(null));
            });
        }

        /**
         * PriorityQueue acts as a min-heap ordered by Event.Ticks.
         *
         * Duplicate ticks are allowed, so no collision resolution is needed.
         * The previous SortedList was array-backed (O(n) insert due to shifting),
         * needed a Ticks++ collision dance, and forced a full scan in Update.
         *
         */
        PriorityQueue<Event, long> events;

        /**
         * Command trie for O(k) prefix matching on incoming packets.
         * Each node that represents a complete command key holds a CommandDefinition.
         * LongestPrefixMatch returns the deepest (longest) match, so "/group " beats
         * "/group" when the packet is "/group add someone".
         */
        Trie<CommandDefinition> commandTrie;
        public delegate Event CreateEvent(Player player, Object data);

        /**
         * CommandDefinition, a dispatch table entry
         *
         * RequiredPrivilege is null for commands any player may use. Anything else is
         * refused by AddEvent unless the caller holds that privilege.
         *
         * EventFactory is used for script-registered commands that need custom creation
         * logic. When null, EventTypeId is used to instantiate the event directly.
         *
         */
        private sealed class CommandDefinition
        {
            public System.Type EventTypeId;
            public CreateEvent EventFactory;
            public AccessPrivilege? RequiredPrivilege;
        }

        private static CommandDefinition Open(System.Type eventType)
        {
            return new CommandDefinition { EventTypeId = eventType, RequiredPrivilege = null };
        }

        private static CommandDefinition Open(CreateEvent factory)
        {
            return new CommandDefinition { EventFactory = factory, RequiredPrivilege = null };
        }

        private static CommandDefinition Restricted(System.Type eventType, AccessPrivilege privilege)
        {
            return new CommandDefinition { EventTypeId = eventType, RequiredPrivilege = privilege };
        }

        private static CommandDefinition Restricted(CreateEvent factory, AccessPrivilege privilege)
        {
            return new CommandDefinition { EventFactory = factory, RequiredPrivilege = privilege };
        }

        /**
         * Constructor, constructs sortedlist
         *
         */
        public EventHandler()
        {
            this.events = new PriorityQueue<Event, long>();
            // Every entry states its access requirement, either Open or Restricted. The
            // dispatcher used to hold bare delegates and perform no authorization at all,
            // leaving every check to the individual handler - which is how /hax shipped
            // ungated. Requiring the decision here means a newly added command cannot
            // silently default to being reachable by anyone.
            //
            // This table is now the only place a command's access requirement is stated.
            // The duplicate HasPrivilege checks the handlers used to carry have been
            // removed, so an entry that is wrong here is wrong everywhere - see
            // RegisterEvent, which refuses to downgrade a restricted entry for that reason.
            //
            // Commands whose requirement varies by argument rather than by command
            // (/toggle, /custom, /givecredits) are Open here and enforce the finer rule
            // themselves. Handlers still check Player.State; the dispatcher does not.
            this.commandTrie = new Trie<CommandDefinition>();
            this._SeedCommands();
        }

        /**
         * SeedCommands, populates the trie with built-in command definitions.
         *
         * See the comment in the constructor for the access-control policy.
         *
         */
        private void _SeedCommands()
        {
            var commands = new (string Key, CommandDefinition Def)[]
            {
                ("LOGIN", Open(typeof(LoginEvent))),
                ("LCNT", Open(typeof(LoginContinuedEvent))),
                ("DLM", Open(typeof(DoneLoadingMapEvent))),
                (";", Open(typeof(ChatEvent))),
                ("M1", Open(typeof(MoveEvent))),
                ("M2", Open(typeof(MoveEvent))),
                ("M3", Open(typeof(MoveEvent))),
                ("M4", Open(typeof(MoveEvent))),
                ("F1", Open(typeof(FacingEvent))),
                ("F2", Open(typeof(FacingEvent))),
                ("F3", Open(typeof(FacingEvent))),
                ("F4", Open(typeof(FacingEvent))),
                ("/tell ", Open(typeof(TellEvent))),
                ("/who", Open(typeof(WhoEvent))),
                ("/summon ", Restricted(typeof(SummonEvent), AccessPrivilege.Summon)),
                ("/warp ", Restricted(typeof(WarpEvent), AccessPrivilege.Warp)),
                ("/approach ", Restricted(typeof(ApproachEvent), AccessPrivilege.Approach)),
                ("CHANGE", Open(typeof(InventoryChangeSlotEvent))),
                ("SPLIT", Open(typeof(InventorySplitEvent))),
                ("USE", Open(typeof(InventoryUseEvent))),
                ("GET", Open(typeof(PickupItemEvent))),
                ("DRP", Open(typeof(PlayerDropItemEvent))),
                ("/dropgold ", Open(typeof(PlayerDropGoldEvent))),
                ("ATT", Open(typeof(PlayerAttackEvent))),
                ("PONG", Open(typeof(PlayerPongEvent))),
                ("/shutdown", Restricted(typeof(ShutdownCommandEvent), AccessPrivilege.Shutdown)),
                ("/location", Open(typeof(LocationEvent))),
                ("RPU", Open(typeof(RefreshPositionEvent))),
                ("/refresh", Open(typeof(RefreshPositionEvent))),
                ("CAST", Open(typeof(PlayerCastSpellEvent))),
                ("/getitem ", Restricted(typeof(GetItemCommandEvent), AccessPrivilege.SpawnItem)),
                ("/hax ", Restricted(typeof(HaxCommandEvent), AccessPrivilege.Debug)),
                ("/gmhax ", Restricted(typeof(GMHaxCommandEvent), AccessPrivilege.Debug)),
                ("/togglegroup", Open(typeof(ToggleGroupCommandEvent))),
                ("/group ", Open(typeof(GroupChatEvent))),
                ("/invite ", Open(typeof(GroupAddEvent))),
                ("/groupadd ", Open(typeof(GroupAddEvent))),
                ("/disband", Open(typeof(GroupRemoveEvent))),
                ("/groupremove", Open(typeof(GroupRemoveEvent))),
                ("RC", Open(typeof(PlayerRightClickEvent))),
                ("WBC", Open(typeof(WindowButtonClickEvent))),
                ("VPI", Open(typeof(VendorPurchaseInventoryEvent))),
                ("VSI", Open(typeof(VendorSellInventoryEvent))),
                ("/ban ", Restricted(typeof(BanCommandEvent), AccessPrivilege.Ban)),
                ("/kick ", Restricted(typeof(KickCommandEvent), AccessPrivilege.Kick)),
                ("/shout ", Open(typeof(ShoutCommandEvent))),
                ("/auction ", Open(typeof(AuctionCommandEvent))),
                ("/random", Open(typeof(RandomCommandEvent))),
                ("/broadcast ", Restricted(typeof(BroadcastCommandEvent), AccessPrivilege.Broadcast)),
                ("EMOT", Open(typeof(EmoteEvent))),
                ("/buyvita", Open(typeof(BuyVitaCommandEvent))),
                ("/buymana", Open(typeof(BuyManaCommandEvent))),
                ("DITM", Open(typeof(DestroyItemEvent))),
                ("DSPL", Open(typeof(DestroySpellEvent))),
                ("SWAP", Open(typeof(SpellbookSwapEvent))),
                ("OCB", Open(typeof(OpenCombineBagEvent))),
                ("ITW", Open(typeof(InventoryToWindowEvent))),
                ("WTI", Open(typeof(WindowToInventoryEvent))),
                ("/charinfo", Open(typeof(CharacterInfoCommandEvent))),
                ("/guildcreate ", Open(typeof(GuildCreateCommandEvent))),
                ("/guildadd ", Open(typeof(GuildAddCommandEvent))),
                ("/guildremove", Open(typeof(GuildRemoveCommandEvent))),
                ("/guildmotd", Open(typeof(GuildMotdCommandEvent))),
                ("/guildowner ", Open(typeof(GuildOwnerCommandEvent))),
                ("/guildofficer ", Open(typeof(GuildOfficerCommandEvent))),
                ("/guild ", Open(typeof(GuildChatCommandEvent))),
                ("/rank", Open(typeof(RankCommandEvent))),
                ("/setconfig ", Restricted(typeof(SetConfigCommandEvent), AccessPrivilege.SetConfig)),
                ("/saveconfig", Restricted(typeof(SaveConfigCommandEvent), AccessPrivilege.SetConfig)),
                ("/respawnmap", Restricted(typeof(RespawnMapCommandEvent), AccessPrivilege.RespawnMap)),
                ("/changepassword ", Open(typeof(ChangePasswordCommandEvent))),
                ("KBUF", Open(typeof(KillBuffEvent))),
                ("/toggle ", Open(typeof(ToggleCommandEvent))),
                ("/aether ", Open(typeof(AetherCommandEvent))),
                ("/petlist", Open(typeof(PetListCommandEvent))),
                ("/petspawn ", Open(typeof(PetSpawnCommandEvent))),
                ("/petinfo ", Open(typeof(PetInfoCommandEvent))),
                ("/petdamage ", Open(typeof(PetDamageCommandEvent))),
                ("/petvita ", Open(typeof(PetVitaCommandEvent))),
                ("/petdelete ", Open(typeof(PetDeleteCommandEvent))),
                ("/unban ", Restricted(typeof(UnbanCommandEvent), AccessPrivilege.Ban)),
                ("/checkname ", Restricted(typeof(CheckNameCommandEvent), AccessPrivilege.ChangeName)),
                ("/changeclass ", Restricted(typeof(ChangeClassCommandEvent), AccessPrivilege.ClassChange)),
                ("/changename ", Restricted(typeof(ChangeNameCommandEvent), AccessPrivilege.ChangeName)),
                ("/giveexperience ", Restricted(typeof(GiveExperienceCommandEvent), AccessPrivilege.GiveExperience)),
                ("/givegold ", Restricted(typeof(GiveGoldCommandEvent), AccessPrivilege.GiveGold)),
                ("/credits", Open(typeof(CreditsCommandEvent))),
                ("/playtime", Open(typeof(PlaytimeCommandEvent))),
                ("/settitle ", Restricted(typeof(SetTitleCommandEvent), AccessPrivilege.SetTitle)),
                ("/setsurname ", Restricted(typeof(SetSurnameCommandEvent), AccessPrivilege.SetSurname)),
                ("/givecredits ", Open(typeof(GiveCreditsCommandEvent))),
                ("/hairdye", Open(typeof(HairdyeCommandEvent))),
                ("SBN", Open(typeof(SpellbookNextEvent))),
                ("SBB", Open(typeof(SpellbookBackEvent))),
                ("LC", Open(typeof(PlayerLeftClickEvent))),
                ("/spawnnpc ", Restricted(typeof(SpawnNPCCommandEvent), AccessPrivilege.SpawnNPC)),
                ("/search ", Restricted(typeof(SearchCommandEvent), AccessPrivilege.Search)),
                ("WTW", Open(typeof(WindowToWindowEvent))),
                ("/custom", Open(typeof(CustomCommandEvent))),
                ("SID", Open(typeof(SpellInfoEvent))),
                ("/mutemap", Restricted(typeof(MuteMapEvent), AccessPrivilege.MuteMap)),
                ("/setaccess", Restricted(typeof(SetAccessCommandEvent), AccessPrivilege.SetAccess)),
                ("/macrocheck ", Restricted(typeof(MacroCheckCommandEvent), AccessPrivilege.MacroCheck)),
                ("/mc ", Open(typeof(MacroConfirmCommandEvent))),
                ("/reloadscripts", Restricted(typeof(ReloadScriptsCommandEvent), AccessPrivilege.ReloadScripts)),
                ("/reloadsql", Restricted(typeof(ReloadSqlCommandEvent), AccessPrivilege.ReloadSQL)),
                ("/updatesql", Restricted(typeof(UpdateSqlCommandEvent), AccessPrivilege.ReloadSQL)),
                ("/placespawn", Restricted(typeof(PlaceSpawnCommandEvent), AccessPrivilege.PlaceSpawn)),
                ("/playerinfo ", Restricted(typeof(PlayerInfoCommandEvent), AccessPrivilege.PlayerInfoCheck)),
                ("/setpassword ", Restricted(typeof(GMSetPasswordCommandEvent), AccessPrivilege.SetPassword))
            };

            foreach (var (key, def) in commands)
            {
                this.commandTrie.Insert(key, def);
            }
        }

        /**
         * RegisterEvent, registers a command any player may use via a custom factory.
         *
         * Used by global scripts that need custom event creation logic.
         *
         */
        public void RegisterEvent(string key, CreateEvent action)
        {
            if (this.commandTrie.TryGetValue(key, out CommandDefinition existing) &&
                existing.RequiredPrivilege.HasValue)
            {
                log.Error("Refusing to register {0} unprivileged: it already requires {1}. " +
                    "Use the RegisterEvent overload that states a privilege.",
                    key, existing.RequiredPrivilege.Value);

                return;
            }

            this.commandTrie.Insert(key, Open(action));
        }

        /**
         * RegisterEvent, registers a command requiring a privilege via a custom factory.
         *
         */
        public void RegisterEvent(string key, CreateEvent action, AccessPrivilege privilege)
        {
            this.commandTrie.Insert(key, Restricted(action, privilege));
        }

        /**
         * AddEvent, creates Event object from packet and adds it to events
         *
         * Walks the command trie following the packet characters and returns the
         * longest registered prefix. O(k) where k is the packet length, replacing
         * the previous O(n) dictionary scan.
         *
         * If we find a matching command we return true.
         * If we don't find a match returns false.
         *
         */
        public bool AddEvent(Player player, string packet)
        {
            if (!this.commandTrie.TryGetLongestPrefix(packet, out CommandDefinition definition, out int matchedLength))
            {
                return false;
            }

            string matchedKey = packet.Substring(0, matchedLength);

            if (definition.RequiredPrivilege.HasValue &&
                (player == null || !AccessLevels.HasPrivilege(player, definition.RequiredPrivilege.Value)))
            {
                // Matched but refused. Swallowed rather than answered so an
                // unprivileged player cannot probe which commands exist.
                log.Debug("Refused {0} for {1}: missing {2}.",
                    matchedKey, player?.Name ?? "unknown", definition.RequiredPrivilege.Value);

                return true;
            }

            Event e;
            if (definition.EventFactory != null)
            {
                e = definition.EventFactory(player, packet);
            }
            else
            {
                e = GetOrCreateFactory(definition.EventTypeId)();
                e.Player = player;
                e.Data = packet;
            }
            this.AddEvent(e);
            return true;
        }

        /**
         * AddEvent, adds the Event object to events
         *
         * Duplicate ticks are fine; the heap orders by tick and does not require
         * unique keys, so e.Ticks is never mutated here (previously a same-tick
         * collision bumped Ticks and recursed).
         *
         */
        public void AddEvent(Event e)
        {
            this.events.Enqueue(e, e.Ticks);
        }

        /**
         * RemoveEvent, removes event from event handler
         *
         * Removes the specific event instance (reference equality), which is more
         * precise than the old remove-by-key and works even when several events
         * share a tick. O(n) worst case, but only used for cancellation (e.g. buff
         * expiry), never on the per-packet hot path.
         *
         */
        public void RemoveEvent(Event e)
        {
            this.events.Remove(e, out _, out _, EqualityComparer<Event>.Default);
        }

        /**
         * Update, loops through list doing events if they need to be done
         *
         * Dequeues and runs events in tick order until the heap head is in the
         * future. Events are removed before Ready runs, so a handler that
         * reschedules itself simply enqueues a fresh entry.
         *
         * Invariant: Ready handlers that re-enqueue (e.g. ScriptTimerEvent.Reschedule,
         * BuffTickEvent, ClearMapItemsEvent) always set a future tick first. If one
         * ever re-enqueued at or before now it would be re-processed in this same
         * loop; keep that invariant when adding new recurring events.
         *
         */
        public void Update(GameWorld world)
        {
            long now = world.TimeNow;

            while (this.events.TryPeek(out Event ev, out long tick) && tick <= now)
            {
                this.events.Dequeue();

                try
                {
                    ev?.Ready(world);
                }
                catch (Exception e)
                {
                    // An exception here used to unwind the whole game loop and restart the
                    // world. Contain it to the offending event so one client cannot take
                    // the server down. The event has already been removed from the queue.
                    log.Error(e, "Unhandled exception in {0} for player {1}",
                        ev?.GetType().Name ?? "null event",
                        ev?.Player?.Name ?? "none");
                }
            }
        }
    }
}
