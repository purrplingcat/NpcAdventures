using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NpcAdventure.Loader
{
    internal static class AssetPatchHelper
    {
        internal static void ApplyPatch<TModel>(TModel target, TModel source)
        {
            if (typeof(TModel).IsGenericType && typeof(TModel).GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                MethodInfo method = typeof(AssetPatchHelper).GetMethod(nameof(ApplyDictionary), BindingFlags.Static | BindingFlags.NonPublic);

                if (method == null)
                    throw new InvalidOperationException($"Can't fetch the internal {nameof(ApplyDictionary)} method.");

                MakeKeyValuePatcher<TModel>(method).Invoke(null, new object[] { target, source });
                return;
            }

            throw new AssetPatchException(typeof(TModel));
        }

        private static void ApplyDictionary<TKey, TValue>(IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            source.ToList().ForEach(s => target[s.Key] = s.Value);
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
