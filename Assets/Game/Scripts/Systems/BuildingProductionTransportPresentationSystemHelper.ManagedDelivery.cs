using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    using ProductionTransportMode = BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode;

    internal sealed partial class BuildingProductionTransportPresentationSystemHelper
    {
        private const float HelicopterDepartureDistance = 180f;
        private const float HelicopterDepartureHeight = 32f;
        private const float HelicopterMinimumDepartureSeconds = 4f;

        private int _activeManagedDeliveryCount;
        private bool _managedDeliveryCountPrimed;

        public bool TryEnsureActiveProductionTransport(
            Context context,
            RuntimeBuildingEntity building,
            RuntimeBuildingEntity.PendingProduction pending,
            float now,
            ref uint randomState)
        {
            if (building == null || pending == null || pending.TransportPrefab == null || building.ActiveTransport != null)
                return building?.ActiveTransport != null;

            Vector3 hoverPosition;
            Vector3 entryPosition;
            Vector3 touchdownPosition;
            Vector3 exitPosition;
            Quaternion hoverRotation;
            Quaternion entryRotation;
            Quaternion exitRotation;
            Vector3 committedDropPosition = default;
            int2 committedDropCell = default;
            bool hasCommittedDropPosition = false;
            int laneIndex = 0;

            if (pending.TransportMode == ProductionTransportMode.Plane)
            {
                if (context.RunwaySystem == null ||
                    !context.RunwaySystem.TryGetNearestAirportRunway(
                        context.RuntimeBuildings,
                        building.Instance != null ? building.Instance.transform.position : Vector3.zero,
                        out _,
                        out Vector3 runwayCenter,
                        out Quaternion runwayRotation,
                        out Vector3 runwayHalfExtents))
                {
                    return false;
                }

                if (!TryAcquireProductionTransportLane(context, pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                    return false;

                Vector3 runwayAxis = runwayRotation * Vector3.forward;
                runwayAxis.y = 0f;
                if (runwayAxis.sqrMagnitude <= 0.0001f)
                    runwayAxis = Vector3.forward;
                runwayAxis.Normalize();

                float runwayHalfLength = Mathf.Max(8f, runwayHalfExtents.z);
                Vector3 runwayStart = runwayCenter - (runwayAxis * runwayHalfLength);
                touchdownPosition = runwayStart + (runwayAxis * Mathf.Min(8f, runwayHalfLength * 0.35f));
                touchdownPosition.y = runwayCenter.y + RunwaySurfaceClearance;
                hoverPosition = runwayCenter;
                hoverPosition.y = touchdownPosition.y;
                entryPosition = touchdownPosition - (runwayAxis * Mathf.Max(80f, runwayHalfExtents.z * 5f)) + new Vector3(0f, 28f, 0f);
                exitPosition = hoverPosition + (runwayAxis * Mathf.Max(90f, runwayHalfExtents.z * 6f)) + new Vector3(0f, 32f, 0f);
                hoverRotation = Quaternion.LookRotation(runwayAxis, Vector3.up);
                entryRotation = hoverRotation;
                exitRotation = hoverRotation;
            }
            else if (pending.TransportMode == ProductionTransportMode.AirSelf)
            {
                if (!TryAcquireProductionTransportLane(context, pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                    return false;

                touchdownPosition = ResolveProductionTransportDropPosition(context, building, pending, ref randomState);
                hoverPosition = touchdownPosition + new Vector3(0f, 6f, 0f);
                hoverPosition += ResolveProductionTransportLaneOffset(context, laneIndex, pending.TransportMaxConcurrent);
                Vector3 horizontalOffset = context.WorldCamera != null
                    ? -context.WorldCamera.transform.right.normalized * 70f
                    : new Vector3(-70f, 0f, 0f);
                entryPosition = hoverPosition + horizontalOffset + new Vector3(0f, 16f, 0f);
                exitPosition = hoverPosition;
                hoverRotation = Quaternion.LookRotation((hoverPosition - entryPosition).normalized, Vector3.up);
                entryRotation = hoverRotation;
                exitRotation = hoverRotation;
            }
            else
            {
                if (!TryAcquireProductionTransportLane(context, pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                    return false;

                hoverPosition = ResolveProductionTransportHoverPosition(building, pending);
                hoverPosition += ResolveProductionTransportLaneOffset(context, laneIndex, pending.TransportMaxConcurrent);
                // The landing point belongs to this transport. Re-resolving it between drops visibly teleports the carrier.
                if (!TryResolveClearHelicopterDropPosition(
                        context,
                        building,
                        pending,
                        hoverPosition,
                        null,
                        pending.TransportClearDropSearchStartRadius,
                        out bool exhaustedSearchBudget,
                        out int nextSearchRadius,
                        out committedDropCell,
                        out committedDropPosition))
                {
                    pending.TransportClearDropSearchStartRadius = exhaustedSearchBudget
                        ? nextSearchRadius
                        : 0;
                    return false;
                }

                pending.TransportClearDropSearchStartRadius = 0;
                hasCommittedDropPosition = true;
                hoverPosition.x = committedDropPosition.x;
                hoverPosition.z = committedDropPosition.z;
                Vector3 horizontalOffset = context.WorldCamera != null
                    ? -context.WorldCamera.transform.right.normalized * 60f
                    : new Vector3(-60f, 0f, 0f);
                entryPosition = hoverPosition + horizontalOffset;
                Vector3 departureDirection = -horizontalOffset.normalized;
                exitPosition = hoverPosition +
                               (departureDirection * HelicopterDepartureDistance) +
                               new Vector3(0f, HelicopterDepartureHeight, 0f);
                entryPosition.y = hoverPosition.y + 12f;
                touchdownPosition = hoverPosition;
                hoverRotation = Quaternion.LookRotation((hoverPosition - entryPosition).normalized, Vector3.up);
                entryRotation = hoverRotation;
                exitRotation = Quaternion.LookRotation((exitPosition - hoverPosition).normalized, Vector3.up);
            }

            GameObject instance = AcquireProductionTransportInstance(pending.TransportPrefab, context.VisualSystem);
            Transform doorTransform = GetProductionTransportDoorTransform(instance, context.VisualSystem);

            RuntimeBuildingEntity.ActiveProductionTransport transport = AcquireProductionTransportState();
            transport.LaneIndex = laneIndex;
            transport.Prefab = pending.TransportPrefab;
            transport.Instance = instance;
            transport.Transform = instance.transform;
            transport.VisualRenderers = GetProductionTransportRenderers(instance);
            transport.DoorTransform = doorTransform;
            transport.DoorOpenLocalEulerX = doorTransform != null ? doorTransform.localEulerAngles.x : 0f;
            transport.HoverPosition = hoverPosition;
            transport.EntryPosition = entryPosition;
            transport.TouchdownPosition = touchdownPosition;
            transport.ExitPosition = exitPosition;
            transport.CommittedDropPosition = committedDropPosition;
            transport.CommittedDropCell = committedDropCell;
            transport.HoverRotation = hoverRotation;
            transport.EntryRotation = entryRotation;
            transport.ExitRotation = exitRotation;
            transport.ArrivalSeconds = Mathf.Max(0.5f, pending.TransportArrivalSeconds);
            transport.DepartureSeconds = pending.TransportMode == ProductionTransportMode.Helicopter
                ? Mathf.Max(HelicopterMinimumDepartureSeconds, pending.TransportArrivalSeconds)
                : Mathf.Max(0.5f, pending.TransportArrivalSeconds);
            transport.HoldForNextReadySeconds = Mathf.Max(0.5f, pending.TransportHoldForNextReadySeconds);
            transport.PhaseStartedAt = now;
            transport.HoverEnteredAt = -1f;
            transport.NextDropReadyAt = now;
            transport.NextClearDropSearchAt = now;
            transport.ClearDropFailureCount = 0;
            transport.ClearDropSearchStartRadius = 0;
            transport.DeliveredUnitCount = 0;
            transport.Phase = 0;
            transport.Mode = pending.TransportMode;
            transport.HasCommittedDropPosition = hasCommittedDropPosition;
            transport.FocusRequested = false;
            transport.ActiveDrop = null;

            if (transport.Mode == ProductionTransportMode.Helicopter && transport.HasCommittedDropPosition)
                AlignHelicopterTransportAnchorOverDrop(transport, transport.CommittedDropPosition);

            transport.Transform.position = transport.EntryPosition;
            transport.Transform.rotation = transport.EntryRotation;
            SetProductionTransportDoorOpen01(transport, 0f);
            PrimeManagedDeliveryCount(context);
            building.ActiveTransport = transport;
            _activeManagedDeliveryCount++;
            PublishManagedDeliveryReadModel(context);
            return true;
        }

        private static bool TryResolveCommittedHelicopterDropPosition(
            Context context,
            RuntimeBuildingEntity.ActiveProductionTransport transport,
            out int2 dropCell,
            out Vector3 dropPosition)
        {
            dropCell = transport.CommittedDropCell;
            dropPosition = transport.CommittedDropPosition;
            if (!transport.HasCommittedDropPosition)
                return false;

            int2 offset = transport.DeliveredUnitCount switch
            {
                1 => new int2(1, 0),
                2 => new int2(-1, 0),
                3 => new int2(0, 1),
                _ => int2.zero
            };
            if (offset.Equals(int2.zero) ||
                context.TransportBridgeContext.TryGetEntityManager == null ||
                !context.TransportBridgeContext.TryGetEntityManager(out EntityManager entityManager) ||
                context.TransportBridgeContext.TryGetGridData == null ||
                !context.TransportBridgeContext.TryGetGridData(
                    out _,
                    out GridConfig grid,
                    out _,
                    out _))
            {
                return true;
            }

            dropCell += offset;
            float3 resolved = GridUtils.CellToWorldCenter(grid, dropCell);
            new MapSurfaceSpawnGrounding().TryGroundCellCenter(
                entityManager,
                grid,
                dropCell,
                ref resolved,
                out _);
            dropPosition = new Vector3(resolved.x, resolved.y, resolved.z);
            return true;
        }

        private void PublishManagedDeliveryReadModel(Context context)
        {
            PrimeManagedDeliveryCount(context);
            PublishManagedDeliveryReadModel(context, _activeManagedDeliveryCount);
        }

        private static int ResolveManagedDeliveryCount(Context context)
        {
            int activeCount = 0;
            if (context.RuntimeBuildings == null)
                return activeCount;

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
            {
                if (pair.Value?.ActiveTransport != null)
                    activeCount++;
            }

            return activeCount;
        }

        private static void PublishManagedDeliveryReadModel(Context context, int activeCount)
        {
            BuildingProductionTransportBridgeCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager =
                context.TransportBridgeContext.TryGetEntityManager;
            BuildingSpawnCompositionSystemHelper.TryGetRuntimeBoundaryEntityDelegate tryGetBoundary =
                context.TransportBridgeContext.SpawnContext.TryGetRuntimeBoundaryEntity;
            if (tryGetEntityManager == null || tryGetBoundary == null ||
                !tryGetEntityManager(out EntityManager entityManager) ||
                !tryGetBoundary(entityManager, out Entity boundary) ||
                boundary == Entity.Null ||
                !entityManager.Exists(boundary) ||
                !entityManager.HasComponent<BuildingProductionDeliveryReadModel>(boundary))
            {
                return;
            }

            BuildingProductionDeliveryReadModel current =
                entityManager.GetComponentData<BuildingProductionDeliveryReadModel>(boundary);
            if (current.ActiveManagedDeliveryCount == activeCount)
                return;

            current.ActiveManagedDeliveryCount = activeCount;
            current.Version++;
            entityManager.SetComponentData(boundary, current);
        }

        private void PrimeManagedDeliveryCount(Context context)
        {
            if (_managedDeliveryCountPrimed)
                return;

            _activeManagedDeliveryCount = ResolveManagedDeliveryCount(context);
            _managedDeliveryCountPrimed = true;
        }
    }
}
