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
        private readonly ITranslationHelper translation;
        private readonly IMonitor monitor;
        private readonly List<ManagedContentPack> packs;
        private readonly Dictionary<string, List<IManifest>> knownReplaceAplicators;

        /// <summary>
        /// Provides patches from content packs into mod's content
        /// </summary>
        /// <param name="modName"></param>
        /// <param name="helper"></param>
        /// <param name="monitor"></param>
        public ContentPackManager(IContentPackHelper helper, ITranslationHelper translation, IMonitor monitor)
        {
            this.translation = translation;
            this.monitor = monitor;
            this.knownReplaceAplicators = new Dictionary<string, List<IManifest>>();
            this.packs = this.LoadPacks(helper);
        }

        private void CheckForMultipleReplacers(List<ManagedContentPack> packs)
        {
            var replacers = from pack in packs
                            from change in pack.Contents.Changes
                            where change.Action == "Replace"
                            select Tuple.Create(change, pack.Pack.Manifest);
            var multipleReplacers = from multiple in (from replacer in replacers group replacer by replacer.Item1.Target)
                                    where multiple.Count() > 1
                                    select multiple;
            var incompatiblePacks = from groupedIncompatibles in multipleReplacers.Select(g => g.Select(r => r.Item2).Distinct())
                                    where groupedIncompatibles.Count() > 1
                                    from incompatible in groupedIncompatibles
                                    select incompatible;

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
        /// Parse content pack definitions and loads possible patches
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        private List<ManagedContentPack> LoadPacks(IContentPackHelper helper)
        {
            var managed = new List<ManagedContentPack>();

            this.monitor.Log("Loading content packs ...");

            // Try to load content packs and their's patches
            foreach (var pack in helper.GetOwned())
            {
                try
                {
                    var managedPack = new ManagedContentPack(pack, this.translation, this.monitor);

                    managedPack.Load();
                    managed.Add(managedPack);
                } catch (ContentPackException e)
                {
                    this.monitor.Log($"Unable to load content pack `{pack.Manifest.Name}`:\n   {e.Message}", LogLevel.Error);
                }
            }

            this.monitor.Log($"Loaded {managed.Count} content packs", LogLevel.Info);
            this.CheckForMultipleReplacers(managed);

            return managed;
        }

        public bool Apply<TKey, TValue>(Dictionary<TKey, TValue> target, string path)
        {
            bool applied = false;

            foreach (var pack in this.packs)
            {
                applied |= pack.Apply(target, path);
            }

            return applied;
        }
    }
}
