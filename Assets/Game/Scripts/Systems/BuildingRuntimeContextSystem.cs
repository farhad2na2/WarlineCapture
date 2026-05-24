using System;
using UnityEngine;

internal sealed class BuildingRuntimeContextSystem
{
    public readonly struct Source
    {
        public readonly Transform BuildingRoot;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingRunwaySystem RunwaySystem;
        public readonly BuildingPlacementValidationSystem PlacementValidationSystem;
        public readonly BuildingPlacementValidationSystem.WallValidationContext WallValidationContext;
        public readonly BuildingRuntimeSpawnSystem.TryGetGridDataDelegate TryGetGridData;
        public readonly BuildingRuntimeSpawnSystem.GetPlacementFootprintDelegate GetPlacementFootprint;
        public readonly BuildingRuntimeSpawnSystem.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
        public readonly BuildingRuntimeSpawnSystem.IsPlacementValidDelegate IsPlacementValid;
        public readonly BuildingRuntimeSpawnSystem.HasCachedInvalidCellInFootprintDelegate HasCachedInvalidCellInFootprint;
        public readonly BuildingRuntimeSpawnSystem.CreateBuildingVisualInstanceDelegate CreateBuildingVisualInstance;
        public readonly BuildingRuntimeSpawnSystem.PositionBuildingObjectDelegate PositionBuildingObject;
        public readonly BuildingRuntimeSpawnSystem.RegisterRuntimeBuildingDelegate RegisterRuntimeBuilding;
        public readonly BuildingRuntimeSpawnSystem.SetRuntimeBuildingOwnerFactionDelegate SetRuntimeBuildingOwnerFaction;
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly BuildingPlacementInteractionSystem RuntimeLinkInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context RuntimeLinkInteractionContext;
        public readonly Func<bool> IsDeferringSideEffects;
        public readonly BuildingRuntimeCreationSystem.TryGetGridDelegate TryGetGridForRuntimeCreation;
        public readonly BuildingRuntimeCreationSystem.ResolvePlacementRectDelegate ResolvePlacementRect;
        public readonly BuildingRuntimeCreationSystem.ShouldBlockPathingDelegate ShouldBlockPathing;
        public readonly BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate RemoveOverlappingBlockers;
        public readonly BuildingRuntimeCreationSystem.CreateBlockerEntityDelegate CreateBlockerEntity;
        public readonly BuildingRuntimeCreationSystem.CreateCombatEntityDelegate CreateCombatEntity;
        public readonly BuildingRuntimeCreationSystem.RedirectUnitsDelegate RedirectUnits;
        public readonly BuildingRuntimeCreationSystem.AddDeferredRedirectFootprintDelegate AddDeferredRedirectFootprint;
        public readonly BuildingRuntimeCreationSystem.RuntimeAction MarkPendingMarkerRefresh;
        public readonly BuildingRuntimeCreationSystem.RuntimeBuildingAction InitializeVisuals;
        public readonly BuildingRuntimeCreationSystem.RuntimeAction RefreshMarkers;
        public readonly BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingVisualSystem BuildingVisualSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;
        public readonly Func<int, bool> DeleteBuildingById;
        public readonly Action BeginDeferredRuntimeBuildingSideEffects;
        public readonly Action EndDeferredRuntimeBuildingSideEffects;

        public Source(
            Transform buildingRoot,
            BuildingDefinitionSystem definitionSystem,
            BuildingRunwaySystem runwaySystem,
            BuildingPlacementValidationSystem placementValidationSystem,
            BuildingPlacementValidationSystem.WallValidationContext wallValidationContext,
            BuildingRuntimeSpawnSystem.TryGetGridDataDelegate tryGetGridData,
            BuildingRuntimeSpawnSystem.GetPlacementFootprintDelegate getPlacementFootprint,
            BuildingRuntimeSpawnSystem.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            BuildingRuntimeSpawnSystem.IsPlacementValidDelegate isPlacementValid,
            BuildingRuntimeSpawnSystem.HasCachedInvalidCellInFootprintDelegate hasCachedInvalidCellInFootprint,
            BuildingRuntimeSpawnSystem.CreateBuildingVisualInstanceDelegate createBuildingVisualInstance,
            BuildingRuntimeSpawnSystem.PositionBuildingObjectDelegate positionBuildingObject,
            BuildingRuntimeSpawnSystem.RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
            BuildingRuntimeSpawnSystem.SetRuntimeBuildingOwnerFactionDelegate setRuntimeBuildingOwnerFaction,
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            BuildingPlacementInteractionSystem runtimeLinkInteractionSystem,
            BuildingPlacementInteractionSystem.Context runtimeLinkInteractionContext,
            Func<bool> isDeferringSideEffects,
            BuildingRuntimeCreationSystem.TryGetGridDelegate tryGetGridForRuntimeCreation,
            BuildingRuntimeCreationSystem.ResolvePlacementRectDelegate resolvePlacementRect,
            BuildingRuntimeCreationSystem.ShouldBlockPathingDelegate shouldBlockPathing,
            BuildingRuntimeCreationSystem.RemoveOverlappingBlockersDelegate removeOverlappingBlockers,
            BuildingRuntimeCreationSystem.CreateBlockerEntityDelegate createBlockerEntity,
            BuildingRuntimeCreationSystem.CreateCombatEntityDelegate createCombatEntity,
            BuildingRuntimeCreationSystem.RedirectUnitsDelegate redirectUnits,
            BuildingRuntimeCreationSystem.AddDeferredRedirectFootprintDelegate addDeferredRedirectFootprint,
            BuildingRuntimeCreationSystem.RuntimeAction markPendingMarkerRefresh,
            BuildingRuntimeCreationSystem.RuntimeBuildingAction initializeVisuals,
            BuildingRuntimeCreationSystem.RuntimeAction refreshMarkers,
            BuildingRuntimeOwnershipSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingVisualSystem buildingVisualSystem,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock,
            Func<int, bool> deleteBuildingById,
            Action beginDeferredRuntimeBuildingSideEffects,
            Action endDeferredRuntimeBuildingSideEffects)
        {
            BuildingRoot = buildingRoot;
            DefinitionSystem = definitionSystem;
            RunwaySystem = runwaySystem;
            PlacementValidationSystem = placementValidationSystem;
            WallValidationContext = wallValidationContext;
            TryGetGridData = tryGetGridData;
            GetPlacementFootprint = getPlacementFootprint;
            GetEffectivePlacementRect = getEffectivePlacementRect;
            IsPlacementValid = isPlacementValid;
            HasCachedInvalidCellInFootprint = hasCachedInvalidCellInFootprint;
            CreateBuildingVisualInstance = createBuildingVisualInstance;
            PositionBuildingObject = positionBuildingObject;
            RegisterRuntimeBuilding = registerRuntimeBuilding;
            SetRuntimeBuildingOwnerFaction = setRuntimeBuildingOwnerFaction;
            RuntimeBuildingSystem = runtimeBuildingSystem;
            RuntimeLinkInteractionSystem = runtimeLinkInteractionSystem;
            RuntimeLinkInteractionContext = runtimeLinkInteractionContext;
            IsDeferringSideEffects = isDeferringSideEffects;
            TryGetGridForRuntimeCreation = tryGetGridForRuntimeCreation;
            ResolvePlacementRect = resolvePlacementRect;
            ShouldBlockPathing = shouldBlockPathing;
            RemoveOverlappingBlockers = removeOverlappingBlockers;
            CreateBlockerEntity = createBlockerEntity;
            CreateCombatEntity = createCombatEntity;
            RedirectUnits = redirectUnits;
            AddDeferredRedirectFootprint = addDeferredRedirectFootprint;
            MarkPendingMarkerRefresh = markPendingMarkerRefresh;
            InitializeVisuals = initializeVisuals;
            RefreshMarkers = refreshMarkers;
            TryGetEntityManager = tryGetEntityManager;
            BuildingVisualSystem = buildingVisualSystem;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
            DeleteBuildingById = deleteBuildingById;
            BeginDeferredRuntimeBuildingSideEffects = beginDeferredRuntimeBuildingSideEffects;
            EndDeferredRuntimeBuildingSideEffects = endDeferredRuntimeBuildingSideEffects;
        }
    }

    public BuildingRuntimeSpawnSystem.Context CreateSpawnContext(Source source)
    {
        return new BuildingRuntimeSpawnSystem.Context(
            source.BuildingRoot,
            source.DefinitionSystem,
            source.RunwaySystem,
            source.PlacementValidationSystem,
            source.WallValidationContext,
            source.TryGetGridData,
            source.GetPlacementFootprint,
            source.GetEffectivePlacementRect,
            source.IsPlacementValid,
            source.HasCachedInvalidCellInFootprint,
            source.CreateBuildingVisualInstance,
            source.PositionBuildingObject,
            source.RegisterRuntimeBuilding,
            source.SetRuntimeBuildingOwnerFaction);
    }

    public BuildingRuntimeCreationSystem.Context CreateCreationContext(Source source)
    {
        return new BuildingRuntimeCreationSystem.Context(
            source.RuntimeBuildingSystem,
            source.RuntimeLinkInteractionSystem,
            source.RuntimeLinkInteractionContext,
            source.IsDeferringSideEffects?.Invoke() == true,
            source.TryGetGridForRuntimeCreation,
            source.ResolvePlacementRect,
            source.ShouldBlockPathing,
            source.RemoveOverlappingBlockers,
            source.CreateBlockerEntity,
            source.CreateCombatEntity,
            source.RedirectUnits,
            source.AddDeferredRedirectFootprint,
            source.MarkPendingMarkerRefresh,
            source.InitializeVisuals,
            source.RefreshMarkers);
    }

    public BuildingRuntimeOwnershipSystem.Context CreateOwnershipContext(Source source)
    {
        return new BuildingRuntimeOwnershipSystem.Context(
            source.TryGetEntityManager,
            source.BuildingVisualSystem,
            source.FactionVisualSettings,
            source.MarkerPropertyBlock);
    }

    public BuildingRuntimeCitySpawnSystem.Context CreateCitySpawnContext(Source source)
    {
        return new BuildingRuntimeCitySpawnSystem.Context(
            CreateSpawnContext(source),
            source.DeleteBuildingById,
            source.BeginDeferredRuntimeBuildingSideEffects,
            source.EndDeferredRuntimeBuildingSideEffects);
    }
}
