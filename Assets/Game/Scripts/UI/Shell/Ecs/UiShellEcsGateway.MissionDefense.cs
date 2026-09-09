using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionDefenseGateway
    {
        private World defenseGameplayWorld;
        private EntityQuery defenseGameplayQuery;
        // Keep each value tuple at seven fields or fewer: the nested Rest tuple
        // constructor boxes its tail on this Editor's Mono runtime.
        private (World World,Entity Root,FixedString64Bytes Session,int Attempt,uint Source) defenseAttemptKey;
        private (uint Ledger,uint Ping,int Cooldown,bool Active,bool Sensor,int Guidance,bool Return) defenseReadKey;
        private string defenseReadLocale;
        private UiMissionDefenseModel defenseReadModel;
        private bool hasDefenseReadModel;
        private EntityQuery GetDefenseGameplayQuery(EntityManager em)
        {
            if(defenseGameplayWorld!=em.World)
            {
                if(defenseGameplayWorld!=null && defenseGameplayWorld.IsCreated) defenseGameplayQuery.Dispose();
                defenseGameplayWorld=em.World;
                defenseGameplayQuery=em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            }
            return defenseGameplayQuery;
        }
        public bool TryReadMissionDefense(out UiMissionDefenseModel model)
        {
            model=default;
            if (!TryGetMissionRoot(out EntityManager em,out Entity root) || !em.HasComponent<RadarPingState>(root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var ping=em.GetComponentData<RadarPingState>(root);
            if (!ping.SessionToken.Equals(runtime.SessionToken) || ping.AttemptOrdinal!=runtime.AttemptOrdinal ||
                ping.SourceVersion!=runtime.SourceVersion || runtime.Outcome!=MissionOutcomeKind.None || runtime.Phase!=MissionPhaseKind.Engage) return false;
            var ledger=em.GetComponentData<ThreatWarningLedgerState>(root);
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            var gameplayQuery=GetDefenseGameplayQuery(em);
            if(gameplayQuery.CalculateEntityCount()!=1) return false;
            var gameplay=gameplayQuery.GetSingleton<RuntimeGameplayStateComponent>();
            if(gameplay.PlayRequested==0) return false;
            bool active=gameplay.SimulationActive!=0;
            int cooldown=math.max(0,(ping.ReadyAtMilliseconds-facts.ElapsedMilliseconds+999)/1000);
            bool sensor=RadarPingRequestSystem.FindSensor(em,root)!=Entity.Null;
            bool canPing=active && ping.Charges>0 && cooldown==0 && sensor && ping.PendingRequestId==0;
            int guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).GuidanceId;
            bool canReturn=active && em.GetComponentData<CampaignMissionDefenseStateComponent>(root).FocusReturnAvailable!=0;
            var attemptKey=(em.World,root,runtime.SessionToken,runtime.AttemptOrdinal,runtime.SourceVersion);
            var key=(ledger.Version,ping.Version,cooldown,active,sensor,guidance,canReturn);
            string locale=GameLocalization.CurrentLocaleCode;
            if(hasDefenseReadModel && defenseAttemptKey.Equals(attemptKey) && defenseReadKey.Equals(key) && defenseReadLocale==locale)
            {model=defenseReadModel; return true;}
            string status=!active ? GameText.Get("mission.m03.ping.paused","Paused") :
                !sensor ? GameText.Get("mission.m03.ping.no_sensor","Ground sensor unavailable") :
                ping.Charges==0 ? GameText.Get("mission.m03.ping.empty","No uses remaining") :
                cooldown>0 ? GameText.Format("mission.m03.ping.cooldown","Ready in {0}s",cooldown) :
                GameText.Format("mission.m03.ping.ready","Radar Ping · {0} uses",ping.Charges);
            string warning=string.Empty; bool hasWarning=false,focus=false,attention=false;
            var records=em.GetBuffer<ThreatWarningRecord>(root,true);
            for(int i=0;i<records.Length;i++)
                if(ledger.Active!=0 && records[i].ElementIndex==ledger.FocusElementIndex && records[i].Resolved==0)
                { warning=ThreatWarningDisplayText.Build(records[i]); hasWarning=true; focus=active && records[i].HasFocus!=0; attention=records[i].AttentionEscalated!=0 && records[i].ReadByPlayer==0; break; }
            model=new UiMissionDefenseModel(true,hasWarning,focus,canPing,warning,status,ping.Charges,cooldown,ledger.FocusElementIndex,guidance,ledger.Version+ping.Version,canReturn,attention);
            defenseAttemptKey=attemptKey; defenseReadKey=key; defenseReadLocale=locale;
            defenseReadModel=model; hasDefenseReadModel=true;
            return true;
        }

        public bool TryRequestMissionDefenseAction(UiMissionDefenseAction action,int warningElementIndex,int guidanceId)
        {
            if(action is UiMissionDefenseAction.SkipCameraTour or UiMissionDefenseAction.ReduceCameraMotion)
                return RequestCameraTourAction(action);
            if(action is UiMissionDefenseAction.CloseWarning or UiMissionDefenseAction.OpenGuide or UiMissionDefenseAction.CloseGuide)
            {
                if(!TryGetBoundary(out EntityManager shell,out Entity boundary)) return false;
                UiShellPopupKind popup=action==UiMissionDefenseAction.CloseWarning ? UiShellPopupKind.ThreatAlert : UiShellPopupKind.MissionFieldGuide;
                UiShellPopupIntent intent=action==UiMissionDefenseAction.OpenGuide ? UiShellPopupIntent.Show : UiShellPopupIntent.Hide;
                if(action==UiMissionDefenseAction.OpenGuide)
                {
                    if(!TryGetGuideMission(out var mission,out _)) return false;
                    var context=new UiMissionFieldGuideContextComponent {SessionToken=mission.SessionToken,AttemptOrdinal=mission.AttemptOrdinal,SourceVersion=mission.SourceVersion};
                    if(shell.HasComponent<UiShellActivePopupComponent>(boundary))
                    {
                        var active=shell.GetComponentData<UiShellActivePopupComponent>(boundary);
                        // Resolve the caller's context after queued close/show intents as well.
                        // A guide opened just after Jump must return to the HUD.
                        var pending=shell.GetBuffer<UiShellPopupRequestComponent>(boundary);
                        for(int i=0;i<pending.Length;i++)
                        {
                            if(pending[i].Intent==UiShellPopupIntent.Show)
                            {active.PopupKind=pending[i].PopupKind; active.Visible=1;}
                            else if(active.PopupKind==pending[i].PopupKind) active.Visible=0;
                        }
                        if(active.Visible!=0 && active.PopupKind==UiShellPopupKind.MissionFieldGuide) return true;
                        if(active.Visible!=0 && active.PopupKind is UiShellPopupKind.Pause or UiShellPopupKind.Settings or UiShellPopupKind.ThreatAlert)
                        {context.ReturnPopup=active.PopupKind; context.HasReturnPopup=1;}
                    }
                    if(!shell.HasComponent<UiMissionFieldGuideContextComponent>(boundary)) shell.AddComponentData(boundary,context);
                    else shell.SetComponentData(boundary,context);
                }
                else if(action==UiMissionDefenseAction.CloseGuide && shell.HasComponent<UiMissionFieldGuideContextComponent>(boundary))
                {
                    var context=shell.GetComponentData<UiMissionFieldGuideContextComponent>(boundary);
                    if(context.HasReturnPopup!=0 && TryGetMissionRoot(out EntityManager missionEm,out Entity missionRoot))
                    {
                        var mission=missionEm.GetComponentData<CampaignMissionRuntimeComponent>(missionRoot);
                        if(context.SessionToken.Equals(mission.SessionToken) && context.AttemptOrdinal==mission.AttemptOrdinal && context.SourceVersion==mission.SourceVersion)
                        {popup=context.ReturnPopup; intent=UiShellPopupIntent.Show;}
                    }
                    shell.SetComponentData(boundary,default(UiMissionFieldGuideContextComponent));
                }
                shell.GetBuffer<UiShellPopupRequestComponent>(boundary).Add(new UiShellPopupRequestComponent
                    {PopupKind=popup,Intent=intent});
                return true;
            }
            if(!TryReadMissionDefense(out var model) || !TryGetMissionRoot(out EntityManager em,out Entity root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(action==UiMissionDefenseAction.OpenWarning)
            {
                if(!model.HasWarning || !TryGetBoundary(out EntityManager shell,out Entity boundary) ||
                    !shell.HasBuffer<UiShellPopupRequestComponent>(boundary)) return false;
                shell.GetBuffer<UiShellPopupRequestComponent>(boundary).Add(new UiShellPopupRequestComponent
                    {PopupKind=UiShellPopupKind.ThreatAlert,Intent=UiShellPopupIntent.Show});
                action=UiMissionDefenseAction.ReadWarning;
            }
            if(action==UiMissionDefenseAction.RadarPing)
            {
                if(!model.CanPing) return false;
                var requests=em.GetBuffer<RadarPingRequest>(root);
                if(requests.Length>=8) return false;
                uint next=em.GetComponentData<RadarPingState>(root).LastRequestId;
                for(int i=0;i<requests.Length;i++) next=math.max(next,requests[i].RequestId);
                if(next==uint.MaxValue) return false;
                requests.Add(new RadarPingRequest {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,
                    SourceVersion=runtime.SourceVersion,RequestId=next+1});
                return true;
            }
            if(action==UiMissionDefenseAction.ReturnCamera ? !model.CanReturnCamera : action is < UiMissionDefenseAction.ReadWarning or > UiMissionDefenseAction.SkipOptional) return false;
            if(action==UiMissionDefenseAction.FocusWarning && !model.CanFocus) return false;
            var interactions=em.GetBuffer<MissionDefenseInteractionRequest>(root);
            if(interactions.Length>=8) return false;
            interactions.Add(new MissionDefenseInteractionRequest {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,
                SourceVersion=runtime.SourceVersion,Kind=(MissionDefenseInteractionKind)action,
                WarningElementIndex=warningElementIndex<0 ? model.WarningElementIndex : warningElementIndex,
                GuidanceId=guidanceId==0 ? model.GuidanceId : guidanceId});
            return true;
        }
    }
}
