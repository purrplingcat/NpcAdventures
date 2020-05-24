using NpcAdventure.Loader.ContentPacks;
using NpcAdventure.Model;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NpcAdventure.Loader
{
    class ContentPackManager
    {
        private readonly ITranslationHelper translation;
        private readonly IMonitor monitor;
        private readonly List<ManagedContentPack> packs;

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
            this.packs = this.LoadPacks(helper);
            
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
                    managed.Add(new ManagedContentPack(pack, this.translation, this.monitor));
                    this.monitor.Log($"Loaded content pack `{pack.Manifest.Name}`");
                } catch (ContentPackException e)
                {
                    this.monitor.Log($"Unable to load content pack `{pack.Manifest.Name}`:\n   {e.Message}", LogLevel.Error);
                }
            }

            this.monitor.Log($"Loaded {managed.Count} content packs", LogLevel.Info);

            return managed;
        }

        public bool Apply<TKey, TValue>(Dictionary<TKey, TValue> target, string path)
        {
            bool applied = false;

            foreach (var pack in this.packs)
            {
                var toApply = pack.Load<TKey, TValue>(path);

                if (toApply != null)
                {
                    AssetPatchHelper.ApplyPatch(target, toApply);
                    applied = true;
                }
            }

            return applied;
        }
    }
}
