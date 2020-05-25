using NpcAdventure.Loader.ContentPacks;
using NpcAdventure.Loader.ContentPacks.Data;
using StardewModdingAPI;
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

        private void SaveUsedReplaceAplicators(Contents packContents, IManifest packManifest)
        {
            var targetsToReplace = from change in packContents.Changes
                             where change.Action == "Replace"
                             select change.Target;

            foreach (var target in targetsToReplace)
            {
                if (this.knownReplaceAplicators.ContainsKey(target))
                {
                    this.knownReplaceAplicators[target].Add(packManifest);
                }
                else
                { 
                    this.knownReplaceAplicators.Add(target, new List<IManifest> { packManifest });
                }
            }
        }

        private void CheckMultipleUsedReplaceAplicators()
        {
            foreach (var aplicator in this.knownReplaceAplicators)
            {
                this.monitor.Log($"Detected multiple replace patches for `{aplicator.Key}`.\n    replace patches applied by: {string.Join(", ", aplicator.Value.Select(m => m.Name))}", LogLevel.Warn);
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

            // Try to load content packs and their's patches
            foreach (var pack in helper.GetOwned())
            {
                try
                {
                    var managedPack = new ManagedContentPack(pack, this.translation, this.monitor);

                    managedPack.Initialize();
                    managed.Add(managedPack);
                    this.SaveUsedReplaceAplicators(managedPack.Contents, managedPack.Pack.Manifest);
                    
                    this.monitor.Log($"Loaded content pack `{pack.Manifest.Name}`");
                } catch (ContentPackException e)
                {
                    this.monitor.Log($"Unable to load content pack `{pack.Manifest.Name}`:\n   {e.Message}", LogLevel.Error);
                }
            }

            this.CheckMultipleUsedReplaceAplicators();
            this.monitor.Log($"Loaded {managed.Count} content packs", LogLevel.Info);

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
