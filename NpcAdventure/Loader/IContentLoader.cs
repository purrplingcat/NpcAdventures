using System;
using System.Collections.Generic;

namespace NpcAdventure.Loader
{
    public interface IContentLoader
    {
        Dictionary<TKey, TValue> Load<TKey, TValue>(string assetName);
        Dictionary<string, string> LoadStrings(string stringsAssetName);
        string LoadString(string path);
        string LoadString(string path, params object[] substitutions);
        void InvalidateCache();
    }
}