using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingPlacementStartupSystemHelper
    {
        private void CreateBuildingRoot()
        {
            _buildingRoot = new GameObject("RuntimeBuildings").transform;
            _buildingRoot.SetParent(_runtimeRoot, false);
            _buildingRoot.localPosition = Vector3.zero;
            _buildingRoot.localRotation = Quaternion.identity;
            _buildingRoot.localScale = Vector3.one;
        }

        private static void DestroyBuildingRoot(GameObject root)
        {
            if (root == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(root);
            else
                UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
