using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using Game.Components;
using Game.Rendering;

namespace Game.Runtime
{
    public sealed partial class PerformanceDiagnosticsSystemHelper
    {
        private const string EnableProfilerMarkerDiagnosticsEnvironmentVariable = "WARLINE_ENABLE_PROFILER_MARKERS";
        private const string EnableProfilerMarkerDiagnosticsCommandLineArg = "-warlineProfilerMarkers";
        private const double FreezeLogThresholdSeconds = 0.15d;
        private const double LowFpsDiagThreshold = 55d;
        private const double FrameRateDiagIntervalSeconds = 2d;
        private const double SlowFrameDiagThresholdSeconds = 0.025d;
        private const double SlowFrameDiagCooldownSeconds = 0.5d;
        private const double RenderSceneBreakdownDiagIntervalSeconds = 10d;
        private const int MaxAutoProfilerMarkerRecorders = 200;
        private const int MaxTopSystemProfilerMarkers = 8;
        private const int MaxLastStepSamples = 16;
        private const int MaxMainMenuMarkerRegistrationAttempts = 8;
        private static readonly string[] MainMenuDiagnosticMarkerNames =
        {
            "GameplayRuntimeUpdate.ThreatWarningPresentation",
            "GameplayRuntimeUpdate.MainMenuUi",
            "MainMenuPlayUI.MinimapUpdate"
        };
        private static readonly string[] DrawCallsProfilerCounterNames = { "Draw Calls Count", "Draw Calls", "DrawCalls Count", "DrawCalls" };
        private static readonly string[] BatchesProfilerCounterNames = { "Batches Count", "Batches" };
        private static readonly string[] SetPassCallsProfilerCounterNames = { "SetPass Calls Count", "SetPass Calls", "SetPassCalls Count", "SetPassCalls" };
        private static readonly string[] TrianglesProfilerCounterNames = { "Triangles Count", "Triangles" };
        private static readonly string[] VerticesProfilerCounterNames = { "Vertices Count", "Vertices" };
        private static readonly string[] PriorityProfilerMarkerNameFragments =
        {
            "UnitEngagementSystem",
            "UnitPathfindingSystem",
            "DynamicOccupancyRebuildSystem",
            "UnitAnimationIndexSystem",
            "UnitMove",
            "AITargetingSystem",
            "AICombatOrderSystem",
            "AISquadSystem",
            "AIBuildPlannerSystem",
            "AIProductionSystem",
            "AIEconomySystem",
            "AIFactionControlSystem",
            "UnitSurfaceTrackingSystem",
            "UnitGroundingSystem",
            "VehicleSlopeAlignmentSystem",
            "UnitGridMovementSystem",
            "UnitMoveOrderSystem",
            "UnitEngagedMovementSystem",
            "UnitIdleWanderSystem",
            "UnitLookAtTargetSystem",
            "UnitRenderBudgetSystem",
            "SimulationSystemGroup",
            "AudioEventRequestSystem",
            "AudioCooldownSystem",
            "AudioMusicStateSystem",
            "AudioSettingsSystem",
            "UnitMotionAudioSystem",
            "MissileFlightAudioSystem",
            "ResourceExchange",
            "UiShell"
        };

        private readonly bool _enableFrameRateDiagnostics = true;
        private readonly bool _enableSlowFrameDiagnostics = true;
        private readonly bool _enableProfilerMarkerDiagnostics;
        private readonly System.Text.StringBuilder _freezeLogBuilder = new(256);
        private readonly System.Text.StringBuilder _lastStepLogBuilder = new(256);
        private readonly StepSample[] _lastStepSamples = new StepSample[MaxLastStepSamples];
        private readonly List<NamedProfilerRecorder> _markerRecorders = new();
        private int _mainMenuMarkerRegistrationAttempts;
        private double _lastUpdateTimestamp;
        private double _suppressFrameGapUntilTimestamp;
        private double _nextFrameRateDiagTimestamp;
        private double _nextSlowFrameDiagTimestamp;
        private double _nextRenderSceneBreakdownDiagTimestamp;
        private double _frameRateDiagAccumulatedSeconds;
        private double _frameRateDiagUpdateAccumulatedSeconds;
        private double _frameRateDiagMaxUpdateSeconds;
        private double _frameStartTimestamp;
        private int _frameRateDiagFrames;
        private int _lastStepSampleCount;
        private bool _lastApplicationFocused;
        private bool _applicationPaused;
        private int _lastGcGen0Count;
        private int _lastGcGen1Count;
        private int _lastGcGen2Count;
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _batchesRecorder;
        private ProfilerRecorder _setPassCallsRecorder;
        private ProfilerRecorder _trianglesRecorder;
        private ProfilerRecorder _verticesRecorder;

        public PerformanceDiagnosticsSystemHelper()
        {
            _enableProfilerMarkerDiagnostics = ShouldEnableProfilerMarkerDiagnostics();
        }

        private struct StepSample
        {
            public string Name;
            public double Seconds;
        }

        private struct NamedProfilerRecorder
        {
            public string Name;
            public ProfilerRecorder Recorder;
        }

        private struct ProfilerMarkerSample
        {
            public string Name;
            public long Value;
        }

        public void Initialize()
        {
            _lastApplicationFocused = Application.isFocused;
            _lastUpdateTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
            _nextFrameRateDiagTimestamp = _lastUpdateTimestamp + FrameRateDiagIntervalSeconds;
            StartProfilerRecorders();
            InitializeAndroidPerformanceRecorder();
            CaptureGcCounts();
        }

        public void BeginUpdate(bool gameplayActive)
        {
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            bool applicationFocused = Application.isFocused;
            if (applicationFocused != _lastApplicationFocused)
            {
                _lastApplicationFocused = applicationFocused;
                _lastUpdateTimestamp = now;
                _suppressFrameGapUntilTimestamp = now + 0.5d;
            }

            bool canReportFrameGap =
                gameplayActive &&
                !PerformanceDiagnosticsCapturePolicy.SuppressLogging &&
                applicationFocused &&
                !_applicationPaused &&
                now >= _suppressFrameGapUntilTimestamp;

            if (canReportFrameGap && _lastUpdateTimestamp > 0d)
            {
                double gapSeconds = now - _lastUpdateTimestamp;
                if (gapSeconds >= FreezeLogThresholdSeconds)
                {
                    LogNoStackTrace($"[FreezeDetect] Frame gap frame={UnityEngine.Time.frameCount} Gap={FormatMilliseconds(gapSeconds)}ms GC={BuildGcDeltaString()} LastSteps={BuildLastStepLogString()}");
                }
            }

            _lastUpdateTimestamp = now;
            _frameStartTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
            _freezeLogBuilder.Clear();
            _lastStepLogBuilder.Clear();
            _lastStepSampleCount = 0;
        }

        public double BeginStep() => UnityEngine.Time.realtimeSinceStartupAsDouble;

        public bool EndStep(string name, double start)
        {
            double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
            RecordLastStepSample(name, elapsed);

            if (elapsed < FreezeLogThresholdSeconds)
                return false;

            if (_freezeLogBuilder.Length > 0)
                _freezeLogBuilder.Append(", ");

            AppendStepTiming(_freezeLogBuilder, name, elapsed);
            return true;
        }

        private void RecordLastStepSample(string name, double elapsed)
        {
            if (_lastStepSampleCount >= _lastStepSamples.Length)
                return;

            _lastStepSamples[_lastStepSampleCount++] = new StepSample
            {
                Name = name,
                Seconds = elapsed
            };
        }

        private string BuildLastStepLogString()
        {
            if (_lastStepSampleCount == 0)
                return "none";

            _lastStepLogBuilder.Clear();
            for (int i = 0; i < _lastStepSampleCount; i++)
            {
                StepSample sample = _lastStepSamples[i];
                if (_lastStepLogBuilder.Length > 0)
                    _lastStepLogBuilder.Append(", ");

                AppendStepTiming(_lastStepLogBuilder, sample.Name, sample.Seconds);
            }

            return _lastStepLogBuilder.ToString();
        }

        private static void AppendStepTiming(System.Text.StringBuilder builder, string name, double seconds)
        {
            builder.Append(name);
            builder.Append('=');
            AppendMilliseconds(builder, seconds);
            builder.Append("ms");
        }

        private static void AppendMilliseconds(System.Text.StringBuilder builder, double seconds)
        {
            int tenths = (int)Math.Round(seconds * 10000d, MidpointRounding.AwayFromZero);
            int whole = tenths / 10;
            int fraction = Math.Abs(tenths % 10);
            builder.Append(whole);
            builder.Append('.');
            builder.Append(fraction);
        }


        private static readonly bool EnableStartupMemoryCensus = ShouldEnableStartupMemoryCensus();
        private static bool _startupMemoryCensusCaptured;
        private const int StartupCensusObjectLimit = 4096;
        private const int StartupCensusTopLimit = 16;
        private const double StartupCensusScanSeconds = 0.020d;

        [Serializable]
        private sealed class StartupCensusObject
        {
            public string name, type, detail, nativeEntityId;
            public long reportedBytes;
        }

        [Serializable]
        private sealed class StartupCensusPass
        {
            public string category;
            public int enumeratedReferences, scanned, unavailable, ordinaryObjects, renderTextures;
            public long referenceSlotsBytes, ordinaryReportedBytes, renderTextureReportedBytes;
            public double enumerationMs, scanMs;
            public bool partial;
            public StartupCensusObject[] topOrdinary, topRenderTextures;
        }

        [Serializable]
        private sealed class StartupCensusScene
        {
            public string name, path, handle;
            public int rootCount;
            public bool loaded;
        }

        [Serializable]
        private sealed class StartupCensusReport
        {
            public string utc, phase, attempt, catalog, definition, worldSequence;
            public int frame, seed, contentVersion, sceneCount;
            public long unityAllocatedBytes, unityReservedBytes, unityUnusedReservedBytes, monoUsedBytes, monoHeapBytes;
            public long observerManagedBytesBeforeSerialization;
            public double observerMsBeforeSerialization;
            public bool sceneListPartial;
            public StartupCensusScene[] scenes;
            public StartupCensusPass meshes, textures;
            public string scope = "Loaded resources; scanned bytes only; counters overlap; full reference arrays allocated; ownership and camera acceptance unproved";
        }

        private static bool ShouldEnableStartupMemoryCensus()
        {
            try { return Application.isEditor && IsTruthy(Environment.GetEnvironmentVariable("WARLINE_S005_STARTUP_MEMORY_CENSUS")); }
            catch { return false; }
        }

        internal static void CaptureStartupMemoryCensus(World world, string phase)
        {
            if (!EnableStartupMemoryCensus || _startupMemoryCensusCaptured || world == null || !world.IsCreated)
                return;

            string attempt = "unavailable";
            double started = UnityEngine.Time.realtimeSinceStartupAsDouble;
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            try
            {
                using EntityQuery query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>());
                if (query.CalculateEntityCount() != 1)
                    return;
                SkirmishExpandedSessionComponent session = query.GetSingleton<SkirmishExpandedSessionComponent>();
                if (session.CatalogId.ToString() != "S005" || session.SessionId.Length == 0)
                    return;
                attempt = session.SessionId.ToString();
                _startupMemoryCensusCaptured = true;
                var report = new StartupCensusReport
                {
                    utc = DateTime.UtcNow.ToString("O"), phase = phase, attempt = attempt,
                    catalog = session.CatalogId.ToString(), definition = session.DefinitionId.ToString(),
                    worldSequence = world.SequenceNumber.ToString(), frame = UnityEngine.Time.frameCount,
                    seed = session.Seed, contentVersion = session.ContentVersion,
                    unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                    unityReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                    unityUnusedReservedBytes = Profiler.GetTotalUnusedReservedMemoryLong(),
                    monoUsedBytes = Profiler.GetMonoUsedSizeLong(), monoHeapBytes = Profiler.GetMonoHeapSizeLong(),
                    sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount
                };
                report.scenes = new StartupCensusScene[Math.Min(report.sceneCount, 32)];
                report.sceneListPartial = report.scenes.Length != report.sceneCount;
                for (int i = 0; i < report.scenes.Length; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    report.scenes[i] = new StartupCensusScene
                    {
                        name = StartupCensusLabel(scene.name, 96), path = StartupCensusLabel(scene.path, 256),
                        handle = scene.handle.GetRawData().ToString(System.Globalization.CultureInfo.InvariantCulture), loaded = scene.isLoaded, rootCount = scene.rootCount
                    };
                }
                report.meshes = CaptureStartupCensusObjects<Mesh>("Meshes");
                report.textures = CaptureStartupCensusObjects<Texture>("TexturesExceptRenderTextures");
                report.observerManagedBytesBeforeSerialization = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                report.observerMsBeforeSerialization = (UnityEngine.Time.realtimeSinceStartupAsDouble - started) * 1000d;
                LogNoStackTrace("[S005StartupMemoryCensus] " + JsonUtility.ToJson(report));
                LogNoStackTrace($"[S005StartupMemoryCensusOverhead] attempt={attempt} phase={phase} elapsedMs={(UnityEngine.Time.realtimeSinceStartupAsDouble - started) * 1000d:F1} includesReportSerializationAndLog=1");
            }
            catch (Exception error)
            {
                _startupMemoryCensusCaptured = true;
                LogNoStackTrace($"[S005StartupMemoryCensus] status=Unavailable attempt={attempt} phase={phase} errorType={error.GetType().Name} elapsedMs={(UnityEngine.Time.realtimeSinceStartupAsDouble - started) * 1000d:F1}");
            }
        }

        private static StartupCensusPass CaptureStartupCensusObjects<T>(string category) where T : UnityEngine.Object
        {
            double started = UnityEngine.Time.realtimeSinceStartupAsDouble;
            // Unity allocates this COMPLETE reference array before the scan can be capped.
            // It exposes no payload data; these references are not retained after this pass.
            T[] objects = Resources.FindObjectsOfTypeAll<T>();
            double scanStarted = UnityEngine.Time.realtimeSinceStartupAsDouble;
            var result = new StartupCensusPass
            {
                category = category, enumeratedReferences = objects.Length,
                referenceSlotsBytes = (long)objects.Length * IntPtr.Size,
                enumerationMs = (scanStarted - started) * 1000d
            };
            var ordinary = new List<StartupCensusObject>(StartupCensusTopLimit);
            var renderTextures = new List<StartupCensusObject>(StartupCensusTopLimit);
            while (result.scanned < objects.Length && result.scanned < StartupCensusObjectLimit &&
                   UnityEngine.Time.realtimeSinceStartupAsDouble - scanStarted < StartupCensusScanSeconds)
            {
                T value = objects[result.scanned++];
                try
                {
                    if (value == null) { result.unavailable++; continue; }
                    long bytes = Profiler.GetRuntimeMemorySizeLong(value);
                    bool renderTexture = value is RenderTexture;
                    if (renderTexture) { result.renderTextures++; result.renderTextureReportedBytes += bytes; }
                    else { result.ordinaryObjects++; result.ordinaryReportedBytes += bytes; }
                    List<StartupCensusObject> top = renderTexture ? renderTextures : ordinary;
                    if (top.Count == StartupCensusTopLimit && bytes <= top[top.Count - 1].reportedBytes)
                        continue;
                    string detail = value switch
                    {
                        Mesh mesh => $"vertices={mesh.vertexCount},subMeshes={mesh.subMeshCount},readable={mesh.isReadable}",
                        Texture2D texture => $"size={texture.width}x{texture.height},format={texture.format},mips={texture.mipmapCount},readable={texture.isReadable}",
                        RenderTexture texture => $"size={texture.width}x{texture.height},format={texture.graphicsFormat},depth={texture.depth},msaa={texture.antiAliasing}",
                        Texture texture => $"size={texture.width}x{texture.height},dimension={texture.dimension}",
                        _ => "unavailable"
                    };
                    var sample = new StartupCensusObject
                    {
                        name = StartupCensusLabel(value.name, 96), type = value.GetType().Name,
                        nativeEntityId = value.GetEntityId().ToString(), reportedBytes = bytes, detail = detail
                    };
                    int insert = 0;
                    while (insert < top.Count && top[insert].reportedBytes >= bytes) insert++;
                    if (top.Count == StartupCensusTopLimit) top.RemoveAt(top.Count - 1);
                    top.Insert(insert, sample);
                }
                catch (MissingReferenceException) { result.unavailable++; }
                catch (UnityException) { result.unavailable++; }
            }
            result.partial = result.scanned != objects.Length;
            result.scanMs = (UnityEngine.Time.realtimeSinceStartupAsDouble - scanStarted) * 1000d;
            result.topOrdinary = ordinary.ToArray();
            result.topRenderTextures = renderTextures.ToArray();
            return result;
        }

        private static string StartupCensusLabel(string value, int limit) =>
            value == null ? string.Empty : value.Length <= limit ? value : value.Substring(0, limit);

        private static string FormatMilliseconds(double seconds) => (seconds * 1000d).ToString("F1");

        public void EndUpdate(
            bool gameplayActive,
            bool hadSlowStep,
            int impostorCount,
            bool gameplayInitialized,
            bool playRequested,
            bool simulationActive, Camera camera)
        {
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            double totalSeconds = now - _frameStartTimestamp;
            RecordUpdateFrameStats(totalSeconds);
            SampleAndroidPerformanceRecorder(gameplayActive, camera);
            EnsureMainMenuMarkerRecorders(simulationActive);
            if (!PerformanceDiagnosticsCapturePolicy.SuppressLogging &&
                gameplayActive && (hadSlowStep || totalSeconds >= FreezeLogThresholdSeconds))
            {
                if (_freezeLogBuilder.Length > 0)
                    _freezeLogBuilder.Append(", ");

                _freezeLogBuilder.Append("GC=");
                _freezeLogBuilder.Append(BuildGcDeltaString());
                _freezeLogBuilder.Append(", ");
                _freezeLogBuilder.Append("Total=");
                AppendMilliseconds(_freezeLogBuilder, totalSeconds);
                _freezeLogBuilder.Append("ms");
                LogNoStackTrace($"[FreezeDetect] Update hitch frame={UnityEngine.Time.frameCount} {_freezeLogBuilder}");
            }

            LogSlowUpdateDiagnosticsIfNeeded(gameplayActive, totalSeconds, now, gameplayInitialized, playRequested, simulationActive);
            UpdateFrameRateDiagnostics(gameplayActive, now, impostorCount, playRequested, simulationActive, camera);
            CaptureGcCounts();
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            _lastApplicationFocused = hasFocus;
            _lastUpdateTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
            _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            _applicationPaused = pauseStatus;
            _lastUpdateTimestamp = UnityEngine.Time.realtimeSinceStartupAsDouble;
            _suppressFrameGapUntilTimestamp = _lastUpdateTimestamp + 0.5d;
        }

        public double BeginTimedSection() => UnityEngine.Time.realtimeSinceStartupAsDouble;

        public void EndLateUpdate(double start, int impostorCount)
        {
            double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
            if (!PerformanceDiagnosticsCapturePolicy.SuppressLogging && elapsed >= FreezeLogThresholdSeconds)
                LogNoStackTrace($"[FreezeDetect] LateUpdate hitch frame={UnityEngine.Time.frameCount} UnitRenderLate={FormatMilliseconds(elapsed)}ms impostors={impostorCount} GC={BuildGcDeltaString()}");
        }

        public void EndOnGui(double start)
        {
            double elapsed = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
            if (!PerformanceDiagnosticsCapturePolicy.SuppressLogging && elapsed >= FreezeLogThresholdSeconds)
                LogNoStackTrace($"[FreezeDetect] OnGUI hitch frame={UnityEngine.Time.frameCount} Total={FormatMilliseconds(elapsed)}ms GC={BuildGcDeltaString()}");
        }

        public void Dispose()
        {
            DisposeAndroidPerformanceRecorder();
            DisposeProfilerRecorders();
        }

        private void StartProfilerRecorders()
        {
            _drawCallsRecorder = StartRenderProfilerRecorder(DrawCallsProfilerCounterNames);
            _batchesRecorder = StartRenderProfilerRecorder(BatchesProfilerCounterNames);
            _setPassCallsRecorder = StartRenderProfilerRecorder(SetPassCallsProfilerCounterNames);
            _trianglesRecorder = StartRenderProfilerRecorder(TrianglesProfilerCounterNames);
            _verticesRecorder = StartRenderProfilerRecorder(VerticesProfilerCounterNames);
            if (!_enableProfilerMarkerDiagnostics)
                return;

            _mainMenuMarkerRegistrationAttempts = 0;
            AddProfilerMarkerRecorder(ProfilerCategory.Internal, "PlayerLoop");
            AddProfilerMarkerRecorder(ProfilerCategory.Internal, "EditorLoop");
            AddProfilerMarkerRecorder(ProfilerCategory.Internal, "Overhead");
            AddProfilerMarkerRecorder(ProfilerCategory.Internal, "WaitForTargetFPS");
            AddProfilerMarkerRecorder(ProfilerCategory.Render, "Camera.Render");
            AddProfilerMarkerRecorder(ProfilerCategory.Render, "RenderPipelineManager.DoRenderLoop_Internal");
            AddProfilerMarkerRecorder(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread");
            AddProfilerMarkerRecorder(ProfilerCategory.Render, "Gfx.PresentFrame");
            AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "BehaviourUpdate");
            AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "LateBehaviourUpdate");
            AddProfilerMarkerRecorder(ProfilerCategory.Scripts, "Canvas.SendWillRenderCanvases");
            AddPriorityPlayerLoopMarkerRecorders();
            AddAvailablePlayerLoopMarkerRecorders();
        }

        private static bool ShouldEnableProfilerMarkerDiagnostics()
        {
            try
            {
                string environmentValue = Environment.GetEnvironmentVariable(EnableProfilerMarkerDiagnosticsEnvironmentVariable);
                if (IsTruthy(environmentValue))
                    return true;

                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], EnableProfilerMarkerDiagnosticsCommandLineArg, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // Diagnostics configuration must never make runtime startup fail.
            }

            return false;
        }

        private static bool IsTruthy(string value) =>
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);

        private ProfilerRecorder StartProfilerRecorder(ProfilerCategory category, string statName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, statName);
            }
            catch
            {
                return default;
            }
        }

        private ProfilerRecorder StartRenderProfilerRecorder(IReadOnlyList<string> candidateNames)
        {
            for (int i = 0; i < candidateNames.Count; i++)
            {
                ProfilerRecorder recorder = StartProfilerRecorder(ProfilerCategory.Render, candidateNames[i]);
                if (recorder.Valid)
                    return recorder;
            }

            try
            {
                List<ProfilerRecorderHandle> handles = new();
                ProfilerRecorderHandle.GetAvailable(handles);
                for (int i = 0; i < handles.Count; i++)
                {
                    ProfilerRecorderHandle handle = handles[i];
                    ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.Category != ProfilerCategory.Render)
                        continue;

                    string name = description.Name.ToString();
                    if (!MatchesProfilerCounterName(name, candidateNames))
                        continue;

                    ProfilerRecorder recorder = StartProfilerRecorder(description.Category, name);
                    if (recorder.Valid)
                        return recorder;
                }
            }
            catch
            {
                // Render counter names vary by Unity version and platform; diagnostics must degrade gracefully.
            }

            return default;
        }

        private static bool MatchesProfilerCounterName(string name, IReadOnlyList<string> candidateNames)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string normalizedName = NormalizeProfilerCounterName(name);
            for (int i = 0; i < candidateNames.Count; i++)
            {
                string normalizedCandidate = NormalizeProfilerCounterName(candidateNames[i]);
                if (string.Equals(normalizedName, normalizedCandidate, StringComparison.OrdinalIgnoreCase) ||
                    normalizedName.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string NormalizeProfilerCounterName(string name)
        {
            return name
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);
        }

        private void EnsureMainMenuMarkerRecorders(bool simulationActive)
        {
            if (!_enableProfilerMarkerDiagnostics || !simulationActive ||
                _mainMenuMarkerRegistrationAttempts >= MaxMainMenuMarkerRegistrationAttempts)
                return;

            _mainMenuMarkerRegistrationAttempts++;
            bool allRegistered = true;
            for (int i = 0; i < MainMenuDiagnosticMarkerNames.Length; i++)
            {
                string name = MainMenuDiagnosticMarkerNames[i];
                AddProfilerMarkerRecorder(ProfilerCategory.Scripts, name);
                allRegistered &= HasProfilerMarkerRecorder(name);
            }
            if (allRegistered)
                _mainMenuMarkerRegistrationAttempts = MaxMainMenuMarkerRegistrationAttempts;
        }

        private void AppendMainMenuMarkerDiagnostics(System.Text.StringBuilder builder)
        {
            for (int i = 0; i < MainMenuDiagnosticMarkerNames.Length; i++)
            {
                string name = MainMenuDiagnosticMarkerNames[i];
                if (builder.Length > 0)
                    builder.Append("|");
                builder.Append(name).Append("=");
                int recorderIndex = -1;
                for (int j = 0; j < _markerRecorders.Count; j++)
                {
                    if (_markerRecorders[j].Name != name)
                        continue;
                    recorderIndex = j;
                    break;
                }
                if (recorderIndex < 0 || !_markerRecorders[recorderIndex].Recorder.Valid)
                    builder.Append("unavailable");
                else if (_markerRecorders[recorderIndex].Recorder.Count == 0)
                    builder.Append("uncollected");
                else
                    builder.Append((_markerRecorders[recorderIndex].Recorder.LastValue / 1000000d).ToString("F1")).Append("ms");
            }
            builder.Append("|targetSample=latestCompletedFrame");
        }

        private void AddProfilerMarkerRecorder(ProfilerCategory category, string statName)
        {
            if (HasProfilerMarkerRecorder(statName))
                return;

            ProfilerRecorder recorder = StartProfilerRecorder(category, statName);
            if (!recorder.Valid)
                return;

            _markerRecorders.Add(new NamedProfilerRecorder
            {
                Name = statName,
                Recorder = recorder
            });
        }

        private void AddAvailablePlayerLoopMarkerRecorders()
        {
            try
            {
                List<ProfilerRecorderHandle> handles = new();
                ProfilerRecorderHandle.GetAvailable(handles);
                int added = 0;
                for (int i = 0; i < handles.Count && added < MaxAutoProfilerMarkerRecorders; i++)
                {
                    ProfilerRecorderHandle handle = handles[i];
                    ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                    string name = description.Name.ToString();
                    if (!ShouldTrackProfilerMarker(name) || HasProfilerMarkerRecorder(name))
                        continue;

                    ProfilerRecorder recorder = StartProfilerRecorder(description.Category, name);
                    if (!recorder.Valid)
                        continue;

                    _markerRecorders.Add(new NamedProfilerRecorder
                    {
                        Name = name,
                        Recorder = recorder
                    });
                    added++;
                }
            }
            catch
            {
                // Marker enumeration is diagnostic-only and can vary by Unity/editor platform.
            }
        }

        private void AddPriorityPlayerLoopMarkerRecorders()
        {
            try
            {
                List<ProfilerRecorderHandle> handles = new();
                ProfilerRecorderHandle.GetAvailable(handles);
                for (int i = 0; i < handles.Count; i++)
                {
                    ProfilerRecorderHandle handle = handles[i];
                    ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                    string name = description.Name.ToString();
                    if (!ShouldTrackPriorityProfilerMarker(name) || HasProfilerMarkerRecorder(name))
                        continue;

                    ProfilerRecorder recorder = StartProfilerRecorder(description.Category, name);
                    if (!recorder.Valid)
                        continue;

                    _markerRecorders.Add(new NamedProfilerRecorder
                    {
                        Name = name,
                        Recorder = recorder
                    });
                }
            }
            catch
            {
                // Priority marker enumeration is diagnostic-only and can vary by Unity/editor platform.
            }
        }

        private bool HasProfilerMarkerRecorder(string statName)
        {
            for (int i = 0; i < _markerRecorders.Count; i++)
            {
                if (string.Equals(_markerRecorders[i].Name, statName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private bool ShouldTrackProfilerMarker(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return
                name.Contains("PlayerLoop", StringComparison.OrdinalIgnoreCase) ||
                IsEcsSystemProfilerMarker(name) ||
                name.Contains("EditorLoop", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Render", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Camera", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Canvas", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("UI", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Entities", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Script", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Wait", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Present", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Gfx", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldTrackPriorityProfilerMarker(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            for (int i = 0; i < PriorityProfilerMarkerNameFragments.Length; i++)
            {
                if (name.Contains(PriorityProfilerMarkerNameFragments[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsEcsSystemProfilerMarker(string name)
        {
            return name.StartsWith("Default World ", StringComparison.OrdinalIgnoreCase) &&
                   name.Contains("System", StringComparison.OrdinalIgnoreCase);
        }

        private void DisposeProfilerRecorders()
        {
            if (_drawCallsRecorder.Valid) _drawCallsRecorder.Dispose();
            if (_batchesRecorder.Valid) _batchesRecorder.Dispose();
            if (_setPassCallsRecorder.Valid) _setPassCallsRecorder.Dispose();
            if (_trianglesRecorder.Valid) _trianglesRecorder.Dispose();
            if (_verticesRecorder.Valid) _verticesRecorder.Dispose();
            for (int i = 0; i < _markerRecorders.Count; i++)
            {
                ProfilerRecorder recorder = _markerRecorders[i].Recorder;
                if (recorder.Valid)
                    recorder.Dispose();
            }

            _markerRecorders.Clear();
        }

        private void UpdateFrameRateDiagnostics(bool gameplayActive, double now, int impostorCount, bool playRequested, bool simulationActive, Camera camera)
        {
            if (!_enableFrameRateDiagnostics)
                return;

            if (PerformanceDiagnosticsCapturePolicy.SuppressLogging)
            {
                ResetFrameRateDiagnosticWindow(now);
                return;
            }

            if (_applicationPaused || (!Application.isFocused && !Application.isBatchMode))
            {
                ResetFrameRateDiagnosticWindow(now);
                return;
            }

            _frameRateDiagFrames++;
            _frameRateDiagAccumulatedSeconds += UnityEngine.Time.unscaledDeltaTime;
            if (now < _nextFrameRateDiagTimestamp)
                return;

            double averageFrameMs = _frameRateDiagFrames > 0
                ? (_frameRateDiagAccumulatedSeconds * 1000d) / _frameRateDiagFrames
                : 0d;
            double averageFps = averageFrameMs > 0d ? 1000d / averageFrameMs : 0d;
            double updateAverageMs = _frameRateDiagFrames > 0
                ? (_frameRateDiagUpdateAccumulatedSeconds * 1000d) / _frameRateDiagFrames
                : 0d;
            if (averageFps < LowFpsDiagThreshold)
            {
                GetRuntimeVisualCounts(
                    out int units,
                    out int modelInstances,
                    out int sourceKeys,
                    out int sourceKeyFallbackVisuals,
                    out int initialSpawnConfigs);
                string label = gameplayActive ? "FrameRateDiag" : "FrameRateDiag:PreGame";
                string preGameDetails = gameplayActive
                    ? string.Empty
                    : $" lastSteps={BuildLastStepLogString()}";
                LogNoStackTrace(
                    $"[{label}] fps={averageFps:F1} avgFrame={averageFrameMs:F1}ms " +
                    $"updateAvg={updateAverageMs:F1}ms updateMax={_frameRateDiagMaxUpdateSeconds * 1000d:F1}ms " +
                    $"{BuildFrameTimingDiagString()} " +
                    $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
                    $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
                    $"units={units} models={modelInstances} sourceKeys={sourceKeys} sourceKeyFallbackVisuals={sourceKeyFallbackVisuals} initialSpawnConfigs={initialSpawnConfigs} impostors={impostorCount} " +
                    $"render={BuildRenderQualityDiagString(camera)} " +
                    $"memory={BuildMemoryDiagString()} focused={(Application.isFocused ? 1 : 0)} playRequested={(playRequested ? 1 : 0)} simulationActive={(simulationActive ? 1 : 0)}{preGameDetails} " +
                    $"stepStats={BuildStepStatsString()} topSystems={BuildTopSystemProfilerMarkerString()} markers={BuildProfilerMarkerDiagString()}");
                LogRenderSceneBreakdownIfNeeded(now, averageFps);
            }

            _frameRateDiagFrames = 0;
            _frameRateDiagAccumulatedSeconds = 0d;
            _frameRateDiagUpdateAccumulatedSeconds = 0d;
            _frameRateDiagMaxUpdateSeconds = 0d;
            _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
        }

        private static void LogNoStackTrace(string message)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
        }

        private void ResetFrameRateDiagnosticWindow(double now)
        {
            _frameRateDiagFrames = 0;
            _frameRateDiagAccumulatedSeconds = 0d;
            _frameRateDiagUpdateAccumulatedSeconds = 0d;
            _frameRateDiagMaxUpdateSeconds = 0d;
            _nextFrameRateDiagTimestamp = now + FrameRateDiagIntervalSeconds;
        }

        private long ReadProfilerRecorder(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : -1L;
        }

        private static string BuildRenderQualityDiagString(Camera camera)
        {
            string pipelineName = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline.name
                : "null";
            float renderScale = QualitySettings.renderPipeline is UniversalRenderPipelineAsset urpAsset
                ? urpAsset.renderScale
                : -1f;
            string name = camera != null ? camera.name : "null";
            string data = "none";
            if (camera != null && camera.TryGetComponent(out UniversalAdditionalCameraData additionalCameraData))
            {
                data =
                    $"post:{(additionalCameraData.renderPostProcessing ? 1 : 0)},aa:{additionalCameraData.antialiasing},stack:{additionalCameraData.cameraStack.Count}";
            }

            int qualityIndex = QualitySettings.GetQualityLevel();
            string qualityName = qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length
                ? QualitySettings.names[qualityIndex]
                : "unknown";

            return
                $"screen={Screen.width}x{Screen.height},batch={(Application.isBatchMode ? 1 : 0)},quality={qualityIndex}:{qualityName},vSync={QualitySettings.vSyncCount},targetFps={Application.targetFrameRate},pipeline={pipelineName},scale={renderScale:F2},msaa={QualitySettings.antiAliasing},camera={name},cameraData={data}";
        }

        private void GetRuntimeVisualCounts(
            out int units,
            out int modelInstances,
            out int sourceKeys,
            out int sourceKeyFallbackVisuals,
            out int initialSpawnConfigs)
        {
            units = 0;
            modelInstances = 0;
            sourceKeys = 0;
            sourceKeyFallbackVisuals = 0;
            initialSpawnConfigs = 0;
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager em = world.EntityManager;
            using EntityQuery unitQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<Faction>());
            using EntityQuery modelQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<UnitModelInstanceReference>(),
                    ComponentType.ReadOnly<UnitDetailedVisualReference>(),
                }
            });
            using EntityQuery sourceKeyQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitSourcePrefabKey>());
            using EntityQuery sourceKeyFallbackVisualQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<UnitModelInstanceReference>(),
                ComponentType.Exclude<UnitDetailedVisualReference>(),
                ComponentType.Exclude<RuntimeBuildingCombatTag>(),
                ComponentType.Exclude<UnitRenderBudgetCulledUnitTag>());
            using EntityQuery initialSpawnConfigQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            units = unitQuery.CalculateEntityCount();
            modelInstances = modelQuery.CalculateEntityCount();
            sourceKeys = sourceKeyQuery.CalculateEntityCount();
            sourceKeyFallbackVisuals = sourceKeyFallbackVisualQuery.CalculateEntityCount();
            initialSpawnConfigs = initialSpawnConfigQuery.CalculateEntityCount();
        }

        private void RecordUpdateFrameStats(double totalSeconds)
        {
            _frameRateDiagUpdateAccumulatedSeconds += totalSeconds;
            if (totalSeconds > _frameRateDiagMaxUpdateSeconds)
                _frameRateDiagMaxUpdateSeconds = totalSeconds;
        }

        private void LogSlowUpdateDiagnosticsIfNeeded(
            bool gameplayActive,
            double totalSeconds,
            double now,
            bool gameplayInitialized,
            bool playRequested,
            bool simulationActive)
        {
            if (PerformanceDiagnosticsCapturePolicy.SuppressLogging || !_enableSlowFrameDiagnostics ||
                totalSeconds < SlowFrameDiagThresholdSeconds || now < _nextSlowFrameDiagTimestamp)
                return;
            if (!Application.isFocused && !Application.isBatchMode)
                return;

            _nextSlowFrameDiagTimestamp = now + SlowFrameDiagCooldownSeconds;
            GetRuntimeVisualCounts(
                out int units,
                out int modelInstances,
                out int sourceKeys,
                out int sourceKeyFallbackVisuals,
                out int initialSpawnConfigs);
            string label = gameplayActive ? "PerfDiag" : "PerfDiag:PreGame";
            LogNoStackTrace(
                $"[{label}] slowUpdate frame={UnityEngine.Time.frameCount} total={totalSeconds * 1000d:F1}ms " +
                $"gc={BuildGcDeltaString()} {BuildFrameTimingDiagString()} steps={BuildLastStepLogString()} units={units} models={modelInstances} sourceKeys={sourceKeys} sourceKeyFallbackVisuals={sourceKeyFallbackVisuals} initialSpawnConfigs={initialSpawnConfigs} " +
                $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
                $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
                $"memory={BuildMemoryDiagString()} uiToolkit=0 " +
                $"gameplayInitialized={(gameplayInitialized ? 1 : 0)} playRequested={(playRequested ? 1 : 0)} simulationActive={(simulationActive ? 1 : 0)} " +
                $"focused={(Application.isFocused ? 1 : 0)} vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} " +
                $"topSystems={BuildTopSystemProfilerMarkerString()} markers={BuildProfilerMarkerDiagString()}");
        }

        private string BuildStepStatsString()
        {
            return "none";
        }

        private string BuildMemoryDiagString()
        {
            return
                $"alloc={Profiler.GetTotalAllocatedMemoryLong() / (1024L * 1024L)}MB " +
                $"reserved={Profiler.GetTotalReservedMemoryLong() / (1024L * 1024L)}MB " +
                $"mono={Profiler.GetMonoUsedSizeLong() / (1024L * 1024L)}MB";
        }

        private string BuildFrameTimingDiagString()
        {
            FrameTimingManager.CaptureFrameTimings();
            FrameTiming[] timings = new FrameTiming[1];
            uint count = FrameTimingManager.GetLatestTimings(1, timings);
            if (count == 0)
                return "frameTiming=unavailable";

            FrameTiming timing = timings[0];
            return
                $"cpuFrame={timing.cpuFrameTime:F1}ms " +
                $"cpuMain={timing.cpuMainThreadFrameTime:F1}ms " +
                $"cpuRender={timing.cpuRenderThreadFrameTime:F1}ms " +
                $"gpu={timing.gpuFrameTime:F1}ms";
        }

        private string BuildProfilerMarkerDiagString()
        {
            if (!_enableProfilerMarkerDiagnostics)
                return "disabled";

            System.Text.StringBuilder builder = new();
            AppendMainMenuMarkerDiagnostics(builder);
            for (int i = 0; i < _markerRecorders.Count; i++)
            {
                NamedProfilerRecorder entry = _markerRecorders[i];
                if (Array.IndexOf(MainMenuDiagnosticMarkerNames, entry.Name) >= 0 || !entry.Recorder.Valid)
                    continue;

                long value = entry.Recorder.LastValue;
                if (value <= 0)
                    continue;

                if (builder.Length > 0)
                    builder.Append("|");
                builder.Append(entry.Name);
                builder.Append("=");
                builder.Append((value / 1000000d).ToString("F1"));
                builder.Append("ms");
            }

            return builder.Length > 0 ? builder.ToString() : "none-active";
        }

        private void LogRenderSceneBreakdownIfNeeded(double now, double averageFps)
        {
            if (averageFps >= LowFpsDiagThreshold || now < _nextRenderSceneBreakdownDiagTimestamp)
                return;
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            _nextRenderSceneBreakdownDiagTimestamp = now + RenderSceneBreakdownDiagIntervalSeconds;
            LogNoStackTrace(BuildRenderSceneBreakdownDiagString());
        }

        private string BuildRenderSceneBreakdownDiagString()
        {
            return
                $"[RenderSceneDiag] frame={UnityEngine.Time.frameCount} source=profilerCounters " +
                $"drawCalls={ReadProfilerRecorder(_drawCallsRecorder)} batches={ReadProfilerRecorder(_batchesRecorder)} " +
                $"setPass={ReadProfilerRecorder(_setPassCallsRecorder)} tris={ReadProfilerRecorder(_trianglesRecorder)} verts={ReadProfilerRecorder(_verticesRecorder)} " +
                $"topSystems={BuildTopSystemProfilerMarkerString()} markers={BuildProfilerMarkerDiagString()}";
        }

        private string BuildTopSystemProfilerMarkerString()
        {
            if (!_enableProfilerMarkerDiagnostics)
                return "disabled";

            if (_markerRecorders.Count == 0)
                return "none";

            List<ProfilerMarkerSample> samples = new();
            for (int i = 0; i < _markerRecorders.Count; i++)
            {
                NamedProfilerRecorder entry = _markerRecorders[i];
                if (!entry.Recorder.Valid || !IsEcsSystemProfilerMarker(entry.Name))
                    continue;

                long value = entry.Recorder.LastValue;
                if (value <= 0)
                    continue;

                samples.Add(new ProfilerMarkerSample
                {
                    Name = entry.Name,
                    Value = value
                });
            }

            if (samples.Count == 0)
                return "none-active";

            samples.Sort((lhs, rhs) => rhs.Value.CompareTo(lhs.Value));
            System.Text.StringBuilder builder = new();
            int count = Math.Min(MaxTopSystemProfilerMarkers, samples.Count);
            for (int i = 0; i < count; i++)
            {
                ProfilerMarkerSample sample = samples[i];
                if (builder.Length > 0)
                    builder.Append("|");

                builder.Append(sample.Name);
                builder.Append("=");
                builder.Append((sample.Value / 1000000d).ToString("F1"));
                builder.Append("ms");
            }

            return builder.ToString();
        }

        private string BuildGcDeltaString()
        {
            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);
            return $"{gen0 - _lastGcGen0Count}/{gen1 - _lastGcGen1Count}/{gen2 - _lastGcGen2Count}";
        }

        private void CaptureGcCounts()
        {
            _lastGcGen0Count = GC.CollectionCount(0);
            _lastGcGen1Count = GC.CollectionCount(1);
            _lastGcGen2Count = GC.CollectionCount(2);
        }
    }
}
