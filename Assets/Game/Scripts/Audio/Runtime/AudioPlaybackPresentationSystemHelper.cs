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
        public const float SpatialSfxMinDistance = AudioPlaybackSourceConfiguration.SpatialSfxMinDistance;
        public const float SpatialSfxMaxDistance = AudioPlaybackSourceConfiguration.SpatialSfxMaxDistance;

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
            AudioSettingsComponent settings,
            float now = 0f,
            float musicTransitionSeconds = 0f)
        {
            if (request.Status != AudioPlaybackRequestStatus.Accepted)
            {
                return new AudioPlaybackPresentationResult(false, request.Status, "RequestNotAccepted", -1);
            }

            if (entry == null)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.MissingEvent, "MissingCatalogEntry", -1);
            }

            AudioClip clip = AudioPlaybackSourceConfiguration.SelectClip(entry);
            if (clip == null)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.MissingClip, "MissingClip", -1);
            }

            int maxInstances = math.max(1, entry.Playback?.MaxInstances ?? 1);
            if (CountActiveInstances(request.EventHash) >= maxInstances)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.Culled, "MaxInstances", -1);
            }

            bool isMusicState = request.Kind == AudioPlaybackRequestKind.MusicState;
            float transitionSeconds = math.max(0f, musicTransitionSeconds);
            int sourceIndex;
            if (isMusicState && transitionSeconds <= 0f)
            {
                StopActiveMusicExcept(request.EventHash);
                sourceIndex = RentSource();
            }
            else
            {
                sourceIndex = RentSource();
                if (sourceIndex < 0 && isMusicState)
                {
                    StopActiveMusicExcept(request.EventHash);
                    sourceIndex = RentSource();
                }
            }

            if (sourceIndex < 0)
            {
                return new AudioPlaybackPresentationResult(false, AudioPlaybackRequestStatus.Culled, "PoolFull", -1);
            }

            if (isMusicState && transitionSeconds > 0f)
                FadeOutActiveMusicExcept(request.EventHash, now, transitionSeconds);

            PooledAudioSource pooledSource = _sources[sourceIndex];
            AudioPlaybackSourceConfiguration.Apply(pooledSource.Source, request, entry, bus, settings, clip);
            pooledSource.EventHash = request.EventHash;
            pooledSource.RequestId = request.RequestId;
            pooledSource.Priority = request.Priority;
            pooledSource.BusId = AudioPlaybackSourceConfiguration.ResolveBusId(request, entry);
            pooledSource.VolumeDecibels = AudioPlaybackSourceConfiguration.ResolveTotalDecibels(request, entry, bus);
            pooledSource.InUse = true;
            pooledSource.ReleaseAfterFade = false;
            if (isMusicState && transitionSeconds > 0f)
            {
                float targetVolume = pooledSource.Source.volume;
                pooledSource.Source.volume = 0f;
                pooledSource.FadeActive = true;
                pooledSource.FadeStartVolume = 0f;
                pooledSource.FadeTargetVolume = targetVolume;
                pooledSource.FadeStartTime = now;
                pooledSource.FadeDuration = transitionSeconds;
            }
            _sources[sourceIndex] = pooledSource;

            pooledSource.Source.Play();
            return new AudioPlaybackPresentationResult(true, AudioPlaybackRequestStatus.Presented, "Played", sourceIndex);
        }

        public void UpdatePool()
        {
            UpdatePool(now: 0f);
        }

        public void UpdatePool(float now)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (!pooledSource.InUse)
                    continue;

                if (pooledSource.Source == null || !pooledSource.Source.isPlaying)
                {
                    ReleaseSource(i);
                    continue;
                }

                if (AdvanceFade(ref pooledSource, now))
                {
                    if (!pooledSource.FadeActive && pooledSource.ReleaseAfterFade)
                    {
                        pooledSource.Source.Stop();
                        ReleaseSource(i);
                        continue;
                    }

                    _sources[i] = pooledSource;
                }
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

        public void ApplySettingsToActiveSources(AudioSettingsComponent settings)
        {
            ApplySettingsToActiveSources(settings, now: 0f, fadeSeconds: 0f);
        }

        public void ApplySettingsToActiveSources(
            AudioSettingsComponent settings,
            float now,
            float fadeSeconds)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (!pooledSource.InUse || pooledSource.Source == null)
                    continue;
                if (pooledSource.ReleaseAfterFade)
                    continue;

                AdvanceFade(ref pooledSource, now);
                float targetVolume = AudioPlaybackSourceConfiguration.ResolveLinearVolume(
                    pooledSource.BusId,
                    pooledSource.VolumeDecibels,
                    settings);

                if (fadeSeconds > 0f && AudioPlaybackSourceConfiguration.IsMusicBus(pooledSource.BusId))
                {
                    pooledSource.FadeActive = true;
                    pooledSource.FadeStartVolume = pooledSource.Source.volume;
                    pooledSource.FadeTargetVolume = targetVolume;
                    pooledSource.FadeStartTime = now;
                    pooledSource.FadeDuration = fadeSeconds;
                }
                else
                {
                    pooledSource.FadeActive = false;
                    pooledSource.Source.volume = targetVolume;
                }

                _sources[i] = pooledSource;
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
            return AudioPlaybackSourceConfiguration.ResolveLinearVolume(request, entry, bus, settings);
        }

        public static float DecibelsToLinear(float decibels)
        {
            return AudioPlaybackSourceConfiguration.DecibelsToLinear(decibels);
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
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = SpatialSfxMinDistance;
            source.maxDistance = SpatialSfxMaxDistance;
            source.dopplerLevel = 0f;
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
                pooledSource.Source.rolloffMode = AudioRolloffMode.Linear;
                pooledSource.Source.minDistance = SpatialSfxMinDistance;
                pooledSource.Source.maxDistance = SpatialSfxMaxDistance;
                pooledSource.Source.dopplerLevel = 0f;
                pooledSource.Source.transform.localPosition = Vector3.zero;
            }

            pooledSource.InUse = false;
            pooledSource.EventHash = 0u;
            pooledSource.RequestId = 0;
            pooledSource.Priority = AudioPlaybackPriority.Low;
            pooledSource.BusId = null;
            pooledSource.VolumeDecibels = 0f;
            pooledSource.FadeActive = false;
            pooledSource.FadeStartVolume = 0f;
            pooledSource.FadeTargetVolume = 0f;
            pooledSource.FadeStartTime = 0f;
            pooledSource.FadeDuration = 0f;
            pooledSource.ReleaseAfterFade = false;
            _sources[index] = pooledSource;
        }

        private void StopActiveMusicExcept(uint eventHash)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (!pooledSource.InUse ||
                    pooledSource.EventHash == eventHash ||
                    !AudioPlaybackSourceConfiguration.IsMusicBus(pooledSource.BusId))
                {
                    continue;
                }

                pooledSource.Source?.Stop();
                ReleaseSource(i);
            }
        }

        private void FadeOutActiveMusicExcept(uint eventHash, float now, float duration)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (!pooledSource.InUse ||
                    pooledSource.Source == null ||
                    pooledSource.EventHash == eventHash ||
                    !AudioPlaybackSourceConfiguration.IsMusicBus(pooledSource.BusId))
                {
                    continue;
                }

                AdvanceFade(ref pooledSource, now);
                pooledSource.FadeActive = true;
                pooledSource.FadeStartVolume = pooledSource.Source.volume;
                pooledSource.FadeTargetVolume = 0f;
                pooledSource.FadeStartTime = now;
                pooledSource.FadeDuration = duration;
                pooledSource.ReleaseAfterFade = true;
                _sources[i] = pooledSource;
            }
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

        private static bool AdvanceFade(ref PooledAudioSource pooledSource, float now)
        {
            if (!pooledSource.FadeActive || pooledSource.Source == null)
                return false;

            float duration = math.max(0.0001f, pooledSource.FadeDuration);
            float t = math.saturate((now - pooledSource.FadeStartTime) / duration);
            pooledSource.Source.volume = math.lerp(
                pooledSource.FadeStartVolume,
                pooledSource.FadeTargetVolume,
                t);

            if (t >= 1f)
                pooledSource.FadeActive = false;

            return true;
        }

        private struct PooledAudioSource
        {
            public AudioSource Source;
            public bool InUse;
            public uint EventHash;
            public int RequestId;
            public AudioPlaybackPriority Priority;
            public string BusId;
            public float VolumeDecibels;
            public bool FadeActive;
            public float FadeStartVolume;
            public float FadeTargetVolume;
            public float FadeStartTime;
            public float FadeDuration;
            public bool ReleaseAfterFade;
        }
    }
}
