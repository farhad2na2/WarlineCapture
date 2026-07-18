using Game.Configs;
using Game.Components;
using Game.Editor;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Editor
{
    public sealed class TransportPlaneRunwayAlignmentTests
    {
        private const string TransportPlanePrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab";
        private const string MapVehiclePlacementConfigPath = "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset";
        private const string MapBuildingPlacementConfigPath = "Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset";
        private const string OperationMapScenePath = OperationMapCurrentCompatibilitySceneStager.DestinationScenePath;
        private const float RootCenterTolerance = 0.25f;
        private const float RunwayCenterlineTolerance = 0.35f;

        public static void RunFocusedValidation()
        {
            TransportPlaneRunwayAlignmentTests tests = new();
            tests.TransportPlaneVisualBoundsAreCenteredOnRunwayRoot();
            tests.BakedTransportPlanePlacementsUseRunwayRootAsWorldCenter();
            tests.MapAuthoredAirportRunwayWorldDataUsesLiveRunwayMarkers();
            Debug.Log("[TransportPlaneRunwayAlignmentValidation] result=Passed tests=3");
        }

        [Test]
        public void TransportPlaneVisualBoundsAreCenteredOnRunwayRoot()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TransportPlanePrefabPath);
            Assert.IsNotNull(prefab, $"Missing transport plane prefab at {TransportPlanePrefabPath}.");

            Transform model = prefab.transform.Find("Model");
            Assert.IsNotNull(model, "Transport plane prefab must keep its visual model under a Model child.");
            Assert.IsTrue(
                TryGetCombinedLocalBounds(model, prefab.transform.worldToLocalMatrix, out Bounds bounds),
                "Transport plane Model child must have renderers.");

            Assert.That(
                bounds.center.x,
                Is.InRange(-RootCenterTolerance, RootCenterTolerance),
                $"Transport plane visual bounds must be centered on the unit root X axis. Current center={bounds.center}.");
            Assert.That(
                bounds.center.z,
                Is.InRange(-RootCenterTolerance, RootCenterTolerance),
                $"Transport plane visual bounds must be centered on the unit root Z axis. Current center={bounds.center}.");
        }

        [Test]
        public void BakedTransportPlanePlacementsUseRunwayRootAsWorldCenter()
        {
            MapVehiclePlacementConfig config =
                AssetDatabase.LoadAssetAtPath<MapVehiclePlacementConfig>(MapVehiclePlacementConfigPath);
            Assert.IsNotNull(config, $"Missing map vehicle placement config at {MapVehiclePlacementConfigPath}.");

            int checkedEntries = 0;
            foreach (MapVehiclePlacementConfigEntry placement in config.Placements)
            {
                if (placement == null || placement.Category != "Unit_Veh_Plane_Transport")
                    continue;

                checkedEntries++;
                Vector3 delta = placement.WorldCenter - placement.WorldPosition;
                delta.y = 0f;
                Assert.That(
                    delta.magnitude,
                    Is.LessThanOrEqualTo(RootCenterTolerance),
                    $"Transport plane placement should use the runway/root position as its WorldCenter. Source={placement.SourcePath} delta={delta}.");
            }

            Assert.Greater(checkedEntries, 0, "Expected at least one baked transport plane placement.");
        }

        [Test]
        public void MapAuthoredAirportRunwayWorldDataUsesLiveRunwayMarkers()
        {
            MapBuildingPlacementConfig config =
                AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(MapBuildingPlacementConfigPath);
            Assert.IsNotNull(config, $"Missing map building placement config at {MapBuildingPlacementConfigPath}.");

            Scene scene = SceneManager.GetSceneByPath(OperationMapScenePath);
            bool openedSceneForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedSceneForTest)
                scene = EditorSceneManager.OpenScene(OperationMapScenePath, OpenSceneMode.Additive);

            int checkedAirports = 0;
            try
            {
                foreach (MapBuildingPlacementConfigEntry placement in config.Placements)
                {
                    if (placement == null || placement.Category != "Building_Airport")
                        continue;

                    Assert.IsTrue(
                        TryResolveSceneSourceTransform(scene, placement.SourcePath, out Transform source),
                        $"Could not resolve map-authored airport source path {placement.SourcePath}.");

                    GameObject wrapper = CreateRuntimeMapAuthoredWrapper(placement, source);
                    try
                    {
                        Transform authoringBuildingsRoot = FindAncestorByName(source, "Buildings");
                        Assert.IsNotNull(authoringBuildingsRoot, $"Could not resolve authored Buildings root for {placement.SourcePath}.");
                        Assert.IsTrue(
                            MapBuildingPlacementSpawnPrefabSystemHelper.TryAttachMapRunwayAnchor(
                                authoringBuildingsRoot,
                                placement,
                                wrapper.transform,
                                Debug.LogWarning),
                            $"Map-authored airport must attach a live runway marker anchor. Source={placement.SourcePath}");

                        Assert.IsTrue(TryFindChildByName(wrapper.transform, "Runway_Start", out Transform runwayStart));
                        Assert.IsTrue(TryFindChildByName(wrapper.transform, "Runway_End", out Transform runwayEnd));

                        RuntimeBuildingEntity building = new()
                        {
                            Id = checkedAirports + 1,
                            Instance = wrapper,
                            Definition = new BuildingDefinition
                            {
                                HasRunway = true,
                                RunwayLocalPosition = new Vector3(1000f, 0f, 1000f),
                                RunwayLocalRotation = Quaternion.Euler(0f, 90f, 0f),
                                RunwayHalfExtents = new Vector3(8f, 0.5f, 24f)
                            }
                        };

                        Assert.IsTrue(
                            BuildingRunwaySystem.TryResolveRuntimeRunwayWorldData(
                                building,
                                out Vector3 runwayCenter,
                                out Quaternion runwayRotation,
                                out Vector3 halfExtents),
                            $"Map-authored airport runway world data should resolve from live runway markers. Source={placement.SourcePath}");

                        AssertRunwayMatchesMarkers(
                            placement.SourcePath,
                            runwayStart.position,
                            runwayEnd.position,
                            runwayCenter,
                            runwayRotation,
                            halfExtents);
                        checkedAirports++;
                    }
                    finally
                    {
                        Object.DestroyImmediate(wrapper);
                    }
                }
            }
            finally
            {
                if (openedSceneForTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            Assert.Greater(checkedAirports, 0, "Expected at least one map-authored airport placement.");
        }

        private static bool TryGetCombinedLocalBounds(Transform root, Matrix4x4 worldToLocal, out Bounds combinedBounds)
        {
            combinedBounds = default;
            if (root == null)
                return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Bounds localBounds = TransformBounds(worldToLocal * renderer.localToWorldMatrix, renderer.localBounds);
                if (!hasBounds)
                {
                    combinedBounds = localBounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(localBounds);
                }
            }

            return hasBounds;
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            Vector3 center = matrix.MultiplyPoint3x4(bounds.center);
            Vector3 extents = bounds.extents;

            Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));

            extents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(center, extents * 2f);
        }

        private static GameObject CreateRuntimeMapAuthoredWrapper(
            MapBuildingPlacementConfigEntry placement,
            Transform source)
        {
            GameObject wrapper = new($"{placement.BuildingPrefab.name}_MapVisualRoot_Test");
            wrapper.transform.SetPositionAndRotation(placement.WorldPosition, Quaternion.Euler(placement.WorldEulerAngles));
            wrapper.transform.localScale = placement.WorldScale;
            wrapper.AddComponent<MapAuthoredBuildingVisualComponent>();

            GameObject visual = Object.Instantiate(source.gameObject, wrapper.transform);
            visual.name = source.name;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            visual.SetActive(true);
            return wrapper;
        }

        private static void AssertRunwayMatchesMarkers(
            string sourcePath,
            Vector3 runwayStart,
            Vector3 runwayEnd,
            Vector3 runwayCenter,
            Quaternion runwayRotation,
            Vector3 halfExtents)
        {
            Vector3 markerDelta = runwayEnd - runwayStart;
            Vector3 markerDirection = new(markerDelta.x, 0f, markerDelta.z);
            Assert.Greater(markerDirection.sqrMagnitude, 0.001f, $"Runway markers must define a planar axis. Source={sourcePath}");
            markerDirection.Normalize();

            Vector3 markerCenter = (runwayStart + runwayEnd) * 0.5f;
            Vector3 centerDelta = runwayCenter - markerCenter;
            centerDelta.y = 0f;
            Vector3 lateral = centerDelta - markerDirection * Vector3.Dot(centerDelta, markerDirection);
            Assert.That(
                lateral.magnitude,
                Is.LessThanOrEqualTo(RunwayCenterlineTolerance),
                $"Resolved runway center must stay on the live marker centerline. Source={sourcePath} center={runwayCenter} markerCenter={markerCenter} lateral={lateral}");

            Vector3 resolvedDirection = runwayRotation * Vector3.forward;
            resolvedDirection.y = 0f;
            resolvedDirection.Normalize();
            Assert.That(
                Mathf.Abs(Vector3.Dot(markerDirection, resolvedDirection)),
                Is.GreaterThan(0.999f),
                $"Resolved runway rotation must match the live marker axis. Source={sourcePath}");

            Assert.That(
                halfExtents.z,
                Is.GreaterThanOrEqualTo(markerDelta.magnitude * 0.5f - 0.5f),
                $"Resolved runway length must cover the marker span. Source={sourcePath}");
        }

        private static bool TryResolveSceneSourceTransform(Scene scene, string sourcePath, out Transform source)
        {
            source = null;
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(sourcePath))
                return false;

            string[] segments = sourcePath.Split('/');
            if (segments.Length == 0)
                return false;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (!string.Equals(roots[i].name, segments[0], System.StringComparison.Ordinal))
                    continue;

                Transform current = roots[i].transform;
                for (int segmentIndex = 1; segmentIndex < segments.Length && current != null; segmentIndex++)
                    current = FindDirectChildByName(current, segments[segmentIndex]);

                if (current == null)
                    continue;

                source = current;
                return true;
            }

            return false;
        }

        private static Transform FindDirectChildByName(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && string.Equals(child.name, childName, System.StringComparison.Ordinal))
                    return child;
            }

            return null;
        }

        private static Transform FindAncestorByName(Transform transform, string ancestorName)
        {
            Transform current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, ancestorName, System.StringComparison.Ordinal))
                    return current;

                current = current.parent;
            }

            return null;
        }

        private static bool TryFindChildByName(Transform root, string childName, out Transform child)
        {
            child = null;
            if (root == null)
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
    }
}
