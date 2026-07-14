using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal static class AudioPlaybackSourceConfiguration
    {
        public const float SpatialSfxMinDistance = 24f;
        public const float SpatialSfxMaxDistance = 180f;

        public static AudioClip SelectClip(AudioEventCatalogEntry entry)
        {
            IReadOnlyList<AudioClipWeightEntry> clips = entry.Clips;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i]?.Clip != null && clips[i].Weight > 0)
                    return clips[i].Clip;
            }

            return null;
        }

        public static void Apply(
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
            bool spatial = entry.Playback?.Spatial ?? request.Spatial != 0;
            source.spatialBlend = spatial ? 1f : 0f;
            ConfigureSpatialReach(source, spatial);
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

        public static string ResolveBusId(AudioPlaybackRequestElement request, AudioEventCatalogEntry entry)
        {
            return !string.IsNullOrWhiteSpace(entry?.BusId)
                ? entry.BusId
                : request.BusId.ToString();
        }

        public static float ResolveTotalDecibels(
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry,
            AudioMixerBusEntry bus)
        {
            return (entry?.VolumeDecibels ?? 0f) +
                request.VolumeDecibels +
                (bus?.DefaultVolumeDecibels ?? 0f);
        }

        public static float ResolveLinearVolume(
            AudioPlaybackRequestElement request,
            AudioEventCatalogEntry entry,
            AudioMixerBusEntry bus,
            AudioSettingsComponent settings)
        {
            return ResolveLinearVolume(
                ResolveBusId(request, entry),
                ResolveTotalDecibels(request, entry, bus),
                settings);
        }

        public static float ResolveLinearVolume(
            string busId,
            float decibels,
            AudioSettingsComponent settings)
        {
            float busVolume = ResolveBusVolume(busId, settings);
            return math.saturate(DecibelsToLinear(decibels) * busVolume);
        }

        public static float DecibelsToLinear(float decibels)
        {
            if (decibels <= -80f)
                return 0f;
            return math.pow(10f, decibels / 20f);
        }

        public static bool IsMusicBus(string busId)
        {
            return string.Equals(busId, "Music", StringComparison.Ordinal);
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

        private static float ResolvePitch(AudioPlaybackRequestElement request, AudioEventCatalogEntry entry)
        {
            float requestPitch = request.PitchMultiplier <= 0f ? 1f : request.PitchMultiplier;
            Vector2 variance = entry?.PitchVariance ?? Vector2.zero;
            float deterministicVariance = (variance.x + variance.y) * 0.5f;
            return math.max(0.01f, requestPitch + deterministicVariance);
        }

        private static void ConfigureSpatialReach(AudioSource source, bool spatial)
        {
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = spatial ? SpatialSfxMinDistance : 1f;
            source.maxDistance = spatial ? SpatialSfxMaxDistance : 500f;
            source.dopplerLevel = 0f;
            source.spread = 0f;
        }
    }
}
