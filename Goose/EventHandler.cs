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
        /**
         * SortedList acts like a priority queue
         *
         */
        SortedList<long, Event> events;

        /**
         * StringToEvent, converts a string to an event creator delegate
         *
         */
        Dictionary<string, CreateEvent> stringToEvent;
        public delegate Event CreateEvent(Player player, Object data);

        /**
         * Constructor, constructs sortedlist
         *
         */
        public EventHandler()
        {
            this.events = new SortedList<long, Event>();
            this.stringToEvent = new Dictionary<string, CreateEvent>
            {
                { "LOGIN", LoginEvent.Create },
                { "LCNT", LoginContinuedEvent.Create },
                { "DLM", DoneLoadingMapEvent.Create },
                { ";", ChatEvent.Create },
                { "M1", MoveEvent.Create },
                { "M2", MoveEvent.Create },
                { "M3", MoveEvent.Create },
                { "M4", MoveEvent.Create },
                { "F1", FacingEvent.Create },
                { "F2", FacingEvent.Create },
                { "F3", FacingEvent.Create },
                { "F4", FacingEvent.Create },
                { "/tell ", TellEvent.Create },
                { "/who", WhoEvent.Create },
                { "/summon ", SummonEvent.Create },
                { "/warp ", WarpEvent.Create },
                { "/approach ", ApproachEvent.Create },
                { "CHANGE", InventoryChangeSlotEvent.Create },
                { "SPLIT", InventorySplitEvent.Create },
                { "USE", InventoryUseEvent.Create },
                { "GET", PickupItemEvent.Create },
                { "DRP", PlayerDropItemEvent.Create },
                { "/dropgold ", PlayerDropGoldEvent.Create },
                { "ATT", PlayerAttackEvent.Create },
                { "PONG", PlayerPongEvent.Create },
                { "/shutdown", ShutdownCommandEvent.Create },
                { "/location", LocationEvent.Create },
                { "RPU", RefreshPositionEvent.Create },
                { "/refresh", RefreshPositionEvent.Create },
                { "CAST", PlayerCastSpellEvent.Create },
                { "/getitem ", GetItemCommandEvent.Create },
                { "/hax ", HaxCommandEvent.Create },
                { "/gmhax ", GMHaxCommandEvent.Create },
                { "/togglegroup", ToggleGroupCommandEvent.Create },
                { "/group ", GroupChatEvent.Create },
                { "/invite ", GroupAddEvent.Create },
                { "/groupadd ", GroupAddEvent.Create },
                { "/disband", GroupRemoveEvent.Create },
                { "/groupremove", GroupRemoveEvent.Create },
                { "RC", PlayerRightClickEvent.Create },
                { "WBC", WindowButtonClickEvent.Create },
                { "VPI", VendorPurchaseInventoryEvent.Create },
                { "VSI", VendorSellInventoryEvent.Create },
                { "/ban ", BanCommandEvent.Create },
                { "/kick ", KickCommandEvent.Create },
                { "/shout ", ShoutCommandEvent.Create },
                { "/auction ", AuctionCommandEvent.Create },
                { "/random", RandomCommandEvent.Create },
                { "/broadcast ", BroadcastCommandEvent.Create },
                { "EMOT", EmoteEvent.Create },
                { "/buyvita", BuyVitaCommandEvent.Create },
                { "/buymana", BuyManaCommandEvent.Create },
                { "DITM", DestroyItemEvent.Create },
                { "DSPL", DestroySpellEvent.Create },
                { "SWAP", SpellbookSwapEvent.Create },
                { "OCB", OpenCombineBagEvent.Create },
                { "ITW", InventoryToWindowEvent.Create },
                { "WTI", WindowToInventoryEvent.Create },
                { "/charinfo", CharacterInfoCommandEvent.Create },
                { "/guildcreate ", GuildCreateCommandEvent.Create },
                { "/guildadd ", GuildAddCommandEvent.Create },
                { "/guildremove", GuildRemoveCommandEvent.Create },
                { "/guildmotd", GuildMotdCommandEvent.Create },
                { "/guildowner ", GuildOwnerCommandEvent.Create },
                { "/guildofficer ", GuildOfficerCommandEvent.Create },
                { "/guild ", GuildChatCommandEvent.Create },
                { "/rank", RankCommandEvent.Create },
                { "/setconfig ", SetConfigCommandEvent.Create },
                { "/saveconfig", SaveConfigCommandEvent.Create },
                { "/respawnmap", RespawnMapCommandEvent.Create },
                { "/changepassword ", ChangePasswordCommandEvent.Create },
                { "KBUF", KillBuffEvent.Create },
                { "/toggle ", ToggleCommandEvent.Create },
                { "/aether ", AetherCommandEvent.Create },
                { "/petlist", PetListCommandEvent.Create },
                { "/petspawn ", PetSpawnCommandEvent.Create },
                { "/petinfo ", PetInfoCommandEvent.Create },
                { "/petdamage ", PetDamageCommandEvent.Create },
                { "/petvita ", PetVitaCommandEvent.Create },
                { "/petdelete ", PetDeleteCommandEvent.Create },
                { "/unban ", UnbanCommandEvent.Create },
                { "/checkname ", CheckNameCommandEvent.Create },
                { "/changeclass ", ChangeClassCommandEvent.Create },
                { "/changename ", ChangeNameCommandEvent.Create },
                { "/giveexperience ", GiveExperienceCommandEvent.Create },
                { "/credits", CreditsCommandEvent.Create },
                { "/playtime", PlaytimeCommandEvent.Create },
                { "/settitle ", SetTitleCommandEvent.Create },
                { "/setsurname ", SetSurnameCommandEvent.Create },
                { "/givecredits ", GiveCreditsCommandEvent.Create },
                { "/hairdye", HairdyeCommandEvent.Create },
                { "SBN", SpellbookNextEvent.Create },
                { "SBB", SpellbookBackEvent.Create },
                { "LC", PlayerLeftClickEvent.Create },
                { "/spawnnpc ", SpawnNPCCommandEvent.Create },
                { "/search ", SearchCommandEvent.Create },
                { "WTW", WindowToWindowEvent.Create },
                { "/custom", CustomCommandEvent.Create },
                { "SID", SpellInfoEvent.Create },
                { "/mutemap", MuteMapEvent.Create },
                { "/setaccess", SetAccessCommandEvent.Create },
                { "/macrocheck ", MacroCheckCommandEvent.Create },
                { "/mc ", MacroConfirmCommandEvent.Create },
                { "/reloadscripts", ReloadScriptsCommandEvent.Create },
                { "/reloadsql", ReloadSqlCommandEvent.Create },
                { "/updatesql", UpdateSqlCommandEvent.Create },
                { "/placespawn", PlaceSpawnCommandEvent.Create },
                { "/playerinfo ", PlayerInfoCommandEvent.Create },
                { "/setpassword ", GMSetPasswordCommandEvent.Create }
            };
        }

        public void RegisterEvent(string key, CreateEvent action)
        {
            this.stringToEvent[key] = action;
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
            foreach (string key in this.stringToEvent.Keys)
            {
                if (packet.StartsWith(key))
                {
                    Event e = this.stringToEvent[key](player, packet);
                    this.AddEvent(e);
                    return true;
                }
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

                ev?.Ready(world);

                if ((index = this.events.IndexOfValue(ev)) < readyEvents.Count && index > -1)
                {
                    this.events.RemoveAt(index);
                }
            }
        }
    }
}
