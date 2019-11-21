using NpcAdventure.Loader.ContentPacks;
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

        /// <summary>
        /// Provides patches from content packs into mod's content
        /// </summary>
        /// <param name="modName"></param>
        /// <param name="helper"></param>
        /// <param name="monitor"></param>
        public ContentPackProvider(string modName, IContentPackHelper helper, IMonitor monitor)
        {
            this.modName = modName;
            this.monitor = monitor;
            this.patches = this.LoadPatches(helper);
            
        }

        /// <summary>
        /// Checks mod's asset can be patched by patches from content packs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (!asset.AssetName.StartsWith(this.modName))
                return false; // Do not check assets not owned by this mod

            bool check = this.GetPatchesForAsset(asset, "Edit").Any();

            this.monitor.VerboseLog($"Check: [{(check ? "x" : " ")}] asset {asset.AssetName} can be edited by any content pack");

            return check;
        }

        /// <summary>
        /// Checks mod's asset can be covered with a patch from content pack or checks new file can be loaded from content pack.
        /// If multiple content packs defines cover for the same asset, this patch can't be loaded
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool CanLoad<T>(IAssetInfo asset)
        {
            if (!asset.AssetName.StartsWith(this.modName))
                return false; // Do not check assets not owned by this mod

            var toCheck = this.GetPatchesForAsset(asset, "Load");
            

            if (toCheck.Count > 1)
            {
                this.monitor.Log($"Multiple patches want to load {asset.AssetName} ({string.Join(", ", from entry in toCheck select entry.LogName)}). None will be applied.", LogLevel.Error);
                return false;
            }

            
            if (toCheck.Count == 1 && !toCheck.First().FromAssetExists() )
            {
                this.monitor.Log($"Can't load cover for {asset.AssetName} ({toCheck.First().LogName}), because patch assets exists!", LogLevel.Error);
                return false;
            }

            bool check = toCheck.Any();

            this.monitor.VerboseLog($"Check: [{(check ? "x" : " ")}] asset {asset.AssetName} can be replaced/loaded by any content pack");

            return check;
        }

        /// <summary>
        /// Patch a mod's asset with patches from content packs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        public void Edit<T>(IAssetData asset)
        {
            var toApply = this.GetPatchesForAsset(asset, "Edit");
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

                    this.monitor.Log($"Applied patch '{patch.LogName}' to asset {asset.AssetName}");
                } catch (Exception e)
                {
                    this.monitor.Log($"Cannot apply patch '{patch.LogName}': {e.Message}", LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// Cover existing mod's asset or load new asset into game from content pack patch
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset"></param>
        /// <returns></returns>
        public T Load<T>(IAssetInfo asset)
        {
            var toApply = this.GetPatchesForAsset(asset, "Load").First();

            return toApply.LoadData<T>();
        }

        /// <summary>
        /// Filter patches only for this asset and action
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private List<AssetPatch> GetPatchesForAsset(IAssetInfo asset, string action)
        {
            return this.patches.Where((p) => p.Action.Equals(action) && asset.AssetNameEquals($"{this.modName}/{p.Target}")).ToList();
        }

        /// <summary>
        /// Parse content pack definitions and loads possible patches
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public List<AssetPatch> LoadPatches(IContentPackHelper helper)
        {
            var packs = helper.GetOwned();
            var patches = new List<AssetPatch>();

            foreach (var pack in packs)
            {
                try
                {
                    int entryNo = 0;
                    var metadata = pack.ReadJsonFile<ContentPackData>("content.json");
                    var managedPack = new ManagedContentPack(pack);

                    foreach (var patch in metadata.Changes)
                    {
                        patches.Add(new AssetPatch(patch, managedPack, $"{pack.Manifest.Name} -> entry #{entryNo} ({patch.Action} {patch.Target})"));
                        entryNo++;
                    }

                    this.monitor.Log($"Loaded content pack {pack.Manifest.Name} v{pack.Manifest.Version} ({pack.Manifest.UniqueID})", LogLevel.Info);
                } catch (Exception e)
                {
                    this.monitor.Log($"An error occured during parse content pack {pack.Manifest.Name}: ${e.Message}", LogLevel.Error);
                }
            }

            this.monitor.Log($"SUMMARY: {patches.Count} patches in {packs.Count()} content packs", LogLevel.Info);
            return patches;
        }
    }
}
