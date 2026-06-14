using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SelectionOrderMarkerSystem
{
    public delegate bool IsPreviewTargetValidWithSourceDelegate(EntityManager em, Entity source, Entity target);
    public delegate bool TryResolveRuntimeBuildingInstanceDelegate(Entity combatEntity, int runtimeBuildingId, out GameObject instance);

    private World _queryWorld;
    private EntityQuery _gridBlockerQuery;
    private GameObject _moveOrderMarker;
    private Renderer[] _moveOrderMarkerRenderers;
    private MaterialPropertyBlock _moveOrderMarkerPropertyBlock;
    private float _moveOrderMarkerHideTime = -1f;
    private GameObject _attackOrderMarker;
    private Renderer[] _attackOrderMarkerRenderers;
    private MaterialPropertyBlock _attackOrderMarkerPropertyBlock;
    private float _attackOrderMarkerHideTime = -1f;
    private GameObject _attackTargetRingMarker;
    private LineRenderer _attackTargetRingRenderer;
    private GameObject _attackTargetSelectionMarker;
    private Renderer[] _attackTargetSelectionMarkerRenderers;
    private Vector3 _attackTargetSelectionMarkerBaseRendererSize = Vector3.one;
    private readonly MaterialPropertyBlock _attackTargetSelectionMarkerPropertyBlock = new();
    private PremiumWorldSelectionBoundaryView _attackTargetSelectionBoundaryView;
    private GameObject _scanOrderMarker;
    private LineRenderer _scanOrderMarkerRenderer;
    private float _scanOrderMarkerHideTime = -1f;
    private GameObject _moveOrderMarkerPrefab;
    private GameObject _attackOrderMarkerPrefab;
    private GameObject _attackTargetMarkerPrefab;
    private TryResolveRuntimeBuildingInstanceDelegate _tryResolveRuntimeBuildingInstance;
    private float _orderMarkerVisibleSeconds = 1.25f;
    private Transform _runtimeRoot;
    private EntityQuery _attackTargetPreviewQuery;
    private readonly List<GameObject> _attackTargetPreviewMarkers = new();
    private readonly List<Renderer[]> _attackTargetPreviewMarkerRenderers = new();
    private readonly MaterialPropertyBlock _boardTargetPreviewPropertyBlock = new();
    private bool _attackTargetPreviewVisible;
    private int _attackTargetPreviewVisibleCount;
    private float _nextAttackTargetPreviewUpdateTime;
    private const int MaxAttackTargetPreviewMarkers = 64;
    private const float AttackTargetPreviewUpdateSeconds = 0.15f;
    private const float MoveOrderMarkerVerticalOffset = 0.18f;
    private const float MoveOrderMarkerHorizontalScale = 2.4f;
    private const float AttackOrderMarkerVerticalOffset = 0.45f;
    private const float AttackOrderMarkerHorizontalScale = 2.1f;
    private const float AttackTargetSelectionMarkerVerticalOffset = 0.12f;
    private const float AttackTargetSelectionMarkerScaleMultiplier = 1.25f;
    private const float AttackTargetRingMinimumRadius = 0.95f;
    private const float AttackTargetRingWidth = 0.55f;
    private const float AttackTargetMarkerMinimumVisibleSeconds = 14f;
    private const int AttackTargetRingSegments = 96;
    private const string SelectionObjectOutlineToken = "SelectionObjectOutline";
    private const string BaseColorProperty = "_BaseColor";
    private const string LegacyColorProperty = "_Color";
    private const string EmissionColorProperty = "_EmissionColor";
    private const string AccentColorProperty = "_AccentColor";
    private static readonly Color AttackTargetMarkerColor = new(1f, 0.08f, 0.04f, 0.95f);
    private static readonly Color AttackTargetMarkerEmissionColor = new(0.76f, 0.05f, 0.03f, 1f);
    private static readonly Color AttackTargetMarkerAccentColor = new(1f, 0.92f, 0.5f, 1f);
    private static readonly Color AttackPreviewMarkerColor = new(0.92f, 0.12f, 0.08f, 0.62f);
    private static readonly Color AttackPreviewMarkerEmissionColor = new(0.24f, 0.03f, 0.02f, 1f);
    private static readonly Color AttackPreviewMarkerAccentColor = new(1f, 0.64f, 0.42f, 1f);
    private static readonly Color BoardPreviewMarkerColor = new(0.2f, 1f, 0.78f, 0.68f);
    private static readonly Color BoardPreviewMarkerEmissionColor = new(0.04f, 0.34f, 0.25f, 1f);
    private static readonly Color BoardPreviewMarkerAccentColor = new(0.72f, 1f, 0.88f, 1f);
    private const int ScanMarkerSegments = 72;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _gridBlockerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>());
        _attackTargetPreviewQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<LocalTransform>());
    }

    public void Initialize(
        GameObject moveOrderMarkerPrefab,
        GameObject attackOrderMarkerPrefab,
        GameObject attackTargetMarkerPrefab,
        TryResolveRuntimeBuildingInstanceDelegate tryResolveRuntimeBuildingInstance,
        float orderMarkerVisibleSeconds,
        Transform runtimeRoot)
    {
        Dispose();
        _moveOrderMarkerPrefab = moveOrderMarkerPrefab;
        _attackOrderMarkerPrefab = attackOrderMarkerPrefab;
        _attackTargetMarkerPrefab = attackTargetMarkerPrefab;
        _tryResolveRuntimeBuildingInstance = tryResolveRuntimeBuildingInstance;
        _orderMarkerVisibleSeconds = Mathf.Max(0.01f, orderMarkerVisibleSeconds);
        _runtimeRoot = runtimeRoot;
        CacheMoveOrderMarker();
        CacheAttackOrderMarker();
    }

    public bool TryShowCommandResultMarker(
        EntityManager em,
        RtsSelectionCommandResultElement result,
        float attackVisibleSeconds = 6f)
    {
        if (result.Accepted == 0 || result.HasWorldPosition == 0)
            return false;

        switch (result.Kind)
        {
            case RtsSelectionCommandIntentKind.Move:
            case RtsSelectionCommandIntentKind.BoardTransport:
            case RtsSelectionCommandIntentKind.BoardSelectedTransport:
            case RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger:
                if (result.HasTargetCell == 0)
                    return false;
                ShowMoveOrderMarker(em, result.TargetCell, result.WorldPosition, result.MarkerFactionId);
                return true;

            case RtsSelectionCommandIntentKind.Attack:
                if (result.HasTargetEntity != 0)
                    ShowAttackOrderMarker(em, result.TargetEntity, result.WorldPosition, attackVisibleSeconds);
                else
                    ShowAttackOrderMarker(em, result.WorldPosition, attackVisibleSeconds);
                return true;

            case RtsSelectionCommandIntentKind.Scan:
                if (result.HasTargetCell == 0)
                    return false;
                ShowScanOrderMarker(em, result.TargetCell, result.WorldPosition, result.RadiusCells);
                return true;

            default:
                return false;
        }
    }

    public void Dispose()
    {
        if (_moveOrderMarker != null)
            UnityEngine.Object.Destroy(_moveOrderMarker);
        if (_attackOrderMarker != null)
            UnityEngine.Object.Destroy(_attackOrderMarker);
        if (_attackTargetRingMarker != null)
            UnityEngine.Object.Destroy(_attackTargetRingMarker);
        if (_attackTargetSelectionMarker != null)
            UnityEngine.Object.Destroy(_attackTargetSelectionMarker);
        if (_scanOrderMarker != null)
            UnityEngine.Object.Destroy(_scanOrderMarker);
        for (int i = 0; i < _attackTargetPreviewMarkers.Count; i++)
        {
            if (_attackTargetPreviewMarkers[i] != null)
                UnityEngine.Object.Destroy(_attackTargetPreviewMarkers[i]);
        }

        _moveOrderMarker = null;
        _moveOrderMarkerRenderers = null;
        _attackOrderMarker = null;
        _attackOrderMarkerRenderers = null;
        _attackTargetRingMarker = null;
        _attackTargetRingRenderer = null;
        _attackTargetSelectionMarker = null;
        _attackTargetSelectionMarkerRenderers = null;
        _attackTargetSelectionMarkerBaseRendererSize = Vector3.one;
        _attackTargetSelectionBoundaryView = null;
        _scanOrderMarker = null;
        _scanOrderMarkerRenderer = null;
        _attackTargetPreviewMarkers.Clear();
        _attackTargetPreviewMarkerRenderers.Clear();
        _attackTargetPreviewVisible = false;
        _attackTargetPreviewVisibleCount = 0;
        _nextAttackTargetPreviewUpdateTime = 0f;
        _moveOrderMarkerHideTime = -1f;
        _attackOrderMarkerHideTime = -1f;
        _scanOrderMarkerHideTime = -1f;
    }

    public void UpdateMoveOrderMarkerVisibility(System.Action<bool> setHudWorldMarkersVisible)
    {
        if (_moveOrderMarker == null || _moveOrderMarkerHideTime < 0f)
            return;

        if (Time.time < _moveOrderMarkerHideTime)
            return;

        _moveOrderMarker.SetActive(false);
        _moveOrderMarkerHideTime = -1f;
        if (_attackOrderMarkerHideTime < 0f)
            setHudWorldMarkersVisible?.Invoke(false);
    }

    public void UpdateAttackOrderMarkerVisibility(System.Action<bool> setHudWorldMarkersVisible)
    {
        if ((_attackOrderMarker == null && _attackTargetRingMarker == null && _attackTargetSelectionMarker == null) || _attackOrderMarkerHideTime < 0f)
            return;

        if (Time.time < _attackOrderMarkerHideTime)
            return;

        if (_attackOrderMarker != null)
            _attackOrderMarker.SetActive(false);
        if (_attackTargetRingMarker != null)
            _attackTargetRingMarker.SetActive(false);
        if (_attackTargetSelectionMarker != null)
            _attackTargetSelectionMarker.SetActive(false);
        _attackOrderMarkerHideTime = -1f;
        if (_moveOrderMarkerHideTime < 0f && _scanOrderMarkerHideTime < 0f)
            setHudWorldMarkersVisible?.Invoke(false);
    }

    public void UpdateScanOrderMarkerVisibility(System.Action<bool> setHudWorldMarkersVisible)
    {
        if (_scanOrderMarker == null || _scanOrderMarkerHideTime < 0f)
            return;

        if (Time.time < _scanOrderMarkerHideTime)
            return;

        _scanOrderMarker.SetActive(false);
        _scanOrderMarkerHideTime = -1f;
        if (_moveOrderMarkerHideTime < 0f && _attackOrderMarkerHideTime < 0f)
            setHudWorldMarkersVisible?.Invoke(false);
    }

    public void ShowMoveOrderMarker(EntityManager em, int2 cell, Vector3 worldPoint, byte factionId)
    {
        if (_moveOrderMarker == null || _moveOrderMarkerRenderers == null || _moveOrderMarkerRenderers.Length == 0)
            return;

        EnsureEntityQueries(em);
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
            return;

        int cellIndex = GridUtils.CellToIndex(cell, grid.Width);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        bool blocked = walkable[cellIndex].Value == 0 || (blockerData.Blocked.IsCreated && blockerData.Blocked.IsSet(cellIndex));

        if (blocked)
        {
            _moveOrderMarker.SetActive(false);
            _moveOrderMarkerHideTime = -1f;
            return;
        }

        float markerSurfaceY = ResolveCommandMarkerSurfaceY(grid, worldPoint);
        Vector3 worldPosition = worldPoint;
        worldPosition.y = markerSurfaceY + MoveOrderMarkerVerticalOffset;

        _moveOrderMarker.transform.position = worldPosition;
        _moveOrderMarker.transform.rotation = Quaternion.identity;
        _moveOrderMarker.transform.localScale = new Vector3(
            MoveOrderMarkerHorizontalScale,
            1f,
            MoveOrderMarkerHorizontalScale);
        _moveOrderMarker.SetActive(true);
        LiftMarkerRendererBoundsAbove(_moveOrderMarker, _moveOrderMarkerRenderers, markerSurfaceY + MoveOrderMarkerVerticalOffset);

        for (int i = 0; i < _moveOrderMarkerRenderers.Length; i++)
        {
            Renderer renderer = _moveOrderMarkerRenderers[i];
            if (renderer == null)
                continue;

            _moveOrderMarkerPropertyBlock.Clear();
            renderer.SetPropertyBlock(_moveOrderMarkerPropertyBlock);
        }

        _moveOrderMarkerHideTime = Time.time + _orderMarkerVisibleSeconds;
    }

    public void ShowAttackOrderMarker(EntityManager em, Vector3 worldPoint, float visibleSeconds = -1f)
    {
        ShowAttackOrderMarker(em, Entity.Null, worldPoint, visibleSeconds);
    }

    public void ShowAttackOrderMarker(EntityManager em, Entity targetEntity, Vector3 worldPoint, float visibleSeconds = -1f)
    {
        bool hasPrefabMarker = _attackOrderMarker != null &&
                               _attackOrderMarkerRenderers != null &&
                               _attackOrderMarkerRenderers.Length > 0;
        if (targetEntity == Entity.Null && !hasPrefabMarker)
            return;

        EnsureEntityQueries(em);
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);

        Vector3 worldPosition = ResolveAttackMarkerWorldPosition(em, targetEntity, grid, worldPoint);
        if (targetEntity != Entity.Null)
        {
            if (!ShowAttackTargetSelectionMarker(em, targetEntity, grid, worldPosition, visibleSeconds))
                ShowAttackTargetRingMarker(em, targetEntity, grid, worldPosition, visibleSeconds);
            if (_attackOrderMarker != null)
                _attackOrderMarker.SetActive(false);
            return;
        }

        if (!hasPrefabMarker)
            return;

        float markerSurfaceY = ResolveCommandMarkerSurfaceY(grid, worldPoint);
        worldPosition.y = markerSurfaceY + AttackOrderMarkerVerticalOffset;

        _attackOrderMarker.transform.position = worldPosition;
        _attackOrderMarker.transform.rotation = Quaternion.identity;
        _attackOrderMarker.transform.localScale = new Vector3(
            AttackOrderMarkerHorizontalScale,
            1f,
            AttackOrderMarkerHorizontalScale);
        _attackOrderMarker.SetActive(true);
        LiftMarkerRendererBoundsAbove(_attackOrderMarker, _attackOrderMarkerRenderers, markerSurfaceY + AttackOrderMarkerVerticalOffset);

        for (int i = 0; i < _attackOrderMarkerRenderers.Length; i++)
        {
            Renderer renderer = _attackOrderMarkerRenderers[i];
            if (renderer == null)
                continue;

            _attackOrderMarkerPropertyBlock.Clear();
            renderer.SetPropertyBlock(_attackOrderMarkerPropertyBlock);
        }

        _attackOrderMarkerHideTime = Time.time + (visibleSeconds > 0f ? visibleSeconds : _orderMarkerVisibleSeconds);
    }

    private bool ShowAttackTargetSelectionMarker(
        EntityManager em,
        Entity targetEntity,
        GridConfig grid,
        Vector3 worldPosition,
        float visibleSeconds)
    {
        EnsureAttackTargetSelectionMarker();
        if (_attackTargetSelectionMarker == null ||
            _attackTargetSelectionMarkerRenderers == null ||
            _attackTargetSelectionMarkerRenderers.Length == 0)
            return false;

        Vector2 markerSize = ResolveAttackMarkerWorldSize(em, targetEntity, grid);
        Vector2 boundarySize = markerSize;
        Vector3 markerPosition = worldPosition;
        Quaternion markerRotation = ResolveAttackMarkerRotation(em, targetEntity);
        float markerSurfaceY;
        float targetHeight = ResolveAttackMarkerWorldHeight(em, targetEntity);
        bool usedRuntimeBounds = TryResolveRuntimeBuildingMarkerPlacement(
            em,
            targetEntity,
            grid,
            out Vector3 runtimePosition,
            out Quaternion runtimeRotation,
            out Bounds runtimeBounds);
        if (usedRuntimeBounds)
        {
            markerPosition = runtimePosition;
            markerRotation = runtimeRotation;
            markerSurfaceY = runtimePosition.y - AttackTargetSelectionMarkerVerticalOffset;
            boundarySize = new Vector2(
                Mathf.Max(markerSize.x, runtimeBounds.size.x),
                Mathf.Max(markerSize.y, runtimeBounds.size.z));
            targetHeight = Mathf.Max(targetHeight, runtimeBounds.size.y);
        }
        else
        {
            markerSurfaceY = Mathf.Max(grid.Origin.y, worldPosition.y);
            markerPosition.y = markerSurfaceY + AttackTargetSelectionMarkerVerticalOffset;
        }

        Transform markerTransform = _attackTargetSelectionMarker.transform;
        markerTransform.SetPositionAndRotation(markerPosition, markerRotation);
        markerTransform.localScale = ResolveAttackTargetSelectionMarkerScale(markerSize);
        _attackTargetSelectionMarker.SetActive(true);
        LiftMarkerRendererBoundsAbove(
            _attackTargetSelectionMarker,
            _attackTargetSelectionMarkerRenderers,
            markerSurfaceY + AttackTargetSelectionMarkerVerticalOffset);
        ConfigureAttackTargetBoundaryView(markerPosition, markerRotation, boundarySize, markerSurfaceY, targetHeight);
        if (_attackTargetRingMarker != null)
            _attackTargetRingMarker.SetActive(false);

        float duration = visibleSeconds > 0f ? visibleSeconds : _orderMarkerVisibleSeconds;
        _attackOrderMarkerHideTime = Time.time + Mathf.Max(duration, AttackTargetMarkerMinimumVisibleSeconds);

        return true;
    }

    private void ShowAttackTargetRingMarker(
        EntityManager em,
        Entity targetEntity,
        GridConfig grid,
        Vector3 worldPosition,
        float visibleSeconds)
    {
        EnsureAttackTargetRingMarker();
        if (_attackTargetRingMarker == null || _attackTargetRingRenderer == null)
            return;

        Vector2 markerSize = ResolveAttackMarkerWorldSize(em, targetEntity, grid);
        float radiusX = Mathf.Max(AttackTargetRingMinimumRadius, markerSize.x * AttackTargetSelectionMarkerScaleMultiplier * 0.5f);
        float radiusZ = Mathf.Max(AttackTargetRingMinimumRadius, markerSize.y * AttackTargetSelectionMarkerScaleMultiplier * 0.5f);
        float markerY = Mathf.Max(grid.Origin.y + AttackTargetSelectionMarkerVerticalOffset, worldPosition.y + AttackTargetSelectionMarkerVerticalOffset);
        _attackTargetRingMarker.transform.position = Vector3.zero;
        _attackTargetRingMarker.transform.rotation = Quaternion.identity;
        _attackTargetRingRenderer.positionCount = AttackTargetRingSegments;
        for (int i = 0; i < AttackTargetRingSegments; i++)
        {
            float t = (i / (float)AttackTargetRingSegments) * Mathf.PI * 2f;
            _attackTargetRingRenderer.SetPosition(
                i,
                new Vector3(
                    worldPosition.x + Mathf.Cos(t) * radiusX,
                    markerY,
                    worldPosition.z + Mathf.Sin(t) * radiusZ));
        }

        _attackTargetRingMarker.SetActive(true);
        float duration = visibleSeconds > 0f ? visibleSeconds : _orderMarkerVisibleSeconds;
        _attackOrderMarkerHideTime = Time.time + Mathf.Max(duration, AttackTargetMarkerMinimumVisibleSeconds);
    }

    private static Vector3 ResolveAttackMarkerWorldPosition(EntityManager em, Entity targetEntity, GridConfig grid, Vector3 fallbackWorldPoint)
    {
        if (targetEntity == Entity.Null || !em.Exists(targetEntity))
            return fallbackWorldPoint;

        if (em.HasComponent<RuntimeBuildingCombatInfo>(targetEntity))
        {
            RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(targetEntity);
            int2 footprint = UnitFootprintUtility.ClampSize(info.FootprintCells);
            float3 minWorld = grid.Origin + new float3(info.OriginCell.x * grid.CellSize, 0f, info.OriginCell.y * grid.CellSize);
            return new Vector3(
                minWorld.x + footprint.x * grid.CellSize * 0.5f,
                fallbackWorldPoint.y,
                minWorld.z + footprint.y * grid.CellSize * 0.5f);
        }

        if (em.HasComponent<LocalTransform>(targetEntity))
        {
            float3 position = em.GetComponentData<LocalTransform>(targetEntity).Position;
            return new Vector3(position.x, position.y, position.z);
        }

        return fallbackWorldPoint;
    }

    private static Vector2 ResolveAttackMarkerWorldSize(EntityManager em, Entity targetEntity, GridConfig grid)
    {
        if (targetEntity == Entity.Null || !em.Exists(targetEntity))
            return Vector2.one * math.max(0.01f, grid.CellSize);

        int2 footprint = default;
        if (em.HasComponent<RuntimeBuildingCombatInfo>(targetEntity))
            footprint = em.GetComponentData<RuntimeBuildingCombatInfo>(targetEntity).FootprintCells;
        else if (em.HasComponent<UnitFootprint>(targetEntity))
            footprint = em.GetComponentData<UnitFootprint>(targetEntity).Size;

        footprint = UnitFootprintUtility.ClampSize(footprint);
        float cellSize = math.max(0.01f, grid.CellSize);
        return new Vector2(math.max(1, footprint.x) * cellSize, math.max(1, footprint.y) * cellSize);
    }

    public void ShowScanOrderMarker(EntityManager em, int2 cell, float3 worldPoint, int radiusCells, float visibleSeconds = -1f)
    {
        EnsureEntityQueries(em);
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
            return;

        EnsureScanOrderMarker();
        if (_scanOrderMarker == null || _scanOrderMarkerRenderer == null)
            return;

        float radius = Mathf.Max(grid.CellSize, radiusCells * grid.CellSize);
        Vector3 center = new(worldPoint.x, grid.Origin.y + 0.08f, worldPoint.z);
        _scanOrderMarker.transform.position = Vector3.zero;
        _scanOrderMarker.transform.rotation = Quaternion.identity;
        _scanOrderMarkerRenderer.positionCount = ScanMarkerSegments;
        for (int i = 0; i < ScanMarkerSegments; i++)
        {
            float t = (i / (float)ScanMarkerSegments) * Mathf.PI * 2f;
            _scanOrderMarkerRenderer.SetPosition(
                i,
                center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius));
        }

        _scanOrderMarker.SetActive(true);
        _scanOrderMarkerHideTime = Time.time + (visibleSeconds > 0f ? visibleSeconds : _orderMarkerVisibleSeconds);
    }

    public void UpdateAttackTargetPreviewMarkers(EntityManager em, bool visible)
    {
        if (!visible || _attackOrderMarkerPrefab == null)
        {
            HideAttackTargetPreviewMarkersIfNeeded(0);
            _attackTargetPreviewVisible = false;
            _attackTargetPreviewVisibleCount = 0;
            return;
        }

        if (_attackTargetPreviewVisible && Time.unscaledTime < _nextAttackTargetPreviewUpdateTime)
            return;

        _attackTargetPreviewVisible = true;
        _nextAttackTargetPreviewUpdateTime = Time.unscaledTime + AttackTargetPreviewUpdateSeconds;

        EnsureEntityQueries(em);
        if (_attackTargetPreviewQuery.IsEmptyIgnoreFilter)
        {
            HideAttackTargetPreviewMarkersIfNeeded(0);
            _attackTargetPreviewVisibleCount = 0;
            return;
        }

        bool hasGroundY = TryGetMarkerGroundY(em, out float groundY);
        int markerIndex = UpdateTargetPreviewMarkersFromChunks(
            em,
            hasGroundY,
            groundY,
            boardPreview: false,
            Entity.Null,
            null);

        HideAttackTargetPreviewMarkersIfNeeded(markerIndex);
        _attackTargetPreviewVisibleCount = markerIndex;
    }

    public void UpdateBoardTargetPreviewMarkers(
        EntityManager em,
        bool visible,
        Entity source,
        IsPreviewTargetValidWithSourceDelegate isValidTarget)
    {
        if (!visible || _attackOrderMarkerPrefab == null || isValidTarget == null)
        {
            HideAttackTargetPreviewMarkersIfNeeded(0);
            _attackTargetPreviewVisible = false;
            _attackTargetPreviewVisibleCount = 0;
            return;
        }

        if (_attackTargetPreviewVisible && Time.unscaledTime < _nextAttackTargetPreviewUpdateTime)
            return;

        _attackTargetPreviewVisible = true;
        _nextAttackTargetPreviewUpdateTime = Time.unscaledTime + AttackTargetPreviewUpdateSeconds;

        EnsureEntityQueries(em);
        if (_attackTargetPreviewQuery.IsEmptyIgnoreFilter)
        {
            HideAttackTargetPreviewMarkersIfNeeded(0);
            _attackTargetPreviewVisibleCount = 0;
            return;
        }

        bool hasGroundY = TryGetMarkerGroundY(em, out float groundY);
        int markerIndex = UpdateTargetPreviewMarkersFromChunks(
            em,
            hasGroundY,
            groundY,
            boardPreview: true,
            source,
            isValidTarget);

        HideAttackTargetPreviewMarkersIfNeeded(markerIndex);
        _attackTargetPreviewVisibleCount = markerIndex;
    }

    private int UpdateTargetPreviewMarkersFromChunks(
        EntityManager em,
        bool hasGroundY,
        float groundY,
        bool boardPreview,
        Entity source,
        IsPreviewTargetValidWithSourceDelegate isValidTarget)
    {
        int markerIndex = 0;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<Faction> factionType = em.GetComponentTypeHandle<Faction>(true);
        ComponentTypeHandle<LocalTransform> transformType = em.GetComponentTypeHandle<LocalTransform>(true);
        ComponentTypeHandle<UnitHealth> healthType = em.GetComponentTypeHandle<UnitHealth>(true);
        using NativeArray<ArchetypeChunk> chunks = _attackTargetPreviewQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length && markerIndex < MaxAttackTargetPreviewMarkers; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
            NativeArray<LocalTransform> transforms = chunk.GetNativeArray(ref transformType);
            bool hasHealth = chunk.Has(ref healthType);
            NativeArray<UnitHealth> healths = hasHealth
                ? chunk.GetNativeArray(ref healthType)
                : default;

            for (int i = 0; i < chunk.Count && markerIndex < MaxAttackTargetPreviewMarkers; i++)
            {
                Entity target = entities[i];
                UnitHealth health = hasHealth ? healths[i] : default;
                if (boardPreview)
                {
                    if (!IsValidBoardPreviewTarget(em, source, target, factions[i], hasHealth, health, isValidTarget))
                        continue;
                }
                else if (!IsValidAttackPreviewTarget(factions[i], hasHealth, health))
                {
                    continue;
                }

                GameObject marker = EnsureAttackTargetPreviewMarker(markerIndex);
                if (marker == null)
                    continue;

                float3 position = transforms[i].Position;
                marker.transform.position = new Vector3(
                    position.x,
                    hasGroundY ? groundY : position.y + MoveOrderMarkerVerticalOffset,
                    position.z);
                marker.transform.rotation = Quaternion.identity;
                if (boardPreview)
                    ApplyBoardTargetPreviewColor(markerIndex);
                else
                    ApplyAttackTargetPreviewColor(markerIndex);
                SetMarkerActive(marker, true);
                if ((uint)markerIndex < (uint)_attackTargetPreviewMarkerRenderers.Count)
                    LiftMarkerRendererBoundsAbove(marker, _attackTargetPreviewMarkerRenderers[markerIndex], marker.transform.position.y);
                markerIndex++;
            }
        }

        return markerIndex;
    }

    private void CacheMoveOrderMarker()
    {
        _moveOrderMarkerPropertyBlock = new MaterialPropertyBlock();

        if (_moveOrderMarkerPrefab != null)
        {
            UnityEngine.Object markerInstance = UnityEngine.Object.Instantiate((UnityEngine.Object)_moveOrderMarkerPrefab);
            _moveOrderMarker = markerInstance as GameObject;
            if (_moveOrderMarker == null)
                return;
            _moveOrderMarker.name = "MoveOrderMarkerRuntime";
            if (_runtimeRoot != null)
                _moveOrderMarker.transform.SetParent(_runtimeRoot, false);
            _moveOrderMarkerRenderers = _moveOrderMarker.GetComponentsInChildren<Renderer>(true);
            ConfigureMarkerRenderers(_moveOrderMarkerRenderers);
            _moveOrderMarker.SetActive(false);
            return;
        }

        _moveOrderMarker = null;
        _moveOrderMarkerRenderers = null;
    }

    private void CacheAttackOrderMarker()
    {
        _attackOrderMarkerPropertyBlock = new MaterialPropertyBlock();

        if (_attackOrderMarkerPrefab != null)
        {
            UnityEngine.Object markerInstance = UnityEngine.Object.Instantiate((UnityEngine.Object)_attackOrderMarkerPrefab);
            _attackOrderMarker = markerInstance as GameObject;
            if (_attackOrderMarker == null)
                return;
            _attackOrderMarker.name = "AttackOrderMarkerRuntime";
            if (_runtimeRoot != null)
                _attackOrderMarker.transform.SetParent(_runtimeRoot, false);
            _attackOrderMarkerRenderers = _attackOrderMarker.GetComponentsInChildren<Renderer>(true);
            ConfigureMarkerRenderers(_attackOrderMarkerRenderers);
            _attackOrderMarker.SetActive(false);
            return;
        }

        _attackOrderMarker = null;
        _attackOrderMarkerRenderers = null;
    }

    private GameObject EnsureAttackTargetPreviewMarker(int index)
    {
        while (_attackTargetPreviewMarkers.Count <= index)
        {
            UnityEngine.Object markerInstance = UnityEngine.Object.Instantiate((UnityEngine.Object)_attackOrderMarkerPrefab);
            GameObject marker = markerInstance as GameObject;
            if (marker == null)
                return null;

            marker.name = "AttackTargetPreviewMarkerRuntime";
            if (_runtimeRoot != null)
                marker.transform.SetParent(_runtimeRoot, false);
            Renderer[] renderers = marker.GetComponentsInChildren<Renderer>(true);
            ConfigureMarkerRenderers(renderers);
            marker.SetActive(false);
            _attackTargetPreviewMarkers.Add(marker);
            _attackTargetPreviewMarkerRenderers.Add(renderers);
        }

        return _attackTargetPreviewMarkers[index];
    }

    private void EnsureScanOrderMarker()
    {
        if (_scanOrderMarker != null && _scanOrderMarkerRenderer != null)
            return;

        _scanOrderMarker = new GameObject("ScanOrderMarkerRuntime");
        if (_runtimeRoot != null)
            _scanOrderMarker.transform.SetParent(_runtimeRoot, false);

        _scanOrderMarkerRenderer = _scanOrderMarker.AddComponent<LineRenderer>();
        _scanOrderMarkerRenderer.useWorldSpace = true;
        _scanOrderMarkerRenderer.loop = true;
        _scanOrderMarkerRenderer.positionCount = ScanMarkerSegments;
        _scanOrderMarkerRenderer.widthMultiplier = 0.18f;
        _scanOrderMarkerRenderer.numCornerVertices = 4;
        _scanOrderMarkerRenderer.numCapVertices = 4;
        _scanOrderMarkerRenderer.alignment = LineAlignment.View;
        _scanOrderMarkerRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _scanOrderMarkerRenderer.receiveShadows = false;
        _scanOrderMarkerRenderer.lightProbeUsage = LightProbeUsage.Off;
        _scanOrderMarkerRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _scanOrderMarkerRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            _scanOrderMarkerRenderer.material = new Material(shader);
        _scanOrderMarkerRenderer.startColor = new Color(0.25f, 1f, 0.85f, 0.95f);
        _scanOrderMarkerRenderer.endColor = new Color(0.12f, 0.65f, 1f, 0.95f);
        _scanOrderMarker.SetActive(false);
    }

    private void EnsureAttackTargetSelectionMarker()
    {
        if (_attackTargetSelectionMarker != null &&
            _attackTargetSelectionMarkerRenderers != null &&
            _attackTargetSelectionMarkerRenderers.Length > 0)
        {
            return;
        }

        if (_attackTargetMarkerPrefab == null)
            return;

        _attackTargetSelectionMarker = UnityEngine.Object.Instantiate(_attackTargetMarkerPrefab, _runtimeRoot);
        _attackTargetSelectionMarker.name = "AttackTargetSelectionMarkerRuntime";
        _attackTargetSelectionMarkerRenderers = _attackTargetSelectionMarker.GetComponentsInChildren<Renderer>(true);
        _attackTargetSelectionMarkerBaseRendererSize = CalculateRendererSize(_attackTargetSelectionMarkerRenderers);
        ConfigureMarkerRenderers(_attackTargetSelectionMarkerRenderers);
        ApplyAttackTargetSelectionMarkerColor();
        _attackTargetSelectionBoundaryView = _attackTargetSelectionMarker.GetComponent<PremiumWorldSelectionBoundaryView>();
        if (_attackTargetSelectionBoundaryView == null)
            _attackTargetSelectionBoundaryView = _attackTargetSelectionMarker.AddComponent<PremiumWorldSelectionBoundaryView>();
        _attackTargetSelectionMarker.SetActive(false);
    }

    private void ConfigureAttackTargetBoundaryView(
        Vector3 markerPosition,
        Quaternion markerRotation,
        Vector2 markerSize,
        float surfaceY,
        float targetHeight)
    {
        if (_attackTargetSelectionBoundaryView == null)
            return;

        Color accent = Color.Lerp(AttackTargetMarkerAccentColor, Color.white, 0.25f);
        _attackTargetSelectionBoundaryView.Configure(
            markerPosition,
            markerRotation,
            markerSize,
            surfaceY,
            Mathf.Max(0.9f, targetHeight),
            AttackTargetMarkerColor,
            accent);
    }

    private Vector3 ResolveAttackTargetSelectionMarkerScale(Vector2 markerSize)
    {
        float baseX = Mathf.Max(0.001f, _attackTargetSelectionMarkerBaseRendererSize.x);
        float baseZ = Mathf.Max(0.001f, _attackTargetSelectionMarkerBaseRendererSize.z);
        return new Vector3(
            Mathf.Max(0.01f, markerSize.x * AttackTargetSelectionMarkerScaleMultiplier) / baseX,
            1f,
            Mathf.Max(0.01f, markerSize.y * AttackTargetSelectionMarkerScaleMultiplier) / baseZ);
    }

    private void ApplyAttackTargetSelectionMarkerColor()
    {
        if (_attackTargetSelectionMarkerRenderers == null)
            return;

        for (int i = 0; i < _attackTargetSelectionMarkerRenderers.Length; i++)
        {
            Renderer renderer = _attackTargetSelectionMarkerRenderers[i];
            if (renderer == null)
                continue;

            _attackTargetSelectionMarkerPropertyBlock.Clear();
            renderer.GetPropertyBlock(_attackTargetSelectionMarkerPropertyBlock);
            SetHologramMarkerColors(
                _attackTargetSelectionMarkerPropertyBlock,
                AttackTargetMarkerColor,
                AttackTargetMarkerEmissionColor,
                AttackTargetMarkerAccentColor);
            renderer.SetPropertyBlock(_attackTargetSelectionMarkerPropertyBlock);
        }
    }

    private bool TryResolveRuntimeBuildingMarkerPlacement(
        EntityManager em,
        Entity targetEntity,
        GridConfig grid,
        out Vector3 position,
        out Quaternion rotation,
        out Bounds bounds)
    {
        position = default;
        rotation = Quaternion.identity;
        bounds = default;

        if (targetEntity == Entity.Null ||
            !em.Exists(targetEntity) ||
            !em.HasComponent<RuntimeBuildingCombatInfo>(targetEntity) ||
            _tryResolveRuntimeBuildingInstance == null)
        {
            return false;
        }

        RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(targetEntity);
        if (info.RuntimeBuildingId <= 0 ||
            !_tryResolveRuntimeBuildingInstance(targetEntity, info.RuntimeBuildingId, out GameObject instance) ||
            instance == null ||
            !TryCalculateRendererBounds(instance, out bounds))
            return false;

        float surfaceY = Mathf.Max(grid.Origin.y, bounds.min.y);
        if (bounds.min.y < grid.Origin.y - 0.001f)
            surfaceY = Mathf.Max(surfaceY, instance.transform.position.y);
        position = bounds.center;
        position.y = surfaceY + AttackTargetSelectionMarkerVerticalOffset;
        rotation = Quaternion.Euler(0f, instance.transform.eulerAngles.y, 0f);
        return true;
    }

    private static float ResolveAttackMarkerWorldHeight(EntityManager em, Entity targetEntity)
    {
        if (targetEntity == Entity.Null || !em.Exists(targetEntity))
            return 1.2f;

        if (em.HasComponent<RuntimeBuildingCombatInfo>(targetEntity))
            return 3.4f;

        if (em.HasComponent<UnitMovementBehavior>(targetEntity) &&
            em.GetComponentData<UnitMovementBehavior>(targetEntity).UsesVehicleMotion != 0)
        {
            return 1.65f;
        }

        return 1.25f;
    }

    private static bool TryCalculateRendererBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        if (instance == null)
            return false;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(false);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || IsSelectionObjectOutlineRenderer(renderer))
                continue;

            if (hasBounds)
                bounds.Encapsulate(renderer.bounds);
            else
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
        }

        return hasBounds;
    }

    private static bool IsSelectionObjectOutlineRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;

        Transform current = renderer.transform;
        while (current != null)
        {
            if (current.name.IndexOf(SelectionObjectOutlineToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            current = current.parent;
        }

        return false;
    }

    private static Quaternion ResolveAttackMarkerRotation(EntityManager em, Entity targetEntity)
    {
        if (targetEntity == Entity.Null || !em.Exists(targetEntity) || !em.HasComponent<LocalTransform>(targetEntity))
            return Quaternion.identity;

        Quaternion rotation = em.GetComponentData<LocalTransform>(targetEntity).Rotation;
        return Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
    }

    private void EnsureAttackTargetRingMarker()
    {
        if (_attackTargetRingMarker != null && _attackTargetRingRenderer != null)
            return;

        _attackTargetRingMarker = new GameObject("AttackTargetRingMarkerRuntime");
        if (_runtimeRoot != null)
            _attackTargetRingMarker.transform.SetParent(_runtimeRoot, false);

        _attackTargetRingRenderer = _attackTargetRingMarker.AddComponent<LineRenderer>();
        _attackTargetRingRenderer.useWorldSpace = true;
        _attackTargetRingRenderer.loop = true;
        _attackTargetRingRenderer.widthMultiplier = AttackTargetRingWidth;
        _attackTargetRingRenderer.numCornerVertices = 4;
        _attackTargetRingRenderer.numCapVertices = 4;
        _attackTargetRingRenderer.alignment = LineAlignment.View;
        _attackTargetRingRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _attackTargetRingRenderer.receiveShadows = false;
        _attackTargetRingRenderer.lightProbeUsage = LightProbeUsage.Off;
        _attackTargetRingRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _attackTargetRingRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        Material ringMaterial = CreateAttackTargetRingMaterial();
        if (ringMaterial != null)
            _attackTargetRingRenderer.material = ringMaterial;
        _attackTargetRingRenderer.startColor = AttackTargetMarkerColor;
        _attackTargetRingRenderer.endColor = AttackTargetMarkerColor;
        _attackTargetRingMarker.SetActive(false);
    }

    private static Material CreateAttackTargetRingMaterial()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        var material = new Material(shader)
        {
            name = "AttackTargetRingOverlayMaterial",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Overlay
        };
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_Cull", (int)CullMode.Off);
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)CompareFunction.Always);
        material.SetColor(LegacyColorProperty, AttackTargetMarkerColor);
        material.SetColor(BaseColorProperty, AttackTargetMarkerColor);
        material.SetColor(EmissionColorProperty, AttackTargetMarkerEmissionColor);
        material.SetColor(AccentColorProperty, AttackTargetMarkerAccentColor);
        return material;
    }

    private static Vector3 CalculateRendererSize(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
            return Vector3.one;

        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (hasBounds)
                bounds.Encapsulate(renderer.bounds);
            else
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
        }

        return hasBounds ? bounds.size : Vector3.one;
    }

    private static void LiftMarkerRendererBoundsAbove(GameObject marker, Renderer[] renderers, float minimumWorldY)
    {
        if (marker == null || !TryCalculateRendererBounds(renderers, out Bounds markerBounds))
            return;

        float lift = minimumWorldY - markerBounds.min.y;
        if (lift <= 0.001f)
            return;

        marker.transform.position += Vector3.up * lift;
    }

    private static bool TryCalculateRendererBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        if (renderers == null)
            return false;

        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (hasBounds)
                bounds.Encapsulate(renderer.bounds);
            else
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
        }

        return hasBounds;
    }

    private void HideAttackTargetPreviewMarkersIfNeeded(int startIndex)
    {
        int end = Mathf.Min(_attackTargetPreviewVisibleCount, _attackTargetPreviewMarkers.Count);
        for (int i = startIndex; i < end; i++)
        {
            GameObject marker = _attackTargetPreviewMarkers[i];
            SetMarkerActive(marker, false);
        }
    }

    private static void ConfigureMarkerRenderers(Renderer[] renderers)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    private void ApplyBoardTargetPreviewColor(int markerIndex)
    {
        if ((uint)markerIndex >= (uint)_attackTargetPreviewMarkerRenderers.Count)
            return;

        Renderer[] renderers = _attackTargetPreviewMarkerRenderers[markerIndex];
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            _boardTargetPreviewPropertyBlock.Clear();
            renderer.GetPropertyBlock(_boardTargetPreviewPropertyBlock);
            SetHologramMarkerColors(
                _boardTargetPreviewPropertyBlock,
                BoardPreviewMarkerColor,
                BoardPreviewMarkerEmissionColor,
                BoardPreviewMarkerAccentColor);
            renderer.SetPropertyBlock(_boardTargetPreviewPropertyBlock);
        }
    }

    private void ApplyAttackTargetPreviewColor(int markerIndex)
    {
        if ((uint)markerIndex >= (uint)_attackTargetPreviewMarkerRenderers.Count)
            return;

        Renderer[] renderers = _attackTargetPreviewMarkerRenderers[markerIndex];
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            _boardTargetPreviewPropertyBlock.Clear();
            renderer.GetPropertyBlock(_boardTargetPreviewPropertyBlock);
            SetHologramMarkerColors(
                _boardTargetPreviewPropertyBlock,
                AttackPreviewMarkerColor,
                AttackPreviewMarkerEmissionColor,
                AttackPreviewMarkerAccentColor);
            renderer.SetPropertyBlock(_boardTargetPreviewPropertyBlock);
        }
    }

    private static void SetHologramMarkerColors(
        MaterialPropertyBlock propertyBlock,
        Color baseColor,
        Color emissionColor,
        Color accentColor)
    {
        propertyBlock.SetColor(BaseColorProperty, baseColor);
        propertyBlock.SetColor(LegacyColorProperty, baseColor);
        propertyBlock.SetColor(EmissionColorProperty, emissionColor);
        propertyBlock.SetColor(AccentColorProperty, accentColor);
    }

    private static void SetMarkerActive(GameObject marker, bool active)
    {
        if (marker != null && marker.activeSelf != active)
            marker.SetActive(active);
    }

    private bool TryGetMarkerGroundY(EntityManager em, out float y)
    {
        y = MoveOrderMarkerVerticalOffset;
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        y = grid.Origin.y + MoveOrderMarkerVerticalOffset;
        return true;
    }

    private static float ResolveCommandMarkerSurfaceY(GridConfig grid, Vector3 worldPoint)
    {
        return Mathf.Max(grid.Origin.y, worldPoint.y);
    }

    private static bool IsValidAttackPreviewTarget(Faction faction, bool hasHealth, UnitHealth health)
    {
        if (!FactionIdentitySystem.IsHostileToPlayer(faction.Id))
            return false;

        return !hasHealth || health.Current > 0;
    }

    private static bool IsValidBoardPreviewTarget(
        EntityManager em,
        Entity source,
        Entity target,
        Faction faction,
        bool hasHealth,
        UnitHealth health,
        IsPreviewTargetValidWithSourceDelegate isValidTarget)
    {
        if (!FactionIdentitySystem.IsPlayerControlled(faction.Id))
            return false;

        if (hasHealth && health.Current <= 0)
            return false;

        return isValidTarget(em, source, target);
    }
}
