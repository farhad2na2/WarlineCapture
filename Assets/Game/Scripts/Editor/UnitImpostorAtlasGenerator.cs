using System.Collections.Generic;
using System.IO;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Configs;
using Game.Rendering;

namespace Game.Editor
{
    public static class UnitImpostorAtlasGenerator
    {
        private const string SoldierPrefabPath = "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";
        private const string RegistryConfigPath = "Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";
        private const string OutputFolder = "Assets/Game/Textures/Generated/Impostors";
        private const int DirectionCount = 8;
        private const int Columns = 4;
        private const int Rows = 2;
        private const int TileSize = 512;
        private const int TilePadding = 24;
        private const int InnerTileSize = TileSize - TilePadding * 2;
        private const int AndroidMaxTextureSize = 1024;
        private const int AndroidCompressionQuality = 50;
        private static readonly int SnivelerModelShownId = Shader.PropertyToID("_SnivelerModelShown");
        private static readonly int SnivelerRenderPixelId = Shader.PropertyToID("_SnivelerRenderPixel");

        public static void GenerateSoldierMale02Alt04Atlas()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierPrefabPath);
            UnitPrefabRegistryAuthoringConfig registry = AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(RegistryConfigPath);
            if (prefab == null)
            {
                Debug.LogError($"[ImpostorAtlasGen] Missing prefab path={SoldierPrefabPath}");
                return;
            }
            if (registry == null)
            {
                Debug.LogError($"[ImpostorAtlasGen] Missing registry path={RegistryConfigPath}");
                return;
            }

            EnsureOutputFolder();
            GenerateAtlasForPrefab(registry, prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Game/Rendering/Atlases/Generate All Character Atlases")]
        public static void GenerateAllRegisteredSoldierAtlases()
        {
            UnitPrefabRegistryAuthoringConfig registry = AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(RegistryConfigPath);
            if (registry == null)
            {
                Debug.LogError($"[ImpostorAtlasGen] Missing registry path={RegistryConfigPath}");
                return;
            }

            EnsureOutputFolder();
            HashSet<GameObject> generatedPrefabs = new();
            int generated = 0;
            List<GameObject> prefabs = registry.UnitSpawnPrefabs;
            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject prefab = prefabs[i];
                if (!IsSoldierPrefab(prefab) || !generatedPrefabs.Add(prefab))
                    continue;

                if (GenerateAtlasForPrefab(registry, prefab))
                    generated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ImpostorAtlasGen] generatedAll soldiers={generated} registry={RegistryConfigPath}");
        }

        [MenuItem("Game/Rendering/Atlases/Apply Mobile Import Policy")]
        public static void ApplyMobileImportPolicyToExistingAtlases()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { OutputFolder });
            var paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));

            paths.Sort(System.StringComparer.Ordinal);
            int configured = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                if (ConfigureTextureImporter(paths[i]))
                    configured++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ImpostorAtlasGen] mobileImportPolicy configured={configured} discovered={paths.Count} folder={OutputFolder}");
        }

        private static bool GenerateAtlasForPrefab(UnitPrefabRegistryAuthoringConfig registry, GameObject prefab)
        {
            if (registry == null || prefab == null)
                return false;

            EnsureOutputFolder();
            GameObject bakePrefab = ResolveBakePrefab(prefab);
            SharedPrefabPreviewCache.ReleaseAll();
            string atlasPath = BuildAtlasPath(prefab);
            Texture2D atlas = new(Columns * TileSize, Rows * TileSize, TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(atlasPath),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] clearPixels = new Color32[atlas.width * atlas.height];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = new Color32(0, 0, 0, 0);
            atlas.SetPixels32(clearPixels);

            for (int direction = 0; direction < DirectionCount; direction++)
            {
                Texture2D tile = RenderDirection(bakePrefab, direction, DirectionCount);
                if (tile == null)
                {
                    Debug.LogError($"[ImpostorAtlasGen] Failed direction={direction} prefab={prefab.name} bakePrefab={bakePrefab.name}");
                    Object.DestroyImmediate(atlas);
                    return false;
                }

                if (!TryAnalyzeAlphaBounds(tile, out RectInt alphaBounds) || alphaBounds.height < InnerTileSize / 4 || alphaBounds.width > alphaBounds.height * 3)
                {
                    Debug.LogWarning($"[ImpostorAtlasGen] suspiciousTile unit={prefab.name} bakePrefab={bakePrefab.name} direction={direction} alphaBounds={alphaBounds} innerTile={InnerTileSize}");
                }

                int column = direction % Columns;
                int rowFromTop = direction / Columns;
                int y = (Rows - 1 - rowFromTop) * TileSize;
                WritePaddedTile(atlas, tile, column * TileSize, y);
                Object.DestroyImmediate(tile);
            }

            atlas.Apply(true, false);
            File.WriteAllBytes(ToFullPath(atlasPath), atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(atlasPath);
            ConfigureTextureImporter(atlasPath);
            Texture2D importedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            if (importedAtlas == null)
            {
                Debug.LogError($"[ImpostorAtlasGen] Failed to import atlas path={atlasPath}");
                return false;
            }

            Vector2 size = ResolveImpostorSize(bakePrefab);
            AssignAtlasToRegistry(registry, prefab, importedAtlas, size);
            Debug.Log($"[ImpostorAtlasGen] generated unit={prefab.name} bakePrefab={bakePrefab.name} atlas={atlasPath} directions={DirectionCount} columns={Columns} rows={Rows} tile={TileSize} innerTile={InnerTileSize} padding={TilePadding} size={size}");
            return true;
        }

        private static GameObject ResolveBakePrefab(GameObject runtimePrefab)
        {
            if (runtimePrefab == null)
                return null;

            // The generated MidLOD prefab uses GPU-animation source geometry that can render as a flat atlas strip in editor cameras.
            // Bake from the original runtime prefab, then register the atlas against the same runtime prefab key.
            return runtimePrefab;
        }

        private static Texture2D RenderDirection(GameObject prefab, int directionIndex, int directionCount)
        {
            if (!SharedPrefabPreviewCache.TryGetOrCreateDirectionalImpostor(prefab, directionIndex, directionCount, out RenderTexture sourceTexture) ||
                sourceTexture == null)
            {
                return null;
            }

            RenderTexture scaledTexture = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                scaledTexture = new RenderTexture(InnerTileSize, InnerTileSize, 0, RenderTextureFormat.ARGB32)
                {
                    name = $"ImpostorAtlasTile_{prefab.name}_{directionIndex:00}",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                Graphics.Blit(sourceTexture, scaledTexture);
                RenderTexture.active = scaledTexture;

                Texture2D texture = new(InnerTileSize, InnerTileSize, TextureFormat.RGBA32, false)
                {
                    name = $"ImpostorAtlasTile_{prefab.name}_{directionIndex:00}",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.ReadPixels(new Rect(0, 0, InnerTileSize, InnerTileSize), 0, 0);
                BleedTransparentPixels(texture, 8);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
                if (scaledTexture != null)
                {
                    scaledTexture.Release();
                    Object.DestroyImmediate(scaledTexture);
                }
            }
        }

        private static void WritePaddedTile(Texture2D atlas, Texture2D tile, int tileX, int tileY)
        {
            Color32[] source = tile.GetPixels32();
            Color32[] padded = new Color32[TileSize * TileSize];
            for (int y = 0; y < TileSize; y++)
            {
                int sourceY = Mathf.Clamp(y - TilePadding, 0, InnerTileSize - 1);
                for (int x = 0; x < TileSize; x++)
                {
                    int sourceX = Mathf.Clamp(x - TilePadding, 0, InnerTileSize - 1);
                    padded[y * TileSize + x] = source[sourceY * InnerTileSize + sourceX];
                }
            }

            atlas.SetPixels32(tileX, tileY, TileSize, TileSize, padded);
        }

        private static bool TryAnalyzeAlphaBounds(Texture2D texture, out RectInt bounds)
        {
            bounds = default;
            if (texture == null)
                return false;

            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a < 16)
                        continue;

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
                return false;

            bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return true;
        }

        private static void ApplyGpuAnimatedIdlePose(GameObject visual)
        {
            if (visual == null)
                return;

            MaterialAnimatorIndexAuthoring indexAuthoring = visual.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
            if (indexAuthoring == null || indexAuthoring.animator == null)
                return;

            MaterialAnimatorAuthoring animatorAuthoring = indexAuthoring.animator.GetComponent<MaterialAnimatorAuthoring>();
            if (animatorAuthoring == null || animatorAuthoring.animations == null || animatorAuthoring.animations.Count < 2)
                return;

            int animationIndex = Mathf.Clamp(indexAuthoring.animationIndex, 0, animatorAuthoring.animations.Count - 1);
            MaterialAnimatorBake idleAnimation = animatorAuthoring.animations[animationIndex];
            int frameCount = Mathf.Max(1, idleAnimation.frames);
            int boneCount = Mathf.Max(1, animatorAuthoring.bonesCount);
            int chosenFrame = Mathf.Clamp(Mathf.FloorToInt(frameCount * 0.35f), 0, frameCount - 1);
            int startPixel = idleAnimation.start + chosenFrame * boneCount;
            int endPixel = startPixel;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            MaterialPropertyBlock propertyBlock = new();
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || renderer.sharedMaterials == null)
                    continue;

                for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; materialIndex++)
                {
                    renderer.GetPropertyBlock(propertyBlock, materialIndex);
                    propertyBlock.SetFloat(SnivelerModelShownId, 1f);
                    propertyBlock.SetVector(SnivelerRenderPixelId, new Vector4(startPixel, endPixel, 0f, 0f));
                    renderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }
        }

        private static bool TryCalculateRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            LODGroup lodGroup = root.GetComponentInChildren<LODGroup>(true);
            if (lodGroup != null)
            {
                Transform lodTransform = lodGroup.transform;
                Vector3 center = lodTransform.TransformPoint(lodGroup.localReferencePoint);
                float size = Mathf.Max(0.1f, lodGroup.size);
                Vector3 scale = lodTransform.lossyScale;
                Vector3 worldSize = new Vector3(
                    size * Mathf.Max(0.01f, Mathf.Abs(scale.x)),
                    size * Mathf.Max(0.01f, Mathf.Abs(scale.y)),
                    size * Mathf.Max(0.01f, Mathf.Abs(scale.z)));
                bounds = new Bounds(center, worldSize);
                return true;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds.min);
                    bounds.Encapsulate(renderer.bounds.max);
                }
            }

            return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
        }

        private static void BleedTransparentPixels(Texture2D texture, int iterations)
        {
            if (texture == null || iterations <= 0)
                return;

            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            Color32[] next = new Color32[pixels.Length];
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                System.Array.Copy(pixels, next, pixels.Length);
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int index = y * width + x;
                        if (pixels[index].a != 0)
                            continue;

                        int r = 0;
                        int g = 0;
                        int b = 0;
                        int count = 0;
                        for (int yy = -1; yy <= 1; yy++)
                        {
                            for (int xx = -1; xx <= 1; xx++)
                            {
                                if (xx == 0 && yy == 0)
                                    continue;

                                Color32 neighbor = pixels[(y + yy) * width + x + xx];
                                if (neighbor.a == 0)
                                    continue;

                                r += neighbor.r;
                                g += neighbor.g;
                                b += neighbor.b;
                                count++;
                            }
                        }

                        if (count == 0)
                            continue;

                        next[index] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
                    }
                }

                (pixels, next) = (next, pixels);
            }

            texture.SetPixels32(pixels);
        }

        private static Vector2 ResolveImpostorSize(GameObject prefab)
        {
            LODGroup lodGroup = prefab != null ? prefab.GetComponentInChildren<LODGroup>(true) : null;
            if (lodGroup != null)
            {
                float size = Mathf.Max(1f, lodGroup.size);
                return new Vector2(Mathf.Max(1.25f, size * 0.72f), Mathf.Max(2.1f, size * 1.05f));
            }

            Bounds bounds = default;
            bool hasBounds = false;
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds.min);
                    bounds.Encapsulate(renderer.bounds.max);
                }
            }

            if (!hasBounds)
                return new Vector2(1.7f, 2.8f);

            float width = Mathf.Max(1.4f, Mathf.Max(bounds.size.x, bounds.size.z) * 1.8f);
            float height = Mathf.Max(2.3f, bounds.size.y * 1.45f);
            return new Vector2(width, height);
        }

        private static void AssignAtlasToRegistry(UnitPrefabRegistryAuthoringConfig registry, GameObject prefab, Texture2D atlas, Vector2 size)
        {
            SerializedObject serializedRegistry = new(registry);
            SerializedProperty entries = serializedRegistry.FindProperty("impostorAtlases");
            if (entries == null)
            {
                Debug.LogError("[ImpostorAtlasGen] Registry is missing impostorAtlases field.");
                return;
            }

            SerializedProperty entry = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty candidate = entries.GetArrayElementAtIndex(i);
                SerializedProperty candidatePrefab = candidate.FindPropertyRelative("prefab");
                if (candidatePrefab != null && candidatePrefab.objectReferenceValue == prefab)
                {
                    entry = candidate;
                    break;
                }
            }

            if (entry == null)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }

            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("atlas").objectReferenceValue = atlas;
            entry.FindPropertyRelative("directionCount").intValue = DirectionCount;
            entry.FindPropertyRelative("columns").intValue = Columns;
            entry.FindPropertyRelative("rows").intValue = Rows;
            entry.FindPropertyRelative("size").vector2Value = size;
            SerializedProperty groundAnchor = entry.FindPropertyRelative("groundAnchorNormalized");
            if (groundAnchor != null)
                groundAnchor.floatValue = 0f;
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        private static bool ConfigureTextureImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return false;

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
            androidSettings.name = "Android";
            androidSettings.overridden = true;
            androidSettings.maxTextureSize = AndroidMaxTextureSize;
            androidSettings.format = TextureImporterFormat.ASTC_6x6;
            androidSettings.compressionQuality = AndroidCompressionQuality;
            importer.SetPlatformTextureSettings(androidSettings);
            importer.SaveAndReimport();
            return true;
        }

        private static void EnsureOutputFolder()
        {
            string fullPath = ToFullPath(OutputFolder);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }

        private static bool IsSoldierPrefab(GameObject prefab)
        {
            return prefab != null && prefab.name.StartsWith("Unit_Chr_", System.StringComparison.Ordinal);
        }

        private static string BuildAtlasPath(GameObject prefab)
        {
            string safeName = prefab.name.Replace(' ', '_');
            return $"{OutputFolder}/ImpostorAtlas_{safeName}.png";
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        }
    }
}
