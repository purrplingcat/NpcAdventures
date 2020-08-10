using NpcAdventure.StateMachine;
using NpcAdventure.StateMachine.State;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NpcAdventure
{
    public class NpcAdventureModApi
    {
        private NpcAdventureMod npcAdventureMod;

        internal NpcAdventureModApi(NpcAdventureMod npcAdventureMod)
        {
            this.npcAdventureMod = npcAdventureMod;
        }
        public bool CanRecruit()
        {
            return npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.CanRecruit();
        }
        public string[] GetPossibleCompanions()
        {
            return npcAdventureMod.CompanionManager.PossibleCompanions.Keys.ToArray();
        }
        public bool IsPossibleCompanion(string npc)
        {
            return npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.PossibleCompanions != null && npcAdventureMod.CompanionManager.PossibleCompanions.ContainsKey(npc);
        }
        public bool CanRecruit(NPC npc)
        {
            return npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.PossibleCompanions != null && npcAdventureMod.CompanionManager.PossibleCompanions.TryGetValue(npc.Name, out CompanionStateMachine csm) && csm != null && npc != null && csm.Name == npc.Name && csm.CurrentStateFlag == CompanionStateMachine.StateFlag.AVAILABLE && csm.CanPerformAction();
        }
        public bool IsRecruited(NPC npc)
        {
            if (npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.PossibleCompanions != null && npcAdventureMod.CompanionManager.PossibleCompanions.TryGetValue(npc.Name, out CompanionStateMachine csm) && csm != null && npc != null && csm.Name == npc.Name && csm.CurrentStateFlag == CompanionStateMachine.StateFlag.RECRUITED)
            {
                return true;
            }
            return false;
        }
        public bool IsAvailable(NPC npc)
        {
            if (npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.PossibleCompanions != null && npcAdventureMod.CompanionManager.PossibleCompanions.TryGetValue(npc.Name, out CompanionStateMachine csm) && csm != null && npc != null && csm.Name == npc.Name && csm.CurrentStateFlag == CompanionStateMachine.StateFlag.AVAILABLE)
            {
                return true;
            }
            return false;
        }
        public int GetNpcState(NPC npc)
        {
            if (npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.PossibleCompanions != null && npcAdventureMod.CompanionManager.PossibleCompanions.TryGetValue(npc.Name, out CompanionStateMachine csm) && csm != null && npc != null && csm.Name == npc.Name)
            {
                return (int) csm.CurrentStateFlag;
            }
            return -1;
        }
        public bool RecruitCompanion(Farmer who, NPC npc, GameLocation location, bool skipDialogue = false)
        {
            if (!CanRecruit())
                return false;
            if (skipDialogue)
            {
                if(npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.PossibleCompanions != null && npcAdventureMod.CompanionManager.PossibleCompanions.TryGetValue(npc.Name, out CompanionStateMachine csm) && csm != null && npc != null && csm.Name == npc.Name && csm.CurrentStateFlag == CompanionStateMachine.StateFlag.AVAILABLE && csm.CanPerformAction() && Game1.player.getFriendshipHeartLevelForNPC(npc.Name) >= csm.CompanionManager.Config.HeartThreshold && Game1.timeOfDay < 2200)
                        return true;
                return false;
            }
            else
                return npcAdventureMod != null && npcAdventureMod.CompanionManager != null && npcAdventureMod.CompanionManager.PossibleCompanions != null && npcAdventureMod.CompanionManager.PossibleCompanions.TryGetValue(npc.Name, out CompanionStateMachine csm) && csm != null && npc != null && csm.Name == npc.Name && csm.CheckAction(who, location);
        }
    }
}