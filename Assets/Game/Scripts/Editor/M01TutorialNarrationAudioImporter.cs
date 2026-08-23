using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M01TutorialNarrationAudioImporter
    {
        public const string RightsStatus = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE";
        public const string VoiceId = "Fi9tPTnEcbh3of7hOHC8";
        public const float VorbisQuality = 0.7f;

        public static readonly string[] StableClipPaths =
        {
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_find_squad_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_move_to_cover_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_move_destination_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_confirm_threat_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_attack_target_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/en/tutorial_m01_secure_corridor_aria.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_find_squad_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_move_to_cover_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_move_destination_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_confirm_threat_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_attack_target_aria_fa.wav",
            "Assets/Game/Audio/Voice/Tutorial/fa/tutorial_m01_secure_corridor_aria_fa.wav"
        };

        [MenuItem("Game/Audio/Configure M01 Tutorial ARIA Voice Imports")]
        public static void ConfigureImports()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for (int i = 0; i < StableClipPaths.Length; i++)
                ConfigureImport(StableClipPaths[i], i >= 6 ? "fa-IR" : "en-US");
            ValidateImports();
            Debug.Log($"[M01TutorialNarrationAudioImporter] result=Passed clips={StableClipPaths.Length}");
        }

        public static void ValidateImports()
        {
            for (int i = 0; i < StableClipPaths.Length; i++)
            {
                string assetPath = StableClipPaths[i];
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
                Require(importer.userData.IndexOf(RightsStatus, StringComparison.Ordinal) >= 0, assetPath, "rights metadata");
                Require(importer.userData.IndexOf(VoiceId, StringComparison.Ordinal) >= 0, assetPath, "voice metadata");
            }
        }

        private static void ConfigureImport(string assetPath, string locale)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing AudioImporter for M01 tutorial voice: {assetPath}");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
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
            importer.userData =
                $"status={RightsStatus}; provider=ElevenLabs; model=eleven_v3; voiceId={VoiceId}; " +
                $"locale={locale}; runtimeNetworkTts=false";
            importer.SaveAndReimport();
        }

        private static void Require(bool condition, string assetPath, string requirement)
        {
            if (!condition)
                throw new InvalidOperationException($"{assetPath}: {requirement} does not match the M01 voice profile.");
        }
    }
}
