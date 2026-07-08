using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeEntityCompositionSystemHelper
    {
        public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
        public delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData);
        public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);

        public readonly struct Context
        {
            public readonly TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly TryGetGridDataDelegate TryGetGridData;
            public readonly GetFootprintCenterDelegate GetFootprintCenter;
            public readonly BuildingCombatUtilitySystemHelper CombatSystem;
            public readonly BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> CombatContext;
            public readonly System.Func<float> GetTime;
            public readonly float DestroyedBuildingLifetimeSeconds;

            public Context(
                TryGetEntityManagerDelegate tryGetEntityManager,
                TryGetGridDataDelegate tryGetGridData,
                GetFootprintCenterDelegate getFootprintCenter,
                BuildingCombatUtilitySystemHelper combatSystem,
                BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity> combatContext,
                System.Func<float> getTime,
                float destroyedBuildingLifetimeSeconds)
            {
                TryGetEntityManager = tryGetEntityManager;
                TryGetGridData = tryGetGridData;
                GetFootprintCenter = getFootprintCenter;
                CombatSystem = combatSystem;
                CombatContext = combatContext;
                GetTime = getTime;
                DestroyedBuildingLifetimeSeconds = destroyedBuildingLifetimeSeconds;
            }
        }

        public Entity CreateBlockerEntity(Context context, BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells)
        {
            if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
                return Entity.Null;

            Entity entity = em.CreateEntity();
            em.AddComponentData(entity, new UnitGrid { Cell = new int2(originCell.x, originCell.y) });
            em.AddComponentData(entity, new GridBlockerSize { Size = new int2(footprintCells.x, footprintCells.y) });
            em.AddComponent<StaticGridBlocker>(entity);
            return entity;
        }

        public bool DeleteBuildingById(Context context, int buildingId)
        {
            return context.CombatSystem != null &&
                context.CombatSystem.DeleteBuilding(
                    context.CombatContext,
                    buildingId,
                    destroyVisual: true,
                    context.GetTime?.Invoke() ?? 0f,
                    context.DestroyedBuildingLifetimeSeconds);
        }

        public void HandleRuntimeBuildingEntityDestroyed(
            Context context,
            int buildingId,
            Entity blockerEntity,
            GameObject buildingObject)
        {
            context.CombatSystem?.HandleRuntimeBuildingEntityDestroyed(
                context.CombatContext,
                buildingId,
                blockerEntity,
                buildingObject);
        }

        public bool ShouldRuntimeBuildingBlockPathing(BuildingDefinition definition)
        {
            return !BuildingDefinitionPrefabSystemHelper.RuntimeDefinitionMatchesId(
                definition,
                BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Helipad"));
        }

        public Entity CreateBuildingCombatEntity(Context context, int runtimeBuildingId, Vector2Int originCell, BuildingDefinition definition, byte ownerFactionId, Quaternion worldRotation)
        {
            if (definition == null)
                return Entity.Null;
            if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
                return Entity.Null;
            if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
                return Entity.Null;
            if (context.GetFootprintCenter == null)
                return Entity.Null;

            Vector2Int footprintCells = new(Mathf.Max(1, definition.FootprintCells.x), Mathf.Max(1, definition.FootprintCells.y));
            float3 center = (float3)context.GetFootprintCenter(originCell, footprintCells, grid);
            int maxHealth = Mathf.Max(1, definition.MaxHealth);
            Entity entity = em.CreateEntity();
            em.AddComponentData(entity, new LocalTransform
            {
                Position = center,
                Rotation = new quaternion(worldRotation.x, worldRotation.y, worldRotation.z, worldRotation.w),
                Scale = 1f
            });
            em.AddComponentData(entity, new LocalToWorld());
            em.AddComponentData(entity, new UnitGrid
            {
                Cell = new int2(originCell.x + footprintCells.x / 2, originCell.y + footprintCells.y / 2)
            });
            em.AddComponentData(entity, new UnitFootprint
            {
                Size = new int2(footprintCells.x, footprintCells.y)
            });
            em.AddComponent<RuntimeBuildingCombatTag>(entity);
            em.AddComponentData(entity, new RuntimeBuildingCombatInfo
            {
                RuntimeBuildingId = runtimeBuildingId,
                OwnerFactionId = ownerFactionId,
                OriginCell = new int2(originCell.x, originCell.y),
                FootprintCells = new int2(footprintCells.x, footprintCells.y),
                IsWall = definition.IsWall ? (byte)1 : (byte)0,
                IsGate = BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition) ? (byte)1 : (byte)0
            });
            AddResourceStorageComponent(em, entity, runtimeBuildingId, ownerFactionId, definition);
            em.AddComponentData(entity, new UnitGridInitialized());
            em.AddComponentData(entity, new Faction { Id = ownerFactionId });
            em.AddComponentData(entity, new UnitHealth { Current = maxHealth, Max = maxHealth });
            em.AddComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
            em.AddComponentData(entity, new UnitSourcePrefabKey
            {
                Value = new FixedString64Bytes(definition.Prefab != null ? definition.Prefab.name : definition.DisplayName)
            });
            em.AddComponentData(entity, new UnitDisplayInfo
            {
                Name = new FixedString64Bytes(string.IsNullOrWhiteSpace(definition.DisplayName) ? "Building" : definition.DisplayName),
                Description = new FixedString128Bytes(definition.Description ?? string.Empty)
            });
            if (definition.ThreatDetectionKind != ThreatDetectionKind.None && definition.ThreatDetectionRadiusCells > 0)
            {
                em.AddComponentData(entity, new ThreatDetector
                {
                    Kind = (byte)definition.ThreatDetectionKind,
                    RadiusCells = Mathf.Max(0, definition.ThreatDetectionRadiusCells)
                });
                AddAirDefenseSupportProvider(em, entity, definition.ThreatDetectionKind, definition.ThreatDetectionRadiusCells);
            }
            AddBuildingDefenseWeapon(em, entity, definition);
            em.AddComponentData(entity, new UnitPrevWorldPos { Value = center });
            em.AddComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
            em.AddComponentData(entity, new UnitAnimationSettings
            {
                AttackAnimationSeconds = 0.1f,
                DeathAnimationSeconds = 0.01f
            });
            return entity;
        }

        private static void AddBuildingDefenseWeapon(EntityManager em, Entity entity, BuildingDefinition definition)
        {
            if (definition == null ||
                !definition.CanAttack ||
                definition.AttackRange <= 0f ||
                definition.AttackDamage <= 0)
            {
                return;
            }

            Color traceColor = definition.AttackTraceColor;
            var attack = new UnitAttack
            {
                Range = Mathf.Max(0f, definition.AttackRange),
                CooldownSeconds = Mathf.Max(0.01f, definition.AttackCooldownSeconds),
                Damage = Mathf.Max(0, definition.AttackDamage),
                TraceColor = new float4(traceColor.r, traceColor.g, traceColor.b, traceColor.a),
                TraceWidth = Mathf.Max(0.01f, definition.AttackTraceWidth),
                TraceScrollSpeed = Mathf.Max(0.1f, definition.AttackTraceScrollSpeed),
                TraceDashDensity = Mathf.Max(1f, definition.AttackTraceDashDensity),
                TraceVisibleSeconds = Mathf.Max(0.01f, definition.AttackTraceVisibleSeconds),
                TracerEveryNthShot = Mathf.Max(1, definition.AttackTracerEveryNthShot)
            };

            em.AddComponentData(entity, new BuildingDefenseWeapon
            {
                Range = attack.Range,
                CooldownSeconds = attack.CooldownSeconds,
                Damage = attack.Damage,
                MaxConcurrentAttacks = (byte)Mathf.Clamp(definition.MaxConcurrentAttacks, 1, 4),
                TraceColor = attack.TraceColor,
                TraceWidth = attack.TraceWidth,
                TraceScrollSpeed = attack.TraceScrollSpeed,
                TraceDashDensity = attack.TraceDashDensity,
                TraceVisibleSeconds = attack.TraceVisibleSeconds,
                TracerEveryNthShot = attack.TracerEveryNthShot
            });
            em.AddComponentData(entity, attack);
            em.AddComponentData(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
            int slotCount = Mathf.Clamp(definition.MaxConcurrentAttacks, 1, 4);
            if (slotCount > 1)
            {
                em.AddComponentData(entity, new UnitAttackTraceOriginPattern
                {
                    OriginCount = (byte)slotCount,
                    LateralOffset = 0.8f,
                    TargetLateralOffset = 0.2f
                });
            }

            DynamicBuffer<BuildingDefenseAttackSlot> slots = em.AddBuffer<BuildingDefenseAttackSlot>(entity);
            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new BuildingDefenseAttackSlot
                {
                    Target = Entity.Null,
                    CooldownRemaining = 0f,
                    ShotCounter = 0
                });
            }

            if (definition.AttackImpactPrefab != null)
            {
                em.AddComponentData(entity, new UnitAttackImpactVfxReference
                {
                    Prefab = definition.AttackImpactPrefab
                });
            }

            if (definition.MuzzleFlashPrefab != null)
            {
                em.AddComponentData(entity, new UnitMuzzleFlashVfxReference
                {
                    Prefab = definition.MuzzleFlashPrefab,
                    HeightOffset = Mathf.Max(0f, definition.MuzzleFlashHeightOffset),
                    ForwardOffset = Mathf.Max(0f, definition.MuzzleFlashForwardOffset)
                });
            }
        }

        private static void AddResourceStorageComponent(
            EntityManager em,
            Entity entity,
            int runtimeBuildingId,
            byte ownerFactionId,
            BuildingDefinition definition)
        {
            int oilCapacity = Mathf.Max(0, definition.OilStorageCapacity);
            int fuelCapacity = Mathf.Max(0, definition.FuelStorageCapacity);
            float oilRate = Mathf.Max(0f, definition.OilBarrelsPerDay);
            float fuelRate = Mathf.Max(0f, definition.FuelBarrelsPerDay);
            if (oilCapacity <= 0 && fuelCapacity <= 0 && oilRate <= 0f && fuelRate <= 0f)
                return;

            em.AddComponentData(entity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = runtimeBuildingId,
                OwnerFactionId = ownerFactionId,
                OilStorageCapacity = oilCapacity,
                FuelStorageCapacity = fuelCapacity,
                OilBarrelsPerDay = oilRate,
                FuelBarrelsPerDay = fuelRate,
                StoredOilBarrels = 0f,
                StoredFuelBarrels = 0f
            });
        }

        private static void AddAirDefenseSupportProvider(
            EntityManager em,
            Entity entity,
            ThreatDetectionKind kind,
            int radiusCells)
        {
            if (kind == ThreatDetectionKind.None || radiusCells <= 0)
                return;

            byte supportKind = kind == ThreatDetectionKind.Air
                ? (byte)AirDefenseSupportProviderKind.Satellite
                : (byte)AirDefenseSupportProviderKind.Radar;
            em.AddComponentData(entity, new AirDefenseSupportProviderComponent
            {
                Kind = supportKind,
                Level = 1,
                SupportRadius = math.max(0, radiusCells),
                RangeBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                    ? AirDefenseSupportTuning.SatelliteRangeBonus
                    : AirDefenseSupportTuning.RadarRangeBonus,
                LockTimeMultiplier = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                    ? AirDefenseSupportTuning.SatelliteLockTimeMultiplier
                    : AirDefenseSupportTuning.RadarLockTimeMultiplier,
                TrackingBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                    ? AirDefenseSupportTuning.SatelliteTrackingBonus
                    : AirDefenseSupportTuning.RadarTrackingBonus,
                TurnRateBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                    ? AirDefenseSupportTuning.SatelliteTurnRateBonus
                    : AirDefenseSupportTuning.RadarTurnRateBonus
            });
        }
    }
}
