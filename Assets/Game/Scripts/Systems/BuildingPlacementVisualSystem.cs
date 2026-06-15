using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingPlacementVisualSystem : SystemBase
{
    public delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);
    public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
    public delegate bool ShouldAlignGateToNearbyWallDelegate(Vector2Int originCell, BuildingDefinition definition, out bool gateVertical);

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public GameObject CreateBuildingVisualInstance(BuildingDefinition definition, Transform parent)
    {
        if (definition == null)
            return null;

        var wrapper = new GameObject($"{definition.DisplayName}_VisualRoot");
        wrapper.transform.SetParent(parent, false);
        wrapper.transform.localPosition = Vector3.zero;
        wrapper.transform.localRotation = Quaternion.identity;
        wrapper.transform.localScale = Vector3.one;

        GameObject visual = null;
        if (definition.Prefab != null)
        {
            visual = Object.Instantiate(definition.Prefab, wrapper.transform);
            Transform combinedMesh = FindDescendantByName(visual.transform, "CombinedMesh");
            if (combinedMesh != null)
                DisableSourceRenderersOutsideCombinedMesh(visual.transform, combinedMesh);
        }

        if (visual != null)
        {
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
        }

        return wrapper;
    }

    public void PositionBuildingObject(
        GameObject instance,
        Vector2Int originCell,
        BuildingDefinition definition,
        GridConfig grid,
        bool rotateVertical,
        GetPlacementFootprintDelegate getPlacementFootprint,
        GetFootprintCenterDelegate getFootprintCenter,
        ShouldAlignGateToNearbyWallDelegate shouldAlignGateToNearbyWall)
    {
        if (instance == null)
            return;

        if (!rotateVertical &&
            shouldAlignGateToNearbyWall != null &&
            shouldAlignGateToNearbyWall(originCell, definition, out bool gateVertical))
            rotateVertical = gateVertical;

        Vector2Int footprintCells = getPlacementFootprint != null
            ? getPlacementFootprint(definition, rotateVertical)
            : Vector2Int.one;
        Vector3 center = getFootprintCenter != null
            ? getFootprintCenter(originCell, footprintCells, grid)
            : Vector3.zero;
        Vector3 offset = Vector3.zero;
        if (definition != null && definition.HasLocalBounds)
            offset = new Vector3(definition.LocalBounds.center.x, 0f, definition.LocalBounds.center.z);

        Quaternion worldRotation = BuildingPlacementCommitSystem.ResolvePlacementWorldRotation(definition, rotateVertical);
        instance.transform.SetPositionAndRotation(center, worldRotation);
        instance.transform.localScale = Vector3.one;

        if (instance.transform.childCount > 0)
        {
            Transform visualRoot = instance.transform.GetChild(0);
            visualRoot.localPosition = -offset;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;
        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void DisableSourceRenderersOutsideCombinedMesh(Transform root, Transform combinedMesh)
    {
        Transform sourceRoot = combinedMesh.parent != null ? combinedMesh.parent : root;
        Renderer[] renderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null ||
                IsSelfOrDescendantOf(renderer.transform, combinedMesh) ||
                ShouldKeepRuntimeRenderer(renderer.transform))
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private static bool ShouldKeepRuntimeRenderer(Transform rendererTransform)
    {
        Transform current = rendererTransform;
        while (current != null)
        {
            string name = current.name;
            if (name == "Destroyed" ||
                name == "SelectionMarker" ||
                name == "Door_Z")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsSelfOrDescendantOf(Transform candidate, Transform root)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current == root)
                return true;

            current = current.parent;
        }

        return false;
    }
}
