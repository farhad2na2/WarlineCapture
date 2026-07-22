using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class DenseCityBuildingMaterialLibrary
    {
        private const string MaterialAPath =
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat";
        private const string MaterialBPath =
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_B.mat";
        private const string MaterialCPath =
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_C.mat";
        private const string GeneratedFolder =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_building_materials";

        private static readonly string[] ShopMaterialPaths =
        {
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_03_A.mat",
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_03_B.mat",
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_03_C.mat"
        };

        private static readonly Color[] FacadeTints =
        {
            new(1f, 0.97f, 0.91f, 1f),
            new(0.94f, 0.96f, 0.98f, 1f),
            new(0.95f, 0.98f, 0.93f, 1f),
            new(0.98f, 0.94f, 0.90f, 1f),
            new(0.97f, 0.94f, 0.96f, 1f),
            new(0.92f, 0.95f, 0.95f, 1f)
        };

        private static readonly Color[] ShopTones =
        {
            new(0.86f, 0.82f, 0.70f, 1f),
            new(0.63f, 0.72f, 0.76f, 1f),
            new(0.68f, 0.74f, 0.64f, 1f),
            new(0.72f, 0.68f, 0.61f, 1f),
            new(0.48f, 0.53f, 0.54f, 1f)
        };

        private readonly Material[] _facadeSources;
        private readonly Material[] _shopSources;
        private readonly Material[,] _facadeVariants;
        private readonly Material[] _shopVariants;

        private DenseCityBuildingMaterialLibrary(
            Material[] facadeSources,
            Material[] shopSources,
            Material[,] facadeVariants,
            Material[] shopVariants)
        {
            _facadeSources = facadeSources;
            _shopSources = shopSources;
            _facadeVariants = facadeVariants;
            _shopVariants = shopVariants;
        }

        internal static int FacadeTintCount => FacadeTints.Length;
        internal static int ShopToneCount => ShopTones.Length;

        internal static DenseCityBuildingMaterialLibrary CreateOrUpdate()
        {
            Material[] facadeSources = LoadFacadeSources();
            Material[] shopSources = LoadShopSources();
            EnsureAssetFolder(GeneratedFolder);
            Material[,] facadeVariants = CreateOrUpdateFacadeVariants(facadeSources);
            Material[] shopVariants = CreateOrUpdateShopVariants(shopSources[0], facadeSources.Length);
            AssetDatabase.SaveAssets();
            return new DenseCityBuildingMaterialLibrary(
                facadeSources,
                shopSources,
                facadeVariants,
                shopVariants);
        }

        internal static DenseCityBuildingMaterialLibrary LoadExisting()
        {
            Material[] facadeSources = LoadFacadeSources();
            Material[] shopSources = LoadShopSources();
            var facadeVariants = new Material[facadeSources.Length, FacadeTints.Length];
            for (int sourceIndex = 0; sourceIndex < facadeSources.Length; sourceIndex++)
            {
                for (int tintIndex = 0; tintIndex < FacadeTints.Length; tintIndex++)
                {
                    facadeVariants[sourceIndex, tintIndex] = LoadRequired<Material>(
                        FacadeMaterialPath(sourceIndex, tintIndex));
                }
            }

            var shopVariants = new Material[ShopTones.Length];
            for (int toneIndex = 0; toneIndex < ShopTones.Length; toneIndex++)
                shopVariants[toneIndex] = LoadRequired<Material>(ShopMaterialPath(toneIndex));

            return new DenseCityBuildingMaterialLibrary(
                facadeSources,
                shopSources,
                facadeVariants,
                shopVariants);
        }

        internal bool IsFacadeFamily(Material material) =>
            TryGetFacadeSourceIndex(material, out _);

        internal bool IsShopFamily(Material material)
        {
            if (IsOriginalShopMaterial(material))
                return true;

            for (int index = 0; index < _shopVariants.Length; index++)
            {
                if (material == _shopVariants[index])
                    return true;
            }

            return false;
        }

        internal bool IsOriginalShopMaterial(Material material)
        {
            for (int index = 0; index < _shopSources.Length; index++)
            {
                if (material == _shopSources[index])
                    return true;
            }

            return false;
        }

        internal Material Resolve(
            Material currentMaterial,
            DenseCityBuildingMaterialSelection selection)
        {
            if (!selection.ApplyVariant)
                return currentMaterial;

            if (IsShopFamily(currentMaterial))
            {
                if (selection.UseOriginalShopMaterial)
                    return _shopSources[0];
                if ((uint)selection.ShopToneIndex >= (uint)_shopVariants.Length)
                    throw new ArgumentOutOfRangeException(nameof(selection));
                return _shopVariants[selection.ShopToneIndex];
            }

            if (!TryGetFacadeSourceIndex(currentMaterial, out int sourceIndex))
                return currentMaterial;
            int facadeTintIndex = selection.FacadeTintIndex >= 0
                ? selection.FacadeTintIndex
                : selection.PaletteIndex;
            if ((uint)facadeTintIndex >= (uint)FacadeTints.Length)
                throw new ArgumentOutOfRangeException(nameof(selection));
            return _facadeVariants[sourceIndex, facadeTintIndex];
        }

        private bool TryGetFacadeSourceIndex(Material material, out int sourceIndex)
        {
            for (int index = 0; index < _facadeSources.Length; index++)
            {
                if (material == _facadeSources[index])
                {
                    sourceIndex = index;
                    return true;
                }
            }

            for (int index = 0; index < _facadeSources.Length; index++)
            {
                for (int tintIndex = 0; tintIndex < FacadeTints.Length; tintIndex++)
                {
                    if (material != _facadeVariants[index, tintIndex])
                        continue;
                    sourceIndex = index;
                    return true;
                }
            }

            sourceIndex = -1;
            return false;
        }

        private static Material[] LoadFacadeSources() =>
            new[]
            {
                LoadRequired<Material>(MaterialAPath),
                LoadRequired<Material>(MaterialBPath),
                LoadRequired<Material>(MaterialCPath)
            };

        private static Material[] LoadShopSources()
        {
            var materials = new Material[ShopMaterialPaths.Length];
            for (int index = 0; index < ShopMaterialPaths.Length; index++)
                materials[index] = LoadRequired<Material>(ShopMaterialPaths[index]);
            return materials;
        }

        private static Material[,] CreateOrUpdateFacadeVariants(Material[] facadeSources)
        {
            var materials = new Material[facadeSources.Length, FacadeTints.Length];
            for (int sourceIndex = 0; sourceIndex < facadeSources.Length; sourceIndex++)
            {
                for (int tintIndex = 0; tintIndex < FacadeTints.Length; tintIndex++)
                {
                    string path = FacadeMaterialPath(sourceIndex, tintIndex);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(facadeSources[sourceIndex]);
                        AssetDatabase.CreateAsset(material, path);
                    }

                    material.name = Path.GetFileNameWithoutExtension(path);
                    material.CopyPropertiesFromMaterial(facadeSources[sourceIndex]);
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", FacadeTints[tintIndex]);
                    if (material.HasProperty("_Color"))
                        material.SetColor("_Color", FacadeTints[tintIndex]);
                    material.enableInstancing = true;
                    EditorUtility.SetDirty(material);
                    materials[sourceIndex, tintIndex] = material;
                }
            }

            return materials;
        }

        private static Material[] CreateOrUpdateShopVariants(
            Material sourceMaterial,
            int facadeSourceCount)
        {
            Texture2D sourceTexture = sourceMaterial.GetTexture("_BaseMap") as Texture2D;
            if (sourceTexture == null || !sourceTexture.isReadable)
            {
                throw new InvalidOperationException(
                    $"Shop_05 source material '{sourceMaterial.name}' requires a readable _BaseMap texture.");
            }

            for (int sourceIndex = 1; sourceIndex < facadeSourceCount; sourceIndex++)
            {
                for (int toneIndex = 0; toneIndex < ShopTones.Length; toneIndex++)
                {
                    AssetDatabase.DeleteAsset(
                        $"{GeneratedFolder}/DenseCity_Shop05_{(char)('A' + sourceIndex)}_{toneIndex + 1:00}.mat");
                }
            }

            var materials = new Material[ShopTones.Length];
            Color32[] sourcePixels = sourceTexture.GetPixels32();
            for (int toneIndex = 0; toneIndex < ShopTones.Length; toneIndex++)
            {
                string texturePath = ShopTexturePath(toneIndex);
                var texture = new Texture2D(
                    sourceTexture.width,
                    sourceTexture.height,
                    TextureFormat.RGBA32,
                    false,
                    false);
                var recoloredPixels = new Color32[sourcePixels.Length];
                Color.RGBToHSV(ShopTones[toneIndex], out float targetHue, out float targetSaturation, out _);
                float brightnessScale = Mathf.Lerp(0.72f, 1f, ShopTones[toneIndex].maxColorComponent);
                for (int pixelIndex = 0; pixelIndex < sourcePixels.Length; pixelIndex++)
                {
                    Color sourceColor = sourcePixels[pixelIndex];
                    Color.RGBToHSV(sourceColor, out float hue, out float saturation, out float value);
                    bool isWarmFacadePixel =
                        saturation > 0.04f &&
                        (hue < 0.18f || hue > 0.78f) &&
                        sourceColor.r > sourceColor.g * 1.01f;
                    if (!isWarmFacadePixel)
                    {
                        recoloredPixels[pixelIndex] = sourcePixels[pixelIndex];
                        continue;
                    }

                    float recoloredSaturation = Mathf.Clamp(targetSaturation, 0.08f, 0.22f);
                    Color recolored = Color.HSVToRGB(
                        targetHue,
                        recoloredSaturation,
                        Mathf.Clamp01(value * brightnessScale));
                    recolored.a = sourceColor.a;
                    recoloredPixels[pixelIndex] = recolored;
                }

                texture.SetPixels32(recoloredPixels);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.GetFullPath(texturePath), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                ConfigureShopTexture(texturePath);

                Texture2D recoloredTexture = LoadRequired<Texture2D>(texturePath);
                string materialPath = ShopMaterialPath(toneIndex);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(sourceMaterial);
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                material.name = Path.GetFileNameWithoutExtension(materialPath);
                material.CopyPropertiesFromMaterial(sourceMaterial);
                material.SetTexture("_BaseMap", recoloredTexture);
                material.SetTexture("_MainTex", recoloredTexture);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", Color.white);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                materials[toneIndex] = material;
            }

            return materials;
        }

        private static void ConfigureShopTexture(string texturePath)
        {
            if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
                throw new InvalidOperationException($"Missing texture importer for {texturePath}.");

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException($"Missing dense-city material asset {path}.");

        private static string FacadeMaterialPath(int sourceIndex, int tintIndex) =>
            $"{GeneratedFolder}/DenseCity_Facade_{(char)('A' + sourceIndex)}_{tintIndex + 1:00}.mat";

        private static string ShopMaterialPath(int toneIndex) =>
            $"{GeneratedFolder}/DenseCity_Shop05_A_{toneIndex + 1:00}.mat";

        private static string ShopTexturePath(int toneIndex) =>
            $"{GeneratedFolder}/DenseCity_Shop05_Texture_{toneIndex + 1:00}.png";

        private static void EnsureAssetFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }
}
