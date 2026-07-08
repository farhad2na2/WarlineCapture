using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    public readonly struct AudioPlaybackPresentationResult
    {
        public AudioPlaybackPresentationResult(
            bool played,
            AudioPlaybackRequestStatus status,
            string reason,
            int sourceIndex)
        {
            Played = played;
            Status = status;
            Reason = reason;
            SourceIndex = sourceIndex;
        }

        public bool Played { get; }
        public AudioPlaybackRequestStatus Status { get; }
        public string Reason { get; }
        public int SourceIndex { get; }
    }

    public sealed class AudioPlaybackPresentationSystemHelper : IDisposable
    {
        private readonly List<PooledAudioSource> _sources = new();
        private readonly int _maxPoolSize;
        private readonly GameObject _root;
        private int _createdSourceCount;

        public AudioPlaybackPresentationSystemHelper(Transform parent = null, int initialPoolSize = 8, int maxPoolSize = 32)
        {
            if (initialPoolSize < 0)
                throw new ArgumentOutOfRangeException(nameof(initialPoolSize), "Initial pool size must be non-negative.");
            if (maxPoolSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "Max pool size must be positive.");
            if (initialPoolSize > maxPoolSize)
                throw new ArgumentOutOfRangeException(nameof(initialPoolSize), "Initial pool size cannot exceed max pool size.");

            _maxPoolSize = maxPoolSize;
            _root = new GameObject("AudioPlaybackPresentationPool");
            _root.transform.SetParent(parent, false);
            if (parent == null && Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(_root);

            for (int i = 0; i < initialPoolSize; i++)
                CreateSource();
        }

        public int PoolSize => _sources.Count;
        public int MaxPoolSize => _maxPoolSize;
        public int CreatedSourceCount => _createdSourceCount;

        public int ActiveSourceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _sources.Count; i++)
                {
                    if (_sources[i].InUse)
                        count++;
                }

                return count;
            }
        }

        public AudioPlaybackPresentationResult PlayAcceptedRequest(
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry,
            AudioMixerBusEntry bus,
            AudioSettingsComponent settings)
        {
            if (request.Status != AudioPlaybackRequestStatus.Accepted)
            {
                return new AudioPlaybackPresentationResult(false, request.Status, "RequestNotAccepted", -1);
            }

            if (entry == null)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.MissingEvent, "MissingCatalogEntry", -1);
            }

            AudioClip clip = SelectClip(entry);
            if (clip == null)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.MissingClip, "MissingClip", -1);
            }

            int maxInstances = math.max(1, entry.Playback?.MaxInstances ?? 1);
            if (CountActiveInstances(request.EventHash) >= maxInstances)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.Culled, "MaxInstances", -1);
            }

            int sourceIndex = RentSource();
            if (sourceIndex < 0)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.Culled, "PoolFull", -1);
            }

            PooledAudioSource pooledSource = _sources[sourceIndex];
            ConfigureSource(pooledSource.Source, request, entry, bus, settings, clip);
            pooledSource.EventHash = request.EventHash;
            pooledSource.RequestId = request.RequestId;
            pooledSource.Priority = request.Priority;
            pooledSource.InUse = true;
            _sources[sourceIndex] = pooledSource;

            pooledSource.Source.Play();
            return new AudioPlaybackPresentationResult(true, AudioPlaybackRequestStatus.Accepted, "Played", sourceIndex);
        }

        public void UpdatePool()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (!pooledSource.InUse)
                    continue;

                if (pooledSource.Source == null || !pooledSource.Source.isPlaying)
                    ReleaseSource(i);
            }
        }

        public void StopAll()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (pooledSource.Source != null)
                    pooledSource.Source.Stop();
                ReleaseSource(i);
            }
        }

        public bool TryGetActiveSource(int requestId, out AudioSource source)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (pooledSource.InUse && pooledSource.RequestId == requestId)
                {
                    source = pooledSource.Source;
                    return true;
                }
            }

            source = null;
            return false;
        }

        public void Dispose()
        {
            StopAll();
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            _sources.Clear();
        }

        public static float ResolveLinearVolume(
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry,
            AudioMixerBusEntry bus,
            AudioSettingsComponent settings)
        {
            string busId = !string.IsNullOrWhiteSpace(entry?.BusId)
                ? entry.BusId
                : request.BusId.ToString();
            float busVolume = ResolveBusVolume(busId, settings);
            float decibels = (entry?.VolumeDecibels ?? 0f) + request.VolumeDecibels + (bus?.DefaultVolumeDecibels ?? 0f);
            return math.saturate(DecibelsToLinear(decibels) * busVolume);
        }

        public static float DecibelsToLinear(float decibels)
        {
            if (decibels <= -80f)
                return 0f;
            return math.pow(10f, decibels / 20f);
        }

        private static float ResolveBusVolume(string busId, AudioSettingsComponent settings)
        {
            if (settings.MasterMuted != 0)
                return 0f;

            float master = math.saturate(settings.MasterVolume);
            string normalizedBus = string.IsNullOrWhiteSpace(busId) ? "SFX" : busId;
            float busVolume = normalizedBus switch
            {
                "UI" => settings.UiMuted != 0 ? 0f : math.saturate(settings.UiVolume),
                "Music" => settings.MusicMuted != 0 ? 0f : math.saturate(settings.MusicVolume),
                "Ambience" => settings.AmbienceMuted != 0 ? 0f : math.saturate(settings.AmbienceVolume),
                "Voice" => settings.VoiceMuted != 0 ? 0f : math.saturate(settings.VoiceVolume),
                "Alerts" => settings.AlertsMuted != 0 ? 0f : math.saturate(settings.AlertsVolume),
                _ => settings.SfxMuted != 0 ? 0f : math.saturate(settings.SfxVolume)
            };

            return master * busVolume;
        }

        private int RentSource()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                if (!_sources[i].InUse)
                    return i;
            }

            if (_sources.Count >= _maxPoolSize)
                return -1;

            return CreateSource();
        }

        private int CreateSource()
        {
            GameObject sourceObject = new($"AudioSource_{_sources.Count:00}");
            sourceObject.transform.SetParent(_root.transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            _sources.Add(new PooledAudioSource { Source = source });
            _createdSourceCount++;
            return _sources.Count - 1;
        }

        private void ReleaseSource(int index)
        {
            PooledAudioSource pooledSource = _sources[index];
            if (pooledSource.Source != null)
            {
                pooledSource.Source.clip = null;
                pooledSource.Source.outputAudioMixerGroup = null;
                pooledSource.Source.loop = false;
                pooledSource.Source.spatialBlend = 0f;
                pooledSource.Source.transform.localPosition = Vector3.zero;
            }

            pooledSource.InUse = false;
            pooledSource.EventHash = 0u;
            pooledSource.RequestId = 0;
            pooledSource.Priority = AudioPlaybackPriority.Low;
            _sources[index] = pooledSource;
        }

        private int CountActiveInstances(uint eventHash)
        {
            int count = 0;
            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sources[i].InUse && _sources[i].EventHash == eventHash)
                    count++;
            }

            return count;
        }

        private static AudioClip SelectClip(AudioEventCatalogEntry entry)
        {
            IReadOnlyList<AudioClipWeightEntry> clips = entry.Clips;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i]?.Clip != null && clips[i].Weight > 0)
                    return clips[i].Clip;
            }

            return null;
        }

        private static void ConfigureSource(
            AudioSource source,
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry,
            AudioMixerBusEntry bus,
            AudioSettingsComponent settings,
            AudioClip clip)
        {
            source.clip = clip;
            source.outputAudioMixerGroup = bus?.MixerGroup;
            source.loop = entry.Playback?.Loop ?? request.Kind == AudioPlaybackRequestKind.MusicState;
            source.spatialBlend = (entry.Playback?.Spatial ?? request.Spatial != 0) ? 1f : 0f;
            source.volume = ResolveLinearVolume(request, entry, bus, settings);
            source.pitch = ResolvePitch(request, entry);

            if (request.HasWorldPosition != 0)
            {
                source.transform.position = new Vector3(
                    request.WorldPosition.x,
                    request.WorldPosition.y,
                    request.WorldPosition.z);
            }
            else
            {
                source.transform.localPosition = Vector3.zero;
            }
        }

        private static float ResolvePitch(AudioPlaybackRequestElement request, AudioEventCatalogEntry entry)
        {
            float requestPitch = request.PitchMultiplier <= 0f ? 1f : request.PitchMultiplier;
            Vector2 variance = entry?.PitchVariance ?? Vector2.zero;
            float deterministicVariance = (variance.x + variance.y) * 0.5f;
            return math.max(0.01f, requestPitch + deterministicVariance);
        }

        private struct PooledAudioSource
        {
            public AudioSource Source;
            public bool InUse;
            public uint EventHash;
            public int RequestId;
            public AudioPlaybackPriority Priority;
        }
    }
}
