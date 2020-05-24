using System.Collections.Generic;
using NpcAdventure.Loader.ContentPacks.Data;
using NpcAdventure.Loader.ContentPacks.Provider;
using NpcAdventure.Utils;
using StardewModdingAPI;

namespace NpcAdventure.Loader.ContentPacks
{
    /// <summary>Handles loading assets from content packs.</summary>
    internal class ManagedContentPack
    {
        private static string[] SUPPORTED_FORMATS = { "1.2", "1.3", "2.0" };

        /// <summary>The managed content pack.</summary>
        public IContentPack Pack { get; }
        public ITranslationHelper Translation { get; }
        public IMonitor Monitor { get; }

        private readonly LegacyDataProvider legacyDataProvider;
        private readonly DataProvider dataProvider;

        public Contents Contents { get; private set; }

        /// <summary>Construct an instance.</summary>
        /// <param name="pack">The content pack to manage.</param>
        public ManagedContentPack(IContentPack pack, ITranslationHelper translation, IMonitor monitor)
        {
            this.Pack = pack;
            this.Translation = translation;
            this.Monitor = monitor;

            this.legacyDataProvider = new LegacyDataProvider(this);
            this.dataProvider = new DataProvider(this);

            this.Initialize();
        }

        private void Initialize()
        {
            if (!this.Pack.HasFile("content.json"))
                throw new ContentPackException("Declaration file `content.json` not found!");

            this.Contents = this.Pack.ReadJsonFile<Contents>("content.json");

            var version = new SemanticVersion(this.Contents.Format);

            if (!this.CheckFormatVersion(version))
                throw new ContentPackException($"Unsupported format `{this.Contents.Format}`");

            if (version.IsOlderThan("2.0"))
            {
                this.TagLegacyPatches();
            }
        }

        private void TagLegacyPatches()
        {
            int num = 0;
            foreach (var change in this.Contents.Changes)
            {
                if (change.LogName == null)
                    change.LogName = $"Patch #{num}";
                ++num;
            }
        }

        private bool CheckFormatVersion(SemanticVersion semanticVersion)
        {
            foreach (var compareTo in SUPPORTED_FORMATS)
            {
                if (semanticVersion.EqualsMajorMinor(new SemanticVersion(compareTo)))
                    return true;
            }

            return false;
        }

        public Dictionary<TKey, TValue> Load<TKey, TValue>(string path)
        {
            ISemanticVersion version = new SemanticVersion(this.Contents.Format);

            if (version.IsOlderThan("2.0"))
            {
                return this.legacyDataProvider.Provide<TKey, TValue>(path);
            }

            return this.dataProvider.Provide<TKey, TValue>(path);
        }
    }
}