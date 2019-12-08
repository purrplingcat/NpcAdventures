using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.Events
{
    internal class SpecialModEvents : ISpecialModEvents
    {
        public event EventHandler<ILocationRenderedEventArgs> RenderedLocation;
        public event EventHandler<IMailEventArgs> MailboxOpen;
        public event EventHandler<IQuestCompletedArgs> QuestCompleted;
        public event EventHandler<IQuestReloadObjectiveArgs> ReloadObjective;

        internal void FireQuestCompleted(object sender, QuestCompletedArgs e)
        {
            this.QuestCompleted?.Invoke(sender, e);
        }

        internal void FireRenderedLocation(object sender, LocationRenderedEventArgs e)
        {
            this.RenderedLocation?.Invoke(sender, e);
        }

        internal void FireMailOpen(object sender, MailEventArgs e)
        {
            this.MailboxOpen?.Invoke(sender, e);
        }

        internal void FireQuestRealoadObjective(object sender, QuestReloadObjectiveArgs e)
        {
            this.ReloadObjective?.Invoke(sender, e);
        }
    }

    internal class QuestReloadObjectiveArgs : QuestCompletedArgs, IQuestReloadObjectiveArgs
    {

    }
    internal class QuestCompletedArgs : IQuestCompletedArgs
    {
        public Quest Quest { get; internal set; }
    }

    internal class LocationRenderedEventArgs : ILocationRenderedEventArgs
    {
        public SpriteBatch SpriteBatch { get; internal set; }
    }

    internal class MailEventArgs : IMailEventArgs
    {
        public string FullLetterKey { get; internal set; }
        public IList<string> Mailbox { get; internal set; }
        public string LetterKey { get; internal set; }
        public Farmer Player { get; internal set; }
    }
}
