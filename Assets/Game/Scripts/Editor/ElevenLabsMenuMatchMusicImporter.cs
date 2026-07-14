using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class ElevenLabsMenuMatchMusicImporter
    {
        public const string StagingRoot = "Assets/Game/Audio/GeneratedSource/ElevenLabsMusicRaw";
        private const string MusicRoot = "Assets/Game/Audio/Music";
        private const int SampleRate = 44100;
        private const float CrossfadeSeconds = 3f;
        private const float PeakLimit = 0.79433f;

        private readonly struct TrackSettings
        {
            public readonly string Name;
            public readonly float TargetRms;

            public TrackSettings(string name, float targetRmsDb)
            {
                Name = name;
                TargetRms = Mathf.Pow(10f, targetRmsDb / 20f);
            }
        }

        private static readonly TrackSettings[] Tracks =
        {
            new("music_menu_loop_01", -18f),
            new("music_match_calm_loop_01", -19f),
        };

        [MenuItem("Game/Audio/Convert ElevenLabs Menu And Match Music")]
        public static void Convert()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] stagedPaths = Directory.GetFiles(StagingRoot, "*.mp3", SearchOption.TopDirectoryOnly)
                .Select(NormalizeAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] expectedNames = Tracks.Select(track => track.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] stagedNames = stagedPaths.Select(Path.GetFileNameWithoutExtension).OrderBy(name => name, StringComparer.Ordinal).ToArray();

            if (!stagedNames.SequenceEqual(expectedNames, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Music staging set mismatch. Expected {string.Join(", ", expectedNames)} and found " +
                    $"{string.Join(", ", stagedNames)}.");
            }

            foreach (string stagedPath in stagedPaths)
            {
                TrackSettings settings = Tracks.Single(track => track.Name == Path.GetFileNameWithoutExtension(stagedPath));
                ConfigureStagingImporter(stagedPath);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(stagedPath);
                if (clip == null || !clip.LoadAudioData())
                    throw new InvalidOperationException($"Unable to decode staged music: {stagedPath}");

                float[] source = new float[clip.samples * clip.channels];
                if (!clip.GetData(source, 0))
                    throw new InvalidOperationException($"Unable to read staged music samples: {stagedPath}");

                float[] stereo = EnsureStereo(source, clip.channels);
                float[] resampled = ResampleStereo(stereo, clip.frequency, SampleRate);
                float[] loop = BuildSeamlessLoop(resampled, SampleRate, CrossfadeSeconds);
                Normalize(loop, settings.TargetRms, PeakLimit);

                string destination = $"{MusicRoot}/{settings.Name}.wav";
                WritePcm16StereoWave(destination, loop);
                Debug.Log(
                    $"[ElevenLabsMenuMatchMusicImporter] {destination}: " +
                    $"{loop.Length / 2f / SampleRate:F2}s stereo, seam delta {MeasureLoopSeam(loop):F6}");
            }

            AssetDatabase.DeleteAsset(StagingRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[ElevenLabsMenuMatchMusicImporter] Converted licensed menu and match music loops.");
        }

        private static void ConfigureStagingImporter(string assetPath)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException($"Missing AudioImporter for staged music: {assetPath}");

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }

        private static float[] EnsureStereo(float[] input, int channels)
        {
            if (channels == 2)
                return input;

            int frames = input.Length / Mathf.Max(1, channels);
            float[] output = new float[frames * 2];
            for (int frame = 0; frame < frames; frame++)
            {
                float sum = 0f;
                for (int channel = 0; channel < channels; channel++)
                    sum += input[frame * channels + channel];
                float sample = sum / channels;
                output[frame * 2] = sample;
                output[frame * 2 + 1] = sample;
            }
            return output;
        }

        private static float[] ResampleStereo(float[] input, int sourceRate, int destinationRate)
        {
            if (sourceRate == destinationRate)
                return input;

            int sourceFrames = input.Length / 2;
            int destinationFrames = (int)Math.Round(sourceFrames * (double)destinationRate / sourceRate);
            float[] output = new float[destinationFrames * 2];
            for (int frame = 0; frame < destinationFrames; frame++)
            {
                double sourcePosition = frame * (double)sourceRate / destinationRate;
                int leftFrame = Math.Min((int)Math.Floor(sourcePosition), sourceFrames - 1);
                int rightFrame = Mathf.Min(leftFrame + 1, sourceFrames - 1);
                float blend = (float)(sourcePosition - leftFrame);
                for (int channel = 0; channel < 2; channel++)
                {
                    output[frame * 2 + channel] = Mathf.Lerp(
                        input[leftFrame * 2 + channel],
                        input[rightFrame * 2 + channel],
                        blend);
                }
            }
            return output;
        }

        private static float[] BuildSeamlessLoop(float[] input, int sampleRate, float crossfadeSeconds)
        {
            int sourceFrames = input.Length / 2;
            int crossfadeFrames = Mathf.RoundToInt(crossfadeSeconds * sampleRate);
            if (sourceFrames <= crossfadeFrames * 4)
                throw new InvalidOperationException("Generated music is too short for the loop crossfade.");

            int cutFrame = sourceFrames / 2;
            int firstSegmentFrames = sourceFrames - cutFrame;
            int outputFrames = sourceFrames - crossfadeFrames;
            float[] output = new float[outputFrames * 2];
            int outputFrame = 0;

            for (int frame = cutFrame; frame < sourceFrames - crossfadeFrames; frame++, outputFrame++)
                CopyStereoFrame(input, frame, output, outputFrame);

            for (int frame = 0; frame < crossfadeFrames; frame++, outputFrame++)
            {
                float t = (frame + 0.5f) / crossfadeFrames;
                float outgoingGain = Mathf.Cos(t * Mathf.PI * 0.5f);
                float incomingGain = Mathf.Sin(t * Mathf.PI * 0.5f);
                int outgoingFrame = sourceFrames - crossfadeFrames + frame;
                for (int channel = 0; channel < 2; channel++)
                {
                    output[outputFrame * 2 + channel] =
                        input[outgoingFrame * 2 + channel] * outgoingGain +
                        input[frame * 2 + channel] * incomingGain;
                }
            }

            for (int frame = crossfadeFrames; frame < cutFrame; frame++, outputFrame++)
                CopyStereoFrame(input, frame, output, outputFrame);

            if (outputFrame != outputFrames || firstSegmentFrames <= crossfadeFrames)
                throw new InvalidOperationException("Loop assembly produced an unexpected frame count.");
            return output;
        }

        private static void CopyStereoFrame(float[] source, int sourceFrame, float[] destination, int destinationFrame)
        {
            destination[destinationFrame * 2] = source[sourceFrame * 2];
            destination[destinationFrame * 2 + 1] = source[sourceFrame * 2 + 1];
        }

        private static void Normalize(float[] samples, float targetRms, float peakLimit)
        {
            double squareSum = 0d;
            float peak = 0f;
            foreach (float sample in samples)
            {
                squareSum += sample * sample;
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            float rms = Mathf.Sqrt((float)(squareSum / samples.Length));
            float gain = Mathf.Min(targetRms / Mathf.Max(rms, 0.00001f), peakLimit / Mathf.Max(peak, 0.00001f));
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
        }

        private static float MeasureLoopSeam(float[] samples)
        {
            int last = samples.Length - 2;
            return Mathf.Max(Mathf.Abs(samples[0] - samples[last]), Mathf.Abs(samples[1] - samples[last + 1]));
        }

        private static void WritePcm16StereoWave(string assetPath, float[] samples)
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
                writer.Write((short)2);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2 * sizeof(short));
                writer.Write((short)(2 * sizeof(short)));
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);
                foreach (float sample in samples)
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
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
