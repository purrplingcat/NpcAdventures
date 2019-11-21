using NpcAdventure.Model;
using StardewModdingAPI;
using System.Collections.Generic;

namespace NpcAdventure.Loader
{
    internal class AssetPatch
    {
        private readonly ContentPackData.DataChanges changes;
        private readonly IContentPack contentPack;

        public AssetPatch(ContentPackData.DataChanges changes, IContentPack contentPack, string logName)
        {
            this.changes = changes;
            this.contentPack = contentPack;
            this.LogName = logName;
        }

        public string Action { get => this.changes.Action; }
        public string Target { get => this.changes.Target; }

        public string LogName { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public T LoadData<T>()
        {
            return this.contentPack.LoadAsset<T>(this.changes.FromFile);
        }
    }
}