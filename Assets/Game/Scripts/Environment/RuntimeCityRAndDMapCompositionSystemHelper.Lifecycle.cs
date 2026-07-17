using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class RuntimeCityRAndDMapCompositionSystemHelper
    {
        private void DestroyGeneratedRoot()
        {
            if (_generatedRoot == null)
                return;

            DestroyRuntimeObject(_generatedRoot.gameObject);
            _generatedRoot = null;
        }

        private void ClearGeneratedRootChildren()
        {
            if (_generatedRoot == null)
                return;

            for (int i = _generatedRoot.childCount - 1; i >= 0; i--)
                DestroyRuntimeObject(_generatedRoot.GetChild(i).gameObject);
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
