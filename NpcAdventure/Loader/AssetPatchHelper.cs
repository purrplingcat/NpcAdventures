using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NpcAdventure.Loader
{
    internal static class AssetPatchHelper
    {
        internal static IEnumerable<object> ApplyPatch<TKey, TValue>(IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source, bool allowOverrides)
        {
            var conflicts = new List<object>();

            foreach (KeyValuePair<TKey, TValue> field in source)
            {
                if (target.ContainsKey(field.Key))
                {
                    conflicts.Add(field.Key);

                    if (!allowOverrides)
                        continue;
                }

                target[field.Key] = field.Value;
            }

            return conflicts.Distinct();
        }

        internal static Dictionary<TKey, TValue> ToDictionary<TKey, TValue, SKey, SValue>(Dictionary<SKey, SValue> dict)
        {
            return dict.ToDictionary(p => (TKey)(object)p.Key, p => (TValue)(object)p.Value);
        }

        internal static MethodInfo MakeKeyValuePatcher<T>(MethodInfo patchMethod)
        {
            // get dictionary's key/value types
            Type[] genericArgs = typeof(T).GetGenericArguments();
            if (genericArgs.Length != 2)
                throw new InvalidOperationException("Can't parse the asset's dictionary key/value types.");
            Type keyType = typeof(T).GetGenericArguments().FirstOrDefault();
            Type valueType = typeof(T).GetGenericArguments().LastOrDefault();
            if (keyType == null)
                throw new InvalidOperationException("Can't parse the asset's dictionary key type.");
            if (valueType == null)
                throw new InvalidOperationException("Can't parse the asset's dictionary value type.");

            if (!patchMethod.IsGenericMethodDefinition || patchMethod.GetGenericArguments().Length != 2)
            {
                throw new InvalidOperationException($"Patch method {patchMethod.Name} is not generic method definition or don't match generic pattern <TKey, TValue>");
            }

            return patchMethod.MakeGenericMethod(keyType, valueType);
        }
    }
}
