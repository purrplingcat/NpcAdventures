using NpcAdventure.Loader;
using NpcAdventure.Story.Messaging;
using QuestFramework.Extensions;
using QuestFramework.Quests;

namespace NpcAdventure.Story.Quests
{
    class RecruitmentQuest : CustomQuest, IQuestInfoUpdater
    {
        public const int TYPE_ID = 4582100;

        public RecruitmentQuest(IGameMaster gameMaster, IContentLoader contentLoader) : base()
        {
            this.GameMaster = gameMaster;
            this.ContentLoader = contentLoader;
            this.CustomTypeId = TYPE_ID;
        }

        public IGameMaster GameMaster { get; }
        public IContentLoader ContentLoader { get; }

        public override bool OnCompletionCheck(ICompletionMessage completionMessage)
        {
            if (completionMessage.Name == "recruit" && completionMessage is RecruitMessage recruitMessage)
            {
                int goal = int.Parse(this.Trigger.ToString());
                var ps = this.GameMaster.Data.GetPlayerState();

                if (ps.recruited.Count >= goal)
                {
                    this.Complete();
                    return true;
                }
            }

            return false;
        }

        public void UpdateDescription(IQuestInfo questData, ref string description)
        {
            
        }

        public void UpdateObjective(IQuestInfo questData, ref string objective)
        {
            var ps = this.GameMaster.Data.GetPlayerState();

            objective = this.ContentLoader.LoadString($"Strings/Strings:questObjective.recruitment", ps.recruited.Count, this.Trigger);
        }

        public void UpdateTitle(IQuestInfo questData, ref string title)
        {
            
        }
    }
}
