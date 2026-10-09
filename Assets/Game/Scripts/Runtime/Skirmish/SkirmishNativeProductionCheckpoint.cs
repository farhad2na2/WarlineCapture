using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    using ProductionTransportMode = BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode;

    // Native production data contains stable prefab/actor keys and relative clocks.
    // The recovery owner must recreate the building before committing this state.
    internal static class SkirmishNativeProductionCheckpoint
    {
        [Serializable] internal sealed class Snapshot
        {
            public QueueItem[] Queue = Array.Empty<QueueItem>();
            public string[] ProducedUnits = Array.Empty<string>();
            public string[] ProducedSlots = Array.Empty<string>();
            public string[] ProducedPrefabKeys = Array.Empty<string>();
            public string[] ProducedSourceKeys = Array.Empty<string>();
            public bool HasDelivery;
            public Transport Delivery;
        }
        [Serializable] internal sealed class QueueItem
        {
            public string PrefabKey;
            public string TransportPrefabKey;
            public string ReceiptOwner;
            public string ReceiptAttempt;
            public int ProductionIndex;
            public float StartedAt;
            public float ReadyAt;
            public int ReservedProductionSlotIndex;
            public float TransportArrivalSeconds;
            public float TransportHoldForNextReadySeconds;
            public int TransportMaxConcurrent;
            public int RemainingQuantity;
            public int RefundableMaterials;
            public int PaidQuantity;
            public uint ReceiptId;
            public int TransportClearDropSearchStartRadius;
            public ProductionTransportMode TransportMode;
            public bool TransportRequiresAirportRunway;
        }
        [Serializable] internal sealed class Transport
        {
            public string PrefabKey;
            public Vector3 Position;
            public Quaternion Rotation;
            public bool HasActiveDrop;
            public Drop ActiveDrop;
            public int LaneIndex;
            public float DoorOpenLocalEulerX;
            public Vector3 EntryPosition;
            public Vector3 TouchdownPosition;
            public Vector3 HoverPosition;
            public Vector3 ExitPosition;
            public Vector3 CommittedDropPosition;
            public int2 CommittedDropCell;
            public Quaternion HoverRotation;
            public Quaternion EntryRotation;
            public Quaternion ExitRotation;
            public float ArrivalSeconds;
            public float DepartureSeconds;
            public float HoldForNextReadySeconds;
            public float PhaseStartedAt;
            public byte Phase;
            public float HoverEnteredAt;
            public float NextDropReadyAt;
            public float NextClearDropSearchAt;
            public byte ClearDropFailureCount;
            public int ClearDropSearchStartRadius;
            public int DeliveredUnitCount;
            public ProductionTransportMode Mode;
            public bool HasCommittedDropPosition;
            public bool FocusRequested;
        }
        [Serializable] internal sealed class Drop
        {
            public int QueueIndex;
            public string PrefabKey;
            public float StartedAt;
            public float Duration;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public int2 FinalGoalCell;
        }

        internal static Snapshot Capture(RuntimeBuildingEntity building, float now,
            Func<GameObject, string> prefabKey, Func<Entity, string> actorKey)
        {
            if (building == null || !math.isfinite(now)) throw new ArgumentException("Invalid production capture.");
            string Prefab(GameObject prefab)
            {
                if (prefab == null) return "";
                string key = prefabKey(prefab);
                return !string.IsNullOrEmpty(key) ? key : throw new ArgumentException("Production prefab has no stable key.");
            }
            string Actor(Entity entity)
            {
                if (entity == Entity.Null) return "";
                string key = actorKey(entity);
                return !string.IsNullOrEmpty(key) ? key : throw new ArgumentException("Production actor has no stable key.");
            }
            var queues = building.PendingProductions ?? new List<RuntimeBuildingEntity.PendingProduction>();
            var result = new Snapshot { Queue = new QueueItem[queues.Count] };
            for (int i = 0; i < queues.Count; i++)
            {
                var pending = queues[i];
                result.Queue[i] = new QueueItem
                {
                    PrefabKey = Prefab(pending.Prefab), TransportPrefabKey = Prefab(pending.TransportPrefab),
                    ReceiptOwner = Actor(pending.ReceiptOwner), ReceiptAttempt = pending.ReceiptAttempt.ToString(),
                    ProductionIndex = pending.ProductionIndex,
                    StartedAt = pending.StartedAt - now,
                    ReadyAt = pending.ReadyAt - now,
                    ReservedProductionSlotIndex = pending.ReservedProductionSlotIndex,
                    TransportArrivalSeconds = pending.TransportArrivalSeconds,
                    TransportHoldForNextReadySeconds = pending.TransportHoldForNextReadySeconds,
                    TransportMaxConcurrent = pending.TransportMaxConcurrent,
                    RemainingQuantity = pending.RemainingQuantity,
                    RefundableMaterials = pending.RefundableMaterials,
                    PaidQuantity = pending.PaidQuantity,
                    ReceiptId = pending.ReceiptId,
                    TransportClearDropSearchStartRadius = pending.TransportClearDropSearchStartRadius,
                    TransportMode = pending.TransportMode,
                    TransportRequiresAirportRunway = pending.TransportRequiresAirportRunway,
                };
            }
            result.ProducedUnits = (building.ProducedUnits ?? new List<Entity>()).ConvertAll(Actor).ToArray();
            result.ProducedPrefabKeys = new string[result.ProducedUnits.Length];
            result.ProducedSourceKeys = new string[result.ProducedUnits.Length];
            for (int i = 0; i < result.ProducedUnits.Length; i++)
            {
                Entity entity = building.ProducedUnits[i];
                result.ProducedPrefabKeys[i] = building.ProducedUnitPrefabs != null && building.ProducedUnitPrefabs.TryGetValue(entity, out var unitPrefab)
                    ? Prefab(unitPrefab) : "";
                result.ProducedSourceKeys[i] = building.ProducedUnitSourceKeys != null && building.ProducedUnitSourceKeys.TryGetValue(entity, out var sourceKey)
                    ? sourceKey.ToString() : "";
            }
            var slots = building.ProducedUnitSlots ?? Array.Empty<Entity>();
            result.ProducedSlots = new string[slots.Length];
            for (int i = 0; i < slots.Length; i++) result.ProducedSlots[i] = Actor(slots[i]);
            var delivery = building.ActiveTransport;
            if (delivery != null)
            {
                if (delivery.Transform == null) throw new ArgumentException("Delivery visual is missing.");
                result.HasDelivery = true;
                result.Delivery = new Transport
                {
                    PrefabKey = Prefab(delivery.Prefab), Position = delivery.Transform.position, Rotation = delivery.Transform.rotation,
                    LaneIndex = delivery.LaneIndex,
                    DoorOpenLocalEulerX = delivery.DoorOpenLocalEulerX,
                    EntryPosition = delivery.EntryPosition,
                    TouchdownPosition = delivery.TouchdownPosition,
                    HoverPosition = delivery.HoverPosition,
                    ExitPosition = delivery.ExitPosition,
                    CommittedDropPosition = delivery.CommittedDropPosition,
                    CommittedDropCell = delivery.CommittedDropCell,
                    HoverRotation = delivery.HoverRotation,
                    EntryRotation = delivery.EntryRotation,
                    ExitRotation = delivery.ExitRotation,
                    ArrivalSeconds = delivery.ArrivalSeconds,
                    DepartureSeconds = delivery.DepartureSeconds,
                    HoldForNextReadySeconds = delivery.HoldForNextReadySeconds,
                    PhaseStartedAt = delivery.PhaseStartedAt - now,
                    Phase = delivery.Phase,
                    HoverEnteredAt = delivery.HoverEnteredAt - now,
                    NextDropReadyAt = delivery.NextDropReadyAt - now,
                    NextClearDropSearchAt = delivery.NextClearDropSearchAt - now,
                    ClearDropFailureCount = delivery.ClearDropFailureCount,
                    ClearDropSearchStartRadius = delivery.ClearDropSearchStartRadius,
                    DeliveredUnitCount = delivery.DeliveredUnitCount,
                    Mode = delivery.Mode,
                    HasCommittedDropPosition = delivery.HasCommittedDropPosition,
                    FocusRequested = delivery.FocusRequested,
                };
                var drop = delivery.ActiveDrop;
                if (drop != null)
                {
                    int index = queues.IndexOf(drop.Production);
                    if (index < 0) throw new ArgumentException("Delivery is detached from its paid queue.");
                    result.Delivery.HasActiveDrop = true;
                    result.Delivery.ActiveDrop = new Drop
                    {
                        QueueIndex = index, PrefabKey = Prefab(drop.Prefab), StartedAt = drop.StartedAt - now,
                        Duration = drop.Duration, StartPosition = drop.StartPosition,
                        EndPosition = drop.EndPosition, FinalGoalCell = drop.FinalGoalCell
                    };
                }
            }
            return result;
        }

        // No payment, grant, queue insertion API or delivered-unit spawn is invoked.
        // Resolve all references and stage the replacement before committing it.
        internal static bool TryPrepare(Snapshot saved, float now, Func<string, GameObject> prefab,
            Func<string, Entity> actor, out Prepared prepared)
        {
            prepared = null;
            if (saved?.Queue == null || saved.ProducedUnits == null || saved.ProducedSlots == null ||
                saved.ProducedPrefabKeys == null || saved.ProducedSourceKeys == null ||
                saved.ProducedPrefabKeys.Length != saved.ProducedUnits.Length || saved.ProducedSourceKeys.Length != saved.ProducedUnits.Length ||
                saved.Queue.Length > 65536 || saved.ProducedUnits.Length > 65536 || saved.ProducedSlots.Length > 65536 ||
                !math.isfinite(now)) return false;
            try
            {
                GameObject Prefab(string key, bool required)
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        if (required) throw new ArgumentException("Required production prefab is missing.");
                        return null;
                    }
                    return prefab(key) ?? throw new ArgumentException("Unknown production prefab.");
                }
                Entity Actor(string key)
                {
                    if (key == null) throw new ArgumentException("Actor key is null.");
                    if (key.Length == 0) return Entity.Null;
                    Entity value = actor(key);
                    if (value == Entity.Null) throw new ArgumentException("Unknown production actor.");
                    return value;
                }
                var value = new Prepared();
                for (int i = 0; i < saved.Queue.Length; i++)
                {
                    var item = saved.Queue[i];
                    if (item == null || item.RemainingQuantity < 1 || item.PaidQuantity < 0 || item.RefundableMaterials < 0 ||
                        item.ReservedProductionSlotIndex < -1 || item.ProductionIndex < 0 || item.TransportMaxConcurrent < 0 ||
                        item.TransportArrivalSeconds < 0 || item.TransportHoldForNextReadySeconds < 0 ||
                        !Enum.IsDefined(typeof(ProductionTransportMode), item.TransportMode) || !math.isfinite(item.StartedAt) || !math.isfinite(item.ReadyAt) ||
                        item.ReadyAt < item.StartedAt || item.ReceiptAttempt == null ||
                        (item.ReceiptId != 0 && (string.IsNullOrEmpty(item.ReceiptOwner) || string.IsNullOrEmpty(item.ReceiptAttempt))) ||
                        (item.PaidQuantity > 0 && (item.RemainingQuantity > item.PaidQuantity || item.ReceiptId == 0 ||
                         string.IsNullOrEmpty(item.ReceiptOwner) || string.IsNullOrEmpty(item.ReceiptAttempt)))) return false;
                    if (!math.isfinite(item.StartedAt) ||
                        !math.isfinite(item.ReadyAt) ||
                        !math.isfinite(item.TransportArrivalSeconds) ||
                        !math.isfinite(item.TransportHoldForNextReadySeconds)) return false;
                    value.Queue.Add(new RuntimeBuildingEntity.PendingProduction
                    {
                        Prefab = Prefab(item.PrefabKey, true), TransportPrefab = Prefab(item.TransportPrefabKey, false),
                        ReceiptOwner = Actor(item.ReceiptOwner), ReceiptAttempt = new FixedString64Bytes(item.ReceiptAttempt),
                        ProductionIndex = item.ProductionIndex,
                        StartedAt = item.StartedAt + now,
                        ReadyAt = item.ReadyAt + now,
                        ReservedProductionSlotIndex = item.ReservedProductionSlotIndex,
                        TransportArrivalSeconds = item.TransportArrivalSeconds,
                        TransportHoldForNextReadySeconds = item.TransportHoldForNextReadySeconds,
                        TransportMaxConcurrent = item.TransportMaxConcurrent,
                        RemainingQuantity = item.RemainingQuantity,
                        RefundableMaterials = item.RefundableMaterials,
                        PaidQuantity = item.PaidQuantity,
                        ReceiptId = item.ReceiptId,
                        TransportClearDropSearchStartRadius = item.TransportClearDropSearchStartRadius,
                        TransportMode = item.TransportMode,
                        TransportRequiresAirportRunway = item.TransportRequiresAirportRunway,
                    });
                }
                foreach (string id in saved.ProducedUnits) value.Units.Add(Actor(id));
                for (int i = 0; i < value.Units.Count; i++)
                {
                    Entity unit = value.Units[i];
                    if (unit == Entity.Null || value.SourceKeys.ContainsKey(unit) || saved.ProducedSourceKeys[i] == null) return false;
                    value.SourceKeys.Add(unit, new FixedString64Bytes(saved.ProducedSourceKeys[i]));
                    var unitPrefab = Prefab(saved.ProducedPrefabKeys[i], false);
                    if (unitPrefab != null) value.Prefabs.Add(unit, unitPrefab);
                }
                value.Slots = new Entity[saved.ProducedSlots.Length];
                for (int i = 0; i < value.Slots.Length; i++) value.Slots[i] = Actor(saved.ProducedSlots[i]);
                if (saved.HasDelivery)
                {
                    var delivery = saved.Delivery;
                    if (delivery == null) return false;
                    if (delivery.Phase > 2 || delivery.DeliveredUnitCount < 0 || delivery.LaneIndex < 0 ||
                        delivery.ArrivalSeconds < 0 || delivery.DepartureSeconds < 0 || delivery.HoldForNextReadySeconds < 0 ||
                        !Enum.IsDefined(typeof(ProductionTransportMode), delivery.Mode)) return false;
                    if (!math.all(math.isfinite((float3)delivery.Position)) ||
                        !math.all(math.isfinite((float4)new Vector4(delivery.Rotation.x, delivery.Rotation.y, delivery.Rotation.z, delivery.Rotation.w))) ||
                        !math.isfinite(delivery.DoorOpenLocalEulerX) ||
                        !math.all(math.isfinite((float3)delivery.EntryPosition)) ||
                        !math.all(math.isfinite((float3)delivery.TouchdownPosition)) ||
                        !math.all(math.isfinite((float3)delivery.HoverPosition)) ||
                        !math.all(math.isfinite((float3)delivery.ExitPosition)) ||
                        !math.all(math.isfinite((float3)delivery.CommittedDropPosition)) ||
                        !math.all(math.isfinite((float4)new Vector4(delivery.HoverRotation.x, delivery.HoverRotation.y, delivery.HoverRotation.z, delivery.HoverRotation.w))) ||
                        !math.all(math.isfinite((float4)new Vector4(delivery.EntryRotation.x, delivery.EntryRotation.y, delivery.EntryRotation.z, delivery.EntryRotation.w))) ||
                        !math.all(math.isfinite((float4)new Vector4(delivery.ExitRotation.x, delivery.ExitRotation.y, delivery.ExitRotation.z, delivery.ExitRotation.w))) ||
                        !math.isfinite(delivery.ArrivalSeconds) ||
                        !math.isfinite(delivery.DepartureSeconds) ||
                        !math.isfinite(delivery.HoldForNextReadySeconds) ||
                        !math.isfinite(delivery.PhaseStartedAt) ||
                        !math.isfinite(delivery.HoverEnteredAt) ||
                        !math.isfinite(delivery.NextDropReadyAt) ||
                        !math.isfinite(delivery.NextClearDropSearchAt)) return false;
                    value.TransportPosition = delivery.Position;
                    value.TransportRotation = delivery.Rotation;
                    value.Delivery = new RuntimeBuildingEntity.ActiveProductionTransport
                    {
                        Prefab = Prefab(delivery.PrefabKey, true),
                        LaneIndex = delivery.LaneIndex,
                        DoorOpenLocalEulerX = delivery.DoorOpenLocalEulerX,
                        EntryPosition = delivery.EntryPosition,
                        TouchdownPosition = delivery.TouchdownPosition,
                        HoverPosition = delivery.HoverPosition,
                        ExitPosition = delivery.ExitPosition,
                        CommittedDropPosition = delivery.CommittedDropPosition,
                        CommittedDropCell = delivery.CommittedDropCell,
                        HoverRotation = delivery.HoverRotation,
                        EntryRotation = delivery.EntryRotation,
                        ExitRotation = delivery.ExitRotation,
                        ArrivalSeconds = delivery.ArrivalSeconds,
                        DepartureSeconds = delivery.DepartureSeconds,
                        HoldForNextReadySeconds = delivery.HoldForNextReadySeconds,
                        PhaseStartedAt = delivery.PhaseStartedAt + now,
                        Phase = delivery.Phase,
                        HoverEnteredAt = delivery.HoverEnteredAt + now,
                        NextDropReadyAt = delivery.NextDropReadyAt + now,
                        NextClearDropSearchAt = delivery.NextClearDropSearchAt + now,
                        ClearDropFailureCount = delivery.ClearDropFailureCount,
                        ClearDropSearchStartRadius = delivery.ClearDropSearchStartRadius,
                        DeliveredUnitCount = delivery.DeliveredUnitCount,
                        Mode = delivery.Mode,
                        HasCommittedDropPosition = delivery.HasCommittedDropPosition,
                        FocusRequested = delivery.FocusRequested,
                    };
                    var drop = delivery.ActiveDrop;
                    if (delivery.HasActiveDrop)
                    {
                        if (drop == null) return false;
                        if (drop.QueueIndex < 0 || drop.QueueIndex >= value.Queue.Count ||
                            !math.isfinite(drop.StartedAt) || !math.isfinite(drop.Duration) || drop.Duration <= 0) return false;
                        if (!math.all(math.isfinite((float3)drop.StartPosition)) ||
                            !math.all(math.isfinite((float3)drop.EndPosition))) return false;
                        value.Delivery.ActiveDrop = new RuntimeBuildingEntity.PendingDropVisual
                        {
                            Production = value.Queue[drop.QueueIndex], Prefab = Prefab(drop.PrefabKey, true),
                            StartedAt = drop.StartedAt + now, Duration = drop.Duration,
                            StartPosition = drop.StartPosition, EndPosition = drop.EndPosition, FinalGoalCell = drop.FinalGoalCell
                        };
                    }
                }
                prepared = value;
                return true;
            }
            catch (ArgumentException) { return false; }
        }

        internal sealed class Prepared
        {
            internal readonly List<RuntimeBuildingEntity.PendingProduction> Queue = new();
            internal readonly List<Entity> Units = new();
            internal Entity[] Slots;
            internal readonly Dictionary<Entity, GameObject> Prefabs = new();
            internal readonly Dictionary<Entity, FixedString64Bytes> SourceKeys = new();
            internal RuntimeBuildingEntity.ActiveProductionTransport Delivery;
            internal Vector3 TransportPosition;
            internal Quaternion TransportRotation;
        }
    }
}
