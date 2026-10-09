using System;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    using ProductionTransportMode = BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode;

    internal sealed partial class BuildingProductionTransportPresentationSystemHelper
    {
        internal void BindCheckpointProductionOwner(Context context, RuntimeBuildingEntity building,
            SkirmishNativeBuildingCheckpointRecord record, SkirmishNativeCheckpointPrefabRegistry prefabs = null)
        {
            if (record == null || record.Building != building) return;
            if (prefabs != null) record.Prefabs = prefabs;
            if (record.TryRestoreProduction != null) return;
            record.TryRestoreProduction = prepared => TryCommitCheckpointProduction(context, building, prepared, Time.time);
            record.RollbackProduction = prepared => RollbackCheckpointProduction(building, prepared);
        }

        internal void RollbackCheckpointProduction(RuntimeBuildingEntity building, SkirmishNativeProductionCheckpoint.Prepared prepared)
        {
            if (building == null || prepared == null || !ReferenceEquals(building.PendingProductions, prepared.Queue)) return;
            var delivery = building.ActiveTransport;
            if (delivery != null)
            {
                var drop = delivery.ActiveDrop;
                if (drop != null)
                {
                    ReturnTransportDropVisual(drop.Prefab, drop.Visual);
                    ReturnTransportDropRope(drop.Rope);
                    drop.Visual = null;
                    drop.Rope = null;
                }
                ReturnProductionTransportInstance(delivery.Prefab, delivery.Instance);
                delivery.Instance = null;
                delivery.Transform = null;
            }
            building.ActiveTransport = null;
            building.PendingProductions = new System.Collections.Generic.List<RuntimeBuildingEntity.PendingProduction>();
            building.ProducedUnits = new System.Collections.Generic.List<Unity.Entities.Entity>();
            building.ProducedUnitSlots = new Unity.Entities.Entity[prepared.Slots.Length];
            building.ProducedUnitPrefabs = new System.Collections.Generic.Dictionary<Unity.Entities.Entity, GameObject>();
            building.ProducedUnitSourceKeys = new System.Collections.Generic.Dictionary<Unity.Entities.Entity, Unity.Collections.FixedString64Bytes>();
            _managedDeliveryCountPrimed = false;
        }

        // Commit only into a newly reconstructed building. Existing paid queues
        // must never be overwritten or silently refunded during recovery.
        internal bool TryCommitCheckpointProduction(Context context, RuntimeBuildingEntity building,
            SkirmishNativeProductionCheckpoint.Prepared prepared, float now)
        {
            if (building == null || prepared == null || !math.isfinite(now) || building.ActiveTransport != null ||
                (building.PendingProductions != null && building.PendingProductions.Count != 0) ||
                (building.ProducedUnits != null && building.ProducedUnits.Count != 0) ||
                (building.ProducedUnitSlots?.Length ?? 0) != prepared.Slots.Length)
                return false;

            var delivery = prepared.Delivery;
            bool committed = false;
            try
            {
                if (delivery != null)
                {
                    delivery.Instance = AcquireProductionTransportInstance(delivery.Prefab, context.VisualSystem);
                    delivery.Transform = delivery.Instance.transform;
                    delivery.VisualRenderers = GetProductionTransportRenderers(delivery.Instance);
                    delivery.DoorTransform = GetProductionTransportDoorTransform(delivery.Instance, context.VisualSystem);
                    delivery.Transform.SetPositionAndRotation(prepared.TransportPosition, prepared.TransportRotation);
                    var drop = delivery.ActiveDrop;
                    if (drop != null)
                    {
                        drop.Visual = AcquireTransportDropVisual(drop.Prefab, context.PrepareTransportDropVisual);
                        float progress = Mathf.Clamp01((now - drop.StartedAt) / drop.Duration);
                        drop.Visual.transform.position = Vector3.Lerp(drop.StartPosition, drop.EndPosition, progress);
                        if (delivery.Mode == ProductionTransportMode.Plane)
                            drop.Visual.transform.rotation = Quaternion.LookRotation(-delivery.Transform.forward, Vector3.up);
                        if (delivery.Mode == ProductionTransportMode.Helicopter)
                        {
                            drop.Rope = AcquireTransportDropRope();
                            drop.Rope.transform.SetParent(delivery.Transform, false);
                            drop.Rope.positionCount = 2;
                            drop.Rope.widthMultiplier = 0.05f;
                            drop.Rope.startColor = new Color(0.82f, 0.82f, 0.82f, 0.95f);
                            drop.Rope.endColor = drop.Rope.startColor;
                            Vector3 anchor = ResolveTransportVisualCenterWorld(delivery);
                            Vector3 position = drop.Visual.transform.position;
                            drop.Rope.SetPosition(0, new Vector3(position.x, anchor.y, position.z));
                            drop.Rope.SetPosition(1, position);
                        }
                    }
                }

                building.PendingProductions = prepared.Queue;
                building.ProducedUnits = prepared.Units;
                building.ProducedUnitSlots = prepared.Slots;
                building.ProducedUnitPrefabs = prepared.Prefabs;
                building.ProducedUnitSourceKeys = prepared.SourceKeys;
                building.ActiveTransport = delivery;
                _managedDeliveryCountPrimed = false;
                committed = true;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is UnityException)
            {
                Debug.LogWarning("Checkpoint production visual reconstruction failed: " + exception.Message);
                return false;
            }
            finally
            {
                if (!committed && delivery != null)
                {
                    var drop = delivery.ActiveDrop;
                    if (drop != null)
                    {
                        ReturnTransportDropVisual(drop.Prefab, drop.Visual);
                        ReturnTransportDropRope(drop.Rope);
                        drop.Visual = null;
                        drop.Rope = null;
                    }
                    ReturnProductionTransportInstance(delivery.Prefab, delivery.Instance);
                    delivery.Instance = null;
                    delivery.Transform = null;
                }
            }
        }
    }
}
