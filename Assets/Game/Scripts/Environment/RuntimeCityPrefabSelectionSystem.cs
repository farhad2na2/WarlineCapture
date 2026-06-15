using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed partial class RuntimeCityPrefabSelectionSystem : SystemBase
{
    private readonly RuntimeCityPrefabSelectionState _state = new();

    public RuntimeCityPrefabSelectionState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public bool IsConfiguredPrefab(GameObject prefab, List<GameObject> configuredPrefabs)
    {
        return _state.IsConfiguredPrefab(prefab, configuredPrefabs);
    }

    public GameObject GetRandomPrefab(List<GameObject> prefabs, ref Unity.Mathematics.Random rng)
    {
        return _state.GetRandomPrefab(prefabs, ref rng);
    }

    public void Shuffle<T>(List<T> list, ref Unity.Mathematics.Random rng)
    {
        _state.Shuffle(list, ref rng);
    }

    public int GetMajorFootprint(GameObject prefab)
    {
        return _state.GetMajorFootprint(prefab);
    }

    public int GetMinorFootprint(GameObject prefab)
    {
        return _state.GetMinorFootprint(prefab);
    }

    public Vector2Int GetCachedFootprintCells(GameObject prefab)
    {
        return _state.GetCachedFootprintCells(prefab);
    }
}

internal sealed class RuntimeCityPrefabSelectionState
{
    private readonly Dictionary<GameObject, Vector2Int> _prefabFootprintCache = new();

    public bool IsConfiguredPrefab(GameObject prefab, List<GameObject> configuredPrefabs)
    {
        if (prefab == null || configuredPrefabs == null)
            return false;

        for (int i = 0; i < configuredPrefabs.Count; i++)
        {
            if (configuredPrefabs[i] == prefab)
                return true;
        }

        return false;
    }

    public GameObject GetRandomPrefab(List<GameObject> prefabs, ref Unity.Mathematics.Random rng)
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        return prefabs[rng.NextInt(0, prefabs.Count)];
    }

    public void Shuffle<T>(List<T> list, ref Unity.Mathematics.Random rng)
    {
        if (list == null)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.NextInt(0, i + 1);
            T value = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = value;
        }
    }

    public int GetMajorFootprint(GameObject prefab)
    {
        Vector2Int footprint = GetCachedFootprintCells(prefab);
        return Mathf.Max(1, Mathf.Max(footprint.x, footprint.y));
    }

    public int GetMinorFootprint(GameObject prefab)
    {
        Vector2Int footprint = GetCachedFootprintCells(prefab);
        return Mathf.Max(1, Mathf.Min(footprint.x, footprint.y));
    }

    public Vector2Int GetCachedFootprintCells(GameObject prefab)
    {
        if (prefab == null)
            return new Vector2Int(6, 6);

        if (_prefabFootprintCache.TryGetValue(prefab, out Vector2Int footprint))
            return footprint;

        footprint = EstimateFootprintCells(prefab);
        _prefabFootprintCache[prefab] = footprint;
        return footprint;
    }

    private static Vector2Int EstimateFootprintCells(GameObject prefab)
    {
        if (prefab == null)
            return new Vector2Int(6, 6);

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Vector2Int(6, 6);

        Bounds bounds = default;
        bool hasBounds = false;
        Matrix4x4 worldToLocal = prefab.transform.worldToLocalMatrix;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Bounds rendererBounds = renderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        if (!hasBounds)
            return new Vector2Int(6, 6);

        return new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(bounds.size.x))),
            Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(bounds.size.z))));
    }
}
