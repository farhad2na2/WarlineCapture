using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class RuntimeCityVisualPresentationSystemHelper
    {
        private static void DestroyRoot(Transform root)
        {
            if (root == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(root.gameObject);
            else
                Object.DestroyImmediate(root.gameObject);
        }
    }
}
