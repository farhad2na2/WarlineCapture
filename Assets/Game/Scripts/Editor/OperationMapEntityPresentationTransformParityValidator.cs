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
            var bakedBounds = CollectBakedBounds(entityManager, bakedBySource, bakedWorldMatrices);
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
                schemaVersion = 1,
                operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                result = rejected == 0 ? "SourceCandidateBakedParityPassed" : "SourceCandidateBakedParityRejected",
                expectedIdentityCount = OperationMapEntityPresentationIdentityBackfillEditor.ExpectedIdentityCount,
                candidateIdentityCount = candidateBySource.Count,
                bakedIdentityCount = bakedBySource.Count,
                rejectedRowCount = rejected,
                matrixTolerance = MatrixTolerance,
                boundsTolerance = BoundsTolerance,
                rows = rows
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
            Dictionary<Entity, Matrix4x4> worldMatrices)
        {
            var identityByEntity = identities.ToDictionary(pair => pair.Value, pair => pair.Key);
            var result = new Dictionary<string, WorldBounds>(StringComparer.Ordinal);
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity current = entities[i];
                string sourceId = null;
                for (int depth = 0; depth < 64; depth++)
                {
                    if (identityByEntity.TryGetValue(current, out sourceId))
                        break;
                    if (!entityManager.HasComponent<Parent>(current))
                        break;
                    current = entityManager.GetComponentData<Parent>(current).Value;
                }
                if (sourceId == null)
                    continue;

                RenderBounds local = entityManager.GetComponentData<RenderBounds>(entities[i]);
                Matrix4x4 world = ComputeBakedWorldMatrix(
                    entities[i],
                    entityManager,
                    worldMatrices,
                    new HashSet<Entity>());
                WorldBounds transformed = TransformBounds(local.Value.Center, local.Value.Extents, world);
                if (result.TryGetValue(sourceId, out WorldBounds existing))
                    transformed = WorldBounds.Encapsulate(existing, transformed);
                result[sourceId] = transformed;
            }
            return result;
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

                if (renderer is SkinnedMeshRenderer)
                {
                    if (materials.Any(material => material != null))
                        EncapsulateTransformed(renderer.localBounds, renderer.transform.localToWorldMatrix, ref bounds, ref hasBounds);
                    continue;
                }

                int subMeshCount = Math.Min(mesh.subMeshCount, materials.Length);
                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    if (materials[subMesh] == null)
                        continue;
                    EncapsulateTransformed(
                        mesh.GetSubMesh(subMesh).bounds,
                        renderer.transform.localToWorldMatrix,
                        ref bounds,
                        ref hasBounds);
                }
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
