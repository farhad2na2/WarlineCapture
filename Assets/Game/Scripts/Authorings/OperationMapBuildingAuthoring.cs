using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public sealed class OperationMapBuildingAuthoring : MonoBehaviour
    {
        [SerializeField] private string operationMapId;
        [SerializeField] private string sourceGlobalObjectId;
        [SerializeField, Min(0)] private int placementIndex;
        [SerializeField] private byte factionId;
        [SerializeField] private Vector2Int originCell;
        [SerializeField] private OperationMapBuildingBlockerPolicy blockerPolicy =
            OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked;
        [SerializeField] private BuildingDefinitionAuthoring definition;
        [SerializeField] private GameObject intactVisualRoot;
        [SerializeField] private GameObject destroyedVisualRoot;

        public string OperationMapId => operationMapId;
        public string SourceGlobalObjectId => sourceGlobalObjectId;
        public int PlacementIndex => placementIndex;
        public byte FactionId => factionId;
        public Vector2Int OriginCell => originCell;
        public OperationMapBuildingBlockerPolicy BlockerPolicy => blockerPolicy;
        public BuildingDefinitionAuthoring Definition => definition;
        public GameObject IntactVisualRoot => intactVisualRoot;
        public GameObject DestroyedVisualRoot => destroyedVisualRoot;

        public bool TryValidate(out string error)
        {
            if (!Game.Configs.OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(sourceGlobalObjectId) ||
                !sourceGlobalObjectId.StartsWith("GlobalObjectId_V1-", System.StringComparison.Ordinal))
            {
                error = "A stable source GlobalObjectId is required.";
                return false;
            }
            if (placementIndex < 0)
            {
                error = "Placement index must be non-negative.";
                return false;
            }
            if (blockerPolicy != OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked)
            {
                error = $"Unsupported operation-map building blocker policy: {(byte)blockerPolicy}.";
                return false;
            }
            if (definition == null)
            {
                error = "Building definition authoring is required.";
                return false;
            }
            if (intactVisualRoot == null || intactVisualRoot.transform.parent != transform)
            {
                error = "An immediate intact visual root is required.";
                return false;
            }
            if (destroyedVisualRoot != null && destroyedVisualRoot.transform.parent != transform)
            {
                error = "The destroyed visual root must be an immediate child when configured.";
                return false;
            }
            Vector2Int footprint = definition.ConfiguredFootprintCells;
            if (footprint.x <= 0 || footprint.y <= 0)
            {
                error = "Building footprint must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        private sealed class OperationMapBuildingBaker : Baker<OperationMapBuildingAuthoring>
        {
            public override void Bake(OperationMapBuildingAuthoring authoring)
            {
                if (!authoring.TryValidate(out _))
                    return;

                BuildingDefinitionAuthoring definition = authoring.definition;
                DependsOn(definition);
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                Vector2Int footprint = definition.ConfiguredFootprintCells;
                int2 footprintCells = new(math.max(1, footprint.x), math.max(1, footprint.y));
                int2 origin = new(authoring.originCell.x, authoring.originCell.y);
                int runtimeBuildingId = authoring.placementIndex + 1;
                int maxHealth = math.max(1, definition.ConfiguredMaxHealth);

                AddComponent(entity, new OperationMapBuildingIdentity
                {
                    OperationMapId = new FixedString128Bytes(authoring.operationMapId),
                    SourceGlobalObjectId = new FixedString128Bytes(authoring.sourceGlobalObjectId),
                    PlacementIndex = authoring.placementIndex
                });
                AddComponent(entity, new OperationMapBuildingComponent
                {
                    OperationMapId = new FixedString128Bytes(authoring.operationMapId),
                    SourceGlobalObjectId = new FixedString128Bytes(authoring.sourceGlobalObjectId),
                    PlacementIndex = authoring.placementIndex,
                    BlockerPolicy = authoring.blockerPolicy
                });
                AddComponent<OperationMapBuildingDestroyedComponent>(entity);
                SetComponentEnabled<OperationMapBuildingDestroyedComponent>(entity, false);
                AddComponent<RuntimeBuildingCombatTag>(entity);
                AddComponent(entity, new RuntimeBuildingCombatInfo
                {
                    RuntimeBuildingId = runtimeBuildingId,
                    OwnerFactionId = authoring.factionId,
                    OriginCell = origin,
                    FootprintCells = footprintCells,
                    IsWall = definition.ConfiguredIsWall ? (byte)1 : (byte)0,
                    IsGate = 0
                });
                AddComponent(entity, new Faction { Id = authoring.factionId });
                AddComponent(entity, new UnitHealth { Current = maxHealth, Max = maxHealth });
                AddComponent(entity, new UnitGrid
                {
                    Cell = origin + footprintCells / 2
                });
                AddComponent(entity, new UnitFootprint { Size = footprintCells });
                AddComponent<UnitGridInitialized>(entity);
                AddComponent(entity, new GridBlockerSize { Size = footprintCells });
                AddComponent<StaticGridBlocker>(entity);
                AddComponent(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
                AddComponent(entity, new UnitSourcePrefabKey
                {
                    Value = new FixedString64Bytes(definition.gameObject.name)
                });
                AddComponent(entity, new UnitDisplayInfo
                {
                    Name = new FixedString64Bytes(
                        string.IsNullOrWhiteSpace(definition.ConfiguredDisplayName)
                            ? definition.gameObject.name
                            : definition.ConfiguredDisplayName),
                    Description = new FixedString128Bytes(definition.ConfiguredDescription ?? string.Empty)
                });
                AddComponent(entity, new UnitPrevWorldPos { Value = authoring.transform.position });
                AddComponent(entity, new UnitMoveVisualComponent());
                AddComponent(entity, new UnitAnimationSettings
                {
                    AttackAnimationSeconds = 0.1f,
                    DeathAnimationSeconds = 0.01f
                });

                Entity intactVisual = GetEntity(authoring.intactVisualRoot, TransformUsageFlags.Renderable);
                Entity destroyedVisual = authoring.destroyedVisualRoot != null
                    ? GetEntity(authoring.destroyedVisualRoot, TransformUsageFlags.Renderable)
                    : Entity.Null;
                AddComponent(entity, new OperationMapBuildingPresentation
                {
                    IntactVisualRoot = intactVisual,
                    DestroyedVisualRoot = destroyedVisual,
                    IntactVisibleScale = math.max(0.0001f, authoring.intactVisualRoot.transform.localScale.x),
                    DestroyedVisibleScale = authoring.destroyedVisualRoot != null
                        ? math.max(0.0001f, authoring.destroyedVisualRoot.transform.localScale.x)
                        : 1f,
                    State = byte.MaxValue
                });

                AddResourceStorage(entity, runtimeBuildingId, authoring.factionId, definition);
                AddDefense(entity, definition);
                AddProductionPrefabs(entity, definition);
            }

            private void AddResourceStorage(
                Entity entity,
                int runtimeBuildingId,
                byte factionId,
                BuildingDefinitionAuthoring definition)
            {
                if (definition.ConfiguredOilStorageCapacity <= 0 &&
                    definition.ConfiguredFuelStorageCapacity <= 0 &&
                    definition.ConfiguredOilBarrelsPerDay <= 0f &&
                    definition.ConfiguredFuelBarrelsPerDay <= 0f)
                {
                    return;
                }

                AddComponent(entity, new BuildingResourceStorageComponent
                {
                    RuntimeBuildingId = runtimeBuildingId,
                    OwnerFactionId = factionId,
                    OilStorageCapacity = definition.ConfiguredOilStorageCapacity,
                    FuelStorageCapacity = definition.ConfiguredFuelStorageCapacity,
                    OilBarrelsPerDay = definition.ConfiguredOilBarrelsPerDay,
                    FuelBarrelsPerDay = definition.ConfiguredFuelBarrelsPerDay,
                    Version = 1
                });
            }

            private void AddDefense(Entity entity, BuildingDefinitionAuthoring definition)
            {
                if (!definition.ConfiguredCanAttack || definition.ConfiguredAttackRange <= 0f ||
                    definition.ConfiguredAttackDamage <= 0)
                {
                    return;
                }

                Color color = definition.ConfiguredAttackTraceColor;
                AddComponent(entity, new BuildingDefenseWeapon
                {
                    Range = definition.ConfiguredAttackRange,
                    CooldownSeconds = definition.ConfiguredAttackCooldownSeconds,
                    Damage = definition.ConfiguredAttackDamage,
                    MaxConcurrentAttacks = (byte)math.clamp(definition.ConfiguredMaxConcurrentAttacks, 1, 4),
                    TraceColor = new float4(color.r, color.g, color.b, color.a),
                    TraceWidth = definition.ConfiguredAttackTraceWidth,
                    TraceScrollSpeed = definition.ConfiguredAttackTraceScrollSpeed,
                    TraceDashDensity = definition.ConfiguredAttackTraceDashDensity,
                    TraceVisibleSeconds = definition.ConfiguredAttackTraceVisibleSeconds,
                    TracerEveryNthShot = definition.ConfiguredAttackTracerEveryNthShot
                });
                AddComponent(entity, new UnitAttack
                {
                    Range = definition.ConfiguredAttackRange,
                    CooldownSeconds = definition.ConfiguredAttackCooldownSeconds,
                    Damage = definition.ConfiguredAttackDamage,
                    TraceColor = new float4(color.r, color.g, color.b, color.a),
                    TraceWidth = definition.ConfiguredAttackTraceWidth,
                    TraceScrollSpeed = definition.ConfiguredAttackTraceScrollSpeed,
                    TraceDashDensity = definition.ConfiguredAttackTraceDashDensity,
                    TraceVisibleSeconds = definition.ConfiguredAttackTraceVisibleSeconds,
                    TracerEveryNthShot = definition.ConfiguredAttackTracerEveryNthShot
                });
                AddComponent(entity, new UnitAttackTraceComponent());
                DynamicBuffer<BuildingDefenseAttackSlot> slots = AddBuffer<BuildingDefenseAttackSlot>(entity);
                int slotCount = math.clamp(definition.ConfiguredMaxConcurrentAttacks, 1, 4);
                for (int i = 0; i < slotCount; i++)
                    slots.Add(new BuildingDefenseAttackSlot { Target = Entity.Null });
            }

            private void AddProductionPrefabs(Entity entity, BuildingDefinitionAuthoring definition)
            {
                DynamicBuffer<OperationMapBuildingProductionPrefab> buffer =
                    AddBuffer<OperationMapBuildingProductionPrefab>(entity);
                for (int index = 0; index < definition.ConfiguredProductionCount; index++)
                {
                    BuildingDefinitionAuthoring.ProductionDefinition production =
                        definition.GetProductionOrDefault(index);
                    if (production?.spawnUnitPrefab == null)
                        continue;
                    buffer.Add(new OperationMapBuildingProductionPrefab
                    {
                        ProductionIndex = index,
                        Prefab = GetEntity(production.spawnUnitPrefab, TransformUsageFlags.Dynamic),
                        SourceKey = new FixedString64Bytes(production.spawnUnitPrefab.name)
                    });
                }
            }
        }
    }
}
