#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Renders the SCN-13 battle library to PNG for Theatre, Map and Grid at 16:9, 20:9 and 4:3,
    /// in English and Persian. This is an Editor capture, not player-readiness evidence.
    /// </summary>
    public static class SkirmishBattleLibraryCapture
    {
        public const string OutputDirectory = "Design/AgentReports/SkirmishBattleLibrary/2026-10-14";
        public const string PassMarker = "[SkirmishBattleLibraryCapture] result=Passed";

        private static readonly CaptureSize[] Sizes =
        {
            new CaptureSize("16x9", 1920, 1080),
            new CaptureSize("20x9", 2400, 1080),
            new CaptureSize("4x3", 1440, 1080)
        };

        private static readonly string[] Locales = { "en", "fa-IR" };
        private static readonly string[] States = { "theater", "map", "grid" };

        [MenuItem("Game/UI/V3/Capture Skirmish Battle Library")]
        public static void CaptureMenu()
        {
            Capture(false);
        }

        public static void RunFocusedValidation()
        {
            Capture(true);
        }

        private static void Capture(bool quit)
        {
            var created = new System.Collections.Generic.List<UnityEngine.Object>();
            string previousLocale = GameLocalization.CurrentLocaleCode;
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                if (catalog == null)
                    throw new FileNotFoundException("Missing V3 localisation catalog.", V3UiLocalizationCatalogBuilder.CatalogPath);
                GameLocalization.Initialize(catalog, "en", false);
                UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab");
                if (prefab == null)
                    throw new FileNotFoundException("Missing SCN-13 prefab.");

                string folder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), OutputDirectory));
                Directory.CreateDirectory(folder);
                int written = 0;
                for (int localeIndex = 0; localeIndex < Locales.Length; localeIndex++)
                {
                    GameLocalization.SetLocale(Locales[localeIndex], false);
                    for (int sizeIndex = 0; sizeIndex < Sizes.Length; sizeIndex++)
                    {
                        for (int stateIndex = 0; stateIndex < States.Length; stateIndex++)
                        {
                            string path = Path.Combine(
                                folder,
                                States[stateIndex] + "-" + Sizes[sizeIndex].Name + "-" + Locales[localeIndex] + ".png");
                            Render(prefab, Sizes[sizeIndex], States[stateIndex], path, created);
                            var info = new FileInfo(path);
                            if (!info.Exists || info.Length < 2048)
                                throw new InvalidOperationException("Capture was empty: " + path);
                            written++;
                        }
                    }
                }

                if (written != 18)
                    throw new InvalidOperationException("Expected 18 captures, wrote " + written + ".");
                Debug.Log(PassMarker + " files=" + written + " folder=" + OutputDirectory);
                if (quit)
                    EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishBattleLibraryCapture] result=Failed\n" + exception);
                if (quit)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
            finally
            {
                for (int i = created.Count - 1; i >= 0; i--)
                {
                    if (created[i] != null)
                        UnityEngine.Object.DestroyImmediate(created[i]);
                }

                if (!string.IsNullOrEmpty(previousLocale))
                    GameLocalization.SetLocale(previousLocale, false);
            }
        }

        private static void Render(GameObject prefab, CaptureSize size, string state, string path, System.Collections.Generic.List<UnityEngine.Object> created)
        {
            var cameraObject = new GameObject("SkirmishLibraryCaptureCamera");
            created.Add(cameraObject);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size.Height * 0.5f;
            camera.aspect = size.Width / (float)size.Height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.05f, 0.06f, 1f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 0f, -100f);

            var canvasObject = new GameObject("SkirmishLibraryCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            created.Add(canvasObject);
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.position = Vector3.zero;
            canvasRect.sizeDelta = new Vector2(size.Width, size.Height);
            canvasRect.localScale = Vector3.one;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            var instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
            created.Add(instance);
            var root = instance.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
            root.localPosition = Vector3.zero;

            var view = instance.GetComponent<QuickCustomScreenView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("baseAssaultPreset").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.Bind(default);
            view.PresentLibraryCapture(state);
            Canvas.ForceUpdateCanvases();
            instance.GetComponentInChildren<MainMenuV3SectionLayoutView>(true)?.RefreshLayout();
            foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                text.ForceMeshUpdate();

            var target = new RenderTexture(size.Width, size.Height, 24, RenderTextureFormat.ARGB32);
            created.Add(target);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(size.Width, size.Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, size.Width, size.Height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }

        private struct CaptureSize
        {
            public readonly string Name;
            public readonly int Width;
            public readonly int Height;
            public CaptureSize(string name, int width, int height)
            {
                Name = name;
                Width = width;
                Height = height;
            }
        }

        private sealed class CaptureLocalization : IUiLocalization
        {
            public event Action LocaleChanged
            {
                add => GameLocalization.LocaleChanged += value;
                remove => GameLocalization.LocaleChanged -= value;
            }

            public string CurrentLocaleCode => GameLocalization.CurrentLocaleCode;
            public bool IsRightToLeft => GameLocalization.IsRightToLeft;
            public UnityEngine.Object CurrentFontAsset => GameLocalization.CurrentFontAsset;
            public int AvailableLocaleCount => 2;
            public string[] GetLocaleShortLabels() => GameLocalization.GetLocaleShortLabels();
            public string[] GetLocaleSwitchLabels() => GameLocalization.GetLocaleSwitchLabels();
            public int GetLocaleIndex(string code) => GameLocalization.GetLocaleIndex(code);
            public string GetLocaleCode(int index) => GameLocalization.GetLocaleCode(index);
            public bool SetLocale(string code, bool persist = true) => GameLocalization.SetLocale(code, persist);
            public string Get(string key, string fallback = "") => GameLocalization.Get(key, fallback);
            public bool TryGet(string key, out string value) => GameLocalization.TryGet(key, out value);
            public string Format(string key, string fallback, params object[] args) => string.Format(Get(key, fallback), args);
            public string GetBySource(string source) => GameLocalization.GetBySource(source);
            public bool TryGetBySource(string source, out string key, out string value) => GameLocalization.TryGetBySource(source, out key, out value);
            public bool TryGetSourceByLocalized(string value, out string key, out string source) =>
                GameLocalization.TryGetSourceByLocalized(value, out key, out source);
        }
    }
}
#endif
