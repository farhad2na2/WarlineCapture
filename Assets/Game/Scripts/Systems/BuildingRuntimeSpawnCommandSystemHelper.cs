using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeSpawnCommandSystemHelper
    {
        public readonly struct Context
        {
            public readonly BuildingRuntimeSpawnCompositionSystemHelper RuntimeSpawnSystem;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.Context SpawnContext;

            public Context(
                BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
                BuildingRuntimeSpawnCompositionSystemHelper.Context spawnContext)
            {
                RuntimeSpawnSystem = runtimeSpawnSystem;
                SpawnContext = spawnContext;
            }
        }

        public bool TryEnqueueRuntimeBuildingSpawnRequest(
            EntityManager em,
            string buildingId,
            Vector2Int preferredOrigin,
            byte factionId,
            out int requestId,
            bool rotateVertical = false,
            bool hasOwnerFaction = true)
        {
            requestId = 0;
            string normalizedBuildingId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId);
            if (string.IsNullOrEmpty(normalizedBuildingId) ||
                !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
            {
                return false;
            }

            requestId = EnqueueRuntimeSpawnRequest(
                em,
                boundaryEntity,
                BuildingRuntimeSpawnRequest.KindBuilding,
                normalizedBuildingId,
                preferredOrigin,
                default,
                factionId,
                hasOwnerFaction,
                rotateVertical,
                allowExistingWallOverlap: false);
            return true;
        }

        public bool TryEnqueueRuntimeWallRunSpawnRequest(
            EntityManager em,
            string wallId,
            Vector2Int startOrigin,
            Vector2Int endOrigin,
            byte factionId,
            out int requestId)
        {
            requestId = 0;
            string normalizedWallId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(wallId);
            if (string.IsNullOrEmpty(normalizedWallId) ||
                !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
            {
                return false;
            }

            requestId = EnqueueRuntimeSpawnRequest(
                em,
                boundaryEntity,
                BuildingRuntimeSpawnRequest.KindWallRun,
                normalizedWallId,
                startOrigin,
                endOrigin,
                factionId,
                hasOwnerFaction: true,
                rotateVertical: false,
                allowExistingWallOverlap: false);
            return true;
        }

        public bool TryEnqueueRuntimeWallSegmentSpawnRequest(
            EntityManager em,
            string wallId,
            Vector2Int origin,
            bool rotateVertical,
            byte factionId,
            bool allowExistingWallOverlap,
            out int requestId)
        {
            requestId = 0;
            string normalizedWallId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(wallId);
            if (string.IsNullOrEmpty(normalizedWallId) ||
                !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity))
            {
                return false;
            }

            requestId = EnqueueRuntimeSpawnRequest(
                em,
                boundaryEntity,
                BuildingRuntimeSpawnRequest.KindWallSegment,
                normalizedWallId,
                origin,
                default,
                factionId,
                hasOwnerFaction: true,
                rotateVertical,
                allowExistingWallOverlap);
            return true;
        }

        public bool TryGetRuntimeSpawnRequestResult(
            EntityManager em,
            int requestId,
            out BuildingRuntimeSpawnRequest result)
        {
            result = default;
            if (requestId <= 0 || !TryGetRuntimeBoundaryEntity(em, out Entity boundaryEntity) ||
                !em.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
            {
                return false;
            }

            DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].RequestId != requestId)
                    continue;

                result = requests[i];
                return true;
            }

            return false;
        }

        internal static bool TryGetRuntimeBoundaryEntity(EntityManager em, out Entity boundaryEntity)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            if (!query.IsEmptyIgnoreFilter)
            {
                boundaryEntity = query.GetSingletonEntity();
                return em.Exists(boundaryEntity);
            }

            boundaryEntity = Entity.Null;
            return false;
        }

        private static DynamicBuffer<BuildingRuntimeSpawnRequest> EnsureRuntimeSpawnRequestBuffer(
            EntityManager em,
            Entity boundaryEntity)
        {
            if (!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
                em.AddBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);

            return em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        }

        private static int EnqueueRuntimeSpawnRequest(
            EntityManager em,
            Entity boundaryEntity,
            byte requestKind,
            string normalizedBuildingId,
            Vector2Int preferredOrigin,
            Vector2Int endOrigin,
            byte factionId,
            bool hasOwnerFaction,
            bool rotateVertical,
            bool allowExistingWallOverlap)
        {
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests = EnsureRuntimeSpawnRequestBuffer(em, boundaryEntity);
            int requestId = NextRequestId(requests);
            requests.Add(new BuildingRuntimeSpawnRequest
            {
                RequestId = requestId,
                RequestKind = requestKind,
                FactionId = factionId,
                HasOwnerFaction = hasOwnerFaction ? (byte)1 : (byte)0,
                BuildingId = new FixedString128Bytes(normalizedBuildingId),
                PreferredOrigin = new int2(preferredOrigin.x, preferredOrigin.y),
                EndOrigin = new int2(endOrigin.x, endOrigin.y),
                RotateVertical = rotateVertical ? (byte)1 : (byte)0,
                AllowExistingWallOverlap = allowExistingWallOverlap ? (byte)1 : (byte)0,
                Status = BuildingRuntimeSpawnRequest.Pending
            });
            return requestId;
        }

        private static int NextRequestId(DynamicBuffer<BuildingRuntimeSpawnRequest> requests)
        {
            int nextRequestId = 0;
            for (int i = 0; i < requests.Length; i++)
                nextRequestId = Mathf.Max(nextRequestId, requests[i].RequestId);

            return nextRequestId + 1;
        }
    }
}
