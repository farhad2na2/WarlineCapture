#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Rendering;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    // Explicitly armed, passive diagnostic. It never creates a World/system, launches a
    // mission, injects input, processes camera requests, or changes gameplay/camera state.
    public static class SkirmishEmptyViewCameraRecorder
    {
        private static World world;
        private static string session;
        private static StreamWriter writer;
        private static double deadline;
        private static int rows, lastFrame = -1;
        private static uint setupHash;
        private static int contentVersion;
        private static double nextSample;

        // Invoke only after a separately authorized normal-input S002 attempt exists.
        // Environment value must equal that live attempt ID; no historical ID fallback.
        public static void ArmCurrentS002()
        {
            if (writer != null) throw new InvalidOperationException("Recorder already armed");
            var candidate = World.DefaultGameObjectInjectionWorld;
            if (candidate == null || !candidate.IsCreated ||
                !Read(candidate.EntityManager, out SkirmishExpandedSessionComponent live) ||
                live.CatalogId.ToString() != "S002" || live.Seed != 104731 || live.IsLegacy != 0 || live.IsCustom != 0 ||
                live.SizeId.ToString() != "Standard" || live.DifficultyId.ToString() != "Regular" ||
                string.IsNullOrEmpty(live.SessionId.ToString()) ||
                Environment.GetEnvironmentVariable("WARLINE_EMPTY_VIEW_ATTEMPT") != live.SessionId.ToString())
                throw new InvalidOperationException("Exact live S002/Standard/Regular/104731 attempt not admitted");
            // Output is always local and unique. No caller-supplied path or overwrite.
            string root = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Logs", "EmptyViewCamera", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            writer = new StreamWriter(new FileStream(Path.Combine(root, "camera.jsonl"),
                FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            world = candidate; session = live.SessionId.ToString();
            setupHash = live.SetupHash; contentVersion = live.ContentVersion;
            rows = 0; lastFrame = -1; nextSample = 0;
            deadline = EditorApplication.timeSinceStartup + 120;
            RenderPipelineManager.endCameraRendering += Observe;
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
            Write(new JObject { ["kind"] = "armed", ["attempt"] = session,
                ["catalog"] = live.CatalogId.ToString(), ["definition"] = live.DefinitionId.ToString(),
                ["contentVersion"] = live.ContentVersion, ["setupHash"] = live.SetupHash,
                ["size"] = live.SizeId.ToString(), ["difficulty"] = live.DifficultyId.ToString(),
                ["seed"] = live.Seed, ["localeVerified"] = false,
                ["scope"] = "Passive endCameraRendering pose/state samples; no pixel, accepted-request, source-fit or mission acceptance" });
            Debug.Log("[EmptyViewCamera] Armed local bounded recorder: " + root);
        }

        private static bool Read<T>(EntityManager em, out T value) where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            if (query.CalculateEntityCount() != 1) { value = default; return false; }
            value = em.GetComponentData<T>(query.GetSingletonEntity()); return true;
        }
        private static bool SameAttempt() => world != null && world.IsCreated &&
            World.DefaultGameObjectInjectionWorld == world &&
            Read(world.EntityManager, out SkirmishExpandedSessionComponent live) &&
            live.SessionId.ToString() == session && live.SetupHash == setupHash && live.ContentVersion == contentVersion;
        private static void Tick()
        {
            if (!SameAttempt()) Finish("AttemptOrWorldChanged");
            else if (EditorApplication.timeSinceStartup >= deadline || rows >= 960) Finish("BoundReached");
        }
        private static JArray V(Vector3 v) => new JArray(v.x, v.y, v.z);
        private static JArray Q(Quaternion q) => new JArray(q.x, q.y, q.z, q.w);
        private static JArray R(Rect r) => new JArray(r.xMin, r.yMin, r.xMax, r.yMax);
        private static JArray M(Matrix4x4 m)
        {
            var values = new JArray();
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) values.Add(m[r, c]);
            return values;
        }
        private static JObject Data<T>(T value) where T : struct => JObject.Parse(JsonUtility.ToJson(value));
        private static void Write(JObject row)
        {
            row["utc"] = DateTime.UtcNow.ToString("O");
            writer.WriteLine(row.ToString(Newtonsoft.Json.Formatting.None));
        }
        private static void Observe(ScriptableRenderContext context, Camera camera)
        {
            if (writer == null) return;
            try
            {
                if (!SameAttempt()) { Finish("AttemptOrWorldChanged"); return; }
                if (rows >= 960 || EditorApplication.timeSinceStartup >= deadline) { Finish("BoundReached"); return; }
                if (Time.frameCount == lastFrame || EditorApplication.timeSinceStartup < nextSample ||
                    !RuntimeCameraReferenceSystem.TryGetWorldCamera(world, out Camera registered) || camera != registered) return;
                lastFrame = Time.frameCount; nextSample = EditorApplication.timeSinceStartup + .125; rows++;
                var em = world.EntityManager;
                var owner = world.GetExistingSystemManaged<RtsCameraSystem>();
                var row = new JObject { ["kind"] = "renderSample", ["frame"] = lastFrame,
                    ["attempt"] = session, ["worldSequence"] = world.Unmanaged.SequenceNumber,
                    ["cameraName"] = camera.name, ["position"] = V(camera.transform.position),
                    ["rotation"] = Q(camera.transform.rotation), ["orthographic"] = camera.orthographic,
                    ["fieldOfView"] = camera.fieldOfView, ["orthographicSize"] = camera.orthographicSize,
                    ["aspect"] = camera.aspect, ["pixelRect"] = R(camera.pixelRect),
                    ["projectionRowMajor"] = M(camera.projectionMatrix),
                    ["worldToCameraRowMajor"] = M(camera.worldToCameraMatrix),
                    ["safeArea"] = R(Screen.safeArea), ["screenWidth"] = Screen.width, ["screenHeight"] = Screen.height,
                    ["hasCameraSystem"] = owner != null, ["pixelsCaptured"] = false,
                    ["acceptedRequestsObserved"] = false, ["groundFootprintPlaneY"] = 0 };
                if (owner != null)
                {
                    row["normalIso"] = owner.NormalIsoModeActive;
                    row["hasBoundary"] = owner.HasGroundBoundary;
                    row["aircraftContext"] = owner.HasAircraftFocusContext;
                    row["placementPreview"] = owner.HasPlacementPreviewPose;
                    row["groundFootprintValid"] = owner.TryGetCameraGroundBounds(camera, out Rect footprint);
                    if ((bool)row["groundFootprintValid"]) row["groundFootprintXZ"] = R(footprint);
                }
                if (Read(em, out TacticalFollowCameraPoseComponent follow)) row["followPose"] = Data(follow);
                if (Read(em, out ActiveOperationMapBoundsComponent bounds)) row["authoredBounds"] = Data(bounds);
                if (Read(em, out ActiveOperationMapComponent map)) row["map"] = new JObject {
                    ["id"] = map.OperationMapId.ToString(), ["generation"] = map.Generation,
                    ["contentVersion"] = map.ContentVersion, ["mission"] = map.MissionId.ToString() };
                if (Read(em, out OperationMapRenderVirtualizationStateComponent slots)) row["residencyState"] = Data(slots);
                if (Read(em, out OperationMapRenderVirtualizationMetricsComponent metrics)) row["residencyMetrics"] = Data(metrics);
                Write(row);
            }
            catch (Exception exception)
            {
                Debug.LogError("[EmptyViewCamera] Diagnostic failed: " + exception); Finish("DiagnosticFailure");
            }
        }
        public static void Stop() => Finish("ExplicitStopOrReloadOrQuit");
        private static void Finish(string reason)
        {
            RenderPipelineManager.endCameraRendering -= Observe;
            EditorApplication.update -= Tick;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            EditorApplication.quitting -= Stop;
            var pending = writer;
            try
            {
                if (pending != null) Write(new JObject { ["kind"] = "stopped", ["reason"] = reason,
                    ["attempt"] = session, ["renderSamples"] = rows, ["lastFrame"] = lastFrame,
                    ["captureComplete"] = false, ["readinessAccepted"] = false });
            }
            finally { writer = null; world = null; session = null; pending?.Dispose(); }
        }
    }
}
#endif
