using QuestEssentials.Messages;
using QuestFramework.Quests;

namespace NpcAdventure.Story.Messaging
{
    class RecruitMessage : GameMasterMessage, ICompletionMessage, IStoryMessage
    {
        public RecruitMessage() : base("recruit")
        {
        }

        public RecruitMessage(string companionName) : this()
        {
            this.CompanionName = companionName;
        }

        public string CompanionName { get; set; }

        public string Trigger => this.CompanionName;
    }
}
