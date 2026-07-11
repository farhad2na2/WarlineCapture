using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeAudioImporter
    {
        public const string VoiceRoot = "Assets/Game/Audio/Narrative/FirstLaunch/Voice";
        public const string RightsStatus = "TEMP_INTERNAL_ONLY_DISTRIBUTION_RIGHTS_UNVERIFIED";
        public const float VorbisQuality = 0.7f;

        private static readonly string[] ClipIds =
        {
            "p02_radio",
            "p03_radio",
            "p04_dalia",
            "p04_samira",
            "p05_aria",
            "p06_aria",
            "p07_aria",
            "p09_aria",
            "p10_aria",
            "p11_dalia",
            "p12_samira",
            "p13_aria",
            "p14_commander",
            "p15_dalia",
            "p16_aria",
            "p17_dalia",
            "p18_aria",
        };

        private static readonly IReadOnlyList<string> StableClipIdsView = Array.AsReadOnly(ClipIds);

        public static IReadOnlyList<string> StableClipIds => StableClipIdsView;

        [MenuItem("Game/Narrative/Configure FirstLaunch Temporary Voice Imports")]
        public static void ConfigureTemporaryVoiceImports()
        {
            ValidateAssetSet();

            foreach (string clipId in ClipIds)
            {
                string assetPath = GetAssetPath(clipId);
                AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer == null)
                    throw new InvalidOperationException($"Audio importer not found for {assetPath}.");

                importer.forceToMono = true;
                importer.loadInBackground = true;
                importer.ambisonic = false;
                importer.userData = BuildUserData(clipId);

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = VorbisQuality;
                settings.preloadAudioData = true;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;

                SetNormalizeFalseWhenSupported(importer);
                importer.SaveAndReimport();
            }

            ValidateTemporaryVoiceImports();
            Debug.Log($"Configured {ClipIds.Length} FirstLaunch temporary voice clips.");
        }

        [MenuItem("Game/Narrative/Validate FirstLaunch Temporary Voice Imports")]
        public static void ValidateTemporaryVoiceImports()
        {
            ValidateAssetSet();

            foreach (string clipId in ClipIds)
                ValidateClip(clipId);

            Debug.Log($"Validated {ClipIds.Length} FirstLaunch temporary voice clips.");
        }

        public static string GetAssetPath(string clipId)
        {
            if (!StableClipIdsView.Contains(clipId))
                throw new ArgumentException($"Unknown FirstLaunch voice clip ID '{clipId}'.", nameof(clipId));

            return $"{VoiceRoot}/{clipId}.wav";
        }

        private static void ValidateAssetSet()
        {
            string[] actualClipIds = Directory.GetFiles(VoiceRoot, "*.wav", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            string[] expectedClipIds = ClipIds.OrderBy(value => value, StringComparer.Ordinal).ToArray();

            if (!actualClipIds.SequenceEqual(expectedClipIds, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"FirstLaunch voice asset set mismatch. Expected [{string.Join(", ", expectedClipIds)}], " +
                    $"found [{string.Join(", ", actualClipIds)}].");
            }
        }

        private static void ValidateClip(string clipId)
        {
            string assetPath = GetAssetPath(clipId);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (importer == null || clip == null)
                throw new InvalidOperationException($"Imported AudioClip not found for {assetPath}.");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Require(importer.forceToMono, assetPath, "Force To Mono must be enabled");
            Require(importer.loadInBackground, assetPath, "Load In Background must be enabled");
            Require(!importer.ambisonic, assetPath, "Ambisonic must be disabled");
            Require(settings.loadType == AudioClipLoadType.CompressedInMemory, assetPath, "Load Type must be Compressed In Memory");
            Require(settings.compressionFormat == AudioCompressionFormat.Vorbis, assetPath, "Compression Format must be Vorbis");
            Require(Mathf.Abs(settings.quality - VorbisQuality) < 0.0001f, assetPath, $"Vorbis quality must be {VorbisQuality:0.0}");
            Require(settings.preloadAudioData, assetPath, "Default sample settings must preload audio data");
            Require(settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate, assetPath, "Sample rate must be preserved");
            Require(clip.channels == 1, assetPath, "Imported clip must be mono");
            Require(clip.name == clipId, assetPath, $"AudioClip name must retain stable ID '{clipId}'");
            Require(importer.userData == BuildUserData(clipId), assetPath, "Temporary rights/runtime TTS metadata is missing");

            if (TryGetNormalize(importer, out bool normalize))
                Require(!normalize, assetPath, "Normalize must be disabled when the importer exposes the setting");
        }

        private static string BuildUserData(string clipId)
        {
            return $"clipId={clipId}; status={RightsStatus}; runtimeNetworkTts=false";
        }

        private static void SetNormalizeFalseWhenSupported(AudioImporter importer)
        {
            SerializedObject serializedImporter = new(importer);
            SerializedProperty normalize = FindNormalizeProperty(serializedImporter);
            if (normalize == null)
                return;

            normalize.boolValue = false;
            serializedImporter.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool TryGetNormalize(AudioImporter importer, out bool value)
        {
            SerializedObject serializedImporter = new(importer);
            SerializedProperty normalize = FindNormalizeProperty(serializedImporter);
            value = normalize != null && normalize.boolValue;
            return normalize != null;
        }

        private static SerializedProperty FindNormalizeProperty(SerializedObject serializedImporter)
        {
            return serializedImporter.FindProperty("m_Normalize") ?? serializedImporter.FindProperty("normalize");
        }

        private static void Require(bool condition, string assetPath, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"{assetPath}: {message}.");
        }
    }
}
