using Unity.Entities;
using UnityEngine;

public sealed partial class RuntimeRootSystem : SystemBase
{
    private const string RuntimeBlockersRootName = "RuntimeBlockers";
    private const string RuntimeCityRootName = "RuntimeCity";
    private const string RuntimeTransportsRootName = "RuntimeTransports";
    private const string RuntimeUiRootName = "RuntimeUi";

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void Ensure(
        Transform owner,
        ref Transform runtimeBlockerRoot,
        ref Transform runtimeCityRoot,
        ref Transform runtimeTransportsRoot,
        ref Transform runtimeUiRoot)
    {
        runtimeBlockerRoot = EnsureRoot(owner, runtimeBlockerRoot, RuntimeBlockersRootName);
        runtimeCityRoot = EnsureRoot(owner, runtimeCityRoot, RuntimeCityRootName);
        runtimeTransportsRoot = EnsureRoot(owner, runtimeTransportsRoot, RuntimeTransportsRootName);
        runtimeUiRoot = EnsureRoot(owner, runtimeUiRoot, RuntimeUiRootName);
    }

    private Transform EnsureRoot(Transform owner, Transform currentRoot, string rootName)
    {
        if (currentRoot != null)
            return currentRoot;

        var rootObject = new GameObject(rootName);
        rootObject.transform.SetParent(owner, false);
        return rootObject.transform;
    }
}
