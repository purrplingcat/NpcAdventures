using Harmony;
using Microsoft.Xna.Framework.Graphics;
using NpcAdventure.Events;
using NpcAdventure.Internal;
using NpcAdventure.StateMachine;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NpcAdventure.StateMachine.CompanionStateMachine;

namespace NpcAdventure.Patches
{
    internal class CompanionSayHiPatch : Patch<CompanionSayHiPatch>
    {
        private CompanionManager Manager { get; set; }

        public override string Name => nameof(CompanionSayHiPatch);

        public CompanionSayHiPatch(CompanionManager manager)
        {
            this.Manager = manager;
            Instance = this;
        }

        internal static bool Before_sayHiTo(ref NPC __instance, Character c)
        {
            try
            {
                if (Instance.Manager != null && Instance.Manager.PossibleCompanions.TryGetValue(__instance.Name, out CompanionStateMachine csm))
                {
                    // Avoid say hi to monsters while companion is recruited
                    return !(csm.CurrentStateFlag == StateFlag.RECRUITED && c is Monster);
                }

                return true;
            } 
            catch (Exception ex)
            {
                Instance.LogFailure(ex, nameof(Before_sayHiTo));
                return true;
            }
        }

        protected override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.sayHiTo)),
                prefix: new HarmonyMethod(typeof(CompanionSayHiPatch), nameof(CompanionSayHiPatch.Before_sayHiTo))
            );
        }
    }
}
