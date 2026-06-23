using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingUiCommandAdapter : IBuildingUiCommand
{
    private readonly BuildingUiCommandBoundary boundary;
    private readonly BuildingUiCommandBoundary.Context context;

    public BuildingUiCommandAdapter(BuildingUiCommandBoundary boundary, BuildingUiCommandBoundary.Context context)
    {
        this.boundary = boundary;
        this.context = context;
    }

    public int CurrentDollars => boundary != null ? boundary.CurrentDollars(context) : 0;
    public bool HasPendingBuildingPlacement => boundary != null && boundary.HasPendingBuildingPlacement(context);
    public bool CanConfirmBuildingPlacement => boundary != null && boundary.CanConfirmBuildingPlacement(context);
    public string PlacementStatusText => boundary != null ? boundary.PlacementStatusText(context) : string.Empty;
    public int ActivePlacementCost => boundary != null ? boundary.ActivePlacementCost(context) : 0;
    public float ActivePlacementDurationSeconds => boundary != null ? boundary.ActivePlacementDurationSeconds(context) : 0f;

    public BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        requiredBuildingDisplayName = string.Empty;
        return boundary != null
            ? Map(boundary.GetCampRequestFailure(context, prefab, price, out requiredBuildingDisplayName))
            : BuildingUiCommandFailure.InvalidSelection;
    }

    public BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
    {
        requiredBuildingDisplayName = string.Empty;
        return boundary != null
            ? Map(boundary.TryRequestCampItem(context, prefab, price, out requiredBuildingDisplayName, focusProducerOnSuccess))
            : BuildingUiCommandFailure.InvalidSelection;
    }

    public bool CancelProduction(int buildingId, int pendingProductionIndex)
    {
        return boundary != null && boundary.CancelProduction(context, buildingId, pendingProductionIndex);
    }

    public bool ConfirmBuildingPlacement()
    {
        return boundary != null && boundary.ConfirmBuildingPlacement(context);
    }

    public void CancelBuildingPlacement()
    {
        boundary?.CancelBuildingPlacement(context);
    }

    public bool RotateBuildingPlacement()
    {
        return boundary != null && boundary.RotateBuildingPlacement(context);
    }

    private static BuildingUiCommandFailure Map(BuildingUiCommandBoundary.CampRequestFailure failure)
    {
        return failure switch
        {
            BuildingUiCommandBoundary.CampRequestFailure.None => BuildingUiCommandFailure.None,
            BuildingUiCommandBoundary.CampRequestFailure.NotEnoughMoney => BuildingUiCommandFailure.NotEnoughMoney,
            BuildingUiCommandBoundary.CampRequestFailure.MissingProducerBuilding => BuildingUiCommandFailure.MissingProducerBuilding,
            BuildingUiCommandBoundary.CampRequestFailure.InvalidSelection => BuildingUiCommandFailure.InvalidSelection,
            _ => BuildingUiCommandFailure.InvalidSelection
        };
    }
}

internal sealed class BuildingUiQueryAdapter : IBuildingUiQuery
{
    private readonly BuildingUiQuerySystem system;
    private readonly BuildingUiQuerySystem.Context context;
    private readonly List<BuildingUiQuerySystem.PendingProductionUiEntry> scratch = new();

    public BuildingUiQueryAdapter(BuildingUiQuerySystem system, BuildingUiQuerySystem.Context context)
    {
        this.system = system;
        this.context = context;
    }

    public void GetFriendlyPendingProductionUiEntries(List<BuildingPendingProductionUiEntry> entries)
    {
        if (entries == null)
            return;

        entries.Clear();
        if (system == null)
            return;

        scratch.Clear();
        system.GetFriendlyPendingProductionUiEntries(context, scratch);
        for (int i = 0; i < scratch.Count; i++)
        {
            BuildingUiQuerySystem.PendingProductionUiEntry entry = scratch[i];
            entries.Add(new BuildingPendingProductionUiEntry(
                entry.BuildingId,
                entry.PendingProductionIndex,
                entry.Prefab,
                entry.RemainingSeconds,
                entry.DurationSeconds,
                entry.Progress01,
                entry.StartedAt,
                entry.ReadyAt,
                entry.ProducerDisplayName));
        }
    }
}

internal sealed class MatchRuntimeStateAdapter : IMatchRuntimeState
{
    private RuntimeGameplayStateSystem state;

    public MatchRuntimeStateAdapter(RuntimeGameplayStateSystem state)
    {
        this.state = state;
    }

    public bool PlayRequested
    {
        get => state.PlayRequested;
        set => state.PlayRequested = value;
    }

    public bool SimulationActive
    {
        get => state.SimulationActive;
        set => state.SimulationActive = value;
    }

    public bool SelectionModeActive
    {
        get => state.SelectionModeActive;
        set => state.SelectionModeActive = value;
    }

    public bool BuildModeActive
    {
        get => state.BuildModeActive;
        set => state.BuildModeActive = value;
    }

    public bool ZoomInHeld
    {
        get => state.ZoomInHeld;
        set => state.ZoomInHeld = value;
    }

    public bool ZoomOutHeld
    {
        get => state.ZoomOutHeld;
        set => state.ZoomOutHeld = value;
    }

    public bool SuppressNextWorldClick
    {
        get => state.SuppressNextWorldClick;
        set => state.SuppressNextWorldClick = value;
    }
}

internal sealed class SelectionRectangleStateAdapter : ISelectionRectangleState
{
    private readonly IMatchRuntimeState runtimeState;
    private readonly RtsSelectionInputStateSystem inputStateSystem = new();

    public SelectionRectangleStateAdapter(IMatchRuntimeState runtimeState)
    {
        this.runtimeState = runtimeState;
    }

    public bool TryRead(out SelectionRectangleStateModel state)
    {
        state = default;
        if (runtimeState == null || !runtimeState.PlayRequested)
            return false;

        if (!inputStateSystem.TryRead(out _, out RtsSelectionInputStateComponent inputState))
            return false;

        bool canDrawSelectionRect = runtimeState.SelectionModeActive ||
                                    (TacticalCommandMode)inputState.ActiveCommandMode == TacticalCommandMode.Board;
        if (!canDrawSelectionRect || inputState.HasLiveSelectionRect == 0)
            return false;

        state = new SelectionRectangleStateModel(true, ToGuiRect(inputState.LastLiveSelectionRect));
        return true;
    }

    private static Rect ToGuiRect(float4 screenRect)
    {
        var rect = Rect.MinMaxRect(screenRect.x, screenRect.y, screenRect.z, screenRect.w);
        rect.y = Screen.height - rect.yMax;
        return rect;
    }
}

internal sealed class MatchHudCameraControlAdapter : IMatchHudCameraControl
{
    private readonly SelectionUiCameraSystem cameraSystem;

    public MatchHudCameraControlAdapter(SelectionUiCameraSystem cameraSystem)
    {
        this.cameraSystem = cameraSystem;
    }

    public Camera WorldCamera => cameraSystem != null ? cameraSystem.WorldCamera : null;
    public bool IsCameraDragging => cameraSystem != null && cameraSystem.IsCameraDragging;

    public void MoveCameraGroundCenterTo(Vector3 worldPosition)
    {
        cameraSystem?.MoveCameraGroundCenterTo(worldPosition);
    }
}

internal sealed class MatchHudMinimapDataSourceAdapter : IMatchHudMinimapDataSource
{
    private const int MaxMinimapRasterSamplesPerAxis = 256;
    private Entity cachedGridEntity = Entity.Null;
    private Entity cachedGridRoadEntity = Entity.Null;
    private Entity cachedMarkerBoundaryEntity = Entity.Null;
    private Entity cachedMapSurfaceEntity = Entity.Null;

    public bool TryGetGrid(out MatchHudMinimapGridModel grid)
    {
        grid = default;
        if (!TryGetDefaultEntityManager(out EntityManager em) || !TryGetGridEntity(em, out Entity gridEntity))
            return false;

        GridConfig gridConfig = em.GetComponentData<GridConfig>(gridEntity);
        grid = ToModel(gridConfig);
        return grid.IsValid;
    }

    public void GetMarkers(List<MatchHudMinimapMarkerModel> markers)
    {
        if (markers == null)
            return;

        markers.Clear();
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return;

        if (!TryGetMarkerBoundaryEntity(em, out Entity markerEntity))
            return;

        DynamicBuffer<MatchHudMinimapMarkerElement> buffer = em.GetBuffer<MatchHudMinimapMarkerElement>(markerEntity, true);
        for (int i = 0; i < buffer.Length; i++)
        {
            MatchHudMinimapMarkerElement marker = buffer[i];
            markers.Add(new MatchHudMinimapMarkerModel(
                new Vector3(marker.Position.x, marker.Position.y, marker.Position.z),
                ResolveMarkerAllegiance(marker.FactionId)));
        }
    }

    public void GetRoadCells(MatchHudMinimapAreaModel area, List<MatchHudMinimapRoadCellModel> roadCells)
    {
        if (roadCells == null)
            return;

        roadCells.Clear();
        if (!TryGetDefaultEntityManager(out EntityManager em) || !TryGetGridRoadBuffers(
                em,
                out GridConfig grid,
                out DynamicBuffer<GridRoad> roads,
                out DynamicBuffer<GridRoadSidewalk> sidewalks,
                out DynamicBuffer<GridRoadDirt> dirtRoads))
        {
            return;
        }

        int width = grid.Width;
        int height = grid.Height;
        if (width <= 0 || height <= 0 || grid.CellSize <= 0f)
            return;

        int minX = math.clamp((int)math.floor((area.Origin.x - grid.Origin.x) / grid.CellSize) - 2, 0, width - 1);
        int maxX = math.clamp((int)math.ceil((area.Origin.x + area.Width - grid.Origin.x) / grid.CellSize) + 2, 0, width - 1);
        int minY = math.clamp((int)math.floor((area.Origin.z - grid.Origin.z) / grid.CellSize) - 2, 0, height - 1);
        int maxY = math.clamp((int)math.ceil((area.Origin.z + area.Height - grid.Origin.z) / grid.CellSize) + 2, 0, height - 1);

        int spanX = maxX - minX + 1;
        int spanY = maxY - minY + 1;
        int sampleStride = math.max(1, math.max(
            (int)math.ceil(spanX / (float)MaxMinimapRasterSamplesPerAxis),
            (int)math.ceil(spanY / (float)MaxMinimapRasterSamplesPerAxis)));

        for (int y = minY; y <= maxY; y += sampleStride)
        {
            int rowOffset = y * width;
            for (int x = minX; x <= maxX; x += sampleStride)
            {
                int index = rowOffset + x;
                if ((uint)index >= (uint)roads.Length || roads[index].Value == 0)
                    continue;

                MatchHudMinimapRoadKind kind = MatchHudMinimapRoadKind.Road;
                if (dirtRoads.IsCreated && index < dirtRoads.Length && dirtRoads[index].Value != 0)
                    kind = MatchHudMinimapRoadKind.DirtRoad;
                else if (sidewalks.IsCreated && index < sidewalks.Length && sidewalks[index].Value != 0)
                    kind = MatchHudMinimapRoadKind.Sidewalk;

                roadCells.Add(new MatchHudMinimapRoadCellModel(
                    new Vector3(
                        grid.Origin.x + (x + 0.5f) * grid.CellSize,
                        grid.Origin.y,
                        grid.Origin.z + (y + 0.5f) * grid.CellSize),
                    grid.CellSize,
                    kind));
            }
        }
    }

    public void GetSurfaceFeatures(MatchHudMinimapAreaModel area, List<MatchHudMinimapSurfaceFeatureModel> features)
    {
        if (features == null)
            return;

        features.Clear();
        if (!TryGetDefaultEntityManager(out EntityManager em) ||
            !TryGetMapSurface(em, out MapSurfaceComponent surface, out DynamicBuffer<MapSurfaceSceneOverlay> sceneOverlays, out bool hasSceneOverlays))
        {
            return;
        }

        AppendSurfaceBlobFeatures(area, surface, features);
        if (hasSceneOverlays)
            AppendSceneOverlayFeatures(area, sceneOverlays, features);
    }

    private static MatchHudMinimapGridModel ToModel(GridConfig grid)
    {
        return new MatchHudMinimapGridModel(
            new Vector3(grid.Origin.x, grid.Origin.y, grid.Origin.z),
            grid.Width,
            grid.Height,
            grid.CellSize);
    }

    private static bool TryGetDefaultEntityManager(out EntityManager em)
    {
        em = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        em = world.EntityManager;
        return true;
    }

    private bool TryGetGridEntity(EntityManager em, out Entity gridEntity)
    {
        if (IsValidEntity(em, cachedGridEntity, ComponentType.ReadOnly<GridConfig>()))
        {
            gridEntity = cachedGridEntity;
            return true;
        }

        if (!TryFindSingleEntity(em, out gridEntity, ComponentType.ReadOnly<GridConfig>()))
            return false;

        cachedGridEntity = gridEntity;
        return true;
    }

    private bool TryGetGridRoadBuffers(
        EntityManager em,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBuffer<GridRoadSidewalk> sidewalks,
        out DynamicBuffer<GridRoadDirt> dirtRoads)
    {
        grid = default;
        roads = default;
        sidewalks = default;
        dirtRoads = default;

        if (!IsValidEntity(
                em,
                cachedGridRoadEntity,
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridRoad>()) &&
            !TryFindSingleEntity(
                em,
                out cachedGridRoadEntity,
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridRoad>()))
        {
            return false;
        }

        Entity gridEntity = cachedGridRoadEntity;
        grid = em.GetComponentData<GridConfig>(gridEntity);
        roads = em.GetBuffer<GridRoad>(gridEntity, true);
        if (em.HasBuffer<GridRoadSidewalk>(gridEntity))
            sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity, true);
        if (em.HasBuffer<GridRoadDirt>(gridEntity))
            dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity, true);
        return true;
    }

    private bool TryGetMapSurface(
        EntityManager em,
        out MapSurfaceComponent surface,
        out DynamicBuffer<MapSurfaceSceneOverlay> sceneOverlays,
        out bool hasSceneOverlays)
    {
        surface = default;
        sceneOverlays = default;
        hasSceneOverlays = false;
        if (!IsValidEntity(em, cachedMapSurfaceEntity, ComponentType.ReadOnly<MapSurfaceComponent>()) &&
            !TryFindSingleEntity(em, out cachedMapSurfaceEntity, ComponentType.ReadOnly<MapSurfaceComponent>()))
        {
            return false;
        }

        Entity entity = cachedMapSurfaceEntity;
        surface = em.GetComponentData<MapSurfaceComponent>(entity);
        if (em.HasBuffer<MapSurfaceSceneOverlay>(entity))
        {
            sceneOverlays = em.GetBuffer<MapSurfaceSceneOverlay>(entity, true);
            hasSceneOverlays = sceneOverlays.IsCreated && sceneOverlays.Length > 0;
        }

        return true;
    }

    private bool TryGetMarkerBoundaryEntity(EntityManager em, out Entity markerEntity)
    {
        if (IsValidEntity(
                em,
                cachedMarkerBoundaryEntity,
                ComponentType.ReadOnly<MatchHudMinimapMarkerBoundary>(),
                ComponentType.ReadOnly<MatchHudMinimapMarkerElement>()))
        {
            markerEntity = cachedMarkerBoundaryEntity;
            return true;
        }

        if (!TryFindSingleEntity(
                em,
                out markerEntity,
                ComponentType.ReadOnly<MatchHudMinimapMarkerBoundary>(),
                ComponentType.ReadOnly<MatchHudMinimapMarkerElement>()))
        {
            return false;
        }

        cachedMarkerBoundaryEntity = markerEntity;
        return true;
    }

    private static bool IsValidEntity(EntityManager em, Entity entity, ComponentType requiredComponent)
    {
        return entity != Entity.Null &&
               em.Exists(entity) &&
               em.HasComponent(entity, requiredComponent);
    }

    private static bool IsValidEntity(EntityManager em, Entity entity, ComponentType firstRequiredComponent, ComponentType secondRequiredComponent)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return false;

        return em.HasComponent(entity, firstRequiredComponent) &&
               em.HasComponent(entity, secondRequiredComponent);
    }

    private static bool TryFindSingleEntity(EntityManager em, out Entity entity, params ComponentType[] requiredComponents)
    {
        entity = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(requiredComponents);
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        if (entities.Length == 0)
            return false;

        entity = entities[0];
        return true;
    }

    private static void AppendSurfaceBlobFeatures(
        MatchHudMinimapAreaModel area,
        MapSurfaceComponent surface,
        List<MatchHudMinimapSurfaceFeatureModel> features)
    {
        if (surface.HasSurfaceData == 0 ||
            !surface.SurfaceBlob.IsCreated ||
            surface.CellSize <= 0f ||
            surface.Dimensions.x <= 0 ||
            surface.Dimensions.y <= 0)
        {
            return;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        int minX = math.clamp((int)math.floor((area.Origin.x - surface.GridOrigin.x) / surface.CellSize) - 2, 0, surface.Dimensions.x - 1);
        int maxX = math.clamp((int)math.ceil((area.Origin.x + area.Width - surface.GridOrigin.x) / surface.CellSize) + 2, 0, surface.Dimensions.x - 1);
        int minY = math.clamp((int)math.floor((area.Origin.z - surface.GridOrigin.z) / surface.CellSize) - 2, 0, surface.Dimensions.y - 1);
        int maxY = math.clamp((int)math.ceil((area.Origin.z + area.Height - surface.GridOrigin.z) / surface.CellSize) + 2, 0, surface.Dimensions.y - 1);

        int spanX = maxX - minX + 1;
        int spanY = maxY - minY + 1;
        int sampleStride = math.max(1, math.max(
            (int)math.ceil(spanX / (float)MaxMinimapRasterSamplesPerAxis),
            (int)math.ceil(spanY / (float)MaxMinimapRasterSamplesPerAxis)));

        for (int y = minY; y <= maxY; y += sampleStride)
        {
            int rowOffset = y * surface.Dimensions.x;
            for (int x = minX; x <= maxX; x += sampleStride)
            {
                int index = rowOffset + x;
                if ((uint)index >= (uint)blob.Cells.Length)
                    continue;

                MapSurfaceCell cell = blob.Cells[index];
                if (cell.SurfaceCount == 0 ||
                    !TryResolveRasterSurfaceSample(ref blob, cell, out MapSurfaceSample sample) ||
                    !TryResolveSurfaceFeatureKind(sample.SurfaceType, sample.Flags, out MatchHudMinimapSurfaceFeatureKind kind))
                {
                    continue;
                }

                features.Add(new MatchHudMinimapSurfaceFeatureModel(
                    new Vector3(
                        surface.GridOrigin.x + (x + 0.5f) * surface.CellSize,
                        surface.GridOrigin.y,
                        surface.GridOrigin.z + (y + 0.5f) * surface.CellSize),
                    new Vector2(surface.CellSize * 0.5f, surface.CellSize * 0.5f),
                    surface.CellSize,
                    kind,
                    fillArea: false));
            }
        }
    }

    private static bool TryResolveRasterSurfaceSample(ref MapSurfaceBlob blob, MapSurfaceCell cell, out MapSurfaceSample sample)
    {
        sample = default;
        for (int i = 0; i < cell.SurfaceCount; i++)
        {
            int sampleIndex = cell.FirstSurfaceIndex + i;
            if ((uint)sampleIndex >= (uint)blob.Samples.Length)
                continue;

            MapSurfaceSample candidate = blob.Samples[sampleIndex];
            if (!TryResolveSurfaceFeatureKind(candidate.SurfaceType, candidate.Flags, out _))
                continue;

            sample = candidate;
            return true;
        }

        return false;
    }

    private static void AppendSceneOverlayFeatures(
        MatchHudMinimapAreaModel area,
        DynamicBuffer<MapSurfaceSceneOverlay> overlays,
        List<MatchHudMinimapSurfaceFeatureModel> features)
    {
        if (!overlays.IsCreated || overlays.Length == 0)
            return;

        Rect projectionRect = new(area.Origin.x, area.Origin.z, area.Width, area.Height);
        for (int i = 0; i < overlays.Length; i++)
        {
            MapSurfaceSceneOverlay overlay = overlays[i];
            if (!TryResolveSurfaceFeatureKind(overlay.SurfaceType, overlay.Flags, out MatchHudMinimapSurfaceFeatureKind kind))
                continue;

            float minX = overlay.Center.x - overlay.HalfExtents.x;
            float maxX = overlay.Center.x + overlay.HalfExtents.x;
            float minZ = overlay.Center.z - overlay.HalfExtents.y;
            float maxZ = overlay.Center.z + overlay.HalfExtents.y;
            Rect overlayRect = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
            if (!projectionRect.Overlaps(overlayRect))
                continue;

            features.Add(new MatchHudMinimapSurfaceFeatureModel(
                new Vector3(overlay.Center.x, overlay.Center.y, overlay.Center.z),
                new Vector2(overlay.HalfExtents.x, overlay.HalfExtents.y),
                math.max(1f, math.max(overlay.HalfExtents.x, overlay.HalfExtents.y) * 2f),
                kind,
                fillArea: true));
        }
    }

    private static bool TryResolveSurfaceFeatureKind(
        MapSurfaceType surfaceType,
        MapSurfaceFlags flags,
        out MatchHudMinimapSurfaceFeatureKind kind)
    {
        if (surfaceType == MapSurfaceType.Blocked)
        {
            kind = MatchHudMinimapSurfaceFeatureKind.Blocked;
            return true;
        }

        if ((flags & MapSurfaceFlags.Bridge) != 0 || surfaceType == MapSurfaceType.BridgeDeck)
        {
            kind = MatchHudMinimapSurfaceFeatureKind.Bridge;
            return true;
        }

        if ((flags & MapSurfaceFlags.Ramp) != 0 || surfaceType == MapSurfaceType.Ramp)
        {
            kind = MatchHudMinimapSurfaceFeatureKind.Ramp;
            return true;
        }

        if ((flags & MapSurfaceFlags.Highway) != 0 || surfaceType == MapSurfaceType.Highway)
        {
            kind = MatchHudMinimapSurfaceFeatureKind.Highway;
            return true;
        }

        if (surfaceType == MapSurfaceType.DirtRoad)
        {
            kind = MatchHudMinimapSurfaceFeatureKind.DirtRoad;
            return true;
        }

        if (surfaceType == MapSurfaceType.Plaza)
        {
            kind = MatchHudMinimapSurfaceFeatureKind.Plaza;
            return true;
        }

        if (surfaceType == MapSurfaceType.Road || (flags & MapSurfaceFlags.Road) != 0)
        {
            kind = MatchHudMinimapSurfaceFeatureKind.Road;
            return true;
        }

        kind = default;
        return false;
    }

    private static MatchHudMinimapMarkerAllegiance ResolveMarkerAllegiance(byte factionId)
    {
        if (FactionIdentity.IsPlayerControlled(factionId))
            return MatchHudMinimapMarkerAllegiance.Player;
        if (FactionIdentity.IsHostileToPlayer(factionId))
            return MatchHudMinimapMarkerAllegiance.Enemy;
        return MatchHudMinimapMarkerAllegiance.Neutral;
    }
}

internal sealed class QuickCustomGameConfigStore : IQuickCustomGameConfigStore
{
    public UiQuickCustomGameConfig Current => ToUiConfig(QuickGameConfig.FromAISettingsSnapshot(AISettingsRuntimeState.CurrentSnapshot));
    public UiQuickCustomGameConfig Defaults => ToUiConfig(QuickGameConfig.Defaults);

    public void Apply(UiQuickCustomGameConfig config)
    {
        AISettingsRuntimeState.ApplySnapshot(ToRuntimeConfig(config).ToAISettingsSnapshot());
    }

    private static UiQuickCustomGameConfig ToUiConfig(QuickGameConfig config)
    {
        return new UiQuickCustomGameConfig
        {
            EnemyType = (UiQuickGameEnemyType)config.EnemyType,
            EnemyCount = config.EnemyCount,
            Difficulty = (UiAiDifficultySetting)config.Difficulty,
            StartingMoney = (UiAiStartingMoneySetting)config.StartingMoney,
            IncomeMultiplier = config.IncomeMultiplier,
            BuildSpeed = (UiAiSpeedSetting)config.BuildSpeed,
            UnitProductionSpeed = (UiAiSpeedSetting)config.UnitProductionSpeed,
            AttackGroupSize = (UiAiAttackGroupSizeSetting)config.AttackGroupSize,
            AttackFrequency = (UiAiAttackFrequencySetting)config.AttackFrequency,
            Aggression = (UiAiAggressionSetting)config.Aggression,
            Expansion = (UiAiExpansionSetting)config.Expansion,
            TargetPriority = (UiAiTargetPriority)config.TargetPriority,
            PlayerAutoAIEnabled = config.PlayerAutoAIEnabled,
            WinCondition = (UiQuickGameWinCondition)config.WinCondition,
            FogOfWar = config.FogOfWar,
            IntelReveal = config.IntelReveal,
            StartingResources = (UiQuickGameStartingResources)config.StartingResources,
            MapSeed = config.MapSeed
        };
    }

    private static QuickGameConfig ToRuntimeConfig(UiQuickCustomGameConfig config)
    {
        return new QuickGameConfig
        {
            EnemyType = (QuickGameEnemyType)config.EnemyType,
            EnemyCount = config.EnemyCount,
            Difficulty = (AIDifficultySetting)config.Difficulty,
            StartingMoney = (AIStartingMoneySetting)config.StartingMoney,
            IncomeMultiplier = config.IncomeMultiplier,
            BuildSpeed = (AISpeedSetting)config.BuildSpeed,
            UnitProductionSpeed = (AISpeedSetting)config.UnitProductionSpeed,
            AttackGroupSize = (AIAttackGroupSizeSetting)config.AttackGroupSize,
            AttackFrequency = (AIAttackFrequencySetting)config.AttackFrequency,
            Aggression = (AIAggressionSetting)config.Aggression,
            Expansion = (AIExpansionSetting)config.Expansion,
            TargetPriority = (AITargetPriority)config.TargetPriority,
            PlayerAutoAIEnabled = config.PlayerAutoAIEnabled,
            WinCondition = (QuickGameWinCondition)config.WinCondition,
            FogOfWar = config.FogOfWar,
            IntelReveal = config.IntelReveal,
            StartingResources = (QuickGameStartingResources)config.StartingResources,
            MapSeed = config.MapSeed
        };
    }
}

internal sealed class MatchLaunchCommand : IMatchLaunchCommand
{
    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartRequestStartupSystemHelper matchStartRequestSystem = new();

    public void LaunchMatch(Component source)
    {
        QueueMatchLoadAndStart();

        UIRouterView router = source != null ? source.GetComponentInParent<UIRouterView>() : null;
        if (router != null)
            router.gameObject.SetActive(false);
    }

    private void QueueMatchLoadAndStart()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogError("[GameLaunch] Cannot queue Match start because the default ECS world is missing.");
            return;
        }

        EntityManager entityManager = world.EntityManager;
        bool loadQueued = sceneLifecycleSystem.QueueLoadMatch(entityManager);
        bool startQueued = matchStartRequestSystem.QueueStartAfterMatchLoaded(entityManager);
        if (!loadQueued || !startQueued)
            Debug.LogError($"[GameLaunch] Failed to queue Match start. loadQueued={(loadQueued ? 1 : 0)} startQueued={(startQueued ? 1 : 0)}");
    }
}

internal sealed class SelectionDiagnosticsSinkAdapter : ISelectionDiagnosticsSink
{
    public void LogMoveCommandTrace(string message)
    {
        SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(message);
    }
}
