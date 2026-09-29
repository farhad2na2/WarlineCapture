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
        private const string GroundMaterialPath = "Assets/Game/Rendering/Materials/PolygonMilitary_GroundVariation_Real.mat";
        private const string GroundUvSourcePrefab = "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Square_01.prefab";
        private const string SkyboxPath = "Assets/Game/Art/MapPrototypes/M01/M01_DesertSkybox.mat";
        private const string VolumeProfilePath = "Assets/Game/Art/MapPrototypes/M01/M01_VisualVolumeProfile.asset";
        private const int GroundChunkCells = 128;

        public string GeneratedFolder => $"{GeneratedRoot}/{MapId}";

        public int BuildGround()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath) ??
                           throw new InvalidOperationException($"[MapVariants] Missing ground material {GroundMaterialPath}.");
            Vector2 uv = ResolveGroundUv();
            string folder = $"{GeneratedFolder}/Ground";
            // A coarser grid produces fewer chunks, so meshes from an earlier build are removed first.
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.DeleteAsset(folder);
            EnsureFolder(folder);

            int chunks = 0;
            for (int z = 0; z < Height.CellsZ; z += GroundChunkCells)
            for (int x = 0; x < Height.CellsX; x += GroundChunkCells)
            {
                string name = $"Ground_{x / GroundChunkCells}_{z / GroundChunkCells}";
                Mesh mesh = Height.BuildChunkMesh(x, z, GroundChunkCells, GroundChunkCells, uv, name);
                string path = $"{folder}/{name}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
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

        public void BuildLighting(Vector3 sunEuler, Color sunColor, float sunIntensity, Color fogColor, float fogStart, float fogEnd)
        {
            var lighting = new GameObject("Lighting").transform;
            lighting.SetParent(Root, false);

            var sun = new GameObject("Sun", typeof(Light));
            sun.transform.SetParent(lighting, false);
            sun.transform.rotation = Quaternion.Euler(sunEuler);
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = sunColor;
            light.intensity = sunIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.82f;
            RenderSettings.sun = light;

            RenderSettings.skybox = SunsetSky();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.80f, 0.64f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.82f, 0.58f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.29f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

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

        // Comic palette: lavender zenith, rose mid-sky, gold horizon. A latitude gradient keeps it exact.
        private static readonly (float latitude, Color color)[] SunsetGradient =
        {
            (-1.00f, new Color(0.52f, 0.38f, 0.30f)),
            (-0.02f, new Color(0.86f, 0.62f, 0.44f)),
            (0.00f, new Color(1.00f, 0.76f, 0.47f)),
            (0.06f, new Color(0.99f, 0.66f, 0.45f)),
            (0.20f, new Color(0.90f, 0.56f, 0.52f)),
            (0.45f, new Color(0.66f, 0.50f, 0.62f)),
            (1.00f, new Color(0.38f, 0.40f, 0.62f))
        };

        private static Material SunsetSky()
        {
            string folder = $"{GeneratedRoot}/Shared";
            string texturePath = $"{folder}/MapVariant_SunsetGradient.png";
            string path = $"{folder}/MapVariant_SunsetSky.mat";
            EnsureFolder(folder);

            const int width = 8;
            const int height = 512;
            var gradient = new Texture2D(width, height, TextureFormat.RGB24, false);
            for (int y = 0; y < height; y++)
            {
                float latitude = y / (float)(height - 1) * 2f - 1f;
                Color color = SunsetGradient[^1].color;
                for (int i = 0; i + 1 < SunsetGradient.Length; i++)
                {
                    if (latitude > SunsetGradient[i + 1].latitude)
                        continue;
                    float t = Mathf.InverseLerp(SunsetGradient[i].latitude, SunsetGradient[i + 1].latitude, latitude);
                    color = Color.Lerp(SunsetGradient[i].color, SunsetGradient[i + 1].color, Mathf.SmoothStep(0f, 1f, t));
                    break;
                }

                for (int x = 0; x < width; x++)
                    gradient.SetPixel(x, y, color);
            }

            File.WriteAllBytes(Path.Combine(ProjectRoot, texturePath), gradient.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(gradient);
            AssetDatabase.ImportAsset(texturePath);
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(sky, path);
            }

            sky.shader = Shader.Find("Skybox/Panoramic");
            sky.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            // Panoramic's KeywordEnum maps _Mapping 1 to the latitude-longitude layout.
            sky.shaderKeywords = Array.Empty<string>();
            sky.SetFloat("_Exposure", 1f);
            sky.SetFloat("_Mapping", 1f);
            sky.SetFloat("_ImageType", 0f);
            sky.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            EditorUtility.SetDirty(sky);
            return sky;
        }

        // Synty ships one container colour; hue-rotated copies of its atlas give the yard a mixed-colour stack.
        public static string HueShiftedMaterial(string sourceMaterialPath, string textureProperty, string suffix, float hueShift)
        {
            string folder = $"{GeneratedRoot}/Shared";
            string materialPath = $"{folder}/{Path.GetFileNameWithoutExtension(sourceMaterialPath)}_{suffix}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
                return materialPath;

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

        private static Vector2 ResolveGroundUv()
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
