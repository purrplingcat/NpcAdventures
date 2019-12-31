using Harmony;
using NpcAdventure.Events;
using StardewValley;
using System.Linq;

namespace NpcAdventure.Patches
{
    internal class MailBoxPatch
    {
        private static SpecialModEvents events;

        /// <summary>
        /// This patches mailbox read method on gamelocation and allow call custom logic 
        /// for NPC Adventures mail letters only. For other mails call vanilla logic.
        /// </summary>
        /// <param name="__instance">Game location</param>
        /// <returns></returns>
        internal static bool Prefix(ref GameLocation __instance)
        {
            if (Game1.mailbox.Count > 0)
            {
                var letter = Game1.mailbox.First();

                if (letter != null && letter.StartsWith("npcAdventures."))
                {
                    string[] parsedLetter = letter.Split('.');
                    MailEventArgs args = new MailEventArgs
                    {
                        FullLetterKey = letter,
                        LetterKey = parsedLetter.Length > 1 ? parsedLetter[1] : null,
                        Mailbox = Game1.player.mailbox,
                        Player = Game1.player,
                    };

                    events.FireMailOpen(__instance, args);

                    return false;
                }
            }

            return true;
        }

        internal static void Setup(HarmonyInstance harmony, SpecialModEvents events)
        {
            MailBoxPatch.events = events;

            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.mailbox)),
                prefix: new HarmonyMethod(typeof(MailBoxPatch), nameof(MailBoxPatch.Prefix))
            );
        }
    }
}
