#if UNITY_EDITOR
using System;
using Game.Components;
using Unity.Entities;
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
