using NpcAdventure.Story;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure
{
    internal class Commander
    {
        private readonly GameMaster gameMaster;
        private readonly CompanionManager companionManager;
        private readonly IMonitor monitor;

        public Commander(GameMaster gameMaster, CompanionManager companionManager, IMonitor monitor)
        {
            this.gameMaster = gameMaster;
            this.companionManager = companionManager;
            this.monitor = monitor;
        }

        internal void SetupCommands(ICommandHelper consoleCommands)
        {
            consoleCommands.Add("npcadventure_eligible", "Make player eligible to recruit a companion (server only)", this.Eligible);
            this.monitor.Log("Registered debug commands", LogLevel.Info);
        }

        private void Eligible(string command, string[] args)
        {
            if (Context.IsWorldReady && Context.IsMainPlayer && this.gameMaster.Mode == GameMasterMode.MASTER)
            {
                this.gameMaster.Data.GetPlayerState(Game1.player).isEligible = true;
                this.gameMaster.SyncData();
                this.monitor.Log("Player is now eligible for recruit companion.", LogLevel.Info);
            } else
            {
                this.monitor.Log("Can't eligible player when game is not loaded, in non-adventure mode or not running on server!", LogLevel.Alert);
            }
        }
    }
}
