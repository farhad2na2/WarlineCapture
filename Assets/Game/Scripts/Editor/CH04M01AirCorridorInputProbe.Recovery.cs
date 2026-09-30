using System;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class CH04M01AirCorridorInputProbe
    {
        private static bool Recovery => SessionState.GetBool(Active + ".Recovery", false);
        private static SaveService recoverySave;
        private static int recoveryStep, failedAttempt, priorCredits, priorXp;
        private static bool recoveryVerified, continueVerified;
        private static Entity[] retiredMembers;
        private static Entity recoveryRadar;
        private static double recoveryInputAt;
        private static int mapTapPhase;
        private static double mapTapReadyAt;
        public static void RunFinalRecoveryEnglish()
        {
            CH04M01AirCorridorPresentationBuilder.BuildCheckpoint();
            RunRecoveryEnglish();
        }
        public static void RunRecoveryEnglish()
        {
            SessionState.SetBool(Active + ".Budget", false);
            SessionState.SetBool(Active + ".Recovery", true);
            SessionState.SetBool(Active + ".Manual", true);
            SessionState.SetString(Locale, "en");
            recoveryStep = 0; recoveryVerified = continueVerified = false;
            recoveryInputAt = 0; retiredMembers = null; mapTapPhase = 0;
            Run();
        }
        private static bool TickRecovery(EntityManager em, Entity root,
            CampaignMissionRuntimeComponent runtime, CampaignMissionAttemptFactsComponent facts)
        {
            if (recoveryVerified) return false;
            if (runtime.Phase == MissionPhaseKind.Engage && !continueVerified)
            {
                if (runtime.RunKind != MissionRunKind.Retry || runtime.AttemptOrdinal != 8 ||
                    !runtime.OperationMapId.Equals(CH04M01AirCorridorConfigBuilder.MapId))
                    throw new InvalidOperationException("Saved Continue did not create a fresh current-map Retry.");
                ValidatePreparedSource(em);
                var catalog = em.GetComponentData<CampaignMissionCatalogComponent>(root);
                var payload = MissionLaunchPayloadFactory.Create(CampaignMissionSequence.AirCorridor,
                    CH04M01AirCorridorConfigBuilder.ScenarioId, "opmap.ch04.air_corridor_01",
                    MissionLaunchOriginKind.CampaignOperations, MissionRunKind.Retry,
                    Game.Narrative.Contracts.NarrativeGuidanceMode.Full, true, 999, "legacy-air-corridor", 7, 4001001);
                var request = FirstLaunchMissionHandoffOperation.ToRequest(in payload);
                var map = new ActiveOperationMapComponent { MissionId = runtime.MissionId,
                    ScenarioId = runtime.ScenarioId, OperationMapId = runtime.OperationMapId };
                if (CampaignMissionLaunchSystem.TryValidate(in request, in catalog, in map, out var reason) ||
                    reason.ToString() != "mission-catalog-mismatch")
                    throw new InvalidOperationException("Old logical-map launch was silently reinterpreted.");
                continueVerified = true;
                Debug.Log("[AirCorridorContinue] result=Passed savedAttempt=7 freshAttempt=8 run=Retry legacy-map=mission-catalog-mismatch");
            }
            if (!continueVerified || runtime.Phase != MissionPhaseKind.Engage && runtime.Outcome != MissionOutcomeKind.Defeat)
                return false;
            if (recoveryStep == 0)
            {
                if (!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep < 4) return false;
                var members = em.GetBuffer<CampaignMissionDefenseMember>(root, true);
                recoveryRadar = Entity.Null;
                for (int i = 0; i < members.Length; i++) if (members[i].IsSensor != 0) recoveryRadar = members[i].Entity;
                if (recoveryRadar == Entity.Null) throw new InvalidOperationException("Radar roster missing.");
                var profile = recoverySave.LoadProfile(); priorCredits = profile.credits; priorXp = profile.commanderXp;
                failedAttempt = runtime.AttemptOrdinal;
                recoveryStep = 1;
                Debug.Log("[AirCorridorRecovery] normal-input radar exposure begins; no simulation-state mutation");
            }
            if (runtime.Outcome == MissionOutcomeKind.Defeat)
            {
                if (facts.ForwardPostDestroyed == 0 || !em.HasComponent<UnitHealth>(recoveryRadar) ||
                    em.GetComponentData<UnitHealth>(recoveryRadar).Current > 0)
                    throw new InvalidOperationException("Negative journey did not fail from real radar loss.");
                if (!UiShellRuntimeGateway.TryReadMissionResult(out var result) || !result.RetryVisible) return true;
                var profile = recoverySave.LoadProfile();
                if (profile.credits != priorCredits || profile.commanderXp != priorXp ||
                    store.ReadAll().Single(x => x.missionId == CampaignMissionSequence.AirCorridor).firstClearRewardSettled)
                    throw new InvalidOperationException("Defeat granted Campaign rewards.");
                if (recoveryStep < 8)
                {
                    Shot("radar-loss-result");
                    var defeatedMembers = em.GetBuffer<CampaignMissionDefenseMember>(root, true);
                    retiredMembers = new Entity[defeatedMembers.Length];
                    for (int i = 0; i < defeatedMembers.Length; i++) retiredMembers[i] = defeatedMembers[i].Entity;
                    var view = UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                    var retry = view == null ? null : typeof(MissionResultPopupView).GetField("retryButton", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(view) as Button;
                    if (!Click(retry)) return true;
                    recoveryStep = 8;
                    Debug.Log("[AirCorridorRadarLoss] result=Passed destroyed=real-combat rewards=unchanged Retry=visible-button");
                }
                return true;
            }
            if (recoveryStep == 8)
            {
                if (runtime.AttemptOrdinal <= failedAttempt || runtime.Phase != MissionPhaseKind.Engage) return true;
                if (runtime.RunKind != MissionRunKind.Retry || facts.HostileDefeatedCount != 0 || facts.CoreBreached != 0 ||
                    retiredMembers.Any(em.Exists)) throw new InvalidOperationException("Retry retained old attempt actors or objective state.");
                var members = em.GetBuffer<CampaignMissionDefenseMember>(root, true);
                int hostileCount = 0; bool freshRadar = false;
                for (int i = 0; i < members.Length; i++)
                {
                    if (members[i].ElementIndex >= 0) hostileCount++;
                    if (members[i].IsSensor != 0) freshRadar = em.GetComponentData<UnitHealth>(members[i].Entity).Current == 500;
                }
                if (members.Length != 11 || hostileCount != 6 || !freshRadar)
                    throw new InvalidOperationException("Retry roster/radar is not fresh.");
                var defense = em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
                if (defense.AcknowledgedGuidanceMask != 0) throw new InvalidOperationException("Retry kept old tutorial acknowledgement.");
                manualLesson = -1; manualFocused.Clear(); lastManualInput = 0; actions = 0;
                recoveryVerified = true;
                Shot("fresh-retry");
                Debug.Log("[AirCorridorRetry] result=Passed newAttempt=" + runtime.AttemptOrdinal + " oldActors=0 roster=11 hostiles=6 radar=500 guidance=fresh");
                return false;
            }
            manualTouch ??= new AriaTouchInputUiSystemHelper();
            if (!manualTouch.IsRunning && !manualTouch.Start()) throw new InvalidOperationException("Recovery touch device unavailable.");
            manualTouch.Tick(Time.unscaledTime);
            if (manualTouch.IsBusy || EditorApplication.timeSinceStartup - recoveryInputAt < 1) return true;
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (controls == null) return true;
            bool accepted = recoveryStep switch
            {
                1 => TapMinimap(em.GetComponentData<LocalTransform>(recoveryRadar).Position),
                2 => Click(controls.SelectButton),
                3 => TapWorld(em.GetComponentData<LocalTransform>(recoveryRadar).Position + new Unity.Mathematics.float3(0,1,0)),
                4 => em.HasComponent<SelectedUnitTag>(recoveryRadar) && Click(controls.MoveButton),
                5 => TapMinimap(new Vector3(900,0,600)),
                6 => MoveRadarThroughTouch(em),
                _ => false
            };
            if (accepted)
            {
                Debug.Log("[AirCorridorRecoveryInput] step=" + recoveryStep + " input=normal-touch/UI");
                recoveryInputAt = EditorApplication.timeSinceStartup; recoveryStep++;
                if (recoveryStep == 7) Shot("radar-exposure-move");
            }
            return true;
        }
        private static bool MoveRadarThroughTouch(EntityManager em)
        {
            var position = em.GetComponentData<LocalTransform>(recoveryRadar).Position;
            if (Unity.Mathematics.math.distance(position, new Unity.Mathematics.float3(631.5f,0,598.5f)) > 3)
                return true;
            // Selection suppresses the next world release to prevent an accidental
            // order. Repeat the ordinary destination tap until the live unit moves.
            if (TapWorld(new Vector3(900,0,600))) recoveryInputAt = EditorApplication.timeSinceStartup;
            return false;
        }
        private static bool TapWorld(Vector3 world)
        {
            var camera = Camera.main; if (camera == null) return false;
            var screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0 || !Screen.safeArea.Contains(screen)) return false;
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
            if (hits.Count > 0) return false;
            return manualTouch.TryGesture(screen, screen, .18f, 0, Time.unscaledTime);
        }
        private static bool TapMinimap(Vector3 world)
        {
            if (EditorApplication.timeSinceStartup < mapTapReadyAt) return false;
            var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            if (mapTapPhase == 2 && popup != null)
            {
                if (!Click(popup.CloseAction)) return false;
                mapTapPhase = 0; return true;
            }
            var map = popup != null && popup.IsOpen ? popup.Minimap : UnityEngine.Object.FindObjectsByType<MatchHudMinimapView>(FindObjectsInactive.Exclude).FirstOrDefault(x => popup == null || x != popup.Minimap);
            if (map == null) return false;
            var canvas = map.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (popup == null || !popup.IsOpen)
            {
                var rect=(RectTransform)map.transform;
                var center=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
                if(manualTouch.TryGesture(center,center,.18f,0,Time.unscaledTime))
                {mapTapPhase=1;mapTapReadyAt=EditorApplication.timeSinceStartup+1;}
                return false;
            }
            object[] args = { world, camera, default(Vector2) };
            var method = typeof(MatchHudMinimapView).GetMethod("TryGetPresentedMapPoint", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null || !(bool)method.Invoke(map, args)) return false;
            var point = (Vector2)args[2];
            if (!manualTouch.TryGesture(point, point, .18f, 0, Time.unscaledTime)) return false;
            mapTapPhase = popup != null && popup.IsOpen ? 2 : 1;
            mapTapReadyAt = EditorApplication.timeSinceStartup + 1;
            return false;
        }
    }
}
