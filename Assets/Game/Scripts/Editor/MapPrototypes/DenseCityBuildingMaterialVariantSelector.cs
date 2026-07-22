using System;
using Game.Configs;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityBuildingMaterialSelection
    {
        internal DenseCityBuildingMaterialSelection(
            bool applyVariant,
            bool useOriginalShopMaterial,
            int facadeTintIndex,
            int shopToneIndex,
            int paletteIndex)
        {
            ApplyVariant = applyVariant;
            UseOriginalShopMaterial = useOriginalShopMaterial;
            FacadeTintIndex = facadeTintIndex;
            ShopToneIndex = shopToneIndex;
            PaletteIndex = paletteIndex;
        }

        internal bool ApplyVariant { get; }
        internal bool UseOriginalShopMaterial { get; }
        internal int FacadeTintIndex { get; }
        internal int ShopToneIndex { get; }
        internal int PaletteIndex { get; }
    }

    internal static class DenseCityBuildingMaterialVariantSelector
    {
        private const int FacadeTintCount = 6;
        private const int ShopToneCount = 5;
        private const int MaterialSalt = 0x4c39;

        internal static DenseCityBuildingMaterialSelection Select(
            Vector3 worldPosition,
            uint seed,
            GeneratedCityBuildingRole role,
            bool usesBuildingMaterialFamily,
            bool usesShop05MaterialFamily)
        {
            if (role == GeneratedCityBuildingRole.None)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (!float.IsFinite(worldPosition.x) || !float.IsFinite(worldPosition.z))
                throw new ArgumentOutOfRangeException(nameof(worldPosition));
            if (role == GeneratedCityBuildingRole.Civic || !usesBuildingMaterialFamily)
                return new DenseCityBuildingMaterialSelection(false, false, -1, -1, -1);

            uint hash = Hash(
                Mathf.RoundToInt(worldPosition.x * 10f),
                Mathf.RoundToInt(worldPosition.z * 10f),
                unchecked((int)seed) ^ MaterialSalt);
            if (!usesShop05MaterialFamily)
            {
                int tintIndex = (int)(hash % FacadeTintCount);
                return new DenseCityBuildingMaterialSelection(true, false, tintIndex, -1, tintIndex);
            }

            bool useOriginal = hash % 5u == 0u;
            int shopToneIndex = useOriginal ? -1 : (int)((hash / 5u) % ShopToneCount);
            int paletteIndex = useOriginal ? 0 : shopToneIndex + 1;
            return new DenseCityBuildingMaterialSelection(true, useOriginal, -1, shopToneIndex, paletteIndex);
        }

        private static uint Hash(int x, int z, int salt)
        {
            uint hash = unchecked((uint)(x * 73856093) ^ (uint)(z * 19349663) ^ (uint)salt);
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            return hash ^ (hash >> 16);
        }
    }
}
