using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
    private GameObject _moveOrderMarkerPrefab;
    private GameObject _attackOrderMarkerPrefab;
    private float _orderMarkerVisibleSeconds = 1.25f;
    private Transform _runtimeRoot;

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

        _moveOrderMarker = null;
        _moveOrderMarkerRenderers = null;
        _attackOrderMarker = null;
        _attackOrderMarkerRenderers = null;
        _moveOrderMarkerHideTime = -1f;
        _attackOrderMarkerHideTime = -1f;
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
        if (_moveOrderMarkerHideTime < 0f)
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
            _attackOrderMarker.SetActive(false);
            return;
        }

        _attackOrderMarker = null;
        _attackOrderMarkerRenderers = null;
    }
}
