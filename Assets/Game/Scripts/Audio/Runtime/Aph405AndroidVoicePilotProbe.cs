#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs;
using UnityEngine;
using UnityEngine.Profiling;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Game.Runtime
{
    public readonly struct Aph405VoicePilotDiscoveryDescriptor
    {
        public Aph405VoicePilotDiscoveryDescriptor(
            int clipIdentity,
            string eventId,
            string busId,
            string clipName,
            bool compressedInMemory,
            bool preloadAudioData,
            AudioClip clip = null)
        {
            ClipIdentity = clipIdentity;
            EventId = eventId ?? string.Empty;
            BusId = busId ?? string.Empty;
            ClipName = clipName ?? string.Empty;
            CompressedInMemory = compressedInMemory;
            PreloadAudioData = preloadAudioData;
            Clip = clip;
        }

        public int ClipIdentity { get; }
        public string EventId { get; }
        public string BusId { get; }
        public string ClipName { get; }
        public bool CompressedInMemory { get; }
        public bool PreloadAudioData { get; }
        public AudioClip Clip { get; }
        public string StableKey => $"{EventId}|{ClipName}";
    }

    public readonly struct Aph405VoicePilotClipResult
    {
        public Aph405VoicePilotClipResult(
            int index,
            string eventId,
            string clipName,
            bool passed,
            double firstPlayLatencyMilliseconds,
            double repeatedPlayLatencyMilliseconds,
            AudioDataLoadState beforeLoadState,
            AudioDataLoadState afterFirstLoadState,
            AudioDataLoadState afterRepeatedLoadState,
            long beforeRuntimeMemoryBytes,
            long afterFirstRuntimeMemoryBytes,
            long afterRepeatedRuntimeMemoryBytes,
            string reason)
        {
            Index = index;
            EventId = eventId ?? string.Empty;
            ClipName = clipName ?? string.Empty;
            Passed = passed;
            FirstPlayLatencyMilliseconds = firstPlayLatencyMilliseconds;
            RepeatedPlayLatencyMilliseconds = repeatedPlayLatencyMilliseconds;
            BeforeLoadState = beforeLoadState;
            AfterFirstLoadState = afterFirstLoadState;
            AfterRepeatedLoadState = afterRepeatedLoadState;
            BeforeRuntimeMemoryBytes = beforeRuntimeMemoryBytes;
            AfterFirstRuntimeMemoryBytes = afterFirstRuntimeMemoryBytes;
            AfterRepeatedRuntimeMemoryBytes = afterRepeatedRuntimeMemoryBytes;
            Reason = reason ?? string.Empty;
        }

        public int Index { get; }
        public string EventId { get; }
        public string ClipName { get; }
        public bool Passed { get; }
        public double FirstPlayLatencyMilliseconds { get; }
        public double RepeatedPlayLatencyMilliseconds { get; }
        public AudioDataLoadState BeforeLoadState { get; }
        public AudioDataLoadState AfterFirstLoadState { get; }
        public AudioDataLoadState AfterRepeatedLoadState { get; }
        public long BeforeRuntimeMemoryBytes { get; }
        public long AfterFirstRuntimeMemoryBytes { get; }
        public long AfterRepeatedRuntimeMemoryBytes { get; }
        public string Reason { get; }
    }

    public static class Aph405VoicePilotProbeContract
    {
        public const int ExpectedClipCount = 8;
        public const string Marker = "[APH405VoicePilot]";
        public const string EditorCommandLineArgument = "-aph405VoicePilot";

        public static List<Aph405VoicePilotDiscoveryDescriptor> SelectEligibleClips(
            IReadOnlyList<Aph405VoicePilotDiscoveryDescriptor> descriptors)
        {
            List<Aph405VoicePilotDiscoveryDescriptor> selected = new();
            if (descriptors == null)
                return selected;

            for (int i = 0; i < descriptors.Count; i++)
            {
                Aph405VoicePilotDiscoveryDescriptor descriptor = descriptors[i];
                if (!string.Equals(descriptor.BusId, "Voice", StringComparison.Ordinal) ||
                    !descriptor.CompressedInMemory ||
                    descriptor.PreloadAudioData)
                {
                    continue;
                }

                int duplicateIndex = FindDuplicateIndex(selected, descriptor);
                if (duplicateIndex < 0)
                {
                    selected.Add(descriptor);
                }
                else if (string.CompareOrdinal(descriptor.StableKey, selected[duplicateIndex].StableKey) < 0)
                {
                    selected[duplicateIndex] = descriptor;
                }
            }

            selected.Sort((left, right) => string.CompareOrdinal(left.StableKey, right.StableKey));
            return selected;
        }

        private static int FindDuplicateIndex(
            IReadOnlyList<Aph405VoicePilotDiscoveryDescriptor> selected,
            Aph405VoicePilotDiscoveryDescriptor candidate)
        {
            for (int i = 0; i < selected.Count; i++)
            {
                Aph405VoicePilotDiscoveryDescriptor existing = selected[i];
                bool sameClip = candidate.Clip != null || existing.Clip != null
                    ? ReferenceEquals(candidate.Clip, existing.Clip)
                    : candidate.ClipIdentity == existing.ClipIdentity;
                if (sameClip)
                    return i;
            }

            return -1;
        }

        public static string FormatDiscoveryMarker(int actualCount)
        {
            string result = actualCount == ExpectedClipCount ? "Passed" : "Failed";
            return $"{Marker} phase=Discovery result={result} expected={ExpectedClipCount} actual={actualCount}";
        }

        public static string FormatClipMarker(Aph405VoicePilotClipResult result)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} phase=Clip result={1} index={2} event={3} clip={4} firstPlayLatencyMs={5:F3} " +
                "repeatedPlayLatencyMs={6:F3} beforeLoadState={7} afterFirstLoadState={8} " +
                "afterRepeatedLoadState={9} beforeRuntimeMemoryBytes={10} afterFirstRuntimeMemoryBytes={11} " +
                "afterRepeatedRuntimeMemoryBytes={12} reason={13}",
                Marker,
                result.Passed ? "Passed" : "Failed",
                result.Index,
                Escape(result.EventId),
                Escape(result.ClipName),
                result.FirstPlayLatencyMilliseconds,
                result.RepeatedPlayLatencyMilliseconds,
                result.BeforeLoadState,
                result.AfterFirstLoadState,
                result.AfterRepeatedLoadState,
                result.BeforeRuntimeMemoryBytes,
                result.AfterFirstRuntimeMemoryBytes,
                result.AfterRepeatedRuntimeMemoryBytes,
                Escape(result.Reason));
        }

        public static string FormatSummaryMarker(int passedCount, int failedCount)
        {
            string result = passedCount == ExpectedClipCount && failedCount == 0 ? "Passed" : "Failed";
            return $"{Marker} phase=Summary result={result} expected={ExpectedClipCount} passed={passedCount} failed={failedCount}";
        }

        public static bool HasCommandLineArgument(IReadOnlyList<string> arguments, string expectedArgument)
        {
            if (arguments == null || string.IsNullOrWhiteSpace(expectedArgument))
                return false;

            for (int i = 0; i < arguments.Count; i++)
            {
                if (string.Equals(arguments[i], expectedArgument, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string Escape(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty);
        }
    }

    public sealed class Aph405AndroidVoicePilotProbe : IDisposable
    {
        private const double PlaybackTimeoutSeconds = 10d;

        private readonly List<Aph405VoicePilotDiscoveryDescriptor> _clips;
        private readonly GameObject _root;
        private readonly AudioSource _source;
        private ProbeStage _stage;
        private int _clipIndex;
        private int _passedCount;
        private int _failedCount;
        private long _stageStartedTimestamp;
        private AudioDataLoadState _beforeLoadState;
        private AudioDataLoadState _afterFirstLoadState;
        private long _beforeMemoryBytes;
        private long _afterFirstMemoryBytes;
        private double _firstLatencyMilliseconds = -1d;

        public Aph405AndroidVoicePilotProbe(AudioEventCatalogConfig catalog, Transform parent)
        {
            _clips = Discover(catalog);
            Debug.Log(Aph405VoicePilotProbeContract.FormatDiscoveryMarker(_clips.Count));

            if (_clips.Count != Aph405VoicePilotProbeContract.ExpectedClipCount)
            {
                _stage = ProbeStage.Complete;
                Debug.LogError(Aph405VoicePilotProbeContract.FormatSummaryMarker(0, _clips.Count));
                return;
            }

            _root = new GameObject("APH405AndroidVoicePilotProbe");
            _root.transform.SetParent(parent, false);
            _source = _root.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.25f;
            _stage = ProbeStage.PrepareColdPlay;
        }

        public bool IsComplete => _stage == ProbeStage.Complete;

        public void Tick()
        {
            if (IsComplete || _source == null)
                return;

            Aph405VoicePilotDiscoveryDescriptor descriptor = _clips[_clipIndex];
            AudioClip clip = descriptor.Clip;
            if (clip == null)
            {
                FinishClip(descriptor, false, -1d, "MissingClip");
                return;
            }

            switch (_stage)
            {
                case ProbeStage.PrepareColdPlay:
                    _source.Stop();
                    _source.clip = null;
                    if (clip.loadState != AudioDataLoadState.Unloaded)
                        clip.UnloadAudioData();
                    _stageStartedTimestamp = Stopwatch.GetTimestamp();
                    _stage = ProbeStage.WaitForUnload;
                    break;

                case ProbeStage.WaitForUnload:
                    if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Failed)
                    {
                        _beforeLoadState = clip.loadState;
                        _beforeMemoryBytes = Profiler.GetRuntimeMemorySizeLong(clip);
                        StartPlayback(clip, ProbeStage.WaitForFirstPlay);
                    }
                    else if (HasTimedOut())
                    {
                        FinishClip(descriptor, false, -1d, "UnloadTimeout");
                    }
                    break;

                case ProbeStage.WaitForFirstPlay:
                    if (HasPlaybackStarted())
                    {
                        _firstLatencyMilliseconds = ElapsedMilliseconds();
                        _afterFirstLoadState = clip.loadState;
                        _afterFirstMemoryBytes = Profiler.GetRuntimeMemorySizeLong(clip);
                        _source.Stop();
                        StartPlayback(clip, ProbeStage.WaitForRepeatedPlay);
                    }
                    else if (HasTimedOut())
                    {
                        FinishClip(descriptor, false, -1d, "FirstPlayTimeout");
                    }
                    break;

                case ProbeStage.WaitForRepeatedPlay:
                    if (HasPlaybackStarted())
                    {
                        FinishClip(descriptor, true, ElapsedMilliseconds(), "None");
                    }
                    else if (HasTimedOut())
                    {
                        FinishClip(descriptor, false, -1d, "RepeatedPlayTimeout");
                    }
                    break;
            }
        }

        public void Dispose()
        {
            if (_source != null)
                _source.Stop();
            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        public static List<Aph405VoicePilotDiscoveryDescriptor> Discover(AudioEventCatalogConfig catalog)
        {
            List<Aph405VoicePilotDiscoveryDescriptor> descriptors = new();
            if (catalog == null)
                return descriptors;

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

                    descriptors.Add(new Aph405VoicePilotDiscoveryDescriptor(
                        0,
                        entry.EventId,
                        entry.BusId,
                        clip.name,
                        clip.loadType == AudioClipLoadType.CompressedInMemory,
                        clip.preloadAudioData,
                        clip));
                }
            }

            return Aph405VoicePilotProbeContract.SelectEligibleClips(descriptors);
        }

        private void StartPlayback(AudioClip clip, ProbeStage nextStage)
        {
            _source.Stop();
            _source.clip = clip;
            _source.time = 0f;
            _stageStartedTimestamp = Stopwatch.GetTimestamp();
            _source.Play();
            _stage = nextStage;
        }

        private bool HasPlaybackStarted()
        {
            return _source.isPlaying && _source.timeSamples > 0;
        }

        private bool HasTimedOut()
        {
            return ElapsedSeconds() >= PlaybackTimeoutSeconds;
        }

        private double ElapsedMilliseconds()
        {
            return ElapsedSeconds() * 1000d;
        }

        private double ElapsedSeconds()
        {
            return (Stopwatch.GetTimestamp() - _stageStartedTimestamp) / (double)Stopwatch.Frequency;
        }

        private void FinishClip(
            Aph405VoicePilotDiscoveryDescriptor descriptor,
            bool passed,
            double repeatedLatencyMilliseconds,
            string reason)
        {
            AudioClip clip = descriptor.Clip;
            _source.Stop();
            Aph405VoicePilotClipResult result = new(
                _clipIndex,
                descriptor.EventId,
                descriptor.ClipName,
                passed,
                _firstLatencyMilliseconds,
                repeatedLatencyMilliseconds,
                _beforeLoadState,
                _afterFirstLoadState,
                clip != null ? clip.loadState : AudioDataLoadState.Failed,
                _beforeMemoryBytes,
                _afterFirstMemoryBytes,
                clip != null ? Profiler.GetRuntimeMemorySizeLong(clip) : 0L,
                reason);

            if (passed)
                _passedCount++;
            else
                _failedCount++;

            if (passed)
                Debug.Log(Aph405VoicePilotProbeContract.FormatClipMarker(result));
            else
                Debug.LogError(Aph405VoicePilotProbeContract.FormatClipMarker(result));

            _clipIndex++;
            if (_clipIndex >= _clips.Count)
            {
                _stage = ProbeStage.Complete;
                string summary = Aph405VoicePilotProbeContract.FormatSummaryMarker(_passedCount, _failedCount);
                if (_failedCount == 0)
                    Debug.Log(summary);
                else
                    Debug.LogError(summary);
                return;
            }

            _firstLatencyMilliseconds = -1d;
            _beforeLoadState = AudioDataLoadState.Unloaded;
            _afterFirstLoadState = AudioDataLoadState.Unloaded;
            _beforeMemoryBytes = 0L;
            _afterFirstMemoryBytes = 0L;
            _stage = ProbeStage.PrepareColdPlay;
        }

        private enum ProbeStage
        {
            PrepareColdPlay,
            WaitForUnload,
            WaitForFirstPlay,
            WaitForRepeatedPlay,
            Complete
        }
    }
}
#endif
