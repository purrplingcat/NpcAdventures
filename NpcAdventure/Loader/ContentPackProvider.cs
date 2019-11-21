using NpcAdventure.Model;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcAdventure.Loader
{
    class ContentPackProvider
    {
        private readonly string modName;
        private readonly IMonitor monitor;
        private readonly List<AssetPatch> patches;

        public ContentPackProvider(string modName, IContentPackHelper helper, IMonitor monitor)
        {
            this.modName = modName;
            this.monitor = monitor;
            this.patches = this.LoadPatches(helper);
            
        }
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (!asset.AssetName.StartsWith(this.modName))
                return false; // Do not check assets not owned by this mod

            bool check = this.patches.Where((p) => p.Action.Equals("Edit") && asset.AssetNameEquals($"{this.modName}/{p.Target}")).Any();

            this.monitor.Log($"Check: [{(check ? "x" : " ")}] asset {asset.AssetName} can be edited by any content pack");

            return check;
        }

        public bool CanLoad<T>(IAssetInfo asset)
        {
            if (!asset.AssetName.StartsWith(this.modName))
                return false; // Do not check assets not owned by this mod

            var toCheck = this.patches.Where((p) => p.Action.Equals("Load") && asset.AssetNameEquals($"{this.modName}/{p.Target}"));

            if (toCheck.Count() > 1)
            {
                this.monitor.Log($"Multiple patches want to load {asset.AssetName} ({string.Join(", ", from entry in toCheck select entry.LogName)}). None will be applied.", LogLevel.Error);
                return false;
            }

            bool check = toCheck.Any();

            this.monitor.Log($"Check: [{(check ? "x" : " ")}] asset {asset.AssetName} can be replaced/loaded by any content pack");

            return check;
        }

        public void Edit<T>(IAssetData asset)
        {
            var toApply = this.patches.Where((p) => p.Action.Equals("Edit") && asset.AssetNameEquals($"{this.modName}/{p.Target}"));
            var target = asset.AsDictionary<string, string>().Data;

            foreach (var patch in toApply)
            {
                try
                {
                    var data = patch.LoadData<Dictionary<string, string>>();

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
            var toApply = this.patches.Where((p) => p.Action.Equals("Load") && asset.AssetNameEquals($"{this.modName}/{p.Target}")).First();

            return toApply.LoadData<T>();
        }

        public List<AssetPatch> LoadPatches(IContentPackHelper helper)
        {
            var packs = helper.GetOwned();
            var patches = new List<AssetPatch>();

            foreach (var pack in packs)
            {
                int entryNo = 0;
                var metadata = pack.ReadJsonFile<ContentPackData>("content.json");
                
                foreach (var patch in metadata.Changes)
                {
                    patches.Add(new AssetPatch(patch, pack, $"entry #{entryNo} ({patch.Action} {patch.Target}) in {pack.Manifest.Name}"));
                    entryNo++;
                }

                this.monitor.Log($"Loaded content pack {pack.Manifest.Name} v{pack.Manifest.Version} ({pack.Manifest.UniqueID})", LogLevel.Info);
            }

            this.monitor.Log($"SUMMARY: {patches.Count} patches in {packs.Count()} content packs", LogLevel.Info);
            return patches;
        }
    }
}
