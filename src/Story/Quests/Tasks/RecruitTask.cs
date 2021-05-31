using NpcAdventure.Story.Messaging;
using QuestEssentials.Messages;
using QuestEssentials.Quests;
using QuestEssentials.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.Story.Quests.Tasks
{
    internal class RecruitTask : QuestTask<RecruitTask.RecruitData>
    {
        public struct RecruitData
        {
            public string CompanionName { get; set; }
        }

        private string[] _companionNames;

        public override bool OnCheckProgress(IStoryMessage message)
        {
            if (this.IsCompleted())
            {
                return false;
            }

            if (message.Name == "recruit" && message is RecruitMessage recruitMessage && this.IsWhenMatched())
            {
                if (this.Data.CompanionName != null && this.CheckCompanion(recruitMessage.CompanionName))
                {
                    this.IncrementCount(this.Goal);
                    return true;
                }
            }

            return false;
        }

        public bool CheckCompanion(string companionName)
        {
            if (this._companionNames == null)
            {
                this._companionNames = this.Data.CompanionName.Split(' ');
            }

            return this._companionNames.Contains(companionName);
        }

        public override void Register(SpecialQuest quest)
        {
            base.Register(quest);
            this._companionNames = null;
        }

        public override bool ShouldShowProgress()
        {
            return false;
        }
    }
}
