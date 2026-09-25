using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Composition
{
    public partial class OperationsMissionPresentationSystem
    {
        private string feedbackSession;
        private readonly float[] previousScanProgress = new float[3];
        private float previousRecoveryProgress;
        private bool previousEvidenceCarried;

        private void ObserveInteractionFeedback(OperationsReconMissionComponent mission, ref UiOperationsMissionModel model)
        {
            string session = mission.SessionId.ToString();
            if (feedbackSession == session && !model.Introduction && !model.Finished)
            {
                string feedback = null;
                for (int i = 0; i < model.SiteProgress.Length && i < previousScanProgress.Length; i++)
                    if (previousScanProgress[i] > 0 && model.SiteProgress[i] == 0 && !model.SiteCompleted[i])
                        feedback = Copy("scan_interrupted", "Scan interrupted. Select infantry inside the blue ring and tap SCAN again.");
                if (previousRecoveryProgress > 0 && model.EvidenceProgress == 0 && !model.EvidenceCarried)
                    feedback = Copy("recovery_interrupted", "Recovery stopped. Clear nearby enemies, then return and recover the evidence.");
                if (previousEvidenceCarried && !model.EvidenceCarried)
                    feedback = Copy("carrier_lost", "The carrier is down. Recover the dropped evidence before extracting.");
                if (feedback != null)
                { notice = model.Status = feedback; noticeExpires = UnityEngine.Time.realtimeSinceStartupAsDouble + 7; }
            }
            feedbackSession = session;
            for (int i = 0; i < model.SiteProgress.Length && i < previousScanProgress.Length; i++) previousScanProgress[i] = model.SiteProgress[i];
            previousRecoveryProgress = model.EvidenceProgress; previousEvidenceCarried = model.EvidenceCarried;
        }

        private bool HandleExperienceRequest(UiOperationsMissionRequest request, Entity root, OperationsReconMissionComponent mission)
        {
            if (mission.Phase != OperationsReconPhase.Playing) return false;
            if (request.Action is UiOperationsMissionAction.StartIntroduction or UiOperationsMissionAction.SkipIntroduction)
            {
                if (!EntityManager.HasComponent<OperationsReconIntroduction>(root)) return true;
                var intro = EntityManager.GetComponentData<OperationsReconIntroduction>(root);
                if (intro.Stage >= 6 || intro.Stage == 5 && request.Action == UiOperationsMissionAction.SkipIntroduction) return true;
                intro.Stage = request.Action == UiOperationsMissionAction.SkipIntroduction || intro.Resumed != 0 ? (byte)5 : (byte)1;
                intro.NextStageAt = UnityEngine.Time.realtimeSinceStartupAsDouble + 2.5;
                EntityManager.SetComponentData(root, intro);
                Focus(intro.Stage == 5 ? SquadPosition(root, mission.ExitPosition) : EntityManager.GetBuffer<OperationsReconSiteElement>(root)[0].Position);
                return true;
            }
            if (request.Action is UiOperationsMissionAction.FocusObjective or UiOperationsMissionAction.FocusSquad)
            {
                viewingObjective = request.Action == UiOperationsMissionAction.FocusObjective;
                Focus(viewingObjective ? NextObjectivePosition(root, mission, out _) : SquadPosition(root, mission.ExitPosition));
                return true;
            }
            return false;
        }

        private void AdvanceIntroduction(Entity root, OperationsReconMissionComponent mission)
        {
            if (mission.Phase != OperationsReconPhase.Playing || !EntityManager.HasComponent<OperationsReconIntroduction>(root)) return;
            var intro = EntityManager.GetComponentData<OperationsReconIntroduction>(root);
            if (intro.Stage < 6) UpdateIntroductionCamera();
            if (intro.Stage == 0 || intro.Stage >= 6 || UnityEngine.Time.realtimeSinceStartupAsDouble < intro.NextStageAt) return;
            // Let the camera settle before handing back control. This runs on real
            // time while the shared pause owner holds simulation and mission clocks.
            using var camera = EntityManager.CreateEntityQuery(typeof(RtsCameraStateComponent));
            if (camera.CalculateEntityCount() == 1)
            {
                var state = camera.GetSingleton<RtsCameraStateComponent>();
                if (state.HasSmoothFocusTarget != 0 || state.HasSmoothPerspectiveTarget != 0) return;
            }
            intro.Stage++;
            intro.NextStageAt = UnityEngine.Time.realtimeSinceStartupAsDouble + 2.5;
            EntityManager.SetComponentData(root, intro);
            if (intro.Stage < 4) Focus(EntityManager.GetBuffer<OperationsReconSiteElement>(root)[intro.Stage - 1].Position);
            else if (intro.Stage == 4) Focus(mission.ExitPosition);
            else if (intro.Stage == 5) Focus(SquadPosition(root, mission.ExitPosition));
            nextRefresh = 0;
        }

        private void UpdateIntroductionCamera()
        {
            // The shared gameplay update intentionally skips selection/camera work
            // while simulation is paused. Drive only its camera request owner here.
            using var gameplay = EntityManager.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1 || gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive != 0) return;
            var cameraSystem = World.GetExistingSystemManaged<RtsCameraSystem>();
            var requests = World.GetExistingSystemManaged<RtsCameraRequestSystem>();
            if (cameraSystem == null || requests == null || !Game.Rendering.RuntimeCameraReferenceSystem.TryGetWorldCamera(World, out var worldCamera)) return;
            using var focus = EntityManager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if (focus.CalculateEntityCount() == 1)
            {
                var request = focus.GetSingleton<RuntimeCameraFocusRequestComponent>();
                if (request.Requested != 0)
                {
                    RuntimeCameraFocusRequestUtility.Queue(requests, EntityManager, request, request.World, 40, 58, 0, 60);
                    request.Requested = 0;
                    EntityManager.SetComponentData(focus.GetSingletonEntity(), request);
                    requests.ProcessPendingRequests(EntityManager, cameraSystem, worldCamera);
                }
            }
            if (cameraSystem.HasSmoothFocusTarget) requests.QueueUpdateSmoothFocus(EntityManager, .55f);
            if (cameraSystem.HasSmoothPerspectiveTarget) requests.QueueUpdateSmoothPerspective(EntityManager, .55f);
            requests.ProcessPendingRequests(EntityManager, cameraSystem, worldCamera);
        }

        private bool CanIssueMissionOrder(Entity root)
        {
            if (EntityManager.HasComponent<OperationsReconIntroduction>(root) &&
                EntityManager.GetComponentData<OperationsReconIntroduction>(root).Stage < 6) return false;
            using var state = EntityManager.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            return state.CalculateEntityCount() == 1 && state.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive != 0;
        }

        private float3 SquadPosition(Entity root, float3 fallback)
        {
            float3 sum = default; int count = 0;
            foreach (var member in EntityManager.GetBuffer<OperationsReconRosterElement>(root))
                if (EntityManager.Exists(member.Unit) && EntityManager.HasComponent<UnitHealth>(member.Unit) &&
                    EntityManager.GetComponentData<UnitHealth>(member.Unit).Current > 0 && EntityManager.HasComponent<LocalTransform>(member.Unit))
                { sum += EntityManager.GetComponentData<LocalTransform>(member.Unit).Position; count++; }
            return count > 0 ? sum / count : fallback;
        }

        private float3 NextObjectivePosition(Entity root, OperationsReconMissionComponent mission, out int site)
        {
            var sites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
            site = -1;
            float nearest = float.MaxValue;
            float3 squad = SquadPosition(root, mission.ExitPosition);
            for (int i = 0; i < sites.Length; i++)
                if (sites[i].Completed == 0)
                {
                    float distance = math.distancesq(squad, sites[i].Position);
                    if (distance < nearest) { site = i; nearest = distance; }
                }
            if (site >= 0) return sites[site].Position;
            var evidence = EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root);
            return evidence.Carrier == Entity.Null ? evidence.Position : mission.ExitPosition;
        }

        private void ProjectExperience(Entity root, OperationsReconMissionComponent mission, ref UiOperationsMissionModel model)
        {
            var intro = EntityManager.HasComponent<OperationsReconIntroduction>(root)
                ? EntityManager.GetComponentData<OperationsReconIntroduction>(root) : new OperationsReconIntroduction { Stage = 6 };
            model.IntroductionStage = intro.Stage;
            model.Introduction = !model.Finished && intro.Stage < 6;
            model.Touring = model.Introduction && intro.Stage > 0;
            model.Resumed = intro.Resumed != 0;
            model.Paused = !CanIssueMissionOrder(root);
            model.ViewingObjective = viewingObjective;
            model.CompletedScans = mission.CompletedScans;
            model.InfantryAtExit = mission.InfantryAtExit;
            model.ObjectivePosition = NextObjectivePosition(root, mission, out model.NextSite);
            ObserveInteractionFeedback(mission, ref model);
            model.Progress = string.Empty;
            model.Guidance = Copy("move_hint", "Move into a blue ring on open ground. Scan sites in any order.");
            if (model.EvidenceAvailable)
                model.Guidance = model.EvidenceCarried
                    ? Copy("extract_hint", "Bring the carrier and 2+ infantry to extraction. Clear nearby enemies.")
                    : Copy("recover_hint", "Move to the evidence. Secure the area, then recover it.");
            for (int i = 0; i < model.SiteProgress.Length; i++)
                if (!model.SiteCompleted[i] && model.SiteProgress[i] > 0)
                {
                    model.Progress = string.Format(Copy("scanning_site", "Scanning site {0}   {1}/15 s"), i + 1, Mathf.FloorToInt(model.SiteProgress[i]));
                    model.Guidance = Copy("scan_hold_hint", "Keep your scanning unit inside the blue ring.");
                    break;
                }
                else if (!model.SiteCompleted[i] && model.CanScanSite[i]) model.Guidance = Copy("scan_ready_hint", "Your infantry are in range. Tap SCAN on the command bar.");
            if (!model.EvidenceCarried && model.EvidenceProgress > 0)
            {
                model.Progress = string.Format(Copy("recovering", "Recovering evidence   {0}/15 s"), Mathf.FloorToInt(model.EvidenceProgress));
                model.Guidance = Copy("recover_hold_hint", "Keep the area secure while your infantry recover the evidence.");
            }
            if (model.EvidenceCarried)
            {
                var carrier = EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Carrier;
                bool carrierHome = EntityManager.HasComponent<LocalTransform>(carrier) && math.distancesq(
                    EntityManager.GetComponentData<LocalTransform>(carrier).Position.xz, mission.ExitPosition.xz) <= mission.ExitRadius * mission.ExitRadius;
                model.Progress = string.Format(Copy("extraction_progress", "Safe extraction: {0}/2 infantry"), math.min(2, mission.InfantryAtExit));
                model.Guidance = carrierHome
                    ? Copy("extraction_hold", "Bring at least two infantry into extraction and clear nearby enemies.")
                    : Copy("extract_hint", "Bring the carrier and 2+ infantry to extraction. Clear nearby enemies.");
            }
            if (!string.IsNullOrEmpty(model.Status)) model.Guidance = model.Status;
            if (model.Touring)
                model.Guidance = intro.Stage < 4
                    ? string.Format(Copy("tour_site", "Signal site {0}. Scan all three courtyards to locate the relay. You choose the route."), intro.Stage)
                    : intro.Stage == 4 ? Copy("tour_exit", "Return here with the relay evidence and at least two infantry. Keep extraction clear of enemies.")
                    : Copy("tour_squad", "Your squad is ready. Keep together for protection, or split scouts to search faster.");
        }
    }
}
