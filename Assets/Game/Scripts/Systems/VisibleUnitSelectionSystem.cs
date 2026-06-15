using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class VisibleUnitSelectionSystem
{
    public enum Filter
    {
        All,
        Soldiers,
        Vehicles
    }

    private World _queryWorld;
    private EntityQuery _visiblePlayerUnitQuery;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _visiblePlayerUnitQuery = em.CreateEntityQuery(new EntityQueryDesc
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
        });
    }

    public bool HasVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        Rect screenRect,
        Filter filter)
    {
        return CollectVisiblePlayerUnits(
            em,
            worldCamera,
            selectionUiReadModelLookup,
            screenRect,
            filter,
            null,
            stopAtFirst: true) > 0;
    }

    public int CollectVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        Rect screenRect,
        Filter filter,
        List<Entity> selected)
    {
        return CollectVisiblePlayerUnits(
            em,
            worldCamera,
            selectionUiReadModelLookup,
            screenRect,
            filter,
            selected,
            stopAtFirst: false);
    }

    private int CollectVisiblePlayerUnits(
        EntityManager em,
        Camera worldCamera,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        Rect screenRect,
        Filter filter,
        List<Entity> selected,
        bool stopAtFirst)
    {
        selected?.Clear();
        if (worldCamera == null || selectionUiReadModelLookup == null)
            return 0;

        EnsureEntityQueries(em);
        int candidateCapacity = _visiblePlayerUnitQuery.CalculateEntityCount();
        using NativeList<VisibleUnitSelectionCandidate> candidates = new(candidateCapacity, Allocator.TempJob);
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
        }.Run(_visiblePlayerUnitQuery);

        int count = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            VisibleUnitSelectionCandidate candidate = candidates[i];
            Vector3 screen = worldCamera.WorldToScreenPoint(candidate.Position);
            if (screen.z <= 0f)
                continue;

            if (screenRect.Contains(new Vector2(screen.x, screen.y)))
            {
                if (stopAtFirst)
                    return 1;

                selected?.Add(candidate.Entity);
                count++;
            }
        }

        return count;
    }

    public void ApplySelectedUnitTags(EntityManager em, IReadOnlyList<Entity> selected)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            Entity entity = selected[i];
            if (!em.HasComponent<SelectedUnitTag>(entity))
                em.AddComponent<SelectedUnitTag>(entity);
        }
    }

    private struct VisibleUnitSelectionCandidate
    {
        public Entity Entity;
        public float3 Position;
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
        public NativeList<VisibleUnitSelectionCandidate> Candidates;

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

                Candidates.Add(new VisibleUnitSelectionCandidate
                {
                    Entity = entities[i],
                    Position = transforms[i].Position
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
