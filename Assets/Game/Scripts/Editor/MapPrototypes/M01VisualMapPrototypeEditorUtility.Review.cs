namespace Game.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public static partial class M01VisualMapPrototypeEditorUtility
    {
        private readonly struct CaptureDefinition
        {
            public CaptureDefinition(string name, string fileName, Vector3 position, Vector3 target, float fieldOfView)
            {
                Name = name;
                FileName = fileName;
                Position = position;
                Target = target;
                FieldOfView = fieldOfView;
                Orthographic = false;
                OrthographicSize = 0f;
            }

            public CaptureDefinition(string name, string fileName, Vector3 position, Vector3 target, float orthographicSize, bool orthographic)
            {
                Name = name;
                FileName = fileName;
                Position = position;
                Target = target;
                FieldOfView = 38f;
                Orthographic = orthographic;
                OrthographicSize = orthographicSize;
            }

            public string Name { get; }
            public string FileName { get; }
            public Vector3 Position { get; }
            public Vector3 Target { get; }
            public float FieldOfView { get; }
            public bool Orthographic { get; }
            public float OrthographicSize { get; }
        }

        private static readonly CaptureDefinition[] CaptureDefinitions =
        {
            new("GameplayOverview", "m01_gameplay_overview.png", new Vector3(-105f, 58f, -72f), new Vector3(-28f, 1f, -5f), 50f),
            new("OldMarketApproach", "m01_old_market_approach.png", new Vector3(-103f, 31f, -58f), new Vector3(-45f, 6f, 2f), 40f),
            new("BombingAftermath", "m01_bombing_aftermath.png", new Vector3(36f, 11f, -43f), new Vector3(13f, 2f, -19f), 31f),
            new("TopDownPlan", "m01_top_down_plan.png", new Vector3(0f, 260f, -4f), Vector3.zero, 116f, true)
        };

        private static void CreateLighting(Transform parent)
        {
            GameObject rig = PlacePrefab(PremiumLightingRigPath, "M01_PremiumLightingRig", Vector3.zero, 0f, 1f, parent);
            ConfigurePrototypeVolume(rig);
            Light[] lights = rig.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type != LightType.Directional)
                    continue;

                lights[i].transform.rotation = Quaternion.Euler(52f, -35f, 0f);
                lights[i].intensity = 1.8f;
                RenderSettings.sun = lights[i];
                break;
            }

            GameObject fillObject = new("M01_SoftSkyFill");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.rotation = Quaternion.Euler(58f, 145f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.60f, 0.70f, 0.82f);
            fill.intensity = 0.48f;
            fill.shadows = LightShadows.None;
        }

        private static void ConfigurePrototypeVolume(GameObject rig)
        {
            if (AssetDatabase.LoadMainAssetAtPath(PrototypeVolumeProfilePath) == null)
            {
                if (!AssetDatabase.CopyAsset(PremiumVolumeProfilePath, PrototypeVolumeProfilePath))
                    throw new InvalidOperationException($"Could not create M01 volume profile from {PremiumVolumeProfilePath}");
                AssetDatabase.ImportAsset(PrototypeVolumeProfilePath, ImportAssetOptions.ForceSynchronousImport);
            }

            Object[] profileAssets = AssetDatabase.LoadAllAssetsAtPath(PrototypeVolumeProfilePath);
            Object profile = AssetDatabase.LoadMainAssetAtPath(PrototypeVolumeProfilePath);
            for (int i = 0; i < profileAssets.Length; i++)
            {
                Object component = profileAssets[i];
                var serialized = new SerializedObject(component);
                if (string.Equals(component.name, "ColorAdjustments", StringComparison.Ordinal))
                {
                    SetFloat(serialized, "postExposure.m_Value", 0.32f);
                    SetFloat(serialized, "contrast.m_Value", 12f);
                    SetFloat(serialized, "saturation.m_Value", 6f);
                    SetColor(serialized, "colorFilter.m_Value", new Color(1f, 0.975f, 0.93f, 1f));
                }
                else if (string.Equals(component.name, "ShadowsMidtonesHighlights", StringComparison.Ordinal))
                {
                    SerializedProperty shadows = serialized.FindProperty("shadows.m_Value");
                    if (shadows != null)
                    {
                        Vector4 value = shadows.vector4Value;
                        value.w = -0.06f;
                        shadows.vector4Value = value;
                    }
                }
                else if (string.Equals(component.name, "WhiteBalance", StringComparison.Ordinal))
                {
                    SetFloat(serialized, "temperature.m_Value", 7f);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(component);
            }

            Component[] components = rig.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (!string.Equals(components[i].GetType().Name, "Volume", StringComparison.Ordinal))
                    continue;
                var serialized = new SerializedObject(components[i]);
                SerializedProperty sharedProfile = serialized.FindProperty("sharedProfile");
                if (sharedProfile != null)
                {
                    sharedProfile.objectReferenceValue = profile;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                break;
            }
        }

        private static void SetFloat(SerializedObject serialized, string path, float value)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property != null)
                property.floatValue = value;
        }

        private static void SetColor(SerializedObject serialized, string path, Color value)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property != null)
                property.colorValue = value;
        }

        private static void CreateReviewCameras(Transform parent)
        {
            for (int i = 0; i < CaptureDefinitions.Length; i++)
            {
                CaptureDefinition definition = CaptureDefinitions[i];
                GameObject cameraObject = new($"M01_Review_{definition.Name}");
                cameraObject.transform.SetParent(parent, false);
                cameraObject.transform.position = definition.Position;
                Vector3 cameraUp = definition.Orthographic ? Vector3.forward : Vector3.up;
                cameraObject.transform.rotation = Quaternion.LookRotation((definition.Target - definition.Position).normalized, cameraUp);
                if (i == 0)
                    cameraObject.tag = "MainCamera";

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.fieldOfView = definition.FieldOfView;
                camera.orthographic = definition.Orthographic;
                camera.orthographicSize = definition.OrthographicSize;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 1200f;
                camera.allowHDR = true;
                camera.allowMSAA = true;
                EnableUrpPostProcessing(cameraObject);
            }
        }

        private static void EnableUrpPostProcessing(GameObject cameraObject)
        {
            Type dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (dataType == null)
                return;

            Component data = cameraObject.GetComponent(dataType) ?? cameraObject.AddComponent(dataType);
            PropertyInfo renderPostProcessing = dataType.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public);
            renderPostProcessing?.SetValue(data, true);
        }

        private static string CaptureReviewSet()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            SimulateParticles();
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", CaptureDirectory));
            Directory.CreateDirectory(outputDirectory);
            var capturePaths = new List<string>(CaptureDefinitions.Length);

            for (int i = 0; i < CaptureDefinitions.Length; i++)
            {
                CaptureDefinition definition = CaptureDefinitions[i];
                GameObject cameraObject = GameObject.Find($"M01_Review_{definition.Name}");
                Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
                if (camera == null)
                    throw new InvalidOperationException($"Missing M01 review camera: {definition.Name}");

                string path = Path.Combine(outputDirectory, definition.FileName);
                CaptureCamera(camera, path);
                capturePaths.Add(path);
            }

            string contactSheetPath = Path.Combine(outputDirectory, "m01_visual_prototype_contact_sheet_v13.png");
            CreateContactSheet(contactSheetPath, capturePaths);
            WriteCaptureManifest(outputDirectory, capturePaths, contactSheetPath);
            AssetDatabase.Refresh();
            return contactSheetPath;
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                name = "M01VisualPrototypeCapture"
            };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            ParticleSystemRenderer[] particleRenderers =
                Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Include);
            var particleRendererStates = new bool[particleRenderers.Length];
            Texture2D texture = null;
            try
            {
                for (int i = 0; i < particleRenderers.Length; i++)
                {
                    ParticleSystemRenderer particleRenderer = particleRenderers[i];
                    particleRendererStates[i] = particleRenderer.enabled;
                    particleRenderer.enabled = false;
                }

                camera.targetTexture = target;
                RenderTexture.active = target;
                // Prime URP state so the first review camera renders like subsequent cameras.
                for (int i = 0; i < 3; i++)
                    camera.Render();
                texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                if (!HasVisiblePixels(texture, out float averageLuminance))
                    throw new InvalidOperationException($"M01 visual capture failed luminance validation: average={averageLuminance:0.0} path={path}");
                Debug.Log($"[M01VisualMap] capture={Path.GetFileName(path)} averageLuma={averageLuminance:0.0}");
            }
            finally
            {
                for (int i = 0; i < particleRenderers.Length; i++)
                {
                    if (particleRenderers[i] != null)
                        particleRenderers[i].enabled = particleRendererStates[i];
                }

                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (texture != null)
                    Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        private static bool HasVisiblePixels(Texture2D texture, out float averageLuminance)
        {
            Color32[] pixels = texture.GetPixels32();
            if (pixels.Length == 0)
            {
                averageLuminance = 0f;
                return false;
            }

            long total = 0;
            int samples = 0;
            byte min = byte.MaxValue;
            byte max = byte.MinValue;
            int step = Mathf.Max(1, pixels.Length / 4096);
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                byte luminance = (byte)((pixel.r * 54 + pixel.g * 183 + pixel.b * 19) >> 8);
                min = Math.Min(min, luminance);
                max = Math.Max(max, luminance);
                total += luminance;
                samples++;
            }

            averageLuminance = samples > 0 ? total / (float)samples : 0f;
            return averageLuminance > 45f && averageLuminance < 250f && max - min > 18;
        }

        private static void CreateContactSheet(string outputPath, IReadOnlyList<string> paths)
        {
            const int columns = 2;
            int rows = Mathf.CeilToInt(paths.Count / (float)columns);
            const int padding = 18;
            int width = CaptureWidth * columns + padding * (columns + 1);
            int height = CaptureHeight * rows + padding * (rows + 1);
            var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Texture2D[] captures = new Texture2D[paths.Count];
            try
            {
                Color32[] background = new Color32[width * height];
                for (int i = 0; i < background.Length; i++)
                    background[i] = new Color32(18, 17, 15, 255);
                sheet.SetPixels32(background);

                for (int i = 0; i < paths.Count; i++)
                {
                    captures[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!captures[i].LoadImage(File.ReadAllBytes(paths[i])))
                        throw new InvalidOperationException($"Could not load M01 capture for contact sheet: {paths[i]}");

                    int column = i % columns;
                    int row = rows - 1 - i / columns;
                    sheet.SetPixels32(padding + column * (CaptureWidth + padding), padding + row * (CaptureHeight + padding), CaptureWidth, CaptureHeight, captures[i].GetPixels32());
                }

                sheet.Apply(false, false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < captures.Length; i++)
                {
                    if (captures[i] != null)
                        Object.DestroyImmediate(captures[i]);
                }
                Object.DestroyImmediate(sheet);
            }
        }

        private static void WriteCaptureManifest(string outputDirectory, IReadOnlyList<string> paths, string contactSheetPath)
        {
            string manifestPath = Path.Combine(outputDirectory, "m01_visual_prototype_capture_manifest.md");
            string fingerprint = ComputeSceneFingerprint(FindSceneRoot(SceneManager.GetActiveScene()));
            string[] lines =
            {
                "# M01 Visual Prototype Capture Manifest",
                string.Empty,
                $"- Generator: `{GeneratorVersion}`",
                $"- Seed: `{GenerationSeed}`",
                $"- Unity: `{Application.unityVersion}`",
                $"- Scene: `{ScenePath}`",
                $"- Semantic fingerprint: `{fingerprint}`",
                $"- Resolution: `{CaptureWidth}x{CaptureHeight}`",
                $"- Gameplay overview: `{Path.GetFileName(paths[0])}`",
                $"- Old Market approach: `{Path.GetFileName(paths[1])}`",
                $"- Bombing aftermath: `{Path.GetFileName(paths[2])}`",
                $"- Top-down plan: `{Path.GetFileName(paths[3])}`",
                $"- Contact sheet: `{Path.GetFileName(contactSheetPath)}`",
                string.Empty,
                "This is an isolated visual prototype. It does not claim gameplay, navigation, loading, Addressables, or device acceptance."
            };
            File.WriteAllLines(manifestPath, lines);
        }

        private static GameObject FindSceneRoot(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, "M01_VisualPrototype_Root", StringComparison.Ordinal))
                    return roots[i];
            }

            throw new InvalidOperationException("M01 visual prototype root is missing.");
        }

        private static string ComputeSceneFingerprint(GameObject root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var builder = new StringBuilder(256 * 1024);
            AppendTransformFingerprint(root.transform, root.name, builder);
            using SHA256 sha = SHA256.Create();
            byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            return BitConverter.ToString(digest).Replace("-", string.Empty);
        }

        private static void AppendTransformFingerprint(Transform transform, string path, StringBuilder builder)
        {
            builder.Append(path).Append('|');
            AppendVector(builder, transform.localPosition);
            AppendQuaternion(builder, transform.localRotation);
            AppendVector(builder, transform.localScale);
            builder.Append(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject)).Append('|');

            Renderer renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    builder.Append(AssetDatabase.GetAssetPath(materials[i])).Append(';');
            }

            Camera camera = transform.GetComponent<Camera>();
            if (camera != null)
            {
                builder.Append(camera.orthographic).Append('|')
                    .Append(camera.fieldOfView.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(camera.orthographicSize.ToString("R", CultureInfo.InvariantCulture)).Append('|');
            }

            Light light = transform.GetComponent<Light>();
            if (light != null)
            {
                builder.Append((int)light.type).Append('|')
                    .Append(light.intensity.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(ColorUtility.ToHtmlStringRGBA(light.color)).Append('|');
            }

            builder.AppendLine();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                AppendTransformFingerprint(child, $"{path}/{i}:{child.name}", builder);
            }
        }

        private static void AppendVector(StringBuilder builder, Vector3 value)
        {
            builder.Append(value.x.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(value.y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(value.z.ToString("R", CultureInfo.InvariantCulture)).Append('|');
        }

        private static void AppendQuaternion(StringBuilder builder, Quaternion value)
        {
            builder.Append(value.x.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(value.y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(value.z.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(value.w.ToString("R", CultureInfo.InvariantCulture)).Append('|');
        }
    }
#endif
}
