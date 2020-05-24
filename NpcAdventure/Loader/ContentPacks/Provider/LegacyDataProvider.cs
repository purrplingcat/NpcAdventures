using NpcAdventure.Loader.ContentPacks.Data;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Dictionary<TKey, TValue> Provide<TKey, TValue>(string path)
        {
            var baseData = new Dictionary<TKey, TValue>();
            var patches = new List<LegacyChanges>();

            patches.AddRange(this.GetPatchesForAsset(path, "Load"));
            patches.AddRange(this.GetPatchesForAsset(path, "Edit"));

            if (patches.Count() < 1)
            {
                return null;
            }

            foreach (var patch in patches)
            {
                AssetPatchHelper.ApplyPatch(baseData, this.Managed.Pack.LoadAsset<Dictionary<TKey, TValue>>(patch.FromFile));
                this.Monitor.Log($"Applied content patch `{patch.LogName}` from content pack `{this.Managed.Pack.Manifest.Name}`");
            }

            return baseData;
        }

        private List<LegacyChanges> GetPatchesForAsset(string path, string action)
        {
            var patches = this.Managed.Contents.Changes
                .Where((p) => p.Action.Equals(action) && p.Target.Equals(path))
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
