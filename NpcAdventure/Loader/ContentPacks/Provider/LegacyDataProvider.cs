using NpcAdventure.Loader.ContentPacks.Data;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace NpcAdventure.Loader.ContentPacks.Provider
{
    class LegacyDataProvider : IDataProvider
    {
        public LegacyDataProvider(ManagedContentPack managed)
        {
            this.Managed = managed;
            this.Monitor = managed.Monitor;
        }

        public ManagedContentPack Managed { get; }
        public IMonitor Monitor { get; private set; }

        public bool Apply<TKey, TValue>(Dictionary<TKey, TValue> target, string path)
        {
            var patches = new List<LegacyChanges>();

            patches.AddRange(this.GetPatchesForAsset(path, "Replace"));
            patches.AddRange(this.GetPatchesForAsset(path, "Patch"));

            if (patches.Count() < 1)
            {
                return false;
            }

            foreach (var patch in patches)
            {
                if (patch.Action == "Replace")
                {
                    if (target.Count > 0)
                        this.Monitor.Log($"Content pack `{this.Managed.Pack.Manifest.Name}` patch `{patch.LogName}` replaces contents for `{path}`.", LogLevel.Alert);
                    target.Clear(); // Load replaces all content
                }

                AssetPatchHelper.ApplyPatch(target, this.Managed.Pack.LoadAsset<Dictionary<TKey, TValue>>(patch.FromFile));
                this.Monitor.Log($"Applied content patch `{patch.LogName}` from content pack `{this.Managed.Pack.Manifest.Name}`");
            }

            return true;
        }

        private List<LegacyChanges> GetPatchesForAsset(string path, string action)
        {
            var patches = this.Managed.Contents.Changes
                .Where((p) => p.Action.Equals(action) && p.Target.Equals(path) && !p.Disabled)
                .Where((p) => string.IsNullOrEmpty(p.Locale) || p.Locale.ToLower().Equals(this.Managed.Translation.Locale))
                .ToList();

            patches.Sort((a, b) => {
                if (string.IsNullOrEmpty(a.Locale) && !string.IsNullOrEmpty(b.Locale)) return -1;
                else if (!string.IsNullOrEmpty(a.Locale)) return 1;
                return 0;
            });

            return patches;
        }
    }
}
