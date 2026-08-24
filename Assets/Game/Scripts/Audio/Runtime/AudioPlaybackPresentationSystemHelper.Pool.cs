using System;
using Game.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class AudioPlaybackPresentationSystemHelper
    {
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

        private bool TryInterruptBus(string busId, AudioPlaybackPriority incomingPriority)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (pooledSource.InUse &&
                    string.Equals(pooledSource.BusId, busId, StringComparison.Ordinal) &&
                    pooledSource.Priority > incomingPriority)
                {
                    return false;
                }
            }

            for (int i = 0; i < _sources.Count; i++)
            {
                PooledAudioSource pooledSource = _sources[i];
                if (!pooledSource.InUse ||
                    !string.Equals(pooledSource.BusId, busId, StringComparison.Ordinal))
                {
                    continue;
                }

                pooledSource.Source?.Stop();
                ReleaseSource(i);
            }

            return true;
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
