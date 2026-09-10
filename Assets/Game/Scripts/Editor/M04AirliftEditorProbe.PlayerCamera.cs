using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private static int playerCameraStage = -1, playerCameraCaptures;
        private static double playerCameraStarted, playerCameraCaptureAt;
        public static void RunPlayerCameraAcceptance()
        {
            SessionState.SetBool("Warline.M04.PlayerCamera", true);
            RunCommittedAcceptance();
        }

        private static void ReviewPlayerCamera(EntityManager em, Entity vehicle, int stage)
        {
            if (stage is not (1 or 3 or 6 or 7) || !em.Exists(vehicle) || Camera.main == null) return;
            if (playerCameraStage != stage)
            {
                playerCameraStage = stage;
                playerCameraCaptures = 0;
                playerCameraStarted = EditorApplication.timeSinceStartup;
                playerCameraCaptureAt = playerCameraStarted + (stage == 7 ? 0.15 : 4);
                Transport(em, new RtsSelectionCommandIntentRequestElement
                    { Kind = RtsSelectionCommandIntentKind.FocusUnit, TargetEntity = vehicle, HasTargetEntity = 1 });
                return;
            }
            using var modes = em.CreateEntityQuery(typeof(TacticalFollowCameraModeComponent));
            if (modes.CalculateEntityCount() != 1) return;
            var mode = modes.GetSingleton<TacticalFollowCameraModeComponent>();
            if (mode.Enabled == 0 || mode.BaseTargetEntity != vehicle)
            {
                var panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>();
                var button = panel != null ? typeof(MatchHudSelectionPanelView).GetField("cameraAction",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(panel) as Button : null;
                if (button != null && button.isActiveAndEnabled && button.interactable) button.onClick.Invoke();
                if (EditorApplication.timeSinceStartup - playerCameraStarted > 5)
                    throw new InvalidOperationException("Player CAMERA button did not activate vehicle follow.");
                return;
            }
            if (EditorApplication.timeSinceStartup < playerCameraCaptureAt || playerCameraCaptures >= 3) return;
            playerCameraCaptureAt = EditorApplication.timeSinceStartup + (stage == 7 ? 0.25 : 4);
            var position = em.GetComponentData<LocalTransform>(vehicle).Position;
            var viewport = Camera.main.WorldToViewportPoint(position);
            if (viewport.z <= 0 || viewport.x < .24f || viewport.x > .76f || viewport.y < .15f || viewport.y > .85f)
                throw new InvalidOperationException("Player follow camera lost its vehicle behind the HUD: " + viewport);
            string id = "player-camera-" + stage + "-" + (++playerCameraCaptures);
            ScreenCapture.CaptureScreenshot(Output + "/" + id + ".png");
            File.AppendAllText(Output + "/player-camera.txt", id + " position=" + position + " viewport=" + viewport + "\n");
            Debug.Log("[M04PlayerCamera] " + id + " vehicle remains visible; viewport=" + viewport);
        }

        private static void RecordIdlePursuit(EntityManager em, int clock)
        {
            if (clock / 30000 == lastIdlePursuitBucket) return;
            lastIdlePursuitBucket = clock / 30000;
            using var query = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(LocalTransform), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(actor);

                string row = clock + " " + role.MissionRoleId + " position=" + em.GetComponentData<LocalTransform>(actor).Position +
                    " hp=" + em.GetComponentData<UnitHealth>(actor).Current + " route=" + role.RouteIndex + " orders=" + role.PatrolOrderVersion +
                    " path=" + em.HasComponent<UnitPathFollow>(actor) + " engaged=" + em.HasComponent<EngageTarget>(actor);
                if (em.HasComponent<UnitCombat>(actor))
                {
                    var combat=em.GetComponentData<UnitCombat>(actor);
                    row += " can="+combat.CanAttack+" auto="+combat.AutoEngage+" aggro="+combat.AggroRangeCells;
                }
                if(em.HasComponent<UnitAttack>(actor)) row+=" range="+em.GetComponentData<UnitAttack>(actor).Range;
                if(em.HasComponent<Faction>(actor)) row+=" faction="+em.GetComponentData<Faction>(actor).Id;
                row+=" manual="+em.HasComponent<ManualMoveOrderTag>(actor)+" suppressed="+em.HasComponent<CampaignMissionCombatSuppressedTag>(actor)+" static="+em.HasComponent<StaticGridBlocker>(actor);
                File.AppendAllText(Output + "/idle-pursuit.txt", row + "\n");
            }
        }
        private static int lastIdlePursuitBucket = -1;
    }
}
