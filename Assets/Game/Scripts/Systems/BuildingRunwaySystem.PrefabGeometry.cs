using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingRunwaySystem
    {
        public bool TryGetRunwayLocalData(
            GameObject prefab,
            out Vector3 localPosition,
            out Quaternion localRotation,
            out Vector3 halfExtents)
        {
            return TryResolvePrefabRunwayLocalData(
                prefab,
                out localPosition,
                out localRotation,
                out halfExtents);
        }

        internal static bool TryResolvePrefabRunwayLocalData(
            GameObject prefab,
            out Vector3 localPosition,
            out Quaternion localRotation,
            out Vector3 halfExtents)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            halfExtents = new Vector3(8f, 0.5f, 24f);
            if (prefab == null)
                return false;

            Transform runway = null;
            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (transforms[index] != null && transforms[index].name == "Runway")
                {
                    runway = transforms[index];
                    break;
                }
            }

            if (runway == null)
                return false;

            Transform runwayStart = null;
            Transform runwayEnd = null;
            for (int index = 0; index < runway.childCount; index++)
            {
                Transform child = runway.GetChild(index);
                if (child == null)
                    continue;
                if (child.name == "Runway_Start")
                    runwayStart = child;
                else if (child.name == "Runway_End")
                    runwayEnd = child;
            }

            if (runwayStart != null && runwayEnd != null)
            {
                Vector3 worldStart = runwayStart.position;
                Vector3 worldEnd = runwayEnd.position;
                Vector3 worldDirection = worldEnd - worldStart;
                Vector3 planarDirection = new(worldDirection.x, 0f, worldDirection.z);
                if (planarDirection.sqrMagnitude > 0.0001f)
                {
                    Vector3 worldCenter = ResolveRunwaySurfaceWorldCenter(
                        runway,
                        (worldStart + worldEnd) * 0.5f);
                    localPosition = prefab.transform.InverseTransformPoint(worldCenter);
                    Quaternion worldRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
                    localRotation = Quaternion.Inverse(prefab.transform.rotation) * worldRotation;
                    halfExtents = new Vector3(
                        8f,
                        0.5f,
                        Mathf.Max(8f, planarDirection.magnitude * 0.5f));
                    return true;
                }
            }

            localPosition = runway.localPosition;
            localRotation = runway.localRotation;

            Renderer runwayRenderer = runway.GetComponentInChildren<Renderer>(true);
            if (runwayRenderer != null)
            {
                Bounds bounds = runwayRenderer.localBounds;
                halfExtents = bounds.extents;
                if (halfExtents.x <= 0.01f || halfExtents.z <= 0.01f)
                    halfExtents = new Vector3(8f, 0.5f, 24f);
            }

            return true;
        }
    }
}
