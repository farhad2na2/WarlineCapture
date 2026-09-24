#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Replaces the northern inherited hillside market with an open courtyard on the existing
    /// continuous sand foundation. No elevated tiles, runtime snapping, or extra ground overlay.
    /// All coordinates are world/grid coordinates in the shared authored city presentation.
    /// </summary>
    public static class CityCrossroadsCourtyardRepair
    {
        public const string RootName = "CityCrossroads_NorthCourtyard";
        public static readonly RectInt Area = new(938, 632, 104, 86);
        public const float Grade = 0.01f;
        private const string Evidence = "Design/AgentReports/SkirmishExpansion/CityCrossroadsCourtyard";
        private const string Prefabs = "Assets/Game/GeneratedOperationMaps/DenseCity/PhysicsFreePrefabDefinitions/";

        [MenuItem("Tools/Warline/Skirmish/Replace Northern Shelf With Courtyard")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
            Scene scene = SceneManager.GetSceneByPath(CityCrossroadsShelfGeometryAudit.PresentationScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(CityCrossroadsShelfGeometryAudit.PresentationScenePath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("Save or resolve existing scene edits before applying.");
            if (scene.GetRootGameObjects().Any(g => g.name == RootName))
                throw new InvalidOperationException("Courtyard already authored; refusing a duplicate application.");

            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var remove = new HashSet<GameObject>();
            foreach (var t in all)
            {
                bool authoredChild = t.parent != null && t.parent.name.StartsWith("__SourceTransform__", StringComparison.Ordinal);
                bool generatedDecoration = t.parent != null && t.parent.name == "Infrastructure";
                bool building = t.GetComponent<OperationMapBuildingAuthoring>() != null;
                if (!authoredChild && !generatedDecoration && !building) continue;
                var renderers = t.GetComponentsInChildren<MeshRenderer>(true);
                if (renderers.Length == 0) continue;
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                // Preserve the single map-wide ground and the major through-roads.
                if (bounds.size.x * bounds.size.z > 10000f) continue;
                if (!Inside(t.position) && !Inside(bounds.center)) continue;
                remove.Add(t.gameObject);
            }
            var top = remove.Where(g => !g.transform.GetComponentsInParent<Transform>(true)
                .Skip(1).Any(p => remove.Contains(p.gameObject))).ToArray();
            if (top.Length < 100) throw new InvalidOperationException("Northern cluster selection unexpectedly small.");

            Directory.CreateDirectory(Evidence);
            File.WriteAllLines(Path.Combine(Evidence, "removed-objects.txt"), top.Select(g =>
                $"{CityCrossroadsShelfGeometryAudit.GetScenePath(g.transform)} position={g.transform.position}"));
            int buildings = top.Count(g => g.GetComponent<OperationMapBuildingAuthoring>() != null);
            foreach (var g in top) Undo.DestroyObjectImmediate(g);

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create grounded courtyard");
            CreateDressing(root);
            RebuildSurface(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Courtyard scene save failed.");
            AssetDatabase.SaveAssets();
            CityCrossroadsSurfaceRebake.RefreshSurfaceMetadata();
            File.WriteAllText(Path.Combine(Evidence, "authoring.txt"),
                $"Removed objects={top.Length}; buildings={buildings}; dressing={root.transform.childCount}\n" +
                $"Footprint={Area}; existing continuous ground grade={Grade}; no added ground slabs.\n");
            RebuildPresentation();
            Debug.Log($"[CityCrossroadsCourtyard] result=Applied removed={top.Length} buildings={buildings}");
        }

        private static void CreateDressing(GameObject root)
        {
            // Asymmetric paired groves leave a 22m clear north/south route and a broad cross-route.
            var palms = new[] { new Vector2(950,646), new Vector2(956,658), new Vector2(950,682),
                new Vector2(958,700), new Vector2(969,707), new Vector2(1028,642),
                new Vector2(1025,657), new Vector2(1030,684), new Vector2(1021,701), new Vector2(1032,708) };
            for (int i = 0; i < palms.Length; i++)
                Dress(root.transform, "SM_Env_Tree_01_e0eaa3a1.prefab", "Palm", palms[i], 2f + (i % 3) * .18f, i * 47f, 1.0f);
            var rocks = new[] {new Vector2(948,643),new Vector2(953,684),new Vector2(965,709),
                new Vector2(1031,645),new Vector2(1028,688),new Vector2(1025,709)};
            for (int i = 0; i < rocks.Length; i++)
                Dress(root.transform, "SM_Env_Rock_02_a1f63368.prefab", "Rock", rocks[i], .9f, i * 71f, -1f);
            var walls = new[] { new Vector2(962,642), new Vector2(973,642), new Vector2(1015,638),
                new Vector2(1034,667),new Vector2(946,694),new Vector2(973,710),new Vector2(1012,710) };
            for (int i = 0; i < walls.Length; i++)
                Dress(root.transform, "SM_Bld_Village_Wall_02_36d09b1b.prefab", "LowRuinedWall", walls[i], 1.4f,
                    i == 3 || i == 4 ? 90f : 0f, -1f, .48f);
        }

        public static void RefreshDressing()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
            var scene = SceneManager.GetSceneByPath(CityCrossroadsShelfGeometryAudit.PresentationScenePath);
            var root = scene.GetRootGameObjects().Single(g => g.name == RootName);
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
            CreateDressing(root);
            RebuildSurface(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Courtyard scene save failed.");
            AssetDatabase.SaveAssets();
            CityCrossroadsSurfaceRebake.RefreshSurfaceMetadata();
            RebuildPresentation();
            Debug.Log("[CityCrossroadsCourtyard] result=DressingRefreshed");
        }

        private static bool Inside(Vector3 p) => p.x >= Area.xMin && p.x < Area.xMax && p.z >= Area.yMin && p.z < Area.yMax;

        private static void Dress(Transform parent, string prefab, string label, Vector2 position,
            float scale, float yaw, float trunkRadius, float heightScale = 1f)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + prefab);
            if (source == null) throw new InvalidOperationException("Missing courtyard asset: " + prefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            go.name = label + "_" + parent.childCount.ToString("00");
            go.transform.localScale = new Vector3(scale, scale * heightScale, scale);
            go.transform.SetPositionAndRotation(new Vector3(position.x, 0, position.y), Quaternion.Euler(0,yaw,0));
            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            float bottom = renderers.Min(r => r.bounds.min.y);
            go.transform.position += Vector3.up * (Grade - bottom);
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
            // Palms block the trunk only; walls and rocks block their actual footprint, never their shadow.
            if (trunkRadius > 0) b = new Bounds(new Vector3(position.x, Grade, position.y),new Vector3(trunkRadius*2,1,trunkRadius*2));
            var blocker = go.AddComponent<StaticGridBlockerAuthoring>();
            var so = new SerializedObject(blocker);
            int x = Mathf.FloorToInt(b.min.x), z = Mathf.FloorToInt(b.min.z);
            so.FindProperty("cell").vector2IntValue = new Vector2Int(x,z);
            so.FindProperty("size").vector2IntValue = new Vector2Int(Mathf.CeilToInt(b.max.x)-x,Mathf.CeilToInt(b.max.z)-z);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RebuildSurface(GameObject courtyard)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(CityCrossroadsSurfaceRebake.SurfaceAssetPath);
            if (!asset.TryCreateRuntimeBlobAsset(Allocator.Temp, out var oldBlob)) throw new InvalidOperationException("No surface blob.");
            try
            {
                ref var old = ref oldBlob.Value;
                if (!MapSurfaceBlobAccess.IsCompactSingleLayer(ref old) || old.CellSize != 1f || !old.GridOrigin.Equals(float3.zero))
                    throw new InvalidOperationException("Unexpected courtyard grid coordinate system.");
                using var builder = new BlobBuilder(Allocator.Temp);
                ref var next = ref builder.ConstructRoot<MapSurfaceBlob>();
                next.GridOrigin=old.GridOrigin; next.CellSize=old.CellSize; next.Dimensions=old.Dimensions;
                next.RuntimeEncoding=old.RuntimeEncoding; next.CompactMinHeight=old.CompactMinHeight; next.CompactHeightStep=old.CompactHeightStep;
                builder.Allocate(ref next.Cells,0); builder.Allocate(ref next.Samples,0); builder.Allocate(ref next.Connections,0);
                var samples=builder.Allocate(ref next.CompactSamples,old.CompactSamples.Length);
                for (int i=0;i<samples.Length;i++) samples[i]=old.CompactSamples[i];
                ushort height=(ushort)Mathf.Clamp(Mathf.RoundToInt((Grade-old.CompactMinHeight)/old.CompactHeightStep),0,65535);
                for(int z=Area.yMin;z<Area.yMax;z++) for(int x=Area.xMin;x<Area.xMax;x++)
                {
                    int i=x+z*old.Dimensions.x;
                    var sample=samples[i];
                    sample.PackedHeight=height; sample.LayerId=0;
                    sample.MovementMask=MapSurfaceMovementMask.AllGroundUnits|MapSurfaceMovementMask.AirGrounded|MapSurfaceMovementMask.BuildingPlacement;
                    sample.Flags=MapSurfaceFlags.None; sample.SurfaceType=MapSurfaceType.Terrain;
                    sample.NormalX=0; sample.NormalY=127; sample.NormalZ=0;
                    samples[i]=sample;
                }
                // Navigation blockage for new dressing is also authored as ECS static blockers.
                foreach(var blocker in courtyard.GetComponentsInChildren<StaticGridBlockerAuthoring>())
                {
                    var so=new SerializedObject(blocker);
                    var cell=so.FindProperty("cell").vector2IntValue; var size=so.FindProperty("size").vector2IntValue;
                    for(int z=cell.y;z<cell.y+size.y;z++) for(int x=cell.x;x<cell.x+size.x;x++)
                    {
                        if(!Area.Contains(new Vector2Int(x,z))) throw new InvalidOperationException("Dressing escapes courtyard.");
                        int i=x+z*old.Dimensions.x; var sample=samples[i]; sample.MovementMask=MapSurfaceMovementMask.None; samples[i]=sample;
                    }
                }
                using var result=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Temp);
                asset.ConfigureBakedSurface(Vector3.zero,1,new Vector2Int(old.Dimensions.x,old.Dimensions.y),result,asset.GeneratedFlatEquivalent);
                if(!asset.TryCreateRuntimeBlobAsset(Allocator.Temp,out var check)) throw new InvalidOperationException("Cannot verify surface.");
                using(check)
                {
                    for(int i=0;i<old.CompactSamples.Length;i++)
                        if(!Area.Contains(new Vector2Int(i%old.Dimensions.x,i/old.Dimensions.x)) && !old.CompactSamples[i].Equals(check.Value.CompactSamples[i]))
                            throw new InvalidOperationException("Surface changed outside courtyard.");
                }
                EditorUtility.SetDirty(asset);
            }
            finally { oldBlob.Dispose(); }
        }

        public static void RunFocusedValidation()
        {
            var scene=EditorSceneManager.OpenScene(CityCrossroadsShelfGeometryAudit.PresentationScenePath);
            var root=scene.GetRootGameObjects().Single(g=>g.name==RootName);
            if(root.transform.childCount!=23) throw new InvalidOperationException("Courtyard dressing count changed.");
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
                if(Mathf.Abs(r.bounds.min.y-Grade)>.025f) throw new InvalidOperationException("Floating courtyard prop: "+r.name);
            var asset=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(CityCrossroadsSurfaceRebake.SurfaceAssetPath);
            if(!asset.TryCreateRuntimeBlobAsset(Allocator.Temp,out var blob)) throw new InvalidOperationException("Missing surface.");
            int checkedCells=0;
            using(blob)
            {
                ref var value=ref blob.Value;
                for(int z=Area.yMin;z<Area.yMax;z++) for(int x=Area.xMin;x<Area.xMax;x++)
                {
                    var sample=value.CompactSamples[x+z*value.Dimensions.x];
                    float y=value.CompactMinHeight+sample.PackedHeight*value.CompactHeightStep;
                    if(Mathf.Abs(y-Grade)>.02f) throw new InvalidOperationException($"Courtyard surface mismatch {x},{z}: {y}");
                    if(x>=980 && x<=1002 && (sample.MovementMask&MapSurfaceMovementMask.AllGroundUnits)!=MapSurfaceMovementMask.AllGroundUnits)
                        throw new InvalidOperationException($"Blocked central route {x},{z}");
                    checkedCells++;
                }
            }
            foreach(var b in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)))
                if(Inside(b.transform.position)) throw new InvalidOperationException("Old building remains: "+b.name);
            Debug.Log($"[CityCrossroadsCourtyardValidation] result=Passed cells={checkedCells} dressing=23 centralRouteWidth=23");
        }

        public static void RebuildPresentation()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
            OperationMapRenderEligibilityInventoryProbe.Run();
            OperationMapRenderDatabaseBuilder.Run();
            var scene = EditorSceneManager.OpenScene(CityCrossroadsShelfGeometryAudit.PresentationScenePath);
            var generated = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true)).ToArray();
            var accepted = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true)).ToArray();
            if (generated.Length != 36411 || accepted.Length != 8568)
                throw new InvalidOperationException("Unexpected courtyard identity inventory.");
            var roles = generated.Select(g => g.Role).Concat(accepted.Select(g => g.Role)).ToArray();
            var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true))
                .Single(g => g.Role == OperationMapEntityPresentationRole.GameplayBuildings);
            var serialized = new SerializedObject(root);
            serialized.FindProperty("expectedGameplayBuildingCount").intValue = roles.Count(r => r == OperationMapEntityPresentationRole.GameplayBuildings);
            serialized.FindProperty("expectedGameplayVehicleCount").intValue = roles.Count(r => r == OperationMapEntityPresentationRole.GameplayVehicles);
            serialized.FindProperty("expectedRenderOnlyCount").intValue = roles.Count(r => r == OperationMapEntityPresentationRole.RenderOnly);
            serialized.FindProperty("expectedGeneratedIdentityCount").intValue = generated.Length;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[CityCrossroadsCourtyardPresentation] result=Passed");
        }

        // Run through invoke_unity_macos.sh on a disposable copy; captures never save scene state.
        public static void ValidateAndCapture()
        {
            RunFocusedValidation();
            Directory.CreateDirectory(Evidence);
            var cameraObject = new GameObject("CourtyardEvidenceCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 4000f;
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.68f,.64f,.53f);
            var sunObject = new GameObject("CourtyardEvidenceSun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(1,.96f,.88f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48,138,0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.42f,.44f,.49f);
            var capture = typeof(CityCrossroadsShelfGeometryAudit).GetMethod("CaptureCamera",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var positions = new[] {new Vector3(990,72,587),new Vector3(925,14,675),new Vector3(1055,16,688)};
            var names = new[] {"courtyard-overview.png","courtyard-west-join.png","courtyard-east-join.png"};
            try
            {
                for (int i=0;i<positions.Length;i++)
                {
                    camera.transform.position = positions[i];
                    camera.transform.LookAt(new Vector3(990,0,678));
                    capture.Invoke(null,new object[] {camera, Path.Combine(Evidence,names[i])});
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sunObject);
            }
            Debug.Log("[CityCrossroadsCourtyardCapture] result=Passed views=3");
        }
    }
}
#endif
