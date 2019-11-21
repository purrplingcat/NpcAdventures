using NpcAdventure.Model;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.Loader
{
    class ContentPackManager : IAssetLoader, IAssetEditor
    {
        private readonly string modName;
        private readonly IContentPackHelper helper;
        private readonly IMonitor monitor;
        private readonly List<AssetPatch> patches;

        public ContentPackManager(string modName, IContentPackHelper helper, IMonitor monitor)
        {
            this.modName = modName;
            this.helper = helper;
            this.monitor = monitor;
            this.patches = this.LoadPatches(helper);
            
        }
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (!asset.AssetName.StartsWith(this.modName))
                return false; // Do not check assets not owned by this mod

            bool check = this.patches.Where((p) => p.Action.Equals("Edit") && asset.AssetNameEquals($"{this.modName}/{p.Target}")).Any();

            this.monitor.VerboseLog($"Check: [{(check ? "x" : " ")}] asset {asset.AssetName} can be edited");

            return check;
        }

        public bool CanLoad<T>(IAssetInfo asset)
        {
            throw new NotImplementedException();
        }

        public void Edit<T>(IAssetData asset)
        {
            var toApply = this.patches.Where((p) => p.Action.Equals("Edit") && asset.AssetNameEquals($"{this.modName}/{p.Target}"));
            var target = asset.AsDictionary<string, string>().Data;

            foreach (var patch in toApply)
            {
                try
                {
                    var data = patch.LoadData();

                    foreach (var pair in data)
                    {
                        target[pair.Key] = pair.Value;
                    }

                    this.monitor.Log($"Applied patch for target {patch.Target} asset {asset.AssetName}");
                } catch (Exception e)
                {
                    this.monitor.Log($"Cannot apply patch for target {patch.Target}: {e.Message}", LogLevel.Error);
                }
            }
        }

        public T Load<T>(IAssetInfo asset)
        {
            throw new NotImplementedException();
        }

        public List<AssetPatch> LoadPatches(IContentPackHelper helper)
        {
            var packs = helper.GetOwned();
            var patches = new List<AssetPatch>();

            foreach (var pack in packs)
            {
                var metadata = pack.ReadJsonFile<ContentPackData>("content.json");
                
                foreach (var patch in metadata.Changes)
                {
                    patches.Add(new AssetPatch(patch, pack));
                }

                this.monitor.Log($"Loaded content pack {pack.Manifest.Name} v{pack.Manifest.Version} ({pack.Manifest.UniqueID})", LogLevel.Info);
            }

            this.monitor.Log($"SUMMARY: {patches.Count} patches in {packs.Count()} content packs", LogLevel.Info);
            return patches;
        }
    }
}
