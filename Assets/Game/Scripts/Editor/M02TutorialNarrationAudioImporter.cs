using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M02TutorialNarrationAudioImporter
    {
        public const string RightsStatus = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE";
        public const string VoiceId = "Fi9tPTnEcbh3of7hOHC8";
        public const float VorbisQuality = 0.7f;

        public static readonly string[] StableClipPaths =
        {
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m02_open_build_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m02_select_barracks_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m02_place_barracks_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m02_check_cost_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m02_train_rifle_squad_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m02_incoming_patrol_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m02_defend_post_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m02_open_build_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m02_select_barracks_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m02_place_barracks_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m02_check_cost_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m02_train_rifle_squad_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m02_incoming_patrol_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m02_defend_post_aria_fa.wav"
        };

        [MenuItem("Game/Audio/Configure M02 Tutorial ARIA Voice Imports")]
        public static void ConfigureImports()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for (int index = 0; index < StableClipPaths.Length; index++)
                ConfigureImport(StableClipPaths[index], index >= 7 ? "fa-IR" : "en-US");
            ValidateImports();
            Debug.Log($"[M02TutorialNarrationAudioImporter] result=Passed clips={StableClipPaths.Length}");
        }

        public static void ValidateImports()
        {
            foreach (string assetPath in StableClipPaths)
            {
                AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                Require(importer != null, assetPath, "missing AudioImporter");
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                Require(settings.loadType == AudioClipLoadType.CompressedInMemory, assetPath, "load type");
                Require(settings.compressionFormat == AudioCompressionFormat.Vorbis, assetPath, "compression");
                Require(settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate, assetPath, "sample rate");
                Require(importer.forceToMono, assetPath, "force to mono");
                Require(!settings.preloadAudioData, assetPath, "preload disabled");
                Require(importer.loadInBackground, assetPath, "background loading");
                Require(!importer.ambisonic, assetPath, "ambisonic disabled");
                Require(importer.userData.Contains(RightsStatus), assetPath, "rights metadata");
                Require(importer.userData.Contains(VoiceId), assetPath, "voice metadata");
            }
        }

        private static void ConfigureImport(string assetPath, string locale)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing AudioImporter for M02 tutorial voice: {assetPath}");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            string metadata =
                $"status={RightsStatus}; provider=ElevenLabs; model=eleven_v3; voiceId={VoiceId}; " +
                $"locale={locale}; runtimeNetworkTts=false";
            bool changed = settings.loadType != AudioClipLoadType.CompressedInMemory ||
                           settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                           settings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate ||
                           settings.sampleRateOverride != 44100 ||
                           Math.Abs(settings.quality - VorbisQuality) > 0.001f ||
                           settings.preloadAudioData || !importer.forceToMono ||
                           !importer.loadInBackground || importer.ambisonic ||
                           !string.Equals(importer.userData, metadata, StringComparison.Ordinal);
            if (!changed)
                return;

            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.sampleRateOverride = 44100;
            settings.quality = VorbisQuality;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = true;
            importer.ambisonic = false;
            importer.userData = metadata;
            importer.SaveAndReimport();
        }

        private static void Require(bool condition, string assetPath, string requirement)
        {
            if (!condition)
                throw new InvalidOperationException($"{assetPath}: {requirement} does not match the M02 voice profile.");
        }
    }
}
