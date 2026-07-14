using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeEnvironmentAudioImporter
    {
        public const string EnvironmentRoot = "Assets/Game/Audio/Narrative/FirstLaunch/Environment";
        public const string RightsStatus = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE";
        public const float VorbisQuality = 0.7f;

        private static readonly string[] StreamingLoopIds =
        {
            "first_launch_story_calm_loop_01",
            "first_launch_story_crisis_loop_01",
            "first_launch_city_market_loop_01",
            "first_launch_city_attack_loop_01",
            "first_launch_command_room_loop_01",
            "first_launch_convoy_interior_loop_01",
        };

        private static readonly string[] ResidentEventIds =
        {
            "first_launch_distant_attack_event_01",
            "first_launch_radio_emergency_event_01",
        };

        public static IReadOnlyList<string> StableStreamingLoopIds => StreamingLoopIds;
        public static IReadOnlyList<string> StableResidentEventIds => ResidentEventIds;

        [MenuItem("Game/Narrative/Configure FirstLaunch Environment Audio Imports")]
        public static void ConfigureEnvironmentAudioImports()
        {
            foreach (string clipId in StreamingLoopIds)
                Configure(clipId, streaming: true);
            foreach (string clipId in ResidentEventIds)
                Configure(clipId, streaming: false);

            ValidateEnvironmentAudioImports();
            Debug.Log($"Configured {StreamingLoopIds.Length} streaming loops and {ResidentEventIds.Length} resident FirstLaunch event cues.");
        }

        [MenuItem("Game/Narrative/Validate FirstLaunch Environment Audio Imports")]
        public static void ValidateEnvironmentAudioImports()
        {
            foreach (string clipId in StreamingLoopIds)
                Validate(clipId, streaming: true);
            foreach (string clipId in ResidentEventIds)
                Validate(clipId, streaming: false);
        }

        public static string GetAssetPath(string clipId) => $"{EnvironmentRoot}/{clipId}.wav";

        private static void Configure(string clipId, bool streaming)
        {
            string assetPath = GetAssetPath(clipId);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException($"Audio importer not found for {assetPath}.");

            importer.forceToMono = !streaming;
            importer.loadInBackground = streaming;
            importer.ambisonic = false;
            importer.userData = BuildUserData(clipId, streaming);

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            settings.preloadAudioData = !streaming;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void Validate(string clipId, bool streaming)
        {
            string assetPath = GetAssetPath(clipId);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (importer == null || clip == null)
                throw new InvalidOperationException($"Imported AudioClip not found for {assetPath}.");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Require(importer.forceToMono == !streaming, assetPath, "Force To Mono does not match the import profile");
            Require(importer.loadInBackground == streaming, assetPath, "Load In Background does not match the import profile");
            Require(!importer.ambisonic, assetPath, "Ambisonic must be disabled");
            Require(settings.loadType == (streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad), assetPath, "Load Type does not match the import profile");
            Require(settings.compressionFormat == AudioCompressionFormat.Vorbis, assetPath, "Compression Format must be Vorbis");
            Require(Mathf.Abs(settings.quality - VorbisQuality) < 0.0001f, assetPath, $"Vorbis quality must be {VorbisQuality:0.0}");
            Require(settings.preloadAudioData == !streaming, assetPath, "Preload Audio Data does not match the import profile");
            Require(settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate, assetPath, "Sample rate must be preserved");
            Require(clip.frequency == 44100, assetPath, "Imported sample rate must be 44100 Hz");
            Require(streaming || clip.channels == 1, assetPath, "Resident event cues must be mono");
            Require(importer.userData == BuildUserData(clipId, streaming), assetPath, "Rights/runtime generation metadata is missing");
        }

        private static string BuildUserData(string clipId, bool streaming)
        {
            string profile = streaming ? "streaming" : "resident-event";
            return $"clipId={clipId}; status={RightsStatus}; runtimeNetworkGeneration=false; profile={profile}";
        }

        private static void Require(bool condition, string assetPath, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"{assetPath}: {message}.");
        }
    }
}
