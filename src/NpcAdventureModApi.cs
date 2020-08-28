using NpcAdventure.StateMachine;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using static NpcAdventure.StateMachine.CompanionStateMachine;

namespace NpcAdventure
{
    public interface INpcAdventureModApi
    {
        bool CanRecruitCompanions();
        IEnumerable<NPC> GetPossibleCompanions();
        bool IsPossibleCompanion(string npc);
        bool IsPossibleCompanion(NPC npc);
        bool CanAskToFollow(NPC npc);
        bool CanRecruit(Farmer farmer, NPC npc);
        bool IsRecruited(NPC npc);
        bool IsAvailable(NPC npc);
        string GetNpcState(NPC npc);
        bool RecruitCompanion(Farmer farmer, NPC npc);
        string GetFriendSpecificDialogueText(Farmer farmer, NPC npc, string key);
        string LoadString(string path);
        string LoadString(string path, string substitution);
        string LoadString(string path, string[] substitutions);
    }
    public class NpcAdventureModApi
    {
        private NpcAdventureMod npcAdventureMod;
        internal NpcAdventureModApi(NpcAdventureMod npcAdventureMod)
        {
            this.npcAdventureMod = npcAdventureMod;
        }
        public bool CanRecruitCompanions()
        {
            return npcAdventureMod.CompanionManager.CanRecruit();
        }
        public IEnumerable<NPC> GetPossibleCompanions()
        {
            return npcAdventureMod.CompanionManager.PossibleCompanions.Select(s => s.Value.Companion);
        }
        public bool IsPossibleCompanion(string npc)
        {
            return npcAdventureMod.CompanionManager.PossibleCompanions.ContainsKey(npc);
        }
        public bool IsPossibleCompanion(NPC npc)
        {
            return IsPossibleCompanion(npc.Name);
        }
        public bool CanAskToFollow(NPC npc)
        {
            if (!IsPossibleCompanion(npc))
                return false;
            var csm = npcAdventureMod.CompanionManager.PossibleCompanions[npc.Name];
            return csm != null && csm.Name == npc.Name && csm.CurrentStateFlag == StateFlag.AVAILABLE && csm.CanPerformAction();
        }
        public bool CanRecruit(Farmer farmer, NPC npc)
        {
            if (!IsPossibleCompanion(npc))
                return false;
            var csm = npcAdventureMod.CompanionManager.PossibleCompanions[npc.Name];
            return farmer.getFriendshipHeartLevelForNPC(npc.Name) >= csm.CompanionManager.Config.HeartThreshold && Game1.timeOfDay < 2200;
        }
        public bool IsRecruited(NPC npc)
        {
            if (!IsPossibleCompanion(npc))
                return false;
            var csm = npcAdventureMod.CompanionManager.PossibleCompanions[npc.Name];
            return csm != null && csm.Name == npc.Name && csm.CurrentStateFlag == StateFlag.RECRUITED;
        }
        public bool IsAvailable(NPC npc)
        {
            if (!IsPossibleCompanion(npc))
                return false;
            var csm = npcAdventureMod.CompanionManager.PossibleCompanions[npc.Name];
            return csm != null && csm.Name == npc.Name && csm.CurrentStateFlag == StateFlag.AVAILABLE;
        }
        public string GetNpcState(NPC npc)
        {
            if (!IsPossibleCompanion(npc))
                return null;
            var csm = npcAdventureMod.CompanionManager.PossibleCompanions[npc.Name];
            if (csm != null && csm.Name == npc.Name)
                return Enum.GetName(typeof(StateFlag), csm.CurrentStateFlag);
            return null;
        }
        public bool RecruitCompanion(Farmer farmer, NPC npc)
        {
            var csm = npcAdventureMod.CompanionManager.PossibleCompanions[npc.Name];

            if (CanRecruitCompanions() && CanAskToFollow(npc) && CanRecruit(farmer, npc))
            {
                csm.Recruit();
                return true;
            }
            return false;
        }
        public string GetFriendSpecificDialogueText(Farmer farmer, NPC npc, string key)
        {
            if (npc == null)
                return null;
            string text = null;
            var csm = npcAdventureMod.CompanionManager.PossibleCompanions[npc.Name];
            if (csm != null)
                text = csm.Dialogues.GetFriendSpecificDialogueText(farmer, key);
            return text;
        }
        public string LoadString(string path)
        {
            return npcAdventureMod.ContentLoader.LoadString(path);
        }
        public string LoadString(string path, string substitution)
        {
            return npcAdventureMod.ContentLoader.LoadString(path, substitution);
        }
        public string LoadString(string path, string[] substitutions)
        {
            return npcAdventureMod.ContentLoader.LoadString(path, substitutions);
        }
    }
}