using NpcAdventure.Events;
using NpcAdventure.Loader;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Quests;
using System;

namespace NpcAdventure.Story.Scenario
{
    internal class AdventureBegins : BaseScenario
    {
        const string LETTER_KEY = "npcAdventures.adventureBegins";
        const string DONE_LETTER_KEY = "npcAdventures.adventureContinue";

        private readonly ISpecialModEvents modEvents;
        private readonly IModEvents gameEvents;
        private readonly IContentLoader contentLoader;
        private readonly IMonitor monitor;

        public AdventureBegins(ISpecialModEvents modEvents, IModEvents gameEvents, IContentLoader contentLoader, IMonitor monitor): base()
        {
            this.modEvents = modEvents;
            this.gameEvents = gameEvents;
            this.contentLoader = contentLoader;
            this.monitor = monitor;
        }

        public override void Dispose()
        {
            this.modEvents.MailboxOpen -= this.Events_MailboxOpen;
            this.modEvents.QuestCompleted -= this.ModEvents_QuestCompleted;
            this.gameEvents.Player.Warped -= this.Player_Warped;
        }

        public override void Initialize()
        {
            this.modEvents.MailboxOpen += this.Events_MailboxOpen;
            this.modEvents.QuestCompleted += this.ModEvents_QuestCompleted;
            this.gameEvents.Player.Warped += this.Player_Warped;
        }

        private void ModEvents_QuestCompleted(object sender, IQuestCompletedArgs e)
        {
            int id = StoryHelper.ResolveId(e.Quest.id.Value);

            if (id == 4 && !Game1.player.hasOrWillReceiveMail(DONE_LETTER_KEY))
            {
                Game1.addMailForTomorrow(DONE_LETTER_KEY);
            }
        }

        /// <summary>
        /// Check for Marlon's mail read from mailbox
        /// Adds a introduction "Adventure begins" quest and allows to play Marlon's event in Adventurer's guild
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Events_MailboxOpen(object sender, IMailEventArgs e)
        {
            if (!e.LetterKey.Equals("adventureBegins"))
                return;

            Quest quest = this.StoryHelper.GetQuestById(1);
            quest.showNew.Value = true;
            quest.accept();

            Game1.player.questLog.Add(quest);
            Game1.addHUDMessage(new HUDMessage(this.contentLoader.LoadString("Strings/Strings:newObjective"), 2));
            Game1.playSound("questcomplete");
        }

        /// <summary>
        /// Check a parts of introduction event/quest when player warped.
        /// When player reach 10 floor of mines, then we got letter tomorrow
        /// When player got letter and go to Adventurer's guild, play Marlon event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (e.Player.mailReceived.Contains("guildMember") && e.Player.deepestMineLevel >= 10 && !e.Player.mailReceived.Contains(LETTER_KEY))
            {
                if (e.Player.mailForTomorrow.Contains(LETTER_KEY) || e.Player.mailbox.Contains(LETTER_KEY))
                    return; // Don't send letter again when it's in mailbox or it's ready to be placed in tomorrow

                // Marlon sends letter with invitation if player can't recruit and don't recieved Marlon's letter
                Game1.addMailForTomorrow(LETTER_KEY);
                this.monitor.Log("Adventure Begins: Marlon's mail added for tomorrow!");
            }

            if (e.NewLocation.Name.Equals("AdventureGuild") && e.Player.mailReceived.Contains(LETTER_KEY) && !this.GameMaster.Data.GetPlayerState(e.Player).isEligible)
            {
                if (this.contentLoader.LoadStrings("Data/Events").TryGetValue("adventureBegins", out string eventData))
                {
                    e.NewLocation.startEvent(new Event(eventData));
                    this.GameMaster.Data.GetPlayerState().isEligible = true;
                    this.GameMaster.SyncData();
                    this.monitor.Log($"Player {e.Player.Name} is now eligible to recruit companions!", LogLevel.Info);
                }
            }
        }
    }
}
