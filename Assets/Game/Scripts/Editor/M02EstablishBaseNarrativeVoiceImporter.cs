using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M02EstablishBaseNarrativeVoiceImporter
    {
        public const string RightsStatus = "ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE";
        public const string EnglishRoot = "Assets/Game/Audio/Narrative/M02EstablishBase/Voice/en";
        public const string PersianRoot = "Assets/Game/Audio/Narrative/M02EstablishBase/Voice/fa";
        public const int ExpectedClipCount = 18;
        public const float VorbisQuality = 0.7f;

        [MenuItem("Game/Campaign/M02/Configure Final Narrative Voice Imports")]
        public static void ConfigureImports()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (M02NarrativeLocalizedLine line in AllLines())
            {
                Configure(GetEnglishClipPath(line.LineId), "en-US", VoiceId(line.Speaker));
                Configure(GetPersianClipPath(line.LineId), "fa-IR", VoiceId(line.Speaker));
            }
            AssetDatabase.SaveAssets();
            ValidateImports();
            Debug.Log(
                $"[M02EstablishBaseNarrativeVoiceImporter] result=Passed clips={ExpectedClipCount} locales=2");
        }

        public static void ValidateImports()
        {
            foreach (M02NarrativeLocalizedLine line in AllLines())
            {
                Validate(GetEnglishClipPath(line.LineId), "en-US", VoiceId(line.Speaker));
                Validate(GetPersianClipPath(line.LineId), "fa-IR", VoiceId(line.Speaker));
            }
        }

        public static string GetEnglishClipPath(string lineId) =>
            $"{EnglishRoot}/{FileStem(lineId)}.wav";

        public static string GetPersianClipPath(string lineId) =>
            $"{PersianRoot}/{FileStem(lineId)}_fa.wav";

        public static IEnumerable<M02NarrativeLocalizedLine> AllLines()
        {
            foreach (M02NarrativeLocalizedLine line in M02EstablishBaseLocalizedText.Brief) yield return line;
            foreach (M02NarrativeLocalizedLine line in M02EstablishBaseLocalizedText.Comms) yield return line;
            foreach (M02NarrativeLocalizedLine line in M02EstablishBaseLocalizedText.Debrief) yield return line;
        }

        private static string FileStem(string lineId) => lineId.Replace('-', '_').Replace('.', '_');

        private static string VoiceId(NarrativeSpeakerId speaker) => speaker switch
        {
            NarrativeSpeakerId.Dalia => "MK1Zvh93428YrgOQ8Obr",
            NarrativeSpeakerId.Samira => "7uxeJ73HfJL9gOH2mttA",
            NarrativeSpeakerId.Aria => "Fi9tPTnEcbh3of7hOHC8",
            _ => throw new InvalidOperationException($"M02 has no approved voice for {speaker}.")
        };

        private static void Configure(string assetPath, string locale, string voiceId)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing M02 narrative voice clip: {assetPath}");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            string metadata =
                $"status={RightsStatus}; provider=ElevenLabs; model=eleven_v3; voiceId={voiceId}; " +
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

        private static void Validate(string assetPath, string locale, string voiceId)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing M02 narrative voice importer: {assetPath}");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.CompressedInMemory ||
                settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                settings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate ||
                settings.preloadAudioData ||
                !importer.forceToMono ||
                !importer.loadInBackground ||
                importer.ambisonic)
            {
                throw new InvalidOperationException($"Invalid M02 narrative voice import settings: {assetPath}");
            }

            string metadata = importer.userData ?? string.Empty;
            if (!metadata.Contains($"status={RightsStatus}", StringComparison.Ordinal) ||
                !metadata.Contains("provider=ElevenLabs", StringComparison.Ordinal) ||
                !metadata.Contains("model=eleven_v3", StringComparison.Ordinal) ||
                !metadata.Contains($"voiceId={voiceId}", StringComparison.Ordinal) ||
                !metadata.Contains($"locale={locale}", StringComparison.Ordinal) ||
                !metadata.Contains("runtimeNetworkTts=false", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Invalid M02 narrative voice rights metadata: {assetPath}");
            }
        }
    }
}
