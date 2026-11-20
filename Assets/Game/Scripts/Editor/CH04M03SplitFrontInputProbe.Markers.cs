using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Editor
{
    public static partial class CH04M03SplitFrontInputProbe
    {
        private const string MarkerWidthKey = Active + ".MarkerWidth";
        private static int markerStage;
        private static double markerDue;
        private static double markerStageStarted;
        private static Entity markerActor;
        private static string markerFile;
        private static bool markerInputWaitLogged;
        private static int markerMoveAttempt;
        private static int markerShots;
        private static string markerProjectionFailure;
        private static readonly List<RaycastResult> MarkerUiHits = new();

        public static void RunMarkerCapture()
        {
            string width = Environment.GetEnvironmentVariable("WARLINE_SELECTION_MARKER_WIDTH");
            if (width != "1920" && width != "2400")
                throw new InvalidOperationException("Set WARLINE_SELECTION_MARKER_WIDTH to 1920 or 2400.");

            SessionState.SetInt(MarkerWidthKey, int.Parse(width));
            RunEnglish();
        }

        private static void AdvanceMarkerCapture(EntityManager em, Entity root)
        {
            int width = SessionState.GetInt(MarkerWidthKey, 0);
            if (width == 0)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (markerStageStarted == 0)
                markerStageStarted = now;
            if (now - markerStageStarted > 35)
            {
                string failure = $"Marker capture stalled at stage {markerStage} actor={markerActor}.";
                if (markerStage < 4)
                    throw new TimeoutException(failure);

                // A command capture failure should still leave the selection and
                // other command evidence available for review.
                Debug.LogWarning("[SelectionMarkerNativeCapture] state=Missing " + failure);
                markerProjectionFailure ??= failure;
                JumpMarkerStage(markerStage < 14 ? 14 : 18, 0);
            }
            if (now < markerDue || Screen.width != width || Screen.height != 1080 || Camera.main == null)
                return;

            if (UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) &&
                restrictions.CinematicInteractionLocked)
                return;

            smokeHand ??= new AriaTouchInputUiSystemHelper();
            if (!smokeHand.IsRunning && !smokeHand.Start())
            {
                if (!markerInputWaitLogged)
                    Debug.Log("[SelectionMarkerNativeInput] waiting for physical controls to be released");
                markerInputWaitLogged = true;
                return;
            }
            if (markerInputWaitLogged)
            {
                Debug.Log("[SelectionMarkerNativeInput] physical controls released, touch actuator running");
                markerInputWaitLogged = false;
            }
            if (smokeHand.IsBusy || !UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command))
                return;

            var mission = em.GetComponentData<CampaignMissionSplitFrontState>(root);
            if (!em.Exists(mission.Launcher) || !em.Exists(mission.Battery))
                return;

            switch (markerStage)
            {
                case 0:
                    markerActor = FindMarkerActor(em, root, mission.Launcher, vehicle: false);
                    if (FocusMarkerActor(em, markerActor))
                        NextMarkerStage(0.8);
                    break;
                case 1:
                    if (SelectMarkerActor(em, command.ActiveCommandMode) && WriteMarkerShot("infantry-selected", width))
                        NextMarkerStage(0.3);
                    break;
                case 2:
                    markerActor = FindMarkerActor(em, root, mission.Launcher, vehicle: true);
                    if (FocusMarkerActor(em, markerActor))
                        NextMarkerStage(0.8);
                    break;
                case 3:
                    if (SelectMarkerActor(em, command.ActiveCommandMode) && WriteMarkerShot("vehicle-selected", width))
                        NextMarkerStage(0.3);
                    break;
                case 4:
                    markerActor = mission.Launcher;
                    var launcherTutorial = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                    var launcherGuidance = em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
                    if ((launcherGuidance.AcknowledgedGuidanceMask & 1) == 0)
                        SmokeTap(launcherTutorial?.ContinueButton);
                    else if (SmokeTap(launcherTutorial?.ShowMeButton) || FocusMarkerActor(em, markerActor))
                        NextMarkerStage(2);
                    break;
                case 5:
                    if (SelectMarkerActor(em, command.ActiveCommandMode) && WriteMarkerShot("large-unit-selected", width))
                        NextMarkerStage(0.3);
                    break;
                case 6:
                    if (MarkerHudCamera()?.RequestZoomInLevel() == true)
                        NextMarkerStage(2);
                    break;
                case 7:
                    if (WriteMarkerShot("zoom-in", width))
                        NextMarkerStage(0.3);
                    break;
                case 8:
                    var zoom = MarkerHudCamera();
                    if (zoom != null && zoom.RequestZoomOutLevel() && zoom.RequestZoomOutLevel())
                        NextMarkerStage(2);
                    break;
                case 9:
                    if (WriteMarkerShot("zoom-out", width))
                        NextMarkerStage(0.3);
                    break;
                case 10:
                    var tutorial = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                    if (command.ActiveCommandMode != TacticalCommandMode.Attack)
                        SmokeTapNamed("AttackCommand");
                    else if (SmokeTap(tutorial?.ShowMeButton) || FocusMarkerActor(em, mission.Battery))
                        NextMarkerStage(2);
                    break;
                case 11:
                    if (GameObject.Find("AttackTargetPreviewMarkerRuntime") != null && WriteMarkerShot("attack-preview", width))
                        NextMarkerStage(0.3);
                    break;
                case 12:
                    if (TapLauncherWorld(em, mission.Battery))
                        NextMarkerStage(0);
                    break;
                case 13:
                    var launcher = em.GetComponentData<SplitFrontLauncherCommandState>(mission.Launcher);
                    if (launcher.CommandedTarget == mission.Battery &&
                        (GameObject.Find("AttackTargetSelectionMarkerRuntime") != null ||
                         GameObject.Find("AttackTargetFallbackReticleRuntime") != null) &&
                        WriteMarkerShot("attack-accepted", width))
                        NextMarkerStage(0.3);
                    break;
                case 14:
                    SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled = true;
                    markerActor = FindMarkerActor(em, root, mission.Launcher, vehicle: true);
                    if (FocusMarkerActor(em, markerActor))
                        NextMarkerStage(0.8);
                    break;
                case 15:
                    if (SelectMarkerActor(em, command.ActiveCommandMode))
                        NextMarkerStage(0.3);
                    break;
                case 16:
                    if (command.ActiveCommandMode != TacticalCommandMode.Move)
                        SmokeTapNamed("MoveCommand");
                    else if (TapMoveDestination(em))
                        NextMarkerStage(0);
                    break;
                case 17:
                    if (GameObject.Find("MoveOrderMarkerRuntime") != null && WriteMarkerShot("move-accepted", width))
                        NextMarkerStage(0.3);
                    else if (markerFile == null && now > markerStageStarted + 2 && markerMoveAttempt < 3)
                    {
                        markerMoveAttempt++;
                        JumpMarkerStage(16, 0);
                    }
                    break;
                case 18:
                    bool passed = markerShots == 8 && markerProjectionFailure == null;
                    Debug.Log($"[SelectionMarkerNativeCapture] result={(passed ? "Passed" : "Failed")} locale=en width={width} height=1080 shots={markerShots} failure={markerProjectionFailure}");
                    markerStage++;
                    Complete(passed, $"marker-capture locale=en width={width} runtime-touch=1 shots={markerShots}");
                    break;
            }
        }

        private static void NextMarkerStage(double wait) => JumpMarkerStage(markerStage + 1, wait);

        private static void JumpMarkerStage(int stage, double wait)
        {
            markerStage = stage;
            markerStageStarted = EditorApplication.timeSinceStartup;
            markerDue = markerStageStarted + wait;
            markerFile = null;
        }

        private static SelectionUiCameraSystemHelper MarkerHudCamera() =>
            UnityEngine.Object.FindAnyObjectByType<MatchSceneView>()?.MatchBootstrap?.SelectionUiCamera;

        private static Entity FindMarkerActor(EntityManager em, Entity root, Entity exclude, bool vehicle)
        {
            foreach (var member in em.GetBuffer<CampaignMissionDefenseMember>(root, true))
            {
                Entity actor = member.Entity;
                if (actor == exclude || member.FactionId != 1 || member.IsSensor != 0 ||
                    !em.Exists(actor) || !em.HasComponent<LocalTransform>(actor) ||
                    !em.HasComponent<UnitMovementBehavior>(actor) || !em.HasComponent<UnitHealth>(actor))
                    continue;
                if (em.GetComponentData<UnitHealth>(actor).Current > 0 &&
                    (em.GetComponentData<UnitMovementBehavior>(actor).UsesVehicleMotion != 0) == vehicle)
                    return actor;
            }
            return Entity.Null;
        }

        private static bool FocusMarkerActor(EntityManager em, Entity actor)
        {
            if (actor == Entity.Null || !em.HasComponent<LocalTransform>(actor))
                return false;

            var camera = em.World.GetExistingSystemManaged<RtsCameraRequestSystem>();
            Vector3 position = em.GetComponentData<LocalTransform>(actor).Position;
            return camera != null && camera.QueueMoveGroundCenterTo(em, position);
        }

        private static bool SelectMarkerActor(EntityManager em, TacticalCommandMode mode)
        {
            if (mode != TacticalCommandMode.Select)
            {
                SmokeTapNamed("SelectCommand");
                return false;
            }
            if (!em.HasComponent<SelectedUnitTag>(markerActor))
            {
                TapLauncherWorld(em, markerActor);
                return false;
            }
            return em.HasComponent<UnitSelectionMarkerInstanceReference>(markerActor);
        }

        private static bool TapMoveDestination(EntityManager em)
        {
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent));
            if (grids.CalculateEntityCount() != 1 || !em.HasComponent<LocalTransform>(markerActor))
                return false;

            Entity gridEntity = grids.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            float3 source = em.GetComponentData<LocalTransform>(markerActor).Position;
            int2 center = (int2)math.floor((source.xz - grid.Origin.xz) / grid.CellSize);
            var walkable = em.GetBuffer<GridWalkable>(gridEntity, true);
            var blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
            int distance = 12 + markerMoveAttempt * 3;
            foreach (int2 delta in new[] { new int2(distance, 0), new int2(-distance, 0), new int2(0, distance), new int2(0, -distance) })
            {
                int2 cell = center + delta;
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    continue;

                int index = GridUtils.CellToIndex(cell, grid.Width);
                if (walkable[index].Value == 0 || (blockers.IsCreated && blockers.IsSet(index)))
                    continue;

                float2 xz = grid.Origin.xz + ((float2)cell + 0.5f) * grid.CellSize;
                Vector3 point = Camera.main.WorldToScreenPoint(new Vector3(xz.x, source.y, xz.y));
                if (point.z <= 0 || !Screen.safeArea.Contains(point))
                    continue;
                MarkerUiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, MarkerUiHits);
                if (MarkerUiHits.Count == 0 && SmokeGesture(point))
                {
                    Debug.Log($"[SelectionMarkerNativeMove] requested cell={cell} attempt={markerMoveAttempt + 1}");
                    return true;
                }
            }
            return false;
        }

        private static bool WriteMarkerShot(string state, int width)
        {
            if (markerFile == null)
            {
                string folder = Path.Combine(Output, "markers");
                Directory.CreateDirectory(folder);
                markerFile = Path.Combine(folder, $"en-{width}x1080-{state}.png");
                if (File.Exists(markerFile))
                    throw new IOException("Marker capture output already exists: " + markerFile);
                ScreenCapture.CaptureScreenshot(markerFile);
                Debug.Log($"[SelectionMarkerNativeFrame] state={state} size={Screen.width}x{Screen.height} " +
                    $"cameraY={Camera.main.transform.position.y:F2} fov={Camera.main.fieldOfView:F2} ortho={Camera.main.orthographic} orthoSize={Camera.main.orthographicSize:F2} path={markerFile}");
                return false;
            }
            if (!File.Exists(markerFile))
                return false;
            byte[] header = new byte[24];
            using FileStream png = File.OpenRead(markerFile);
            if (png.Read(header, 0, header.Length) != header.Length)
                return false;
            int savedWidth = header[16] << 24 | header[17] << 16 | header[18] << 8 | header[19];
            int savedHeight = header[20] << 24 | header[21] << 16 | header[22] << 8 | header[23];
            if (savedWidth != width || savedHeight != 1080)
                throw new InvalidOperationException($"Marker PNG dimensions {savedWidth}x{savedHeight}, expected {width}x1080.");
            markerShots++;
            return true;
        }
    }
}
