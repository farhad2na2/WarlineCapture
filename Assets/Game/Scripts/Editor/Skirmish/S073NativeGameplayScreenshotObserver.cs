#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Opt-in passive screenshots of the actual S073 Playing GameView.</summary>
    [InitializeOnLoad]
    internal static class S073NativeGameplayScreenshotObserver
    {
        const string OutputKey = "WARLINE_S073_NATIVE_SCREENSHOT_OUTPUT";
        const string FramesKey = "WARLINE_S073_NATIVE_SCREENSHOT_FRAMES";
        const string MapId = "opmap.skirmish.industrial_basin";
        const string SourceMapId = "opmap.skirmish.desert_base_01";
        const string SourceIdentity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        const string SourceContent = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        const string SourceSceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        static int[] frameOffsets;
        static readonly HashSet<int> CapturedOffsets = new();
        static readonly JArray Shots = new();
        static bool done;
        static World observedWorld;
        static string observedSession;
        static int firstPlayingFrame = -1;
        static string outputPrefix;

        static S073NativeGameplayScreenshotObserver()
        {
            string configured = Environment.GetEnvironmentVariable(OutputKey);
            if (string.IsNullOrWhiteSpace(configured)) return;
            try
            {
                frameOffsets = ReadFrameOffsets();
                outputPrefix = Path.GetFullPath(configured);
                Require(outputPrefix.StartsWith("/private/tmp/", StringComparison.Ordinal),
                    "Screenshot output prefix must be under /private/tmp");
                foreach (int offset in frameOffsets)
                    Require(!File.Exists(ShotPath(offset)), "Screenshot output already exists: " + ShotPath(offset));
                Require(!File.Exists(outputPrefix + ".json"), "Screenshot manifest already exists: " + outputPrefix + ".json");
                EditorApplication.update += Observe;
                Debug.Log("[S073NativeGameplayScreenshot] armed offsets=" + string.Join(",", frameOffsets) + " prefix=" + outputPrefix);
            }
            catch (Exception error)
            {
                done = true;
                Debug.LogError("[S073NativeGameplayScreenshot] result=Failed reason=" + error.Message);
            }
        }

        static void Observe()
        {
            if (done || !EditorApplication.isPlaying) return;
            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                EntityManager em = world.EntityManager;
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1) return;
                SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.Phase != SkirmishPhase.Playing) return;
                using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                if (maps.CalculateEntityCount() != 1) return;
                Entity map = maps.GetSingletonEntity();
                ActiveOperationMapComponent active = em.GetComponentData<ActiveOperationMapComponent>(map);
                OperationMapMetadataComponent metadata = em.GetComponentData<OperationMapMetadataComponent>(map);
                if (!metadata.Blob.IsCreated || metadata.PhysicalSourceValidated == 0 ||
                    active.OperationMapId.ToString() != MapId ||
                    metadata.Blob.Value.SourceOperationMapId.ToString() != SourceMapId ||
                    metadata.Blob.Value.SourceIdentityHash.ToString() != SourceIdentity ||
                    metadata.Blob.Value.SourceContentHash.ToString() != SourceContent ||
                    metadata.Blob.Value.Navigation.AuthoredSubSceneGuid.ToString() != SourceSceneGuid) return;

                string session = match.SessionId.ToString();
                if (observedWorld == null)
                {
                    observedWorld = world;
                    observedSession = session;
                    firstPlayingFrame = Time.frameCount;
                }
                Require(ReferenceEquals(world, observedWorld) && session == observedSession,
                    "S073 world/session changed during screenshot capture");

                int elapsed = Time.frameCount - firstPlayingFrame;
                foreach (int offset in frameOffsets)
                {
                    if (CapturedOffsets.Contains(offset) || elapsed < offset) continue;
                    CaptureFrame(em, match, offset, elapsed);
                    CapturedOffsets.Add(offset);
                }
                if (CapturedOffsets.Count != frameOffsets.Length) return;

                JObject manifest = new()
                {
                    ["kind"] = "S073NativeGameplayScreenshots",
                    ["schema_version"] = 1,
                    ["scope"] = "PassiveGameViewCaptures_SamePlayingSession_NoInput_NoReadinessClaim",
                    ["unity_version"] = Application.unityVersion,
                    ["world_name"] = world.Name,
                    ["world_sequence"] = world.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                    ["session_id"] = observedSession,
                    ["scenario_index"] = match.ScenarioIndex,
                    ["seed"] = match.Seed,
                    ["phase"] = match.Phase.ToString(),
                    ["operation_map_id"] = active.OperationMapId.ToString(),
                    ["source_map_id"] = SourceMapId,
                    ["source_identity_hash"] = SourceIdentity,
                    ["source_content_hash"] = SourceContent,
                    ["source_scene_guid"] = SourceSceneGuid,
                    ["first_playing_frame"] = firstPlayingFrame,
                    ["shots"] = Shots,
                    ["mission_ready"] = false,
                    ["source_fit_certified"] = false,
                    ["player_readiness"] = false
                };
                File.WriteAllText(outputPrefix + ".json", manifest.ToString(Formatting.Indented) + "\n");
                done = true;
                EditorApplication.update -= Observe;
                Debug.Log("[S073NativeGameplayScreenshot] result=Captured shots=" + Shots.Count + " manifest=" + outputPrefix + ".json");
            }
            catch (Exception error)
            {
                done = true;
                EditorApplication.update -= Observe;
                Debug.LogException(error);
                Debug.LogError("[S073NativeGameplayScreenshot] result=Failed reason=" + error.Message);
            }
        }

        static void CaptureFrame(EntityManager em, SkirmishMatchState match, int offset, int elapsed)
        {
            string path = ShotPath(offset);
            Require(!File.Exists(path), "Screenshot output appeared after arming: " + path);
            Camera camera = Camera.main;
            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            Require(texture != null && texture.width > 0 && texture.height > 0,
                "GameView screenshot texture unavailable");
            try
            {
                byte[] png = texture.EncodeToPNG();
                Require(png != null && png.Length > 8, "Screenshot PNG encoding failed");
                File.WriteAllBytes(path, png);
                Shots.Add(new JObject
                {
                    ["requested_frame_offset"] = offset,
                    ["captured_playing_frame_offset"] = elapsed,
                    ["unity_frame"] = Time.frameCount,
                    ["screen_width"] = texture.width,
                    ["screen_height"] = texture.height,
                    ["camera_name"] = camera != null ? camera.name : null,
                    ["camera_position"] = camera != null ? Vector(camera.transform.position) : null,
                    ["camera_rotation_euler"] = camera != null ? Vector(camera.transform.eulerAngles) : null,
                    ["path"] = path
                });
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }

            using var sessions = em.CreateEntityQuery(typeof(SkirmishMatchState));
            Require(sessions.CalculateEntityCount() == 1 &&
                sessions.GetSingleton<SkirmishMatchState>().SessionId.ToString() == observedSession &&
                sessions.GetSingleton<SkirmishMatchState>().Phase == match.Phase,
                "Screenshot capture session identity/state changed");
        }

        static int[] ReadFrameOffsets()
        {
            string raw = Environment.GetEnvironmentVariable(FramesKey);
            if (string.IsNullOrWhiteSpace(raw)) return new[] { 0 };
            var values = new SortedSet<int>();
            foreach (string token in raw.Split(','))
            {
                if (!int.TryParse(token.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int frame) || frame < 0)
                    throw new InvalidOperationException("Frame offsets must be comma-separated nonnegative integers");
                values.Add(frame);
            }
            if (values.Count == 0 || values.Count > 20)
                throw new InvalidOperationException("Specify between one and twenty distinct frame offsets");
            return values.ToArray();
        }

        static string ShotPath(int offset) => outputPrefix + "-frame-" + offset.ToString(CultureInfo.InvariantCulture) + ".png";
        static JArray Vector(Vector3 value) => new() { value.x, value.y, value.z };
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
