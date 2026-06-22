using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public struct VisibleUnitSelectionCandidateSnapshot : IComponentData
{
}

public struct VisibleUnitSelectionCandidateElement : IBufferElementData
{
    public Entity Entity;
    public Unity.Mathematics.float3 Position;
    public byte IsVehicle;
}

public partial struct VisibleUnitSelectionCandidateSystem : ISystem
{
    private EntityQuery _visiblePlayerUnitQuery;
    private Entity _snapshotEntity;

    public void OnCreate(ref SystemState state)
    {
        _visiblePlayerUnitQuery = state.GetEntityQuery(VisibleUnitSelectionCandidateCollector.CreateQueryDesc());
        _snapshotEntity = state.EntityManager.CreateEntity(
            typeof(VisibleUnitSelectionCandidateSnapshot),
            typeof(VisibleUnitSelectionCandidateElement));
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_snapshotEntity != Entity.Null && state.EntityManager.Exists(_snapshotEntity))
            state.EntityManager.DestroyEntity(_snapshotEntity);
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_snapshotEntity == Entity.Null || !state.EntityManager.Exists(_snapshotEntity))
        {
            _snapshotEntity = state.EntityManager.CreateEntity(
                typeof(VisibleUnitSelectionCandidateSnapshot),
                typeof(VisibleUnitSelectionCandidateElement));
        }

        DynamicBuffer<VisibleUnitSelectionCandidateElement> snapshot =
            state.EntityManager.GetBuffer<VisibleUnitSelectionCandidateElement>(_snapshotEntity);
        snapshot.Clear();

        int candidateCapacity = _visiblePlayerUnitQuery.CalculateEntityCount();
        using NativeList<VisibleUnitSelectionSystem.VisibleUnitSelectionCandidate> candidates =
            new(candidateCapacity, Allocator.TempJob);
        VisibleUnitSelectionCandidateCollector.Collect(ref state, _visiblePlayerUnitQuery, candidates);

        for (int i = 0; i < candidates.Length; i++)
        {
            VisibleUnitSelectionSystem.VisibleUnitSelectionCandidate candidate = candidates[i];
            snapshot.Add(new VisibleUnitSelectionCandidateElement
            {
                Entity = candidate.Entity,
                Position = candidate.Position,
                IsVehicle = candidate.IsVehicle
            });
        }
    }
}

internal static class VisibleUnitSelectionCandidateCollector
{
    public static EntityQueryDesc CreateQueryDesc()
    {
        return new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<StaticGridBlocker>()
            }
        };
    }

    public static void Collect(
        EntityManager em,
        EntityQuery query,
        VisibleUnitSelectionSystem.Filter filter,
        NativeList<VisibleUnitSelectionSystem.VisibleUnitSelectionCandidate> candidates)
    {
        candidates.Clear();
        new CollectVisibleUnitCandidatesJob
        {
            Filter = (byte)filter,
            EntityType = em.GetEntityTypeHandle(),
            FactionType = em.GetComponentTypeHandle<Faction>(true),
            LocalToWorldType = em.GetComponentTypeHandle<LocalToWorld>(true),
            SourcePrefabKeyType = em.GetComponentTypeHandle<UnitSourcePrefabKey>(true),
            FootprintType = em.GetComponentTypeHandle<UnitFootprint>(true),
            MovementBehaviorType = em.GetComponentTypeHandle<UnitMovementBehavior>(true),
            Candidates = candidates
        }.Run(query);
    }

    public static void Collect(
        ref SystemState state,
        EntityQuery query,
        NativeList<VisibleUnitSelectionSystem.VisibleUnitSelectionCandidate> candidates)
    {
        candidates.Clear();
        new CollectVisibleUnitCandidatesJob
        {
            Filter = (byte)VisibleUnitSelectionSystem.Filter.All,
            EntityType = state.GetEntityTypeHandle(),
            FactionType = state.GetComponentTypeHandle<Faction>(true),
            LocalToWorldType = state.GetComponentTypeHandle<LocalToWorld>(true),
            SourcePrefabKeyType = state.GetComponentTypeHandle<UnitSourcePrefabKey>(true),
            FootprintType = state.GetComponentTypeHandle<UnitFootprint>(true),
            MovementBehaviorType = state.GetComponentTypeHandle<UnitMovementBehavior>(true),
            Candidates = candidates
        }.Run(query);
    }

    [BurstCompile]
    private struct CollectVisibleUnitCandidatesJob : IJobChunk
    {
        public byte Filter;
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<Faction> FactionType;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldType;
        [ReadOnly] public ComponentTypeHandle<UnitSourcePrefabKey> SourcePrefabKeyType;
        [ReadOnly] public ComponentTypeHandle<UnitFootprint> FootprintType;
        [ReadOnly] public ComponentTypeHandle<UnitMovementBehavior> MovementBehaviorType;
        public NativeList<VisibleUnitSelectionSystem.VisibleUnitSelectionCandidate> Candidates;

        public void Execute(
            in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref FactionType);
            NativeArray<LocalToWorld> transforms = chunk.GetNativeArray(ref LocalToWorldType);
            bool hasSourcePrefabKey = chunk.Has(ref SourcePrefabKeyType);
            bool hasFootprint = chunk.Has(ref FootprintType);
            bool hasMovementBehavior = chunk.Has(ref MovementBehaviorType);
            NativeArray<UnitSourcePrefabKey> sourcePrefabKeys = hasSourcePrefabKey
                ? chunk.GetNativeArray(ref SourcePrefabKeyType)
                : default;
            NativeArray<UnitFootprint> footprints = hasFootprint
                ? chunk.GetNativeArray(ref FootprintType)
                : default;
            NativeArray<UnitMovementBehavior> movementBehaviors = hasMovementBehavior
                ? chunk.GetNativeArray(ref MovementBehaviorType)
                : default;

            for (int i = 0; i < entities.Length; i++)
            {
                if (factions[i].Id != FactionIdentity.PlayerFactionId)
                    continue;

                bool isVehicle = IsVehicleForVisibleSelection(
                    i,
                    hasSourcePrefabKey,
                    sourcePrefabKeys,
                    hasFootprint,
                    footprints,
                    hasMovementBehavior,
                    movementBehaviors);
                if (Filter == (byte)VisibleUnitSelectionSystem.Filter.Soldiers && isVehicle)
                    continue;
                if (Filter == (byte)VisibleUnitSelectionSystem.Filter.Vehicles && !isVehicle)
                    continue;

                Candidates.Add(new VisibleUnitSelectionSystem.VisibleUnitSelectionCandidate
                {
                    Entity = entities[i],
                    Position = transforms[i].Position,
                    IsVehicle = isVehicle ? (byte)1 : (byte)0
                });
            }
        }

        private static bool IsVehicleForVisibleSelection(
            int index,
            bool hasSourcePrefabKey,
            NativeArray<UnitSourcePrefabKey> sourcePrefabKeys,
            bool hasFootprint,
            NativeArray<UnitFootprint> footprints,
            bool hasMovementBehavior,
            NativeArray<UnitMovementBehavior> movementBehaviors)
        {
            if (hasSourcePrefabKey)
            {
                FixedString64Bytes sourceKey = sourcePrefabKeys[index].Value;
                if (StartsWithUnitVehiclePrefix(sourceKey))
                    return true;
                if (StartsWithUnitCharacterPrefix(sourceKey))
                    return false;
            }

            return hasFootprint &&
                   hasMovementBehavior &&
                   UnitVehicleMovementUtility.IsVehicle(footprints[index], movementBehaviors[index]);
        }

        private static bool StartsWithUnitVehiclePrefix(FixedString64Bytes value)
        {
            return HasNineBytePrefixIgnoreCase(
                value,
                (byte)'U',
                (byte)'n',
                (byte)'i',
                (byte)'t',
                (byte)'_',
                (byte)'V',
                (byte)'e',
                (byte)'h',
                (byte)'_');
        }

        private static bool StartsWithUnitCharacterPrefix(FixedString64Bytes value)
        {
            return HasNineBytePrefixIgnoreCase(
                value,
                (byte)'U',
                (byte)'n',
                (byte)'i',
                (byte)'t',
                (byte)'_',
                (byte)'C',
                (byte)'h',
                (byte)'r',
                (byte)'_');
        }

        private static bool HasNineBytePrefixIgnoreCase(
            FixedString64Bytes value,
            byte c0,
            byte c1,
            byte c2,
            byte c3,
            byte c4,
            byte c5,
            byte c6,
            byte c7,
            byte c8)
        {
            return value.Length >= 9 &&
                   EqualsAsciiIgnoreCase(value[0], c0) &&
                   EqualsAsciiIgnoreCase(value[1], c1) &&
                   EqualsAsciiIgnoreCase(value[2], c2) &&
                   EqualsAsciiIgnoreCase(value[3], c3) &&
                   EqualsAsciiIgnoreCase(value[4], c4) &&
                   EqualsAsciiIgnoreCase(value[5], c5) &&
                   EqualsAsciiIgnoreCase(value[6], c6) &&
                   EqualsAsciiIgnoreCase(value[7], c7) &&
                   EqualsAsciiIgnoreCase(value[8], c8);
        }

        private static bool EqualsAsciiIgnoreCase(byte a, byte b)
        {
            return ToLowerAscii(a) == ToLowerAscii(b);
        }

        private static byte ToLowerAscii(byte value)
        {
            return value >= (byte)'A' && value <= (byte)'Z'
                ? (byte)(value + 32)
                : value;
        }
    }
}
