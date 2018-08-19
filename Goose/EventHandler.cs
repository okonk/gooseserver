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
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

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
        delegate Event CreateEvent(Player player, Object data);

        /**
         * Constructor, constructs sortedlist
         * 
         */
        public EventHandler()
        {
            this.events = new SortedList<long, Event>();
            this.stringToEvent = new Dictionary<string, CreateEvent>();

            this.stringToEvent.Add("LOGIN", LoginEvent.Create);
            this.stringToEvent.Add("LCNT", LoginContinuedEvent.Create);
            this.stringToEvent.Add("DLM", DoneLoadingMapEvent.Create);
            this.stringToEvent.Add(";", ChatEvent.Create);
            this.stringToEvent.Add("M1", MoveEvent.Create);
            this.stringToEvent.Add("M2", MoveEvent.Create);
            this.stringToEvent.Add("M3", MoveEvent.Create);
            this.stringToEvent.Add("M4", MoveEvent.Create);
            this.stringToEvent.Add("F1", FacingEvent.Create);
            this.stringToEvent.Add("F2", FacingEvent.Create);
            this.stringToEvent.Add("F3", FacingEvent.Create);
            this.stringToEvent.Add("F4", FacingEvent.Create);
            this.stringToEvent.Add("/tell ", TellEvent.Create);
            this.stringToEvent.Add("/who", WhoEvent.Create);
            //this.stringToEvent.Add("/changeclass ", ChangeClassEvent.Create);
            this.stringToEvent.Add("/summon ", SummonEvent.Create);
            this.stringToEvent.Add("/warp ", WarpEvent.Create);
            this.stringToEvent.Add("/approach ", ApproachEvent.Create);
            this.stringToEvent.Add("CHANGE", InventoryChangeSlotEvent.Create);
            this.stringToEvent.Add("SPLIT", InventorySplitEvent.Create);
            this.stringToEvent.Add("USE", InventoryUseEvent.Create);
            this.stringToEvent.Add("GET", PickupItemEvent.Create);
            this.stringToEvent.Add("DRP", PlayerDropItemEvent.Create);
            this.stringToEvent.Add("/dropgold ", PlayerDropGoldEvent.Create);
            this.stringToEvent.Add("ATT", PlayerAttackEvent.Create);
            this.stringToEvent.Add("PONG", PlayerPongEvent.Create);
            this.stringToEvent.Add("/shutdown", ShutdownCommandEvent.Create);
            this.stringToEvent.Add("/location", LocationEvent.Create);
            this.stringToEvent.Add("/refresh", RefreshPositionEvent.Create);
            this.stringToEvent.Add("CAST", PlayerCastSpellEvent.Create);
            this.stringToEvent.Add("/getitem ", GMGetItemCommandEvent.Create);
            this.stringToEvent.Add("/hax ", HaxCommandEvent.Create);
            this.stringToEvent.Add("/togglegroup", ToggleGroupCommandEvent.Create);
            this.stringToEvent.Add("/group ", GroupChatEvent.Create);
            this.stringToEvent.Add("/groupadd ", GroupAddEvent.Create);
            this.stringToEvent.Add("/groupremove", GroupRemoveEvent.Create);
            this.stringToEvent.Add("RC", PlayerRightClickEvent.Create);
            this.stringToEvent.Add("WBC", WindowButtonClickEvent.Create);
            this.stringToEvent.Add("VPI", VendorPurchaseInventoryEvent.Create);
            this.stringToEvent.Add("VSI", VendorSellInventoryEvent.Create);
            this.stringToEvent.Add("/ban ", GMBanCommandEvent.Create);
            this.stringToEvent.Add("/kick ", GMKickCommandEvent.Create);
            this.stringToEvent.Add("GID", ItemInfoEvent.Create);
            this.stringToEvent.Add("/shout ", ShoutCommandEvent.Create);
            this.stringToEvent.Add("/auction ", AuctionCommandEvent.Create);
            this.stringToEvent.Add("/random", RandomCommandEvent.Create);
            this.stringToEvent.Add("/broadcast ", GMBroadcastCommandEvent.Create);
            this.stringToEvent.Add("EMOT", EmoteEvent.Create);
            this.stringToEvent.Add("/buyvita", BuyVitaCommandEvent.Create);
            this.stringToEvent.Add("/buymana", BuyManaCommandEvent.Create);
            this.stringToEvent.Add("DITM", DestroyItemEvent.Create);
            this.stringToEvent.Add("DSPL", DestroySpellEvent.Create);
            this.stringToEvent.Add("SWAP", SpellbookSwapEvent.Create);
            this.stringToEvent.Add("OCB", OpenCombineBagEvent.Create);
            this.stringToEvent.Add("ITW", InventoryToWindowEvent.Create);
            this.stringToEvent.Add("WTI", WindowToInventoryEvent.Create);
            this.stringToEvent.Add("/charinfo", CharacterInfoCommandEvent.Create);
            this.stringToEvent.Add("/guildcreate ", GuildCreateCommandEvent.Create);
            this.stringToEvent.Add("/guildadd ", GuildAddCommandEvent.Create);
            this.stringToEvent.Add("/guildremove", GuildRemoveCommandEvent.Create);
            this.stringToEvent.Add("/guildmotd", GuildMotdCommandEvent.Create);
            this.stringToEvent.Add("/guildowner ", GuildOwnerCommandEvent.Create);
            this.stringToEvent.Add("/guildofficer ", GuildOfficerCommandEvent.Create);
            this.stringToEvent.Add("/guild ", GuildChatCommandEvent.Create);
            this.stringToEvent.Add("/rank", RankCommandEvent.Create);
            this.stringToEvent.Add("/setconfig ", SetConfigCommandEvent.Create);
            this.stringToEvent.Add("/saveconfig", SaveConfigCommandEvent.Create);
            this.stringToEvent.Add("/respawnmap", RespawnMapCommandEvent.Create);
            this.stringToEvent.Add("/changepassword ", ChangePasswordCommandEvent.Create);
            this.stringToEvent.Add("KBUF", KillBuffEvent.Create);
            this.stringToEvent.Add("/toggle ", ToggleCommandEvent.Create);
            this.stringToEvent.Add("/aether ", AetherCommandEvent.Create);
            this.stringToEvent.Add("/instalevel", InstaLevelCommandEvent.Create);
            this.stringToEvent.Add("/petlist", PetListCommandEvent.Create);
            this.stringToEvent.Add("/petspawn ", PetSpawnCommandEvent.Create);
            this.stringToEvent.Add("/petinfo ", PetInfoCommandEvent.Create);
            this.stringToEvent.Add("/petdamage ", PetDamageCommandEvent.Create);
            this.stringToEvent.Add("/petvita ", PetVitaCommandEvent.Create);
            this.stringToEvent.Add("/petdelete ", PetDeleteCommandEvent.Create);
            this.stringToEvent.Add("/unban ", UnbanCommandEvent.Create);
            this.stringToEvent.Add("/checkname ", CheckNameCommandEvent.Create);
            this.stringToEvent.Add("/classchange ", GMClassChangeCommandEvent.Create);
            this.stringToEvent.Add("/changename ", ChangeNameCommandEvent.Create);
            this.stringToEvent.Add("/giveexperience ", GMGiveExperienceCommandEvent.Create);
            this.stringToEvent.Add("/credits", CreditsCommandEvent.Create);
            this.stringToEvent.Add("/playtime", PlaytimeCommandEvent.Create);
            this.stringToEvent.Add("/settitle ", GMSetTitleCommandEvent.Create);
            this.stringToEvent.Add("/setsurname ", GMSetSurnameCommandEvent.Create);
            this.stringToEvent.Add("/givecredits ", GiveCreditsCommandEvent.Create);
            this.stringToEvent.Add("/hairdye ", HairdyeCommandEvent.Create);
            this.stringToEvent.Add("SBN", SpellbookNextEvent.Create);
            this.stringToEvent.Add("SBB", SpellbookBackEvent.Create);
            this.stringToEvent.Add("LC", PlayerLeftClickEvent.Create);
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
            //this.events[e.Ticks] = e;
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
            long now; // High resolution timing
            QueryPerformanceCounter(out now);
            int index;

            var readyEvents = (from e in this.events
                              where e.Key <= now
                              select e.Value).ToList<Event>();

            for (int i = 0; i < readyEvents.Count; i++)
            {
                Event ev = readyEvents[i];

                ev.Ready(world);

                if ((index = this.events.IndexOfValue(ev)) < readyEvents.Count)
                {
                    this.events.RemoveAt(index);
                }
            }
        }
    }
}
