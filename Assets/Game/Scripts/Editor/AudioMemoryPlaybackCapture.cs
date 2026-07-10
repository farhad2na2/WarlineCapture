#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Game.Components;
    using Game.Configs;
    using Game.Runtime;
    using Game.UI.Contracts;
    using Game.UI.Runtime;
    using Game.UI.Shell.Contracts.Ecs;
    using Game.UI.Shell.Ecs;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Profiling;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Profiler = UnityEngine.Profiling.Profiler;

    public static class AudioMemoryPlaybackCapture
    {
        public const string CatalogAssetPath = "Assets/Game/Audio/Events/AudioEventCatalogConfig.asset";
        public const string MenuJsonReportPath = "Design/AgentReports/aph-401_audio-memory-playback-menu.json";
        public const string MenuMarkdownReportPath = "Design/AgentReports/aph-401_audio-memory-playback-menu.md";
        public const string MenuRawProfilerPath = "/private/tmp/warline-aph401-audio-memory-menu.raw";
        public const string MatchJsonReportPath = "Design/AgentReports/aph-401_audio-memory-playback-match.json";
        public const string MatchMarkdownReportPath = "Design/AgentReports/aph-401_audio-memory-playback-match.md";
        public const string MatchRawProfilerPath = "/private/tmp/warline-aph401-audio-memory-match.raw";

        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string MatchSceneName = "Match";
        private const string ActiveKey = "AudioMemoryPlaybackCapture.Active";
        private const string TargetKey = "AudioMemoryPlaybackCapture.Target";
        private const string StartedAtKey = "AudioMemoryPlaybackCapture.StartedAt";
        private const double TimeoutSeconds = 360d;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        private static bool s_ContinuationRunning;
        private static bool s_ProfilerStarted;
        private static double s_CaptureEpochSeconds;
        private static AudioMemoryPlaybackReport s_Report;
        private static ProfilerState s_PreviousProfilerState;

        private enum CaptureTarget
        {
            Menu = 0,
            Match = 1
        }

        private readonly struct CapturePaths
        {
            public CapturePaths(string targetName, string jsonPath, string markdownPath, string rawProfilerPath)
            {
                TargetName = targetName;
                JsonPath = jsonPath;
                MarkdownPath = markdownPath;
                RawProfilerPath = rawProfilerPath;
            }

            public string TargetName { get; }
            public string JsonPath { get; }
            public string MarkdownPath { get; }
            public string RawProfilerPath { get; }
        }

        private readonly struct EventHandle
        {
            public EventHandle(int requestId, string eventId, uint eventHash, double triggeredAtSeconds)
            {
                RequestId = requestId;
                EventId = eventId;
                EventHash = eventHash;
                TriggeredAtSeconds = triggeredAtSeconds;
            }

            public int RequestId { get; }
            public string EventId { get; }
            public uint EventHash { get; }
            public double TriggeredAtSeconds { get; }
        }

        private struct ProfilerState
        {
            public bool Enabled;
            public bool BinaryLogEnabled;
            public string LogFile;
            public bool MemoryCategoryEnabled;
            public bool AudioCategoryEnabled;
        }

        private sealed class CatalogClipDescriptor
        {
            public AudioClip Clip;
            public string AssetPath;
            public readonly HashSet<string> EventIds = new(StringComparer.Ordinal);
            public readonly HashSet<string> BusIds = new(StringComparer.Ordinal);
        }

        [InitializeOnLoadMethod]
        private static void ResumeActiveCapture()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            EditorApplication.delayCall += ResumeAfterDomainReload;
        }

        private static async void ResumeAfterDomainReload()
        {
            await ContinueActiveCaptureAsync();
        }

        public static async void RunMenu()
        {
            await StartCaptureAsync(CaptureTarget.Menu);
        }

        public static async void RunMatch()
        {
            await StartCaptureAsync(CaptureTarget.Match);
        }

        public static AudioMemoryPhaseSnapshot CreateSnapshot(
            string phase,
            double elapsedSeconds,
            long totalAllocatedMemoryBytes,
            long totalReservedMemoryBytes,
            long monoUsedMemoryBytes,
            long monoHeapMemoryBytes,
            int? sourcePoolSize,
            int? activeSourceCount,
            AudioMemoryEventSnapshot eventSnapshot,
            IEnumerable<AudioMemoryCatalogClipSnapshot> catalogClips)
        {
            if (string.IsNullOrWhiteSpace(phase))
                throw new ArgumentException("Snapshot phase is required.", nameof(phase));
            if (eventSnapshot == null)
                throw new ArgumentNullException(nameof(eventSnapshot));
            if (catalogClips == null)
                throw new ArgumentNullException(nameof(catalogClips));

            List<AudioMemoryCatalogClipSnapshot> normalizedClips = catalogClips
                .Select(NormalizeClip)
                .OrderBy(clip => clip.AssetPath, StringComparer.Ordinal)
                .ToList();

            for (int i = 1; i < normalizedClips.Count; i++)
            {
                if (string.Equals(normalizedClips[i - 1].AssetPath, normalizedClips[i].AssetPath, StringComparison.Ordinal))
                    throw new ArgumentException($"Duplicate catalog clip path: {normalizedClips[i].AssetPath}", nameof(catalogClips));
            }

            Dictionary<string, AudioMemoryBusSnapshot> busTotals = new(StringComparer.Ordinal);
            long catalogRuntimeMemoryBytes = 0;
            int loadedClipCount = 0;
            for (int i = 0; i < normalizedClips.Count; i++)
            {
                AudioMemoryCatalogClipSnapshot clip = normalizedClips[i];
                catalogRuntimeMemoryBytes += clip.RuntimeMemoryBytes;
                bool loaded = string.Equals(clip.LoadState, AudioDataLoadState.Loaded.ToString(), StringComparison.Ordinal);
                if (loaded)
                    loadedClipCount++;

                for (int busIndex = 0; busIndex < clip.BusIds.Count; busIndex++)
                {
                    string busId = clip.BusIds[busIndex];
                    if (!busTotals.TryGetValue(busId, out AudioMemoryBusSnapshot total))
                    {
                        total = new AudioMemoryBusSnapshot { BusId = busId };
                        busTotals.Add(busId, total);
                    }

                    total.RuntimeMemoryBytes += clip.RuntimeMemoryBytes;
                    total.ClipCount++;
                    if (loaded)
                        total.LoadedClipCount++;
                }
            }

            return new AudioMemoryPhaseSnapshot
            {
                Phase = phase,
                ElapsedSeconds = elapsedSeconds,
                TotalAllocatedMemoryBytes = totalAllocatedMemoryBytes,
                TotalReservedMemoryBytes = totalReservedMemoryBytes,
                MonoUsedMemoryBytes = monoUsedMemoryBytes,
                MonoHeapMemoryBytes = monoHeapMemoryBytes,
                SourcePoolSize = sourcePoolSize,
                ActiveSourceCount = activeSourceCount,
                CatalogRuntimeMemoryBytes = catalogRuntimeMemoryBytes,
                CatalogClipCount = normalizedClips.Count,
                LoadedCatalogClipCount = loadedClipCount,
                Event = NormalizeEvent(eventSnapshot),
                BusTotals = busTotals.Values.OrderBy(total => total.BusId, StringComparer.Ordinal).ToList(),
                CatalogClips = normalizedClips
            };
        }

        public static string SerializeReport(AudioMemoryPlaybackReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            return JsonConvert.SerializeObject(report, JsonSettings);
        }

        public static string BuildMarkdown(AudioMemoryPlaybackReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            StringBuilder builder = new(32768);
            AppendLine(builder, "# APH-401 Audio Memory Playback Capture");
            AppendLine(builder, string.Empty);
            AppendLine(builder, $"- Task: `{EscapeMarkdown(report.TaskId)}`");
            AppendLine(builder, $"- Capture target: `{EscapeMarkdown(report.CaptureTarget)}`");
            AppendLine(builder, $"- Capture result: `{EscapeMarkdown(report.CaptureResult)}`");
            AppendLine(builder, $"- Unity: `{EscapeMarkdown(report.UnityVersion)}`");
            AppendLine(builder, $"- JSON: `{EscapeMarkdown(report.JsonReportPath)}`");
            AppendLine(builder, $"- Markdown: `{EscapeMarkdown(report.MarkdownReportPath)}`");
            AppendLine(builder, $"- Raw profiler: `{EscapeMarkdown(report.RawProfilerPath)}`");
            if (!string.IsNullOrWhiteSpace(report.Failure))
                AppendLine(builder, $"- Failure: `{EscapeMarkdown(report.Failure)}`");

            AppendLine(builder, string.Empty);
            AppendLine(builder, "## Phase Summary");
            AppendLine(builder, string.Empty);
            AppendLine(builder, "| Phase | Time (s) | Event ID | Hash | Status | Catalog bytes | Allocated | Reserved | Mono used | Mono heap | Pool | Active |");
            AppendLine(builder, "|---|---:|---|---:|---|---:|---:|---:|---:|---:|---:|---:|");
            for (int i = 0; i < report.Snapshots.Count; i++)
            {
                AudioMemoryPhaseSnapshot snapshot = report.Snapshots[i];
                AppendLine(
                    builder,
                    $"| {EscapeMarkdown(snapshot.Phase)} | {FormatSeconds(snapshot.ElapsedSeconds)} | " +
                    $"{EscapeMarkdown(DisplayEventId(snapshot.Event.EventId))} | {snapshot.Event.EventHash.ToString(CultureInfo.InvariantCulture)} | " +
                    $"{EscapeMarkdown(snapshot.Event.Status)} | {FormatBytes(snapshot.CatalogRuntimeMemoryBytes)} | " +
                    $"{FormatBytes(snapshot.TotalAllocatedMemoryBytes)} | {FormatBytes(snapshot.TotalReservedMemoryBytes)} | " +
                    $"{FormatBytes(snapshot.MonoUsedMemoryBytes)} | {FormatBytes(snapshot.MonoHeapMemoryBytes)} | " +
                    $"{FormatNullableInt(snapshot.SourcePoolSize)} | {FormatNullableInt(snapshot.ActiveSourceCount)} |");
            }

            for (int i = 0; i < report.Snapshots.Count; i++)
            {
                AudioMemoryPhaseSnapshot snapshot = report.Snapshots[i];
                AppendLine(builder, string.Empty);
                AppendLine(builder, $"## {EscapeMarkdown(snapshot.Phase)}");
                AppendLine(builder, string.Empty);
                AppendLine(builder, $"- Snapshot time: `{FormatSeconds(snapshot.ElapsedSeconds)} s`");
                AppendLine(builder, $"- Event: `{EscapeMarkdown(DisplayEventId(snapshot.Event.EventId))}`");
                AppendLine(builder, $"- Event hash: `{snapshot.Event.EventHash.ToString(CultureInfo.InvariantCulture)}`");
                AppendLine(builder, $"- Event status: `{EscapeMarkdown(snapshot.Event.Status)}`");
                AppendLine(builder, $"- Triggered at: `{FormatNullableSeconds(snapshot.Event.TriggeredAtSeconds)}`");
                AppendLine(builder, $"- Requested at: `{FormatNullableSeconds(snapshot.Event.RequestedAtSeconds)}`");
                AppendLine(builder, $"- Processed at: `{FormatNullableSeconds(snapshot.Event.ProcessedAtSeconds)}`");
                AppendLine(builder, $"- Observed at: `{FormatNullableSeconds(snapshot.Event.ObservedAtSeconds)}`");
                AppendLine(builder, $"- Catalog clips: `{snapshot.CatalogClipCount.ToString(CultureInfo.InvariantCulture)}`");
                AppendLine(builder, $"- Loaded catalog clips: `{snapshot.LoadedCatalogClipCount.ToString(CultureInfo.InvariantCulture)}`");
                AppendLine(builder, $"- Catalog runtime memory: `{FormatBytes(snapshot.CatalogRuntimeMemoryBytes)} bytes`");
                AppendLine(builder, $"- Total allocated memory: `{FormatBytes(snapshot.TotalAllocatedMemoryBytes)} bytes`");
                AppendLine(builder, $"- Total reserved memory: `{FormatBytes(snapshot.TotalReservedMemoryBytes)} bytes`");
                AppendLine(builder, $"- Mono used memory: `{FormatBytes(snapshot.MonoUsedMemoryBytes)} bytes`");
                AppendLine(builder, $"- Mono heap memory: `{FormatBytes(snapshot.MonoHeapMemoryBytes)} bytes`");
                AppendLine(builder, $"- Source pool: `{FormatNullableInt(snapshot.SourcePoolSize)}`");
                AppendLine(builder, $"- Active sources: `{FormatNullableInt(snapshot.ActiveSourceCount)}`");
                AppendLine(builder, string.Empty);
                AppendLine(builder, "### Bus Totals");
                AppendLine(builder, string.Empty);
                AppendLine(builder, "| Bus | Runtime bytes | Clips | Loaded clips |");
                AppendLine(builder, "|---|---:|---:|---:|");
                for (int busIndex = 0; busIndex < snapshot.BusTotals.Count; busIndex++)
                {
                    AudioMemoryBusSnapshot total = snapshot.BusTotals[busIndex];
                    AppendLine(
                        builder,
                        $"| {EscapeMarkdown(total.BusId)} | {FormatBytes(total.RuntimeMemoryBytes)} | " +
                        $"{total.ClipCount.ToString(CultureInfo.InvariantCulture)} | " +
                        $"{total.LoadedClipCount.ToString(CultureInfo.InvariantCulture)} |");
                }

                AppendLine(builder, string.Empty);
                AppendLine(builder, "### Catalog Clip Runtime State");
                AppendLine(builder, string.Empty);
                AppendLine(builder, "| Asset | Buses | Events | Load state | Runtime bytes |");
                AppendLine(builder, "|---|---|---|---|---:|");
                for (int clipIndex = 0; clipIndex < snapshot.CatalogClips.Count; clipIndex++)
                {
                    AudioMemoryCatalogClipSnapshot clip = snapshot.CatalogClips[clipIndex];
                    AppendLine(
                        builder,
                        $"| {EscapeMarkdown(clip.AssetPath)} | {EscapeMarkdown(string.Join(", ", clip.BusIds))} | " +
                        $"{EscapeMarkdown(string.Join(", ", clip.EventIds))} | {EscapeMarkdown(clip.LoadState)} | " +
                        $"{FormatBytes(clip.RuntimeMemoryBytes)} |");
                }
            }

            return builder.ToString();
        }

        private static async Task StartCaptureAsync(CaptureTarget target)
        {
            if (SessionState.GetBool(ActiveKey, false))
            {
                Debug.LogError("[AudioMemoryPlaybackCapture] another capture is already active.");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Start APH-401 capture from Edit Mode.");

                s_Report = null;
                s_CaptureEpochSeconds = 0d;
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(TargetKey, (int)target);
                SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
                await ContinueActiveCaptureAsync();
            }
            catch (Exception exception)
            {
                FailCapture(exception);
            }
        }

        private static async Task ContinueActiveCaptureAsync()
        {
            if (s_ContinuationRunning || !SessionState.GetBool(ActiveKey, false))
                return;

            s_ContinuationRunning = true;
            try
            {
                await WaitUntilAsync(() => EditorApplication.isPlaying, "entering Play Mode");
                await WaitUntilAsync(IsMenuReady, "Menu shell and audio runtime readiness");

                CaptureTarget target = (CaptureTarget)SessionState.GetInt(TargetKey, (int)CaptureTarget.Menu);
                CapturePaths paths = GetPaths(target);
                StartRawProfilerCapture(paths.RawProfilerPath);
                s_Report = CreateReport(paths);

                if (target == CaptureTarget.Menu)
                    await CaptureMenuAsync();
                else
                    await CaptureMatchAsync();

                CompleteCapture();
            }
            catch (Exception exception)
            {
                FailCapture(exception);
            }
            finally
            {
                s_ContinuationRunning = false;
            }
        }

        private static async Task CaptureMenuAsync()
        {
            await WaitEditorUpdatesAsync(2);
            AddSnapshot("menu-before-controlled-playback", CreateNoEventSnapshot());

            EventHandle uiClick = EnqueueUiEvent(UIAudioEventKind.ButtonPrimaryClick);
            AddSnapshot("menu-after-ui-primary-click", await WaitForTerminalEventAsync(uiClick));

            EventHandle menuMusic = EnqueueMusic(
                AudioEventIds.MusicMenuLoop,
                AudioEventIds.MusicMenuLoopHash,
                transitionSeconds: 1.5f);
            AddSnapshot("menu-after-music-loop", await WaitForTerminalEventAsync(menuMusic));
        }

        private static async Task CaptureMatchAsync()
        {
            EnqueueMatchRoute();
            await WaitUntilAsync(IsMatchReady, "Menu-to-Match transition readiness");
            await WaitEditorUpdatesAsync(2);
            AddSnapshot("match-before-controlled-playback", CreateNoEventSnapshot());

            EventHandle smallArms = EnqueueOneShot(
                AudioEventIds.GameplayWeaponFireSmallArms,
                AudioEventIds.GameplayWeaponFireSmallArmsHash,
                "SFX",
                AudioPlaybackPriority.Medium);
            AddSnapshot("match-after-small-arms", await WaitForTerminalEventAsync(smallArms));

            EventHandle matchMusic = EnqueueMusic(
                AudioEventIds.MusicMatchCalmLoop,
                AudioEventIds.MusicMatchCalmLoopHash,
                transitionSeconds: 2f);
            AddSnapshot("match-after-music-calm-loop", await WaitForTerminalEventAsync(matchMusic));

            EventHandle ambience = EnqueueOneShot(
                AudioEventIds.AmbienceCityDayLoop,
                AudioEventIds.AmbienceCityDayLoopHash,
                "Ambience",
                AudioPlaybackPriority.Low);
            AddSnapshot("match-after-city-day-ambience", await WaitForTerminalEventAsync(ambience));

            EventHandle voice = EnqueueUiEvent(UIAudioEventKind.SettingsVoiceSample);
            AddSnapshot("match-after-aria-settings-voice", await WaitForTerminalEventAsync(voice));
        }

        private static void AddSnapshot(string phase, AudioMemoryEventSnapshot eventSnapshot)
        {
            AudioPlaybackPresentationRuntimeView audioRuntime = FindAudioRuntime();
            s_Report.Snapshots.Add(CreateSnapshot(
                phase,
                ElapsedSeconds(),
                Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong(),
                Profiler.GetMonoUsedSizeLong(),
                Profiler.GetMonoHeapSizeLong(),
                audioRuntime != null ? audioRuntime.PoolSize : null,
                audioRuntime != null ? audioRuntime.ActiveSourceCount : null,
                eventSnapshot,
                CaptureCatalogClips()));
        }

        private static List<AudioMemoryCatalogClipSnapshot> CaptureCatalogClips()
        {
            AudioEventCatalogConfig catalog = AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>(CatalogAssetPath);
            if (catalog == null)
                throw new InvalidOperationException($"Missing audio event catalog at {CatalogAssetPath}.");

            Dictionary<string, CatalogClipDescriptor> clipsByPath = new(StringComparer.Ordinal);
            IReadOnlyList<AudioEventCatalogEntry> events = catalog.Events;
            for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                AudioEventCatalogEntry entry = events[eventIndex];
                if (entry == null)
                    continue;

                IReadOnlyList<AudioClipWeightEntry> clips = entry.Clips;
                for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
                {
                    AudioClip clip = clips[clipIndex]?.Clip;
                    if (clip == null)
                        continue;

                    string assetPath = AssetDatabase.GetAssetPath(clip);
                    if (string.IsNullOrWhiteSpace(assetPath))
                        assetPath = clip.name;

                    if (!clipsByPath.TryGetValue(assetPath, out CatalogClipDescriptor descriptor))
                    {
                        descriptor = new CatalogClipDescriptor
                        {
                            Clip = clip,
                            AssetPath = assetPath
                        };
                        clipsByPath.Add(assetPath, descriptor);
                    }

                    if (!string.IsNullOrWhiteSpace(entry.EventId))
                        descriptor.EventIds.Add(entry.EventId);
                    if (!string.IsNullOrWhiteSpace(entry.BusId))
                        descriptor.BusIds.Add(entry.BusId);
                }
            }

            List<AudioMemoryCatalogClipSnapshot> snapshots = new(clipsByPath.Count);
            foreach (CatalogClipDescriptor descriptor in clipsByPath.Values)
            {
                snapshots.Add(new AudioMemoryCatalogClipSnapshot
                {
                    AssetPath = descriptor.AssetPath,
                    EventIds = descriptor.EventIds.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                    BusIds = descriptor.BusIds.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                    LoadState = descriptor.Clip.loadState.ToString(),
                    RuntimeMemoryBytes = Profiler.GetRuntimeMemorySizeLong(descriptor.Clip)
                });
            }

            return snapshots;
        }

        private static EventHandle EnqueueUiEvent(UIAudioEventKind kind)
        {
            if (!UIAudioEventGateway.TryCreateRequest(kind, out UIAudioEventRequest request))
                throw new InvalidOperationException($"Unable to create UI audio request for {kind}.");

            World world = RequireWorld();
            EntityManager entityManager = world.EntityManager;
            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(entityManager);
            int previousRequestId = entityManager.GetComponentData<AudioPlaybackRequestQueueComponent>(audioEntity).LastRequestId;
            double triggeredAt = ElapsedSeconds();
            if (!UiAudioEventBridgeSystem.TryEnqueue(world, request))
                throw new InvalidOperationException($"Unable to enqueue UI audio request for {kind}.");

            int requestId = entityManager.GetComponentData<AudioPlaybackRequestQueueComponent>(audioEntity).LastRequestId;
            if (requestId <= previousRequestId)
                throw new InvalidOperationException($"UI audio request for {kind} did not advance the request queue.");

            return new EventHandle(requestId, request.EventId, request.EventHash, triggeredAt);
        }

        private static EventHandle EnqueueOneShot(
            string eventId,
            uint eventHash,
            string busId,
            AudioPlaybackPriority priority)
        {
            EntityManager entityManager = RequireWorld().EntityManager;
            double triggeredAt = ElapsedSeconds();
            int requestId = AudioEventRequestSystem.EnqueueOneShot(
                entityManager,
                new FixedString64Bytes(eventId),
                eventHash,
                new FixedString32Bytes(busId),
                priority,
                requestedAt: Time.unscaledTime,
                cooldownSeconds: 0f);
            return new EventHandle(requestId, eventId, eventHash, triggeredAt);
        }

        private static EventHandle EnqueueMusic(string eventId, uint eventHash, float transitionSeconds)
        {
            EntityManager entityManager = RequireWorld().EntityManager;
            double triggeredAt = ElapsedSeconds();
            int requestId = AudioEventRequestSystem.EnqueueMusicState(
                entityManager,
                new FixedString64Bytes(eventId),
                eventHash,
                requestedAt: Time.unscaledTime,
                transitionSeconds: transitionSeconds,
                loop: true);
            return new EventHandle(requestId, eventId, eventHash, triggeredAt);
        }

        private static async Task<AudioMemoryEventSnapshot> WaitForTerminalEventAsync(EventHandle handle)
        {
            AudioMemoryEventSnapshot observation = null;
            await WaitUntilAsync(
                () => TryReadEvent(handle, out observation) && IsTerminalStatus(observation.Status),
                $"terminal playback result for {handle.EventId}");
            observation.ObservedAtSeconds = ElapsedSeconds();
            if (!string.Equals(
                    observation.Status,
                    AudioPlaybackRequestStatus.Presented.ToString(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Audio event {handle.EventId} ended with {observation.Status}; representative playback was not presented.");
            }

            return observation;
        }

        private static bool TryReadEvent(EventHandle handle, out AudioMemoryEventSnapshot observation)
        {
            observation = null;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(entityManager);
            DynamicBuffer<AudioPlaybackRequestElement> requests = entityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
            AudioPlaybackRequestElement request = default;
            bool found = false;
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].RequestId != handle.RequestId)
                    continue;

                request = requests[i];
                found = true;
                break;
            }

            if (!found)
                return false;

            double? processedAt = null;
            DynamicBuffer<AudioPlaybackResultElement> results = entityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity);
            for (int i = results.Length - 1; i >= 0; i--)
            {
                if (results[i].RequestId != handle.RequestId)
                    continue;

                processedAt = results[i].ProcessedAt;
                break;
            }

            observation = new AudioMemoryEventSnapshot
            {
                RequestId = handle.RequestId,
                EventId = request.EventId.Length > 0 ? request.EventId.ToString() : handle.EventId,
                EventHash = request.EventHash != 0u ? request.EventHash : handle.EventHash,
                Status = request.Status.ToString(),
                TriggeredAtSeconds = handle.TriggeredAtSeconds,
                RequestedAtSeconds = request.RequestedAt,
                ProcessedAtSeconds = processedAt
            };
            return true;
        }

        private static AudioMemoryEventSnapshot CreateNoEventSnapshot()
        {
            double observedAt = ElapsedSeconds();
            return new AudioMemoryEventSnapshot
            {
                RequestId = 0,
                EventId = string.Empty,
                EventHash = 0u,
                Status = "NotRequested",
                ObservedAtSeconds = observedAt
            };
        }

        private static bool IsTerminalStatus(string status)
        {
            return !string.Equals(status, AudioPlaybackRequestStatus.Pending.ToString(), StringComparison.Ordinal) &&
                   !string.Equals(status, AudioPlaybackRequestStatus.Accepted.ToString(), StringComparison.Ordinal);
        }

        private static void EnqueueMatchRoute()
        {
            World world = RequireWorld();
            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            if (query.CalculateEntityCount() != 1)
                throw new InvalidOperationException("UI shell route boundary is unavailable.");

            DynamicBuffer<UiShellRouteRequestComponent> requests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(query.GetSingletonEntity());
            requests.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
        }

        private static bool IsMenuReady()
        {
            return TryGetShellState(out UiShellStateComponent shellState) &&
                   shellState.CurrentMode == UiShellMode.MainMenu &&
                   shellState.ActiveRoute == UIRoute.MainMenu &&
                   shellState.IsTransitionRunning == 0 &&
                   FindAudioRuntime() != null;
        }

        private static bool IsMatchReady()
        {
            if (!TryGetShellState(out UiShellStateComponent shellState) ||
                shellState.CurrentMode != UiShellMode.MatchHud ||
                shellState.ActiveRoute != UIRoute.Match ||
                shellState.IsTransitionRunning != 0)
            {
                return false;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery runtimeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (runtimeQuery.CalculateEntityCount() != 1)
                return false;
            RuntimeGameplayStateComponent runtimeState = runtimeQuery.GetSingleton<RuntimeGameplayStateComponent>();
            if (runtimeState.PlayRequested == 0 || runtimeState.SimulationActive == 0)
                return false;

            using EntityQuery introQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<MatchIntroTransitionComponent>());
            if (introQuery.CalculateEntityCount() != 1)
                return false;
            MatchIntroTransitionComponent intro = introQuery.GetSingleton<MatchIntroTransitionComponent>();
            if (intro.State != MatchIntroTransitionStateKind.Complete || intro.InputLocked != 0)
                return false;

            Scene matchScene = SceneManager.GetSceneByName(MatchSceneName);
            return matchScene.IsValid() && matchScene.isLoaded && FindAudioRuntime() != null;
        }

        private static bool TryGetShellState(out UiShellStateComponent shellState)
        {
            shellState = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<UiShellStateComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            shellState = entityManager.GetComponentData<UiShellStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static AudioPlaybackPresentationRuntimeView FindAudioRuntime()
        {
            return UnityEngine.Object.FindAnyObjectByType<AudioPlaybackPresentationRuntimeView>(FindObjectsInactive.Include);
        }

        private static World RequireWorld()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                throw new InvalidOperationException("Default ECS world is unavailable.");
            return world;
        }

        private static async Task WaitUntilAsync(Func<bool> predicate, string description)
        {
            while (!predicate())
            {
                if (!SessionState.GetBool(ActiveKey, false))
                    throw new OperationCanceledException("APH-401 capture was cancelled.");

                double startedAt = SessionState.GetFloat(StartedAtKey, 0f);
                if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
                    throw new TimeoutException($"Timed out while waiting for {description}.");

                await NextEditorUpdateAsync();
            }
        }

        private static async Task WaitEditorUpdatesAsync(int count)
        {
            for (int i = 0; i < count; i++)
                await NextEditorUpdateAsync();
        }

        private static Task NextEditorUpdateAsync()
        {
            TaskCompletionSource<bool> completion = new();
            void Complete()
            {
                EditorApplication.update -= Complete;
                completion.TrySetResult(true);
            }

            EditorApplication.update += Complete;
            return completion.Task;
        }

        private static void StartRawProfilerCapture(string rawProfilerPath)
        {
            string absoluteRawPath = Path.GetFullPath(rawProfilerPath);
            string directory = Path.GetDirectoryName(absoluteRawPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException($"Invalid raw profiler path: {rawProfilerPath}");

            Directory.CreateDirectory(directory);
            if (File.Exists(absoluteRawPath))
                File.Delete(absoluteRawPath);

            s_PreviousProfilerState = new ProfilerState
            {
                Enabled = Profiler.enabled,
                BinaryLogEnabled = Profiler.enableBinaryLog,
                LogFile = Profiler.logFile ?? string.Empty,
                MemoryCategoryEnabled = Profiler.IsCategoryEnabled(ProfilerCategory.Memory),
                AudioCategoryEnabled = Profiler.IsCategoryEnabled(ProfilerCategory.Audio)
            };

            Profiler.enabled = false;
            ProfilerDriver.ClearAllFrames();
            Profiler.SetCategoryEnabled(ProfilerCategory.Memory, true);
            Profiler.SetCategoryEnabled(ProfilerCategory.Audio, true);
            Profiler.logFile = Path.Combine(directory, Path.GetFileNameWithoutExtension(absoluteRawPath));
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            s_ProfilerStarted = true;
            s_CaptureEpochSeconds = Time.realtimeSinceStartupAsDouble;
        }

        private static void StopRawProfilerCapture()
        {
            if (!s_ProfilerStarted)
                return;

            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = string.Empty;
            Profiler.SetCategoryEnabled(ProfilerCategory.Memory, s_PreviousProfilerState.MemoryCategoryEnabled);
            Profiler.SetCategoryEnabled(ProfilerCategory.Audio, s_PreviousProfilerState.AudioCategoryEnabled);
            Profiler.logFile = s_PreviousProfilerState.LogFile;
            Profiler.enableBinaryLog = s_PreviousProfilerState.BinaryLogEnabled;
            Profiler.enabled = s_PreviousProfilerState.Enabled;
            s_ProfilerStarted = false;
        }

        private static AudioMemoryPlaybackReport CreateReport(CapturePaths paths)
        {
            return new AudioMemoryPlaybackReport
            {
                CaptureTarget = paths.TargetName,
                CaptureResult = "Running",
                UnityVersion = Application.unityVersion,
                JsonReportPath = paths.JsonPath,
                MarkdownReportPath = paths.MarkdownPath,
                RawProfilerPath = paths.RawProfilerPath
            };
        }

        private static CapturePaths GetPaths(CaptureTarget target)
        {
            return target == CaptureTarget.Menu
                ? new CapturePaths("Menu", MenuJsonReportPath, MenuMarkdownReportPath, MenuRawProfilerPath)
                : new CapturePaths("Match", MatchJsonReportPath, MatchMarkdownReportPath, MatchRawProfilerPath);
        }

        private static void CompleteCapture()
        {
            StopRawProfilerCapture();
            s_Report.CaptureResult = "Succeeded";
            WriteReports(s_Report);
            Debug.Log(
                $"[AudioMemoryPlaybackCapture] result=Passed target={s_Report.CaptureTarget} " +
                $"snapshots={s_Report.Snapshots.Count} json={s_Report.JsonReportPath} " +
                $"markdown={s_Report.MarkdownReportPath} raw={s_Report.RawProfilerPath}");
            EndCapture(0);
        }

        private static void FailCapture(Exception exception)
        {
            StopRawProfilerCapture();
            CaptureTarget target = (CaptureTarget)SessionState.GetInt(TargetKey, (int)CaptureTarget.Menu);
            if (s_Report == null)
                s_Report = CreateReport(GetPaths(target));

            s_Report.CaptureResult = "Failed";
            s_Report.Failure = exception.Message;
            try
            {
                WriteReports(s_Report);
            }
            catch (Exception reportException)
            {
                Debug.LogException(reportException);
            }

            Debug.LogException(exception);
            Debug.LogError($"[AudioMemoryPlaybackCapture] result=Failed target={s_Report.CaptureTarget}");
            EndCapture(1);
        }

        private static void EndCapture(int exitCode)
        {
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseInt(TargetKey);
            SessionState.EraseFloat(StartedAtKey);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
                return;
            }

            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        private static void WriteReports(AudioMemoryPlaybackReport report)
        {
            string jsonDirectory = Path.GetDirectoryName(report.JsonReportPath);
            if (!string.IsNullOrWhiteSpace(jsonDirectory))
                Directory.CreateDirectory(jsonDirectory);

            File.WriteAllText(report.JsonReportPath, SerializeReport(report) + "\n", new UTF8Encoding(false));
            File.WriteAllText(report.MarkdownReportPath, BuildMarkdown(report), new UTF8Encoding(false));
        }

        private static double ElapsedSeconds()
        {
            return Math.Max(0d, Time.realtimeSinceStartupAsDouble - s_CaptureEpochSeconds);
        }

        private static AudioMemoryCatalogClipSnapshot NormalizeClip(AudioMemoryCatalogClipSnapshot source)
        {
            if (source == null)
                throw new ArgumentException("Catalog clips cannot contain null entries.", nameof(source));
            if (string.IsNullOrWhiteSpace(source.AssetPath))
                throw new ArgumentException("Catalog clip asset path is required.", nameof(source));

            return new AudioMemoryCatalogClipSnapshot
            {
                AssetPath = source.AssetPath,
                EventIds = NormalizeStrings(source.EventIds),
                BusIds = NormalizeStrings(source.BusIds),
                LoadState = source.LoadState ?? string.Empty,
                RuntimeMemoryBytes = source.RuntimeMemoryBytes
            };
        }

        private static AudioMemoryEventSnapshot NormalizeEvent(AudioMemoryEventSnapshot source)
        {
            return new AudioMemoryEventSnapshot
            {
                RequestId = source.RequestId,
                EventId = source.EventId ?? string.Empty,
                EventHash = source.EventHash,
                Status = source.Status ?? string.Empty,
                TriggeredAtSeconds = source.TriggeredAtSeconds,
                RequestedAtSeconds = source.RequestedAtSeconds,
                ProcessedAtSeconds = source.ProcessedAtSeconds,
                ObservedAtSeconds = source.ObservedAtSeconds
            };
        }

        private static List<string> NormalizeStrings(IEnumerable<string> values)
        {
            return (values ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
        }

        private static void AppendLine(StringBuilder builder, string value)
        {
            builder.Append(value).Append('\n');
        }

        private static string EscapeMarkdown(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Replace("`", "'");
        }

        private static string DisplayEventId(string eventId)
        {
            return string.IsNullOrWhiteSpace(eventId) ? "None" : eventId;
        }

        private static string FormatBytes(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string FormatSeconds(double value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private static string FormatNullableSeconds(double? value)
        {
            return value.HasValue ? $"{FormatSeconds(value.Value)} s" : "Unavailable";
        }

        private static string FormatNullableInt(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "Unavailable";
        }
    }

    [Serializable]
    public sealed class AudioMemoryPlaybackReport
    {
        public string TaskId { get; set; } = "APH-401";
        public string CaptureTarget { get; set; } = string.Empty;
        public string CaptureResult { get; set; } = string.Empty;
        public string Failure { get; set; }
        public string UnityVersion { get; set; } = string.Empty;
        public string JsonReportPath { get; set; } = string.Empty;
        public string MarkdownReportPath { get; set; } = string.Empty;
        public string RawProfilerPath { get; set; } = string.Empty;
        public List<AudioMemoryPhaseSnapshot> Snapshots { get; set; } = new();
    }

    [Serializable]
    public sealed class AudioMemoryPhaseSnapshot
    {
        public string Phase { get; set; } = string.Empty;
        public double ElapsedSeconds { get; set; }
        public long TotalAllocatedMemoryBytes { get; set; }
        public long TotalReservedMemoryBytes { get; set; }
        public long MonoUsedMemoryBytes { get; set; }
        public long MonoHeapMemoryBytes { get; set; }
        public int? SourcePoolSize { get; set; }
        public int? ActiveSourceCount { get; set; }
        public long CatalogRuntimeMemoryBytes { get; set; }
        public int CatalogClipCount { get; set; }
        public int LoadedCatalogClipCount { get; set; }
        public AudioMemoryEventSnapshot Event { get; set; } = new();
        public List<AudioMemoryBusSnapshot> BusTotals { get; set; } = new();
        public List<AudioMemoryCatalogClipSnapshot> CatalogClips { get; set; } = new();
    }

    [Serializable]
    public sealed class AudioMemoryEventSnapshot
    {
        public int RequestId { get; set; }
        public string EventId { get; set; } = string.Empty;
        public uint EventHash { get; set; }
        public string Status { get; set; } = string.Empty;
        public double? TriggeredAtSeconds { get; set; }
        public double? RequestedAtSeconds { get; set; }
        public double? ProcessedAtSeconds { get; set; }
        public double? ObservedAtSeconds { get; set; }
    }

    [Serializable]
    public sealed class AudioMemoryBusSnapshot
    {
        public string BusId { get; set; } = string.Empty;
        public long RuntimeMemoryBytes { get; set; }
        public int ClipCount { get; set; }
        public int LoadedClipCount { get; set; }
    }

    [Serializable]
    public sealed class AudioMemoryCatalogClipSnapshot
    {
        public string AssetPath { get; set; } = string.Empty;
        public List<string> EventIds { get; set; } = new();
        public List<string> BusIds { get; set; } = new();
        public string LoadState { get; set; } = string.Empty;
        public long RuntimeMemoryBytes { get; set; }
    }
}

#endif
