using NpcAdventure.Loader.ContentPacks.Data;
using System;
using System.Collections.Generic;

namespace NpcAdventure.Loader.ContentPacks
{
    internal class ManagedPatch
    {
        public LegacyChanges Change { get; }
        public ManagedContentPack Owner { get; }
        public bool Disabled { get => this.Change.Disabled; }

        public ManagedPatch(LegacyChanges change, ManagedContentPack managedContentPack)
        {
            this.Change = change ?? throw new System.ArgumentNullException(nameof(change));
            this.Owner = managedContentPack ?? throw new System.ArgumentNullException(nameof(managedContentPack));
        }

        public Dictionary<TKey, TValue> LoadData<TKey, TValue>()
        {
            return this.Owner.Pack.LoadAsset<Dictionary<TKey, TValue>>(this.Change.FromFile);
        }
    }
}