using UnityEngine;
using static UnityEngine.Object;

internal sealed class BuildingRuntimeObjectSystem
{
    internal void DestroyRuntimeObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
