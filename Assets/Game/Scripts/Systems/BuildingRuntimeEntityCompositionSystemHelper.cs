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

        public Entity CreateBuildingCombatEntity(Context context, Vector2Int originCell, BuildingDefinition definition, byte ownerFactionId, Quaternion worldRotation)
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
                OwnerFactionId = ownerFactionId,
                OriginCell = new int2(originCell.x, originCell.y),
                FootprintCells = new int2(footprintCells.x, footprintCells.y),
                IsWall = definition.IsWall ? (byte)1 : (byte)0,
                IsGate = BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition) ? (byte)1 : (byte)0
            });
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
            em.AddComponentData(entity, new UnitPrevWorldPos { Value = center });
            em.AddComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
            em.AddComponentData(entity, new UnitAnimationSettings
            {
                AttackAnimationSeconds = 0.1f,
                DeathAnimationSeconds = 0.01f
            });
            return entity;
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
