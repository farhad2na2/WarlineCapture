#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Compares generated dense-city authoring owners with their in-memory baked ECS output.
    /// This validator is passive: the caller owns scene loading, baking, and world disposal.
    /// </summary>
    internal static class OperationMapDenseCityGeneratedTransformParityValidator
    {
        internal const string DefaultReportPath =
            "Design/AgentReports/2026-07-24_dense_city_generated_transform_parity.json";
        internal const float MatrixTolerance = 0.0001f;
        internal const float BoundsTolerance = 0.001f;

        private const float RendererBakeKeyTolerance = 0.01f;
        private const float RendererBakeFallbackJoinTolerance = 0.125f;
        private const int MaxRejectedSamples = 64;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        internal static DenseCityGeneratedTransformParityReport ValidateAndWrite(
            string projectRoot,
            Scene candidateScene,
            EntityManager entityManager,
            string reportPath = DefaultReportPath)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("A project root is required.", nameof(projectRoot));
            if (!candidateScene.IsValid() || !candidateScene.isLoaded)
                throw new ArgumentException("The dense-city candidate scene must be valid and loaded.", nameof(candidateScene));
            if (string.IsNullOrWhiteSpace(reportPath))
                throw new ArgumentException("A report path is required.", nameof(reportPath));

            DenseCityPresentationIdentityAuthoring[] candidates = candidateScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true))
                .OrderBy(identity => identity.StableId, StringComparer.Ordinal)
                .ThenBy(identity => GetPath(identity.transform), StringComparer.Ordinal)
                .ToArray();

            var candidateGroups = candidates
                .GroupBy(identity => identity.StableId ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            string[] duplicateCandidateStableIds = candidateGroups
                .Where(pair => pair.Value.Length != 1)
                .Select(pair => pair.Key)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            BakedIdentityIndex bakedIndex = ReadBakedIdentities(entityManager);
            var worldMatrices = new Dictionary<Entity, Matrix4x4>();
            CandidateRendererBakeMap rendererBakeMap = BuildCandidateRendererBakeMap(candidateScene);
            Dictionary<string, WorldBounds> bakedBounds = CollectBakedBounds(
                entityManager,
                bakedIndex.ByEntity,
                rendererBakeMap,
                worldMatrices,
                out int generatedBakedRenderEntityCount,
                out int unresolvedGeneratedRendererEntityCount);
            Dictionary<string, Bounds> candidateBounds = CollectCandidateBounds(candidateScene);

            var rows = new List<DenseCityGeneratedTransformParityRow>(candidates.Length);
            foreach (DenseCityPresentationIdentityAuthoring candidate in candidates)
            {
                rows.Add(BuildRow(
                    candidate,
                    candidateGroups,
                    bakedIndex,
                    candidateBounds,
                    bakedBounds,
                    worldMatrices,
                    entityManager));
            }

            rows.Sort(DenseCityGeneratedTransformParityRowComparer.Instance);
            int rejectedRowCount = rows.Count(row =>
                !string.Equals(row.result, "Passed", StringComparison.Ordinal));
            string[] missingBakedStableIds = candidateGroups.Keys
                .Except(bakedIndex.AllStableIds, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            string[] unexpectedBakedStableIds = bakedIndex.AllStableIds
                .Except(candidateGroups.Keys, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var rejectedSamples = rows
                .Where(row => !string.Equals(row.result, "Passed", StringComparison.Ordinal))
                .ToList();
            foreach (string stableId in unexpectedBakedStableIds)
            {
                bool hasRole = bakedIndex.TryGetRole(stableId, out byte role);
                rejectedSamples.Add(new DenseCityGeneratedTransformParityRow
                {
                    stableId = stableId,
                    candidatePath = string.Empty,
                    candidateRole = string.Empty,
                    bakedRole = hasRole
                        ? ((OperationMapEntityPresentationRole)role).ToString()
                        : string.Empty,
                    bakedRoleValue = role,
                    candidateWorldMatrix = Array.Empty<float>(),
                    bakedWorldMatrix = Array.Empty<float>(),
                    candidateBounds = Array.Empty<float>(),
                    bakedBounds = Array.Empty<float>(),
                    result = "Rejected",
                    rejectionReason = "unexpected-baked-identity"
                });
            }
            rejectedSamples.Sort(DenseCityGeneratedTransformParityRowComparer.Instance);
            if (rejectedSamples.Count > MaxRejectedSamples)
                rejectedSamples.RemoveRange(MaxRejectedSamples, rejectedSamples.Count - MaxRejectedSamples);

            bool passed = rejectedRowCount == 0 &&
                          duplicateCandidateStableIds.Length == 0 &&
                          bakedIndex.DuplicateStableIds.Length == 0 &&
                          missingBakedStableIds.Length == 0 &&
                          unexpectedBakedStableIds.Length == 0 &&
                          unresolvedGeneratedRendererEntityCount == 0 &&
                          rendererBakeMap.UnconsumedCount == 0 &&
                          rendererBakeMap.ExpectedEntryCount == generatedBakedRenderEntityCount;
            var report = new DenseCityGeneratedTransformParityReport
            {
                schema = "warline.operation-map.dense-city-generated-transform-parity",
                schemaVersion = 1,
                checkpoint = "ecs-bake",
                result = passed ? "DenseCityGeneratedTransformParityPassed" :
                    "DenseCityGeneratedTransformParityRejected",
                candidateIdentityCount = candidates.Length,
                uniqueCandidateIdentityCount = candidateGroups.Count,
                bakedIdentityCount = bakedIndex.TotalCount,
                uniqueBakedIdentityCount = bakedIndex.AllStableIds.Count,
                generatedCandidateRendererEntityCount = rendererBakeMap.ExpectedEntryCount,
                generatedBakedRenderEntityCount = generatedBakedRenderEntityCount,
                unresolvedGeneratedRendererEntityCount = unresolvedGeneratedRendererEntityCount,
                unconsumedCandidateRendererEntityCount = rendererBakeMap.UnconsumedCount,
                rejectedRowCount = rejectedRowCount,
                rejectedSampleCount = rejectedSamples.Count,
                rejectedSampleLimit = MaxRejectedSamples,
                matrixTolerance = MatrixTolerance,
                boundsTolerance = BoundsTolerance,
                duplicateCandidateStableIdCount = duplicateCandidateStableIds.Length,
                duplicateBakedStableIdCount = bakedIndex.DuplicateStableIds.Length,
                missingBakedStableIdCount = missingBakedStableIds.Length,
                unexpectedBakedStableIdCount = unexpectedBakedStableIds.Length,
                candidateIdentitySetSha256 = ComputeStringSetDigest(candidateGroups.Keys),
                bakedIdentitySetSha256 = ComputeStringSetDigest(bakedIndex.AllStableIds),
                evaluatedRowsSha256 = ComputeRowsDigest(rows),
                rejectedSamples = rejectedSamples
            };

            WriteReport(projectRoot, reportPath, report);
            if (!passed)
            {
                throw new InvalidOperationException(
                    $"Dense-city generated transform parity rejected: " +
                    $"candidate={candidates.Length}/{candidateGroups.Count} unique, " +
                    $"baked={bakedIndex.TotalCount}/{bakedIndex.AllStableIds.Count} unique, " +
                    $"rejected={rejectedRowCount}, missing={missingBakedStableIds.Length}, " +
                    $"unexpected={unexpectedBakedStableIds.Length}, " +
                    $"renderers={generatedBakedRenderEntityCount}/{rendererBakeMap.ExpectedEntryCount}, " +
                    $"unresolved={unresolvedGeneratedRendererEntityCount}, " +
                    $"unconsumed={rendererBakeMap.UnconsumedCount}. Report: {reportPath}");
            }

            return report;
        }

        private static DenseCityGeneratedTransformParityRow BuildRow(
            DenseCityPresentationIdentityAuthoring candidate,
            IReadOnlyDictionary<string, DenseCityPresentationIdentityAuthoring[]> candidateGroups,
            BakedIdentityIndex bakedIndex,
            IReadOnlyDictionary<string, Bounds> candidateBounds,
            IReadOnlyDictionary<string, WorldBounds> bakedBounds,
            Dictionary<Entity, Matrix4x4> worldMatrices,
            EntityManager entityManager)
        {
            string stableId = candidate.StableId ?? string.Empty;
            var row = new DenseCityGeneratedTransformParityRow
            {
                stableId = stableId,
                candidatePath = GetPath(candidate.transform),
                candidateRole = candidate.Role.ToString(),
                candidateRoleValue = (byte)candidate.Role,
                candidateWorldMatrix = ToArray(candidate.transform.localToWorldMatrix),
                bakedWorldMatrix = Array.Empty<float>(),
                candidateBounds = Array.Empty<float>(),
                bakedBounds = Array.Empty<float>()
            };

            if (!candidate.TryValidate(out string validationError))
            {
                Reject(row, $"candidate-identity-invalid:{validationError}");
                return row;
            }
            if (candidate.GetComponent<OperationMapEntityPresentationIdentityAuthoring>() != null)
            {
                Reject(row, "candidate-game-object-has-legacy-and-dense-identities");
                return row;
            }
            if (candidateGroups[stableId].Length != 1)
            {
                Reject(row, "candidate-stable-id-duplicate");
                return row;
            }
            if (bakedIndex.DuplicateStableIdSet.Contains(stableId))
            {
                Reject(row, "baked-stable-id-duplicate");
                return row;
            }
            if (!bakedIndex.UniqueByStableId.TryGetValue(stableId, out BakedIdentity baked))
            {
                Reject(row, "baked-identity-missing");
                return row;
            }

            row.bakedRoleValue = baked.Role;
            row.bakedRole = ((OperationMapEntityPresentationRole)baked.Role).ToString();
            row.roleMatches = baked.Role == (byte)candidate.Role ? 1 : 0;
            Matrix4x4 bakedWorld = ComputeBakedWorldMatrix(
                baked.Entity,
                entityManager,
                worldMatrices,
                new HashSet<Entity>());
            row.bakedWorldMatrix = ToArray(bakedWorld);
            row.matrixResidual = MaxResidual(candidate.transform.localToWorldMatrix, bakedWorld);

            bool candidateHasBounds = candidateBounds.TryGetValue(stableId, out Bounds candidateOwnerBounds);
            bool bakedHasBounds = bakedBounds.TryGetValue(stableId, out WorldBounds bakedOwnerBounds);
            row.candidateHasBounds = candidateHasBounds ? 1 : 0;
            row.bakedHasBounds = bakedHasBounds ? 1 : 0;
            if (candidateHasBounds)
                row.candidateBounds = ToArray(candidateOwnerBounds);
            if (bakedHasBounds)
                row.bakedBounds = bakedOwnerBounds.ToArray();
            row.boundsResidual = candidateHasBounds && bakedHasBounds
                ? BoundsResidual(candidateOwnerBounds, bakedOwnerBounds.ToBounds())
                : candidateHasBounds == bakedHasBounds ? 0f : float.MaxValue;

            if (row.roleMatches == 0)
                Reject(row, "role-mismatch");
            else if (row.matrixResidual > MatrixTolerance)
                Reject(row, "owner-matrix-residual");
            else if (candidateHasBounds != bakedHasBounds)
                Reject(row, "renderer-bounds-presence");
            else if (candidateHasBounds && row.boundsResidual > BoundsTolerance)
                Reject(row, "renderer-bounds-residual");
            else
            {
                row.result = "Passed";
                row.rejectionReason = string.Empty;
            }
            return row;
        }

        private static BakedIdentityIndex ReadBakedIdentities(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DenseCityPresentationIdentity>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<DenseCityPresentationIdentity> identities =
                query.ToComponentDataArray<DenseCityPresentationIdentity>(Allocator.Temp);

            var grouped = new Dictionary<string, List<BakedIdentity>>(StringComparer.Ordinal);
            for (int i = 0; i < entities.Length; i++)
            {
                string stableId = identities[i].StableId.ToString();
                if (!grouped.TryGetValue(stableId, out List<BakedIdentity> values))
                {
                    values = new List<BakedIdentity>();
                    grouped.Add(stableId, values);
                }
                values.Add(new BakedIdentity(entities[i], identities[i].Role));
            }

            string[] duplicates = grouped
                .Where(pair => pair.Value.Count != 1)
                .Select(pair => pair.Key)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var unique = grouped
                .Where(pair => pair.Value.Count == 1)
                .ToDictionary(pair => pair.Key, pair => pair.Value[0], StringComparer.Ordinal);
            var byEntity = new Dictionary<Entity, string>();
            foreach (KeyValuePair<string, List<BakedIdentity>> pair in grouped)
            {
                foreach (BakedIdentity identity in pair.Value)
                    byEntity[identity.Entity] = pair.Key;
            }
            return new BakedIdentityIndex(
                entities.Length,
                grouped.Keys,
                unique,
                duplicates,
                byEntity);
        }

        private static Dictionary<string, Bounds> CollectCandidateBounds(Scene candidateScene)
        {
            var result = new Dictionary<string, Bounds>(StringComparer.Ordinal);
            Renderer[] renderers = candidateScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .OrderBy(renderer => GetPath(renderer.transform), StringComparer.Ordinal)
                .ToArray();
            foreach (Renderer renderer in renderers)
            {
                DenseCityPresentationIdentityAuthoring owner =
                    renderer.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true);
                if (owner == null ||
                    !TryGetEntitiesGraphicsBakeData(
                        renderer,
                        out Bounds localBounds,
                        out Matrix4x4 world,
                        out _))
                {
                    continue;
                }

                Bounds transformed = TransformBounds(localBounds.center, localBounds.extents, world).ToBounds();
                if (result.TryGetValue(owner.StableId, out Bounds existing))
                {
                    existing.Encapsulate(transformed);
                    transformed = existing;
                }
                result[owner.StableId] = transformed;
            }
            return result;
        }

        private static CandidateRendererBakeMap BuildCandidateRendererBakeMap(Scene candidateScene)
        {
            var result = new CandidateRendererBakeMap();
            Renderer[] renderers = candidateScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .OrderBy(renderer => GetPath(renderer.transform), StringComparer.Ordinal)
                .ToArray();
            foreach (Renderer renderer in renderers)
            {
                DenseCityPresentationIdentityAuthoring owner =
                    renderer.GetComponentInParent<DenseCityPresentationIdentityAuthoring>(true);
                if (owner == null ||
                    !TryGetEntitiesGraphicsBakeData(
                        renderer,
                        out Bounds localBounds,
                        out Matrix4x4 world,
                        out int renderEntityCount))
                {
                    continue;
                }

                string key = BuildRendererBakeKey(localBounds.center, localBounds.extents, world);
                for (int i = 0; i < renderEntityCount; i++)
                {
                    result.Add(
                        key,
                        owner.StableId,
                        localBounds.center,
                        localBounds.extents,
                        world);
                }
            }
            return result;
        }

        private static Dictionary<string, WorldBounds> CollectBakedBounds(
            EntityManager entityManager,
            IReadOnlyDictionary<Entity, string> identityByEntity,
            CandidateRendererBakeMap rendererBakeMap,
            Dictionary<Entity, Matrix4x4> worldMatrices,
            out int generatedBakedRenderEntityCount,
            out int unresolvedGeneratedRendererEntityCount)
        {
            var result = new Dictionary<string, WorldBounds>(StringComparer.Ordinal);
            generatedBakedRenderEntityCount = 0;
            unresolvedGeneratedRendererEntityCount = 0;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                RenderBounds local = entityManager.GetComponentData<RenderBounds>(entity);
                Matrix4x4 world = ComputeBakedWorldMatrix(
                    entity,
                    entityManager,
                    worldMatrices,
                    new HashSet<Entity>());
                string key = BuildRendererBakeKey(local.Value.Center, local.Value.Extents, world);
                string stableId = ResolveIdentityByParent(entity, entityManager, identityByEntity);
                if (stableId != null)
                {
                    bool consumed = rendererBakeMap.TryConsumeOwner(key, stableId) ||
                                    rendererBakeMap.TryConsumeNearestOwner(
                                        stableId,
                                        local.Value.Center,
                                        local.Value.Extents,
                                        world,
                                        RendererBakeFallbackJoinTolerance);
                    if (!consumed)
                        unresolvedGeneratedRendererEntityCount++;
                }
                else
                {
                    stableId = rendererBakeMap.TryDequeueOwner(key);
                    if (stableId == null)
                    {
                        stableId = rendererBakeMap.TryDequeueNearestOwner(
                            local.Value.Center,
                            local.Value.Extents,
                            world,
                            RendererBakeFallbackJoinTolerance);
                    }
                }

                // A renderer without a dense generated owner is accepted base/legacy content.
                if (stableId == null)
                    continue;

                generatedBakedRenderEntityCount++;
                WorldBounds transformed = TransformBounds(local.Value.Center, local.Value.Extents, world);
                if (result.TryGetValue(stableId, out WorldBounds existing))
                    transformed = WorldBounds.Encapsulate(existing, transformed);
                result[stableId] = transformed;
            }
            return result;
        }

        private static string ResolveIdentityByParent(
            Entity entity,
            EntityManager entityManager,
            IReadOnlyDictionary<Entity, string> identityByEntity)
        {
            Entity current = entity;
            for (int depth = 0; depth < 64; depth++)
            {
                if (identityByEntity.TryGetValue(current, out string stableId))
                    return stableId;
                if (!entityManager.HasComponent<Parent>(current))
                    return null;
                current = entityManager.GetComponentData<Parent>(current).Value;
            }
            throw new InvalidOperationException(
                $"Baked transform parent depth exceeded while resolving dense-city identity for {entity}.");
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

            Matrix4x4 world;
            bool hasLocal = entityManager.HasComponent<LocalTransform>(entity);
            bool hasPostTransform = entityManager.HasComponent<PostTransformMatrix>(entity);
            bool hasParent = entityManager.HasComponent<Parent>(entity);
            if (hasLocal || hasPostTransform || hasParent)
            {
                Matrix4x4 local = hasLocal
                    ? ToMatrix(float4x4.TRS(
                        entityManager.GetComponentData<LocalTransform>(entity).Position,
                        entityManager.GetComponentData<LocalTransform>(entity).Rotation,
                        new float3(entityManager.GetComponentData<LocalTransform>(entity).Scale)))
                    : Matrix4x4.identity;
                if (hasPostTransform)
                    local *= ToMatrix(entityManager.GetComponentData<PostTransformMatrix>(entity).Value);
                if (hasParent)
                {
                    Entity parent = entityManager.GetComponentData<Parent>(entity).Value;
                    world = ComputeBakedWorldMatrix(parent, entityManager, cache, visiting) * local;
                }
                else
                    world = local;
            }
            else if (entityManager.HasComponent<LocalToWorld>(entity))
                world = ToMatrix(entityManager.GetComponentData<LocalToWorld>(entity).Value);
            else
                world = Matrix4x4.identity;

            visiting.Remove(entity);
            cache[entity] = world;
            return world;
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
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;

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
            {
                return false;
            }

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

        private static WorldBounds TransformBounds(float3 center, float3 extents, Matrix4x4 matrix)
        {
            Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = (Vector3)center +
                                 Vector3.Scale((Vector3)extents, new Vector3(x, y, z));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
            return new WorldBounds(min, max);
        }

        private static float MaxResidual(Matrix4x4 left, Matrix4x4 right)
        {
            float max = 0f;
            for (int i = 0; i < 16; i++)
                max = Mathf.Max(max, Mathf.Abs(left[i] - right[i]));
            return max;
        }

        private static float BoundsResidual(Bounds left, Bounds right) =>
            Mathf.Max(
                MaxComponentResidual(left.min, right.min),
                MaxComponentResidual(left.max, right.max));

        private static float MaxComponentResidual(Vector3 left, Vector3 right) =>
            Mathf.Max(
                Mathf.Abs(left.x - right.x),
                Mathf.Abs(left.y - right.y),
                Mathf.Abs(left.z - right.z));

        private static string GetPath(Transform transform)
        {
            var segments = new List<string>();
            while (transform != null)
            {
                segments.Add(transform.name);
                transform = transform.parent;
            }
            segments.Reverse();
            return string.Join("/", segments);
        }

        private static float[] ToArray(Matrix4x4 matrix)
        {
            var result = new float[16];
            for (int i = 0; i < 16; i++)
                result[i] = matrix[i];
            return result;
        }

        private static float[] ToArray(Bounds bounds) =>
            new[]
            {
                bounds.min.x, bounds.min.y, bounds.min.z,
                bounds.max.x, bounds.max.y, bounds.max.z
            };

        private static Matrix4x4 ToMatrix(float4x4 value)
        {
            var matrix = new Matrix4x4();
            matrix.SetColumn(0, new Vector4(value.c0.x, value.c0.y, value.c0.z, value.c0.w));
            matrix.SetColumn(1, new Vector4(value.c1.x, value.c1.y, value.c1.z, value.c1.w));
            matrix.SetColumn(2, new Vector4(value.c2.x, value.c2.y, value.c2.z, value.c2.w));
            matrix.SetColumn(3, new Vector4(value.c3.x, value.c3.y, value.c3.z, value.c3.w));
            return matrix;
        }

        private static void Reject(DenseCityGeneratedTransformParityRow row, string reason)
        {
            row.result = "Rejected";
            row.rejectionReason = reason;
        }

        private static string ComputeStringSetDigest(IEnumerable<string> values)
        {
            var canonical = new StringBuilder();
            foreach (string value in values.OrderBy(value => value, StringComparer.Ordinal))
                AppendCanonical(canonical, value);
            return ComputeSha256(canonical.ToString());
        }

        private static string ComputeRowsDigest(
            IEnumerable<DenseCityGeneratedTransformParityRow> rows)
        {
            var canonical = new StringBuilder();
            foreach (DenseCityGeneratedTransformParityRow row in rows)
            {
                AppendCanonical(canonical, row.stableId);
                AppendCanonical(canonical, row.candidatePath);
                AppendCanonical(canonical, row.candidateRoleValue.ToString(CultureInfo.InvariantCulture));
                AppendCanonical(canonical, row.bakedRoleValue.ToString(CultureInfo.InvariantCulture));
                AppendCanonical(canonical, row.roleMatches.ToString(CultureInfo.InvariantCulture));
                AppendCanonical(canonical, row.candidateWorldMatrix);
                AppendCanonical(canonical, row.bakedWorldMatrix);
                AppendCanonical(canonical, row.matrixResidual);
                AppendCanonical(canonical, row.candidateHasBounds.ToString(CultureInfo.InvariantCulture));
                AppendCanonical(canonical, row.bakedHasBounds.ToString(CultureInfo.InvariantCulture));
                AppendCanonical(canonical, row.candidateBounds);
                AppendCanonical(canonical, row.bakedBounds);
                AppendCanonical(canonical, row.boundsResidual);
                AppendCanonical(canonical, row.result);
                AppendCanonical(canonical, row.rejectionReason);
            }
            return ComputeSha256(canonical.ToString());
        }

        private static void AppendCanonical(StringBuilder builder, string value)
        {
            value ??= string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(value)
                .Append('\n');
        }

        private static void AppendCanonical(StringBuilder builder, float value) =>
            AppendCanonical(builder, value.ToString("R", CultureInfo.InvariantCulture));

        private static void AppendCanonical(StringBuilder builder, float[] values)
        {
            if (values == null)
            {
                AppendCanonical(builder, "-1");
                return;
            }
            AppendCanonical(builder, values.Length.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < values.Length; i++)
                AppendCanonical(builder, values[i]);
        }

        private static string ComputeSha256(string value)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(Utf8WithoutBom.GetBytes(value));
            return BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void WriteReport(
            string projectRoot,
            string reportPath,
            DenseCityGeneratedTransformParityReport report)
        {
            string absolutePath = Path.IsPathRooted(reportPath)
                ? reportPath
                : Path.Combine(projectRoot, reportPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true) + "\n", Utf8WithoutBom);
        }

        private readonly struct BakedIdentity
        {
            internal BakedIdentity(Entity entity, byte role)
            {
                Entity = entity;
                Role = role;
            }

            internal Entity Entity { get; }
            internal byte Role { get; }
        }

        private sealed class BakedIdentityIndex
        {
            internal BakedIdentityIndex(
                int totalCount,
                IEnumerable<string> allStableIds,
                Dictionary<string, BakedIdentity> uniqueByStableId,
                string[] duplicateStableIds,
                Dictionary<Entity, string> byEntity)
            {
                TotalCount = totalCount;
                AllStableIds = new HashSet<string>(allStableIds, StringComparer.Ordinal);
                UniqueByStableId = uniqueByStableId;
                DuplicateStableIds = duplicateStableIds;
                DuplicateStableIdSet = new HashSet<string>(duplicateStableIds, StringComparer.Ordinal);
                ByEntity = byEntity;
            }

            internal int TotalCount { get; }
            internal HashSet<string> AllStableIds { get; }
            internal Dictionary<string, BakedIdentity> UniqueByStableId { get; }
            internal string[] DuplicateStableIds { get; }
            internal HashSet<string> DuplicateStableIdSet { get; }
            internal Dictionary<Entity, string> ByEntity { get; }

            internal bool TryGetRole(string stableId, out byte role)
            {
                if (UniqueByStableId.TryGetValue(stableId, out BakedIdentity identity))
                {
                    role = identity.Role;
                    return true;
                }
                role = 0;
                return false;
            }
        }

        private readonly struct WorldBounds
        {
            internal WorldBounds(Vector3 min, Vector3 max)
            {
                Min = min;
                Max = max;
            }

            internal Vector3 Min { get; }
            internal Vector3 Max { get; }

            internal Bounds ToBounds()
            {
                var bounds = new Bounds();
                bounds.SetMinMax(Min, Max);
                return bounds;
            }

            internal float[] ToArray() =>
                new[] { Min.x, Min.y, Min.z, Max.x, Max.y, Max.z };

            internal static WorldBounds Encapsulate(WorldBounds left, WorldBounds right) =>
                new(Vector3.Min(left.Min, right.Min), Vector3.Max(left.Max, right.Max));
        }

        private sealed class CandidateRendererBakeMap
        {
            private readonly Dictionary<string, Queue<ExpectedRendererBakeEntry>> entriesByKey =
                new(StringComparer.Ordinal);
            private readonly Dictionary<(long X, long Y, long Z), List<ExpectedRendererBakeEntry>>
                entriesByFallbackCell = new();

            internal int ExpectedEntryCount { get; private set; }
            internal int UnconsumedCount { get; private set; }

            internal void Add(
                string key,
                string stableId,
                Vector3 center,
                Vector3 extents,
                Matrix4x4 world)
            {
                if (!entriesByKey.TryGetValue(key, out Queue<ExpectedRendererBakeEntry> queue))
                {
                    queue = new Queue<ExpectedRendererBakeEntry>();
                    entriesByKey.Add(key, queue);
                }
                var entry = new ExpectedRendererBakeEntry(stableId, center, extents, world);
                queue.Enqueue(entry);
                (long X, long Y, long Z) cell = GetFallbackCell(world);
                if (!entriesByFallbackCell.TryGetValue(
                        cell,
                        out List<ExpectedRendererBakeEntry> fallbackEntries))
                {
                    fallbackEntries = new List<ExpectedRendererBakeEntry>();
                    entriesByFallbackCell.Add(cell, fallbackEntries);
                }
                fallbackEntries.Add(entry);
                ExpectedEntryCount++;
                UnconsumedCount++;
            }

            internal bool TryConsumeOwner(string key, string stableId)
            {
                if (!entriesByKey.TryGetValue(key, out Queue<ExpectedRendererBakeEntry> queue))
                    return false;
                foreach (ExpectedRendererBakeEntry entry in queue)
                {
                    if (entry.Consumed ||
                        !string.Equals(entry.StableId, stableId, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    entry.Consumed = true;
                    UnconsumedCount--;
                    return true;
                }
                return false;
            }

            internal string TryDequeueOwner(string key)
            {
                if (!entriesByKey.TryGetValue(key, out Queue<ExpectedRendererBakeEntry> queue))
                    return null;
                while (queue.Count > 0 && queue.Peek().Consumed)
                    queue.Dequeue();
                if (queue.Count == 0)
                    return null;
                ExpectedRendererBakeEntry entry = queue.Dequeue();
                entry.Consumed = true;
                UnconsumedCount--;
                return entry.StableId;
            }

            internal string TryDequeueNearestOwner(
                float3 center,
                float3 extents,
                Matrix4x4 world,
                float maxResidual)
            {
                ExpectedRendererBakeEntry best = FindNearestOwner(
                    null,
                    center,
                    extents,
                    world,
                    maxResidual);
                if (best == null)
                    return null;
                best.Consumed = true;
                UnconsumedCount--;
                return best.StableId;
            }

            internal bool TryConsumeNearestOwner(
                string stableId,
                float3 center,
                float3 extents,
                Matrix4x4 world,
                float maxResidual)
            {
                ExpectedRendererBakeEntry best = FindNearestOwner(
                    stableId,
                    center,
                    extents,
                    world,
                    maxResidual);
                if (best == null)
                    return false;
                best.Consumed = true;
                UnconsumedCount--;
                return true;
            }

            private ExpectedRendererBakeEntry FindNearestOwner(
                string requiredStableId,
                float3 center,
                float3 extents,
                Matrix4x4 world,
                float maxResidual)
            {
                ExpectedRendererBakeEntry best = null;
                float bestResidual = float.PositiveInfinity;
                (long X, long Y, long Z) cell = GetFallbackCell(world);
                for (long x = cell.X - 1; x <= cell.X + 1; x++)
                for (long y = cell.Y - 1; y <= cell.Y + 1; y++)
                for (long z = cell.Z - 1; z <= cell.Z + 1; z++)
                {
                    if (!entriesByFallbackCell.TryGetValue(
                            (x, y, z),
                            out List<ExpectedRendererBakeEntry> fallbackEntries))
                    {
                        continue;
                    }
                    foreach (ExpectedRendererBakeEntry entry in fallbackEntries)
                    {
                        if (entry.Consumed ||
                            (requiredStableId != null &&
                             !string.Equals(
                                 entry.StableId,
                                 requiredStableId,
                                 StringComparison.Ordinal)))
                        {
                            continue;
                        }
                        float residual = Mathf.Max(
                            MaxResidual(entry.World, world),
                            MaxComponentResidual(entry.Center, center),
                            MaxComponentResidual(entry.Extents, extents));
                        if (residual < bestResidual)
                        {
                            best = entry;
                            bestResidual = residual;
                        }
                    }
                }
                if (best == null || bestResidual > maxResidual)
                    return null;
                return best;
            }

            private static (long X, long Y, long Z) GetFallbackCell(Matrix4x4 world) =>
                (
                    (long)Math.Floor(world.m03 / RendererBakeFallbackJoinTolerance),
                    (long)Math.Floor(world.m13 / RendererBakeFallbackJoinTolerance),
                    (long)Math.Floor(world.m23 / RendererBakeFallbackJoinTolerance)
                );

            private static float MaxComponentResidual(Vector3 left, float3 right) =>
                Mathf.Max(
                    Mathf.Abs(left.x - right.x),
                    Mathf.Abs(left.y - right.y),
                    Mathf.Abs(left.z - right.z));
        }

        private sealed class ExpectedRendererBakeEntry
        {
            internal ExpectedRendererBakeEntry(
                string stableId,
                Vector3 center,
                Vector3 extents,
                Matrix4x4 world)
            {
                StableId = stableId;
                Center = center;
                Extents = extents;
                World = world;
            }

            internal string StableId { get; }
            internal Vector3 Center { get; }
            internal Vector3 Extents { get; }
            internal Matrix4x4 World { get; }
            internal bool Consumed { get; set; }
        }

        private sealed class DenseCityGeneratedTransformParityRowComparer :
            IComparer<DenseCityGeneratedTransformParityRow>
        {
            internal static readonly DenseCityGeneratedTransformParityRowComparer Instance = new();

            public int Compare(
                DenseCityGeneratedTransformParityRow left,
                DenseCityGeneratedTransformParityRow right)
            {
                int stableId = string.Compare(left.stableId, right.stableId, StringComparison.Ordinal);
                return stableId != 0
                    ? stableId
                    : string.Compare(left.candidatePath, right.candidatePath, StringComparison.Ordinal);
            }
        }

        [Serializable]
        internal sealed class DenseCityGeneratedTransformParityReport
        {
            public string schema;
            public int schemaVersion;
            public string checkpoint;
            public string result;
            public int candidateIdentityCount;
            public int uniqueCandidateIdentityCount;
            public int bakedIdentityCount;
            public int uniqueBakedIdentityCount;
            public int generatedCandidateRendererEntityCount;
            public int generatedBakedRenderEntityCount;
            public int unresolvedGeneratedRendererEntityCount;
            public int unconsumedCandidateRendererEntityCount;
            public int rejectedRowCount;
            public int rejectedSampleCount;
            public int rejectedSampleLimit;
            public float matrixTolerance;
            public float boundsTolerance;
            public int duplicateCandidateStableIdCount;
            public int duplicateBakedStableIdCount;
            public int missingBakedStableIdCount;
            public int unexpectedBakedStableIdCount;
            public string candidateIdentitySetSha256;
            public string bakedIdentitySetSha256;
            public string evaluatedRowsSha256;
            public List<DenseCityGeneratedTransformParityRow> rejectedSamples;
        }

        [Serializable]
        internal sealed class DenseCityGeneratedTransformParityRow
        {
            public string stableId;
            public string candidatePath;
            public string candidateRole;
            public byte candidateRoleValue;
            public string bakedRole;
            public byte bakedRoleValue;
            public int roleMatches;
            public float[] candidateWorldMatrix;
            public float[] bakedWorldMatrix;
            public float matrixResidual;
            public int candidateHasBounds;
            public int bakedHasBounds;
            public float[] candidateBounds;
            public float[] bakedBounds;
            public float boundsResidual;
            public string result;
            public string rejectionReason;
        }
    }
}

#endif
