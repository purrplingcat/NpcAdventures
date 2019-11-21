using Microsoft.Xna.Framework.Content;
using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace NpcAdventure.Loader
{
    /// <summary>
    /// Mod's assets loader and editor.
    /// </summary>
    internal class AssetsManager : IAssetLoader, IAssetEditor
    {
        private readonly string modName;
        private readonly string modAssetDir;
        private readonly IMonitor monitor;

        public AssetsManager(string modName, string modAssetDir, IContentHelper helper, IMonitor monitor)
        {
            if (string.IsNullOrEmpty(modAssetDir))
            {
                throw new ArgumentException("Mod assets directory must be set!", nameof(modAssetDir));
            }

            this.modName = modName;
            this.modAssetDir = modAssetDir;
            this.Helper = helper ?? throw new ArgumentNullException(nameof(helper));
            this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public IContentHelper Helper { get; }

        /// <summary>
        /// Can we load localisation for mod's asset?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            // Can we load localisation only for localised assets
            if (asset.AssetName.StartsWith(this.modName) && !string.IsNullOrEmpty(asset.Locale))
                return true;

            return false;
        }

        /// <summary>
        /// Is this asset mod's asset and we can load it?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool CanLoad<T>(IAssetInfo asset)
        {
            if (!asset.AssetName.StartsWith(this.modName))
                return false;

            return true;
        }

        /// <summary>
        /// Patch Mod's asset contents with localised content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        public void Edit<T>(IAssetData asset)
        {
            string assetName = asset.AssetName.Replace(this.modName, this.modAssetDir);
            string locale = asset.Locale;
            string fileName = $"{assetName}.{locale}.json"; // Localised filename like Dialogue/Abigail.de-De.json for German localisation

            try
            {
                this.monitor.VerboseLog($"Trying to load localised file {fileName} for {assetName}, locale {locale}");

                var strings = asset.AsDictionary<string, string>().Data;
                var localised = this.Helper.Load<Dictionary<string, string>>(fileName, ContentSource.ModFolder);

                foreach(var pair in localised)
                {
                    strings[pair.Key] = pair.Value;
                }
            }
            catch (ContentLoadException)
            {
                this.monitor.Log($"Loading of localised file {fileName}, fallback to non-localised file.", LogLevel.Alert);
            }
        }

        /// <summary>
        /// Load mod's asset contents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public T Load<T>(IAssetInfo asset)
        {
            string assetName = asset.AssetName.Replace(this.modName, this.modAssetDir);
            string filenName = $"{assetName}.json"; // Asset file name like `Dialogue/Abigail.json`

            return this.Helper.Load<T>(filenName, ContentSource.ModFolder);
        }
    }
}
