using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class FirstLaunchNarrativePerformanceValidation
    {
        private const string EvidenceDirectory = "Design/NarrativeVision/FirstLaunch/evidence/performance";
        private const string ReportPath = EvidenceDirectory + "/FIRST_LAUNCH_EDITOR_PERFORMANCE_REPORT.md";
        private const int StableTickCount = 1800;

        [MenuItem("Game/Narrative/First Launch/Validate Performance And Residency")]
        public static void Run()
        {
            FirstLaunchNarrativeConfigBuilder.Build();
            FirstLaunchNarrativePresentationPrefabBuilder.Build();
            Directory.CreateDirectory(EvidenceDirectory);

            NarrativeSequenceConfig sequence = RequireAsset<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
            NarrativeSpeakerCatalog speakers = RequireAsset<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath);
            NarrativePunctuationProfile punctuation = RequireAsset<NarrativePunctuationProfile>(FirstLaunchNarrativeConfigBuilder.PunctuationPath);
            GameObject prefab = RequireAsset<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            FirstLaunchNarrativePlayer player = new();
            try
            {
                UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
                settings.Audio.VoiceEnabled = false;
                settings.Narrative.AutoAdvance = false;
                if (!player.Initialize(sequence, speakers, punctuation, view, FallbackGameTextResolver.Instance, settings))
                    throw new InvalidOperationException("Unable to initialize First Launch performance validation.");

                Stopwatch stopwatch = Stopwatch.StartNew();
                if (!player.StartAt("FL-P01"))
                    throw new InvalidOperationException("Unable to start FL-P01 for performance validation.");
                stopwatch.Stop();
                double coldLoadMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                AssertResidency(player, "FL-P01 cold load");

                List<double> transitionMilliseconds = new();
                for (int panel = 2; panel <= 18; panel++)
                {
                    if (panel == 8)
                        continue;
                    stopwatch.Restart();
                    if (!player.StartAt($"FL-P{panel:00}"))
                        throw new InvalidOperationException($"Unable to start FL-P{panel:00} during transition validation.");
                    stopwatch.Stop();
                    transitionMilliseconds.Add(stopwatch.Elapsed.TotalMilliseconds);
                    AssertResidency(player, $"FL-P{panel:00} transition");
                }

                player.StartAt("FL-P01");
                for (int i = 0; i < 120; i++)
                    player.Tick(1f / 60f);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
                stopwatch.Restart();
                for (int i = 0; i < StableTickCount; i++)
                    player.Tick(1f / 60f);
                stopwatch.Stop();
                long stableAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
                if (stableAllocatedBytes != 0)
                    throw new InvalidOperationException($"Stable panel playback allocated {stableAllocatedBytes} managed bytes across {StableTickCount} ticks.");

                ValidateMissingAudioFallback(view, punctuation);
                ValidateOfflineRuntimeSources();
                ValidateRetailReviewerDefaults(prefab);

                long currentPanelBytes = view.CurrentPanelSprite != null && view.CurrentPanelSprite.texture != null
                    ? Profiler.GetRuntimeMemorySizeLong(view.CurrentPanelSprite.texture)
                    : 0L;
                long audioBytes = MeasureUniqueAudioMemory(sequence, out int audioClipCount);
                double averageTransitionMilliseconds = transitionMilliseconds.Count == 0 ? 0d : transitionMilliseconds.Average();
                double maximumTransitionMilliseconds = transitionMilliseconds.Count == 0 ? 0d : transitionMilliseconds.Max();
                WriteReport(
                    coldLoadMilliseconds,
                    averageTransitionMilliseconds,
                    maximumTransitionMilliseconds,
                    stopwatch.Elapsed.TotalMilliseconds,
                    stableAllocatedBytes,
                    player.ResidentPanelAssetCount,
                    currentPanelBytes,
                    audioClipCount,
                    audioBytes);

                Debug.Log($"[FirstLaunchNarrativePerformanceValidation] result=Passed report={ReportPath}");
            }
            finally
            {
                player.Cancel();
                Object.DestroyImmediate(instance);
                AssetDatabase.Refresh();
            }
        }

        private static void AssertResidency(FirstLaunchNarrativePlayer player, string context)
        {
            if (player.ResidentPanelAssetCount < 1 || player.ResidentPanelAssetCount > 2)
                throw new InvalidOperationException($"{context} retained {player.ResidentPanelAssetCount} panel assets; expected current and optional next only.");
            if (string.IsNullOrEmpty(player.CurrentPanelAssetKey))
                throw new InvalidOperationException($"{context} did not retain a current Addressables panel key.");
        }

        private static void ValidateMissingAudioFallback(NarrativeSequenceView view, NarrativePunctuationProfile punctuation)
        {
            UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
            settings.Narrative.AutoAdvance = true;
            NarrativeSequencePresenter presentation = new(view);
            presentation.StartDialogue(
                "Optional voice unavailable. Continue with readable text.",
                new NarrativeSpeakerPresentationModel
                {
                    SpeakerId = NarrativeSpeakerId.Aria,
                    DisplayName = "ARIA",
                    Role = "CIVIC RELAY ASSISTANT",
                    Treatment = NarrativeSpeakerTreatment.AriaIcon,
                    AccentColor = Color.cyan
                },
                null,
                1f,
                BuildPunctuationModel(punctuation),
                settings);
            presentation.Tick(2f);
            presentation.Tick(punctuation.TailHoldSeconds + 0.01f);
            if (!presentation.ConsumeAutoAdvanceRequest())
                throw new InvalidOperationException("Missing optional voice did not reach deterministic auto-advance.");
            presentation.Cancel();
        }

        private static NarrativePunctuationPresentationModel BuildPunctuationModel(
            NarrativePunctuationProfile profile)
        {
            return new NarrativePunctuationPresentationModel
            {
                CharactersPerSecond = profile.CharactersPerSecond,
                CommaPauseSeconds = profile.CommaPauseSeconds,
                ClausePauseSeconds = profile.ClausePauseSeconds,
                SentencePauseSeconds = profile.SentencePauseSeconds,
                EllipsisPauseSeconds = profile.EllipsisPauseSeconds,
                TailHoldSeconds = profile.TailHoldSeconds
            };
        }

        private static void ValidateOfflineRuntimeSources()
        {
            string[] runtimeRoots =
            {
                "Assets/Game/Scripts/UI/Narrative",
                "Assets/Game/Scripts/Composition/Narrative",
                "Assets/Game/Scripts/Configs/Narrative"
            };
            string[] sourceFiles = runtimeRoots
                .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                .ToArray();
            string[] forbidden = { "UnityWebRequest", "HttpClient", "speech.microsoft.com", "edge-tts", "Resources.Load" };
            foreach (string path in sourceFiles)
            {
                string source = File.ReadAllText(path);
                foreach (string token in forbidden)
                {
                    if (source.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Runtime narrative source {path} contains forbidden online/Resources token '{token}'.");
                }
            }
        }

        private static void ValidateRetailReviewerDefaults(GameObject prefab)
        {
            NarrativeReviewerControlsView reviewer = prefab.GetComponentInChildren<NarrativeReviewerControlsView>(true);
            if (reviewer == null)
                throw new InvalidOperationException("Development reviewer view is missing.");
            CanvasGroup group = reviewer.GetComponent<CanvasGroup>();
            if (group == null || group.alpha != 0f || group.interactable || group.blocksRaycasts)
                throw new InvalidOperationException("Development reviewer controls are not hidden and inert by default.");
        }

        private static long MeasureUniqueAudioMemory(NarrativeSequenceConfig sequence, out int count)
        {
            HashSet<AudioClip> clips = new();
            for (int stateIndex = 0; stateIndex < sequence.States.Count; stateIndex++)
            {
                IReadOnlyList<NarrativeDialogueLineRecord> lines = sequence.States[stateIndex].Lines;
                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    if (lines[lineIndex].VoiceClip != null)
                        clips.Add(lines[lineIndex].VoiceClip);
                }
            }
            count = clips.Count;
            long bytes = 0L;
            foreach (AudioClip clip in clips)
                bytes += Profiler.GetRuntimeMemorySizeLong(clip);
            return bytes;
        }

        private static void WriteReport(
            double coldLoadMilliseconds,
            double averageTransitionMilliseconds,
            double maximumTransitionMilliseconds,
            double stableTickMilliseconds,
            long stableAllocatedBytes,
            int residentPanels,
            long currentPanelBytes,
            int audioClipCount,
            long audioBytes)
        {
            StringBuilder report = new();
            report.AppendLine("# First Launch Editor Performance Report");
            report.AppendLine();
            report.AppendLine("Status: Passed editor baseline; physical Android device profiling remains required before release lock.");
            report.AppendLine($"Date: {DateTime.UtcNow:yyyy-MM-dd}");
            report.AppendLine($"Unity: {Application.unityVersion}");
            report.AppendLine($"Platform: {Application.platform}");
            report.AppendLine();
            report.AppendLine("| Measure | Result |");
            report.AppendLine("|---|---:|");
            report.AppendLine($"| Cold FL-P01 Addressables load | {Format(coldLoadMilliseconds)} ms |");
            report.AppendLine($"| Warm panel transition average | {Format(averageTransitionMilliseconds)} ms |");
            report.AppendLine($"| Warm panel transition maximum | {Format(maximumTransitionMilliseconds)} ms |");
            report.AppendLine($"| Stable playback sample | {StableTickCount} ticks / {Format(stableTickMilliseconds)} ms |");
            report.AppendLine($"| Stable managed allocation after warmup | {stableAllocatedBytes} bytes |");
            report.AppendLine($"| Resident panel handles after transition | {residentPanels} (current + optional next, maximum 2) |");
            report.AppendLine($"| Current decoded panel texture estimate | {FormatBytes(currentPanelBytes)} |");
            report.AppendLine($"| Referenced temporary voice clips | {audioClipCount} / {FormatBytes(audioBytes)} runtime memory |");
            report.AppendLine();
            report.AppendLine("## Failure And Route Checks");
            report.AppendLine();
            report.AppendLine("- Missing optional voice reaches auto-advance without blocking.");
            report.AppendLine("- Runtime narrative assemblies contain no network TTS, HTTP, or Resources loading path.");
            report.AppendLine("- Development reviewer controls are hidden, non-interactable, and non-raycasting by default.");
            report.AppendLine("- Current/next panel Addressables residency remained between one and two handles across every opening transition.");
            report.AppendLine();
            report.AppendLine("## Scope");
            report.AppendLine();
            report.AppendLine("These are deterministic Editor measurements on the development Mac. They catch recurring managed allocations and residency regressions, but do not replace Android device frame-time, GPU-memory, thermal, and audio-start profiling.");
            File.WriteAllText(ReportPath, report.ToString());
        }

        private static string Format(double value) => value.ToString("F3", CultureInfo.InvariantCulture);
        private static string FormatBytes(long bytes) => $"{bytes / (1024d * 1024d):F2} MiB";

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing required First Launch asset: {path}", path);
            return asset;
        }
    }
}
