using Harmony;
using NpcAdventure.Compatibility;
using NpcAdventure.Internal;
using NpcAdventure.Utils;
using StardewValley;
using System;
using System.Linq;
using static NpcAdventure.StateMachine.CompanionStateMachine;

namespace NpcAdventure.Patches
{
    internal class NpcCheckActionPatch
    {
        private static readonly SetOnce<CompanionManager> manager = new SetOnce<CompanionManager>();
        private static CompanionManager Manager { get => manager.Value; set => manager.Value = value; }

        internal static void Before_checkAction(NPC __instance, ref bool __state, Farmer who)
        {
            bool isMarried = Helper.IsSpouseMarriedToFarmer(__instance, who);
            bool canKiss = (isMarried || (bool)TPMC.Instance?.CustomKissing.CanKissNpc(who, __instance)) && (bool)TPMC.Instance?.CustomKissing.HasRequiredFriendshipToKiss(who, __instance);
            bool recruited = Manager.PossibleCompanions.TryGetValue(__instance.Name, out var csm) && csm.CurrentStateFlag == StateFlag.RECRUITED;

            __state = __instance.hasBeenKissedToday || (!canKiss && recruited);

            Console.WriteLine($"{__instance.Name} kissed today: {__state}");
        }

        [HarmonyPriority(-1000)]
        [HarmonyAfter("Digus.CustomKissingMod")]
        internal static void After_checkAction(ref NPC __instance, ref bool __result, bool __state, Farmer who, GameLocation l)
        {
            Console.WriteLine($"{__instance.Name} {who.Name} {l.Name} {__result} kissed {__state}");
            Console.WriteLine($"farmer sprite frame: {who.FarmerSprite.CurrentFrame} | dialogues: {__instance.CurrentDialogue.Count()}");

            if (__instance.CurrentDialogue.Count > 0)
                return;

            if (!__result || (__result && __state && who.FarmerSprite.CurrentFrame == 101))
            {
                __result = Manager.CheckAction(who, __instance, l);

                if (__result && who.FarmerSprite.CurrentFrame == 101)
                {
                    who.completelyStopAnimatingOrDoingAction();
                    __instance.IsEmoting = false;
                    __instance.Halt();
                    __instance.facePlayer(who);
                }
            }

            Console.WriteLine(__result);
        }

        internal static void Setup(HarmonyInstance harmony, CompanionManager manager)
        {
            Manager = manager;

            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
                prefix: new HarmonyMethod(typeof(NpcCheckActionPatch), nameof(NpcCheckActionPatch.Before_checkAction)),
                postfix: new HarmonyMethod(typeof(NpcCheckActionPatch), nameof(NpcCheckActionPatch.After_checkAction))
            );
        }
    }
}
