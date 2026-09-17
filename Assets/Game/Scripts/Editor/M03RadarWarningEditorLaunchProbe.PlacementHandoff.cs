using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static bool handoffChecked;
        private static string initialHandoffPose;
        private static void StageDistantPlacementCamera()
        {
            handoffChecked = false;
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>().MatchBootstrap;
            var source = FindPlacementSource(bootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,new HashSet<object>(),0);
            var camera = source.BuildingPlacementStartupSystemHelper.WorldCamera;
            camera.transform.position += new Vector3(160,0,-120);
            source.BuildingGameplayDependencyCompositionSystemHelper.SmoothMoveCameraGroundCenterTo(camera.transform.position + Vector3.right * 100);
        }
        private static void AssertInitialPlacementHandoff(MatchBootstrapCompositionSystemHelper bootstrap)
        {
            if (handoffChecked && placementDragPhase > 0) return;
            var source = FindPlacementSource(bootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,new HashSet<object>(),0);
            var placement = source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
            if (placement == null || !ReadGrid(source,out _,out var grid,out _,out _)) throw new InvalidOperationException("Missing preview after drawer closed.");
            var camera = source.BuildingPlacementStartupSystemHelper.WorldCamera;
            var size = source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(placement.Definition,placement.AutoRotateVertical);
            var center = source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(placement.OriginCell,size,grid,source.BuildingPlacementStartupSystemHelper.BuildPlaneY);
            var expected = ((IBuildingPlacementViewportQuery)source.BuildingGameplayDependencyCompositionSystemHelper.MainMenuPlayUi).GetPlacementViewportCenter(placement.IsValid);
            var actual = camera.WorldToViewportPoint(center);
            string pose = " origin="+placement.OriginCell+" center="+center+" camera="+camera.transform.position+" angles="+camera.transform.eulerAngles+" fov="+camera.fieldOfView;
            if (!handoffChecked) initialHandoffPose=pose;
            if (actual.z <= 0 || Vector2.Distance(actual,expected) > .025f)
                throw new InvalidOperationException("Initial preview is not centered in playable viewport: " + actual + " expected=" + expected+" initial="+initialHandoffPose+" now="+pose);
            Debug.Log("[M03PlacementHandoff] result=Passed distant camera + stale focus -> initial preview centered " + actual);
            handoffChecked = true;
        }
    }
}
