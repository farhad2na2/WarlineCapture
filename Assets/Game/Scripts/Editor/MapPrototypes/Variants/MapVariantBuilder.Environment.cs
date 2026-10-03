using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.MapVariants
{
    internal sealed partial class MapVariantBuilder
    {
        public const string GeneratedRoot = "Assets/Game/Art/MapPrototypes/Variants";
        // Inventory uses transient geometry and may only read pre-existing shared art.
        // Set/reset by the preparation transaction in a finally block.
        internal static bool InventoryOnly { get; set; }
        internal const string GroundMaterialPath = "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat";
        private const string GroundUvSourcePrefab = "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Square_01.prefab";
        private const string VolumeProfilePath = "Assets/PolygonMilitary/Scenes/Demo/Military_Demo.asset";
        private const int GroundChunkCells = 128;

        public string GeneratedFolder => $"{GeneratedRoot}/{MapId}";

        public int BuildGround()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath) ??
                           throw new InvalidOperationException($"[MapVariants] Missing ground material {GroundMaterialPath}.");
            Vector2 uv = ResolveGroundUv();
            string folder = $"{GeneratedFolder}/Ground";
            // A coarser grid produces fewer chunks, so meshes from an earlier build are removed first.
            if (!InventoryOnly && AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
            if (!InventoryOnly)
                EnsureFolder(folder);

            int chunks = 0;
            for (int z = 0; z < Height.CellsZ; z += GroundChunkCells)
            for (int x = 0; x < Height.CellsX; x += GroundChunkCells)
            {
                string name = $"Ground_{x / GroundChunkCells}_{z / GroundChunkCells}";
                Mesh mesh = Height.BuildChunkMesh(x, z, GroundChunkCells, GroundChunkCells, uv, name);
                string path = $"{folder}/{name}.asset";
                if (!InventoryOnly)
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(mesh, path);
                }
                var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(Layer(MapVariantLayer.Ground), false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                chunks++;
            }

            return chunks;
        }

        public GameObject BuildWater(Rect area, float level, string materialPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath) ??
                           throw new InvalidOperationException($"[MapVariants] Missing water material {materialPath}.");
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(Layer(MapVariantLayer.Ground), false);
            water.transform.position = new Vector3(area.center.x, level, area.center.y);
            water.transform.localScale = new Vector3(area.width / 10f, 1f, area.height / 10f);
            var renderer = water.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return water;
        }

        // Matches the lighting of Assets/Game/Scenes/Demo.unity so the variants read like the reference scene.
        public void BuildLighting()
        {
            var lighting = new GameObject("Lighting").transform;
            lighting.SetParent(Root, false);

            var sun = new GameObject("Sun", typeof(Light));
            sun.transform.SetParent(lighting, false);
            sun.transform.rotation = new Quaternion(-0.048195288f, 0.74310285f, -0.6193119f, 0.24885356f);
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98261887f, 0.8308824f);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;

            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientSkyColor = new Color(0.5169811f, 0.46643582f, 0.42431468f);
            RenderSettings.ambientEquatorColor = new Color(0.38185117f, 0.5526082f, 0.6679245f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.85660374f, 0.79479903f, 0.6965966f);
            RenderSettings.fogDensity = 0.002f;

            // Game.Editor does not reference the SRP core runtime assembly, so the volume is authored by type name.
            var profile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(VolumeProfilePath);
            Type volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (profile != null && volumeType != null)
            {
                var volumeObject = new GameObject("Global Volume");
                volumeObject.transform.SetParent(lighting, false);
                var serialized = new SerializedObject(volumeObject.AddComponent(volumeType));
                SerializedProperty isGlobal = serialized.FindProperty("m_IsGlobal") ?? serialized.FindProperty("isGlobal");
                SerializedProperty sharedProfile = serialized.FindProperty("sharedProfile") ??
                    throw new InvalidOperationException("[MapVariants] Volume.sharedProfile is not serialized.");
                if (isGlobal != null)
                    isGlobal.boolValue = true;
                sharedProfile.objectReferenceValue = profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Synty ships one container colour; hue-rotated copies of its atlas give the yard a mixed-colour stack.
        public static string HueShiftedMaterial(string sourceMaterialPath, string textureProperty, string suffix, float hueShift)
        {
            string folder = $"{GeneratedRoot}/Shared";
            string materialPath = $"{folder}/{Path.GetFileNameWithoutExtension(sourceMaterialPath)}_{suffix}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
                return materialPath;
            if (InventoryOnly)
                throw new InvalidOperationException($"[MapPreparation] Missing shared reference material: {materialPath}. Inventory cannot generate protected prototype art.");

            var source = AssetDatabase.LoadAssetAtPath<Material>(sourceMaterialPath) ??
                         throw new InvalidOperationException($"[MapVariants] Missing material {sourceMaterialPath}.");
            var atlas = source.GetTexture(textureProperty) as Texture2D ??
                        throw new InvalidOperationException($"[MapVariants] {sourceMaterialPath} has no {textureProperty}.");
            var target = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(atlas, target);
            RenderTexture.active = target;
            var pixels = new Texture2D(atlas.width, atlas.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            Color32[] colors = pixels.GetPixels32();
            for (int i = 0; i < colors.Length; i++)
            {
                Color.RGBToHSV(colors[i], out float h, out float s, out float v);
                colors[i] = Color.HSVToRGB(Mathf.Repeat(h + hueShift, 1f), s, v);
            }

            pixels.SetPixels32(colors);
            EnsureFolder(folder);
            string texturePath = $"{folder}/{atlas.name}_{suffix}.png";
            File.WriteAllBytes(Path.Combine(ProjectRoot, texturePath), pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(texturePath);

            var material = new Material(source);
            material.SetTexture(textureProperty, AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            AssetDatabase.CreateAsset(material, materialPath);
            return materialPath;
        }

        public static void EnsureFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        internal static Vector2 ResolveGroundUv()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundUvSourcePrefab);
            MeshFilter filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
                throw new InvalidOperationException($"[MapVariants] Missing ground UV source mesh in {GroundUvSourcePrefab}.");
            Vector2[] uvs = mesh.uv;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < uvs.Length; i++)
            {
                if (normals.Length == uvs.Length && normals[i].y > 0.95f)
                    return uvs[i];
            }

            return uvs.Length > 0 ? uvs[0] : Vector2.zero;
        }
    }
}
