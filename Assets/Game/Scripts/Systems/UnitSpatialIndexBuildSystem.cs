using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    [UpdateBefore(typeof(BuildingDefenseAttackSystem))]
    [UpdateBefore(typeof(AITargetingSystem))]
    [UpdateBefore(typeof(ThreatDetectionWarningSystem))]
    [UpdateBefore(typeof(VisibleUnitSelectionCandidateSystem))]
    public partial struct UnitSpatialIndexBuildSystem : ISystem
    {
        public const int DefaultBucketSizeCells = 32;
        public const int MaxEntries = 2048;
        public const int MaxBuckets = 4096;

        private Entity _indexEntity;
        private ComponentLookup<UnitHealth> _healthLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<LocalToWorld> _localToWorldLookup;
        private ComponentLookup<UnitAirMovement> _airMovementLookup;
        private ComponentLookup<DebugFireTargetTag> _debugTargetLookup;
        private ComponentLookup<RuntimeBuildingCombatTag> _runtimeBuildingLookup;
        private ComponentLookup<StaticGridBlocker> _staticGridBlockerLookup;
        private ComponentLookup<UnitMovementBehavior> _movementBehaviorLookup;
        private ComponentLookup<UnitSourcePrefabKey> _sourcePrefabKeyLookup;
        private ComponentLookup<UnitFootprint> _footprintLookup;
        private ComponentLookup<UnitAttack> _attackLookup;
        private ComponentLookup<UnitCombat> _combatLookup;
        private ComponentLookup<UnitResourceHauler> _resourceHaulerLookup;
        private ComponentLookup<FuelLogisticsOilSourceTag> _oilSourceLookup;
        private ComponentLookup<FuelLogisticsRefineryInputTag> _refineryInputLookup;
        private ComponentLookup<FuelLogisticsRefineryOutputTag> _refineryOutputLookup;
        private ComponentLookup<FuelLogisticsFuelStorageTag> _fuelStorageLookup;
        private ComponentLookup<UnitMove> _moveLookup;
        private ComponentLookup<UnitSpawnTransitTag> _spawnTransitLookup;
        private ComponentLookup<UnitSelectionHitbox> _selectionHitboxLookup;

        public void OnCreate(ref SystemState state)
        {
            _indexEntity = state.EntityManager.CreateEntity(typeof(UnitSpatialIndexState));
            DynamicBuffer<UnitSpatialIndexEntry> entries =
                state.EntityManager.AddBuffer<UnitSpatialIndexEntry>(_indexEntity);
            DynamicBuffer<UnitSpatialIndexBucketRange> ranges =
                state.EntityManager.AddBuffer<UnitSpatialIndexBucketRange>(_indexEntity);
            DynamicBuffer<UnitSpatialIndexBucketEntry> bucketEntries =
                state.EntityManager.AddBuffer<UnitSpatialIndexBucketEntry>(_indexEntity);
            entries.Capacity = MaxEntries;
            ranges.Capacity = MaxBuckets;
            bucketEntries.Capacity = MaxEntries;
            ranges.ResizeUninitialized(MaxBuckets);
            bucketEntries.ResizeUninitialized(MaxEntries);
            UnitSpatialIndexBuilder.ClearRanges(ranges, MaxBuckets);

            _healthLookup = state.GetComponentLookup<UnitHealth>(true);
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _localToWorldLookup = state.GetComponentLookup<LocalToWorld>(true);
            _airMovementLookup = state.GetComponentLookup<UnitAirMovement>(true);
            _debugTargetLookup = state.GetComponentLookup<DebugFireTargetTag>(true);
            _runtimeBuildingLookup = state.GetComponentLookup<RuntimeBuildingCombatTag>(true);
            _staticGridBlockerLookup = state.GetComponentLookup<StaticGridBlocker>(true);
            _movementBehaviorLookup = state.GetComponentLookup<UnitMovementBehavior>(true);
            _sourcePrefabKeyLookup = state.GetComponentLookup<UnitSourcePrefabKey>(true);
            _footprintLookup = state.GetComponentLookup<UnitFootprint>(true);
            _attackLookup = state.GetComponentLookup<UnitAttack>(true);
            _combatLookup = state.GetComponentLookup<UnitCombat>(true);
            _resourceHaulerLookup = state.GetComponentLookup<UnitResourceHauler>(true);
            _oilSourceLookup = state.GetComponentLookup<FuelLogisticsOilSourceTag>(true);
            _refineryInputLookup = state.GetComponentLookup<FuelLogisticsRefineryInputTag>(true);
            _refineryOutputLookup = state.GetComponentLookup<FuelLogisticsRefineryOutputTag>(true);
            _fuelStorageLookup = state.GetComponentLookup<FuelLogisticsFuelStorageTag>(true);
            _moveLookup = state.GetComponentLookup<UnitMove>(true);
            _spawnTransitLookup = state.GetComponentLookup<UnitSpawnTransitTag>(true);
            _selectionHitboxLookup = state.GetComponentLookup<UnitSelectionHitbox>(true);
            state.RequireForUpdate<GridConfig>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_indexEntity != Entity.Null && state.EntityManager.Exists(_indexEntity))
                state.EntityManager.DestroyEntity(_indexEntity);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            UpdateLookups(ref state);
            GridConfig grid = SystemAPI.GetSingleton<GridConfig>();
            DynamicBuffer<UnitSpatialIndexEntry> entries =
                state.EntityManager.GetBuffer<UnitSpatialIndexEntry>(_indexEntity);
            DynamicBuffer<UnitSpatialIndexBucketRange> ranges =
                state.EntityManager.GetBuffer<UnitSpatialIndexBucketRange>(_indexEntity);
            DynamicBuffer<UnitSpatialIndexBucketEntry> bucketEntries =
                state.EntityManager.GetBuffer<UnitSpatialIndexBucketEntry>(_indexEntity);
            UnitSpatialIndexState indexState = state.EntityManager.GetComponentData<UnitSpatialIndexState>(_indexEntity);
            entries.Clear();
            int overflowCount = 0;
            int sourceOrder = 0;

            foreach (var (faction, unitGrid, entity) in
                     SystemAPI.Query<RefRO<Faction>, RefRO<UnitGrid>>().WithEntityAccess())
            {
                if (entries.Length >= entries.Capacity)
                {
                    overflowCount++;
                    sourceOrder++;
                    continue;
                }

                entries.Add(CreateEntry(entity, sourceOrder++, faction.ValueRO, unitGrid.ValueRO, grid));
            }

            UnitSpatialIndexBuilder.BuildBuckets(
                entries,
                ranges,
                bucketEntries,
                grid.Width,
                grid.Height,
                DefaultBucketSizeCells,
                overflowCount,
                indexState.Version + 1u,
                out indexState);
            state.EntityManager.SetComponentData(_indexEntity, indexState);
        }

        private void UpdateLookups(ref SystemState state)
        {
            _healthLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _localToWorldLookup.Update(ref state);
            _airMovementLookup.Update(ref state);
            _debugTargetLookup.Update(ref state);
            _runtimeBuildingLookup.Update(ref state);
            _staticGridBlockerLookup.Update(ref state);
            _movementBehaviorLookup.Update(ref state);
            _sourcePrefabKeyLookup.Update(ref state);
            _footprintLookup.Update(ref state);
            _attackLookup.Update(ref state);
            _combatLookup.Update(ref state);
            _resourceHaulerLookup.Update(ref state);
            _oilSourceLookup.Update(ref state);
            _refineryInputLookup.Update(ref state);
            _refineryOutputLookup.Update(ref state);
            _fuelStorageLookup.Update(ref state);
            _moveLookup.Update(ref state);
            _spawnTransitLookup.Update(ref state);
            _selectionHitboxLookup.Update(ref state);
        }

        private UnitSpatialIndexEntry CreateEntry(
            Entity entity,
            int sourceOrder,
            in Faction faction,
            in UnitGrid unitGrid,
            in GridConfig grid)
        {
            UnitSpatialIndexFlags flags = UnitSpatialIndexFlags.None;
            UnitHealth health = default;
            if (_healthLookup.HasComponent(entity))
            {
                health = _healthLookup[entity];
                flags |= UnitSpatialIndexFlags.HasHealth;
            }

            float3 fallbackPosition = grid.Origin + new float3(
                (unitGrid.Cell.x + 0.5f) * grid.CellSize,
                0f,
                (unitGrid.Cell.y + 0.5f) * grid.CellSize);
            float3 position = fallbackPosition;
            float3 selectionPosition = fallbackPosition;
            if (_localTransformLookup.HasComponent(entity))
            {
                position = _localTransformLookup[entity].Position;
                flags |= UnitSpatialIndexFlags.HasLocalTransform;
            }
            if (_localToWorldLookup.HasComponent(entity))
            {
                selectionPosition = _localToWorldLookup[entity].Position;
                flags |= UnitSpatialIndexFlags.HasLocalToWorld;
            }
            else
            {
                selectionPosition = position;
            }

            flags |= FlagIf(_airMovementLookup.HasComponent(entity), UnitSpatialIndexFlags.Air);
            flags |= FlagIf(_debugTargetLookup.HasComponent(entity), UnitSpatialIndexFlags.DebugTarget);
            flags |= FlagIf(_runtimeBuildingLookup.HasComponent(entity), UnitSpatialIndexFlags.RuntimeBuilding);
            flags |= FlagIf(_staticGridBlockerLookup.HasComponent(entity), UnitSpatialIndexFlags.StaticGridBlocker);
            flags |= FlagIf(_attackLookup.HasComponent(entity), UnitSpatialIndexFlags.CanAttack);
            flags |= FlagIf(_combatLookup.HasComponent(entity), UnitSpatialIndexFlags.HasCombat);
            flags |= FlagIf(_resourceHaulerLookup.HasComponent(entity), UnitSpatialIndexFlags.ResourceHauler);
            flags |= FlagIf(_oilSourceLookup.HasComponent(entity), UnitSpatialIndexFlags.FuelOilSource);
            flags |= FlagIf(_refineryInputLookup.HasComponent(entity), UnitSpatialIndexFlags.FuelRefineryInput);
            flags |= FlagIf(_refineryOutputLookup.HasComponent(entity), UnitSpatialIndexFlags.FuelRefineryOutput);
            flags |= FlagIf(_fuelStorageLookup.HasComponent(entity), UnitSpatialIndexFlags.FuelStorage);
            flags |= FlagIf(_spawnTransitLookup.HasComponent(entity), UnitSpatialIndexFlags.SpawnTransit);
            flags |= FlagIf(_selectionHitboxLookup.HasComponent(entity), UnitSpatialIndexFlags.HasSelectionHitbox);

            bool hasMovementBehavior = _movementBehaviorLookup.HasComponent(entity);
            bool groundVehicle = hasMovementBehavior && _movementBehaviorLookup[entity].UsesVehicleMotion != 0;
            flags |= FlagIf(groundVehicle, UnitSpatialIndexFlags.GroundVehicle);
            bool selectable = _moveLookup.HasComponent(entity) && !_staticGridBlockerLookup.HasComponent(entity);
            flags |= FlagIf(selectable, UnitSpatialIndexFlags.Selectable);
            flags |= FlagIf(IsSelectionVehicle(entity, groundVehicle), UnitSpatialIndexFlags.SelectionVehicle);

            return new UnitSpatialIndexEntry
            {
                Entity = entity,
                SourceOrder = sourceOrder,
                Cell = unitGrid.Cell,
                Position = position,
                SelectionPosition = selectionPosition,
                HealthCurrent = health.Current,
                HealthMax = health.Max,
                FactionId = faction.Id,
                Flags = flags
            };
        }

        private bool IsSelectionVehicle(Entity entity, bool fallback)
        {
            if (_sourcePrefabKeyLookup.HasComponent(entity))
            {
                FixedString64Bytes key = _sourcePrefabKeyLookup[entity].Value;
                if (HasUnitPrefixIgnoreCase(key, (byte)'V', (byte)'e', (byte)'h'))
                    return true;
                if (HasUnitPrefixIgnoreCase(key, (byte)'C', (byte)'h', (byte)'r'))
                    return false;
            }

            if (_footprintLookup.HasComponent(entity) && _movementBehaviorLookup.HasComponent(entity))
            {
                return UnitVehicleMovementUtility.IsVehicle(
                    _footprintLookup[entity],
                    _movementBehaviorLookup[entity]);
            }

            return fallback;
        }

        private static UnitSpatialIndexFlags FlagIf(bool condition, UnitSpatialIndexFlags flag)
        {
            return condition ? flag : UnitSpatialIndexFlags.None;
        }

        private static bool HasUnitPrefixIgnoreCase(
            FixedString64Bytes value,
            byte type0,
            byte type1,
            byte type2)
        {
            return value.Length >= 9 &&
                   ToLowerAscii(value[0]) == (byte)'u' &&
                   ToLowerAscii(value[1]) == (byte)'n' &&
                   ToLowerAscii(value[2]) == (byte)'i' &&
                   ToLowerAscii(value[3]) == (byte)'t' &&
                   value[4] == (byte)'_' &&
                   ToLowerAscii(value[5]) == ToLowerAscii(type0) &&
                   ToLowerAscii(value[6]) == ToLowerAscii(type1) &&
                   ToLowerAscii(value[7]) == ToLowerAscii(type2) &&
                   value[8] == (byte)'_';
        }

        private static byte ToLowerAscii(byte value)
        {
            return value >= (byte)'A' && value <= (byte)'Z'
                ? (byte)(value + 32)
                : value;
        }
    }

    internal static class UnitSpatialIndexBuilder
    {
        public static void BuildBuckets(
            DynamicBuffer<UnitSpatialIndexEntry> entries,
            DynamicBuffer<UnitSpatialIndexBucketRange> ranges,
            DynamicBuffer<UnitSpatialIndexBucketEntry> bucketEntries,
            int gridWidth,
            int gridHeight,
            int bucketSizeCells,
            int overflowCount,
            uint version,
            out UnitSpatialIndexState state)
        {
            int safeWidth = math.max(1, gridWidth);
            int safeHeight = math.max(1, gridHeight);
            int safeBucketSize = math.max(1, bucketSizeCells);
            int bucketCountX = (safeWidth + safeBucketSize - 1) / safeBucketSize;
            int bucketCountY = (safeHeight + safeBucketSize - 1) / safeBucketSize;
            int requiredBucketCount = bucketCountX * bucketCountY;
            int acceptedEntryCount = math.min(entries.Length, bucketEntries.Length);
            bool capacityValid = requiredBucketCount <= ranges.Length;

            int clearCount = math.min(requiredBucketCount, ranges.Length);
            ClearRanges(ranges, clearCount);
            if (!capacityValid)
            {
                state = new UnitSpatialIndexState
                {
                    Version = version,
                    EntryCount = entries.Length,
                    OverflowCount = overflowCount + entries.Length,
                    GridWidth = safeWidth,
                    GridHeight = safeHeight,
                    BucketSizeCells = safeBucketSize,
                    BucketCountX = bucketCountX,
                    BucketCountY = bucketCountY,
                    BucketCount = requiredBucketCount,
                    Ready = 0
                };
                return;
            }

            for (int i = 0; i < acceptedEntryCount; i++)
            {
                int bucketIndex = BucketIndex(entries[i].Cell, safeWidth, safeHeight, safeBucketSize, bucketCountX);
                UnitSpatialIndexBucketRange range = ranges[bucketIndex];
                range.Count++;
                ranges[bucketIndex] = range;
            }

            int start = 0;
            for (int i = 0; i < requiredBucketCount; i++)
            {
                UnitSpatialIndexBucketRange range = ranges[i];
                range.Start = start;
                range.WriteCursor = 0;
                start += range.Count;
                ranges[i] = range;
            }

            for (int i = 0; i < acceptedEntryCount; i++)
            {
                int bucketIndex = BucketIndex(entries[i].Cell, safeWidth, safeHeight, safeBucketSize, bucketCountX);
                UnitSpatialIndexBucketRange range = ranges[bucketIndex];
                int destination = range.Start + range.WriteCursor;
                bucketEntries[destination] = new UnitSpatialIndexBucketEntry { EntryIndex = i };
                range.WriteCursor++;
                ranges[bucketIndex] = range;
            }

            state = new UnitSpatialIndexState
            {
                Version = version,
                EntryCount = entries.Length,
                BucketReferenceCount = acceptedEntryCount,
                OverflowCount = overflowCount + math.max(0, entries.Length - acceptedEntryCount),
                GridWidth = safeWidth,
                GridHeight = safeHeight,
                BucketSizeCells = safeBucketSize,
                BucketCountX = bucketCountX,
                BucketCountY = bucketCountY,
                BucketCount = requiredBucketCount,
                Ready = 1
            };
        }

        public static void ClearRanges(DynamicBuffer<UnitSpatialIndexBucketRange> ranges, int count)
        {
            int clearCount = math.min(math.max(0, count), ranges.Length);
            for (int i = 0; i < clearCount; i++)
                ranges[i] = default;
        }

        private static int BucketIndex(
            int2 cell,
            int gridWidth,
            int gridHeight,
            int bucketSize,
            int bucketCountX)
        {
            int x = math.clamp(cell.x, 0, gridWidth - 1) / bucketSize;
            int y = math.clamp(cell.y, 0, gridHeight - 1) / bucketSize;
            return y * bucketCountX + x;
        }
    }
}
