using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static UnityEngine.Object;
using CombinedRoadVisualData = RoadGridProjectionSystem.CombinedRoadVisualData;
using FootprintBoundsData = RoadGridProjectionSystem.RoadFootprintBoundsData;
using FootprintKind = RoadGridProjectionSystem.RoadFootprintKind;
using RoadVisualType = RoadNetworkCompositionSystemHelper.RoadVisualType;
using TileConnectionMask = RoadNetworkCompositionSystemHelper.TileConnectionMask;

public sealed partial class RoadVisualVariantSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnDestroy()
    {
        DisposeCachedVisualData();
    }

    protected override void OnUpdate()
    {
    }

    public readonly struct VariantData
    {
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;

        public VariantData(Quaternion rotation, Vector3 scale)
        {
            Rotation = rotation;
            Scale = scale;
        }
    }

    public readonly struct ConnectorMarkerData
    {
        public readonly Vector3 RoadConnectLocalPosition;
        public readonly Vector3 AutobahnConnectLocalPosition;
        public readonly Vector3 Center;

        public ConnectorMarkerData(Vector3 roadConnectLocalPosition, Vector3 autobahnConnectLocalPosition, Vector3 center)
        {
            RoadConnectLocalPosition = roadConnectLocalPosition;
            AutobahnConnectLocalPosition = autobahnConnectLocalPosition;
            Center = center;
        }
    }

    public sealed class MarkerLayoutData
    {
        public readonly List<Vector3> ConnectLocalPositions = new();
        public Vector3? RoadConnectLocalPosition;
        public Vector3? AutobahnConnectLocalPosition;
        public Vector3 Center;
    }

    public readonly struct Prefabs
    {
        public readonly GameObject End;
        public readonly GameObject Straight;
        public readonly GameObject Corner;
        public readonly GameObject TIntersection;
        public readonly GameObject Intersection;
        public readonly GameObject Autobahn;
        public readonly GameObject AutobahnConnect;

        public Prefabs(
            GameObject end,
            GameObject straight,
            GameObject corner,
            GameObject tIntersection,
            GameObject intersection,
            GameObject autobahn,
            GameObject autobahnConnect)
        {
            End = end;
            Straight = straight;
            Corner = corner;
            TIntersection = tIntersection;
            Intersection = intersection;
            Autobahn = autobahn;
            AutobahnConnect = autobahnConnect;
        }
    }

    public Dictionary<RoadVisualType, Dictionary<TileConnectionMask, VariantData>> Variants { get; } = new();
    public Dictionary<RoadVisualType, CombinedRoadVisualData> VisualData { get; } = new();
    public Dictionary<RoadVisualType, MarkerLayoutData> MarkerLayouts { get; } = new();
    public ConnectorMarkerData? AutobahnConnectorMarkerData { get; private set; }

    public GameObject GetPrefab(Prefabs prefabs, RoadVisualType type)
    {
        return type switch
        {
            RoadVisualType.End => prefabs.End,
            RoadVisualType.Straight => prefabs.Straight,
            RoadVisualType.Corner => prefabs.Corner,
            RoadVisualType.TIntersection => prefabs.TIntersection,
            RoadVisualType.Intersection => prefabs.Intersection,
            RoadVisualType.Autobahn => prefabs.Autobahn,
            RoadVisualType.AutobahnConnect => prefabs.AutobahnConnect,
            _ => null
        };
    }

    public void CacheVariants(Prefabs prefabs)
    {
        Variants.Clear();
        VisualData.Clear();
        MarkerLayouts.Clear();
        AutobahnConnectorMarkerData = null;

        CacheVisualData(prefabs, RoadVisualType.End, prefabs.End);
        CacheVisualData(prefabs, RoadVisualType.Straight, prefabs.Straight);
        CacheVisualData(prefabs, RoadVisualType.Corner, prefabs.Corner);
        CacheVisualData(prefabs, RoadVisualType.TIntersection, prefabs.TIntersection);
        CacheVisualData(prefabs, RoadVisualType.Intersection, prefabs.Intersection);
        CacheVisualData(prefabs, RoadVisualType.Autobahn, prefabs.Autobahn);
        CacheVisualData(prefabs, RoadVisualType.AutobahnConnect, prefabs.AutobahnConnect);
    }

    public void DisposeCachedVisualData()
    {
        foreach (var visual in VisualData.Values)
        {
            if (visual.Mesh != null)
                Destroy(visual.Mesh);
        }

        Variants.Clear();
        VisualData.Clear();
        MarkerLayouts.Clear();
        AutobahnConnectorMarkerData = null;
    }

    public bool TryGetVariant(RoadVisualType type, TileConnectionMask mask, out VariantData variant)
    {
        variant = default;
        if (!Variants.TryGetValue(type, out var variantsByMask))
            return false;

        if (variantsByMask.TryGetValue(mask, out variant))
            return true;

        if (type == RoadVisualType.Autobahn || type == RoadVisualType.AutobahnConnect)
        {
            TileConnectionMask normalizedMask = NormalizeAutobahnMask(mask);
            if (variantsByMask.TryGetValue(normalizedMask, out variant))
                return true;
        }

        return false;
    }

    public static TileConnectionMask NormalizeAutobahnMask(TileConnectionMask mask)
    {
        if (mask.North || mask.South)
            return new TileConnectionMask(true, false, true, false);

        if (mask.East || mask.West)
            return new TileConnectionMask(false, true, false, true);

        return mask;
    }

    public static TileConnectionMask BuildAxisMask(Vector2Int direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return new TileConnectionMask(false, true, false, true);

        return new TileConnectionMask(true, false, true, false);
    }

    public static TileConnectionMask BuildMaskFromDirections(params Vector2Int[] directions)
    {
        bool north = false;
        bool east = false;
        bool south = false;
        bool west = false;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int direction = directions[i];
            if (direction == Vector2Int.zero)
                continue;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (direction.x > 0)
                    east = true;
                else
                    west = true;
            }
            else
            {
                if (direction.y > 0)
                    north = true;
                else
                    south = true;
            }
        }

        return new TileConnectionMask(north, east, south, west);
    }

    private void CacheVisualData(Prefabs prefabs, RoadVisualType type, GameObject prefab)
    {
        if (prefab == null)
            return;

        GameObject temp = Instantiate(prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;

        var connectLocalPositions = new List<Vector3>();
        var markerLayout = new MarkerLayoutData();
        Vector3? roadConnectLocalPosition = null;
        Vector3? autobahnConnectLocalPosition = null;
        var allTransforms = temp.GetComponentsInChildren<Transform>(true);
        foreach (var child in allTransforms)
        {
            if (child.name == "Connect")
            {
                Vector3 localPosition = temp.transform.InverseTransformPoint(child.position);
                connectLocalPositions.Add(localPosition);
                markerLayout.ConnectLocalPositions.Add(localPosition);
                if (type == RoadVisualType.AutobahnConnect)
                {
                    roadConnectLocalPosition = localPosition;
                    markerLayout.RoadConnectLocalPosition = localPosition;
                }
            }
            else if (type == RoadVisualType.AutobahnConnect && child.name == "ConnectAutoBahn")
            {
                Vector3 localPosition = temp.transform.InverseTransformPoint(child.position);
                connectLocalPositions.Add(localPosition);
                autobahnConnectLocalPosition = localPosition;
                markerLayout.AutobahnConnectLocalPosition = localPosition;
            }
        }

        if (connectLocalPositions.Count == 0)
        {
            Destroy(temp);
            return;
        }

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        var renderers = temp.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            Vector3 rendererMin = bounds.min;
            Vector3 rendererMax = bounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? rendererMin.x : rendererMax.x,
                            y == 0 ? rendererMin.y : rendererMax.y,
                            z == 0 ? rendererMin.z : rendererMax.z);
                        Vector3 localCorner = temp.transform.InverseTransformPoint(corner);
                        min = Vector3.Min(min, localCorner);
                        max = Vector3.Max(max, localCorner);
                    }
                }
            }
        }

        Vector3 center = (min + max) * 0.5f;
        markerLayout.Center = center;
        if (type == RoadVisualType.AutobahnConnect && roadConnectLocalPosition.HasValue && autobahnConnectLocalPosition.HasValue)
            AutobahnConnectorMarkerData = new ConnectorMarkerData(roadConnectLocalPosition.Value, autobahnConnectLocalPosition.Value, center);

        var variantMap = new Dictionary<TileConnectionMask, VariantData>();
        int[] rotationAngles = { 0, 90, 180, 270 };
        int[] flipValues = { 1, -1 };
        foreach (int angle in rotationAngles)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            foreach (int scaleX in flipValues)
            {
                foreach (int scaleZ in flipValues)
                {
                    Vector3 scale = new(scaleX, 1f, scaleZ);
                    TileConnectionMask mask = BuildVariantMask(connectLocalPositions, center, rotation, scale);
                    if (!variantMap.ContainsKey(mask))
                        variantMap.Add(mask, new VariantData(rotation, scale));
                }
            }
        }

        Destroy(temp);

        Variants[type] = variantMap;
        VisualData[type] = BuildCombinedVisualData(type, prefab);
        MarkerLayouts[type] = markerLayout;
    }

    private static CombinedRoadVisualData BuildCombinedVisualData(RoadVisualType type, GameObject prefab)
    {
        GameObject temp = Instantiate(prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;

        var materialOrder = new List<Material>();
        var combinesByMaterial = new Dictionary<Material, List<CombineInstance>>();
        var footprintBounds = new List<FootprintBoundsData>();
        var meshFilters = temp.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
                continue;

            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (RoadGridProjectionSystem.TryGetFootprintKind(
                    meshFilter.transform,
                    type == RoadVisualType.Autobahn || type == RoadVisualType.AutobahnConnect,
                    out FootprintKind footprintKind))
            {
                Bounds localBounds = RoadGridProjectionSystem.TransformBounds(
                    meshFilter.sharedMesh.bounds,
                    temp.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix);
                footprintBounds.Add(new FootprintBoundsData
                {
                    Bounds = localBounds,
                    Kind = footprintKind
                });
            }

            if (meshRenderer == null || !meshRenderer.enabled)
                continue;
            if (!meshFilter.sharedMesh.isReadable)
                continue;

            Material[] materials = meshRenderer.sharedMaterials;
            int subMeshCount = Mathf.Min(meshFilter.sharedMesh.subMeshCount, materials.Length);
            Matrix4x4 localMatrix = temp.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                Material material = materials[subMeshIndex];
                if (material == null)
                    continue;

                if (!combinesByMaterial.TryGetValue(material, out var combines))
                {
                    combines = new List<CombineInstance>();
                    combinesByMaterial.Add(material, combines);
                    materialOrder.Add(material);
                }

                combines.Add(new CombineInstance
                {
                    mesh = meshFilter.sharedMesh,
                    subMeshIndex = subMeshIndex,
                    transform = localMatrix
                });
            }
        }

        Destroy(temp);

        if (materialOrder.Count == 0)
            return new CombinedRoadVisualData();

        var finalSubmeshCombines = new CombineInstance[materialOrder.Count];
        for (int i = 0; i < materialOrder.Count; i++)
        {
            Mesh submeshMesh = new Mesh
            {
                name = $"{prefab.name}_{materialOrder[i].name}_Combined"
            };
            submeshMesh.CombineMeshes(combinesByMaterial[materialOrder[i]].ToArray(), true, true, false);
            finalSubmeshCombines[i] = new CombineInstance
            {
                mesh = submeshMesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
        }

        Mesh finalMesh = new Mesh
        {
            name = $"{prefab.name}_Combined"
        };
        finalMesh.CombineMeshes(finalSubmeshCombines, false, false, false);

        for (int i = 0; i < finalSubmeshCombines.Length; i++)
            Destroy(finalSubmeshCombines[i].mesh);

        return new CombinedRoadVisualData
        {
            Mesh = finalMesh,
            Materials = materialOrder.ToArray(),
            FootprintBounds = footprintBounds
        };
    }

    private static TileConnectionMask BuildVariantMask(
        List<Vector3> connectLocalPositions,
        Vector3 center,
        Quaternion rotation,
        Vector3 scale)
    {
        bool north = false;
        bool east = false;
        bool south = false;
        bool west = false;
        for (int i = 0; i < connectLocalPositions.Count; i++)
        {
            Vector3 offset = connectLocalPositions[i] - center;
            Vector3 scaledOffset = Vector3.Scale(offset, scale);
            Vector3 transformedOffset = rotation * scaledOffset;

            if (Mathf.Abs(transformedOffset.x) > Mathf.Abs(transformedOffset.z))
            {
                if (transformedOffset.x >= 0f)
                    east = true;
                else
                    west = true;
            }
            else
            {
                if (transformedOffset.z >= 0f)
                    north = true;
                else
                    south = true;
            }
        }

        return new TileConnectionMask(north, east, south, west);
    }
}
