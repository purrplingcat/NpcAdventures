using Harmony;
using StardewValley;
using System.Reflection;

namespace NpcAdventure.Patches
{
    /// <summary>
    /// This Patch fixes broken vanilla game functionality, if we have recruited NPC, Game1.getCharacterFromName returns null
    /// This is a fix that repair getCharacterFromName returns a instance of our recruited companion NPC and keep vanilla functionality
    /// Recruited NPC has a flag `eventActor` set to true and original function can't return event actors, 
    /// but our companion is not event actor in real, just uses this flag for disable unattended functionality 
    /// like can't walk through invisible NPC barrier and etc. We want avoid this functionality but keep the vanilla functionality 
    /// of getCharacterFromName because companion is not real event actor it's fake event actor. They are still regular villager NPC.
    /// </summary>
    internal class GetCharacterPatch
    {
        private static CompanionManager manager;

        internal static void Postfix(ref NPC __result, string name)
        {
            if (__result == null && manager.PossibleCompanions.TryGetValue(name, out var csm) && csm.Companion?.currentLocation != null)
            {
                __result = csm.Companion;
            }
        }

        internal static void Setup(HarmonyInstance harmony, CompanionManager manager)
        {
            bool matchMethod(MethodInfo m) => m.Name == "getCharacterFromName" && m.ReturnType == typeof(NPC) && !m.IsGenericMethod;
            MethodInfo getCharacterByNameMethod = AccessTools.GetDeclaredMethods(typeof(Game1)).Find(matchMethod);
            
            GetCharacterPatch.manager = manager;
            
            harmony.Patch(
                original: getCharacterByNameMethod,
                postfix: new HarmonyMethod(typeof(GetCharacterPatch), nameof(GetCharacterPatch.Postfix))
            );
        }
    }
}
