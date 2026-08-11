using System;
using System.Collections.Generic;
using Game.Configs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Runtime
{
    internal sealed class RuntimeCityAlgorithmicAftermathPresentationSystemHelper
    {
        private const int PlacementAttemptsPerItem = 12;
        private readonly List<Bounds> _protectedBounds = new();
        private Transform _root;

        public int DressingCount { get; private set; }
        public int GroupCount { get; private set; }
        public int RejectedPlacementCount { get; private set; }
        public int FallbackAnchorCount { get; private set; }
        public float MaximumPlanarExtent { get; private set; }

        public void CreateGroupedDressing(
            RuntimeOperationMapAlgorithmicAftermathSettings settings,
            IReadOnlyList<GameObject> damagePrefabs,
            uint seed,
            Transform visualRoot,
            Transform roadVisualRoot,
            Transform parent)
        {
            CreateGroupedDressing(
                settings,
                damagePrefabs,
                seed,
                Vector3.zero,
                1f,
                visualRoot,
                roadVisualRoot,
                parent);
        }

        public void CreateGroupedDressing(
            RuntimeOperationMapAlgorithmicAftermathSettings settings,
            IReadOnlyList<GameObject> damagePrefabs,
            uint seed,
            Vector3 cityCenter,
            float roadCellWorldSize,
            Transform visualRoot,
            Transform roadVisualRoot,
            Transform parent)
        {
            if (!settings.IsConfigured || damagePrefabs == null || visualRoot == null || parent == null)
                return;

            HashSet<string> damageNames = CreateDamagePrefabNameSet(damagePrefabs);
            List<Vector3> anchors = CollectDamageAnchors(visualRoot, damageNames);
            if (anchors.Count == 0 &&
                settings.FallbackAnchorSpacingInRoadCells <= 0f)
            {
                Debug.LogWarning(
                    $"[RuntimeCityAftermathDressing] result=Skipped reason=noDamageAnchors seed={seed}");
                return;
            }

            CollectProtectedBounds(visualRoot);
            CollectProtectedBounds(roadVisualRoot);

            var rootObject = new GameObject(settings.GroupName);
            _root = rootObject.transform;
            _root.SetParent(parent, false);
            EnsureMinimumAnchors(
                settings,
                cityCenter,
                roadCellWorldSize,
                anchors);
            if (anchors.Count == 0)
                return;

            uint combinedSeed = seed ^ settings.SeedOffset;
            var rng = new Random(combinedSeed == 0u ? 1u : combinedSeed);
            int requestedGroups = Mathf.Min(settings.MaxAnchorGroups, anchors.Count);
            for (int groupIndex = 0; groupIndex < requestedGroups; groupIndex++)
            {
                int anchorIndex = Mathf.Min(
                    anchors.Count - 1,
                    Mathf.FloorToInt((groupIndex + 0.5f) * anchors.Count / requestedGroups));
                Vector3 anchorPosition = anchors[anchorIndex];
                var groupObject = new GameObject($"AftermathGroup_{groupIndex:00}");
                Transform groupRoot = groupObject.transform;
                groupRoot.SetParent(_root, false);

                int groupDressingCount = 0;
                for (int itemIndex = 0; itemIndex < settings.ItemsPerGroup; itemIndex++)
                {
                    if (TryCreateDressing(
                            settings,
                            anchorPosition,
                            groupRoot,
                            groupIndex,
                            itemIndex,
                            parent.position.y,
                            ref rng,
                            out Bounds acceptedBounds))
                    {
                        _protectedBounds.Add(acceptedBounds);
                        MaximumPlanarExtent = Mathf.Max(
                            MaximumPlanarExtent,
                            acceptedBounds.size.x,
                            acceptedBounds.size.z);
                        DressingCount++;
                        groupDressingCount++;
                    }
                }

                if (groupDressingCount > 0)
                    GroupCount++;
                else
                    DestroyObject(groupObject);
            }

            Debug.Log(
                $"[RuntimeCityAftermathDressing] result=Completed seed={seed} anchors={anchors.Count} " +
                $"fallbackAnchors={FallbackAnchorCount} " +
                $"groups={GroupCount}/{requestedGroups} dressing={DressingCount}/{settings.RequestedItemCount} " +
                $"rejected={RejectedPlacementCount} maxPlanarExtent={MaximumPlanarExtent:0.0} " +
                $"exposure={settings.ExposureDirection.x:0.00},{settings.ExposureDirection.y:0.00}/{settings.ExposureArcDegrees:0}");
        }

        public void Dispose()
        {
            if (_root != null)
                DestroyObject(_root.gameObject);

            _protectedBounds.Clear();
            _root = null;
            DressingCount = 0;
            GroupCount = 0;
            RejectedPlacementCount = 0;
            FallbackAnchorCount = 0;
            MaximumPlanarExtent = 0f;
        }

        private bool TryCreateDressing(
            RuntimeOperationMapAlgorithmicAftermathSettings settings,
            Vector3 anchorPosition,
            Transform groupRoot,
            int groupIndex,
            int itemIndex,
            float groundY,
            ref Random rng,
            out Bounds acceptedBounds)
        {
            acceptedBounds = default;
            for (int attempt = 0; attempt < PlacementAttemptsPerItem; attempt++)
            {
                GameObject prefab = settings.DressingPrefabs[
                    rng.NextInt(0, settings.DressingPrefabs.Count)];
                if (prefab == null)
                    continue;

                float angle = GetPlacementAngle(settings, ref rng);
                float radialT = math.sqrt(rng.NextFloat());
                float radius = math.lerp(settings.MinRadius, settings.MaxRadius, radialT);
                Vector3 position = anchorPosition + new Vector3(
                    math.cos(angle) * radius,
                    0f,
                    math.sin(angle) * radius);
                float yaw = rng.NextInt(0, 24) * 15f;
                float scale = rng.NextFloat(settings.MinScale, settings.MaxScale);

                GameObject instance = UnityEngine.Object.Instantiate(prefab, groupRoot);
                instance.name = $"{prefab.name}_Aftermath_{groupIndex:00}_{itemIndex:00}";
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
                instance.transform.localScale = Vector3.one * scale;
                DisableColliders(instance);

                if (!TryGetRendererBounds(instance.transform, out Bounds bounds))
                {
                    Reject(instance);
                    continue;
                }

                instance.transform.position += Vector3.up * (groundY - bounds.min.y);
                if (!TryGetRendererBounds(instance.transform, out bounds) ||
                    OverlapsProtectedGeometry(bounds))
                {
                    Reject(instance);
                    continue;
                }

                acceptedBounds = bounds;
                return true;
            }

            return false;
        }

        private static float GetPlacementAngle(
            RuntimeOperationMapAlgorithmicAftermathSettings settings,
            ref Random rng)
        {
            Vector2 direction = settings.ExposureDirection;
            if (direction == Vector2.zero)
                return rng.NextFloat(0f, math.PI * 2f);

            float centerAngle = math.atan2(direction.y, direction.x);
            float halfArc = math.radians(settings.ExposureArcDegrees * 0.5f);
            return centerAngle + rng.NextFloat(-halfArc, halfArc);
        }

        private void Reject(GameObject instance)
        {
            RejectedPlacementCount++;
            if (instance != null)
                instance.SetActive(false);
            DestroyObject(instance);
        }

        private bool OverlapsProtectedGeometry(Bounds candidate)
        {
            candidate.Expand(0.5f);
            for (int i = 0; i < _protectedBounds.Count; i++)
            {
                if (_protectedBounds[i].Intersects(candidate))
                    return true;
            }

            return false;
        }

        private void CollectProtectedBounds(Transform root)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                    _protectedBounds.Add(renderer.bounds);
            }
        }

        private void EnsureMinimumAnchors(
            RuntimeOperationMapAlgorithmicAftermathSettings settings,
            Vector3 cityCenter,
            float roadCellWorldSize,
            List<Vector3> anchors)
        {
            float cellSize = Mathf.Max(0.1f, roadCellWorldSize);
            float spacing = settings.FallbackAnchorSpacingInRoadCells * cellSize;
            if (spacing <= 0f)
                return;

            int realAnchorCapacity = settings.MaxAnchorGroups - settings.MinimumAuthoredAnchorGroups;
            TrimAnchorsToCapacity(anchors, realAnchorCapacity);
            if (anchors.Count >= settings.MaxAnchorGroups)
                return;

            Vector2 exposure = settings.ExposureDirection;
            if (exposure == Vector2.zero)
                exposure = Vector2.down;
            Vector2 tangent = new(-exposure.y, exposure.x);
            Vector2 centerOffset = settings.FallbackCenterOffsetInRoadCells * cellSize;
            Vector3 fallbackCenter = cityCenter + new Vector3(centerOffset.x, 0f, centerOffset.y);
            int candidateLimit = settings.MaxAnchorGroups * 3;
            float minimumDistance = spacing * 0.35f;
            for (int candidateIndex = 0;
                 candidateIndex < candidateLimit && anchors.Count < settings.MaxAnchorGroups;
                 candidateIndex++)
            {
                int slot = GetCenteredSlot(candidateIndex % settings.MaxAnchorGroups, settings.MaxAnchorGroups);
                int row = candidateIndex / settings.MaxAnchorGroups;
                float centeredSlot = slot - ((settings.MaxAnchorGroups - 1) * 0.5f);
                Vector2 offset = tangent * (centeredSlot * spacing) +
                                 exposure * (row * spacing * 0.6f);
                Vector3 candidate = fallbackCenter + new Vector3(offset.x, 0f, offset.y);
                if (IsNearExistingAnchor(candidate, minimumDistance, anchors))
                    continue;

                anchors.Add(candidate);
                FallbackAnchorCount++;
            }

            anchors.Sort(CompareAnchors);
        }

        private static void TrimAnchorsToCapacity(List<Vector3> anchors, int capacity)
        {
            capacity = Mathf.Max(0, capacity);
            if (anchors.Count <= capacity)
                return;
            if (capacity == 0)
            {
                anchors.Clear();
                return;
            }

            int originalCount = anchors.Count;
            for (int selectedIndex = 0; selectedIndex < capacity; selectedIndex++)
            {
                int sourceIndex = Mathf.Min(
                    originalCount - 1,
                    Mathf.FloorToInt((selectedIndex + 0.5f) * originalCount / capacity));
                anchors[selectedIndex] = anchors[sourceIndex];
            }

            anchors.RemoveRange(capacity, anchors.Count - capacity);
        }

        private static int GetCenteredSlot(int order, int slotCount)
        {
            int centerLeft = (slotCount - 1) / 2;
            if ((order & 1) == 0)
                return centerLeft - (order / 2);

            return centerLeft + 1 + (order / 2);
        }

        private static bool IsNearExistingAnchor(
            Vector3 candidate,
            float minimumDistance,
            List<Vector3> anchors)
        {
            float minimumDistanceSquared = minimumDistance * minimumDistance;
            for (int i = 0; i < anchors.Count; i++)
            {
                float deltaX = anchors[i].x - candidate.x;
                float deltaZ = anchors[i].z - candidate.z;
                if ((deltaX * deltaX) + (deltaZ * deltaZ) < minimumDistanceSquared)
                    return true;
            }

            return false;
        }

        private static List<Vector3> CollectDamageAnchors(
            Transform visualRoot,
            HashSet<string> damageNames)
        {
            var anchors = new List<Vector3>();
            for (int i = 0; i < visualRoot.childCount; i++)
            {
                Transform candidate = visualRoot.GetChild(i);
                string prefabName = candidate.name.EndsWith("_Visual", StringComparison.Ordinal)
                    ? candidate.name.Substring(0, candidate.name.Length - "_Visual".Length)
                    : candidate.name;
                if (damageNames.Contains(prefabName))
                    anchors.Add(candidate.position);
            }

            anchors.Sort(CompareAnchors);
            return anchors;
        }

        private static int CompareAnchors(Vector3 left, Vector3 right)
        {
            int comparison = left.x.CompareTo(right.x);
            if (comparison != 0)
                return comparison;

            return left.z.CompareTo(right.z);
        }

        private static HashSet<string> CreateDamagePrefabNameSet(
            IReadOnlyList<GameObject> prefabs)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < prefabs.Count; i++)
            {
                GameObject prefab = prefabs[i];
                if (prefab == null ||
                    prefab.name.IndexOf("ClothCover", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prefab.name.IndexOf("Archway", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                names.Add(prefab.name);
            }

            return names;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;
            bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return initialized;
        }

        private static void DisableColliders(GameObject instance)
        {
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
