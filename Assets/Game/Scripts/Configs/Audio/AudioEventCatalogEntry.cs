using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    public enum AudioEventPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    [Serializable]
    public sealed class AudioClipWeightEntry
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Min(0)] private int weight = 1;

        public AudioClip Clip => clip;
        public int Weight => weight;
    }

    [Serializable]
    public sealed class LocalizedAudioClipSet
    {
        [SerializeField] private string localeCode;
        [SerializeField] private List<AudioClipWeightEntry> clips = new();

        public string LocaleCode => localeCode;
        public IReadOnlyList<AudioClipWeightEntry> Clips => clips;
    }

    [Serializable]
    public sealed class AudioPlaybackConfig
    {
        [SerializeField] private bool loop;
        [SerializeField] private bool spatial;
        [SerializeField, Min(1)] private int maxInstances = 4;
        [SerializeField] private bool allowRuntimeLoad;

        public bool Loop => loop;
        public bool Spatial => spatial;
        public int MaxInstances => maxInstances;
        public bool AllowRuntimeLoad => allowRuntimeLoad;
    }

    [Serializable]
    public sealed class AudioEventCatalogEntry
    {
        [SerializeField] private string eventId;
        [SerializeField] private string busId = "SFX";
        [SerializeField] private AudioEventPriority priority = AudioEventPriority.Medium;
        [SerializeField, Min(0)] private int cooldownMilliseconds;
        [SerializeField] private float volumeDecibels;
        [SerializeField] private Vector2 pitchVariance = new(-0.02f, 0.02f);
        [SerializeField] private AudioPlaybackConfig playback = new();
        [SerializeField] private List<AudioClipWeightEntry> clips = new();
        [SerializeField] private List<LocalizedAudioClipSet> localizedClips = new();

        public string EventId => eventId;
        public string BusId => busId;
        public AudioEventPriority Priority => priority;
        public int CooldownMilliseconds => cooldownMilliseconds;
        public float VolumeDecibels => volumeDecibels;
        public Vector2 PitchVariance => pitchVariance;
        public AudioPlaybackConfig Playback => playback;
        public IReadOnlyList<AudioClipWeightEntry> Clips => clips;
        public IReadOnlyList<LocalizedAudioClipSet> LocalizedClips => localizedClips;

        public IReadOnlyList<AudioClipWeightEntry> ResolveClips(string localeCode)
        {
            if (!string.IsNullOrWhiteSpace(localeCode) && localizedClips != null)
            {
                for (int i = 0; i < localizedClips.Count; i++)
                {
                    LocalizedAudioClipSet clipSet = localizedClips[i];
                    if (clipSet != null &&
                        string.Equals(clipSet.LocaleCode, localeCode, StringComparison.OrdinalIgnoreCase) &&
                        clipSet.Clips != null &&
                        clipSet.Clips.Count > 0)
                    {
                        return clipSet.Clips;
                    }
                }
            }

            return clips != null
                ? (IReadOnlyList<AudioClipWeightEntry>)clips
                : Array.Empty<AudioClipWeightEntry>();
        }

        public bool HasLocalizedClips(string localeCode)
        {
            IReadOnlyList<AudioClipWeightEntry> resolved = ResolveClips(localeCode);
            return clips != null && !ReferenceEquals(resolved, clips) && resolved.Count > 0;
        }
    }
}
