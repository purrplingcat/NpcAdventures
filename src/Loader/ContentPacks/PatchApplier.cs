using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.Loader.ContentPacks
{
    internal class PatchApplier
    {

        public PatchApplier(IMonitor monitor, bool paranoid)
        {
            this.Monitor = monitor;
            this.paranoid = paranoid;
        }

        public IMonitor Monitor { get; }

        private readonly bool paranoid;

        public bool Apply<TKey, TValue>(Dictionary<TKey, TValue> target, IEnumerable<ManagedPatch> patches, string targetName)
        {
            if (patches.Count() < 1)
            {
                return false;
            }

            string contentPackName;
            foreach (var patch in patches)
            {
                contentPackName = patch.Owner.Pack.Manifest.Name;

                if (patch.Change.Action == "Replace")
                {
                    if (target.Count > 0)
                        this.Monitor.Log(
                            $"Content pack `{contentPackName}` patch `{patch.Change.LogName}` replaces all contents for `{targetName}`.",
                            this.paranoid ? LogLevel.Alert : LogLevel.Trace);
                    target.Clear(); // Load replaces all content
                }

                var isLocalized = !string.IsNullOrEmpty(patch.Change.Locale);
                var patchData = patch.LoadData<TKey, TValue>();

                AssetPatchHelper.ApplyPatch(target, patchData);
                this.Monitor.Log($"Content pack `{contentPackName}` applied{(isLocalized ? $" `{patch.Change.Locale}` translation" : "")} patch `{patch.Change.LogName}` ({patch.Change.Action} type) for `{targetName}`");
            }

            return true;
        }
    }
}
