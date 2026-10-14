using System;
using System.Collections.Generic;
using System.Linq;
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
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Real sequence-player starts for every recording, with six full-length samples.
    /// This is an automated playback probe, not a normal-input mission acceptance run.</summary>
    public static class CampaignFinaleComicVoicePlaybackProbe
    {
        private sealed class Item
        {
            public NarrativeSequenceConfig Sequence;
            public NarrativeStateRecord State;
            public NarrativeDialogueLineRecord Line;
            public string Root, Variant;
            public bool Persian, FullLength;
        }
        private static readonly List<Item> Items = new();
        private static readonly HashSet<string> Seen = new(StringComparer.Ordinal);
        private static SceneSetup[] originalScenes;
        private static string originalLocale;
        private static FirstLaunchNarrativeSequencePresentationSystemHelper player;
        private static NarrativeSequenceView view;
        private static GameObject canvas, probeListener;
        private static int index, fullLengthCount;
        private static bool active, prepared, exiting, refreshSuspended;
        private static double runStarted, itemStarted, lastTick;
        private static float greatestProgress;
        private static string failure;

        [MenuItem("Game/Campaign/Probe Finale Comic Voice Playback")]
        public static void Run()
        {
            if (active || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Playback probe requires an idle Editor outside Play Mode");
            if (!EditorSettings.enterPlayModeOptionsEnabled ||
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0)
                throw new InvalidOperationException("Existing-Editor probe requires DisableDomainReload");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save the open scenes before the isolated playback probe");
            CampaignFinaleComicVoiceValidation.Validate();
            originalScenes = EditorSceneManager.GetSceneManagerSetup();
            originalLocale = GameLocalization.CurrentLocaleCode;
            Items.Clear(); Seen.Clear();
            Add(CH05M03NetworkCollapseNarrativeBuilder.Path, CH05M03NetworkCollapseNarrativeBuilder.VoiceRoot);
            Add(CH05M04LastCorridorNarrativeBuilder.Path, CH05M04LastCorridorNarrativeBuilder.VoiceRoot);
            Add(CH05M05CommandNodeNarrativeBuilder.Path, CH05M05CommandNodeNarrativeBuilder.VoiceRoot);
            if (Items.Count != 126) throw new InvalidOperationException("Expected 126 playback cases");
            index = fullLengthCount = 0; prepared = exiting = false; failure = null; active = true; probeListener = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh(); refreshSuspended = true;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayStateChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void Add(string path, string root)
        {
            var sequences = AssetDatabase.LoadAllAssetsAtPath(path).OfType<NarrativeSequenceConfig>()
                .OrderBy(s => s.SequenceId, StringComparer.Ordinal).ToArray();
            foreach (bool fa in new[] { false, true })
            {
                bool first = true;
                foreach (var sequence in sequences) foreach (var state in sequence.States) foreach (var line in state.Lines)
                foreach (string variant in line.Speaker == NarrativeSpeakerId.Commander ? new[] { "", "_female", "_neutral" } : new[] { "" })
                {
                    Items.Add(new Item { Sequence = sequence, State = state, Line = line, Root = root,
                        Persian = fa, Variant = variant, FullLength = first });
                    first = false;
                }
            }
        }

        private static void Tick()
        {
            if (!active || exiting || !EditorApplication.isPlaying) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (!prepared)
                {
                    prepared = true; runStarted = lastTick = now;
                    EnsureSingleListener();
                    canvas = new GameObject("Finale comic probe", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvas.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(4800, 2160);
                    GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), "en", false);
                    StartItem(); return;
                }
                if (now - runStarted > 600) throw new TimeoutException("Finale playback probe exceeded ten minutes");
                EnsureSingleListener();
                player.Tick((float)Math.Min(now - lastTick, .1)); lastTick = now;
                var item = Items[index];
                var source = view.VoiceSource;
                var expected = CampaignFinaleComicVoiceInstaller.Voice(item.Root, item.Line.LineId, item.Persian, item.Variant);
                // Native audio can reach its final sample between Editor updates. Observe
                // the source reaching the end and stopping/changing instead of requiring
                // the dialogue UI's completion event to land on the same sampled frame.
                bool naturallyCompleted = item.FullLength && greatestProgress >= expected.length - .1f
                    && (source.clip != expected || !source.isPlaying);
                if (source.clip != null && source.clip != expected && !naturallyCompleted)
                    throw new InvalidOperationException("Wrong comic voice: " + AssetDatabase.GetAssetPath(source.clip));
                if (source.clip == expected && source.isPlaying && source.timeSamples > 0)
                {
                    if (source.loop || source.mute || source.volume <= 0 || Mathf.Abs(source.pitch - 1f) > .001f)
                        throw new InvalidOperationException("Inaudible, looping or wrong-speed comic voice");
                    greatestProgress = Mathf.Max(greatestProgress, (float)source.timeSamples / source.clip.frequency);
                    string path = AssetDatabase.GetAssetPath(source.clip);
                    if (Seen.Add(path)) Debug.Log("[CampaignFinaleNativeVoice] playing=" + path);
                }
                float target = Mathf.Min(.5f, expected.length * .5f);
                if (greatestProgress >= target && (!item.FullLength || naturallyCompleted))
                {
                    if (item.FullLength) { fullLengthCount++; Debug.Log("[CampaignFinaleNativeVoice] fullLength=" + AssetDatabase.GetAssetPath(expected)); }
                    player.Cancel(); UnityEngine.Object.Destroy(view.gameObject); index++;
                    if (index < Items.Count) { StartItem(); return; }
                    if (Seen.Count != 126 || fullLengthCount != 6)
                        throw new InvalidOperationException("Incomplete native playback coverage");
                    Finish(null); return;
                }
                if (now - itemStarted > expected.length + 25)
                    throw new TimeoutException("Voice did not progress: " + item.Line.LineId + item.Variant);
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(exception.Message); }
        }

        private static void EnsureSingleListener()
        {
            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
                .Where(l => l.isActiveAndEnabled).ToArray();
            // Runtime bootstrap may supply its own listener after an empty scene starts.
            // In that case only the temporary listener owned by this probe is disabled.
            if (listeners.Length > 1 && probeListener != null)
            {
                probeListener.GetComponent<AudioListener>().enabled = false;
                listeners = listeners.Where(l => l.isActiveAndEnabled).ToArray();
            }
            if (listeners.Length == 0)
            {
                if (probeListener == null) probeListener = new GameObject("Finale comic probe listener", typeof(AudioListener));
                else probeListener.GetComponent<AudioListener>().enabled = true;
                listeners = new[] { probeListener.GetComponent<AudioListener>() };
            }
            if (listeners.Length != 1) throw new InvalidOperationException("Exactly one active AudioListener is required");
        }

        private static void StartItem()
        {
            var item = Items[index];
            GameLocalization.SetLocale(item.Persian ? "fa-IR" : "en", false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            view = UnityEngine.Object.Instantiate(prefab, canvas.transform).GetComponent<NarrativeSequenceView>();
            player = new FirstLaunchNarrativeSequencePresentationSystemHelper();
            var resolver = new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(FallbackGameTextResolver.Instance);
            var locale = item.Persian ? AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath) : null;
            if (!player.Initialize(item.Sequence,
                AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath),
                AssetDatabase.LoadAssetAtPath<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath),
                view, resolver, Game.UI.Runtime.SettingsService.Defaults, locale))
                throw new InvalidOperationException("Native comic player initialization failed");
            // The V3 picker has six named portraits. The anonymous neutral voice is
            // the player's default before an identity is selected, not picker index 6
            // (which that UI clamps to its last female portrait).
            if (item.Variant != "_neutral")
                player.ApplyCommanderIdentity(new NarrativeCommanderIdentityData { Callsign = "COMMANDER", DisplayName = "Commander" },
                    item.Variant == "_female" ? 0 : 1);
            greatestProgress = 0; itemStarted = lastTick = EditorApplication.timeSinceStartup;
            if (!player.StartAt(item.State.StateId)) throw new InvalidOperationException("Native comic state failed to start");
        }

        private static void Finish(string error)
        {
            if (exiting) return;
            failure = error; exiting = true;
            player?.Cancel();
            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
        }

        private static void PlayStateChanged(PlayModeStateChange state)
        {
            if (!active || state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= PlayStateChanged;
            EditorApplication.update -= Tick;
            try
            {
                if (!exiting) failure = "Playback was interrupted before completion";
                EditorSceneManager.RestoreSceneManagerSetup(originalScenes);
                if (!string.IsNullOrEmpty(originalLocale)) GameLocalization.SetLocale(originalLocale, false);
            }
            catch (Exception exception) { failure = "Scene/locale restoration failed: " + exception.Message; }
            finally
            {
                if (refreshSuspended) { AssetDatabase.AllowAutoRefresh(); refreshSuspended = false; }
                active = false;
            }
            bool passed = failure == null;
            Debug.Log("[CampaignFinaleNativeComicPlayback] result=" + (passed ? "Passed" : "Failed")
                + " nativeStarts=" + Seen.Count + " fullLengthSamples=" + fullLengthCount
                + " locales=2 commanderVariants=3 audioListeners=1 scenesRestored=1 normalInputJourney=Pending audibleAcceptance=Pending"
                + (passed ? "" : " reason=" + failure));
            MissionEditorValidationExit.Complete(passed);
        }
    }
}
