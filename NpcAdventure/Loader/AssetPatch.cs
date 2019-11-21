using NpcAdventure.Model;
using StardewModdingAPI;
using System.Collections.Generic;

namespace NpcAdventure.Loader
{
    internal class AssetPatch
    {
        private readonly ContentPackData.DataChanges changes;
        private readonly IContentPack contentPack;

        public AssetPatch(ContentPackData.DataChanges changes, IContentPack contentPack)
        {
            this.changes = changes;
            this.contentPack = contentPack;
        }

        public string Action { get => this.changes.Action; }
        public string Target { get => this.changes.Target; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> LoadData()
        {
            return this.contentPack.LoadAsset<Dictionary<string, string>>(this.changes.FromFile);
        }
    }
}