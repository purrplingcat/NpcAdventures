using NpcAdventure.Loader.ContentPacks;
using NpcAdventure.Loader.ContentPacks.Data;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NpcAdventure.Loader
{
    class ContentPackManager
    {
        private readonly IMonitor monitor;
        private readonly bool paranoid;
        private readonly List<ManagedContentPack> packs;

        /// <summary>
        /// Provides patches from content packs into mod's content
        /// </summary>
        /// <param name="monitor"></param>
        public ContentPackManager(IMonitor monitor, bool paranoid = false)
        {
            this.monitor = monitor;
            this.paranoid = paranoid;
            this.packs = new List<ManagedContentPack>();
        }

        /// <summary>
        /// Loads and verify content packs.
        /// </summary>
        /// <returns></returns>
        public void LoadContentPacks(IEnumerable<IContentPack> contentPacks)
        {
            this.monitor.Log("Loading content packs ...");

            // Try to load content packs and their's patches
            foreach (var pack in contentPacks)
            {
                try
                {
                    var managedPack = new ManagedContentPack(pack, this.monitor, this.paranoid);

                    managedPack.Load();
                    this.packs.Add(managedPack);
                } catch (ContentPackException e)
                {
                    this.monitor.Log($"Unable to load content pack `{pack.Manifest.Name}`:", LogLevel.Error);
                    this.monitor.Log($"   {e.Message}", LogLevel.Error);
                }
            }

            this.monitor.Log($"Loaded {this.packs.Count} content packs:", LogLevel.Info);
            this.packs.ForEach(mp => this.monitor.Log($"   {mp.Pack.Manifest.Name} {mp.Pack.Manifest.Version} by {mp.Pack.Manifest.Author}", LogLevel.Info));
            this.CheckCurrentFormat(this.packs);
            this.CheckUnsafe(this.packs);
            this.CheckForDangerousReplacers(this.packs);
        }

        /// <summary>
        /// Apply content packs to the target
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="target">Target to be patched/param>
        /// <param name="path">Which patch (asset path)</param>
        /// <returns>True if any patch was applied on the target</returns>
        public bool Apply<TKey, TValue>(Dictionary<TKey, TValue> target, string path)
        {
            bool applied = false;

            foreach (var pack in this.packs)
            {
                applied |= pack.Apply(target, path);
            }

            return applied;
        }

        /// <summary>
        /// Check format version of available content packs 
        /// and inform user if any pack uses old format
        /// </summary>
        /// <param name="packs"></param>
        private void CheckCurrentFormat(List<ManagedContentPack> packs)
        {
            var currentFormatVersion = ManagedContentPack.SUPPORTED_FORMATS[ManagedContentPack.SUPPORTED_FORMATS.Length - 1];
            var usesOldFormat = from pack in packs
                                where pack.FormatVersion.IsOlderThan(currentFormatVersion)
                                select pack;

            if (usesOldFormat.Count() > 0)
            {
                this.monitor.Log($"Detected {usesOldFormat.Count()} content packs which use old format:", LogLevel.Info);
                this.monitor.Log($"   It's recommended to update these content packs to the new format.", LogLevel.Info);
                usesOldFormat.ToList().ForEach(p => this.monitor.Log($"   - {p.Pack.Manifest.Name} (format {p.FormatVersion})", LogLevel.Info));
            }
        }

        /// <summary>
        /// Check if given patches are safe or unsafe 
        /// (may apply replaces and overrides)
        /// </summary>
        /// <param name="packs"></param>
        private void CheckUnsafe(List<ManagedContentPack> packs)
        {
            var unsafePacks = from pack in packs
                              where pack.Contents.AllowUnsafePatches == true
                              select pack;

            if (unsafePacks.Count() > 0)
            {
                var loglevel = this.paranoid ? LogLevel.Warn : LogLevel.Info;
                this.monitor.Log($"Detected {unsafePacks.Count()} content packs with allowed unsafe patches:", loglevel);
                this.monitor.Log("   These content packs can replace some contents in mod and/or in other content packs (full content replace, existing keys override).", loglevel);
                unsafePacks.ToList().ForEach(p => this.monitor.Log($"   - {p.Pack.Manifest.Name} {(p.FormatVersion.IsOlderThan("1.3") ? "(DANGEROUS! Uses unsafe format)" : "")}", loglevel));
            }
        }

        /// <summary>
        /// Check if given patches contains multiple replacers to the same target path.
        /// If any patches contains multiple replacers to the same target, they will be disabled
        /// to avoid dangerous behavior.
        /// </summary>
        /// <param name="packs"></param>
        private void CheckForDangerousReplacers(List<ManagedContentPack> packs)
        {
            ExtractPacksWithReplacers(packs,
                out IEnumerable<IGrouping<string, Tuple<LegacyChanges, IManifest>>> multipleReplacers,
                out IEnumerable<IManifest> incompatiblePacks);

            foreach (var replacerGroup in multipleReplacers)
            {
                this.monitor.Log($"Multiple content replacers was detected for `{replacerGroup.Key}`:", LogLevel.Error);
                foreach (var replacer in replacerGroup)
                {
                    this.monitor.Log($"   - Patch `{replacer.Item1.LogName}` in content pack `{replacer.Item2.Name}`", LogLevel.Error);
                    replacer.Item1.Disabled = true;
                }
                this.monitor.Log("   All affected patches was disabled and none of them will be applyied, but some problems may be caused while gameplay.", LogLevel.Error);
            }

            if (incompatiblePacks.Count() > 0)
            {
                this.monitor.Log($"These content packs are probably incompatible with each other:", LogLevel.Error);
                incompatiblePacks.ToList().ForEach(p => this.monitor.Log($"   - {p.Name}", LogLevel.Error));
                this.monitor.Log($"To resolve this problem you can remove some of them.", LogLevel.Error);
            }
        }

        /// <summary>
        /// Fetch and filter the patches which contains replacers 
        /// and/or are not potentially compatible withc each other.
        /// </summary>
        /// <param name="packs"></param>
        /// <param name="multipleReplacers"></param>
        /// <param name="incompatiblePacks"></param>
        private static void ExtractPacksWithReplacers(List<ManagedContentPack> packs,
            out IEnumerable<IGrouping<string, Tuple<LegacyChanges, IManifest>>> multipleReplacers,
            out IEnumerable<IManifest> incompatiblePacks)
        {
            var replacers = from pack in packs
                            from change in pack.Contents.Changes
                            where change.Action == "Replace"
                            select Tuple.Create(change, pack.Pack.Manifest);
            multipleReplacers = from multiple in (from replacer in replacers group replacer by replacer.Item1.Target)
                                where multiple.Count() > 1
                                select multiple;
            incompatiblePacks = from groupedIncompatibles in multipleReplacers.Select(g => g.Select(r => r.Item2).Distinct())
                                where groupedIncompatibles.Count() > 1
                                from incompatible in groupedIncompatibles
                                select incompatible;
        }
    }
}
