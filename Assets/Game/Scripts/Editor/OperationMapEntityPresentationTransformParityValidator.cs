#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal static class OperationMapEntityPresentationTransformParityValidator
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_transform_parity.json";
        internal const float MatrixTolerance = 0.0001f;
        internal const float BoundsTolerance = 0.001f;
        private const float RendererBakeKeyTolerance = 0.01f;
        private const float RendererBakeFallbackJoinTolerance = 0.125f;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        internal static TransformParityReport ValidateAndWrite(
            string projectRoot,
            Scene sourceScene,
            Scene candidateScene,
            EntityManager entityManager)
        {
            OperationMapEntityPresentationIdentityAuthoring[] candidates = candidateScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true))
                .OrderBy(identity => identity.SourceGlobalObjectId, StringComparer.Ordinal)
                .ToArray();

            var candidateBySource = candidates.ToDictionary(
                identity => identity.SourceGlobalObjectId,
                identity => identity,
                StringComparer.Ordinal);
            var bakedBySource = ReadBakedIdentities(entityManager);
            var bakedWorldMatrices = new Dictionary<Entity, Matrix4x4>();
            CandidateRendererBakeMap rendererBakeMap = BuildCandidateRendererBakeMap(candidateScene);
            var bakedBounds = CollectBakedBounds(
                entityManager,
                bakedBySource,
                rendererBakeMap,
                bakedWorldMatrices,
                out List<BakedRenderEntityRow> bakedRenderEntities);
            var rows = new List<TransformParityRow>(candidates.Length);

            int rejected = 0;
            foreach (OperationMapEntityPresentationIdentityAuthoring candidate in candidates)
            {
                TransformParityRow row = BuildRow(
                    sourceScene,
                    candidate,
                    bakedBySource,
                    bakedBounds,
                    bakedWorldMatrices,
                    entityManager);
                if (!string.Equals(row.result, "Passed", StringComparison.Ordinal))
                    rejected++;
                rows.Add(row);
            }

            var report = new TransformParityReport
            {
                schema = "warline.operation-map.transform-parity",
                schemaVersion = 2,
                operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                result = rejected == 0 ? "SourceCandidateBakedParityPassed" : "SourceCandidateBakedParityRejected",
                expectedIdentityCount = OperationMapEntityPresentationIdentityBackfillEditor.ExpectedIdentityCount,
                candidateIdentityCount = candidateBySource.Count,
                bakedIdentityCount = bakedBySource.Count,
                rejectedRowCount = rejected,
                matrixTolerance = MatrixTolerance,
                boundsTolerance = BoundsTolerance,
                rows = rows,
                bakedRenderEntityCount = bakedRenderEntities.Count,
                bakedRenderEntities = bakedRenderEntities
            };

            string absolutePath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true) + "\n", Utf8WithoutBom);
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);

            if (candidateBySource.Count != report.expectedIdentityCount ||
                bakedBySource.Count != report.expectedIdentityCount ||
                rejected != 0)
            {
                throw new InvalidOperationException(
                    $"Transform parity rejected: candidate={candidateBySource.Count}, " +
                    $"baked={bakedBySource.Count}, rejected={rejected}. Report: {ReportPath}");
            }

            return report;
        }

        private static TransformParityRow BuildRow(
            Scene sourceScene,
            OperationMapEntityPresentationIdentityAuthoring candidate,
            IReadOnlyDictionary<string, Entity> bakedBySource,
            IReadOnlyDictionary<string, WorldBounds> bakedBounds,
            Dictionary<Entity, Matrix4x4> bakedWorldMatrices,
            EntityManager entityManager)
        {
            var row = new TransformParityRow
            {
                sourceGlobalObjectId = candidate.SourceGlobalObjectId,
                role = candidate.Role.ToString(),
                placementIndex = candidate.PlacementIndex,
                candidatePath = GetPath(candidate.transform),
                candidateLocalMatrix = ToArray(Matrix4x4.TRS(
                    candidate.transform.localPosition,
                    candidate.transform.localRotation,
                    candidate.transform.localScale)),
                candidateWorldMatrix = ToArray(candidate.transform.localToWorldMatrix)
            };

            if (!GlobalObjectId.TryParse(candidate.SourceGlobalObjectId, out GlobalObjectId sourceId) ||
                GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sourceId) is not GameObject source ||
                source.scene != sourceScene)
            {
                row.result = "Rejected";
                row.rejectionReason = "source-match-count-not-one";
                return row;
            }

            row.sourcePath = GetPath(source.transform);
            row.sourceLocalMatrix = ToArray(Matrix4x4.TRS(
                source.transform.localPosition,
                source.transform.localRotation,
                source.transform.localScale));
            row.sourceWorldMatrix = ToArray(source.transform.localToWorldMatrix);
            row.sourceParentChain = BuildParentChain(source.transform.parent);
            if (!bakedBySource.TryGetValue(candidate.SourceGlobalObjectId, out Entity baked))
            {
                row.result = "Rejected";
                row.rejectionReason = "baked-match-or-local-to-world-missing";
                return row;
            }

            Matrix4x4 bakedWorld = ComputeBakedWorldMatrix(
                baked,
                entityManager,
                bakedWorldMatrices,
                new HashSet<Entity>());
            row.bakedWorldMatrix = ToArray(bakedWorld);
            row.bakedHasParent = entityManager.HasComponent<Parent>(baked) ? 1 : 0;
            row.bakedHasPostTransformMatrix = entityManager.HasComponent<PostTransformMatrix>(baked) ? 1 : 0;
            row.bakedLocalTransformMatrix = ToArray(ReadBakedLocalMatrix(baked, entityManager));
            row.bakedPostTransformMatrix = entityManager.HasComponent<PostTransformMatrix>(baked)
                ? ToArray(ToMatrix(entityManager.GetComponentData<PostTransformMatrix>(baked).Value))
                : Array.Empty<float>();
            row.sourceCandidateMatrixResidual = MaxResidual(source.transform.localToWorldMatrix, candidate.transform.localToWorldMatrix);
            row.candidateBakedMatrixResidual = MaxResidual(candidate.transform.localToWorldMatrix, bakedWorld);

            bool sourceHasBounds = TryCombinedRendererBounds(source, out Bounds sourceBounds);
            bool candidateHasBounds = TryCombinedRendererBounds(candidate.gameObject, out Bounds candidateBounds);
            bool bakedHasBounds = bakedBounds.TryGetValue(candidate.SourceGlobalObjectId, out WorldBounds ecsBounds);
            row.sourceBounds = sourceHasBounds ? ToArray(sourceBounds) : Array.Empty<float>();
            row.candidateBounds = candidateHasBounds ? ToArray(candidateBounds) : Array.Empty<float>();
            row.bakedBounds = bakedHasBounds ? ecsBounds.ToArray() : Array.Empty<float>();

            bool ownerMatricesPass = row.sourceCandidateMatrixResidual <= MatrixTolerance &&
                                     row.candidateBakedMatrixResidual <= MatrixTolerance;
            bool boundsPresencePass = sourceHasBounds == candidateHasBounds && candidateHasBounds == bakedHasBounds;
            bool boundsPass = !sourceHasBounds ||
                              (BoundsResidual(sourceBounds, candidateBounds) <= BoundsTolerance &&
                               BoundsResidual(candidateBounds, ecsBounds.ToBounds()) <= BoundsTolerance);
            row.result = ownerMatricesPass && boundsPresencePass && boundsPass ? "Passed" : "Rejected";
            row.rejectionReason = row.result == "Passed"
                ? ""
                : !ownerMatricesPass ? "owner-matrix-residual" :
                  !boundsPresencePass ? "renderer-bounds-presence" : "renderer-bounds-residual";
            return row;
        }

        private static Dictionary<string, Entity> ReadBakedIdentities(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationIdentity));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<OperationMapEntityPresentationIdentity> identities =
                query.ToComponentDataArray<OperationMapEntityPresentationIdentity>(Allocator.Temp);
            var result = new Dictionary<string, Entity>(entities.Length, StringComparer.Ordinal);
            for (int i = 0; i < entities.Length; i++)
            {
                string sourceId = identities[i].SourceGlobalObjectId.ToString();
                if (!result.TryAdd(sourceId, entities[i]))
                    throw new InvalidOperationException($"Duplicate baked presentation identity: {sourceId}");
            }
            return result;
        }

        private static Dictionary<string, WorldBounds> CollectBakedBounds(
            EntityManager entityManager,
            IReadOnlyDictionary<string, Entity> identities,
            CandidateRendererBakeMap rendererBakeMap,
            Dictionary<Entity, Matrix4x4> worldMatrices,
            out List<BakedRenderEntityRow> bakedRenderEntities)
        {
            var identityByEntity = identities.ToDictionary(pair => pair.Value, pair => pair.Key);
            var result = new Dictionary<string, WorldBounds>(StringComparer.Ordinal);
            bakedRenderEntities = new List<BakedRenderEntityRow>();
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            int parentResolved = 0;
            int bakeKeyResolved = 0;
            int fallbackKeyResolved = 0;
            int unresolved = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                RenderBounds local = entityManager.GetComponentData<RenderBounds>(entities[i]);
                Matrix4x4 world = ComputeBakedWorldMatrix(
                    entities[i],
                    entityManager,
                    worldMatrices,
                    new HashSet<Entity>());
                string bakeKey = BuildRendererBakeKey(local.Value.Center, local.Value.Extents, world);
                string sourceId = rendererBakeMap.TryDequeueOwner(bakeKey);
                if (sourceId != null)
                    bakeKeyResolved++;
                else
                {
                    sourceId = rendererBakeMap.TryDequeueNearestOwner(
                        local.Value.Center,
                        local.Value.Extents,
                        world,
                        RendererBakeFallbackJoinTolerance);
                    if (sourceId != null)
                        fallbackKeyResolved++;
                    else
                    {
                        sourceId = ResolveIdentityByParent(
                            entities[i],
                            entityManager,
                            identityByEntity);
                        if (sourceId != null)
                            parentResolved++;
                    }
                }
                if (sourceId == null)
                {
                    unresolved++;
                    bakedRenderEntities.Add(CreateBakedRenderEntityRow(null, local, world));
                    continue;
                }

                WorldBounds transformed = TransformBounds(local.Value.Center, local.Value.Extents, world);
                bakedRenderEntities.Add(CreateBakedRenderEntityRow(sourceId, local, world));
                if (result.TryGetValue(sourceId, out WorldBounds existing))
                    transformed = WorldBounds.Encapsulate(existing, transformed);
                result[sourceId] = transformed;
            }
            bakedRenderEntities.Sort(BakedRenderEntityRowComparer.Instance);
            Debug.Log(
                $"[OperationMapTransformParity] renderEntities={entities.Length} " +
                $"bakeKeyResolved={bakeKeyResolved} fuzzyKeyResolved={fallbackKeyResolved} " +
                $"parentFallbackResolved={parentResolved} " +
                $"unresolved={unresolved} unconsumedExpected={rendererBakeMap.UnconsumedCount} " +
                $"ownersWithBounds={result.Count}");
            return result;
        }

        private static BakedRenderEntityRow CreateBakedRenderEntityRow(
            string sourceId,
            RenderBounds local,
            Matrix4x4 world)
        {
            WorldBounds transformed = TransformBounds(local.Value.Center, local.Value.Extents, world);
            return new BakedRenderEntityRow
            {
                sourceGlobalObjectId = sourceId ?? string.Empty,
                worldMatrix = ToArray(world),
                localBounds = new[]
                {
                    local.Value.Center.x,
                    local.Value.Center.y,
                    local.Value.Center.z,
                    local.Value.Extents.x,
                    local.Value.Extents.y,
                    local.Value.Extents.z
                },
                worldBounds = transformed.ToArray()
            };
        }

        private static CandidateRendererBakeMap BuildCandidateRendererBakeMap(Scene candidateScene)
        {
            var result = new CandidateRendererBakeMap();
            Renderer[] renderers = candidateScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                OperationMapEntityPresentationIdentityAuthoring owner =
                    renderer.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>(true);
                if (owner == null || !TryGetEntitiesGraphicsBakeData(
                        renderer,
                        out Bounds localBounds,
                        out Matrix4x4 world,
                        out int renderEntityCount))
                    continue;

                string key = BuildRendererBakeKey(localBounds.center, localBounds.extents, world);
                for (int entityIndex = 0; entityIndex < renderEntityCount; entityIndex++)
                {
                    result.Add(
                        key,
                        owner.SourceGlobalObjectId,
                        localBounds.center,
                        localBounds.extents,
                        world);
                }
            }
            return result;
        }

        private static bool TryGetEntitiesGraphicsBakeData(
            Renderer renderer,
            out Bounds localBounds,
            out Matrix4x4 world,
            out int renderEntityCount)
        {
            localBounds = default;
            world = default;
            renderEntityCount = 0;
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                return false;

            if (renderer is SkinnedMeshRenderer skinned)
            {
                if (skinned.sharedMesh == null)
                    return false;
                localBounds = skinned.localBounds;
                world = (skinned.rootBone != null ? skinned.rootBone : skinned.transform).localToWorldMatrix;
                renderEntityCount = materials.Length;
                return true;
            }

            if (renderer is not MeshRenderer ||
                renderer.GetComponent<MeshFilter>()?.sharedMesh is not Mesh mesh)
                return false;

            localBounds = mesh.bounds;
            world = renderer.transform.localToWorldMatrix;
            renderEntityCount = materials.Length;
            return true;
        }

        private static string BuildRendererBakeKey(float3 center, float3 extents, Matrix4x4 world)
        {
            var builder = new StringBuilder(192);
            for (int i = 0; i < 16; i++)
                builder.Append(Quantize(world[i])).Append('|');
            builder.Append(Quantize(center.x)).Append('|')
                .Append(Quantize(center.y)).Append('|')
                .Append(Quantize(center.z)).Append('|')
                .Append(Quantize(extents.x)).Append('|')
                .Append(Quantize(extents.y)).Append('|')
                .Append(Quantize(extents.z));
            return builder.ToString();
        }

        private static long Quantize(float value) =>
            (long)Math.Round(value / RendererBakeKeyTolerance, MidpointRounding.AwayFromZero);

        private static string ResolveIdentityByParent(
            Entity entity,
            EntityManager entityManager,
            IReadOnlyDictionary<Entity, string> identityByEntity)
        {
            Entity current = entity;
            for (int depth = 0; depth < 64; depth++)
            {
                if (identityByEntity.TryGetValue(current, out string sourceId))
                    return sourceId;
                if (!entityManager.HasComponent<Parent>(current))
                    return null;
                current = entityManager.GetComponentData<Parent>(current).Value;
            }

            throw new InvalidOperationException(
                $"Baked transform parent depth exceeded while resolving presentation identity for {entity}.");
        }

        private static Matrix4x4 ComputeBakedWorldMatrix(
            Entity entity,
            EntityManager entityManager,
            Dictionary<Entity, Matrix4x4> cache,
            HashSet<Entity> visiting)
        {
            if (cache.TryGetValue(entity, out Matrix4x4 cached))
                return cached;
            if (!visiting.Add(entity))
                throw new InvalidOperationException($"Baked transform parent cycle at {entity}.");

            Matrix4x4 local = ReadBakedLocalMatrix(entity, entityManager);
            if (entityManager.HasComponent<PostTransformMatrix>(entity))
                local *= ToMatrix(entityManager.GetComponentData<PostTransformMatrix>(entity).Value);

            Matrix4x4 world = local;
            if (entityManager.HasComponent<Parent>(entity))
            {
                Entity parent = entityManager.GetComponentData<Parent>(entity).Value;
                world = ComputeBakedWorldMatrix(parent, entityManager, cache, visiting) * local;
            }
            else if (!entityManager.HasComponent<LocalTransform>(entity) &&
                     entityManager.HasComponent<LocalToWorld>(entity))
            {
                world = ToMatrix(entityManager.GetComponentData<LocalToWorld>(entity).Value);
            }

            visiting.Remove(entity);
            cache[entity] = world;
            return world;
        }

        private static Matrix4x4 ReadBakedLocalMatrix(Entity entity, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<LocalTransform>(entity))
                return Matrix4x4.identity;
            LocalTransform local = entityManager.GetComponentData<LocalTransform>(entity);
            return ToMatrix(float4x4.TRS(local.Position, local.Rotation, new float3(local.Scale)));
        }

        private static WorldBounds TransformBounds(float3 center, float3 extents, Matrix4x4 matrix)
        {
            Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = (Vector3)center + Vector3.Scale((Vector3)extents, new Vector3(x, y, z));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
            return new WorldBounds(min, max);
        }

        private static bool TryCombinedRendererBounds(GameObject owner, out Bounds bounds)
        {
            Renderer[] renderers = owner.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                Mesh mesh = renderer switch
                {
                    SkinnedMeshRenderer skinned => skinned.sharedMesh,
                    MeshRenderer => renderer.GetComponent<MeshFilter>()?.sharedMesh,
                    _ => null
                };
                Material[] materials = renderer.sharedMaterials;
                if (mesh == null || materials.Length == 0)
                {
                    // Entities Graphics does not emit a RenderBounds row for this renderer.
                    continue;
                }

                if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    EncapsulateTransformed(
                        renderer.localBounds,
                        skinnedRenderer.rootBone != null
                            ? skinnedRenderer.rootBone.localToWorldMatrix
                            : renderer.transform.localToWorldMatrix,
                        ref bounds,
                        ref hasBounds);
                    continue;
                }

                // Entities Graphics bakes Mesh.bounds for each material entity, not the
                // individual submesh bounds. Repeating the same bounds does not change
                // the combined owner bounds, so evaluate it once here.
                EncapsulateTransformed(
                    mesh.bounds,
                    renderer.transform.localToWorldMatrix,
                    ref bounds,
                    ref hasBounds);
            }
            return hasBounds;
        }

        private static void EncapsulateTransformed(
            Bounds localBounds,
            Matrix4x4 localToWorld,
            ref Bounds combined,
            ref bool hasBounds)
        {
            Bounds worldBounds = TransformBounds(localBounds.center, localBounds.extents, localToWorld).ToBounds();
            if (hasBounds)
                combined.Encapsulate(worldBounds);
            else
            {
                combined = worldBounds;
                hasBounds = true;
            }
        }

        private static string[] BuildParentChain(Transform parent)
        {
            var chain = new List<string>();
            while (parent != null)
            {
                chain.Add(GlobalObjectId.GetGlobalObjectIdSlow(parent.gameObject).ToString());
                parent = parent.parent;
            }
            chain.Reverse();
            return chain.ToArray();
        }

        private static string GetPath(Transform transform)
        {
            var segments = new List<string>();
            while (transform != null) { segments.Add(transform.name); transform = transform.parent; }
            segments.Reverse();
            return string.Join("/", segments);
        }

        internal static float MaxResidual(Matrix4x4 left, Matrix4x4 right)
        {
            float max = 0f;
            for (int i = 0; i < 16; i++) max = Mathf.Max(max, Mathf.Abs(left[i] - right[i]));
            return max;
        }

        private static float BoundsResidual(Bounds left, Bounds right) =>
            Mathf.Max(Vector3.Distance(left.min, right.min), Vector3.Distance(left.max, right.max));

        private static float[] ToArray(Matrix4x4 matrix)
        {
            var values = new float[16];
            for (int i = 0; i < 16; i++) values[i] = matrix[i];
            return values;
        }

        private static float[] ToArray(Bounds bounds) =>
            new[] { bounds.min.x, bounds.min.y, bounds.min.z, bounds.max.x, bounds.max.y, bounds.max.z };

        private static Matrix4x4 ToMatrix(float4x4 value)
        {
            var matrix = new Matrix4x4();
            matrix.SetColumn(0, new Vector4(value.c0.x, value.c0.y, value.c0.z, value.c0.w));
            matrix.SetColumn(1, new Vector4(value.c1.x, value.c1.y, value.c1.z, value.c1.w));
            matrix.SetColumn(2, new Vector4(value.c2.x, value.c2.y, value.c2.z, value.c2.w));
            matrix.SetColumn(3, new Vector4(value.c3.x, value.c3.y, value.c3.z, value.c3.w));
            return matrix;
        }

        private readonly struct WorldBounds
        {
            internal WorldBounds(Vector3 min, Vector3 max) { Min = min; Max = max; }
            internal Vector3 Min { get; }
            internal Vector3 Max { get; }
            internal Bounds ToBounds() { var b = new Bounds(); b.SetMinMax(Min, Max); return b; }
            internal float[] ToArray() => new[] { Min.x, Min.y, Min.z, Max.x, Max.y, Max.z };
            internal static WorldBounds Encapsulate(WorldBounds a, WorldBounds b) =>
                new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));
        }

        private sealed class CandidateRendererBakeMap
        {
            private readonly Dictionary<string, Queue<ExpectedRendererBakeEntry>> ownersByKey =
                new(StringComparer.Ordinal);
            private readonly List<ExpectedRendererBakeEntry> entries = new();

            internal int UnconsumedCount { get; private set; }

            internal void Add(
                string key,
                string sourceId,
                Vector3 center,
                Vector3 extents,
                Matrix4x4 world)
            {
                if (!ownersByKey.TryGetValue(key, out Queue<ExpectedRendererBakeEntry> owners))
                {
                    owners = new Queue<ExpectedRendererBakeEntry>();
                    ownersByKey.Add(key, owners);
                }
                var entry = new ExpectedRendererBakeEntry(sourceId, center, extents, world);
                owners.Enqueue(entry);
                entries.Add(entry);
                UnconsumedCount++;
            }

            internal string TryDequeueOwner(string key)
            {
                if (!ownersByKey.TryGetValue(key, out Queue<ExpectedRendererBakeEntry> owners))
                    return null;
                while (owners.Count > 0 && owners.Peek().Consumed)
                    owners.Dequeue();
                if (owners.Count == 0)
                    return null;
                ExpectedRendererBakeEntry entry = owners.Dequeue();
                entry.Consumed = true;
                UnconsumedCount--;
                return entry.SourceId;
            }

            internal string TryDequeueNearestOwner(
                float3 center,
                float3 extents,
                Matrix4x4 world,
                float maxResidual)
            {
                ExpectedRendererBakeEntry best = null;
                float bestResidual = float.PositiveInfinity;
                for (int i = 0; i < entries.Count; i++)
                {
                    ExpectedRendererBakeEntry entry = entries[i];
                    if (entry.Consumed)
                        continue;
                    float residual = Mathf.Max(
                        MaxResidual(entry.World, world),
                        MaxComponentResidual(entry.Center, center),
                        MaxComponentResidual(entry.Extents, extents));
                    if (residual < bestResidual)
                    {
                        bestResidual = residual;
                        best = entry;
                    }
                }

                if (best == null || bestResidual > maxResidual)
                    return null;
                best.Consumed = true;
                UnconsumedCount--;
                return best.SourceId;
            }

            private static float MaxComponentResidual(Vector3 left, float3 right) =>
                Mathf.Max(
                    Mathf.Abs(left.x - right.x),
                    Mathf.Abs(left.y - right.y),
                    Mathf.Abs(left.z - right.z));
        }

        private sealed class ExpectedRendererBakeEntry
        {
            internal ExpectedRendererBakeEntry(
                string sourceId,
                Vector3 center,
                Vector3 extents,
                Matrix4x4 world)
            {
                SourceId = sourceId;
                Center = center;
                Extents = extents;
                World = world;
            }

            internal string SourceId { get; }
            internal Vector3 Center { get; }
            internal Vector3 Extents { get; }
            internal Matrix4x4 World { get; }
            internal bool Consumed { get; set; }
        }

        private sealed class BakedRenderEntityRowComparer : IComparer<BakedRenderEntityRow>
        {
            internal static readonly BakedRenderEntityRowComparer Instance = new();

            public int Compare(BakedRenderEntityRow left, BakedRenderEntityRow right)
            {
                int source = string.Compare(
                    left.sourceGlobalObjectId,
                    right.sourceGlobalObjectId,
                    StringComparison.Ordinal);
                if (source != 0)
                    return source;
                int matrix = CompareValues(left.worldMatrix, right.worldMatrix);
                return matrix != 0 ? matrix : CompareValues(left.localBounds, right.localBounds);
            }

            private static int CompareValues(float[] left, float[] right)
            {
                int count = Math.Min(left.Length, right.Length);
                for (int i = 0; i < count; i++)
                {
                    int comparison = left[i].CompareTo(right[i]);
                    if (comparison != 0)
                        return comparison;
                }
                return left.Length.CompareTo(right.Length);
            }
        }

        [Serializable]
        internal sealed class TransformParityReport
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string result;
            public int expectedIdentityCount;
            public int candidateIdentityCount;
            public int bakedIdentityCount;
            public int rejectedRowCount;
            public float matrixTolerance;
            public float boundsTolerance;
            public List<TransformParityRow> rows;
            public int bakedRenderEntityCount;
            public List<BakedRenderEntityRow> bakedRenderEntities;
        }

        [Serializable]
        internal sealed class BakedRenderEntityRow
        {
            public string sourceGlobalObjectId;
            public float[] worldMatrix;
            public float[] localBounds;
            public float[] worldBounds;
        }

        [Serializable]
        internal sealed class TransformParityRow
        {
            public string sourceGlobalObjectId;
            public string role;
            public int placementIndex;
            public string sourcePath;
            public string candidatePath;
            public string[] sourceParentChain;
            public float[] sourceLocalMatrix;
            public float[] sourceWorldMatrix;
            public float[] candidateLocalMatrix;
            public float[] candidateWorldMatrix;
            public float[] bakedWorldMatrix;
            public float[] bakedLocalTransformMatrix;
            public float[] bakedPostTransformMatrix;
            public int bakedHasParent;
            public int bakedHasPostTransformMatrix;
            public float sourceCandidateMatrixResidual;
            public float candidateBakedMatrixResidual;
            public float[] sourceBounds;
            public float[] candidateBounds;
            public float[] bakedBounds;
            public string result;
            public string rejectionReason;
        }
    }
}

#endif
