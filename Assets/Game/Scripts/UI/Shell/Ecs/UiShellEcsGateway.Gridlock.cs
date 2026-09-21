using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiGridlockStoryGateway
    {
        public bool TryReplayGridlockChapter()
        {
            if(!TryGetBoundary(out var shell,out var boundary) ||
                shell.GetComponentData<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>(boundary).ActiveRoute!=UIRoute.Campaign ||
                !UiShellReadModelAdapter.TryReadCampaignOperations(out var campaign) ||
                campaign.SelectedMission.MissionId!="saga.ch02.m01.gridlock" || !campaign.SelectedMission.Available ||
                !TryGetMissionRoot(out var em,out var root))return false;
            if(!em.HasComponent<GridlockChapterReplayRequest>(root))em.AddComponent<GridlockChapterReplayRequest>(root);
            em.SetComponentData(root,new GridlockChapterReplayRequest {Pending=1});
            return true;
        }
        private static bool HasGridlockIntegrityDiagnostic()
        {
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionGridlockState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var state=em.GetComponentData<CampaignMissionGridlockState>(root);
            return runtime.MissionId.Equals("saga.ch02.m01.gridlock") && runtime.Outcome==Game.Missions.Contracts.MissionOutcomeKind.None &&
                state.SessionToken.Equals(runtime.SessionToken) && state.AttemptOrdinal==runtime.AttemptOrdinal &&
                state.SourceVersion==runtime.SourceVersion && state.Failure==GridlockFailure.Integrity;
        }
        private static UiMissionResultPopupModel LocalizeGridlockResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.GridlockFailure switch {
                GridlockFailure.FadiLost=>"fadi",GridlockFailure.WorkersLost=>"workers",GridlockFailure.RiflesLost=>"rifles",
                GridlockFailure.VehicleLost=>"vehicle",GridlockFailure.Deadline=>"deadline",_=>"integrity"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.gridlock.result."+(victory?"victory":"defeat")),GameText.Get("mission.gridlock.name"),
                GameText.Get(victory?"mission.gridlock.result.success":"mission.gridlock.failure."+reason),
                model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),
                GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired);
        }
        private static (int Clock,int A,int B,int StatusA,int StatusB,int Flags,int Hold) cachedGridlockStamp;
        private static (int Clock,int A,int B,int StatusA,int StatusB,int Flags,int Hold) ReadGridlockStamp()
        {
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionGridlockState>(root) || !em.HasBuffer<CampaignMissionGridlockWorkSite>(root))return default;
            var g=em.GetComponentData<CampaignMissionGridlockState>(root);var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root,true);
            if(sites.Length!=2)return default;
            return(g.ElapsedMilliseconds/1000,sites[0].WorkMilliseconds/1000,sites[1].WorkMilliseconds/1000,(int)sites[0].Status,(int)sites[1].Status,
                g.RouteContested+2*g.CounterattackWarned+4*g.RouteConnected+8*g.VehicleArrived+16*(int)g.Failure,g.HoldMilliseconds/1000);
        }
        private static string AppendGridlockStatus(string body)
        {
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionGridlockState>(root) || !em.HasBuffer<CampaignMissionGridlockWorkSite>(root))return body;
            var g=em.GetComponentData<CampaignMissionGridlockState>(root);var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root,true);
            if(sites.Length!=2)return body;
            if(g.Failure==GridlockFailure.Integrity) return GameText.Get("mission.gridlock.failure.integrity");
            string Work(GridlockWorkStatus status)=>GameText.Get(status switch {
                GridlockWorkStatus.Working=>"mission.gridlock.work.working",GridlockWorkStatus.ThreatNearby=>"mission.gridlock.work.threat_nearby",
                GridlockWorkStatus.Clearing=>"mission.gridlock.work.clearing",GridlockWorkStatus.Complete=>"mission.gridlock.work.complete",_=>"mission.gridlock.work.crew_missing"});
            string status=GameText.Format("mission.gridlock.hud.work","A: {0}/25 — {1}\nB: {2}/25 — {3}",sites[0].WorkMilliseconds/1000,Work(sites[0].Status),sites[1].WorkMilliseconds/1000,Work(sites[1].Status));
            if(g.CounterattackWarned!=0 && g.ElapsedMilliseconds<g.CounterattackReleaseAtMilliseconds) status=GameText.Get("mission.gridlock.warning")+"\n"+status;
            if(g.RouteConnected!=0) status+="\n"+GameText.Get(g.RouteContested!=0?"mission.gridlock.relief.waiting":"mission.gridlock.relief.moving");
            if(g.VehicleArrived!=0) status+="\n"+GameText.Format("mission.gridlock.hud.hold","Hospital secure: {0}/20",g.HoldMilliseconds/1000);
            return body+"\n\n"+status;
        }
    }
}
