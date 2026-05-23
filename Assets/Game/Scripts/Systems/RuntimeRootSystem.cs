using UnityEngine;

public sealed class RuntimeRootSystem
{
    private const string RuntimeBlockersRootName = "RuntimeBlockers";
    private const string RuntimeCityRootName = "RuntimeCity";
    private const string RuntimeUiRootName = "RuntimeUi";

    public void Ensure(
        Transform owner,
        ref Transform runtimeBlockerRoot,
        ref Transform runtimeCityRoot,
        ref Transform runtimeUiRoot)
    {
        runtimeBlockerRoot = EnsureRoot(owner, runtimeBlockerRoot, RuntimeBlockersRootName);
        runtimeCityRoot = EnsureRoot(owner, runtimeCityRoot, RuntimeCityRootName);
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
