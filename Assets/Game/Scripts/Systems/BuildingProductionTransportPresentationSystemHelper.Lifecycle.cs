using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingProductionTransportPresentationSystemHelper
    {
        public void Dispose()
        {
            if (_runtimeRoot != null)
                DestroyRuntimeRoot(_runtimeRoot.gameObject);

            _runtimeRoot = null;
            DestroyRuntimeMaterial(ref _dropRopeMaterial);
            _transportPoolByPrefab.Clear();
            _dropVisualPoolByPrefab.Clear();
            _transportRenderersByInstance.Clear();
            _dropVisualRenderersByInstance.Clear();
            _dropVisualMaterialCountByRenderer.Clear();
            _transportDoorByInstance.Clear();
            _transportDoorLookupCompleted.Clear();
            _transportBladeTransformsByInstance.Clear();
            _prewarmedTransportCountByPrefab.Clear();
            _transportStatePool.Clear();
            _dropVisualStatePool.Clear();
            _dropRopePool.Clear();
            _transformSearchBuffer.Clear();
            _configuredPoolSourcePrefabs = null;
            _configuredPoolSourcePrefabsByKey = null;
            _createdTransportStateCount = 0;
            System.Array.Clear(_laneUsage, 0, _laneUsage.Length);
        }

        private Transform EnsureRuntimeRoot()
        {
            if (_runtimeRoot != null)
                return _runtimeRoot;

            var root = new GameObject("RuntimeTransports");
            _runtimeRoot = root.transform;
            return _runtimeRoot;
        }

        private static void DestroyRuntimeRoot(GameObject root)
        {
            if (root == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(root);
            else
                Object.DestroyImmediate(root);
        }

        private static void DestroyRuntimeMaterial(ref Material material)
        {
            if (material == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(material);
            else
                Object.DestroyImmediate(material);
            material = null;
        }
    }
}
