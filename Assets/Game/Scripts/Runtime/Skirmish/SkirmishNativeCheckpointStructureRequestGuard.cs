using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>Small owner marker so a missing/stale guard cannot fall through to a legacy grant path.</summary>
    internal struct SkirmishNativeCheckpointStructureRequestGuardTag : IComponentData { }

    /// <summary>
    /// Small immutable receipt created by the validated checkpoint reconstruction owner.
    /// It carries only the exact identity needed by the shared spawn processor; it does
    /// not retain snapshot, codec, prefab, or checkpoint-owner objects.
    /// </summary>
    internal sealed class SkirmishNativeCheckpointStructureRequestGuard : IComponentData
    {
        internal readonly struct RequestIdentity
        {
            internal readonly int RequestId;
            internal readonly int EntryIndex;
            internal readonly FixedString128Bytes BuildingId;
            internal readonly byte FactionId;
            internal readonly int2 Origin;
            internal readonly int2 Footprint;

            internal RequestIdentity(int requestId, int entryIndex, string buildingId, byte factionId,
                int2 origin, int2 footprint)
            {
                RequestId = requestId;
                EntryIndex = entryIndex;
                BuildingId = new FixedString128Bytes(buildingId);
                FactionId = factionId;
                Origin = origin;
                Footprint = footprint;
            }
        }

        private readonly Entity session;
        private readonly RequestIdentity[] requests;

        // Entities may construct an empty managed component; it never validates a request.
        public SkirmishNativeCheckpointStructureRequestGuard()
        {
            session = Entity.Null;
            requests = Array.Empty<RequestIdentity>();
        }

        private SkirmishNativeCheckpointStructureRequestGuard(Entity owner, RequestIdentity[] validatedRequests)
        {
            session = owner;
            requests = validatedRequests == null ? Array.Empty<RequestIdentity>() :
                (RequestIdentity[])validatedRequests.Clone();
        }

        // Called by TryBegin only after the full checkpoint codec and each saved
        // RuntimeBuildingCombatInfo component have passed their existing validation.
        internal static SkirmishNativeCheckpointStructureRequestGuard CreateValidated(
            Entity owner, RequestIdentity[] validatedRequests) =>
            new SkirmishNativeCheckpointStructureRequestGuard(owner, validatedRequests);

        internal bool Matches(in BuildingRuntimeSpawnRequest request, int2 configuredFootprint)
        {
            if (session == Entity.Null || request.PlanEntity != session || request.RequestId <= 0 ||
                request.Status != BuildingRuntimeSpawnRequest.Pending ||
                request.RequestKind != BuildingRuntimeSpawnRequest.KindBuilding ||
                request.AuthoredStartingGrant != 1 || request.HasOwnerFaction != 1 ||
                request.AllowNonBuildableEnemy != 0 || request.RequirePreferredOrigin != 1 ||
                request.RequireTypedPad != 0 || request.RotateVertical != 0 ||
                request.AllowExistingWallOverlap != 0 || request.EntryIndex < 0 || request.EntryIndex >= requests.Length)
                return false;

            RequestIdentity identity = requests[request.EntryIndex];
            return identity.RequestId == request.RequestId && identity.EntryIndex == request.EntryIndex &&
                identity.BuildingId.Equals(request.BuildingId) && identity.FactionId == request.FactionId &&
                identity.Origin.Equals(request.PreferredOrigin) && identity.Footprint.Equals(configuredFootprint) &&
                identity.Footprint.x > 0 && identity.Footprint.y > 0;
        }
    }
}
