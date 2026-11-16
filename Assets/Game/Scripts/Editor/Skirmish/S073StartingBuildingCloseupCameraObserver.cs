#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Opt-in diagnostic closeups from a temporary camera. It does not issue gameplay input
    /// or write gameplay entities/components. Captures are not player-camera or readiness evidence.
    /// </summary>
    [InitializeOnLoad]
    internal static class S073StartingBuildingCloseupCameraObserver
    {
        const string OutputKey = "WARLINE_S073_BUILDING_CLOSEUP_CAMERA_OUTPUT";
        const string MapId = "opmap.skirmish.industrial_basin";
        const string SourceMapId = "opmap.skirmish.desert_base_01";
        const string SourceIdentity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        const string SourceContent = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        const string SceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        const string BoundScenePath = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        const string MapAssetPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
        const string ForcesAssetPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset";
        const string ManifestPath = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";
        const string SurfaceAssetPath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        const int ScenarioIndex = 3;
        const uint Seed = 104729;
        const int ExpectedF1Count = 5;
        const int Width = 1280;
        const int Height = 800;
        const float TransformEpsilon = .0001f;
        static bool done;
        static World stableWorld;
        static string stableSession;
        static string stableSignature;
        static int stableFrames;
        static int lastObservedFrame = -1;
        static int firstPlayingFrame = -1;

        static S073StartingBuildingCloseupCameraObserver()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey)))
                EditorApplication.update += Observe;
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
                if (match.ScenarioIndex != ScenarioIndex || match.Seed != Seed || match.Phase != SkirmishPhase.Playing) return;
                if (firstPlayingFrame < 0) firstPlayingFrame = Time.frameCount;
                // Allow the streamed scene to render before capturing diagnostic views.
                if (Time.frameCount - firstPlayingFrame < 180) return;
                string session = match.SessionId.ToString();
                if (stableWorld == null) { stableWorld = world; stableSession = session; }
                Require(ReferenceEquals(world, stableWorld) && session == stableSession, "World/session changed during closeup observation.");

                using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                using var grids = em.CreateEntityQuery(typeof(GridConfig));
                using var actorsQuery = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<Faction>(),
                        ComponentType.ReadOnly<UnitSourcePrefabKey>(), ComponentType.ReadOnly<LocalTransform>() },
                    None = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>() }
                });
                Require(maps.CalculateEntityCount() == 1 && grids.CalculateEntityCount() == 1, "Unique active map and grid required.");
                Entity mapEntity = maps.GetSingletonEntity();
                ActiveOperationMapComponent active = em.GetComponentData<ActiveOperationMapComponent>(mapEntity);
                OperationMapMetadataComponent metadata = em.GetComponentData<OperationMapMetadataComponent>(mapEntity);
                Require(active.OperationMapId.ToString() == MapId && metadata.Blob.IsCreated && metadata.PhysicalSourceValidated != 0 &&
                    metadata.Blob.Value.SourceOperationMapId.ToString() == SourceMapId &&
                    metadata.Blob.Value.SourceIdentityHash.ToString() == SourceIdentity &&
                    metadata.Blob.Value.SourceContentHash.ToString() == SourceContent &&
                    metadata.Blob.Value.Navigation.AuthoredSubSceneGuid.ToString() == SceneGuid,
                    "Pinned validated S073 map source required.");
                GridConfig grid = em.GetComponentData<GridConfig>(grids.GetSingletonEntity());
                Require(grid.Width == 2048 && grid.Height == 1024 && grid.CellSize == 1f, "Expected native S073 grid required.");
                SkirmishPresetConfig preset = SkirmishPresetResolver.Load(em);
                Require(preset.buildingPlacement != null && preset.buildingPlacement.InitialUnitsConfig != null, "Selected preset has no initial building rows.");

                var f1Expected = new List<ExpectedRow>();
                foreach (var faction in preset.buildingPlacement.InitialUnitsConfig.Factions)
                {
                    if (faction == null || faction.FactionId != 1) continue;
                    foreach (var entry in faction.Buildings)
                    {
                        Require(entry != null && entry.Prefab != null, "Incomplete faction-1 preset row.");
                        f1Expected.Add(new ExpectedRow(entry.Prefab.name,
                            new int2(faction.SpawnCell.x + entry.OriginOffset.x, faction.SpawnCell.y + entry.OriginOffset.y)));
                    }
                }
                Require(f1Expected.Count == ExpectedF1Count, "Expected exactly five faction-1 starting buildings.");

                using var actorEntities = actorsQuery.ToEntityArray(Allocator.Temp);
                RuntimeBuildingEntityLink[] links = Resources.FindObjectsOfTypeAll<RuntimeBuildingEntityLink>()
                    .Where(link => link != null && link.gameObject.scene.IsValid() && link.gameObject.scene.isLoaded && link.gameObject.activeInHierarchy)
                    .ToArray();
                Camera playerCamera = ResolvePlayerCamera();
                CameraSnapshot playerBefore = new CameraSnapshot(playerCamera);
                long orderVersionBefore = em.EntityOrderVersion;
                var buildings = new List<BuildingView>();
                var actorsSeen = new HashSet<Entity>();
                foreach (ExpectedRow expected in f1Expected)
                {
                    var candidates = new List<Entity>();
                    foreach (Entity candidate in actorEntities)
                    {
                        RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(candidate);
                        Faction faction = em.GetComponentData<Faction>(candidate);
                        string key = em.GetComponentData<UnitSourcePrefabKey>(candidate).Value.ToString();
                        if (faction.Id == 1 && info.OwnerFactionId == 1 && key.Contains(expected.PrefabName, StringComparison.Ordinal) &&
                            math.all(info.OriginCell == expected.Origin)) candidates.Add(candidate);
                    }
                    Require(candidates.Count == 1, "Preset row did not resolve to one actual faction-1 runtime actor: " + expected.PrefabName);
                    Entity actor = candidates[0];
                    Require(actorsSeen.Add(actor), "One actor matched multiple preset rows: " + expected.PrefabName);
                    RuntimeBuildingCombatInfo infoForActor = em.GetComponentData<RuntimeBuildingCombatInfo>(actor);
                    var actorLinks = links.Where(link => link.Entity == actor).ToArray();
                    Require(actorLinks.Length == 1, "RuntimeBuildingEntityLink is missing or ambiguous: " + expected.PrefabName);
                    GameObject root = actorLinks[0].gameObject;
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy).ToArray();
                    Require(renderers.Length > 0, "Actual visual root has no active renderers: " + expected.PrefabName);
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    LocalTransform transform = em.GetComponentData<LocalTransform>(actor);
                    buildings.Add(new BuildingView(expected, actor, actorLinks[0], infoForActor, transform, root.transform.localToWorldMatrix, bounds, renderers));
                }
                Require(buildings.Count == ExpectedF1Count, "All five F1 visuals must resolve before capture.");
                string signature = Signature(buildings);
                if (signature != stableSignature) { stableSignature = signature; stableFrames = 0; lastObservedFrame = Time.frameCount; return; }
                if (Time.frameCount != lastObservedFrame) { stableFrames++; lastObservedFrame = Time.frameCount; }
                if (stableFrames < 2) return;

                string outputDir = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
                Require(outputDir.StartsWith("/private/tmp/", StringComparison.Ordinal), "Output directory must be under /private/tmp.");
                Require(!Directory.Exists(outputDir) || !Directory.EnumerateFileSystemEntries(outputDir).Any(), "Output directory must be fresh and empty.");
                Directory.CreateDirectory(outputDir);
                var rows = new JArray();
                foreach (BuildingView building in buildings)
                {
                    CameraRig[] rigs = CreateRigs(building, FindNearestTerrainBearing(building));
                    foreach (CameraRig rig in rigs)
                    {
                        string safeName = SafeName(building.Expected.PrefabName);
                        string fileName = "f1-" + safeName + "-" + rig.AngleId + ".png";
                        byte[] png = Render(playerCamera, rig);
                        string path = Path.Combine(outputDir, fileName);
                        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(png, 0, png.Length);
                        rows.Add(new JObject
                        {
                            ["prefab_name"] = building.Expected.PrefabName,
                            ["faction_id"] = 1,
                            ["actor_entity"] = EntityJson(building.Actor),
                            ["runtime_building_entity_link_entity"] = EntityJson(building.Link.Entity),
                            ["visual_root_name"] = building.Link.gameObject.name,
                            ["visual_root_global_id"] = GlobalObjectId.GetGlobalObjectIdSlow(building.Link.gameObject).ToString(),
                            ["visual_root_scene_path"] = building.Link.gameObject.scene.path,
                            ["runtime_origin_cell"] = new JArray(building.Info.OriginCell.x, building.Info.OriginCell.y),
                            ["runtime_footprint_cells"] = new JArray(building.Info.FootprintCells.x, building.Info.FootprintCells.y),
                            ["runtime_actor_position"] = Float3Json(building.ActorTransform.Position),
                            ["visual_bounds_world"] = BoundsJson(building.Bounds),
                            ["angle_id"] = rig.AngleId,
                            ["camera_position"] = Vector3Json(rig.Position),
                            ["camera_rotation_xyzw"] = new JArray(rig.Rotation.x, rig.Rotation.y, rig.Rotation.z, rig.Rotation.w),
                            ["camera_orthographic"] = true,
                            ["camera_orthographic_size"] = rig.OrthographicSize,
                            ["camera_pixel_size"] = new JArray(Width, Height),
                            ["png_file"] = fileName,
                            ["png_sha256"] = HashBytes(png),
                            ["diagnostic_camera"] = true,
                            ["normal_input_playthrough"] = false,
                            ["readiness"] = false
                        });
                    }
                }
                Require(em.EntityOrderVersion == orderVersionBefore, "Entity order changed while capturing images.");
                Require(playerBefore.EqualsCurrent(playerCamera), "Player camera changed during diagnostic rendering.");
                foreach (BuildingView building in buildings)
                {
                    Require(em.Exists(building.Actor) && SameTransform(building.ActorTransform, em.GetComponentData<LocalTransform>(building.Actor)),
                        "F1 runtime actor transform changed while capturing: " + building.Expected.PrefabName);
                    Require(building.Link != null && MatrixNear(building.RootMatrix, building.Link.transform.localToWorldMatrix),
                        "F1 visual root transform changed while capturing: " + building.Expected.PrefabName);
                }
                var result = new JObject
                {
                    ["kind"] = "S073StartingBuildingCloseupCameraObservation",
                    ["schema_version"] = 1,
                    ["scope"] = "OptInIndependentDiagnosticCamera_ExactFiveFaction1PresetRowsToRuntimeActorsAndVisualLinks_NoPlayerCameraOrGameplayInput_NoReadiness",
                    ["unity_version"] = Application.unityVersion,
                    ["world_name"] = world.Name,
                    ["world_sequence"] = world.SequenceNumber.ToString(),
                    ["session_id"] = session,
                    ["scenario_index"] = match.ScenarioIndex,
                    ["seed"] = match.Seed,
                    ["phase"] = match.Phase.ToString(),
                    ["capture_frame"] = Time.frameCount,
                    ["stable_frames_before_capture"] = stableFrames,
                    ["operation_map_id"] = active.OperationMapId.ToString(),
                    ["campaign_source_map_id"] = SourceMapId,
                    ["source_identity_hash"] = SourceIdentity,
                    ["source_content_hash"] = SourceContent,
                    ["source_subscene_guid"] = SceneGuid,
                    ["source_subscene_path"] = BoundScenePath,
                    ["source_pins_sha256"] = SourcePins(),
                    ["player_camera_state_before"] = playerBefore.ToJson(),
                    ["player_camera_state_unchanged"] = true,
                    ["entity_order_version_unchanged_during_capture"] = true,
                    ["f1_actor_transforms_unchanged_during_capture"] = true,
                    ["diagnostic_camera_count"] = 1,
                    ["images"] = rows,
                    ["image_count"] = rows.Count,
                    ["normal_input_playthrough"] = false,
                    ["readiness"] = false
                };
                string manifest = Path.Combine(outputDir, "manifest.json");
                using (var stream = new FileStream(manifest, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream)) writer.Write(result.ToString(Formatting.Indented) + "\n");
                done = true;
                EditorApplication.update -= Observe;
                Debug.Log("[S073BuildingCloseups] result=Recorded images=" + rows.Count + " path=" + outputDir);
            }
            catch (Exception e)
            {
                done = true;
                EditorApplication.update -= Observe;
                Debug.LogError("[S073BuildingCloseups] result=Failed reason=" + e);
            }
        }

        static Camera ResolvePlayerCamera()
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>()
                .Where(camera => camera != null && camera.enabled && camera.gameObject.scene.IsValid() && camera.gameObject.scene.isLoaded &&
                    camera.cameraType == CameraType.Game && camera.gameObject.CompareTag("MainCamera"))
                .ToArray();
            Require(cameras.Length == 1, "Exactly one enabled MainCamera game camera is required.");
            return cameras[0];
        }

        static Vector3 FindNearestTerrainBearing(BuildingView building)
        {
            Vector3 center = building.Bounds.center;
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>()
                .Where(renderer => renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.gameObject.scene.IsValid() && renderer.gameObject.scene.isLoaded)
                .ToArray();
            string[] tokens = { "Mountain", "SandDune", "Ground_Hill", "Ground_Round", "Ground_Square", "Terrain" };
            Renderer nearest = null; float nearestDistance = float.PositiveInfinity;
            foreach (Renderer renderer in renderers)
            {
                string name = renderer.gameObject.name;
                if (!tokens.Any(token => name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                Vector3 closest = renderer.bounds.ClosestPoint(center);
                float distance = Vector2.Distance(new Vector2(closest.x, closest.z), new Vector2(center.x, center.z));
                if (distance < nearestDistance) { nearestDistance = distance; nearest = renderer; }
            }
            if (nearest == null || nearestDistance > 100f) return Vector3.left;
            Vector3 bearing = nearest.bounds.center - center; bearing.y = 0;
            return bearing.sqrMagnitude > .01f ? bearing.normalized : Vector3.left;
        }

        static CameraRig[] CreateRigs(BuildingView building, Vector3 terrainBearing)
        {
            Vector3 center = building.Bounds.center;
            Vector3 size = building.Bounds.size;
            float radius = Mathf.Max(12f, Mathf.Max(size.x, size.z) * 1.6f);
            Vector3 highOffset = new Vector3(radius * .55f, radius * 1.25f, -radius * .75f);
            float highSize = Mathf.Max(size.x, size.z) * .75f + 5f;
            CameraRig overview = Aim("overview", center + highOffset, center, highSize);
            Vector3 horizontalAwayFromTerrain = -terrainBearing.normalized;
            Vector3 lowOffset = horizontalAwayFromTerrain * radius + Vector3.up * Mathf.Max(5f, size.y * .45f);
            Vector3 lowAim = center + Vector3.up * Mathf.Max(1f, size.y * .25f) + terrainBearing.normalized * Mathf.Min(4f, radius * .2f);
            CameraRig edge = Aim("terrain-edge", center + lowOffset, lowAim, Mathf.Max(size.x, size.z) * .6f + 2.5f);
            return new[] { overview, edge };
        }

        static CameraRig Aim(string id, Vector3 position, Vector3 target, float orthoSize)
        {
            Quaternion rotation = Quaternion.LookRotation(target - position, Vector3.up);
            return new CameraRig(id, position, rotation, Mathf.Max(4f, orthoSize));
        }

        static byte[] Render(Camera playerCamera, CameraRig rig)
        {
            Camera camera = null; RenderTexture target = null; Texture2D readback = null;
            RenderTexture priorActive = RenderTexture.active;
            try
            {
                var cameraRoot = new GameObject("S073_DiagnosticCloseupCamera") { hideFlags = HideFlags.HideAndDontSave };
                camera = cameraRoot.AddComponent<Camera>();
                camera.CopyFrom(playerCamera);
                camera.enabled = true;
                camera.targetTexture = null;
                camera.cullingMask = playerCamera.cullingMask;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.orthographic = true;
                camera.orthographicSize = rig.OrthographicSize;
                camera.aspect = (float)Width / Height;
                camera.transform.SetPositionAndRotation(rig.Position, rig.Rotation);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 1000f;
                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1, useMipMap = false, autoGenerateMips = false };
                target.Create();
                var request = new RenderPipeline.StandardRequest { destination = target };
                Require(RenderPipeline.SupportsRenderRequest(camera, request), "Active render pipeline does not support StandardRequest.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                readback = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
                readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                readback.Apply(false, false);
                Color32[] pixels = readback.GetPixels32();
                int lit = 0, min = 255, max = 0; var colorBins = new HashSet<int>();
                foreach (Color32 p in pixels) { int v = Math.Max(p.r, Math.Max(p.g, p.b)); min = Math.Min(min, v); max = Math.Max(max, v); if (v > 12) lit++; colorBins.Add((p.r >> 3) << 10 | (p.g >> 3) << 5 | (p.b >> 3)); }
                Require(lit > pixels.Length / 20 && max - min >= 12 && colorBins.Count >= 8, "URP closeup capture is black, uniform, or unreadable.");
                return readback.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = priorActive;
                if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }

        static string Signature(List<BuildingView> buildings)
        {
            return string.Join("|", buildings.OrderBy(b => b.Expected.PrefabName, StringComparer.Ordinal).Select(b =>
                b.Expected.PrefabName + ":" + b.Actor.Index + ":" + b.Actor.Version + ":" + b.Link.GetEntityId().GetHashCode() + ":" +
                b.Bounds.center.ToString("R") + ":" + b.Bounds.size.ToString("R") + ":" + b.ActorTransform.Position.ToString()));
        }

        static bool SameTransform(LocalTransform a, LocalTransform b) => math.distance(a.Position, b.Position) <= TransformEpsilon &&
            math.abs(math.dot(a.Rotation.value, b.Rotation.value)) >= 1f - TransformEpsilon && math.abs(a.Scale - b.Scale) <= TransformEpsilon;
        static bool MatrixNear(Matrix4x4 a, Matrix4x4 b) { for (int i = 0; i < 16; i++) if (Mathf.Abs(a[i] - b[i]) > TransformEpsilon) return false; return true; }
        static JObject SourcePins()
        {
            var rows = new JObject();
            foreach (string path in new[] { MapAssetPath, ForcesAssetPath, ManifestPath, SurfaceAssetPath, BoundScenePath, BoundScenePath + ".meta" })
            {
                Require(File.Exists(path), "Pinned S073 source asset missing: " + path);
                using var stream = File.OpenRead(path); using var sha = SHA256.Create();
                rows[path] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
            return rows;
        }
        static string SafeName(string value) => string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-'));
        static string HashBytes(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static JObject EntityJson(Entity value) => new JObject { ["index"] = value.Index, ["version"] = value.Version };
        static JArray Vector3Json(Vector3 v) => new JArray(v.x, v.y, v.z);
        static JArray Float3Json(float3 v) => new JArray(v.x, v.y, v.z);
        static JObject BoundsJson(Bounds b) => new JObject { ["min"] = Vector3Json(b.min), ["max"] = Vector3Json(b.max), ["center"] = Vector3Json(b.center), ["size"] = Vector3Json(b.size) };
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        readonly struct ExpectedRow
        {
            public readonly string PrefabName; public readonly int2 Origin;
            public ExpectedRow(string name, int2 origin) { PrefabName = name; Origin = origin; }
        }
        sealed class BuildingView
        {
            public readonly ExpectedRow Expected; public readonly Entity Actor; public readonly RuntimeBuildingEntityLink Link;
            public readonly RuntimeBuildingCombatInfo Info; public readonly LocalTransform ActorTransform; public readonly Matrix4x4 RootMatrix; public readonly Bounds Bounds; public readonly Renderer[] Renderers;
            public BuildingView(ExpectedRow expected, Entity actor, RuntimeBuildingEntityLink link, RuntimeBuildingCombatInfo info, LocalTransform transform, Matrix4x4 rootMatrix, Bounds bounds, Renderer[] renderers)
            { Expected = expected; Actor = actor; Link = link; Info = info; ActorTransform = transform; RootMatrix = rootMatrix; Bounds = bounds; Renderers = renderers; }
        }
        readonly struct CameraRig
        {
            public readonly string AngleId; public readonly Vector3 Position; public readonly Quaternion Rotation; public readonly float OrthographicSize;
            public CameraRig(string id, Vector3 position, Quaternion rotation, float size) { AngleId = id; Position = position; Rotation = rotation; OrthographicSize = size; }
        }
        readonly struct CameraSnapshot
        {
            readonly Vector3 position; readonly Quaternion rotation; readonly bool orthographic; readonly float size; readonly float fov; readonly int cullingMask;
            readonly RenderTexture target; readonly float near; readonly float far; readonly Rect rect;
            public CameraSnapshot(Camera camera) { position = camera.transform.position; rotation = camera.transform.rotation; orthographic = camera.orthographic; size = camera.orthographicSize; fov = camera.fieldOfView; cullingMask = camera.cullingMask; target = camera.targetTexture; near = camera.nearClipPlane; far = camera.farClipPlane; rect = camera.rect; }
            public bool EqualsCurrent(Camera camera) => camera != null && Vector3.Distance(position, camera.transform.position) < TransformEpsilon && Quaternion.Angle(rotation, camera.transform.rotation) < .01f && orthographic == camera.orthographic && Mathf.Abs(size - camera.orthographicSize) < TransformEpsilon && Mathf.Abs(fov - camera.fieldOfView) < TransformEpsilon && cullingMask == camera.cullingMask && target == camera.targetTexture && Mathf.Abs(near - camera.nearClipPlane) < TransformEpsilon && Mathf.Abs(far - camera.farClipPlane) < TransformEpsilon && rect == camera.rect;
            public JObject ToJson() => new JObject { ["position"] = Vector3Json(position), ["rotation_xyzw"] = new JArray(rotation.x, rotation.y, rotation.z, rotation.w), ["orthographic"] = orthographic, ["orthographic_size"] = size, ["field_of_view"] = fov, ["culling_mask"] = cullingMask, ["target_texture_instance_id"] = target != null ? target.GetEntityId().GetHashCode() : 0, ["near_clip"] = near, ["far_clip"] = far, ["rect"] = new JArray(rect.x, rect.y, rect.width, rect.height) };
        }
    }
}
#endif
