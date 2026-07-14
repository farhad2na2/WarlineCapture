using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Game.Composition;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeRuntimeVideoCapture
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const int FrameRate = 10;
        private const float ExpectedDurationSeconds = 174f;
        private const float SourceAudioLogoOffsetSeconds = 2.5f;
        private const string FfmpegPath = "/opt/homebrew/bin/ffmpeg";
        private const string EvidenceDirectory = "Design/NarrativeVision/FirstLaunch/evidence/runtime/phase9";
        private const string OutputPath = EvidenceDirectory + "/first_launch_integrated_runtime_review_1920x1080.mp4";
        private const string TimingReportPath = EvidenceDirectory + "/first_launch_integrated_runtime_timing.tsv";
        private const string SourceAudioPath = "Design/NarrativeVision/FirstLaunch/animatic/revision_v2/audio/first_launch_opening_mix.wav";

        [MenuItem("Game/Narrative/First Launch/Capture Integrated Runtime Review Video")]
        public static void Capture()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("Runtime video capture requires a graphics device. Run Unity batch mode without -nographics.");
            if (!File.Exists(FfmpegPath))
                throw new FileNotFoundException("The First Launch capture requires ffmpeg.", FfmpegPath);
            if (!File.Exists(SourceAudioPath))
                throw new FileNotFoundException("The approved voice and ambience mix is missing.", SourceAudioPath);

            FirstLaunchNarrativeConfigBuilder.Build();
            FirstLaunchNarrativePresentationPrefabBuilder.Build();
            Directory.CreateDirectory(EvidenceDirectory);

            string silentVideoPath = Path.Combine(EvidenceDirectory, ".first_launch_integrated_runtime_silent.mp4");
            if (File.Exists(silentVideoPath))
                File.Delete(silentVideoPath);
            if (File.Exists(OutputPath))
                File.Delete(OutputPath);

            CaptureContext context = null;
            try
            {
                context = CreateContext();
                RenderSilentVideo(context, silentVideoPath);
                MuxApprovedAudio(silentVideoPath);
                Debug.Log($"[FirstLaunchNarrativeRuntimeVideoCapture] Captured {OutputPath}");
            }
            finally
            {
                context?.Dispose();
                if (File.Exists(silentVideoPath))
                    File.Delete(silentVideoPath);
                AssetDatabase.Refresh();
            }
        }

        private static CaptureContext CreateContext()
        {
            NarrativeSequenceConfig sequence = RequireAsset<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
            NarrativeSpeakerCatalog speakers = RequireAsset<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath);
            NarrativePunctuationConfig punctuation = RequireAsset<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath);
            GameObject prefab = RequireAsset<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);

            GameObject cameraObject = new("FirstLaunchRuntimeCaptureCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new("FirstLaunchRuntimeCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Unable to instantiate the First Launch narrative prefab.");
            NarrativeSequenceView view = instance.GetComponent<NarrativeSequenceView>();
            if (view == null)
                throw new InvalidOperationException("The First Launch narrative prefab has no NarrativeSequenceView.");

            view.ReviewerControlsView?.SetVisible(false);
            view.SetSafeAreaPreview(false);
            Transform playbackControls = view.transform.Find("SafeArea/PlaybackControls");
            if (playbackControls != null)
                playbackControls.gameObject.SetActive(false);

            UISettingsModel settings = Game.UI.Runtime.SettingsService.Defaults;
            settings.Narrative.AutoAdvance = true;
            settings.Narrative.InstantText = false;
            settings.Narrative.SubtitlesEnabled = true;
            settings.Accessibility.ReducedMotion = false;
            settings.Audio.VoiceEnabled = false;

            FirstLaunchNarrativeSequencePresentationSystemHelper player = new();
            if (!player.Initialize(sequence, speakers, punctuation, view, FallbackGameTextResolver.Instance, settings))
                throw new InvalidOperationException("Unable to initialize the First Launch narrative player.");

            RenderTexture target = new(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                name = "FirstLaunchRuntimeCaptureTarget",
                antiAliasing = 1
            };
            target.Create();
            camera.targetTexture = target;
            Texture2D readback = new(Width, Height, TextureFormat.RGBA32, false, false)
            {
                name = "FirstLaunchRuntimeCaptureReadback"
            };

            return new CaptureContext(cameraObject, canvasObject, instance, camera, target, readback, view, player);
        }

        private static void RenderSilentVideo(CaptureContext context, string outputPath)
        {
            bool handoffReached = false;
            context.Player.HandoffRequested += result =>
            {
                if (result.DestinationId == "first_launch.m01_handoff")
                    handoffReached = true;
            };
            if (!context.Player.Start())
                throw new InvalidOperationException("The First Launch player could not start its entry state.");

            List<StateTransition> transitions = new();
            string lastState = string.Empty;
            float elapsed = 0f;
            float frameDuration = 1f / FrameRate;
            int maximumFrames = Mathf.CeilToInt((ExpectedDurationSeconds + 2f) * FrameRate);
            byte[] frameBytes = new byte[Width * Height * 4];

            using Process encoder = StartFfmpeg(
                $"-nostdin -hide_banner -loglevel error -y -f rawvideo -pixel_format rgba -video_size {Width}x{Height} " +
                $"-framerate {FrameRate} -i - -vf vflip -an -c:v libx264 -preset fast -crf 19 -pix_fmt yuv420p {Quote(outputPath)}",
                redirectInput: true);

            for (int frame = 0; frame < maximumFrames && !handoffReached; frame++)
            {
                BypassLiveInteractiveScreens(context.Player);
                string state = context.Player.CurrentStateId;
                if (!string.Equals(lastState, state, StringComparison.Ordinal))
                {
                    transitions.Add(new StateTransition(state, elapsed));
                    lastState = state;
                }

                Canvas.ForceUpdateCanvases();
                context.Camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = context.Target;
                context.Readback.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0, false);
                context.Readback.Apply(false, false);
                context.Readback.GetRawTextureData<byte>().CopyTo(frameBytes);
                RenderTexture.active = previous;
                encoder.StandardInput.BaseStream.Write(frameBytes, 0, frameBytes.Length);

                context.Player.Tick(frameDuration);
                BypassLiveInteractiveScreens(context.Player);
                elapsed += frameDuration;
            }

            encoder.StandardInput.Close();
            encoder.WaitForExit();
            if (encoder.ExitCode != 0)
                throw new InvalidOperationException($"ffmpeg failed to encode the Unity runtime capture (exit {encoder.ExitCode}).");
            if (!handoffReached)
                throw new InvalidOperationException($"The runtime sequence did not reach the M01 handoff within {maximumFrames} frames.");
            if (Mathf.Abs(elapsed - ExpectedDurationSeconds) > 0.15f)
                throw new InvalidOperationException($"Runtime opening duration was {elapsed:F2}s; expected {ExpectedDurationSeconds:F2}s.");

            WriteTimingReport(transitions, elapsed);
        }

        private static void BypassLiveInteractiveScreens(FirstLaunchNarrativeSequencePresentationSystemHelper player)
        {
            while (player.CurrentStateId == "first_launch.commander_identity" || player.CurrentStateId == "first_launch.guidance_choice")
                player.CommitInteractiveState(player.CurrentStateId);
        }

        private static void MuxApprovedAudio(string silentVideoPath)
        {
            using Process muxer = StartFfmpeg(
                $"-nostdin -hide_banner -loglevel error -y -i {Quote(silentVideoPath)} -ss {SourceAudioLogoOffsetSeconds.ToString(CultureInfo.InvariantCulture)} " +
                $"-i {Quote(SourceAudioPath)} -map 0:v:0 -map 1:a:0 -t {ExpectedDurationSeconds.ToString(CultureInfo.InvariantCulture)} " +
                $"-c:v copy -c:a aac -b:a 192k -movflags +faststart {Quote(OutputPath)}",
                redirectInput: false);
            muxer.WaitForExit();
            if (muxer.ExitCode != 0 || !File.Exists(OutputPath))
                throw new InvalidOperationException($"ffmpeg failed to mux the First Launch voice and ambience mix (exit {muxer.ExitCode}).");
        }

        private static Process StartFfmpeg(string arguments, bool redirectInput)
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FfmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = redirectInput,
                    CreateNoWindow = true
                }
            };
            if (!process.Start())
                throw new InvalidOperationException("Unable to start ffmpeg.");
            return process;
        }

        private static void WriteTimingReport(IReadOnlyList<StateTransition> transitions, float duration)
        {
            using StreamWriter writer = new(TimingReportPath, false);
            writer.WriteLine("state_id\tstart_seconds");
            for (int i = 0; i < transitions.Count; i++)
                writer.WriteLine($"{transitions[i].StateId}\t{transitions[i].StartSeconds.ToString("F2", CultureInfo.InvariantCulture)}");
            writer.WriteLine($"first_launch.m01_handoff\t{duration.ToString("F2", CultureInfo.InvariantCulture)}");
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing required First Launch asset: {path}", path);
            return asset;
        }

        private static string Quote(string value) => $"\"{Path.GetFullPath(value).Replace("\"", "\\\"")}\"";

        private readonly struct StateTransition
        {
            public StateTransition(string stateId, float startSeconds)
            {
                StateId = stateId;
                StartSeconds = startSeconds;
            }

            public string StateId { get; }
            public float StartSeconds { get; }
        }

        private sealed class CaptureContext : IDisposable
        {
            private readonly GameObject cameraObject;
            private readonly GameObject canvasObject;
            private readonly GameObject instance;

            public CaptureContext(
                GameObject cameraObject,
                GameObject canvasObject,
                GameObject instance,
                Camera camera,
                RenderTexture target,
                Texture2D readback,
                NarrativeSequenceView view,
                FirstLaunchNarrativeSequencePresentationSystemHelper player)
            {
                this.cameraObject = cameraObject;
                this.canvasObject = canvasObject;
                this.instance = instance;
                Camera = camera;
                Target = target;
                Readback = readback;
                View = view;
                Player = player;
            }

            public Camera Camera { get; }
            public RenderTexture Target { get; }
            public Texture2D Readback { get; }
            public NarrativeSequenceView View { get; }
            public FirstLaunchNarrativeSequencePresentationSystemHelper Player { get; }

            public void Dispose()
            {
                Player?.Cancel();
                if (Camera != null)
                    Camera.targetTexture = null;
                if (Readback != null)
                    Object.DestroyImmediate(Readback);
                if (Target != null)
                {
                    Target.Release();
                    Object.DestroyImmediate(Target);
                }
                if (instance != null)
                    Object.DestroyImmediate(instance);
                if (canvasObject != null)
                    Object.DestroyImmediate(canvasObject);
                if (cameraObject != null)
                    Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
