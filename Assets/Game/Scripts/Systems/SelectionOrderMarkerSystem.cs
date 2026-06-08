using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class SelectionOrderMarkerSystem
{
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
    private GameObject _scanOrderMarker;
    private LineRenderer _scanOrderMarkerRenderer;
    private float _scanOrderMarkerHideTime = -1f;
    private GameObject _moveOrderMarkerPrefab;
    private GameObject _attackOrderMarkerPrefab;
    private float _orderMarkerVisibleSeconds = 1.25f;
    private Transform _runtimeRoot;
    private EntityQuery _attackTargetPreviewQuery;
    private readonly List<GameObject> _attackTargetPreviewMarkers = new();
    private bool _attackTargetPreviewVisible;
    private int _attackTargetPreviewVisibleCount;
    private float _nextAttackTargetPreviewUpdateTime;
    private const int MaxAttackTargetPreviewMarkers = 64;
    private const float AttackTargetPreviewUpdateSeconds = 0.15f;
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
            ComponentType.ReadOnly<DynamicBlockerData>());
        _attackTargetPreviewQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<LocalTransform>());
    }

    public void Initialize(
        GameObject moveOrderMarkerPrefab,
        GameObject attackOrderMarkerPrefab,
        float orderMarkerVisibleSeconds,
        Transform runtimeRoot)
    {
        Dispose();
        _moveOrderMarkerPrefab = moveOrderMarkerPrefab;
        _attackOrderMarkerPrefab = attackOrderMarkerPrefab;
        _orderMarkerVisibleSeconds = Mathf.Max(0.01f, orderMarkerVisibleSeconds);
        _runtimeRoot = runtimeRoot;
        CacheMoveOrderMarker();
        CacheAttackOrderMarker();
    }

    public void Dispose()
    {
        if (_moveOrderMarker != null)
            UnityEngine.Object.Destroy(_moveOrderMarker);
        if (_attackOrderMarker != null)
            UnityEngine.Object.Destroy(_attackOrderMarker);
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
        _scanOrderMarker = null;
        _scanOrderMarkerRenderer = null;
        _attackTargetPreviewMarkers.Clear();
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
        if (_attackOrderMarker == null || _attackOrderMarkerHideTime < 0f)
            return;

        if (Time.time < _attackOrderMarkerHideTime)
            return;

        _attackOrderMarker.SetActive(false);
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
        DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        bool blocked = walkable[cellIndex].Value == 0 || (blockerData.Blocked.IsCreated && blockerData.Blocked.IsSet(cellIndex));

        if (blocked)
        {
            _moveOrderMarker.SetActive(false);
            _moveOrderMarkerHideTime = -1f;
            return;
        }

        Vector3 worldPosition = worldPoint;
        worldPosition.y = grid.Origin.y + 0.05f;

        _moveOrderMarker.transform.position = worldPosition;
        _moveOrderMarker.transform.rotation = Quaternion.identity;
        _moveOrderMarker.SetActive(true);

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

    public void ShowAttackOrderMarker(EntityManager em, Vector3 worldPoint)
    {
        if (_attackOrderMarker == null || _attackOrderMarkerRenderers == null || _attackOrderMarkerRenderers.Length == 0)
            return;

        EnsureEntityQueries(em);
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);

        Vector3 worldPosition = worldPoint;
        worldPosition.y = grid.Origin.y + 0.05f;

        _attackOrderMarker.transform.position = worldPosition;
        _attackOrderMarker.transform.rotation = Quaternion.identity;
        _attackOrderMarker.SetActive(true);

        for (int i = 0; i < _attackOrderMarkerRenderers.Length; i++)
        {
            Renderer renderer = _attackOrderMarkerRenderers[i];
            if (renderer == null)
                continue;

            _attackOrderMarkerPropertyBlock.Clear();
            renderer.SetPropertyBlock(_attackOrderMarkerPropertyBlock);
        }

        _attackOrderMarkerHideTime = Time.time + _orderMarkerVisibleSeconds;
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
        int markerIndex = 0;
        using NativeArray<Entity> targets = _attackTargetPreviewQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < targets.Length && markerIndex < MaxAttackTargetPreviewMarkers; i++)
        {
            Entity target = targets[i];
            if (!IsValidAttackPreviewTarget(em, target))
                continue;

            GameObject marker = EnsureAttackTargetPreviewMarker(markerIndex);
            if (marker == null)
                continue;

            float3 position = em.GetComponentData<LocalTransform>(target).Position;
            marker.transform.position = new Vector3(
                position.x,
                hasGroundY ? groundY : position.y + 0.05f,
                position.z);
            marker.transform.rotation = Quaternion.identity;
            SetMarkerActive(marker, true);
            markerIndex++;
        }

        HideAttackTargetPreviewMarkersIfNeeded(markerIndex);
        _attackTargetPreviewVisibleCount = markerIndex;
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
            ConfigureMarkerRenderers(marker.GetComponentsInChildren<Renderer>(true));
            marker.SetActive(false);
            _attackTargetPreviewMarkers.Add(marker);
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

    private static void SetMarkerActive(GameObject marker, bool active)
    {
        if (marker != null && marker.activeSelf != active)
            marker.SetActive(active);
    }

    private bool TryGetMarkerGroundY(EntityManager em, out float y)
    {
        y = 0.05f;
        if (_gridBlockerQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _gridBlockerQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        y = grid.Origin.y + 0.05f;
        return true;
    }

    private static bool IsValidAttackPreviewTarget(EntityManager em, Entity target)
    {
        if (target == Entity.Null ||
            !em.Exists(target) ||
            !em.HasComponent<Faction>(target) ||
            !em.HasComponent<LocalTransform>(target))
        {
            return false;
        }

        if (!FactionIdentitySystem.IsHostileToPlayer(em.GetComponentData<Faction>(target).Id))
            return false;

        return !em.HasComponent<UnitHealth>(target) ||
               em.GetComponentData<UnitHealth>(target).Current > 0;
    }
}
