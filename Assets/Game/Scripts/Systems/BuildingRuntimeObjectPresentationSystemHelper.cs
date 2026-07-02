using UnityEngine;
using static UnityEngine.Object;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeObjectPresentationSystemHelper
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
}
