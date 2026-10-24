#if UNITY_EDITOR
using System;
using Game.Components;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    // Borrowed input views, invoked only when a fresh opt-in Editor observer is
    // attached. Observers must not retain buffers or change the world.
    internal static class S002PhysicalStartupObservation
    {
        internal static Action<EntityManager, BuildingRuntimeSpawnRequest, BuildingDefinition,
            BuildingRuntimeSpawnCompositionSystemHelper.Context, BuildingPlacementAuthoredBuildingCache,
            BuildingPlacementAuthoredRoadCache> BeforePlacement;
        internal static Action<EntityManager, BuildingRuntimeSpawnRequest> AfterPlacement;
        internal static Action<EntityManager, Entity, BuildingDefinition, int, Vector2Int, byte,
            Quaternion, RuntimeActorBirthContext> BeforeFactory;
        internal static Action<EntityManager, Entity, RuntimeActorBirthContext> AfterFactory;
        internal static Action<EntityManager, Entity, string, Entity[]> SupplyStage;
        // Editor observer owns the allocation until its read dependency is complete.
        // Jobs only write distinct EntityIndexInQuery slots; they never invoke managed code.
        internal static Func<EntityManager, float, ForwardGridFacts, ForwardTraceBuffers> BeginGridInterval;
        internal static Action<EntityManager, string, bool, int, bool> ForwardFuelBoundary;
        internal static Action<EntityManager, Entity> ForwardFuelUnit;
        internal static Action<EntityManager, Entity, int, float> ForwardFuelStorageVisit;
        internal static Action<NativeArray<float>> ForwardFuelRequested;
        internal static Action<EntityManager, EntityQuery, string, float, float, float> ForwardProductionBoundary;
        internal sealed class ForwardGridFacts
        {
            internal GridConfig Grid;
            internal NativeArray<int2> Pool;
            internal NativeArray<GridWalkable> Walkable;
            internal NativeBitArray DynamicBlocked, Occupied;
            internal NativeArray<byte> FriendlyPass;
            internal NativeArray<GridRoad> Roads;
            internal NativeArray<GridRoadSidewalk> Sidewalks;
            internal NativeArray<GridRoadDirt> DirtRoads;
            internal NativeArray<Entity> LiveEntities;
            internal NativeArray<UnitGrid> LiveGrids;
            internal NativeArray<UnitFootprint> LiveFootprints;
        }
        internal struct ForwardTraceBuffers
        {
            internal NativeArray<ForwardVehicleStep> Movement;
            internal NativeArray<ForwardCleanupStep> Cleanup;
        }
        internal struct ForwardCleanupStep
        {
            internal byte Seen;
            internal Entity Entity;
            internal int QueryIndex;
        }
        internal struct ForwardVehicleStep
        {
            internal byte Seen, Called, Moved;
            internal Entity Entity;
            internal int QueryIndex;
            internal LocalTransform Before, After;
            internal UnitVehicleKinematics KinematicsBefore, KinematicsAfter;
            internal UnitVehicleMovement Movement;
            internal float3 Direction;
            internal float Threshold, Speed, DeltaTime, MaxDistance;
        }
        internal static Action<OriginalPlacementFacts> OriginalPlacement;
        internal static Action<Vector2Int, Vector2Int, GridConfig, float> FootprintCenter;
        internal sealed class OriginalPlacementFacts
        {
            internal BuildingDefinition Definition;
            internal Vector2Int Origin, Footprint;
            internal RectInt EffectiveRect;
            internal bool Rotated, HasInvalidPrefix;
            internal GridConfig Grid;
            internal int[] InvalidPrefix;
            internal int PrefixWidth, PrefixHeight;
            internal BuildingPlacementAuthoredBuildingCache Buildings;
            internal BuildingPlacementAuthoredRoadCache Roads;
            internal BuildingPlacementStartupSystemHelper Startup;
            internal BuildingGameplayDependencyCompositionSystemHelper Dependencies;
        }
    }
}
#endif
