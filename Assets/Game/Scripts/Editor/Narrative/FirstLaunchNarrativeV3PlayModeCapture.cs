using System;
using System.IO;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// One-shot, real Menu-scene Play Mode capture used by the V3 visual-lock gate.
    /// SessionState keeps the capture armed across the Editor's Play Mode domain reload.
    /// </summary>
    [InitializeOnLoad]
    public static class FirstLaunchNarrativeV3PlayModeCapture
    {
        private const string ArmedKey = "Warline.FirstLaunchV3.LiveCapture.Armed";
        private const string WidthKey = "Warline.FirstLaunchV3.LiveCapture.Width";
        private const string HeightKey = "Warline.FirstLaunchV3.LiveCapture.Height";
        private const string SuffixKey = "Warline.FirstLaunchV3.LiveCapture.Suffix";
        private const string ComicBackground16Path = "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P04.png";
        private const string ComicBackground20Path = "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P04.png";
        private const string IdentityBackgroundPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_background_21x9_no_ui.png";
        private const string GuidanceBackgroundPath = "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P09.png";

        private static int stateIndex;
        private static int frameCount;
        private static string pendingPath;
        private static bool captureRequested;
        private static bool stateConfigured;
        private static bool exitRequested;

        static FirstLaunchNarrativeV3PlayModeCapture()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ResumeIfArmed();
        }

        [MenuItem("Game/UI/V3/Capture Live First Launch 1920x1080")]
        public static void Capture1920() => Begin(1920, 1080, "16x9");

        [MenuItem("Game/UI/V3/Capture Live First Launch 4800x2160")]
        public static void Capture4800() => Begin(4800, 2160, "20x9");

        private static void Begin(int width, int height, string suffix)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("First Launch live capture must start outside Play Mode.");

            FirstLaunchNarrativeV3PrefabBuilder.Build();
            MainMenuV3PrefabBuilder.SetGameViewResolution(width, height);
            EditorSceneManager.OpenScene(FirstLaunchNarrativeMenuSceneInstaller.MenuScenePath, OpenSceneMode.Single);
            FirstLaunchNarrativeReviewUtilitySystemHelper.Request();

            SessionState.SetBool(ArmedKey, true);
            SessionState.SetInt(WidthKey, width);
            SessionState.SetInt(HeightKey, height);
            SessionState.SetString(SuffixKey, suffix);
            ResetRuntimeState();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void ResumeIfArmed()
        {
            if (!SessionState.GetBool(ArmedKey, false))
                return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ArmedKey, false))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ResetRuntimeState();
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && exitRequested)
            {
                CompleteAndExit();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ArmedKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                if (exitRequested)
                    CompleteAndExit();
                return;
            }

            frameCount++;
            if (frameCount < 30)
                return;

            if (!stateConfigured)
            {
                ConfigureState(stateIndex);
                stateConfigured = true;
            }
            Canvas.ForceUpdateCanvases();

            if (!captureRequested)
            {
                // Let controls complete their authored color transition after the
                // hidden narrative CanvasGroup becomes interactive. Capturing on the
                // enable frame falsely made stable opaque panels look translucent.
                if (frameCount < 38)
                    return;
                pendingPath = OutputPath(stateIndex, SessionState.GetString(SuffixKey, "capture"));
                if (File.Exists(pendingPath))
                    File.Delete(pendingPath);
                ScreenCapture.CaptureScreenshot(pendingPath);
                captureRequested = true;
                frameCount = 0;
                return;
            }

            if (!File.Exists(pendingPath) || new FileInfo(pendingPath).Length == 0)
                return;

            Debug.Log($"[FirstLaunchNarrativeV3PlayModeCapture] state={StateName(stateIndex)} capture={pendingPath} screen={Screen.width}x{Screen.height}");
            stateIndex++;
            captureRequested = false;
            stateConfigured = false;
            pendingPath = null;
            frameCount = 0;

            if (stateIndex < 4)
                return;

            exitRequested = true;
            EditorApplication.isPlaying = false;
        }

        private static void ConfigureState(int index)
        {
            FirstLaunchLanguageChoiceView language = UnityEngine.Object.FindAnyObjectByType<FirstLaunchLanguageChoiceView>(FindObjectsInactive.Include);
            NarrativeSequenceView narrative = UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
            if (language == null || narrative == null)
                throw new MissingReferenceException("Live Menu scene is missing First Launch V3 views.");

            // Reviewer mode is used only to keep the first-launch route available in
            // Play Mode. Its navigation strip and skip-confirmation modal are QA tools,
            // not part of any player-facing V3 target lock.
            if (narrative.ReviewerControlsView != null)
                narrative.ReviewerControlsView.gameObject.SetActive(false);
            if (narrative.SkipConfirmationView != null)
                narrative.SkipConfirmationView.gameObject.SetActive(false);

            if (index == 0)
            {
                narrative.SetVisible(false);
                language.SetVisible(true);
                return;
            }

            language.SetVisible(false);
            narrative.SetVisible(true);
            narrative.SetSkipState(index == 1, index == 1, "SKIP");
            narrative.DialogueView.SetPhase(index == 1 ? NarrativeDialoguePhase.AdvanceReady : NarrativeDialoguePhase.Hidden);

            if (index == 1)
            {
                narrative.ApplyPanel(new NarrativePanelPresentationModel
                {
                    StateId = "FL-P04",
                    PanelSprite = RequireSprite(IsUltrawide() ? ComicBackground20Path : ComicBackground16Path),
                    Tint = Color.white
                });
                narrative.ApplyLocation(new NarrativeLocationPresentationModel
                {
                    Visible = true,
                    Title = "SAHRIN",
                    Subtitle = "OLD MARKET / 10:00 LOCAL"
                });
                narrative.SetInteractiveState(NarrativeInteractiveStateKind.None);
                narrative.DialogueView.ApplySpeaker(new NarrativeSpeakerPresentationModel
                {
                    SpeakerId = NarrativeSpeakerId.Dalia,
                    DisplayName = "DALIA RAHIM",
                    Role = "JRC FIELD COMMAND",
                    AccessibleLabel = "Major Dalia Rahim, JRC Field Command",
                    IdentitySprite = RequireSprite(FirstLaunchNarrativeDialogueAssetImporter.DaliaPortraitPath),
                    AccentColor = new Color32(18, 184, 231, 255),
                    Treatment = NarrativeSpeakerTreatment.HumanPortrait
                });
                narrative.DialogueView.PrepareLine(
                    "District Dispatch, Major Dalia Rahim, JRC Field Command.\nWe found the convoy survivors. Extraction is underway.",
                    NarrativeSubtitleStyleUtilitySystemHelper.Resolve(Game.UI.Runtime.SettingsService.Defaults));
                narrative.DialogueView.CompleteLine();
                return;
            }

            NarrativeInteractiveStateKind kind = index == 2
                ? NarrativeInteractiveStateKind.CommanderIdentity
                : NarrativeInteractiveStateKind.GuidanceChoice;
            narrative.ApplyPanel(new NarrativePanelPresentationModel
            {
                StateId = "review",
                PanelSprite = RequireSprite(index == 2 ? IdentityBackgroundPath : GuidanceBackgroundPath),
                Tint = Color.white
            });
            narrative.SetInteractiveState(kind);
            if (index == 2)
            {
                narrative.CommanderIdentityView.SetIdentity("ECHO-7", "Commander", 3);
                narrative.CommanderIdentityView.SetControlsInteractable(true);
            }
            else
            {
                narrative.GuidanceChoiceView.SetSelectedGuidance(NarrativeGuidanceMode.Full);
                narrative.GuidanceChoiceView.SetControlsInteractable(true);
            }
        }

        private static bool IsUltrawide() => SessionState.GetString(SuffixKey, string.Empty) == "20x9";

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new MissingReferenceException($"First Launch live capture asset missing: {path}");
            return sprite;
        }

        private static string OutputPath(int index, string suffix) =>
            $"/private/tmp/warline-first-launch-live-{StateName(index)}-v3-{suffix}.png";

        private static string StateName(int index) => index switch
        {
            0 => "language",
            1 => "comic",
            2 => "identity",
            3 => "guidance",
            _ => "unknown"
        };

        private static void ResetRuntimeState()
        {
            stateIndex = 0;
            frameCount = 0;
            pendingPath = null;
            captureRequested = false;
            stateConfigured = false;
            exitRequested = false;
        }

        private static void CompleteAndExit()
        {
            int width = SessionState.GetInt(WidthKey, 0);
            int height = SessionState.GetInt(HeightKey, 0);
            string suffix = SessionState.GetString(SuffixKey, "capture");
            SessionState.EraseBool(ArmedKey);
            SessionState.EraseInt(WidthKey);
            SessionState.EraseInt(HeightKey);
            SessionState.EraseString(SuffixKey);
            EditorApplication.update -= Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            Debug.Log($"[FirstLaunchNarrativeV3PlayModeCapture] result=Passed states=4 requested={width}x{height} suffix={suffix}");
            if (IsCommandLineCapture())
                EditorApplication.Exit(0);
        }

        private static bool IsCommandLineCapture()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++)
            {
                if (!string.Equals(arguments[i], "-executeMethod", StringComparison.OrdinalIgnoreCase))
                    continue;

                return arguments[i + 1].StartsWith(
                    "Game.Editor.FirstLaunchNarrativeV3PlayModeCapture.Capture",
                    StringComparison.Ordinal);
            }

            return false;
        }
    }
}
