using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Operations.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Composition
{
    /// <summary>Shell, persistence and authoring boundary. Tactical facts are owned by Burst systems.</summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class OperationsMissionPresentationSystem : SystemBase
    {
        private Entity boundary;
        private OperationsProfileCommandService commands;
        private SaveService saves;
        private string restartCommand;
        private OperationsReconMissionConfig definition;
        private OperationsMissionScreenView view;
        private string notice = string.Empty;
        private bool resultSaved;
        private bool reopenOperationsMenu;
        private double nextRefresh, nextSaveRetry;
        private string settlementCommand;
        private string startupSession, rollbackCommand;
        private double startupBeganAt, nextRollbackRetry;
        private OperationsReconCheckpointCodec.Image pendingResume;
        private double nextCheckpointAt, nextCheckpointRetry;
        private int checkpointScans = -1;
        private bool checkpointEvidence, checkpointTerminal;
        private readonly Vector3[] markerScreenPositions = new Vector3[5];
        private double noticeExpires;
        private bool viewingObjective;

        protected override void OnCreate()
        {
            boundary = EntityManager.CreateEntity();
            EntityManager.AddComponentObject(boundary, new UiOperationsMissionReadModel());
            EntityManager.AddBuffer<UiOperationsMissionRequest>(boundary);
        }

        protected override void OnUpdate()
        {
            if (!UiShellRuntimeGateway.TryReadShellState(out var shell)) return;
            if (reopenOperationsMenu && shell.CurrentMode == UiShellMode.MainMenu && !shell.IsTransitionRunning &&
                UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations, false))
                reopenOperationsMenu = false;
            bool live = OperationsReconLaunchProjection.TryGet(EntityManager, out var root, out var mission);
            if (!live && shell.ActiveRoute != UIRoute.Operations) return;
            saves ??= SaveService.CreateDefault();
            commands ??= new OperationsProfileCommandService(saves);
            definition ??= Resources.Load<OperationsReconMissionConfig>(OperationsReconMissionConfig.ResourcePath);
            if (live && mission.Phase == OperationsReconPhase.Preparing && ObserveFailedStartup(root, mission)) return;
            var requests = EntityManager.GetBuffer<UiOperationsMissionRequest>(boundary);
            if (requests.Length > 0)
            {
                var request = requests[0]; requests.Clear();
                try { Handle(request, root, mission, live); }
                catch (IOException) { notice = Copy("save_retry", "Could not save. Try again; progress has not been confirmed."); }
                catch (InvalidOperationException exception) { notice = exception.Message; }
                nextRefresh = 0;
                noticeExpires = UnityEngine.Time.realtimeSinceStartupAsDouble + 7;
                live = OperationsReconLaunchProjection.TryGet(EntityManager, out root, out mission);
            }
            if (live && mission.Phase == OperationsReconPhase.Preparing && shell.CurrentMode == UiShellMode.MatchHud && !shell.IsTransitionRunning)
            {
                if (OperationsReconSpawnCompositionSystemHelper.TrySpawn(EntityManager, root, definition, out string error))
                {
                    try
                    {
                        bool resumed = pendingResume != null;
                        if (resumed)
                        {
                            OperationsReconCheckpointCodec.Apply(EntityManager, root, pendingResume);
                            mission = EntityManager.GetComponentData<OperationsReconMissionComponent>(root);
                            pendingResume = null;
                            notice = Copy("resumed", "Saved attempt resumed.");
                        }
                        else
                        {
                            mission.Phase = OperationsReconPhase.Playing;
                            EntityManager.SetComponentData(root, mission);
                            notice = string.Empty;
                        }
                        if (EntityManager.HasComponent<OperationsReconIntroduction>(root))
                            EntityManager.SetComponentData(root, new OperationsReconIntroduction { Resumed = resumed ? (byte)1 : (byte)0 });
                        else EntityManager.AddComponentData(root, new OperationsReconIntroduction { Resumed = resumed ? (byte)1 : (byte)0 });
                        viewingObjective = false;
                        Focus(definition.exitPosition);
                        checkpointScans = -1;
                        nextCheckpointAt = 0;
                    }
                    catch (InvalidOperationException exception)
                    { EntityManager.GetComponentObject<OperationsReconLaunchReference>(root).StartupFailure = exception.Message; }
                }
                else if (!string.IsNullOrEmpty(error))
                    EntityManager.GetComponentObject<OperationsReconLaunchReference>(root).StartupFailure = error;
            }
            if (live && view == null && shell.CurrentMode == UiShellMode.MatchHud && !shell.IsTransitionRunning &&
                UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>() != null &&
                UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>() != null)
                view = OperationsMissionScreenView.CreateHud(TMPro.TMP_Settings.defaultFontAsset);
            if (live) AdvanceIntroduction(root, mission);
            if (live && view != null && Game.Rendering.RuntimeCameraReferenceSystem.TryGetWorldCamera(World, out var camera))
            {
                var sites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
                for (int i = 0; i < sites.Length && i < 3; i++)
                {
                    markerScreenPositions[i] = camera.WorldToScreenPoint(sites[i].Position);
                    view.PresentScanArea(i, camera, sites[i].Position, sites[i].Radius, sites[i].Completed != 0);
                }
                markerScreenPositions[3] = camera.WorldToScreenPoint(EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Position);
                markerScreenPositions[4] = camera.WorldToScreenPoint(mission.ExitPosition);
                view.PresentMarkers(markerScreenPositions);
            }
            if (live && mission.Phase is OperationsReconPhase.Playing or OperationsReconPhase.Terminal)
                SaveCheckpointWhenDue(root, mission);
            if (live && mission.Phase == OperationsReconPhase.Terminal)
            {
                StopSimulation();
                if (!resultSaved && UnityEngine.Time.realtimeSinceStartupAsDouble >= nextSaveRetry)
                {
                    nextSaveRetry = UnityEngine.Time.realtimeSinceStartupAsDouble + 5;
                    try { Settle(root, mission); }
                    catch (IOException) { notice = Copy("result_retry", "Result pending save. Retrying…"); }
                    catch (InvalidOperationException exception) { notice = exception.Message; }
                }
            }
            if (UnityEngine.Time.realtimeSinceStartupAsDouble < nextRefresh) return;
            nextRefresh = UnityEngine.Time.realtimeSinceStartupAsDouble + .2;
            EntityManager.GetComponentObject<UiOperationsMissionReadModel>(boundary).Value = Project(root, mission, live);
            if (!live)
            {
                var dashboard = UnityEngine.Object.FindAnyObjectByType<OperationsDashboardScreenView>();
                if (dashboard != null) dashboard.Present(ProjectDashboard());
            }
        }

        private UiOperationsDashboardModel ProjectDashboard()
        {
            var profile = saves.LoadProfile();
            var save = commands.Read();
            bool hasRun = OperationsSaveMigration.HasActiveRun(save);
            var result = new UiOperationsDashboardModel
            {
                Credits = profile.credits,
                Command = profile.commandAuthority,
                HasRun = hasRun,
                Day = hasRun ? save.activeRun.day : 0,
                ActionPoints = hasRun ? save.activeRun.actionPoints : 0
            };
            if (!hasRun) return result;
            var districts = save.activeRun.districts;
            if (districts != null && districts.Length > 0)
            {
                var totals = new int[5];
                foreach (var district in districts)
                {
                    totals[0] += district.security;
                    totals[1] += district.trust;
                    totals[2] += district.enemyInfluence;
                    totals[3] += district.heat;
                    totals[4] += district.supplyReadiness;
                }
                result.Readiness = new int[totals.Length];
                for (int i = 0; i < totals.Length; i++)
                    result.Readiness[i] = Mathf.RoundToInt((float)totals[i] / districts.Length);
            }
            var incidents = save.activeRun.incidents;
            if (incidents != null && incidents.Length > 0)
            {
                var warnings = new System.Collections.Generic.List<string>(3);
                foreach (var incident in incidents)
                {
                    if (warnings.Count == 3) break;
                    string warning = (OperationsIncidentKind)incident.kind switch
                    {
                        OperationsIncidentKind.ServiceDisruption => Copy("dashboard_service", "SERVICE DISRUPTION"),
                        OperationsIncidentKind.RoadBlockade => Copy("dashboard_road", "ROAD BLOCKADE"),
                        OperationsIncidentKind.HostilePressure => Copy("dashboard_hostile", "HOSTILE PRESSURE"),
                        _ => string.Empty
                    };
                    if (!string.IsNullOrEmpty(warning)) warnings.Add(warning);
                }
                result.Warnings = warnings.ToArray();
            }
            return result;
        }

        private void Handle(UiOperationsMissionRequest request, Entity root, OperationsReconMissionComponent mission, bool live)
        {
            if (!live && request.Action is UiOperationsMissionAction.Deploy or UiOperationsMissionAction.RestartAttempt or UiOperationsMissionAction.ResumeAttempt)
            { Deploy(request.Action == UiOperationsMissionAction.RestartAttempt, request.Action == UiOperationsMissionAction.ResumeAttempt); return; }
            if (!live && request.Action == UiOperationsMissionAction.WithdrawInterrupted)
            {
                var saved = commands.Read();
                if (saved.pendingDeployment?.reserved == true && commands.TrySubmit(Command(saved, OperationsCommandKind.Withdraw), out var withdrawn, out notice) && withdrawn.Accepted)
                    notice = Copy("withdrawn", "WITHDRAWN");
                return;
            }
            if (!live) return;
            if (HandleExperienceRequest(request, root, mission)) return;
            if (request.Action == UiOperationsMissionAction.Return)
            {
                if (mission.Phase != OperationsReconPhase.Terminal || !resultSaved) return;
                BeginReturn(root, string.Empty);
                return;
            }
            if (mission.Phase != OperationsReconPhase.Playing) return;
            if (request.Action == UiOperationsMissionAction.SaveAndExit)
            {
                if (TrySaveCheckpoint(root, mission))
                    BeginReturn(root, Copy("saved_exit", "Mission saved. Resume this attempt from Operations."));
                return;
            }
            if (request.Action == UiOperationsMissionAction.ScanNearby)
            {
                if (!CanIssueMissionOrder(root)) { notice = Copy("paused_hint", "Resume the mission before giving an order."); return; }
                var nearbySites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
                int target = -1;
                for (int i = 0; i < nearbySites.Length; i++)
                    if (nearbySites[i].Completed == 0 && HasSelectedInfantryNear(root, nearbySites[i].Position, nearbySites[i].Radius))
                    { target = i; break; }
                if (target < 0) { notice = Copy("scan_range_hint", "Select infantry inside a signal site's blue ring, then tap SCAN."); return; }
                request.Action = UiOperationsMissionAction.ScanSite;
                request.SiteIndex = target;
            }
            if (request.Action is UiOperationsMissionAction.AdvanceSite or UiOperationsMissionAction.AdvanceEvidence or UiOperationsMissionAction.AdvanceExit)
            {
                notice = TryQueueObjectiveAdvance(EntityManager, root, request.Action, request.SiteIndex)
                    ? Copy("advancing", "Advancing to the objective. Your selected infantry will engage threats on the way.")
                    : Copy("advance_unavailable", "Select surviving infantry and resume the mission before advancing.");
                return;
            }
            if (request.Action == UiOperationsMissionAction.PromptWithdraw)
            { view?.ShowWithdrawConfirmation(); return; }
            var sites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
            if (request.Action == UiOperationsMissionAction.FocusSite && request.SiteIndex >= 0 && request.SiteIndex < sites.Length)
            { Focus(sites[request.SiteIndex].Position); return; }
            if (request.Action == UiOperationsMissionAction.FocusEvidence)
            { if (mission.CompletedScans == 3) Focus(EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Position); return; }
            if (request.Action == UiOperationsMissionAction.FocusExit) { Focus(mission.ExitPosition); return; }
            if (request.Action is UiOperationsMissionAction.ScanSite or UiOperationsMissionAction.RecoverEvidence && !CanIssueMissionOrder(root))
            { notice = Copy("paused_hint", "Resume the mission before giving an order."); return; }
            var action = request.Action switch
            {
                UiOperationsMissionAction.ScanSite => OperationsReconAction.Scan,
                UiOperationsMissionAction.RecoverEvidence => OperationsReconAction.RecoverEvidence,
                UiOperationsMissionAction.Conclude => OperationsReconAction.Conclude,
                UiOperationsMissionAction.Withdraw => OperationsReconAction.Withdraw,
                _ => OperationsReconAction.CancelInteraction
            };
            Entity actor = Entity.Null;
            if (action is OperationsReconAction.Scan or OperationsReconAction.RecoverEvidence)
            {
                float3 target;
                if (action == OperationsReconAction.Scan)
                {
                    if (request.SiteIndex < 0 || request.SiteIndex >= sites.Length) return;
                    target = sites[request.SiteIndex].Position;
                }
                else target = EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Position;
                float best = float.MaxValue;
                foreach (var member in EntityManager.GetBuffer<OperationsReconRosterElement>(root))
                {
                    if (!EntityManager.HasComponent<SelectedUnitTag>(member.Unit) || !EntityManager.HasComponent<UnitHealth>(member.Unit) ||
                        EntityManager.GetComponentData<UnitHealth>(member.Unit).Current <= 0 || !EntityManager.HasComponent<LocalTransform>(member.Unit)) continue;
                    float distance = math.distancesq(EntityManager.GetComponentData<LocalTransform>(member.Unit).Position.xz, target.xz);
                    if (distance < best) { best = distance; actor = member.Unit; }
                }
                if (actor == Entity.Null || best > (action == OperationsReconAction.Scan ? 64f : 36f))
                { notice = Copy("closer", "Select surviving infantry and move them closer to this location first."); return; }
                using var surfaces = EntityManager.CreateEntityQuery(typeof(MapSurfaceComponent));
                var surface = surfaces.CalculateEntityCount() == 1 ? surfaces.GetSingleton<MapSurfaceComponent>() : default;
                if (!surface.SurfaceBlob.IsCreated || !OperationsReconObjectiveSystem.CanReach(
                    EntityManager.GetComponentData<LocalTransform>(actor).Position, target,
                    action == OperationsReconAction.Scan ? 8f : 6f, ref surface))
                { notice = Copy("blocked_approach", "The approach is blocked. Move your infantry into the marked area."); return; }
            }
            EntityManager.GetBuffer<OperationsReconActionElement>(root).Add(new OperationsReconActionElement
            { SessionId = mission.SessionId, Action = action, Actor = actor, SiteIndex = request.SiteIndex });
            notice = string.Empty;
        }

        public static bool TryQueueObjectiveAdvance(EntityManager em, Entity root, UiOperationsMissionAction action, int siteIndex)
        {
            if (!em.Exists(root) || !em.HasComponent<OperationsReconMissionComponent>(root)) return false;
            var mission = em.GetComponentData<OperationsReconMissionComponent>(root);
            if (mission.Phase != OperationsReconPhase.Playing) return false;
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            using var input = em.CreateEntityQuery(typeof(RtsSelectionInputStateComponent), typeof(RtsSelectionCommandIntentRequestElement));
            if (gameplay.CalculateEntityCount() != 1 || gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0 ||
                grids.CalculateEntityCount() != 1 || input.CalculateEntityCount() != 1) return false;
            bool selected = false;
            foreach (var member in em.GetBuffer<OperationsReconRosterElement>(root))
                if (em.Exists(member.Unit) && em.HasComponent<SelectedUnitTag>(member.Unit) &&
                    em.HasComponent<UnitHealth>(member.Unit) && em.GetComponentData<UnitHealth>(member.Unit).Current > 0)
                { selected = true; break; }
            if (!selected) return false;
            float3 destination;
            if (action == UiOperationsMissionAction.AdvanceSite)
            {
                var sites = em.GetBuffer<OperationsReconSiteElement>(root);
                if (siteIndex < 0 || siteIndex >= sites.Length) return false;
                destination = sites[siteIndex].Position;
            }
            else if (action == UiOperationsMissionAction.AdvanceEvidence && mission.CompletedScans == 3)
                destination = em.GetComponentData<OperationsReconEvidenceComponent>(root).Position;
            else if (action == UiOperationsMissionAction.AdvanceExit) destination = mission.ExitPosition;
            else return false;
            var grid = grids.GetSingleton<GridConfig>();
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(input.GetSingletonEntity()).Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.Attack, Frame = UnityEngine.Time.frameCount,
                TargetKind = RtsSelectionCommandTargetKind.Cell, TargetCell = GridUtils.WorldToCell(grid, destination),
                WorldPosition = destination, HasTargetCell = 1, HasWorldPosition = 1, ExplicitAttackTargetMode = 1
            });
            return true;
        }

        private bool ObserveFailedStartup(Entity root, OperationsReconMissionComponent mission)
        {
            string session = mission.SessionId.ToString();
            if (startupSession != session)
            { startupSession = session; startupBeganAt = UnityEngine.Time.realtimeSinceStartupAsDouble; rollbackCommand = null; nextRollbackRetry = 0; }
            var launch = EntityManager.GetComponentObject<OperationsReconLaunchReference>(root);
            using var starts = EntityManager.CreateEntityQuery(typeof(MatchStartQueueComponent));
            bool failed = !string.IsNullOrEmpty(launch.StartupFailure) ||
                starts.CalculateEntityCount() == 1 && starts.GetSingleton<MatchStartQueueComponent>().LastStatus == MatchStartStatusKind.Failed ||
                UnityEngine.Time.realtimeSinceStartupAsDouble - startupBeganAt >= 180;
            if (!failed) return false;
            StopSimulation();
            if (UnityEngine.Time.realtimeSinceStartupAsDouble < nextRollbackRetry) return true;
            nextRollbackRetry = UnityEngine.Time.realtimeSinceStartupAsDouble + 5;
            try
            {
                var saved = commands.Read();
                rollbackCommand ??= Id();
                var rollback = new OperationsCommand(rollbackCommand, saved.profileRevision, OperationsCommandKind.TechnicalFailure, "", "", "");
                if (commands.TrySubmit(rollback, out var result, out _) && result.Accepted)
                    BeginReturn(root, Copy("load_refunded", "The mission could not load. Your action point was returned. You can try again."));
                else notice = Copy("load_refund_pending", "The mission could not load. Recovery is waiting for a saved refund.");
            }
            catch (IOException) { notice = Copy("load_refund_pending", "The mission could not load. Recovery is waiting for a saved refund."); }
            catch (InvalidOperationException) { notice = Copy("load_refund_pending", "The mission could not load. Recovery is waiting for a saved refund."); }
            EntityManager.GetComponentObject<UiOperationsMissionReadModel>(boundary).Value =
                new UiOperationsMissionModel { Title = Copy("title", "STREET SIGNALS — OLD QUARTER"), Status = notice };
            nextRefresh = 0;
            return true;
        }

        private bool BeginReturn(Entity root, string message)
        {
            if (!UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.ReturnToMainMenu, UIRoute.Operations, false)) return false;
            reopenOperationsMenu = true;
            using var owned = EntityManager.CreateEntityQuery(new EntityQueryDesc
            { All = new[] { ComponentType.ReadOnly<OperationsReconMemberComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
            using (var units = owned.ToEntityArray(Unity.Collections.Allocator.Temp))
                foreach (var unit in units) if (EntityManager.Exists(unit)) EntityManager.DestroyEntity(unit);
            if (EntityManager.Exists(root)) EntityManager.DestroyEntity(root);
            if (view != null) UnityEngine.Object.Destroy(view.gameObject);
            view = null; resultSaved = false; settlementCommand = null; pendingResume = null; notice = message;
            return true;
        }

        private void Deploy(bool restart, bool resume)
        {
            if (definition == null || !definition.TryValidate(out _)) { notice = Copy("content_missing", "Street Signals content is unavailable."); return; }
            var save = commands.Read();
            if (!OperationsSaveMigration.HasActiveRun(save))
            {
                if (!commands.TryNewRun(Command(save, OperationsCommandKind.NewRun), 1102, OperationsDifficultyKind.Regular, out var created, out notice) || !created.Accepted) return;
                save = commands.Read();
            }
            var attempt = save.pendingDeployment?.reserved == true
                ? Array.Find(save.activeRun.attempts, item => item.sessionId == save.pendingDeployment.sessionId && !item.practice) : null;
            if (attempt != null && !restart && !resume)
            { notice = Copy("interrupted", "An interrupted attempt is reserved. Resume its checkpoint, restart from the beginning if no checkpoint exists, or withdraw."); return; }
            if (restart && attempt == null) return;
            if (resume && attempt == null) return;
            if (attempt == null)
            {
                // Eligibility is recomputed by the strategic command rules. The legacy
                // serialized display flag is not authoritative (new-run offers may leave it unset).
                var offer = Array.Find(save.activeRun.offers, item => item.missionId == "operation.o001");
                if (offer == null) { notice = Copy("no_offer", "Street Signals is not available in the current Operations day."); return; }
                var deploy = new OperationsCommand(Id(), save.profileRevision, OperationsCommandKind.Deploy, offer.districtId, offer.offerId, string.Empty);
                if (!commands.TrySubmit(deploy, out var reserved, out notice) || !reserved.Accepted)
                { if (string.IsNullOrEmpty(notice)) notice = reserved.ReasonCode.ToString(); return; }
                save = commands.Read();
                attempt = save.pendingDeployment?.reserved == true
                    ? Array.Find(save.activeRun.attempts, item => item.sessionId == save.pendingDeployment.sessionId && !item.practice) : null;
            }
            if (attempt == null || attempt.missionId != definition.missionId) { notice = Copy("attempt_conflict", "Finish the existing Operations attempt first."); return; }
            pendingResume = null;
            if (resume)
            {
                var archive = saves.LoadOperationsCheckpoint(attempt.sessionId);
                string content = definition.operationMap.ContentHash;
                if (archive == null || archive.content != content ||
                    !OperationsReconCheckpointCodec.TryDecode(archive.current, attempt.sessionId, content, out pendingResume) &&
                    !OperationsReconCheckpointCodec.TryDecode(archive.previous, attempt.sessionId, content, out pendingResume))
                {
                    if (commands.TrySubmit(Command(save, OperationsCommandKind.TechnicalFailure), out var refunded, out _) && refunded.Accepted)
                        notice = Copy("recovery_refunded", "The saved attempt is incompatible. Your action point was returned and recovery data was preserved.");
                    return;
                }
            }
            if (restart) restartCommand ??= Id();
            if (!resume && !saves.TryBeginOperationsAttempt(attempt.sessionId, definition.operationMap.ContentHash, restart ? restartCommand : null, out string recoveryError))
            {
                // Preserve incompatible journal bytes; the strategic refund is idempotent.
                if (recoveryError == "checkpoint_requires_resume")
                { notice = Copy("resume_available", "Resume the saved attempt to keep its progress."); return; }
                if (commands.TrySubmit(Command(save, OperationsCommandKind.TechnicalFailure), out var refunded, out _) && refunded.Accepted)
                    notice = Copy("recovery_refunded", "The saved attempt is incompatible. Your action point was returned and recovery data was preserved.");
                return;
            }
            if (!OperationsReconLaunchProjection.TryQueue(EntityManager, definition, attempt.sessionId, out notice, unchecked((uint)save.activeRun.seed)))
            { pendingResume = null; return; }
            restartCommand = null;
            resultSaved = false; settlementCommand = null; nextSaveRetry = 0;
            checkpointScans = -1; checkpointEvidence = false; checkpointTerminal = false; nextCheckpointAt = 0;
            UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.EnterMatch, UIRoute.Match, false);
        }

        private void SaveCheckpointWhenDue(Entity root, OperationsReconMissionComponent mission)
        {
            bool terminal = mission.Phase == OperationsReconPhase.Terminal;
            bool evidence = EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Recovered != 0;
            if (mission.ElapsedSeconds < nextCheckpointAt && mission.CompletedScans == checkpointScans &&
                evidence == checkpointEvidence && terminal == checkpointTerminal) return;
            if (UnityEngine.Time.realtimeSinceStartupAsDouble < nextCheckpointRetry) return;
            if (TrySaveCheckpoint(root, mission))
            {
                checkpointScans = mission.CompletedScans;
                checkpointEvidence = evidence;
                checkpointTerminal = terminal;
                nextCheckpointAt = mission.ElapsedSeconds + 30;
            }
            else nextCheckpointRetry = UnityEngine.Time.realtimeSinceStartupAsDouble + 5;
        }

        private bool TrySaveCheckpoint(Entity root, OperationsReconMissionComponent mission)
        {
            try
            {
                string image = OperationsReconCheckpointCodec.Encode(
                    OperationsReconCheckpointCodec.Capture(EntityManager, root, definition.operationMap.ContentHash));
                if (saves.TrySaveOperationsCheckpoint(mission.SessionId.ToString(), image, out _)) return true;
                notice = Copy("checkpoint_failed", "Could not save mission progress. Try again.");
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
            { notice = Copy("checkpoint_failed", "Could not save mission progress. Try again."); }
            return false;
        }

        private UiOperationsMissionModel Project(Entity root, OperationsReconMissionComponent mission, bool live)
        {
            var save = live ? null : commands.Read();
            var model = new UiOperationsMissionModel
            {
                Title = Copy("title", "STREET SIGNALS — OLD QUARTER"),
                Description = Copy("brief_compact", "Scan 3 courtyards • recover relay evidence • extract 2 infantry\n16 infantry • 12 min • 1 AP"),
                Status = !live || UnityEngine.Time.realtimeSinceStartupAsDouble < noticeExpires ? notice : string.Empty,
                CanDeploy = !live && definition != null && (!OperationsSaveMigration.HasActiveRun(save) || save.activeRun.actionPoints > 0 || save.pendingDeployment?.reserved == true),
                Clock = !OperationsSaveMigration.HasActiveRun(save) ? Copy("new_city", "A new city operation will begin on deployment.") :
                    string.Format(Copy("day_ap", "Day {0} • AP {1}"), save.activeRun.day, save.activeRun.actionPoints),
                InterruptedAttempt = !live && save?.pendingDeployment?.reserved == true,
                InMission = live, Finished = live && mission.Phase == OperationsReconPhase.Terminal, Saved = resultSaved
            };
            if (!live)
            {
                if (model.InterruptedAttempt)
                {
                    var archive = saves.LoadOperationsCheckpoint(save.pendingDeployment.sessionId);
                    model.CanResume = archive != null && (!string.IsNullOrEmpty(archive.current) || !string.IsNullOrEmpty(archive.previous));
                }
                if (model.InterruptedAttempt && string.IsNullOrEmpty(model.Status))
                    model.Status = model.CanResume
                        ? Copy("resume_available", "Resume the saved attempt to keep its progress.")
                        : Copy("interrupted", "An interrupted attempt is reserved. Restart from the beginning without another AP cost, or withdraw.");
                return model;
            }
            int remaining = Mathf.Max(0, Mathf.CeilToInt(mission.DeadlineSeconds - mission.ElapsedSeconds));
            model.Clock = string.Format(Copy("clock", "{0}:{1} • {2}/16 infantry"), remaining / 60, (remaining % 60).ToString("00"), mission.SurvivingInfantry);
            model.Objective = mission.CompletedScans < 3 ? Copy("scan_objective", "Scan all three signal sites") + "  " + mission.CompletedScans + "/3"
                : EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Carrier == Entity.Null
                    ? Copy("evidence_objective", "Recover the relay evidence — hold for 15 seconds")
                    : Copy("exit_objective", "Bring the evidence and two original infantry to the safe exit");
            model.CanConclude = mission.PartialAvailable != 0;
            var evidence = EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root);
            model.EvidenceProgress = evidence.ChannelSeconds;
            model.EvidenceAvailable = mission.CompletedScans == 3;
            model.CanRecover = model.EvidenceAvailable && evidence.Carrier == Entity.Null;
            model.EvidenceCarried = evidence.Carrier != Entity.Null;
            model.CanRecoverHere = model.CanRecover && HasSelectedInfantryNear(root, evidence.Position, 6f);
            model.EvidenceStatus = evidence.Carrier != Entity.Null ? Copy("carried", "EVIDENCE CARRIED") :
                string.Format(Copy("recover_progress", "RECOVER {0}/15 s"), Mathf.FloorToInt(evidence.ChannelSeconds));
            var sites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
            model.SiteStatus = new string[sites.Length];
            model.SiteCompleted = new bool[sites.Length];
            model.CanScanSite = new bool[sites.Length];
            model.SiteProgress = new float[sites.Length];
            for (int i = 0; i < sites.Length; i++)
            {
                model.SiteCompleted[i] = sites[i].Completed != 0;
                model.SiteProgress[i] = sites[i].ChannelSeconds;
                model.CanScanSite[i] = !model.SiteCompleted[i] && HasSelectedInfantryNear(root, sites[i].Position, 8f);
                model.SiteStatus[i] = string.Format(Copy("signal", "SIGNAL {0}"), (char)('A' + i)) + " • " +
                    (model.SiteCompleted[i] ? Copy("done", "DONE") : Mathf.FloorToInt(sites[i].ChannelSeconds) + "/15 s");
            }
            if (string.IsNullOrEmpty(model.Status) && EntityManager.HasComponent<OperationsReconWaveComponent>(root))
            {
                var waves = EntityManager.GetComponentData<OperationsReconWaveComponent>(root);
                if (waves.WaveBAnnounced != 0 && mission.ElapsedSeconds < waves.WaveBReleaseAt)
                    model.Status = Copy("wave_b", "Evidence recovered. Enemy reinforcements are approaching the relay.");
                else if (waves.WaveAAnnounced != 0 && mission.ElapsedSeconds < waves.WaveAReleaseAt)
                    model.Status = Copy("wave_a", "Signal detected. Enemy reinforcements are approaching the search area.");
            }
            string outcome = mission.Outcome switch
            {
                OperationsReconOutcome.Victory => Copy("victory", "VICTORY"),
                OperationsReconOutcome.Partial => Copy("partial", "PARTIAL SUCCESS"),
                OperationsReconOutcome.Withdraw => Copy("withdrawn", "WITHDRAWN"),
                _ => Copy("defeat", "DEFEAT")
            };
            model.Result = string.Format(Copy("result", "{0}\n{1}/3 signals • {2} extracted\n{3}"), outcome,
                mission.CompletedScans, mission.InfantryAtExit,
                resultSaved ? Copy("saved", "District result and rewards saved.") : Copy("saving", "Saving result…"));
            ProjectExperience(root, mission, ref model);
            return model;
        }

        private bool HasSelectedInfantryNear(Entity root, float3 target, float radius)
        {
            float limit = radius * radius;
            foreach (var member in EntityManager.GetBuffer<OperationsReconRosterElement>(root))
                if (EntityManager.Exists(member.Unit) && EntityManager.HasComponent<SelectedUnitTag>(member.Unit) &&
                    EntityManager.HasComponent<UnitHealth>(member.Unit) &&
                    EntityManager.GetComponentData<UnitHealth>(member.Unit).Current > 0 &&
                    EntityManager.HasComponent<LocalTransform>(member.Unit) &&
                    math.distancesq(EntityManager.GetComponentData<LocalTransform>(member.Unit).Position.xz, target.xz) <= limit)
                    return true;
            return false;
        }

        private void Focus(float3 position)
        {
            using var query = EntityManager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if (query.CalculateEntityCount() != 1) return;
            EntityManager.SetComponentData(query.GetSingletonEntity(), new RuntimeCameraFocusRequestComponent
            { Requested = 1, Smooth = SettingsService.Load().Accessibility.ReducedMotion ? (byte)0 : (byte)1,
                SmoothTimeSeconds = .55f, UseExplicitPerspective = 1, Perspective = new float4(40,58,0,60), World = position });
        }

        private void Settle(Entity root, OperationsReconMissionComponent mission)
        {
            var save = commands.Read();
            var attempt = Array.Find(save.activeRun.attempts, item => item.sessionId == mission.SessionId.ToString());
            if (attempt == null) { notice = Copy("attempt_missing", "The saved attempt could not be matched. Result retained for recovery."); return; }
            var outcome = mission.Outcome switch
            {
                OperationsReconOutcome.Victory => OperationsOutcomeKind.Victory,
                OperationsReconOutcome.Partial => OperationsOutcomeKind.Partial,
                OperationsReconOutcome.Withdraw => OperationsOutcomeKind.Withdrawn,
                _ => OperationsOutcomeKind.Defeat
            };
            int initial = EntityManager.GetBuffer<OperationsReconRosterElement>(root).Length;
            bool recovered = EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Recovered != 0;
            var snapshot = new OperationsResultSnapshot
            {
                sessionId = mission.SessionId.ToString(), outcome = outcome.ToString(),
                elapsedSeconds = Mathf.FloorToInt(mission.ElapsedSeconds), scans = mission.CompletedScans,
                survivors = mission.SurvivingInfantry, extracted = mission.InfantryAtExit,
                mastery = mission.MasteryCompleted != 0
            };
            string json = JsonUtility.ToJson(snapshot);
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                // Preserve all 256 bits within the contract's 60-character token budget.
                hash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var result = new OperationsMissionResult(1, save.activeRun.runId, attempt.offerId, attempt.missionId,
                attempt.sessionId, attempt.attemptOrdinal, 1, outcome, outcome.ToString().ToLowerInvariant(), snapshot.elapsedSeconds,
                new[]
                {
                    new OperationsObjectiveFact("scan_signals", mission.CompletedScans == 3, false, mission.CompletedScans),
                    new OperationsObjectiveFact("interact_relay", recovered, false, recovered ? 1 : 0),
                    new OperationsObjectiveFact("extract_force", mission.InfantryAtExit >= 2, false, mission.InfantryAtExit)
                },
                new[] { new OperationsObjectiveFact("recon_mastery", snapshot.mastery, !snapshot.mastery, snapshot.mastery ? 1 : 0) },
                0, Array.Empty<OperationsObjectiveFact>(), Array.Empty<OperationsObjectiveFact>(),
                outcome == OperationsOutcomeKind.Victory ? new[] { "evidence.d01.relay" } : Array.Empty<string>(),
                Math.Max(0, initial - mission.SurvivingInfantry), initial, hash);
            settlementCommand ??= Id();
            var command = new OperationsCommand(settlementCommand, save.profileRevision, OperationsCommandKind.Conclude, "", "", "");
            if (commands.TrySettle(command, result, json, out var committed, out notice) && committed.Accepted)
            { resultSaved = true; notice = string.Empty; }
            else if (string.IsNullOrEmpty(notice)) notice = committed.ReasonCode.ToString();
        }

        [Serializable]
        private sealed class OperationsResultSnapshot
        {
            public string sessionId, outcome;
            public int elapsedSeconds, scans, survivors, extracted;
            public bool mastery;
        }

        private void StopSimulation()
        {
            using var query = EntityManager.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (query.CalculateEntityCount() != 1) return;
            var state = query.GetSingleton<RuntimeGameplayStateComponent>(); state.SimulationActive = 0;
            EntityManager.SetComponentData(query.GetSingletonEntity(), state);
        }

        private static OperationsCommand Command(OperationsSaveData save, OperationsCommandKind kind) => new(Id(), save.profileRevision, kind, "", "", "");
        private static string Id() => "cmd.operations." + Guid.NewGuid().ToString("N");
        private static string Copy(string key, string fallback) => GameText.Get("operations.o001." + key, fallback);
        protected override void OnDestroy() { if (view != null) UnityEngine.Object.Destroy(view.gameObject); }
    }
}
