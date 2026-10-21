#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBattleLibraryCapture
    {
        private static Action<GameObject, Camera> SagaChromeProbe;
        private static string SagaTheatreMap;
        private static float SagaCanvasDensity = 1f;

        public static void RunSagaScaleRegression()
        {
            string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/live-canvas-scale");
            Directory.CreateDirectory(folder);
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                GameLocalization.Initialize(catalog, "en", false);
                UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
                SkirmishSetupPrefabBuilder.Build();
                SagaCanvasDensity = 2f;
                SagaChromeProbe = (root, camera) =>
                {
                    int checkedStops = 0, checkedFooter = 0;
                    var corners = new Vector3[4];
                    foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(false))
                    {
                        bool stop = button.name.StartsWith("BattleCard_", StringComparison.Ordinal);
                        bool footer = button.name is "BackButton" or "ResetButton" or "RandomizeSeedButton" or "LaunchMissionButton" or "LibraryAdvancedButton";
                        if (!stop && !footer) continue;
                        ((RectTransform)button.transform).GetWorldCorners(corners);
                        float pixels = Mathf.Abs(camera.WorldToScreenPoint(corners[1]).y - camera.WorldToScreenPoint(corners[0]).y);
                        if (pixels < (stop ? 72f : 90f))
                            throw new InvalidOperationException("Live canvas shrank " + button.name + " to " + pixels + " pixels");
                        if (stop) checkedStops++; else checkedFooter++;
                    }
                    if (checkedStops != 24 || checkedFooter != 4)
                        throw new InvalidOperationException("Expected all native stops/footer controls in scale proof; stops="+checkedStops+" footer="+checkedFooter);
                    Debug.Log("[SagaLiveCanvasControlSize] stops="+checkedStops+" footer="+checkedFooter+" density=2");
                };
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath);
                Render(prefab, Sizes[1], "saga", Path.Combine(folder, "saga-20x9-live-4800x2160.png"), false);
                GameLocalization.SetLocale("fa-IR", false);
                Render(prefab, Sizes[2], "saga", Path.Combine(folder, "saga-4x3-fa-live-2880x2160.png"), false);
                Debug.Log("[SagaLiveCanvasScale] result=Passed density=2 files=2");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(folder, "failure.txt"), e.ToString());
                Debug.LogError("[SagaLiveCanvasScale] result=Failed " + e);
                EditorApplication.Exit(1);
            }
            finally { SagaCanvasDensity = 1f; SagaChromeProbe = null; }
        }

        private static void RejectSagaReadability(GameObject root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(false))
                if (node.name.StartsWith("SafeAreaEdge", StringComparison.Ordinal))
                    throw new InvalidOperationException("Diagnostic safe-area stroke is visible: " + node.name);
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(false))
                if (image.name.StartsWith("TrailMedal_", StringComparison.Ordinal) && (image.rectTransform.rect.width < 18f || image.rectTransform.rect.height < 18f))
                    throw new InvalidOperationException("Theatre medal trail collapsed: " + image.name + " size=" + image.rectTransform.rect.size);
            var search = Array.Find(root.GetComponentsInChildren<TMPro.TMP_InputField>(false), x => x.name.Contains("Search"));
            if (search == null)
                return;
            Rect SearchBounds(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                var a = root.transform.InverseTransformPoint(corners[0]);
                var b = root.transform.InverseTransformPoint(corners[2]);
                return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
            }

            Rect bounds = SearchBounds(search.GetComponent<RectTransform>());
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(false))
                if (button.name.StartsWith("ObjectiveTab_", StringComparison.Ordinal) && bounds.Overlaps(SearchBounds((RectTransform)button.transform)))
                    throw new InvalidOperationException("Search field overlaps objective tab " + button.name);
        }

        public static void RunSagaChromeProbe()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            GameLocalization.Initialize(catalog, "en", false);
            UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
            string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/chrome-probe");
            Directory.CreateDirectory(folder);
            SagaChromeProbe = (root, camera) =>
            {
                var rows = new List<string>();
                foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
                {
                    if (graphic is TMPro.TMP_Text)
                        continue;
                    var serialized = new SerializedObject(graphic);
                    var border = serialized.FindProperty("borderColor");
                    rows.Add(AnimationUtility.CalculateTransformPath(graphic.transform, root.transform) + " type=" + graphic.GetType().Name + " rect=" + graphic.rectTransform.rect + " color=" + graphic.color + " border=" + (border != null ? border.colorValue.ToString() : "n/a") + " texture=" + (graphic.mainTexture != null ? graphic.mainTexture.name : "none"));
                }

                File.WriteAllLines(Path.Combine(folder, "graphics.txt"), rows);
                foreach (string name in new[]
                {
                    "MapPreview",
                    "MapReadability",
                    "PlateVignette",
                    "OperationPreview"
                }

                )
                {
                    var graphic = Array.Find(root.GetComponentsInChildren<UnityEngine.UI.Graphic>(false), g => g.name == name);
                    if (graphic == null)
                        continue;
                    graphic.enabled = false;
                    camera.Render();
                    RenderTexture.active = camera.targetTexture;
                    var pixels = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, pixels.width, pixels.height), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder, name + "-off.png"), pixels.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(pixels);
                    graphic.enabled = true;
                }
            };
            try
            {
                Render(AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath), Sizes[0], "saga", Path.Combine(folder, "baseline.png"), false);
                Debug.Log("[SagaChromeProbe] result=Captured");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
            finally
            {
                SagaChromeProbe = null;
            }
        }

        public static void RunSagaNativeRegression()
        {
            ResetZoneCountLedger();
            BeginTheatreReport();
            string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/focused-native-11");
            Directory.CreateDirectory(folder);
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                GameLocalization.Initialize(catalog, "en", false);
                UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
                SkirmishSetupPrefabBuilder.Build();
                TheatreSeatGeometry.DumpDirectory = folder;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath);
                Render(prefab, Sizes[1], "theater", Path.Combine(folder, "theater-20x9-en.png"), false);
                GameLocalization.SetLocale("fa-IR", false);
                Render(prefab, Sizes[2], "theater", Path.Combine(folder, "theater-4x3-fa-IR.png"), false);
                Render(prefab, new CaptureSize("20x9-notch", 2400, 1080), "theater", Path.Combine(folder, "theater-20x9-notch-fa-IR.png"), true);
                SagaTheatreMap = "DB";
                Render(prefab, Sizes[0], "theater", Path.Combine(folder, "theater-16x9-fa-IR-selected-DB.png"), false);
                GameLocalization.SetLocale("en", false);
                SagaTheatreMap = "AP";
                Render(prefab, Sizes[1], "theater", Path.Combine(folder, "theater-20x9-en-selected-AP.png"), false);
                FinishTheatreReport();
                Debug.Log("[SagaNativeRegression] result=Passed files=5");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(folder, "failure.txt"), e + PendingTheatreSuffix());
                Debug.LogError("[SagaNativeRegression] result=Failed " + e + PendingTheatreSuffix());
                EditorApplication.Exit(1);
            }
            finally
            {
                TheatreSeatGeometry.DumpDirectory = null;
                SagaTheatreMap = null;
            }
        }

        public static void RunSagaApFourByThree()
        {
            ResetZoneCountLedger();
            BeginTheatreReport();
            string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/focused-ap-fa-4x3");
            Directory.CreateDirectory(folder);
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                GameLocalization.Initialize(catalog, "fa-IR", false);
                UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
                SkirmishSetupPrefabBuilder.Build();
                SagaTheatreMap = "AP";
                TheatreSeatGeometry.DumpDirectory = folder;
                Render(AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath), Sizes[2], "theater", Path.Combine(folder,"theater-4x3-fa-selected-AP.png"), false);
                FinishTheatreReport();
                Debug.Log("[SagaApFourByThree] result=Passed files=1");
                EditorApplication.Exit(0);
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(folder,"failure.txt"),e+PendingTheatreSuffix());
                Debug.LogError("[SagaApFourByThree] result=Failed "+e+PendingTheatreSuffix());
                EditorApplication.Exit(1);
            }
            finally { SagaTheatreMap=null; TheatreSeatGeometry.DumpDirectory=null; }
        }

        public static void RunSagaEnFourByThree()
        {
            ResetZoneCountLedger();
            BeginTheatreReport();
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            GameLocalization.Initialize(catalog, "en", false);
            UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
            string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/focused-en-4x3");
            Directory.CreateDirectory(folder);
            try
            {
                SkirmishSetupPrefabBuilder.Build();
                TheatreSeatGeometry.DumpDirectory = folder;
                Render(AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath), Sizes[2], "theater", Path.Combine(folder, "theater-4x3-en.png"), false);
                FinishTheatreReport();
                Debug.Log("[SagaEnFourByThree] result=Passed");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(folder, "failure.txt"), e + PendingTheatreSuffix());
                Debug.LogError("[SagaEnFourByThree] result=Failed " + e + PendingTheatreSuffix());
                EditorApplication.Exit(1);
            }
            finally
            {
                TheatreSeatGeometry.DumpDirectory = null;
            }
        }

        public static void RunSagaFaFourByThree()
        {
            ResetZoneCountLedger();
            BeginTheatreReport();
            var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            GameLocalization.Initialize(catalog, "fa-IR", false);
            UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
            string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/focused-fa");
            Directory.CreateDirectory(folder);
            try
            {
                SkirmishSetupPrefabBuilder.Build();
                TheatreSeatGeometry.DumpDirectory = folder;
                Render(AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath), Sizes[2], "theater", Path.Combine(folder, "theater-4x3-fa-IR.png"), false);
                FinishTheatreReport();
                Debug.Log("[SagaFocusedCapture] result=Passed");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(folder, "failure.txt"), e + PendingTheatreSuffix());
                Debug.LogError("[SagaFocusedCapture] result=Failed " + e + PendingTheatreSuffix());
                EditorApplication.Exit(1);
            }
            finally
            {
                TheatreSeatGeometry.DumpDirectory = null;
            }
        }

        private static string SagaBattleScenario;
        private static bool SagaBriefingAtBottom;
        public static void RunSagaBriefingRegression()
        {
            ResetZoneCountLedger();
            BeginTheatreReport();
            string folder=Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/briefing-regression");
            Directory.CreateDirectory(folder);
            try
            {
                GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), "en", false);
                UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
                SkirmishSetupPrefabBuilder.Build();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath);
                int files=0;
                foreach(string language in new[]{"en","fa-IR"})
                foreach(string scenario in new[]{"S002","S004"})
                {
                    GameLocalization.SetLocale(language,false);
                    SagaBattleScenario=scenario;
                    Render(prefab,Sizes[2],"saga",Path.Combine(folder,"saga-4x3-"+language+"-"+scenario+".png"),false);
                    files++;
                    if(language=="fa-IR" && scenario=="S004")
                    {
                        SagaBriefingAtBottom=true;
                        Render(prefab,Sizes[2],"saga",Path.Combine(folder,"saga-4x3-fa-IR-S004-scrolled.png"),false);
                        SagaBriefingAtBottom=false;
                        files++;
                    }
                }
                FinishTheatreReport();
                Debug.Log("[SagaBriefingRegression] result=Passed files="+files+" localizationReapplied=1 fullTextFits=1");
                EditorApplication.Exit(0);
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(folder,"failure.txt"),e+PendingTheatreSuffix());
                Debug.LogError("[SagaBriefingRegression] result=Failed "+e+PendingTheatreSuffix());
                EditorApplication.Exit(1);
            }
            finally { SagaBattleScenario=null; SagaBriefingAtBottom=false; }
        }

        public static void RunSagaValidation()
        {
            ResetZoneCountLedger();
            BeginTheatreReport();
            string previousLocale = GameLocalization.CurrentLocaleCode;
            string folder = Path.GetFullPath("Design/AgentReports/SkirmishBattleLibrary/2026-10-20/captures");
            var shots = new List<CaptureIdentity>();
            var failures = new List<string>();
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                if (catalog == null)
                    throw new FileNotFoundException("V3 localisation catalog missing.");
                GameLocalization.Initialize(catalog, "en", false);
                UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());
                SkirmishSetupPrefabBuilder.Build();
                SkirmishSetupPrefabBuilder.RequireFreshPrefab();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath);
                Directory.CreateDirectory(folder);
                TheatreSeatGeometry.DumpDirectory = folder;
                foreach (string locale in Locales)
                {
                    GameLocalization.SetLocale(locale, false);
                    foreach (var size in new[]
                    {
                        Sizes[0],
                        Sizes[1],
                        Sizes[2],
                        new CaptureSize("20x9-notch", 2400, 1080)
                    }

                    )
                        foreach (string state in new[]
                        {
                            "theater",
                            "saga"
                        }

                        )
                        {
                            string path = Path.Combine(folder, state + "-" + size.Name + "-" + locale + ".png");
                            try
                            {
                                Render(prefab, size, state, path, size.Name.EndsWith("notch", StringComparison.Ordinal));
                                AcceptShot(path, state, locale, shots);
                                Debug.Log("[SkirmishSagaCapture] shot=" + Path.GetFileName(path));
                            }
                            catch (Exception e)
                            {
                                failures.Add(Path.GetFileName(path) + ": " + e.Message);
                                Debug.LogError("[SkirmishSagaCapture] shotFailed=" + Path.GetFileName(path) + " " + e);
                            }
                        }
                }

                // Selection changes use the same coast layout. These regressions
                // include the Persian Desert Base attachment that used to move its card.
                foreach (var selected in new[] { (Map: "DB", Locale: "fa-IR", Size: Sizes[0]),
                    (Map: "AP", Locale: "en", Size: Sizes[1]) })
                {
                    GameLocalization.SetLocale(selected.Locale, false);
                    SagaTheatreMap = selected.Map;
                    string path = Path.Combine(folder, "theater-" + selected.Size.Name + "-" + selected.Locale
                        + "-selected-" + selected.Map + ".png");
                    try
                    {
                        Render(prefab, selected.Size, "theater", path, false);
                        AcceptShot(path, "theater", selected.Locale, shots);
                        Debug.Log("[SkirmishSagaCapture] shot=" + Path.GetFileName(path));
                    }
                    catch (Exception e)
                    { failures.Add(Path.GetFileName(path) + ": " + e.Message); }
                    finally { SagaTheatreMap = null; }
                }
                RejectDuplicateCaptures(shots);
                try
                {
                    FinishTheatreReport();
                }
                catch (Exception e)
                {
                    failures.Add(e.Message);
                }

                if (shots.Count != 18)
                    failures.Add("Expected 16 matrix captures and 2 selection regressions; wrote " + shots.Count);
                if (failures.Count > 0)
                    throw new InvalidOperationException(string.Join("\n", failures));
                Debug.Log("[SkirmishSagaCapture] result=Passed files=18 matrix=16 selectionRegressions=2 visualReview=Pending deviceAcceptance=Pending");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "failures.txt"), e + PendingTheatreSuffix());
                Debug.LogError("[SkirmishSagaCapture] result=Failed " + e + PendingTheatreSuffix());
                EditorApplication.Exit(1);
            }
            finally
            {
                TheatreSeatGeometry.DumpDirectory = null;
                if (!string.IsNullOrEmpty(previousLocale))
                    GameLocalization.SetLocale(previousLocale, false);
            }
        }
    }
}
#endif

