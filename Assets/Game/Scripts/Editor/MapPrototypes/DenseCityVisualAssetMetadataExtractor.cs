using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityVisualAssetMetadata
    {
        private readonly string[] materialAssetGuids;

        internal DenseCityVisualAssetMetadata(
            string prefabAssetGuid,
            long prefabLocalId,
            string[] materialAssetGuids)
        {
            PrefabAssetGuid = prefabAssetGuid;
            PrefabLocalId = prefabLocalId;
            this.materialAssetGuids = materialAssetGuids;
        }

        internal string PrefabAssetGuid { get; }
        internal long PrefabLocalId { get; }
        internal IReadOnlyList<string> MaterialAssetGuids => materialAssetGuids;
    }

    internal static class DenseCityVisualAssetMetadataExtractor
    {
        internal static DenseCityVisualAssetMetadata Extract(GameObject prefab)
        {
            return Extract(prefab, null);
        }

        internal static DenseCityVisualAssetMetadata Extract(
            GameObject prefab,
            Func<Material, Material> materialResolver)
        {
            return Extract(prefab, materialResolver, null);
        }

        internal static DenseCityVisualAssetMetadata Extract(
            GameObject prefab,
            Func<Material, Material> materialResolver,
            Func<Renderer, bool> rendererFilter)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string prefabGuid, out long prefabLocalId) ||
                string.IsNullOrEmpty(prefabGuid) || prefabLocalId <= 0)
            {
                throw new InvalidOperationException(
                    $"Dense-city visual must be a persistent asset: '{prefab.name}'.");
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var materialIdentities = new SortedDictionary<string, long>(StringComparer.Ordinal);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                if (rendererFilter != null && !rendererFilter(renderers[rendererIndex]))
                    continue;
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                        continue;
                    if (materialResolver != null)
                    {
                        material = materialResolver(material) ??
                                   throw new InvalidOperationException(
                                       "Dense-city material resolver returned null.");
                    }
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string materialGuid, out long localId) ||
                        string.IsNullOrEmpty(materialGuid) || localId <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Dense-city material must be a persistent asset: '{material.name}'.");
                    }
                    if (materialIdentities.TryGetValue(materialGuid, out long existingLocalId))
                    {
                        if (existingLocalId != localId)
                        {
                            throw new InvalidOperationException(
                                $"Dense-city material subassets sharing GUID '{materialGuid}' require explicit local-id records.");
                        }
                    }
                    else
                    {
                        materialIdentities.Add(materialGuid, localId);
                    }
                }
            }
            if (materialIdentities.Count == 0)
                throw new InvalidOperationException($"Dense-city visual has no persistent materials: '{prefab.name}'.");

            var materialArray = new string[materialIdentities.Count];
            materialIdentities.Keys.CopyTo(materialArray, 0);
            return new DenseCityVisualAssetMetadata(prefabGuid, prefabLocalId, materialArray);
        }
    }
}
