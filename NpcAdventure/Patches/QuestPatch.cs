using Harmony;
using NpcAdventure.Events;
using NpcAdventure.Internal;
using StardewValley.Quests;
using System;

namespace NpcAdventure.Patches
{
    internal class QuestPatch : Patch<QuestPatch>
    {
        private SpecialModEvents Events { get; set; }
        public override string Name => nameof(QuestPatch);

        public QuestPatch(SpecialModEvents events)
        {
            this.Events = events ?? throw new System.ArgumentNullException(nameof(events));
            Instance = this;
        }

        /// <summary>
        /// This patches mailbox read method on gamelocation and allow call custom logic 
        /// for NPC Adventures mail letters only. For other mails call vanilla logic.
        /// </summary>
        /// <param name="__instance">Game location</param>
        /// <returns></returns>
        private static void After_questComplete(ref Quest __instance)
        {
            try
            {
                Instance.Events.FireQuestCompleted(__instance, new QuestCompletedArgs(__instance));
            } catch(Exception ex)
            {
                Instance.LogFailure(ex, nameof(After_questComplete));
            }
        }

        private static void After_reloadObjective(ref Quest __instance)
        {
            try
            {
                Instance.Events.FireQuestRealoadObjective(__instance, new QuestReloadObjectiveArgs(__instance));
            } catch(Exception ex)
            {
                Instance.LogFailure(ex, nameof(After_reloadObjective));
            }
        }

        protected override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Quest), nameof(Quest.questComplete)),
                postfix: new HarmonyMethod(typeof(QuestPatch), nameof(QuestPatch.After_questComplete))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Quest), nameof(Quest.reloadObjective)),
                postfix: new HarmonyMethod(typeof(QuestPatch), nameof(QuestPatch.After_reloadObjective))
            );
        }
    }
}
