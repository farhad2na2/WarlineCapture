#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Clicks Theatre → Zoom In → objective tab → card → MAP → GRID → Start Battle
    /// with real button events and writes a numbered PNG for each step.
    /// </summary>
    public static class SkirmishBattleLibraryFlowCapture
    {
        public const string OutputDirectory = "Design/AgentReports/SkirmishBattleLibrary/2026-10-14/flow";
        public const string PassMarker = "[SkirmishBattleLibraryFlow] result=Passed";

        private static readonly string[] Locales = { "en", "fa-IR" };

        [MenuItem("Game/UI/V3/Capture Skirmish Battle Library Flow")]
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
            SkirmishBattleLibraryCapture.ResetZoneCountLedger();
            SkirmishBattleLibraryCapture.BeginTheatreReport();
            string previousLocale = GameLocalization.CurrentLocaleCode;
            var shots = new List<SkirmishBattleLibraryCapture.CaptureIdentity>();
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                if (catalog == null)
                    throw new FileNotFoundException("Missing V3 localisation catalog.", V3UiLocalizationCatalogBuilder.CatalogPath);
                GameLocalization.Initialize(catalog, "en", false);
                UiShellRuntimeGateway.BindLocalization(new FlowLocalization());

                SkirmishSetupPrefabBuilder.Build();
                SkirmishSetupPrefabBuilder.RequireFreshPrefab();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath);
                if (prefab == null)
                    throw new FileNotFoundException("Missing SCN-13 prefab.");

                string folder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), OutputDirectory));
                Directory.CreateDirectory(folder);
                TheatreSeatGeometry.DumpDirectory = folder;
                int written = 0;
                for (int localeIndex = 0; localeIndex < Locales.Length; localeIndex++)
                {
                    GameLocalization.SetLocale(Locales[localeIndex], false);
                    written += RunLocale(prefab, Locales[localeIndex], folder, shots);
                }

                if (written != 14)
                    throw new InvalidOperationException("Expected 14 flow captures, wrote " + written + ".");
                SkirmishBattleLibraryCapture.RejectDuplicateCaptures(shots);
                SkirmishBattleLibraryCapture.FinishTheatreReport();
                Debug.Log(PassMarker + " files=" + written + " folder=" + OutputDirectory);
                if (quit)
                    EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishBattleLibraryFlow] result=Failed\n" + exception
                    + SkirmishBattleLibraryCapture.PendingTheatreSuffix());
                if (quit)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
            finally
            {
                TheatreSeatGeometry.DumpDirectory = null;
                if (!string.IsNullOrEmpty(previousLocale))
                    GameLocalization.SetLocale(previousLocale, false);
            }
        }

        private static int RunLocale(
            GameObject prefab,
            string locale,
            string folder,
            List<SkirmishBattleLibraryCapture.CaptureIdentity> shots)
        {
            const int width = 1920;
            const int height = 1080;
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            RenderTexture target = null;
            try
            {
                cameraObject = new GameObject("SkirmishLibraryFlowCamera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = height * 0.5f;
                camera.aspect = width / (float)height;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.05f, 0.06f, 1f);
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 200f;
                camera.transform.position = new Vector3(0f, 0f, -100f);

                canvasObject = new GameObject("SkirmishLibraryFlowCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.pivot = new Vector2(0.5f, 0.5f);
                canvasRect.position = Vector3.zero;
                canvasRect.sizeDelta = new Vector2(width, height);
                canvasRect.localScale = Vector3.one;
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;

                var instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
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
                view.ApplyLibraryCaptureDemo(difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty);
                view.Bind(default);
                view.FocusTheaterOnMap("DB");
                instance.GetComponentInChildren<SkirmishLibrarySafeFrame>(true)?.ClearSimulatedInsets();
                instance.GetComponentInChildren<MainMenuV3SectionLayoutView>(true)?.RefreshLayout();
                string shotLocale = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? "fa" : "en";
                TheatreSeatGeometry.SetShot("16:9", "none", shotLocale, true, width, height, 0f, 0f, 0f, 0f);
                view.PresentLibraryCapture("theater");
                SkirmishBattleLibraryCapture.RejectUnresolvedSprites(view.Library);

                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;

                string suffix = locale == "fa-IR" ? "fa-IR" : "en";
                Shot(view, camera, canvasObject, target, width, height, Path.Combine(folder, "01-theater-" + suffix + ".png"), "theater", suffix, shots);
                AssertStep(view.Library.Level == SkirmishLibraryLevel.Theater, "Theatre did not stay on the theatre.");
                AssertStep(view.Library.Theater.SelectedMapId == "DB", "Theatre did not focus Desert Base.");
                AssertOverallProgress(instance, view);

                Click(Require(instance, SkirmishLibraryControlNames.ZoomIn));
                Shot(view, camera, canvasObject, target, width, height, Path.Combine(folder, "02-zoom-" + suffix + ".png"), "zoom", suffix, shots);
                AssertStep(view.Library.Level == SkirmishLibraryLevel.Battlefield, "Zoom In did not open the battlefield.");
                Transform hiddenStrip = instance.transform.Find("SkirmishSetupComposition/OperationPreview/OverallProgressStrip");
                AssertStep(hiddenStrip == null || !hiddenStrip.gameObject.activeInHierarchy, "Overall progress strip stayed on the battlefield.");
                AssertStep(view.Library.Battlefield.Layout == SkirmishBattlefieldLayout.Map, "Zoom In did not open the map.");
                AssertStep(view.Library.Battlefield.MapId == "DB", "Zoom In did not open Desert Base.");

                Click(Require(instance, SkirmishLibraryControlNames.ObjectiveTab("BA")));
                Shot(view, camera, canvasObject, target, width, height, Path.Combine(folder, "03-objective-" + suffix + ".png"), "zoom", suffix, shots);
                AssertStep(view.Library.Battlefield.SelectedObjectiveId == "BA", "Objective tab did not select BA.");

                Click(Require(instance, SkirmishLibraryControlNames.BattleCard("S001")));
                Shot(view, camera, canvasObject, target, width, height, Path.Combine(folder, "04-card-" + suffix + ".png"), "card", suffix, shots);
                AssertStep(view.Library.Briefing.ScenarioId == "S001", "Card did not select S001.");
                AssertStep(view.Library.Briefing.CanLaunch, "S001 cannot launch.");

                Click(Require(instance, SkirmishLibraryControlNames.LayoutMap));
                Shot(view, camera, canvasObject, target, width, height, Path.Combine(folder, "05-map-" + suffix + ".png"), "card", suffix, shots);
                AssertStep(view.Library.Battlefield.Layout == SkirmishBattlefieldLayout.Map, "MAP toggle did not select the map.");

                Button gridButton = Require(instance, SkirmishLibraryControlNames.LayoutGrid);
                AssertStep(
                    gridButton.name == SkirmishLibraryControlNames.LayoutGrid,
                    "GRID click target was " + gridButton.name + ".");
                Click(gridButton);
                Shot(view, camera, canvasObject, target, width, height, Path.Combine(folder, "06-grid-" + suffix + ".png"), "grid", suffix, shots);
                AssertStep(view.Library.Battlefield.Layout == SkirmishBattlefieldLayout.Grid, "GRID toggle did not select the grid.");
                AssertStep(view.Library.Briefing.ScenarioId == "S001", "GRID toggle cleared S001.");

                Click(Require(instance, SkirmishLibraryControlNames.StartBattle));
                Shot(view, camera, canvasObject, target, width, height, Path.Combine(folder, "07-launch-" + suffix + ".png"), "launch", suffix, shots);
                AssertStep(view.LibraryLaunchRequested, "Start Battle did not request a launch.");
                Transform launchTransition = instance.transform.Find("SkirmishSetupComposition/LaunchTransition");
                AssertStep(launchTransition != null && launchTransition.gameObject.activeInHierarchy, "Start Battle did not show the launch transition.");
                AssertStep(view.Library.Briefing.CanLaunch, "Start Battle left the briefing unable to launch.");
                AssertStep(
                    SkirmishLibraryLaunchContext.Difficulty == view.Library.Briefing.SelectedDifficulty,
                    "Start Battle did not publish the briefing difficulty to the launch context.");
                return 7;
            }
            finally
            {
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }

                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void AssertStep(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertOverallProgress(GameObject instance, QuickCustomScreenView view)
        {
            Transform strip = instance.transform.Find("SkirmishSetupComposition/OperationPreview/OverallProgressStrip");
            if (strip != null && strip.gameObject.activeInHierarchy)
            {
                SkirmishBattleLibraryCapture.NoteTheatre(
                    "Theatre still shows OverallProgressStrip. The battles-won strip and its medal counts are removed. Tier counts belong on TheatreSummary.");
            }

            Transform column = instance.transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            Transform summary = column != null ? column.Find("TheatreSummary") : null;
            string[] names =
            {
                "TheatreMedal_Recruit",
                "TheatreMedal_Regular",
                "TheatreMedal_Veteran",
                "TheatreMedal_Commander"
            };
            bool ready = summary != null && summary.gameObject.activeInHierarchy;
            var shown = new string[names.Length];
            if (ready)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    TMP_Text tier = summary.Find(names[i] + "/TierCount")?.GetComponent<TMP_Text>();
                    if (tier == null || !tier.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(tier.text))
                    {
                        ready = false;
                        break;
                    }

                    shown[i] = tier.text;
                }
            }

            if (!ready)
            {
                SkirmishBattleLibraryCapture.NoteTheatre("TheatreSummary missing until #68.");
                return;
            }

            Transform medalRow = column.Find("MedalRow");
            if (medalRow != null && medalRow.gameObject.activeInHierarchy)
            {
                SkirmishBattleLibraryCapture.NoteTheatre(
                    "Theatre shows MedalRow. The Theatre has no MedalRow. Tier counts belong on TheatreSummary.");
            }

            SkirmishTheaterState theater = view.Library.Theater;
            int[] medals =
            {
                theater.OverallMedalTotals.Recruit,
                theater.OverallMedalTotals.Regular,
                theater.OverallMedalTotals.Veteran,
                theater.OverallMedalTotals.Commander
            };
            for (int i = 0; i < medals.Length; i++)
            {
                if (SkirmishBattleLibraryCapture.TryParseTierCount(shown[i], out int parsed) && parsed == medals[i])
                    continue;
                SkirmishBattleLibraryCapture.NoteTheatre(
                    "TheatreSummary " + names[i] + " TierCount reads '" + shown[i] +
                    "'. SkirmishMedalLedger whole-theatre total is " + medals[i] + ".");
            }
        }

        private static Button Require(GameObject root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            Button parentFallback = null;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name != name)
                    continue;
                Button direct = transforms[i].GetComponent<Button>();
                if (direct != null)
                    return direct;
                if (parentFallback == null)
                    parentFallback = transforms[i].GetComponentInParent<Button>();
            }

            if (parentFallback != null)
                return parentFallback;
            throw new InvalidOperationException("Missing button " + name + ".");
        }

        private static void Click(Button button)
        {
            if (button == null || !button.isActiveAndEnabled || !button.interactable)
                throw new InvalidOperationException("Button is not interactive: " + (button != null ? button.name : "null"));
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerUpHandler);
            button.onClick.Invoke();
        }

        private static void Shot(
            QuickCustomScreenView view,
            Camera camera,
            GameObject canvas,
            RenderTexture target,
            int width,
            int height,
            string path,
            string state,
            string locale,
            List<SkirmishBattleLibraryCapture.CaptureIdentity> shots)
        {
            Canvas.ForceUpdateCanvases();
            view.GetComponentInChildren<MainMenuV3SectionLayoutView>(true)?.RefreshLayout();
            TMP_Text[] texts = view.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].ForceMeshUpdate();
            SkirmishBattleLibraryCapture.RejectMissingGlyphs(view.gameObject);
            SkirmishBattleLibraryCapture.RejectUntranslatedFa(view.gameObject, path);
            SkirmishBattleLibraryCapture.RejectEmptyCardSubtitles(view.gameObject);
            SkirmishBattleLibraryCapture.RejectGridCardsOutside(view.gameObject);
            SkirmishBattleLibraryCapture.RejectCollapsedLayout(view.gameObject);
            SkirmishBattleLibraryCapture.RejectBlankImages(view.gameObject, state);
            SkirmishBattleLibraryCapture.RejectExtraLibraryRoots();
            SkirmishBattleLibraryCapture.IsolateCaptureLayer(canvas, camera);
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            SkirmishBattleLibraryCapture.AcceptShot(path, state, locale, shots);
        }

        private sealed class FlowLocalization : IUiLocalization
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
