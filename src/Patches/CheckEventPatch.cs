using Harmony;
using Microsoft.Xna.Framework;
using NpcAdventure.Story;
using PurrplingCore.Patching;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.Patches
{
    class CheckEventPatch : Patch<CheckEventPatch>
    {
        private GameMaster GameMaster { get; }
        public override string Name => nameof(CheckEventPatch);

        public CheckEventPatch(GameMaster master)
        {
            this.GameMaster = master;
            Instance = this;
        }

        private static void After_checkForEvents(GameLocation __instance)
        {
            try
            {
                if (!Game1.eventUp && __instance.currentEvent == null && Instance.GameMaster.Mode != GameMasterMode.OFFLINE)
                {
                    Instance.GameMaster.CheckForEvents(__instance, Game1.MasterPlayer);
                }
            }
            catch (Exception ex)
            {
                Instance.LogFailure(ex, nameof(After_checkForEvents));
            }
        }

        protected override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.checkForEvents)),
                postfix: new HarmonyMethod(typeof(CheckEventPatch), nameof(CheckEventPatch.After_checkForEvents))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Event), nameof(Event.command_move)),
                prefix: new HarmonyMethod(typeof(CheckEventPatch), nameof(CheckEventPatch.Before_command_move))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Event), nameof(Event.command_faceDirection)),
                prefix: new HarmonyMethod(typeof(CheckEventPatch), nameof(CheckEventPatch.Before_command_faceDirection))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Event), nameof(Event.command_showFrame)),
                prefix: new HarmonyMethod(typeof(CheckEventPatch), nameof(CheckEventPatch.Before_command_faceDirection))
            );
        }

        private static bool Before_command_faceDirection(string[] split)
        {
            split[1] = split[1].Replace('_', ' ');

            return true;
        }

        private static bool Before_command_move(string[] split)
        {
            for (int i = 1; i < split.Length && split.Length - i >= 3; i += 4)
            {
                split[i] = split[i].Replace('_', ' ');
            }

            return true;
        }
    }
}
