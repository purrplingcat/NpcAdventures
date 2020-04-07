using Harmony;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NpcAdventure.Internal.Patching
{
    internal class GamePatcher
    {
        private readonly IMonitor monitor;
        private readonly bool paranoid;
        private readonly HarmonyInstance harmony;

        public GamePatcher(IMonitor monitor, bool paranoid = true)
        {
            this.monitor = monitor;
            this.paranoid = paranoid;
            this.harmony = HarmonyInstance.Create("cz.purrplingcat.npcadventures");
        }

        public void CheckPatches()
        {
            try
            {
                var methods = this.harmony.GetPatchedMethods();

                foreach (var method in methods)
                {
                    Harmony.Patches info = this.harmony.GetPatchInfo(method);

                    if (info.Owners.Contains(this.harmony.Id) && info.Owners.Count > 1)
                    {
                        IEnumerable<string> foreignOwners = info.Owners.Where(owner => owner != this.harmony.Id);

                        this.monitor.Log($"Detected another patches for game method '{method.FullDescription()}'. This method was patched too by: {string.Join(", ", foreignOwners)}",
                            this.paranoid ? LogLevel.Warn : LogLevel.Debug);
                    }
                }
            } catch (Exception ex)
            {
                this.monitor.Log("Unable to check game patches. See log for more details.", LogLevel.Error);
                this.monitor.Log(ex.ToString(), LogLevel.Trace);
            }
        }

        /// <summary>
        /// Apply game patches
        /// </summary>
        /// <param name="patches"></param>
        public void Apply(params IPatch[] patches)
        {
            foreach (IPatch patch in patches)
            {
                try
                {
                    patch.Apply(this.harmony, this.monitor);
                    this.monitor.Log($"Applied runtime patch '{patch.Name}' to the game.");
                } catch (Exception ex)
                {
                    this.monitor.Log($"Couldn't apply runtime patch '{patch.Name}' to the game. Some features may not works correctly. See log file for more details.", LogLevel.Error);
                    this.monitor.Log(ex.ToString(), LogLevel.Trace);
                }
            }
        }
    }
}
