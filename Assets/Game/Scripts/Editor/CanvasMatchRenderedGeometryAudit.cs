using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Components;
using Game.Configs;
using Game.Composition;

namespace Game.Editor
{
    public static class CanvasMatchRenderedGeometryAudit
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
        private const int TopEntryLimit = 35;

        private static int frameCount;
        private static int deployFrame;
        private static int matchReadyFrame;
        private static int warmupFrames;
        private static bool deploySubmitted;
        private static bool matchReady;
        private static bool completed;
        private static double startedAt;

        private sealed class GeometryBucket
        {
            public string Name;
            public int Renderers;
            public int VisibleRenderers;
            public long Triangles;
            public long VisibleTriangles;
        }

        private struct GeometryEntry
        {
            public string Path;
            public string MeshName;
            public string Owner;
            public int Triangles;
            public bool VisibleInCamera;
            public bool GameObjectRenderer;
        }

        public static void Run()
        {
            try
            {
                RuntimeUiConfig config = AssetDatabase.LoadAssetAtPath<RuntimeUiConfig>(RuntimeUiConfigPath);
                if (config == null)
                    throw new InvalidOperationException($"Missing runtime UI config: {RuntimeUiConfigPath}");

                SetRuntimeUiMode(config, RuntimeUiMode.Canvas);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                warmupFrames = ResolvePositiveInt("WARLINE_GEOMETRY_AUDIT_WARMUP_FRAMES", 900);
                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                frameCount = 0;
                deployFrame = 0;
                matchReadyFrame = 0;
                deploySubmitted = false;
                matchReady = false;
                completed = false;
                startedAt = EditorApplication.timeSinceStartup;

                EditorApplication.update -= Continue;
                EditorApplication.update += Continue;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[RenderedGeometryAudit] result=Failed\n{exception}");
                EditorApplication.Exit(1);
            }
        }

        private static void Continue()
        {
            if (completed || !EditorApplication.isPlaying)
                return;

            try
            {
                frameCount++;
                if (frameCount == 1)
                {
                    startedAt = EditorApplication.timeSinceStartup;
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = -1;
                }

                if (EditorApplication.timeSinceStartup - startedAt > 240d)
                {
                    Complete(false, $"Timed out frame={frameCount} deploy={deploySubmitted} matchReady={matchReady} scene={SceneManager.GetActiveScene().name}");
                    return;
                }

                if (frameCount < 45)
                    return;

                MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
                if (bootstrap == null)
                {
                    Complete(false, "Menu scene is missing MenuBootstrapView.");
                    return;
                }

                bootstrap.ApplyRuntimeUiMode();
                if (bootstrap.UiMode != RuntimeUiMode.Canvas)
                {
                    Complete(false, "Runtime UI mode is not Canvas.");
                    return;
                }

                if (!deploySubmitted)
                {
                    UnityEngine.UI.Button deployButton = FindDeployButton();
                    if (deployButton == null)
                        return;

                    deployButton.onClick.Invoke();
                    deploySubmitted = true;
                    deployFrame = frameCount;
                    return;
                }

                if (!matchReady)
                {
                    MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
                    if (matchScene == null || !SceneManager.GetSceneByName("Match").isLoaded)
                        return;

                    if (!matchScene.GameplayStartComplete && frameCount - deployFrame < 360)
                        return;

                    matchReady = true;
                    matchReadyFrame = frameCount;
                    return;
                }

                if (frameCount - matchReadyFrame < warmupFrames)
                    return;

                Complete(true, BuildAudit());
            }
            catch (Exception exception)
            {
                Complete(false, exception.ToString());
            }
        }

        private static UnityEngine.UI.Button FindDeployButton()
        {
            UnityEngine.UI.Button[] buttons =
                UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude);
            for (int i = 0; i < buttons.Length; i++)
            {
                UnityEngine.UI.Button candidate = buttons[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;

                string objectName = candidate.gameObject.name;
                if (string.Equals(objectName, "DeployCommandButton", StringComparison.Ordinal) ||
                    string.Equals(objectName, "DeployOperationButton", StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string BuildAudit()
        {
            MatchSceneView matchScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(FindObjectsInactive.Exclude);
            Camera worldCamera = matchScene != null ? matchScene.WorldCamera : null;
            if (worldCamera == null)
                return "worldCamera=missing";

            Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(worldCamera);
            List<GeometryEntry> entries = new(1024);
            Dictionary<string, GeometryBucket> buckets = new(StringComparer.Ordinal);
            AppendEcsEntries(frustum, entries, buckets);
            AppendGameObjectEntries(frustum, entries, buckets);

            entries.Sort(static (a, b) =>
            {
                int triangleCompare = b.Triangles.CompareTo(a.Triangles);
                if (triangleCompare != 0)
                    return triangleCompare;

                return string.CompareOrdinal(a.Path, b.Path);
            });

            long totalTriangles = 0;
            long visibleTriangles = 0;
            int visibleRenderers = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                totalTriangles += entries[i].Triangles;
                if (!entries[i].VisibleInCamera)
                    continue;

                visibleRenderers++;
                visibleTriangles += entries[i].Triangles;
            }

            StringBuilder builder = new(8192);
            builder.Append("warmupFrames=");
            builder.Append(warmupFrames);
            builder.Append(" renderers=");
            builder.Append(entries.Count);
            builder.Append(" visibleRenderers=");
            builder.Append(visibleRenderers);
            builder.Append(" triangles=");
            builder.Append(totalTriangles);
            builder.Append(" visibleTriangles=");
            builder.Append(visibleTriangles);
            builder.Append(" ecsVisuals=");
            builder.Append(BuildUnitVisualSummary());
            builder.AppendLine();

            builder.Append("[RenderedGeometryAudit] buckets=");
            AppendBuckets(builder, buckets);
            builder.AppendLine();

            builder.Append("[RenderedGeometryAudit] top=");
            int limit = math.min(TopEntryLimit, entries.Count);
            for (int i = 0; i < limit; i++)
            {
                GeometryEntry entry = entries[i];
                if (i > 0)
                    builder.Append(" | ");

                builder.Append(entry.VisibleInCamera ? "visible" : "outside");
                builder.Append(entry.GameObjectRenderer ? ":go:" : ":ecs:");
                builder.Append(entry.Triangles);
                builder.Append("tris:");
                builder.Append(entry.Owner);
                builder.Append(":");
                builder.Append(entry.MeshName);
                builder.Append(":");
                builder.Append(entry.Path);
            }

            return builder.ToString();
        }

        private static void AppendEcsEntries(
            Plane[] frustum,
            List<GeometryEntry> entries,
            Dictionary<string, GeometryBucket> buckets)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager em = world.EntityManager;
            EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                    ComponentType.ReadOnly<RenderMeshArray>(),
                    ComponentType.ReadOnly<Unity.Rendering.RenderBounds>(),
                    ComponentType.ReadOnly<LocalToWorld>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Disabled>(),
                    ComponentType.ReadOnly<DisableRendering>()
                }
            });

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                    continue;

                RenderMeshArray renderMeshArray = em.GetSharedComponentManaged<RenderMeshArray>(entity);
                MaterialMeshInfo meshInfo = em.GetComponentData<MaterialMeshInfo>(entity);
                LocalToWorld localToWorld = em.GetComponentData<LocalToWorld>(entity);
                Unity.Rendering.RenderBounds renderBounds = em.GetComponentData<Unity.Rendering.RenderBounds>(entity);
                Bounds worldBounds = ToWorldBounds(renderBounds.Value, localToWorld.Value);
                bool visible = GeometryUtility.TestPlanesAABB(frustum, worldBounds);
                string owner = ResolveOwnerLabel(em, entity);
                string bucket = ResolveBucket(owner);

                if (meshInfo.HasMaterialMeshIndexRange)
                {
                    MaterialMeshIndex[] materialMeshIndices = renderMeshArray.MaterialMeshIndices;
                    if (materialMeshIndices == null)
                        continue;

                    RangeInt range = meshInfo.MaterialMeshIndexRange;
                    int end = math.min(range.end, materialMeshIndices.Length);
                    for (int meshIndex = range.start; meshIndex < end; meshIndex++)
                    {
                        MaterialMeshIndex index = materialMeshIndices[meshIndex];
                        Mesh mesh = ResolveRenderMeshArrayMesh(renderMeshArray, index.MeshIndex);
                        int triangles = ResolveTriangleCount(mesh, index.SubMeshIndex);
                        if (triangles <= 0)
                            continue;

                        AddEntry(
                            entries,
                            buckets,
                            bucket,
                            owner,
                            $"Entity({entity.Index}:{entity.Version})/{meshIndex}",
                            mesh.name,
                            triangles,
                            visible,
                            gameObjectRenderer: false);
                    }
                }
                else
                {
                    Mesh mesh = renderMeshArray.GetMesh(meshInfo);
                    int triangles = ResolveTriangleCount(mesh, meshInfo.SubMesh);
                    if (triangles <= 0)
                        continue;

                    AddEntry(
                        entries,
                        buckets,
                        bucket,
                        owner,
                        $"Entity({entity.Index}:{entity.Version})",
                        mesh.name,
                        triangles,
                        visible,
                        gameObjectRenderer: false);
                }
            }

            query.Dispose();
        }

        private static void AppendGameObjectEntries(
            Plane[] frustum,
            List<GeometryEntry> entries,
            Dictionary<string, GeometryBucket> buckets)
        {
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                int triangles = ResolveTriangleCount(renderer, out string meshName);
                if (triangles <= 0)
                    continue;

                bool visible = GeometryUtility.TestPlanesAABB(frustum, renderer.bounds);
                string path = BuildGameObjectPath(renderer.transform);
                string owner = ResolveGameObjectOwner(path);
                AddEntry(entries, buckets, owner, owner, path, meshName, triangles, visible, gameObjectRenderer: true);
            }
        }

        private static void AddEntry(
            List<GeometryEntry> entries,
            Dictionary<string, GeometryBucket> buckets,
            string bucketName,
            string owner,
            string path,
            string meshName,
            int triangles,
            bool visible,
            bool gameObjectRenderer)
        {
            entries.Add(new GeometryEntry
            {
                Path = path,
                MeshName = meshName,
                Owner = owner,
                Triangles = triangles,
                VisibleInCamera = visible,
                GameObjectRenderer = gameObjectRenderer
            });

            if (!buckets.TryGetValue(bucketName, out GeometryBucket bucket))
            {
                bucket = new GeometryBucket { Name = bucketName };
                buckets.Add(bucketName, bucket);
            }

            bucket.Renderers++;
            bucket.Triangles += triangles;
            if (visible)
            {
                bucket.VisibleRenderers++;
                bucket.VisibleTriangles += triangles;
            }
        }

        private static string BuildUnitVisualSummary()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return "world=missing";

            EntityManager em = world.EntityManager;
            EntityQuery unitQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
            EntityQuery visualQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitRenderVisualComponent>());
            EntityQuery farQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitRenderBudgetCulledUnitTag>());
            EntityQuery detailReferenceQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitDetailedVisualReference>());
            EntityQuery midReferenceQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitMidLodInstanceReference>());
            EntityQuery lowReferenceQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitLowLodInstanceReference>());

            int detail = 0;
            int mid = 0;
            int low = 0;
            int far = 0;
            using (NativeArray<UnitRenderVisualComponent> visuals = visualQuery.ToComponentDataArray<UnitRenderVisualComponent>(Allocator.Temp))
            {
                for (int i = 0; i < visuals.Length; i++)
                {
                    switch ((UnitRenderVisualKind)visuals[i].Current)
                    {
                        case UnitRenderVisualKind.Detail:
                            detail++;
                            break;
                        case UnitRenderVisualKind.Mid:
                            mid++;
                            break;
                        case UnitRenderVisualKind.Low:
                            low++;
                            break;
                        case UnitRenderVisualKind.Far:
                            far++;
                            break;
                    }
                }
            }

            string result =
                $"units={unitQuery.CalculateEntityCount()},visualState={visualQuery.CalculateEntityCount()},detailCurrent={detail},midCurrent={mid},lowCurrent={low},farCurrent={far},farTag={farQuery.CalculateEntityCount()},detailRefs={detailReferenceQuery.CalculateEntityCount()},midRefs={midReferenceQuery.CalculateEntityCount()},lowRefs={lowReferenceQuery.CalculateEntityCount()}";
            unitQuery.Dispose();
            visualQuery.Dispose();
            farQuery.Dispose();
            detailReferenceQuery.Dispose();
            midReferenceQuery.Dispose();
            lowReferenceQuery.Dispose();
            return result;
        }

        private static void AppendBuckets(StringBuilder builder, Dictionary<string, GeometryBucket> buckets)
        {
            List<GeometryBucket> ordered = new(buckets.Values);
            ordered.Sort(static (a, b) =>
            {
                int visibleCompare = b.VisibleTriangles.CompareTo(a.VisibleTriangles);
                if (visibleCompare != 0)
                    return visibleCompare;

                return string.CompareOrdinal(a.Name, b.Name);
            });

            for (int i = 0; i < ordered.Count; i++)
            {
                GeometryBucket bucket = ordered[i];
                if (i > 0)
                    builder.Append("; ");

                builder.Append(bucket.Name);
                builder.Append(" visible=");
                builder.Append(bucket.VisibleTriangles);
                builder.Append("tris/");
                builder.Append(bucket.VisibleRenderers);
                builder.Append("r total=");
                builder.Append(bucket.Triangles);
                builder.Append("tris/");
                builder.Append(bucket.Renderers);
                builder.Append("r");
            }
        }

        private static string ResolveOwnerLabel(EntityManager em, Entity entity)
        {
            Entity current = entity;
            for (int depth = 0; depth < 48 && current != Entity.Null && em.Exists(current); depth++)
            {
                if (em.HasComponent<UnitSourcePrefabKey>(current))
                {
                    string key = em.GetComponentData<UnitSourcePrefabKey>(current).Value.ToString();
                    if (em.HasComponent<UnitRenderVisualComponent>(current))
                    {
                        UnitRenderVisualComponent visual = em.GetComponentData<UnitRenderVisualComponent>(current);
                        return $"{key}/{(UnitRenderVisualKind)visual.Current}";
                    }

                    return key;
                }

                if (!em.HasComponent<Parent>(current))
                    break;

                current = em.GetComponentData<Parent>(current).Value;
            }

            return "ECS/Unowned";
        }

        private static string ResolveBucket(string owner)
        {
            int slash = owner.IndexOf('/');
            string key = slash >= 0 ? owner[..slash] : owner;
            if (key.StartsWith("Unit_Char_", StringComparison.Ordinal))
                return "Units/Characters";
            if (key.StartsWith("Unit_Veh_", StringComparison.Ordinal))
                return "Units/Vehicles";
            if (key.StartsWith("Building_", StringComparison.Ordinal))
                return "Buildings";
            if (key.StartsWith("ECS/", StringComparison.Ordinal))
                return key;

            return key;
        }

        private static string ResolveGameObjectOwner(string path)
        {
            if (path.StartsWith("Map/", StringComparison.Ordinal))
                return "Map";
            if (path.StartsWith("Canvas", StringComparison.Ordinal) ||
                path.StartsWith("EventSystem", StringComparison.Ordinal))
                return "UI";
            if (path.StartsWith("GameUIRoot", StringComparison.Ordinal))
                return "UI";

            int slash = path.IndexOf('/');
            return slash > 0 ? path[..slash] : path;
        }

        private static Mesh ResolveRenderMeshArrayMesh(RenderMeshArray renderMeshArray, int meshIndex)
        {
            if (renderMeshArray.MeshReferences == null || meshIndex < 0 || meshIndex >= renderMeshArray.MeshReferences.Length)
                return null;

            return renderMeshArray.MeshReferences[meshIndex].Value;
        }

        private static int ResolveTriangleCount(Renderer renderer, out string meshName)
        {
            meshName = "unknown";
            if (renderer is SkinnedMeshRenderer skinned)
            {
                Mesh mesh = skinned.sharedMesh;
                meshName = mesh != null ? mesh.name : "skinned-null";
                return ResolveTriangleCount(mesh, -1);
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh sharedMesh = filter != null ? filter.sharedMesh : null;
            meshName = sharedMesh != null ? sharedMesh.name : "meshfilter-null";
            return ResolveTriangleCount(sharedMesh, -1);
        }

        private static int ResolveTriangleCount(Mesh mesh, int subMesh)
        {
            if (mesh == null)
                return 0;

            if (subMesh >= 0 && subMesh < mesh.subMeshCount)
                return (int)(mesh.GetIndexCount(subMesh) / 3);

            int triangles = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                triangles += (int)(mesh.GetIndexCount(i) / 3);

            return triangles;
        }

        private static Bounds ToWorldBounds(AABB localBounds, float4x4 localToWorld)
        {
            float3 center = math.transform(localToWorld, localBounds.Center);
            float3 extents = localBounds.Extents;
            float3x3 matrix = new(localToWorld.c0.xyz, localToWorld.c1.xyz, localToWorld.c2.xyz);
            float3 worldExtents = new(
                math.abs(matrix.c0.x) * extents.x + math.abs(matrix.c1.x) * extents.y + math.abs(matrix.c2.x) * extents.z,
                math.abs(matrix.c0.y) * extents.x + math.abs(matrix.c1.y) * extents.y + math.abs(matrix.c2.y) * extents.z,
                math.abs(matrix.c0.z) * extents.x + math.abs(matrix.c1.z) * extents.y + math.abs(matrix.c2.z) * extents.z);
            return new Bounds((Vector3)center, (Vector3)(worldExtents * 2f));
        }

        private static string BuildGameObjectPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            StringBuilder builder = new(256);
            BuildGameObjectPathRecursive(transform, builder);
            return builder.ToString();
        }

        private static void BuildGameObjectPathRecursive(Transform transform, StringBuilder builder)
        {
            if (transform.parent != null)
                BuildGameObjectPathRecursive(transform.parent, builder);

            if (builder.Length > 0)
                builder.Append('/');

            builder.Append(transform.name);
        }

        private static int ResolvePositiveInt(string name, int fallback)
        {
            string configured = Environment.GetEnvironmentVariable(name);
            return int.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0
                ? value
                : fallback;
        }

        private static void SetRuntimeUiMode(RuntimeUiConfig runtimeConfig, RuntimeUiMode mode)
        {
            SerializedObject serialized = new(runtimeConfig);
            SerializedProperty modeProperty = serialized.FindProperty("mode");
            if (modeProperty == null)
                throw new InvalidOperationException("RuntimeUiConfig is missing serialized mode field.");

            modeProperty.enumValueIndex = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Complete(bool success, string message)
        {
            if (completed)
                return;

            completed = true;
            EditorApplication.update -= Continue;
            if (success)
                Debug.Log($"[RenderedGeometryAudit] result=Passed {message}");
            else
                Debug.LogError($"[RenderedGeometryAudit] result=Failed {message}");
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
