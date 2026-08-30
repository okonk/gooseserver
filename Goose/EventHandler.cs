using System.Collections.Concurrent;
using System.Text;
using System.Runtime.InteropServices;

using Goose.Commands;
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
                return (() => (Event)ctor!.Invoke(null));
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
         * Packet trie for O(k) prefix matching on incoming non-command packets
         * (movement, clicks, login, chat). Command keys ("/...") live in the
         * CommandRegistry, which AddEvent consults first; this trie is the
         * fall-through for everything else.
         */
        Trie<PacketDefinition> packetTrie;

        CommandRegistry commands;

        public delegate Event CreateEvent(Player player, Object data);

        /**
         * PacketDefinition, a dispatch table entry for non-command packets.
         *
         * RequiredPrivilege is null for packets any player may send. Anything else is
         * refused by AddEvent unless the caller holds that privilege.
         *
         * EventFactory is used for script-registered packets that need custom creation
         * logic. When null, EventTypeId is used to instantiate the event directly.
         *
         */
        private sealed class PacketDefinition
        {
            public System.Type EventTypeId = null!;
            public CreateEvent? EventFactory;
            public AccessPrivilege? RequiredPrivilege;
        }

        private static PacketDefinition Open(System.Type eventType)
        {
            return new PacketDefinition { EventTypeId = eventType, RequiredPrivilege = null };
        }

        private static PacketDefinition Open(CreateEvent factory)
        {
            return new PacketDefinition { EventFactory = factory, RequiredPrivilege = null };
        }

        private static PacketDefinition Restricted(System.Type eventType, AccessPrivilege privilege)
        {
            return new PacketDefinition { EventTypeId = eventType, RequiredPrivilege = privilege };
        }

        private static PacketDefinition Restricted(CreateEvent factory, AccessPrivilege privilege)
        {
            return new PacketDefinition { EventFactory = factory, RequiredPrivilege = privilege };
        }

        /**
         * Constructor, constructs sortedlist
         *
         * Command definitions live in the CommandRegistry (passed in by GameWorld);
         * every registration states its access requirement there, so a newly added
         * command cannot silently default to being reachable by anyone, and
         * downgrading a restricted key is refused at registration time. Commands
         * whose requirement varies by argument (/toggle, /custom, /givecredits) are
         * Open in the registry and enforce the finer rule themselves.
         */
        public EventHandler(CommandRegistry commands)
        {
            this.commands = commands;
            this.events = new PriorityQueue<Event, long>();
            this.packetTrie = new Trie<PacketDefinition>();
            this._SeedCommands();
            this.commands.SeedBuiltins();
        }

        /**
         * SeedCommands, populates the packet trie with non-command packets and
         * registers the legacy command table in the CommandRegistry.
         *
         * Legacy keys register without section/help: they stay out of /help until
         * migrated to attributed commands.
         *
         */
        private void _SeedCommands()
        {
            var packets = new (string Key, PacketDefinition Def)[]
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
                ("CHANGE", Open(typeof(InventoryChangeSlotEvent))),
                ("SPLIT", Open(typeof(InventorySplitEvent))),
                ("USE", Open(typeof(InventoryUseEvent))),
                ("GET", Open(typeof(PickupItemEvent))),
                ("DRP", Open(typeof(PlayerDropItemEvent))),
                ("ATT", Open(typeof(PlayerAttackEvent))),
                ("PONG", Open(typeof(PlayerPongEvent))),
                ("RPU", Open(typeof(RefreshPositionEvent))),
                ("CAST", Open(typeof(PlayerCastSpellEvent))),
                ("EMOT", Open(typeof(EmoteEvent))),
                ("DITM", Open(typeof(DestroyItemEvent))),
                ("DSPL", Open(typeof(DestroySpellEvent))),
                ("SWAP", Open(typeof(SpellbookSwapEvent))),
                ("OCB", Open(typeof(OpenCombineBagEvent))),
                ("ITW", Open(typeof(InventoryToWindowEvent))),
                ("WTI", Open(typeof(WindowToInventoryEvent))),
                ("KBUF", Open(typeof(KillBuffEvent))),
                ("SBN", Open(typeof(SpellbookNextEvent))),
                ("SBB", Open(typeof(SpellbookBackEvent))),
                ("LC", Open(typeof(PlayerLeftClickEvent))),
                ("RC", Open(typeof(PlayerRightClickEvent))),
                ("WBC", Open(typeof(WindowButtonClickEvent))),
                ("VPI", Open(typeof(VendorPurchaseInventoryEvent))),
                ("VSI", Open(typeof(VendorSellInventoryEvent))),
                ("WTW", Open(typeof(WindowToWindowEvent))),
                ("SID", Open(typeof(SpellInfoEvent)))
            };

            foreach (var (key, def) in packets)
            {
                this.packetTrie.Insert(key, def);
            }

            this.commands.RegisterLegacy("/summon ", typeof(SummonEvent), AccessPrivilege.Summon);
            this.commands.RegisterLegacy("/warp ", typeof(WarpEvent), AccessPrivilege.Warp);
            this.commands.RegisterLegacy("/approach ", typeof(ApproachEvent), AccessPrivilege.Approach);
            this.commands.RegisterLegacy("/shutdown", typeof(ShutdownCommandEvent), AccessPrivilege.Shutdown);
            this.commands.RegisterLegacy("/getitem ", typeof(GetItemCommandEvent), AccessPrivilege.SpawnItem);
            this.commands.RegisterLegacy("/hax ", typeof(HaxCommandEvent), AccessPrivilege.Debug);
            this.commands.RegisterLegacy("/gmhax ", typeof(GMHaxCommandEvent), AccessPrivilege.Debug);
            this.commands.RegisterLegacy("/togglegroup", typeof(ToggleGroupCommandEvent), null);
            this.commands.RegisterLegacy("/group ", typeof(GroupChatEvent), null);
            this.commands.RegisterLegacy("/invite ", typeof(GroupAddEvent), null);
            this.commands.RegisterLegacy("/groupadd ", typeof(GroupAddEvent), null);
            this.commands.RegisterLegacy("/disband", typeof(GroupRemoveEvent), null);
            this.commands.RegisterLegacy("/groupremove", typeof(GroupRemoveEvent), null);
            this.commands.RegisterLegacy("/ban ", typeof(BanCommandEvent), AccessPrivilege.Ban);
            this.commands.RegisterLegacy("/kick ", typeof(KickCommandEvent), AccessPrivilege.Kick);
            this.commands.RegisterLegacy("/broadcast ", typeof(BroadcastCommandEvent), AccessPrivilege.Broadcast);
            this.commands.RegisterLegacy("/guildcreate ", typeof(GuildCreateCommandEvent), null);
            this.commands.RegisterLegacy("/guildadd ", typeof(GuildAddCommandEvent), null);
            this.commands.RegisterLegacy("/guildremove", typeof(GuildRemoveCommandEvent), null);
            this.commands.RegisterLegacy("/guildmotd", typeof(GuildMotdCommandEvent), null);
            this.commands.RegisterLegacy("/guildowner ", typeof(GuildOwnerCommandEvent), null);
            this.commands.RegisterLegacy("/guildofficer ", typeof(GuildOfficerCommandEvent), null);
            this.commands.RegisterLegacy("/guild ", typeof(GuildChatCommandEvent), null);
            this.commands.RegisterLegacy("/setconfig ", typeof(SetConfigCommandEvent), AccessPrivilege.SetConfig);
            this.commands.RegisterLegacy("/saveconfig", typeof(SaveConfigCommandEvent), AccessPrivilege.SetConfig);
            this.commands.RegisterLegacy("/respawnmap", typeof(RespawnMapCommandEvent), AccessPrivilege.RespawnMap);
            this.commands.RegisterLegacy("/petlist", typeof(PetListCommandEvent), null);
            this.commands.RegisterLegacy("/petspawn ", typeof(PetSpawnCommandEvent), null);
            this.commands.RegisterLegacy("/petinfo ", typeof(PetInfoCommandEvent), null);
            this.commands.RegisterLegacy("/petdamage ", typeof(PetDamageCommandEvent), null);
            this.commands.RegisterLegacy("/petvita ", typeof(PetVitaCommandEvent), null);
            this.commands.RegisterLegacy("/petdelete ", typeof(PetDeleteCommandEvent), null);
            this.commands.RegisterLegacy("/unban ", typeof(UnbanCommandEvent), AccessPrivilege.Ban);
            this.commands.RegisterLegacy("/checkname ", typeof(CheckNameCommandEvent), AccessPrivilege.ChangeName);
            this.commands.RegisterLegacy("/changeclass ", typeof(ChangeClassCommandEvent), AccessPrivilege.ClassChange);
            this.commands.RegisterLegacy("/changename ", typeof(ChangeNameCommandEvent), AccessPrivilege.ChangeName);
            this.commands.RegisterLegacy("/giveexperience ", typeof(GiveExperienceCommandEvent), AccessPrivilege.GiveExperience);
            this.commands.RegisterLegacy("/givegold ", typeof(GiveGoldCommandEvent), AccessPrivilege.GiveGold);
            this.commands.RegisterLegacy("/settitle ", typeof(SetTitleCommandEvent), AccessPrivilege.SetTitle);
            this.commands.RegisterLegacy("/setsurname ", typeof(SetSurnameCommandEvent), AccessPrivilege.SetSurname);
            this.commands.RegisterLegacy("/givecredits ", typeof(GiveCreditsCommandEvent), null);
            this.commands.RegisterLegacy("/spawnnpc ", typeof(SpawnNPCCommandEvent), AccessPrivilege.SpawnNPC);
            this.commands.RegisterLegacy("/search ", typeof(SearchCommandEvent), AccessPrivilege.Search);
            this.commands.RegisterLegacy("/custom", typeof(CustomCommandEvent), null);
            this.commands.RegisterLegacy("/mutemap", typeof(MuteMapEvent), AccessPrivilege.MuteMap);
            this.commands.RegisterLegacy("/setaccess", typeof(SetAccessCommandEvent), AccessPrivilege.SetAccess);
            this.commands.RegisterLegacy("/macrocheck ", typeof(MacroCheckCommandEvent), AccessPrivilege.MacroCheck);
            this.commands.RegisterLegacy("/reloadscripts", typeof(ReloadScriptsCommandEvent), AccessPrivilege.ReloadScripts);
            this.commands.RegisterLegacy("/reloadsql", typeof(ReloadSqlCommandEvent), AccessPrivilege.ReloadSQL);
            this.commands.RegisterLegacy("/updatesql", typeof(UpdateSqlCommandEvent), AccessPrivilege.ReloadSQL);
            this.commands.RegisterLegacy("/placespawn", typeof(PlaceSpawnCommandEvent), AccessPrivilege.PlaceSpawn);
            this.commands.RegisterLegacy("/playerinfo ", typeof(PlayerInfoCommandEvent), AccessPrivilege.PlayerInfoCheck);
            this.commands.RegisterLegacy("/setpassword ", typeof(GMSetPasswordCommandEvent), AccessPrivilege.SetPassword);
        }

        /**
         * RegisterEvent, registers a packet any player may use via a custom factory.
         *
         * Used by global scripts that need custom event creation logic.
         *
         */
        public void RegisterEvent(string key, CreateEvent action)
        {
            if (key.StartsWith('/'))
            {
                log.Warn("RegisterEvent({0}): command keys should use CommandRegistry.Register.", key);
                this.commands.RegisterLegacy(key, action, null);
                return;
            }

            if (this.packetTrie.TryGetValue(key, out PacketDefinition? existing) &&
                existing.RequiredPrivilege.HasValue)
            {
                log.Error("Refusing to register {0} unprivileged: it already requires {1}. " +
                    "Use the RegisterEvent overload that states a privilege.",
                    key, existing.RequiredPrivilege.Value);

                return;
            }

            this.packetTrie.Insert(key, Open(action));
        }

        /**
         * RegisterEvent, registers a packet requiring a privilege via a custom factory.
         *
         */
        public void RegisterEvent(string key, CreateEvent action, AccessPrivilege privilege)
        {
            if (key.StartsWith('/'))
            {
                log.Warn("RegisterEvent({0}): command keys should use CommandRegistry.Register.", key);
                this.commands.RegisterLegacy(key, action, privilege);
                return;
            }

            this.packetTrie.Insert(key, Restricted(action, privilege));
        }

        /**
         * AddEvent, creates Event object from packet and adds it to events
         *
         * Tries the command registry first (longest registered command prefix),
         * then falls through to the packet trie for non-command packets.
         * O(k) where k is the packet length.
         *
         * If we find a matching command we return true.
         * If we don't find a match returns false.
         *
         */
        public bool AddEvent(Player player, string packet)
        {
            if (this.commands.Snapshot.Trie.TryGetLongestPrefix(packet, out CommandDefinition? definition, out int matchedLength))
            {
                string matchedKey = packet[..matchedLength];

                if (definition.Privilege is not null &&
                    (player is null || !AccessLevels.HasPrivilege(player, definition.Privilege.Value)))
                {
                    // Matched but refused. Swallowed rather than answered so an
                    // unprivileged player cannot probe which commands exist.
                    log.Debug("Refused {0} for {1}: missing {2}.",
                        matchedKey, player?.Name ?? "unknown", definition.Privilege.Value);

                    return true;
                }

                if (definition.LegacyType is not null || definition.LegacyFactory is not null)
                {
                    Event e;
                    if (definition.LegacyFactory is not null)
                    {
                        e = ((CreateEvent)definition.LegacyFactory)(player, packet);
                    }
                    else
                    {
                        // The branch condition above guarantees LegacyType is set here.
                        e = GetOrCreateFactory(definition.LegacyType!)();
                        e.Player = player;
                        e.Data = packet;
                    }
                    e.ClientOriginated = true;
                    this.AddEvent(e);
                    return true;
                }

                this.AddEvent(new CommandEvent(definition, packet, matchedLength)
                {
                    Player = player,
                    ClientOriginated = true
                });
                return true;
            }

            if (!this.packetTrie.TryGetLongestPrefix(packet, out PacketDefinition? packetDefinition, out int packetLength))
            {
                return false;
            }

            string packetKey = packet[..packetLength];

            if (packetDefinition.RequiredPrivilege.HasValue &&
                (player is null || !AccessLevels.HasPrivilege(player, packetDefinition.RequiredPrivilege.Value)))
            {
                // Matched but refused. Swallowed rather than answered so an
                // unprivileged player cannot probe which commands exist.
                log.Debug("Refused {0} for {1}: missing {2}.",
                    packetKey, player?.Name ?? "unknown", packetDefinition.RequiredPrivilege.Value);

                return true;
            }

            Event pe;
            if (packetDefinition.EventFactory is not null)
            {
                pe = packetDefinition.EventFactory(player, packet);
                pe.ClientOriginated = true;
            }
            else
            {
                pe = GetOrCreateFactory(packetDefinition.EventTypeId)();
                pe.Player = player;
                pe.Data = packet;
                pe.ClientOriginated = true;
            }
            this.AddEvent(pe);
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

        internal int Count => this.events.Count;
        internal int DroppedDuringMapLoad { get; private set; }
        internal Event Peek() => this.events.Peek();

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

            while (this.events.TryPeek(out Event? ev, out long tick) && tick <= now)
            {
                this.events.Dequeue();

                // Warps run inline inside an earlier event's Ready, so check state at
                // execution time. Internal scheduled events must keep running or buffs go permanent.
                if (ev.ClientOriginated && ev.Player is Player p &&
                    ((p.State == Player.States.LoadingGame && ev is not (LoginContinuedEvent or PlayerPongEvent)) ||
                     (p.State == Player.States.LoadingMap && ev is not (DoneLoadingMapEvent or PlayerPongEvent))))
                {
                    this.DroppedDuringMapLoad++;
                    log.Debug("Dropped {0} for {1} (state {2}).", ev.GetType().Name, p.Name, p.State);
                    continue;
                }

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
