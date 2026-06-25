using UnityEngine;
using static UnityEngine.Object;

public sealed class RoadRuntimeRootSceneSystemHelper
{
    public readonly struct Roots
    {
        public readonly Transform RoadRoot;
        public readonly Transform SpecialRoadRoot;
        public readonly Transform SpecialRoadConnectorRoot;
        public readonly Transform DebugStraightRoadRoot;
        public readonly Transform BuildingRoot;

        public Roots(
            Transform roadRoot,
            Transform specialRoadRoot,
            Transform specialRoadConnectorRoot,
            Transform debugStraightRoadRoot,
            Transform buildingRoot)
        {
            RoadRoot = roadRoot;
            SpecialRoadRoot = specialRoadRoot;
            SpecialRoadConnectorRoot = specialRoadConnectorRoot;
            DebugStraightRoadRoot = debugStraightRoadRoot;
            BuildingRoot = buildingRoot;
        }
    }

    public Roots CreateRoots(Transform runtimeRoot)
    {
        return new Roots(
            CreateRuntimeChildRoot(runtimeRoot, "RuntimeRoads"),
            CreateRuntimeChildRoot(runtimeRoot, "RuntimeAutobahns"),
            CreateRuntimeChildRoot(runtimeRoot, "RuntimeAutobahnConnectors"),
            CreateRuntimeChildRoot(runtimeRoot, "RuntimeDebugStraightRoads"),
            CreateRuntimeChildRoot(runtimeRoot, "RuntimeBuildings"));
    }

    public void DisposeRoots(Roots roots)
    {
        DestroyRoot(roots.RoadRoot);
        DestroyRoot(roots.SpecialRoadRoot);
        DestroyRoot(roots.SpecialRoadConnectorRoot);
        DestroyRoot(roots.DebugStraightRoadRoot);
        DestroyRoot(roots.BuildingRoot);
    }

    private static Transform CreateRuntimeChildRoot(Transform runtimeRoot, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(runtimeRoot, false);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    private static void DestroyRoot(Transform root)
    {
        if (root != null)
            Destroy(root.gameObject);
    }
}
