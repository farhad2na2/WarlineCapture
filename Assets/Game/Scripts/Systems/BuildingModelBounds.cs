using UnityEngine;

namespace Game.Runtime
{
    /// <summary>Geometry in the prefab's placement coordinates, independent of camera and scene rotation.</summary>
    public static class BuildingModelBounds
    {
        public static bool TryMeasure(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null) return false;
            bool found = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !IsActiveBelow(renderer.transform, root.transform)) continue;
                Bounds local;
                if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null) local = skin.localBounds;
                else if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                    local = filter.sharedMesh.bounds;
                else continue; // Particle, trail and line effects do not reserve building land.
                var matrix = root.transform.worldToLocalMatrix * renderer.localToWorldMatrix;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    var point = matrix.MultiplyPoint3x4(new Vector3(x == 0 ? local.min.x : local.max.x,
                        y == 0 ? local.min.y : local.max.y, z == 0 ? local.min.z : local.max.z));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return found;
        }

        public static Vector2Int EnclosingCells(Bounds bounds) => new(
            Mathf.Max(1, Mathf.CeilToInt(bounds.size.x - 0.0001f)),
            Mathf.Max(1, Mathf.CeilToInt(bounds.size.z - 0.0001f)));

        private static bool IsActiveBelow(Transform child, Transform root)
        {
            for (var current = child; current != null && current != root; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }
    }
}
