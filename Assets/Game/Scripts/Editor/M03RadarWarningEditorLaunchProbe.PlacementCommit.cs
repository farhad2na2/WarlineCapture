using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Components;
using Game.Runtime;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static BuildingGameplaySourceCompositionSystemHelper commitSource;
        private static GameObject commitPreview;
        private static Vector2Int commitOrigin;
        private static Vector3 commitPosition, commitVisualPosition;
        private static Quaternion commitRotation, commitVisualRotation;

        private static void CapturePreviewPoseForCommit()
        {
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>().MatchBootstrap;
            commitSource = FindPlacementSource(bootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement, new HashSet<object>(), 0);
            var placement = commitSource.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
            commitPreview = placement.PreviewInstance;
            commitOrigin = placement.OriginCell;
            commitPosition = commitPreview.transform.position;
            commitRotation = commitPreview.transform.rotation;
            var visual = commitPreview.transform.GetChild(0);
            commitVisualPosition = visual.position;
            commitVisualRotation = visual.rotation;
            Debug.Log("[M03PlacementCommit] visible=" + commitOrigin + " pointerSnapshot=" + placement.CommittedOriginCell);
        }

        private static void AssertCommittedPreviewPose()
        {
            if (commitPreview == null) throw new InvalidOperationException("Confirmed preview was discarded.");
            RuntimeBuildingEntity built = null;
            foreach (var building in commitSource.RuntimeBuildingSystem.Buildings.Values)
                if (building.Instance == commitPreview) { built = building; break; }
            if (built == null || built.OriginCell != commitOrigin)
                throw new InvalidOperationException("Registered building does not match the visible preview cell: " + built?.OriginCell + " expected=" + commitOrigin);
            var root = commitPreview.transform;
            var visual = root.GetChild(0);
            // Registration applies the independently sampled terrain foundation height.
            // It must not change the road coordinates, yaw, or the model's relative pose.
            var expectedPosition = commitPosition;
            var expectedVisualPosition = commitVisualPosition;
            if (commitSource.BuildingEntityManagerAccessSystem.TryGetEntityManager(out var em) &&
                em.HasComponent<BuildingSurfaceComponent>(built.CombatEntity))
            {
                expectedPosition.y = em.GetComponentData<BuildingSurfaceComponent>(built.CombatEntity).FoundationHeight;
                expectedVisualPosition.y += expectedPosition.y - commitPosition.y;
            }
            if (Vector3.Distance(root.position, expectedPosition) > .001f || Quaternion.Angle(root.rotation, commitRotation) > .01f ||
                Vector3.Distance(visual.position, expectedVisualPosition) > .001f || Quaternion.Angle(visual.rotation, commitVisualRotation) > .01f)
                throw new InvalidOperationException("Confirm moved or rotated the visible building: " + root.position + " expected=" + commitPosition);
            Debug.Log("[M03PlacementCommit] result=Passed registered cell, root and model pose match preview: " + commitOrigin + " position=" + root.position);
        }
    }
}
