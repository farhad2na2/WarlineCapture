using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class AriaMatchVoiceElevenLabsImporter
    {
        public const string StagingRoot = "Assets/Game/Audio/GeneratedSource/ARIAElevenLabsRaw";
        public const string VoiceRoot = "Assets/Game/Audio/Voice/ARIA";
        private const int SampleRate = 44100;
        private const float SilenceThreshold = 0.00178f;
        private const float CompressorThreshold = 0.16f;
        private const float CompressorRatio = 1.8f;
        private const float TargetRms = 0.12589f;
        private const float PeakLimit = 0.79433f;

        [MenuItem("Game/Audio/Convert ElevenLabs ARIA Match Voices")]
        public static void Convert()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] stagedPaths = Directory.GetFiles(StagingRoot, "*.mp3", SearchOption.TopDirectoryOnly)
                .Select(NormalizeAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] expectedNames = Directory.GetFiles(VoiceRoot, "*.wav", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] stagedNames = stagedPaths
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            if (stagedPaths.Length == 0)
                throw new InvalidOperationException($"No staged ElevenLabs MP3 files found in {StagingRoot}.");
            if (!stagedNames.SequenceEqual(expectedNames, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"ARIA staging set mismatch. Expected {expectedNames.Length} WAV names and found " +
                    $"{stagedNames.Length} staged MP3 names.");
            }

            for (int i = 0; i < stagedPaths.Length; i++)
            {
                string stagedPath = stagedPaths[i];
                ConfigureStagingImporter(stagedPath);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(stagedPath);
                if (clip == null)
                    throw new InvalidOperationException($"Unable to load staged ARIA clip: {stagedPath}");
                if (!clip.LoadAudioData())
                    throw new InvalidOperationException($"Unable to decode staged ARIA clip: {stagedPath}");

                float[] interleaved = new float[clip.samples * clip.channels];
                if (!clip.GetData(interleaved, 0))
                    throw new InvalidOperationException($"Unable to read staged ARIA clip samples: {stagedPath}");
                float[] mono = Downmix(interleaved, clip.channels);
                float[] processed = Process(mono, clip.frequency);
                string destination = $"{VoiceRoot}/{Path.GetFileNameWithoutExtension(stagedPath)}.wav";
                WritePcm16Wave(destination, processed);
                Debug.Log($"[AriaMatchVoiceElevenLabsImporter] {i + 1:000}/{stagedPaths.Length:000} {destination}");
            }

            AssetDatabase.DeleteAsset(StagingRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[AriaMatchVoiceElevenLabsImporter] Converted {stagedPaths.Length} licensed ARIA match voice clips.");
        }

        private static void ConfigureStagingImporter(string assetPath)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing AudioImporter for staged ARIA clip: {assetPath}");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = SampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }

        private static float[] Downmix(float[] interleaved, int channels)
        {
            if (channels <= 1)
                return interleaved;

            int frameCount = interleaved.Length / channels;
            float[] mono = new float[frameCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                float sum = 0f;
                int offset = frame * channels;
                for (int channel = 0; channel < channels; channel++)
                    sum += interleaved[offset + channel];
                mono[frame] = sum / channels;
            }
            return mono;
        }

        private static float[] Process(float[] input, int sourceRate)
        {
            if (input == null || input.Length == 0)
                throw new InvalidOperationException("Staged ARIA clip contains no samples.");
            if (sourceRate != SampleRate)
                throw new InvalidOperationException($"Expected {SampleRate} Hz staged ARIA audio, found {sourceRate} Hz.");

            int first = Array.FindIndex(input, sample => Mathf.Abs(sample) > SilenceThreshold);
            int last = Array.FindLastIndex(input, sample => Mathf.Abs(sample) > SilenceThreshold);
            if (first < 0 || last < first)
                throw new InvalidOperationException("Staged ARIA clip contains only silence.");
            first = Mathf.Max(0, first - Mathf.RoundToInt(0.03f * SampleRate));
            last = Mathf.Min(input.Length - 1, last + Mathf.RoundToInt(0.12f * SampleRate));

            float highpassAlpha = 1f / (1f + (2f * Mathf.PI * 90f / SampleRate));
            float lowpassFactor = 2f * Mathf.PI * 14000f / SampleRate;
            float lowpassAlpha = lowpassFactor / (1f + lowpassFactor);
            float previousInput = input[first];
            float previousHighpass = 0f;
            float previousLowpass = 0f;
            float[] output = new float[last - first + 1];
            double squareSum = 0d;
            float peak = 0f;

            for (int sourceIndex = first, outputIndex = 0; sourceIndex <= last; sourceIndex++, outputIndex++)
            {
                float current = input[sourceIndex];
                float highpassed = highpassAlpha * (previousHighpass + current - previousInput);
                float lowpassed = previousLowpass + lowpassAlpha * (highpassed - previousLowpass);
                float magnitude = Mathf.Abs(lowpassed);
                if (magnitude > CompressorThreshold)
                    lowpassed = Mathf.Sign(lowpassed) * (CompressorThreshold + (magnitude - CompressorThreshold) / CompressorRatio);
                output[outputIndex] = lowpassed;
                squareSum += lowpassed * lowpassed;
                peak = Mathf.Max(peak, Mathf.Abs(lowpassed));
                previousInput = current;
                previousHighpass = highpassed;
                previousLowpass = lowpassed;
            }

            float rms = Mathf.Sqrt((float)(squareSum / output.Length));
            float gain = Mathf.Min(TargetRms / Mathf.Max(rms, 0.00001f), PeakLimit / Mathf.Max(peak, 0.00001f));
            for (int i = 0; i < output.Length; i++)
                output[i] = Mathf.Clamp(output[i] * gain, -1f, 1f);
            return output;
        }

        private static void WritePcm16Wave(string assetPath, float[] samples)
        {
            string absolutePath = Path.GetFullPath(assetPath);
            string temporaryPath = absolutePath + ".tmp";
            using (FileStream stream = File.Create(temporaryPath))
            using (BinaryWriter writer = new(stream))
            {
                int dataBytes = samples.Length * sizeof(short);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * sizeof(short));
                writer.Write((short)sizeof(short));
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);
                for (int i = 0; i < samples.Length; i++)
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue));
            }

            File.Copy(temporaryPath, absolutePath, true);
            File.Delete(temporaryPath);
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
