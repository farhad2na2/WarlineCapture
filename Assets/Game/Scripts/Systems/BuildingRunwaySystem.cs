using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingRunwaySystem : SystemBase
    {
        public delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);

        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public bool TryGetNearestAirportRunway(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Vector3 origin,
            out RuntimeBuildingEntity airport,
            out Vector3 runwayCenter,
            out Quaternion runwayRotation,
            out Vector3 runwayHalfExtents)
        {
            airport = null;
            runwayCenter = Vector3.zero;
            runwayRotation = Quaternion.identity;
            runwayHalfExtents = new Vector3(8f, 0.5f, 24f);
            if (runtimeBuildings == null)
                return false;

            float bestDistance = float.PositiveInfinity;
            if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingDictionary)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingDictionary)
                {
                    TryUseNearestAirportRunway(pair.Value, origin, ref bestDistance, ref airport, ref runwayCenter, ref runwayRotation, ref runwayHalfExtents);
                }

                return airport != null;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
            {
                TryUseNearestAirportRunway(pair.Value, origin, ref bestDistance, ref airport, ref runwayCenter, ref runwayRotation, ref runwayHalfExtents);
            }

            return airport != null;
        }

        public bool HasAvailableAirportRunway(IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            if (runtimeBuildings == null)
                return false;

            if (runtimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingDictionary)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingDictionary)
                {
                    if (IsAvailableAirportRunway(pair.Value))
                        return true;
                }

                return false;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
            {
                if (IsAvailableAirportRunway(pair.Value))
                    return true;
            }

            return false;
        }

        private static bool IsAvailableAirportRunway(RuntimeBuildingEntity candidate)
        {
            return candidate != null &&
                   !candidate.IsDestroyed &&
                   candidate.Instance != null &&
                   candidate.Definition != null &&
                   candidate.Definition.HasRunway;
        }

        private static void TryUseNearestAirportRunway(
            RuntimeBuildingEntity candidate,
            Vector3 origin,
            ref float bestDistance,
            ref RuntimeBuildingEntity airport,
            ref Vector3 runwayCenter,
            ref Quaternion runwayRotation,
            ref Vector3 runwayHalfExtents)
        {
            if (candidate == null || candidate.IsDestroyed || candidate.Instance == null || candidate.Definition == null || !candidate.Definition.HasRunway)
                return;

            if (!TryResolveRuntimeRunwayWorldData(candidate, out Vector3 candidateCenter, out Quaternion candidateRotation, out Vector3 candidateHalfExtents))
                return;

            float distance = (candidateCenter - origin).sqrMagnitude;
            if (distance >= bestDistance)
                return;

            bestDistance = distance;
            airport = candidate;
            runwayCenter = candidateCenter;
            runwayRotation = candidateRotation;
            runwayHalfExtents = candidateHalfExtents;
        }

        internal static Vector3 ResolveRuntimeRunwayLocalPosition(BuildingDefinition definition)
        {
            if (definition == null)
                return Vector3.zero;

            Vector3 visualOffset = definition.HasLocalBounds
                ? new Vector3(definition.LocalBounds.center.x, 0f, definition.LocalBounds.center.z)
                : Vector3.zero;
            return definition.RunwayLocalPosition - visualOffset;
        }

        internal static bool TryResolveRuntimeRunwayWorldData(
            RuntimeBuildingEntity building,
            out Vector3 center,
            out Quaternion rotation,
            out Vector3 halfExtents)
        {
            center = Vector3.zero;
            rotation = Quaternion.identity;
            halfExtents = new Vector3(8f, 0.5f, 24f);
            if (building == null ||
                building.IsDestroyed ||
                building.Instance == null ||
                building.Definition == null ||
                !building.Definition.HasRunway)
            {
                return false;
            }

            Transform instanceTransform = building.Instance.transform;
            if (TryFindRunwayTransform(instanceTransform, out Transform runway))
            {
                if (TryResolveRunwayMarkerWorldData(runway, out center, out rotation, out halfExtents))
                    return true;

                center = ResolveRunwaySurfaceWorldCenter(runway, runway.position);
                rotation = runway.rotation;
                halfExtents = ResolveRunwayWorldHalfExtents(
                    runway,
                    center,
                    rotation,
                    Vector3.Scale(building.Definition.RunwayHalfExtents, instanceTransform.lossyScale));
                return true;
            }

            center = instanceTransform.TransformPoint(ResolveRuntimeRunwayLocalPosition(building.Definition));
            rotation = instanceTransform.rotation * building.Definition.RunwayLocalRotation;
            halfExtents = Vector3.Scale(building.Definition.RunwayHalfExtents, instanceTransform.lossyScale);
            return true;
        }

        public RectInt GetEffectivePlacementRect(
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical,
            float buildPlaneY,
            GetPlacementFootprintDelegate getPlacementFootprint)
        {
            Vector2Int modelFootprint = getPlacementFootprint != null ? getPlacementFootprint(definition, rotateVertical) : Vector2Int.one;
            RectInt modelRect = new(originCell, modelFootprint);
            if (definition == null || !definition.HasRunway)
                return modelRect;

            if (!TryGetRunwayFootprintRect(definition, originCell, grid, rotateVertical, buildPlaneY, getPlacementFootprint, out RectInt runwayRect))
                return modelRect;

            return UnionRects(modelRect, runwayRect);
        }

        private static Vector3 ResolveRunwaySurfaceWorldCenter(Transform runway, Vector3 fallbackCenter)
        {
            if (runway == null)
                return fallbackCenter;

            Renderer directRenderer = runway.GetComponent<Renderer>();
            if (directRenderer != null)
                return ResolveRunwayRendererSurfaceCenter(directRenderer);

            Renderer[] childRenderers = runway.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds combinedBounds = default;
            for (int i = 0; i < childRenderers.Length; i++)
            {
                Renderer renderer = childRenderers[i];
                if (renderer == null || IsRunwayEndpointMarker(renderer.transform))
                    continue;

                if (found)
                    combinedBounds.Encapsulate(renderer.bounds);
                else
                {
                    combinedBounds = renderer.bounds;
                    found = true;
                }
            }

            if (!found)
                return new Vector3(runway.position.x, fallbackCenter.y, runway.position.z);

            Vector3 center = combinedBounds.center;
            center.y = combinedBounds.max.y;
            return center;
        }

        private static bool TryResolveRunwayMarkerWorldData(
            Transform runway,
            out Vector3 center,
            out Quaternion rotation,
            out Vector3 halfExtents)
        {
            center = Vector3.zero;
            rotation = Quaternion.identity;
            halfExtents = new Vector3(8f, 0.5f, 24f);
            if (runway == null ||
                !TryFindRunwayMarker(runway, "Runway_Start", out Transform runwayStart) ||
                !TryFindRunwayMarker(runway, "Runway_End", out Transform runwayEnd))
            {
                return false;
            }

            Vector3 worldStart = runwayStart.position;
            Vector3 worldEnd = runwayEnd.position;
            Vector3 worldDirection = worldEnd - worldStart;
            Vector3 planarDirection = new(worldDirection.x, 0f, worldDirection.z);
            if (planarDirection.sqrMagnitude <= 0.0001f)
                return false;

            Vector3 markerCenter = (worldStart + worldEnd) * 0.5f;
            Vector3 surfaceCenter = ResolveRunwaySurfaceWorldCenter(runway, markerCenter);
            center = new Vector3(markerCenter.x, surfaceCenter.y, markerCenter.z);
            rotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            halfExtents = ResolveRunwayWorldHalfExtents(
                runway,
                center,
                rotation,
                new Vector3(8f, 0.5f, Mathf.Max(8f, planarDirection.magnitude * 0.5f)));
            halfExtents.z = Mathf.Max(halfExtents.z, planarDirection.magnitude * 0.5f);
            return true;
        }

        private static Vector3 ResolveRunwayWorldHalfExtents(
            Transform runway,
            Vector3 center,
            Quaternion rotation,
            Vector3 fallbackHalfExtents)
        {
            if (runway == null)
                return fallbackHalfExtents;

            Renderer[] renderers = runway.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return fallbackHalfExtents;

            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            float halfWidth = Mathf.Max(1f, Mathf.Abs(fallbackHalfExtents.x));
            float halfLength = Mathf.Max(8f, Mathf.Abs(fallbackHalfExtents.z));
            bool found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsRunwayEndpointMarker(renderer.transform))
                    continue;

                Bounds bounds = renderer.bounds;
                EncapsulateRunwayBoundsCorner(bounds.min.x, bounds.min.y, bounds.min.z, center, right, forward, ref halfWidth, ref halfLength);
                EncapsulateRunwayBoundsCorner(bounds.min.x, bounds.min.y, bounds.max.z, center, right, forward, ref halfWidth, ref halfLength);
                EncapsulateRunwayBoundsCorner(bounds.min.x, bounds.max.y, bounds.min.z, center, right, forward, ref halfWidth, ref halfLength);
                EncapsulateRunwayBoundsCorner(bounds.min.x, bounds.max.y, bounds.max.z, center, right, forward, ref halfWidth, ref halfLength);
                EncapsulateRunwayBoundsCorner(bounds.max.x, bounds.min.y, bounds.min.z, center, right, forward, ref halfWidth, ref halfLength);
                EncapsulateRunwayBoundsCorner(bounds.max.x, bounds.min.y, bounds.max.z, center, right, forward, ref halfWidth, ref halfLength);
                EncapsulateRunwayBoundsCorner(bounds.max.x, bounds.max.y, bounds.min.z, center, right, forward, ref halfWidth, ref halfLength);
                EncapsulateRunwayBoundsCorner(bounds.max.x, bounds.max.y, bounds.max.z, center, right, forward, ref halfWidth, ref halfLength);
                found = true;
            }

            return found
                ? new Vector3(halfWidth, Mathf.Max(0.5f, Mathf.Abs(fallbackHalfExtents.y)), halfLength)
                : fallbackHalfExtents;
        }

        private static void EncapsulateRunwayBoundsCorner(
            float x,
            float y,
            float z,
            Vector3 center,
            Vector3 right,
            Vector3 forward,
            ref float halfWidth,
            ref float halfLength)
        {
            Vector3 delta = new Vector3(x, y, z) - center;
            halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(delta, right)));
            halfLength = Mathf.Max(halfLength, Mathf.Abs(Vector3.Dot(delta, forward)));
        }

        private static Vector3 ResolveRunwayRendererSurfaceCenter(Renderer renderer)
        {
            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            center.y = bounds.max.y;
            return center;
        }

        private static bool IsRunwayEndpointMarker(Transform transform)
        {
            if (transform == null)
                return false;

            string name = transform.name;
            return ContainsOrdinalIgnoreCase(name, "Runway_Start") ||
                   ContainsOrdinalIgnoreCase(name, "Runway_End") ||
                   ContainsOrdinalIgnoreCase(name, "Touchdown") ||
                   ContainsOrdinalIgnoreCase(name, "Marker");
        }

        private static bool TryFindRunwayTransform(Transform root, out Transform runway)
        {
            return TryFindChildByName(root, "Runway", out runway);
        }

        private static bool TryFindRunwayMarker(Transform root, string markerName, out Transform marker)
        {
            return TryFindChildByName(root, markerName, out marker);
        }

        private static bool TryFindChildByName(Transform root, string childName, out Transform child)
        {
            child = null;
            if (root == null || string.IsNullOrEmpty(childName))
                return false;

            if (string.Equals(root.name, childName, System.StringComparison.Ordinal))
            {
                child = root;
                return true;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                if (TryFindChildByName(root.GetChild(i), childName, out child))
                    return true;
            }

            return false;
        }

        private static bool ContainsOrdinalIgnoreCase(string value, string part)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryGetRunwayFootprintRect(
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical,
            float buildPlaneY,
            GetPlacementFootprintDelegate getPlacementFootprint,
            out RectInt runwayRect)
        {
            runwayRect = default;
            if (definition == null || !definition.HasRunway || grid.CellSize <= 0f || getPlacementFootprint == null)
                return false;

            Vector2Int modelFootprint = getPlacementFootprint(definition, rotateVertical);
            Vector3 buildingCenter = GetFootprintCenter(originCell, modelFootprint, grid, buildPlaneY);
            Quaternion placementRotation = rotateVertical ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            Vector3 runwayCenter = buildingCenter + placementRotation * ResolveRuntimeRunwayLocalPosition(definition);
            Quaternion runwayRotation = placementRotation * definition.RunwayLocalRotation;

            Vector3 halfExtents = definition.RunwayHalfExtents;
            Vector3[] corners =
            {
                runwayCenter + runwayRotation * new Vector3(-halfExtents.x, 0f, -halfExtents.z),
                runwayCenter + runwayRotation * new Vector3(-halfExtents.x, 0f, halfExtents.z),
                runwayCenter + runwayRotation * new Vector3(halfExtents.x, 0f, -halfExtents.z),
                runwayCenter + runwayRotation * new Vector3(halfExtents.x, 0f, halfExtents.z)
            };

            float minX = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxZ = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = corners[i] - (Vector3)grid.Origin;
                minX = Mathf.Min(minX, local.x);
                minZ = Mathf.Min(minZ, local.z);
                maxX = Mathf.Max(maxX, local.x);
                maxZ = Mathf.Max(maxZ, local.z);
            }

            int cellMinX = Mathf.FloorToInt(minX / grid.CellSize);
            int cellMinY = Mathf.FloorToInt(minZ / grid.CellSize);
            int cellMaxX = Mathf.CeilToInt(maxX / grid.CellSize);
            int cellMaxY = Mathf.CeilToInt(maxZ / grid.CellSize);
            if (cellMaxX <= cellMinX || cellMaxY <= cellMinY)
                return false;

            runwayRect = new RectInt(cellMinX, cellMinY, cellMaxX - cellMinX, cellMaxY - cellMinY);
            return true;
        }

        private static Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int footprintCells, GridConfig grid, float buildPlaneY)
        {
            return new Vector3(
                grid.Origin.x + (originCell.x + footprintCells.x * 0.5f) * grid.CellSize,
                buildPlaneY,
                grid.Origin.z + (originCell.y + footprintCells.y * 0.5f) * grid.CellSize);
        }

        private static RectInt UnionRects(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin);
            int yMin = Mathf.Min(a.yMin, b.yMin);
            int xMax = Mathf.Max(a.xMax, b.xMax);
            int yMax = Mathf.Max(a.yMax, b.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
