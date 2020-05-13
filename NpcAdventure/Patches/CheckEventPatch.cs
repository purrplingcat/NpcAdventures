﻿using Harmony;
using NpcAdventure.Story;
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
        }
    }
}
