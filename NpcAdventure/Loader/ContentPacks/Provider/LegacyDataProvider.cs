using NpcAdventure.Loader.ContentPacks.Data;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace NpcAdventure.Loader.ContentPacks.Provider
{
    class LegacyDataProvider : IDataProvider
    {
        private readonly bool paranoid;

        public LegacyDataProvider(ManagedContentPack managed, bool paranoid = false)
        {
            this.Managed = managed;
            this.paranoid = paranoid;
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
                if (patch.Action == "Replace" && this.Managed.Contents.AllowUnsafePatches)
                {
                    if (this.paranoid && target.Count > 0)
                        this.Monitor.Log($"Content pack `{this.Managed.Pack.Manifest.Name}` patch `{patch.LogName}` replaces all contents for `{path}`.", LogLevel.Alert);
                    target.Clear(); // Load replaces all content
                }

                var possiblyOverrided = AssetPatchHelper.ApplyPatch(target, this.Managed.Pack.LoadAsset<Dictionary<TKey, TValue>>(patch.FromFile), patch.AllowOverrides);
                this.Monitor.Log($"Applied content patch `{patch.LogName}` from content pack `{this.Managed.Pack.Manifest.Name}`");

                if (this.paranoid && possiblyOverrided.Count() > 0)
                {
                    this.Monitor.Log($"Found content data key conflicts for `{patch.Target}` by patch `{patch.LogName}` in content pack `{this.Managed.Pack.Manifest.Name}`.", LogLevel.Alert);
                    this.Monitor.Log($"   Conflicted keys: {string.Join(", ", possiblyOverrided)}", LogLevel.Alert);
                    this.Monitor.Log($"Affected parts of contents {(patch.AllowOverrides ? "ARE OVERIDDEN!!!" : "NOT overidden.")}", LogLevel.Alert);
                }
            }

            return true;
        }

        private List<LegacyChanges> GetPatchesForAsset(string path, string action)
        {
            var locale = this.Managed.Pack.Translation.Locale;
            var patches = this.Managed.Contents.Changes
                .Where((p) => p.Action.Equals(action) && p.Target.Equals(path) && !p.Disabled)
                .Where((p) => string.IsNullOrEmpty(p.Locale) || p.Locale.ToLower().Equals(locale))
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
