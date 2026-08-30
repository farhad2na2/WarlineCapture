using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    using ProductionTransportMode = BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode;

    internal sealed class RuntimeBuildingEntity : BuildingCombatUtilitySystemHelper.IRuntimeBuildingVisualState, FactionResourceCompositionSystemHelper.IResourceBuilding
    {
        internal sealed class PendingDropVisual
        {
            public PendingProduction Production;
            public GameObject Prefab;
            public GameObject Visual;
            public LineRenderer Rope;
            public float StartedAt;
            public float Duration;
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public int2 FinalGoalCell;
        }

        internal sealed class ActiveProductionTransport
        {
            public int LaneIndex;
            public GameObject Prefab;
            public GameObject Instance;
            public Transform Transform;
            public Renderer[] VisualRenderers;
            public Transform DoorTransform;
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
            public PendingDropVisual ActiveDrop;
        }

        internal sealed class PendingProduction : BuildingProductionQueueCompositionSystemHelper.IPendingProduction
        {
            public int ProductionIndex { get; set; }
            public GameObject Prefab { get; set; }
            public float StartedAt { get; set; }
            public float ReadyAt { get; set; }
            public int ReservedProductionSlotIndex { get; set; }
            public GameObject TransportPrefab { get; set; }
            public float TransportArrivalSeconds { get; set; }
            public float TransportHoldForNextReadySeconds { get; set; }
            public int TransportMaxConcurrent { get; set; }
            public int RemainingQuantity { get; set; }
            public int TransportClearDropSearchStartRadius { get; set; }
            public ProductionTransportMode TransportMode { get; set; }
            public bool TransportRequiresAirportRunway { get; set; }

            public bool ConsumeUnit()
            {
                RemainingQuantity = Mathf.Max(1, RemainingQuantity) - 1;
                ReservedProductionSlotIndex = -1;
                return RemainingQuantity == 0;
            }
        }

        public int Id { get; set; }
        public BuildingDefinition Definition;
        public GameObject Instance;
        public Vector2Int OriginCell;
        public Entity CombatEntity { get; set; }
        public Entity BlockerEntity { get; set; }
        public Renderer[] FactionVisualRenderers;
        public Color[] FactionVisualBaseColors;
        public Transform DoorZ;
        public float DoorClosedLocalEulerZ;
        public float DoorOpenLocalEulerZ;
        public float DoorOpen01;
        public GameObject DestroyedVisualInstance;
        public Transform[] AliveVisualRoots;
        public BuildingVisualSystem.AnimatedPart[] AnimatedParts;
        public bool ResourceVisualAnimationActive;
        public float NextResourceVisualStateRefreshAt;
        public Vector3[] ProductionSpawnLocalPositions;
        public Entity[] ProducedUnitSlots;
        public List<Entity> ProducedUnits;
        public Dictionary<Entity, GameObject> ProducedUnitPrefabs;
        public Dictionary<Entity, FixedString64Bytes> ProducedUnitSourceKeys;
        public List<PendingProduction> PendingProductions;
        public ActiveProductionTransport ActiveTransport;
        public bool IsDestroyed { get; set; }
        public bool IsCityGenerated;
        public bool HasOwnerFaction { get; set; }
        public byte OwnerFactionId { get; set; }
        public float DestroyedCleanupAt { get; set; }
        public float StoredOilBarrels { get; set; }
        public float StoredFuelBarrels { get; set; }
        public int OilStorageCapacity => Definition != null ? Definition.OilStorageCapacity : 0;
        public int FuelStorageCapacity => Definition != null ? Definition.FuelStorageCapacity : 0;
        public float OilBarrelsPerDay => Definition != null ? Definition.OilBarrelsPerDay : 0f;
        public float FuelBarrelsPerDay => Definition != null ? Definition.FuelBarrelsPerDay : 0f;
        public GameObject InstanceObject => Instance;
        public IReadOnlyList<Transform> AliveVisualRootTransforms => AliveVisualRoots;
    }
}
