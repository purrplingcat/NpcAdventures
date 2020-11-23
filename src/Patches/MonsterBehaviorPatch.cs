using Harmony;
using Microsoft.Xna.Framework;
using NpcAdventure.StateMachine;
using NpcAdventure.StateMachine.State;
using PurrplingCore.Patching;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Linq;
using static NpcAdventure.StateMachine.CompanionStateMachine;

namespace NpcAdventure.Patches
{
    internal class MonsterBehaviorPatch : Patch<MonsterBehaviorPatch>
    {
        private CompanionManager Manager { get; set; }

        public override string Name => nameof(MonsterBehaviorPatch);

        public MonsterBehaviorPatch(CompanionManager manager)
        {
            this.Manager = manager;
            Instance = this;
        }

        private static bool Before_behaviorAtGameTick(ref Monster __instance, GameTime time)
        {
            try
            {
                if (__instance is Duggy duggy && Instance.Manager.IsRecruitedAnyone() && Instance.Manager.GetRecruitedCompanion().GetCurrentStateBehavior() is RecruitedState rstate)
                {
                    if (rstate.GetAI().IsLovedMonster(duggy))
                    {
                        duggy.Sprite.loop = false;
                        duggy.IsInvisible = true;
                        duggy.Sprite.CurrentFrame = 10;

                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Instance.LogFailure(ex, nameof(Before_behaviorAtGameTick));
                return true;
            }
        }

        protected override void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Monster), nameof(Monster.behaviorAtGameTick)),
                prefix: new HarmonyMethod(typeof(MonsterBehaviorPatch), nameof(MonsterBehaviorPatch.Before_behaviorAtGameTick))
            );
        }
    }
}
