using System;
using System.IO;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class FirstLaunchLanguageVisualValidation
    {
        private const string EvidenceRoot = "Temp/FirstLaunchLanguageEvidence";

        [MenuItem("Game/Narrative/First Launch/Capture Language Evidence")]
        public static void Capture()
        {
            Directory.CreateDirectory(EvidenceRoot);
            CaptureLanguageChoice(
                Path.Combine(EvidenceRoot, "language-choice-english-1920x1080.png"),
                FirstLaunchNarrativeLanguage.English);
            CaptureLanguageChoice(
                Path.Combine(EvidenceRoot, "language-choice-persian-1920x1080.png"),
                FirstLaunchNarrativeLanguage.Persian);
            CapturePersianDialogue(Path.Combine(EvidenceRoot, "persian-dialogue-1920x1080.png"));
            Debug.Log($"[FirstLaunchLanguageVisualValidation] result=Passed evidence={EvidenceRoot}");
        }

        private static void CaptureLanguageChoice(
            string outputPath,
            FirstLaunchNarrativeLanguage language)
        {
            CapturePrefab(
                FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath,
                outputPath,
                instance =>
                {
                    FirstLaunchLanguageChoiceView view = instance.GetComponent<FirstLaunchLanguageChoiceView>();
                    if (view == null)
                        throw new InvalidOperationException("Language choice prefab is missing its passive view.");
                    // Edit-mode prefab instantiation does not invoke MonoBehaviour.Awake. Bind through the
                    // same public path used by the runtime composition so the real card listeners are active.
                    view.Bind(_ => { });
                    view.SetVisible(true);
                    string buttonName = language == FirstLaunchNarrativeLanguage.Persian
                        ? "PersianButton"
                        : "EnglishButton";
                    Button selection = instance.transform.Find($"Composition/{buttonName}")?.GetComponent<Button>();
                    if (selection == null)
                        throw new InvalidOperationException($"Language choice prefab is missing {buttonName}.");
                    selection.onClick.Invoke();
                    AssertLanguageChoiceState(instance, language);
                    AssertRtlComponents(instance);
                });
        }

        private static void AssertLanguageChoiceState(
            GameObject instance,
            FirstLaunchNarrativeLanguage language)
        {
            bool persian = language == FirstLaunchNarrativeLanguage.Persian;
            string expectedLocale = persian
                ? GameLocalization.PersianLocaleCode
                : GameLocalization.EnglishLocaleCode;
            if (!string.Equals(GameLocalization.CurrentLocaleCode, expectedLocale, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Language card did not preview {expectedLocale}; active locale is {GameLocalization.CurrentLocaleCode}.");
            }

            AssertOriginalText(
                instance,
                "Composition/Title",
                persian ? "زبان داستان را انتخاب کنید" : "SELECT STORY LANGUAGE");
            AssertOriginalText(
                instance,
                "Composition/InfoPanel/InfoText",
                persian
                    ? "بعداً می‌توانید این مورد را\nدر تنظیمات فرماندهی تغییر دهید."
                    : "This can be changed later\nin Command Settings.");
            AssertOriginalText(
                instance,
                "Composition/ContinueButton/Label",
                persian ? "ادامه   ‹" : "CONTINUE   ›");

            // The cards are language samples. They deliberately stay in their own language.
            AssertOriginalText(instance, "Composition/EnglishButton/Language", "ENGLISH");
            AssertOriginalText(instance, "Composition/PersianButton/Language", "فارسی");
        }

        private static void AssertOriginalText(GameObject instance, string path, string expected)
        {
            TMP_Text target = instance.transform.Find(path)?.GetComponent<TMP_Text>();
            if (target == null)
                throw new InvalidOperationException($"Language choice prefab is missing text at {path}.");

            string actual = target is RTLTextMeshPro rtl ? rtl.OriginalText : target.text;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Language choice text mismatch at {path}. Expected '{expected}', got '{actual}'.");
            }
        }

        private static void CapturePersianDialogue(string outputPath)
        {
            CapturePrefab(
                FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath,
                outputPath,
                instance =>
                {
                    NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
                    NarrativeLocaleConfig locale = RequireAsset<NarrativeLocaleConfig>(FirstLaunchNarrativeConfigBuilder.PersianLocalePath);
                    FirstLaunchNarrativeLocaleTextCompositionSystemHelper resolver = new(FallbackGameTextResolver.Instance, locale);
                    view.ApplyLanguage(true, resolver);
                    view.SetVisible(true);
                    view.ApplyPanel(new NarrativePanelPresentationModel
                    {
                        StateId = "FL-P04",
                        PanelSprite = RequireAsset<Sprite>("Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png"),
                        Tint = Color.white
                    });
                    view.ApplyLocation(default);
                    view.DialogueView.ApplySpeaker(new NarrativeSpeakerPresentationModel
                    {
                        SpeakerId = NarrativeSpeakerId.Dalia,
                        DisplayName = resolver.Get("narrative.first_launch.speaker.dalia.name", "DALIA RAHIM"),
                        Role = resolver.Get("narrative.first_launch.speaker.dalia.role", "JRC FIELD COMMAND"),
                        AccessibleLabel = resolver.Get("narrative.first_launch.speaker.dalia.accessible_label", "Major Dalia Rahim"),
                        IdentitySprite = RequireAsset<Sprite>(FirstLaunchNarrativeDialogueAssetImporter.DaliaPortraitPath),
                        AccentColor = Color.white,
                        Treatment = NarrativeSpeakerTreatment.HumanPortrait
                    });
                    UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
                    view.DialogueView.PrepareLine(
                        resolver.Get("narrative.first_launch.line.p04_dalia", string.Empty),
                        NarrativeSubtitleStyleUtilitySystemHelper.Resolve(settings));
                    view.DialogueView.CompleteLine();
                    view.SetSkipState(true, true, resolver.Get("narrative.first_launch.control.skip", "SKIP"));
                    AssertRtlComponents(instance);
                });
        }

        private static void CapturePrefab(string prefabPath, string outputPath, Action<GameObject> configure)
        {
            GameObject cameraObject = new("FirstLaunchLanguageCaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new("FirstLaunchLanguageCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(4800f, 2160f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject instance = PrefabUtility.InstantiatePrefab(RequireAsset<GameObject>(prefabPath), canvas.transform) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Unable to instantiate {prefabPath}");
            configure(instance);

            RenderTexture target = new(1920, 1080, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D capture = new(1920, 1080, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            capture.Apply(false);
            AssertCaptureHasVisualContent(capture, prefabPath);
            File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }

        private static void AssertCaptureHasVisualContent(Texture2D capture, string prefabPath)
        {
            float minimum = 1f;
            float maximum = 0f;
            for (int y = 16; y < capture.height; y += 32)
            {
                for (int x = 16; x < capture.width; x += 32)
                {
                    float luminance = capture.GetPixel(x, y).grayscale;
                    minimum = Mathf.Min(minimum, luminance);
                    maximum = Mathf.Max(maximum, luminance);
                }
            }

            if (maximum - minimum < 0.1f)
            {
                throw new InvalidOperationException(
                    $"Language validation render is blank or uniform for {prefabPath}. " +
                    "Run the capture with a graphics device (omit -nographics).");
            }
        }

        private static void AssertRtlComponents(GameObject instance)
        {
            TMP_Text[] text = instance.GetComponentsInChildren<TMP_Text>(true);
            if (text.Length == 0)
                throw new InvalidOperationException("Language presentation contains no text components.");
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] is not RTLTextMeshPro)
                    throw new InvalidOperationException($"Text component does not support Persian shaping: {text[i].name}");
            }
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new FileNotFoundException($"Missing required language validation asset: {path}", path);
        }
    }
}
