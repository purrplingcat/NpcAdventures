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
            var contentPackName = this.Managed.Pack.Manifest.Name;

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
                    if (target.Count > 0)
                        this.Monitor.Log(
                            $"Content pack `{contentPackName}` patch `{patch.LogName}` replaces all contents for `{path}`.", 
                            this.paranoid ? LogLevel.Alert : LogLevel.Trace);
                    target.Clear(); // Load replaces all content
                }

                var isLocalized = !string.IsNullOrEmpty(patch.Locale);
                var patchData = this.Managed.Pack.LoadAsset<Dictionary<TKey, TValue>>(patch.FromFile);
                var possiblyOverrided = AssetPatchHelper.ApplyPatch(target, patchData, patch.CanOverride || isLocalized);

                this.Monitor.Log($"Applied content patch `{patch.LogName}` from content pack `{contentPackName}`");

                if (isLocalized && patchData.Count() > 0)
                {
                    var covered = possiblyOverrided.Count();
                    var extraAdded = patchData.Count() - covered;

                    this.Monitor.Log($"Applied content pack `{contentPackName}` translation patch `{patch.LogName}` to `{patch.Locale}` for `{path}`, covered {covered} keys ({extraAdded} extra added)");
                }
                else if (possiblyOverrided.Count() > 0)
                {
                    var loglevel = (this.paranoid || !patch.CanOverride) ? LogLevel.Alert : LogLevel.Trace;
                    this.Monitor.Log($"Found content data key conflicts for `{patch.Target}` by patch `{patch.LogName}` in content pack `{contentPackName}`.", loglevel);
                    this.Monitor.Log($"   Conflicted keys: {string.Join(", ", possiblyOverrided)}", loglevel);
                    this.Monitor.Log($"Affected parts of contents {(patch.CanOverride ? "ARE OVERRIDDEN!!!" : "are NOT overridden.")}", loglevel);
                    if (!patch.CanOverride)
                        this.Monitor.Log(
                            $"If you want to override these keys, you can allow it by set `CanOverride: true` on this patch and allow unsafe patches for this content pack by `AllowUnsafePatches: true`", loglevel);
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
