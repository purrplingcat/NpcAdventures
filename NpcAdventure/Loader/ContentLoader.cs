﻿using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NpcAdventure.Loader
{
    /// <summary>
    /// Content loader for NPC Adventure mod's contents
    /// All asset files must be in JSON format
    /// </summary>
    public class ContentLoader : IContentLoader
    {
        private readonly IMonitor monitor;
        private readonly Dictionary<string, object> assetCache;
        private readonly IContentPackHelper contentPacks;
        private readonly ContentPackManager contentPackManager;

        public IDataHelper Data { get; }
        private IContentHelper Assets { get; }
        public ITranslationHelper Translation { get; }
        public string DirectoryPath { get; }

        /// <summary>
        /// Creatre new instance of custom service ContentLoader
        /// </summary>
        /// <param name="helper"></param>>
        /// <param name="monitor"></param>
        public ContentLoader(IModHelper helper, IMonitor monitor)
        {
            this.Data = helper.Data;
            this.Assets = helper.Content;
            this.Translation = helper.Translation;
            this.DirectoryPath = helper.DirectoryPath;
            this.assetCache = new Dictionary<string, object>();
            this.contentPacks = helper.ContentPacks;
            this.contentPackManager = new ContentPackManager(helper.ContentPacks, helper.Translation, monitor);
            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        /// <summary>
        /// Load an mod content asset
        /// </summary>
        /// <typeparam name="TModel">Type of asset to be loaded</typeparam>
        /// <param name="path">Name of asset, like `Strings/Strings` or `Dialogue/Abigail` and etc</param>
        /// <returns>Loaded content of asset</returns>
        public Dictionary<TKey, TValue> Load<TKey, TValue>(string path)
        {
            // Try to get asset from our map cache
            if (this.assetCache.TryGetValue(path, out object asset))
                return (Dictionary<TKey, TValue>)asset;

            string locale = this.Assets.CurrentLocale.ToLower();
            Dictionary<TKey, TValue> baseData;

            // If this content doesn't exists in mod scope, try load them from content packs
            if (!this.HasFile($"assets/{path}.json"))
            {
                return this.FallbackLoad<TKey, TValue>(path);
            }

            baseData = this.Assets.Load<Dictionary<TKey, TValue>>($"assets/{path}.json");

            this.ApplyTranslation(path, locale, baseData); // Apply translations                
            this.contentPackManager.Apply(baseData, path); // Apply content packs
            this.assetCache.Add(path, baseData); // Save to cache

            return baseData;
        }

        private Dictionary<TKey, TValue> FallbackLoad<TKey, TValue>(string path)
        {
            var baseData = new Dictionary<TKey, TValue>();
              
            if (!this.contentPackManager.Apply(baseData, path))
                this.monitor.Log($"Cannot load asset `{path}`", LogLevel.Error);

            return baseData;
        }

        private bool HasFile(string path)
        {
            return File.Exists(Path.Combine(this.DirectoryPath, path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private void ApplyTranslation<TKey, TValue>(string path, string locale, Dictionary<TKey, TValue> baseData)
        {
            if (string.IsNullOrEmpty(locale))
                return;

            this.monitor.VerboseLog($"Trying to load localised file `locale/{locale}/{path}.json` for `{path}`, locale `{locale}`");
            var translatedData = this.Data.ReadJsonFile<Dictionary<TKey, TValue>>($"locale/{locale}/{path}.json");

            if (translatedData != null)
                AssetPatchHelper.ApplyPatch(baseData, translatedData);
            else
                this.monitor.Log($"No translations for {path} locale {locale}");
        }

        /// <summary>
        /// Loads a string dictionary asset
        /// </summary>
        /// <param name="path">Name of asset, like `Strings/Strings` or `Dialogue/Abigail` and etc</param>
        /// <returns>Loaded dictionary of strings</returns>
        public Dictionary<string, string> LoadStrings(string path)
        {
            return this.Load<string, string>(path);
        }

        /// <summary>
        /// Load one string from strings dictionary asset
        /// </summary>
        /// <param name="path">Path to string in asset with whole asset name (like `Strings/Strings:companionRecruited.yes`</param>
        /// <returns>A loaded string from asset dictionary</returns>
        public string LoadString(string path)
        {
            string[] parsedPath = path.Split(':');

            if (parsedPath.Length != 2)
                throw new ArgumentException($"Unable to parse string path: {path}");

            if (this.LoadStrings(parsedPath[0]).TryGetValue(parsedPath[1], out string str))
                return str;

            return path;
        }

        /// <summary>
        /// Load one string from strings dictionary asset with substituions.
        /// Placeholders `{%number%}` in string wil be replaced with substitution.
        /// </summary>
        /// <param name="path">Path to string in asset with whole asset name (like `Strings/Strings:companionRecruited.yes`)</param>
        /// <param name="substitutions">A substitution for replace placeholder in string</param>
        /// <returns>A loaded string from asset dictionary</returns>
        public string LoadString(string path, params object[] substitutions)
        {
            string str = this.LoadString(path);

            return string.Format(str, substitutions);
        }

        /// <summary>
        /// Invalidate mod's cached assets
        /// </summary>
        public void InvalidateCache()
        {
            this.assetCache.Clear();
        }

        public string GetAssetKey(string assetName)
        {
            string lead = assetName.Split('/')[0];
            if (lead == "~")
            {
                // Get asset key inside the mod
                return this.Assets.GetActualAssetKey($"assets/{assetName.Substring(2)}");
            }

            if (lead.StartsWith("@"))
            {
                var pack = this.contentPacks.GetOwned().FirstOrDefault(cp => cp.Manifest.UniqueID.Equals(lead.Substring(1)));

                if (pack != null)
                {
                    // Get asset key inside specified mod's content pack
                    return pack.GetActualAssetKey(assetName.Remove(0, lead.Length + 1));
                }

                this.monitor.Log($"Unknown content pack uid `{lead.Substring(1)}`");
            }

            // Get asset key in game folder
            return this.Assets.GetActualAssetKey(assetName, ContentSource.GameContent);
        }
    }
}
