using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class TransportPlaneRunwayAlignmentTests
    {
        private const string TransportPlanePrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab";
        private const string MapVehiclePlacementConfigPath = "Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset";
        private const float RootCenterTolerance = 0.25f;

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
    }
}
