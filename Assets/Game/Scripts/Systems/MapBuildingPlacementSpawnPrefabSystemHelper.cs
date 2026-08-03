using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed class MapBuildingPlacementSpawnPrefabSystemHelper
    {
        private const int MaxPlacementsPerUpdate = 32;
        private const string AirportCategory = "Building_Airport";
        private const string RunwaysRootName = "Runways";
        private const string RunwayAnchorName = "Runway";
        private const string RunwayStartName = "Runway_Start";
        private const string RunwayEndName = "Runway_End";

        public delegate bool TryGetGridDataDelegate(
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData);

        public readonly struct Context
        {
            public readonly MapBuildingPlacementConfig Config;
            public readonly Transform AuthoringBuildingsRoot;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper RuntimeSpawnSystem;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.Context RuntimeSpawnContext;
            public readonly TryGetGridDataDelegate TryGetGridData;
            public readonly Action<string> LogWarning;

            public Context(
                MapBuildingPlacementConfig config,
                Transform authoringBuildingsRoot,
                BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
                BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
                TryGetGridDataDelegate tryGetGridData,
                Action<string> logWarning)
            {
                Config = config;
                AuthoringBuildingsRoot = authoringBuildingsRoot;
                RuntimeSpawnSystem = runtimeSpawnSystem;
                RuntimeSpawnContext = runtimeSpawnContext;
                TryGetGridData = tryGetGridData;
                LogWarning = logWarning;
            }
        }

        private bool _queued;
        private bool _authoringHidden;
        private bool _warnedMissingConfig;
        private int _nextPlacementIndex;
        private bool _warnedFailedPlacement;
        private bool _priorityPlayerProductionQueued;
        private readonly HashSet<int> _spawnedPlacementIndices = new();

        public bool IsComplete => _queued && _authoringHidden;

        public bool IsCompleteFor(MapBuildingPlacementConfig config, Transform authoringRoot)
        {
            if (config == null || !config.SpawnOnMatchStart)
                return true;

            bool authoringHidden =
                !config.HideAuthoringVisualsAfterSpawn ||
                authoringRoot == null ||
                _authoringHidden ||
                !authoringRoot.gameObject.activeInHierarchy;
            return _queued && authoringHidden;
        }

        public void Update(Context context)
        {
            if (context.Config == null || !context.Config.SpawnOnMatchStart || IsComplete)
                return;

            if (_queued)
            {
                HideAuthoringVisuals(context);
                return;
            }

            SpawnPlacements(context);
        }

        private void SpawnPlacements(Context context)
        {
            if (context.Config.Placements == null || context.Config.Placements.Count == 0)
            {
                WarnOnce(ref _warnedMissingConfig, context, "[MapBuildingPlacement] no baked map building placements configured.");
                _queued = true;
                return;
            }

            if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
                return;

            if (!_priorityPlayerProductionQueued)
            {
                SpawnPriorityPlayerProductionPlacements(context, grid);
                _priorityPlayerProductionQueued = true;
            }

            int processed = 0;
            for (; _nextPlacementIndex < context.Config.Placements.Count && processed < MaxPlacementsPerUpdate; _nextPlacementIndex++, processed++)
            {
                if (_spawnedPlacementIndices.Contains(_nextPlacementIndex))
                    continue;

                TrySpawnPlacementAtIndex(context, grid, _nextPlacementIndex);
            }

            if (_nextPlacementIndex >= context.Config.Placements.Count)
            {
                _queued = true;
                HideAuthoringVisuals(context);
            }
        }

        private void SpawnPriorityPlayerProductionPlacements(Context context, GridConfig grid)
        {
            if (context.Config?.Placements == null)
                return;

            for (int i = 0; i < context.Config.Placements.Count; i++)
            {
                if (_spawnedPlacementIndices.Contains(i))
                    continue;

                MapBuildingPlacementConfigEntry placement = context.Config.Placements[i];
                if (!IsPlayerProductionPlacement(context, placement))
                    continue;

                if (TrySpawnPlacementAtIndex(context, grid, i))
                    _spawnedPlacementIndices.Add(i);
            }
        }

        private bool TrySpawnPlacementAtIndex(Context context, GridConfig grid, int placementIndex)
        {
            MapBuildingPlacementConfigEntry placement = context.Config.Placements[placementIndex];
            if (placement == null || placement.BuildingPrefab == null)
                return false;

            if (!context.RuntimeSpawnSystem.TryGetRuntimeBuildingPlacementFootprint(
                    context.RuntimeSpawnContext,
                    placement.BuildingPrefab,
                    placement.RotateVertical,
                    out Vector2Int footprint))
            {
                context.LogWarning?.Invoke($"[MapBuildingPlacement] skipped {placement.SourcePath}: could not resolve footprint for {placement.BuildingPrefab.name}.");
                return false;
            }

            Vector3 worldCenter = placement.WorldCenter;
            int2 centerCell = GridUtils.WorldToCell(grid, new float3(worldCenter.x, worldCenter.y, worldCenter.z));
            int2 originCell = CenterCellToOrigin(centerCell, footprint, grid);
            if (TrySpawnAuthoredPlacement(context, placement, new Vector2Int(originCell.x, originCell.y), footprint))
                return true;

            WarnOnce(
                ref _warnedFailedPlacement,
                context,
                $"[MapBuildingPlacement] at least one authored building failed to register. First failed source={placement.SourcePath} prefab={placement.BuildingPrefab.name}.");
            return false;
        }

        private static bool IsPlayerProductionPlacement(Context context, MapBuildingPlacementConfigEntry placement)
        {
            if (placement == null ||
                placement.FactionId != FactionIdentity.PlayerFactionId ||
                placement.BuildingPrefab == null ||
                context.RuntimeSpawnContext.DefinitionSystem == null)
            {
                return false;
            }

            BuildingDefinition definition = context.RuntimeSpawnContext.DefinitionSystem.CreateRuntimeBuildingDefinition(
                placement.BuildingPrefab,
                placement.BuildingPrefab.name,
                "Authored map building.",
                new Vector2Int(10, 10),
                500,
                context.RuntimeSpawnContext.RunwaySystem);
            return BuildingDefinitionPrefabSystemHelper.GetProductionCount(definition) > 0;
        }

        private static bool TrySpawnAuthoredPlacement(
            Context context,
            MapBuildingPlacementConfigEntry placement,
            Vector2Int originCell,
            Vector2Int footprint)
        {
            BuildingRuntimeSpawnCompositionSystemHelper.Context spawnContext = context.RuntimeSpawnContext;
            if (spawnContext.DefinitionSystem == null ||
                spawnContext.RegisterRuntimeBuilding == null)
            {
                return false;
            }

            BuildingDefinition definition = spawnContext.DefinitionSystem.CreateRuntimeBuildingDefinition(
                placement.BuildingPrefab,
                placement.BuildingPrefab.name,
                "Authored map building.",
                footprint,
                500,
                spawnContext.RunwaySystem);
            GameObject instance = CreateAuthoredMapVisualInstance(context, placement, spawnContext.BuildingRoot);
            if (instance == null)
                return false;

            RuntimeBuildingEntity building = spawnContext.RegisterRuntimeBuilding(
                BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint(definition, footprint),
                instance,
                originCell,
                true);
            if (building == null)
                return false;

            spawnContext.SetRuntimeBuildingOwnerFaction?.Invoke(building, placement.FactionId);
            return true;
        }

        internal static GameObject CreateAuthoredMapVisualInstance(
            Context context,
            MapBuildingPlacementConfigEntry placement,
            Transform parent)
        {
            if (placement == null || placement.BuildingPrefab == null)
                return null;

            bool hasAuthoringVisual = TryResolveAuthoringTransform(
                context.AuthoringBuildingsRoot,
                placement,
                out Transform source);

            GameObject wrapper = new GameObject($"{placement.BuildingPrefab.name}_MapVisualRoot");
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.SetPositionAndRotation(placement.WorldPosition, Quaternion.Euler(placement.WorldEulerAngles));
            wrapper.transform.localScale = placement.WorldScale;
            MapAuthoredBuildingVisualComponent authoredVisual =
                wrapper.AddComponent<MapAuthoredBuildingVisualComponent>();
            if (TryCalculatePlacementPresentationSize(placement, out Vector3 presentationWorldSize))
            {
                authoredVisual.ConfigurePresentationGeometry(
                    placement.WorldCenter,
                    presentationWorldSize,
                    placement.YawDegrees);
            }
            else
            {
                authoredVisual.ConfigurePresentationWorldCenter(placement.WorldCenter);
            }

            bool useExistingStaticPresentation =
                !hasAuthoringVisual &&
                context.Config != null &&
                context.Config.UseExistingStaticPresentationWhenAuthoringVisualMissing;
            if (!useExistingStaticPresentation)
            {
                GameObject visual = hasAuthoringVisual
                    ? UnityEngine.Object.Instantiate(source.gameObject, wrapper.transform)
                    : UnityEngine.Object.Instantiate(placement.BuildingPrefab, wrapper.transform);
                visual.name = hasAuthoringVisual ? source.name : placement.BuildingPrefab.name;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                visual.SetActive(true);
            }
            TryAttachMapRunwayAnchor(context.AuthoringBuildingsRoot, placement, wrapper.transform, context.LogWarning);
            if (hasAuthoringVisual && context.Config.HideAuthoringVisualsAfterSpawn)
                source.gameObject.SetActive(false);

            return wrapper;
        }

        private static bool TryCalculatePlacementPresentationSize(
            MapBuildingPlacementConfigEntry placement,
            out Vector3 worldSize)
        {
            worldSize = default;
            GameObject prefab = placement?.BuildingPrefab;
            if (prefab == null)
                return false;

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds rootLocalBounds = default;
            Matrix4x4 rootWorldToLocal = prefab.transform.worldToLocalMatrix;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                    continue;

                Bounds localBounds = renderer.localBounds;
                Matrix4x4 rendererToRoot = rootWorldToLocal * renderer.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 local = new(
                        (corner & 1) == 0 ? localBounds.min.x : localBounds.max.x,
                        (corner & 2) == 0 ? localBounds.min.y : localBounds.max.y,
                        (corner & 4) == 0 ? localBounds.min.z : localBounds.max.z);
                    Vector3 rootLocal = rendererToRoot.MultiplyPoint3x4(local);
                    if (hasBounds)
                        rootLocalBounds.Encapsulate(rootLocal);
                    else
                    {
                        rootLocalBounds = new Bounds(rootLocal, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            if (!hasBounds)
                return false;

            Vector3 scale = placement.WorldScale;
            Vector3 size = rootLocalBounds.size;
            worldSize = new Vector3(
                Mathf.Abs(size.x * scale.x),
                Mathf.Abs(size.y * scale.y),
                Mathf.Abs(size.z * scale.z));
            return worldSize.x > 0.001f && worldSize.z > 0.001f;
        }

        internal static bool TryAttachMapRunwayAnchor(
            Transform authoringBuildingsRoot,
            MapBuildingPlacementConfigEntry placement,
            Transform wrapper,
            Action<string> logWarning = null)
        {
            if (!IsMapAirportPlacement(placement) || authoringBuildingsRoot == null || wrapper == null)
                return false;

            if (TryFindDescendantByName(wrapper, RunwayAnchorName, out _))
                return true;

            Transform mapRoot = authoringBuildingsRoot.parent;
            if (mapRoot == null)
            {
                logWarning?.Invoke($"[MapBuildingPlacement] airport {placement.SourcePath} could not resolve map root for runway alignment.");
                return false;
            }

            Transform runwaysRoot = FindDirectChildByName(mapRoot, RunwaysRootName);
            if (runwaysRoot == null)
            {
                logWarning?.Invoke($"[MapBuildingPlacement] airport {placement.SourcePath} could not resolve {RunwaysRootName} root for runway alignment.");
                return false;
            }

            if (!TryResolveNearestMapRunwayWorldData(
                    runwaysRoot,
                    placement.WorldPosition,
                    out Vector3 runwayCenter,
                    out Quaternion runwayRotation,
                    out Vector3 runwayHalfExtents))
            {
                logWarning?.Invoke($"[MapBuildingPlacement] airport {placement.SourcePath} could not resolve a live runway surface under {GetHierarchyPath(runwaysRoot)}.");
                return false;
            }

            CreateRuntimeRunwayAnchor(wrapper, runwayCenter, runwayRotation, runwayHalfExtents);
            return true;
        }

        private static bool IsMapAirportPlacement(MapBuildingPlacementConfigEntry placement)
        {
            return placement != null &&
                (string.Equals(placement.Category, AirportCategory, StringComparison.Ordinal) ||
                 ContainsOrdinalIgnoreCase(placement.BuildingPrefab != null ? placement.BuildingPrefab.name : null, "Airport"));
        }

        private static bool TryResolveNearestMapRunwayWorldData(
            Transform runwaysRoot,
            Vector3 referencePosition,
            out Vector3 center,
            out Quaternion rotation,
            out Vector3 halfExtents)
        {
            center = Vector3.zero;
            rotation = Quaternion.identity;
            halfExtents = new Vector3(8f, 0.5f, 24f);
            if (runwaysRoot == null)
                return false;

            Renderer[] renderers = runwaysRoot.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !IsRunwaySurfaceRenderer(renderer))
                    continue;

                if (!TryResolveRunwayRendererWorldData(
                        renderer,
                        out Vector3 candidateCenter,
                        out Quaternion candidateRotation,
                        out Vector3 candidateHalfExtents))
                {
                    continue;
                }

                float distance = PlanarDistanceSquared(candidateCenter, referencePosition);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                center = candidateCenter;
                rotation = candidateRotation;
                halfExtents = candidateHalfExtents;
                found = true;
            }

            if (found)
                return true;

            return TryResolveRunwayGroupWorldData(runwaysRoot, referencePosition, out center, out rotation, out halfExtents);
        }

        private static bool TryResolveRunwayGroupWorldData(
            Transform runwaysRoot,
            Vector3 referencePosition,
            out Vector3 center,
            out Quaternion rotation,
            out Vector3 halfExtents)
        {
            center = Vector3.zero;
            rotation = Quaternion.identity;
            halfExtents = new Vector3(8f, 0.5f, 24f);
            bool found = false;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < runwaysRoot.childCount; i++)
            {
                Transform child = runwaysRoot.GetChild(i);
                if (child == null ||
                    !TryResolveCombinedRendererWorldData(
                        child,
                        out Vector3 candidateCenter,
                        out Quaternion candidateRotation,
                        out Vector3 candidateHalfExtents))
                {
                    continue;
                }

                float distance = PlanarDistanceSquared(candidateCenter, referencePosition);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                center = candidateCenter;
                rotation = candidateRotation;
                halfExtents = candidateHalfExtents;
                found = true;
            }

            return found;
        }

        private static bool TryResolveRunwayRendererWorldData(
            Renderer renderer,
            out Vector3 center,
            out Quaternion rotation,
            out Vector3 halfExtents)
        {
            center = Vector3.zero;
            rotation = Quaternion.identity;
            halfExtents = new Vector3(8f, 0.5f, 24f);
            if (renderer == null)
                return false;

            Bounds bounds = renderer.bounds;
            center = bounds.center;
            center.y = bounds.max.y;
            Transform transform = renderer.transform;
            Vector3 rightAxis = ResolvePlanarAxis(transform != null ? transform.right : Vector3.right, Vector3.right);
            Vector3 forwardAxis = ResolvePlanarAxis(transform != null ? transform.forward : Vector3.forward, Vector3.forward);
            ResolveRunwayAxesFromBounds(bounds, center, rightAxis, forwardAxis, out Vector3 lengthAxis, out float halfWidth, out float halfLength);
            if (halfLength <= 2f)
                return false;

            rotation = Quaternion.LookRotation(lengthAxis, Vector3.up);
            halfExtents = new Vector3(Mathf.Max(1f, halfWidth), Mathf.Max(0.5f, bounds.extents.y), halfLength);
            return true;
        }

        private static bool TryResolveCombinedRendererWorldData(
            Transform root,
            out Vector3 center,
            out Quaternion rotation,
            out Vector3 halfExtents)
        {
            center = Vector3.zero;
            rotation = Quaternion.identity;
            halfExtents = new Vector3(8f, 0.5f, 24f);
            if (root == null)
                return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds combinedBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsRunwayPropRenderer(renderer))
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
                return false;

            center = combinedBounds.center;
            center.y = combinedBounds.max.y;
            Vector3 rightAxis = ResolvePlanarAxis(root.right, Vector3.right);
            Vector3 forwardAxis = ResolvePlanarAxis(root.forward, Vector3.forward);
            ResolveRunwayAxesFromBounds(combinedBounds, center, rightAxis, forwardAxis, out Vector3 lengthAxis, out float halfWidth, out float halfLength);
            if (halfLength <= 2f)
                return false;

            rotation = Quaternion.LookRotation(lengthAxis, Vector3.up);
            halfExtents = new Vector3(Mathf.Max(1f, halfWidth), Mathf.Max(0.5f, combinedBounds.extents.y), halfLength);
            return true;
        }

        private static void ResolveRunwayAxesFromBounds(
            Bounds bounds,
            Vector3 center,
            Vector3 rightAxis,
            Vector3 forwardAxis,
            out Vector3 lengthAxis,
            out float halfWidth,
            out float halfLength)
        {
            float rightSpan = ResolveProjectedHalfSpan(bounds, center, rightAxis);
            float forwardSpan = ResolveProjectedHalfSpan(bounds, center, forwardAxis);
            if (rightSpan >= forwardSpan)
            {
                lengthAxis = rightAxis;
                halfLength = rightSpan;
                halfWidth = forwardSpan;
            }
            else
            {
                lengthAxis = forwardAxis;
                halfLength = forwardSpan;
                halfWidth = rightSpan;
            }
        }

        private static float ResolveProjectedHalfSpan(Bounds bounds, Vector3 center, Vector3 axis)
        {
            float halfSpan = 0f;
            EncapsulateProjectedBoundsCorner(bounds.min.x, bounds.min.y, bounds.min.z, center, axis, ref halfSpan);
            EncapsulateProjectedBoundsCorner(bounds.min.x, bounds.min.y, bounds.max.z, center, axis, ref halfSpan);
            EncapsulateProjectedBoundsCorner(bounds.min.x, bounds.max.y, bounds.min.z, center, axis, ref halfSpan);
            EncapsulateProjectedBoundsCorner(bounds.min.x, bounds.max.y, bounds.max.z, center, axis, ref halfSpan);
            EncapsulateProjectedBoundsCorner(bounds.max.x, bounds.min.y, bounds.min.z, center, axis, ref halfSpan);
            EncapsulateProjectedBoundsCorner(bounds.max.x, bounds.min.y, bounds.max.z, center, axis, ref halfSpan);
            EncapsulateProjectedBoundsCorner(bounds.max.x, bounds.max.y, bounds.min.z, center, axis, ref halfSpan);
            EncapsulateProjectedBoundsCorner(bounds.max.x, bounds.max.y, bounds.max.z, center, axis, ref halfSpan);
            return halfSpan;
        }

        private static void EncapsulateProjectedBoundsCorner(
            float x,
            float y,
            float z,
            Vector3 center,
            Vector3 axis,
            ref float halfSpan)
        {
            halfSpan = Mathf.Max(halfSpan, Mathf.Abs(Vector3.Dot(new Vector3(x, y, z) - center, axis)));
        }

        private static Vector3 ResolvePlanarAxis(Vector3 axis, Vector3 fallback)
        {
            axis.y = 0f;
            if (axis.sqrMagnitude <= 0.0001f)
                axis = fallback;

            axis.y = 0f;
            return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
        }

        private static void CreateRuntimeRunwayAnchor(
            Transform wrapper,
            Vector3 center,
            Quaternion rotation,
            Vector3 halfExtents)
        {
            GameObject runway = new(RunwayAnchorName);
            Transform runwayTransform = runway.transform;
            runwayTransform.SetParent(wrapper, true);
            runwayTransform.SetPositionAndRotation(center, rotation);

            GameObject start = new(RunwayStartName);
            start.transform.SetParent(runwayTransform, false);
            start.transform.localPosition = new Vector3(0f, 0f, -Mathf.Abs(halfExtents.z));
            start.transform.localRotation = Quaternion.identity;

            GameObject end = new(RunwayEndName);
            end.transform.SetParent(runwayTransform, false);
            end.transform.localPosition = new Vector3(0f, 0f, Mathf.Abs(halfExtents.z));
            end.transform.localRotation = Quaternion.identity;
        }

        private static bool IsRunwaySurfaceRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            string name = renderer.name;
            string path = GetHierarchyPath(renderer.transform);
            return ContainsOrdinalIgnoreCase(name, "SM_Env_Runway") ||
                   ContainsOrdinalIgnoreCase(path, "SM_Env_Runway") ||
                   (ContainsOrdinalIgnoreCase(name, "Runway") && !IsRunwayPropRenderer(renderer));
        }

        private static bool IsRunwayPropRenderer(Renderer renderer)
        {
            if (renderer == null)
                return true;

            string path = GetHierarchyPath(renderer.transform);
            return ContainsOrdinalIgnoreCase(path, "Light") ||
                   ContainsOrdinalIgnoreCase(path, "Barrier") ||
                   ContainsOrdinalIgnoreCase(path, "Prop_Runway");
        }

        private static float PlanarDistanceSquared(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static bool TryResolveAuthoringTransform(
            Transform authoringRoot,
            MapBuildingPlacementConfigEntry placement,
            out Transform source)
        {
            source = null;
            if (authoringRoot == null || placement == null)
                return false;

            string sourcePath = placement.SourcePath;
            if (!string.IsNullOrEmpty(sourcePath))
            {
                string[] segments = sourcePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                int startIndex = 0;
                for (int i = 0; i < segments.Length; i++)
                {
                    if (string.Equals(segments[i], authoringRoot.name, StringComparison.Ordinal))
                    {
                        startIndex = i + 1;
                        break;
                    }
                }

                Transform current = authoringRoot;
                bool resolved = true;
                for (int i = startIndex; i < segments.Length; i++)
                {
                    current = FindDirectChildByName(current, segments[i]);
                    if (current == null)
                    {
                        resolved = false;
                        break;
                    }
                }

                if (resolved && current != null)
                {
                    source = current;
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(placement.Category))
            {
                Transform category = FindDirectChildByName(authoringRoot, placement.Category);
                string leafName = GetLeafName(sourcePath);
                if (category != null && !string.IsNullOrEmpty(leafName) && TryFindDescendantByName(category, leafName, out source))
                {
                    return true;
                }
            }

            return !string.IsNullOrEmpty(sourcePath) &&
                TryFindDescendantByName(authoringRoot, GetLeafName(sourcePath), out source);
        }

        private static Transform FindDirectChildByName(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static bool TryFindDescendantByName(Transform root, string childName, out Transform result)
        {
            result = null;
            if (root == null || string.IsNullOrEmpty(childName))
                return false;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    result = child;
                    return true;
                }

                if (TryFindDescendantByName(child, childName, out result))
                    return true;
            }

            return false;
        }

        private static string GetLeafName(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return string.Empty;

            int index = sourcePath.LastIndexOf('/');
            return index >= 0 && index + 1 < sourcePath.Length
                ? sourcePath.Substring(index + 1)
                : sourcePath;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }

        private static bool ContainsOrdinalIgnoreCase(string value, string part)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void HideAuthoringVisuals(Context context)
        {
            if (_authoringHidden)
                return;

            if (!context.Config.HideAuthoringVisualsAfterSpawn || context.AuthoringBuildingsRoot == null)
            {
                _authoringHidden = true;
                return;
            }

            if (context.AuthoringBuildingsRoot.gameObject.activeSelf)
                context.AuthoringBuildingsRoot.gameObject.SetActive(false);

            _authoringHidden = true;
        }

        private static int2 CenterCellToOrigin(int2 centerCell, Vector2Int footprint, GridConfig grid)
        {
            int originX = centerCell.x - Mathf.Max(0, footprint.x - 1) / 2;
            int originY = centerCell.y - Mathf.Max(0, footprint.y - 1) / 2;
            return new int2(
                Mathf.Clamp(originX, 0, Mathf.Max(0, grid.Width - footprint.x)),
                Mathf.Clamp(originY, 0, Mathf.Max(0, grid.Height - footprint.y)));
        }

        private static void WarnOnce(ref bool flag, Context context, string message)
        {
            if (flag)
                return;

            flag = true;
            context.LogWarning?.Invoke(message);
        }
    }
}
