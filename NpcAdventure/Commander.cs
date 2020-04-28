using System.Linq;
using NpcAdventure.StateMachine;
using NpcAdventure.Story;
using NpcAdventure.Utils;
using StardewModdingAPI;
using StardewValley;

namespace NpcAdventure
{
    internal class Commander
    {
        private readonly NpcAdventureMod npcAdventureMod;
        private readonly IMonitor monitor;

        private Commander(NpcAdventureMod npcAdventureMod)
        {
            this.npcAdventureMod = npcAdventureMod;
            this.monitor = npcAdventureMod.Monitor;
            this.SetupCommands(npcAdventureMod.Helper.ConsoleCommands);
        }

        internal void SetupCommands(ICommandHelper consoleCommands)
        {
            if (!this.npcAdventureMod.Config.EnableDebug)
                return;

            consoleCommands.Add("npcadventure_eligible", "Make player eligible to recruit a companion (server or singleplayer only)", this.Eligible);
            consoleCommands.Add("npcadventure_recruit", "Recruit an NPC as companion (server or singleplayer only)", this.Recruit);
            this.monitor.Log("Registered debug commands", LogLevel.Info);
        }

        private void Eligible(string command, string[] args)
        {
            if (Context.IsWorldReady && Context.IsMainPlayer && this.npcAdventureMod.GameMaster.Mode == GameMasterMode.MASTER)
            {
                this.npcAdventureMod.GameMaster.Data.GetPlayerState(Game1.player).isEligible = true;
                this.npcAdventureMod.GameMaster.SyncData();
                this.monitor.Log("Player is now eligible for recruit companion.", LogLevel.Info);
            } else
            {
                this.monitor.Log("Can't eligible player when game is not loaded, in non-adventure mode or not running on server!", LogLevel.Alert);
            }
        }

        private void Recruit(string command, string[] args)
        {
            if (!Context.IsWorldReady || !Context.IsMainPlayer)
            {
                this.monitor.Log("Can't recruit a companion when game is not loaded or player is not main player.");
                return;
            }

            if (args.Length < 1)
            {
                this.monitor.Log("Missing NPC name");
                return;
            }

            string npcName = args[0];
            Farmer farmer = this.npcAdventureMod.CompanionManager.Farmer;
            CompanionStateMachine recruited = this.npcAdventureMod
                .CompanionManager
                .PossibleCompanions
                .Values
                .FirstOrDefault((_csm) => _csm.CurrentStateFlag == CompanionStateMachine.StateFlag.RECRUITED);

            if (recruited != null)
            {
                this.monitor.Log($"You have recruited ${recruited.Name}, unrecruit them first!");
                return;
            }

            if (!this.npcAdventureMod.CompanionManager.PossibleCompanions.TryGetValue(npcName, out CompanionStateMachine csm))
            {
                this.monitor.Log($"Cannot recruit '{npcName}' - NPC is not recruitable or doesn't exists");
                return;
            }

            Helper.WarpTo(csm.Companion, farmer.currentLocation, farmer.getTileLocationPoint());
            csm.Recruit();
        }

        public static Commander Register(NpcAdventureMod mod)
        {
            return new Commander(mod);
        }
    }
}
