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
        private OperationsReconMissionConfig definition;
        private OperationsMissionScreenView view;
        private string notice = string.Empty;
        private bool resultSaved;
        private bool reopenOperationsMenu;
        private double nextRefresh, nextSaveRetry;
        private string settlementCommand;
        private string startupSession, rollbackCommand;
        private double startupBeganAt, nextRollbackRetry;
        private readonly Vector3[] markerScreenPositions = new Vector3[5];

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
            commands ??= new OperationsProfileCommandService(SaveService.CreateDefault());
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
                live = OperationsReconLaunchProjection.TryGet(EntityManager, out root, out mission);
            }
            if (live && mission.Phase == OperationsReconPhase.Preparing && shell.CurrentMode == UiShellMode.MatchHud && !shell.IsTransitionRunning)
            {
                if (OperationsReconSpawnCompositionSystemHelper.TrySpawn(EntityManager, root, definition, out string error))
                {
                    mission.Phase = OperationsReconPhase.Playing;
                    EntityManager.SetComponentData(root, mission);
                    Focus(definition.exitPosition);
                    notice = Copy("scan_help", "RIFLE SQUAD selects your force. Use ATTACK on open ground to advance and fight. Within 8 m of a signal, SCAN SELECTED for 15 seconds.");
                }
                else if (!string.IsNullOrEmpty(error))
                    EntityManager.GetComponentObject<OperationsReconLaunchReference>(root).StartupFailure = error;
            }
            if (live && view == null) view = OperationsMissionScreenView.CreateHud(TMPro.TMP_Settings.defaultFontAsset);
            if (live && view != null && Game.Rendering.RuntimeCameraReferenceSystem.TryGetWorldCamera(World, out var camera))
            {
                var sites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
                for (int i = 0; i < sites.Length && i < 3; i++) markerScreenPositions[i] = camera.WorldToScreenPoint(sites[i].Position);
                markerScreenPositions[3] = camera.WorldToScreenPoint(EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Position);
                markerScreenPositions[4] = camera.WorldToScreenPoint(mission.ExitPosition);
                view.PresentMarkers(markerScreenPositions);
            }
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
        }

        private void Handle(UiOperationsMissionRequest request, Entity root, OperationsReconMissionComponent mission, bool live)
        {
            if (request.Action == UiOperationsMissionAction.Deploy && !live) { Deploy(); return; }
            if (!live) return;
            if (request.Action == UiOperationsMissionAction.Return)
            {
                if (mission.Phase != OperationsReconPhase.Terminal || !resultSaved) return;
                BeginReturn(root, string.Empty);
                return;
            }
            if (mission.Phase != OperationsReconPhase.Playing) return;
            if (request.Action == UiOperationsMissionAction.PromptWithdraw)
            { view?.ShowWithdrawConfirmation(); return; }
            var sites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
            if (request.Action == UiOperationsMissionAction.FocusSite && request.SiteIndex >= 0 && request.SiteIndex < sites.Length)
            { Focus(sites[request.SiteIndex].Position); return; }
            if (request.Action == UiOperationsMissionAction.FocusEvidence)
            { if (mission.CompletedScans == 3) Focus(EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Position); return; }
            if (request.Action == UiOperationsMissionAction.FocusExit) { Focus(mission.ExitPosition); return; }
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
            }
            EntityManager.GetBuffer<OperationsReconActionElement>(root).Add(new OperationsReconActionElement
            { SessionId = mission.SessionId, Action = action, Actor = actor, SiteIndex = request.SiteIndex });
            notice = string.Empty;
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
            view = null; resultSaved = false; settlementCommand = null; notice = message;
            return true;
        }

        private void Deploy()
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
            if (!OperationsReconLaunchProjection.TryQueue(EntityManager, definition, attempt.sessionId, out notice, unchecked((uint)save.activeRun.seed))) return;
            resultSaved = false; settlementCommand = null; nextSaveRetry = 0;
            UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.EnterMatch, UIRoute.Match, false);
        }

        private UiOperationsMissionModel Project(Entity root, OperationsReconMissionComponent mission, bool live)
        {
            var save = live ? null : commands.Read();
            var model = new UiOperationsMissionModel
            {
                Title = Copy("title", "STREET SIGNALS — OLD QUARTER"),
                Description = Copy("brief", "Scan three courtyards. Recover the relay evidence. Bring it and at least two original infantry to the secured ground exit.\n\n16 infantry • 12-minute deadline • 1 action point • Regular difficulty\nOptional: complete all scans without losing recon infantry."),
                Status = notice,
                CanDeploy = !live && definition != null && (!OperationsSaveMigration.HasActiveRun(save) || save.activeRun.actionPoints > 0 || save.pendingDeployment?.reserved == true),
                Clock = !OperationsSaveMigration.HasActiveRun(save) ? Copy("new_city", "A new city operation will begin on deployment.") :
                    string.Format(Copy("day_ap", "Day {0} • AP {1}"), save.activeRun.day, save.activeRun.actionPoints),
                InMission = live, Finished = live && mission.Phase == OperationsReconPhase.Terminal, Saved = resultSaved
            };
            if (!live) return model;
            int remaining = Mathf.Max(0, Mathf.CeilToInt(mission.DeadlineSeconds - mission.ElapsedSeconds));
            model.Clock = string.Format(Copy("clock", "{0}:{1} • {2}/16 infantry"), remaining / 60, (remaining % 60).ToString("00"), mission.SurvivingInfantry);
            model.Objective = mission.CompletedScans < 3 ? Copy("scan_objective", "Scan all three signal sites") + "  " + mission.CompletedScans + "/3"
                : EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root).Carrier == Entity.Null
                    ? Copy("evidence_objective", "Recover the relay evidence — hold for 15 seconds")
                    : Copy("exit_objective", "Bring the evidence and two original infantry to the safe exit");
            model.CanConclude = mission.PartialAvailable != 0;
            var evidence = EntityManager.GetComponentData<OperationsReconEvidenceComponent>(root);
            model.EvidenceAvailable = mission.CompletedScans == 3;
            model.CanRecover = model.EvidenceAvailable && evidence.Carrier == Entity.Null;
            model.EvidenceStatus = evidence.Carrier != Entity.Null ? Copy("carried", "EVIDENCE CARRIED") :
                string.Format(Copy("recover_progress", "RECOVER {0}/15 s"), Mathf.FloorToInt(evidence.ChannelSeconds));
            var sites = EntityManager.GetBuffer<OperationsReconSiteElement>(root);
            model.SiteStatus = new string[sites.Length];
            for (int i = 0; i < sites.Length; i++) model.SiteStatus[i] = string.Format(Copy("signal", "SIGNAL {0}"), (char)('A' + i)) + " • " +
                (sites[i].Completed != 0 ? Copy("done", "DONE") : Mathf.FloorToInt(sites[i].ChannelSeconds) + "/15 s");
            if (EntityManager.HasComponent<OperationsReconWaveComponent>(root))
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
            return model;
        }

        private void Focus(float3 position)
        {
            using var query = EntityManager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if (query.CalculateEntityCount() != 1) return;
            EntityManager.SetComponentData(query.GetSingletonEntity(), new RuntimeCameraFocusRequestComponent
            { Requested = 1, Smooth = 1, SmoothTimeSeconds = .3f, UseExplicitPerspective = 1, Perspective = new float4(40,58,0,60), World = position });
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
