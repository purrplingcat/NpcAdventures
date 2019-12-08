using Harmony;
using NpcAdventure.Events;
using StardewValley.Quests;

namespace NpcAdventure.Patches
{
    internal class QuestPatch
    {
        private static SpecialModEvents events;

        /// <summary>
        /// This patches mailbox read method on gamelocation and allow call custom logic 
        /// for NPC Adventures mail letters only. For other mails call vanilla logic.
        /// </summary>
        /// <param name="__instance">Game location</param>
        /// <returns></returns>
        internal static void CompleteQuest(ref Quest __instance)
        {
            events.FireQuestCompleted(__instance, new QuestCompletedArgs() { Quest = __instance });
        }

        internal static void ReloadObjective(ref Quest __instance)
        {
            events.FireQuestRealoadObjective(__instance, new QuestReloadObjectiveArgs() { Quest = __instance });
        }

        internal static void Setup(HarmonyInstance harmony, SpecialModEvents events)
        {
            QuestPatch.events = events;

            harmony.Patch(
                original: AccessTools.Method(typeof(Quest), nameof(Quest.questComplete)),
                postfix: new HarmonyMethod(typeof(QuestPatch), nameof(QuestPatch.CompleteQuest))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Quest), nameof(Quest.reloadObjective)),
                postfix: new HarmonyMethod(typeof(QuestPatch), nameof(QuestPatch.ReloadObjective))
            );
        }
    }
}
