using UnityEngine;
using System.Collections.Generic;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementVisualPresentationSystemHelper
    {
        public delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);
        public delegate Vector3 GetFootprintCenterDelegate(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid);
        public delegate bool ShouldAlignGateToNearbyWallDelegate(Vector2Int originCell, BuildingDefinition definition, out bool gateVertical);

        private const string PoolRootName = "BuildingPlacementVisualPool";

        private readonly Dictionary<BuildingDefinition, Stack<GameObject>> _pooledByDefinition = new();
        private readonly Dictionary<GameObject, BuildingDefinition> _activeDefinitions = new();
        private Transform _poolRoot;

        public GameObject CreateBuildingVisualInstance(BuildingDefinition definition, Transform parent)
        {
#if UNITY_EDITOR
            long allocationProbeStartBytes = System.GC.GetAllocatedBytesForCurrentThread();
            long allocationProbePrefabInstantiateBytes = 0;
            bool allocationProbePooled = false;
            bool allocationProbeWrapperCreated = false;
            bool allocationProbePrefabInstantiated = false;
            try
            {
                return CreateBuildingVisualInstanceCore(
                    definition,
                    parent,
                    ref allocationProbePooled,
                    ref allocationProbeWrapperCreated,
                    ref allocationProbePrefabInstantiated,
                    ref allocationProbePrefabInstantiateBytes);
            }
            finally
            {
                RuntimeDiagnosticsSystem.RecordEditorBuildingVisualAllocation(
                    System.GC.GetAllocatedBytesForCurrentThread() - allocationProbeStartBytes,
                    allocationProbePooled,
                    allocationProbeWrapperCreated,
                    allocationProbePrefabInstantiated,
                    allocationProbePrefabInstantiateBytes);
            }
#else
            return CreateBuildingVisualInstanceCore(definition, parent);
#endif
        }

#if UNITY_EDITOR
        private GameObject CreateBuildingVisualInstanceCore(
            BuildingDefinition definition,
            Transform parent,
            ref bool allocationProbePooled,
            ref bool allocationProbeWrapperCreated,
            ref bool allocationProbePrefabInstantiated,
            ref long allocationProbePrefabInstantiateBytes)
#else
        private GameObject CreateBuildingVisualInstanceCore(BuildingDefinition definition, Transform parent)
#endif
        {
            if (definition == null)
                return null;

            if (TryGetPooledInstance(definition, parent, out GameObject pooled))
            {
#if UNITY_EDITOR
                allocationProbePooled = true;
#endif
                return pooled;
            }

            var wrapper = new GameObject($"{definition.DisplayName}_VisualRoot");
#if UNITY_EDITOR
            allocationProbeWrapperCreated = true;
#endif
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.localPosition = Vector3.zero;
            wrapper.transform.localRotation = Quaternion.identity;
            wrapper.transform.localScale = Vector3.one;

            GameObject visual = null;
            if (definition.Prefab != null)
            {
#if UNITY_EDITOR
                long prefabInstantiateStartBytes = System.GC.GetAllocatedBytesForCurrentThread();
#endif
                visual = Object.Instantiate(definition.Prefab, wrapper.transform);
#if UNITY_EDITOR
                allocationProbePrefabInstantiated = true;
                allocationProbePrefabInstantiateBytes =
                    System.GC.GetAllocatedBytesForCurrentThread() - prefabInstantiateStartBytes;
#endif
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

            _activeDefinitions[wrapper] = definition;
            return wrapper;
        }

        public void ReleaseBuildingVisualInstance(GameObject instance)
        {
            if (instance == null)
                return;

            if (!_activeDefinitions.TryGetValue(instance, out BuildingDefinition definition) || definition == null)
            {
                DestroyRuntimeObject(instance);
                return;
            }

            _activeDefinitions.Remove(instance);
            ClearRendererPropertyBlocks(instance);
            instance.SetActive(false);
            instance.transform.SetParent(EnsurePoolRoot(instance.transform.parent), false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (!_pooledByDefinition.TryGetValue(definition, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                _pooledByDefinition[definition] = pool;
            }

            pool.Push(instance);
        }

        public void Dispose()
        {
            foreach (GameObject activeInstance in _activeDefinitions.Keys)
                DestroyRuntimeObject(activeInstance);

            _activeDefinitions.Clear();

            if (_poolRoot != null)
            {
                DestroyRuntimeObject(_poolRoot.gameObject);
            }
            else
            {
                foreach (Stack<GameObject> pool in _pooledByDefinition.Values)
                {
                    while (pool.Count > 0)
                        DestroyRuntimeObject(pool.Pop());
                }
            }

            _pooledByDefinition.Clear();
            _poolRoot = null;
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

            Quaternion worldRotation = BuildingPlacementCommitCompositionSystemHelper.ResolvePlacementWorldRotation(definition, rotateVertical);
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

        private bool TryGetPooledInstance(BuildingDefinition definition, Transform parent, out GameObject instance)
        {
            instance = null;
            if (!_pooledByDefinition.TryGetValue(definition, out Stack<GameObject> pool))
                return false;

            while (pool.Count > 0)
            {
                instance = pool.Pop();
                if (instance == null)
                    continue;

                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                instance.SetActive(true);
                _activeDefinitions[instance] = definition;
                return true;
            }

            instance = null;
            return false;
        }

        private Transform EnsurePoolRoot(Transform fallbackParent)
        {
            if (_poolRoot != null)
                return _poolRoot;

            var root = new GameObject(PoolRootName);
            if (fallbackParent != null)
                root.transform.SetParent(fallbackParent, false);
            root.SetActive(false);
            _poolRoot = root.transform;
            return _poolRoot;
        }

        private static void ClearRendererPropertyBlocks(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i]?.SetPropertyBlock(null);
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
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
}
