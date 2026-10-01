using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiGroundedSignalGateway
    {
        private static (int Stage, int Recovery, int Exit, int Contested) cachedGroundedSignalStatusStamp;
        private static (int Stage, int Recovery, int Exit, int Contested) ReadGroundedSignalStatusStamp()
        {
            if(!TryGroundedSignal(out var em,out var root,out var mission))return (-1,-1,-1,-1);
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            return (GroundedStage(in mission,in facts),math.clamp(mission.RecoveryHoldMilliseconds/1000,0,6),
                math.clamp(facts.ExtractionSecureMilliseconds/1000,0,6),facts.ExtractionContested);
        }
        private static string AppendGroundedSignalStatus(string text)
        {
            var status=ReadGroundedSignalStatusStamp();
            if(status.Stage==3)return text+"\n\n"+GameText.Format("mission.grounded_signal.hud.recovery","Hardware recovery: {0}/6 s",status.Recovery);
            if(status.Stage==5)return text+"\n\n"+GameText.Format("mission.grounded_signal.hud.exit","Exit secure: {0}/6 s",status.Exit);
            return text;
        }
        private static bool TryGroundedSignal(out EntityManager em,out Entity root,out CampaignMissionGroundedSignalState mission)
        {
            mission=default;
            if(!TryGetMissionRoot(out em,out root) || !em.HasComponent<CampaignMissionGroundedSignalState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            mission=em.GetComponentData<CampaignMissionGroundedSignalState>(root);
            return runtime.MissionId.Equals(CampaignMissionSequence.GroundedSignal) && runtime.Phase==MissionPhaseKind.Engage && runtime.Outcome==MissionOutcomeKind.None &&
                mission.Initialized!=0 && mission.SessionToken.Equals(runtime.SessionToken) && mission.AttemptOrdinal==runtime.AttemptOrdinal && mission.SourceVersion==runtime.SourceVersion;
        }
        private static int GroundedStage(in CampaignMissionGroundedSignalState mission,in CampaignMissionAttemptFactsComponent facts)=>
            mission.Inserted==0?1:mission.RelayDisabled==0?2:mission.HardwareRecovered==0?3:mission.SpecialistsAboardCarrier<2?4:5;
        public bool TryReadGroundedSignal(out UiGroundedSignalModel model)
        {
            model=default;if(!TryGroundedSignal(out var em,out var root,out var mission))return false;
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(!TryFindExtractionDefinition(in catalog,in runtime,out int index))return false;
            ref var config=ref catalog.Blob.Value.Missions[index].Extraction;
            model=new UiGroundedSignalModel(GroundedStage(in mission,in facts),mission.RecoveryHoldMilliseconds/1000,
                facts.ExtractionSecureMilliseconds/1000,math.max(0,(config.DeadlineMilliseconds-mission.ElapsedMilliseconds+999)/1000),
                mission.RelayDisabled!=0,mission.HardwareRecovered!=0,facts.ExtractionContested!=0,mission.ApronCenter,mission.HardwareCenter,extraction.LandingCenter);
            return true;
        }
        private static bool ResolveGroundedSignalTarget(EntityManager em,Entity root,CampaignMissionGuidanceProjectionComponent guidance,out UiMissionTutorialTarget target)
        {
            target=default;
            if(!em.HasComponent<CampaignMissionGroundedSignalState>(root) || !em.Exists(guidance.SourceEntity) || !em.HasComponent<LocalTransform>(guidance.SourceEntity))return false;
            int stage=guidance.GuidanceId-66000;
            var mission=em.GetComponentData<CampaignMissionGroundedSignalState>(root);
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            bool selected=em.HasComponent<SelectedUnitTag>(guidance.SourceEntity);
            float3 selection=em.GetComponentData<LocalTransform>(guidance.SourceEntity).Position;
            float3 selectionMin=selection,selectionMax=selection;
            int requiredSelection=1;
            bool moving=IsTutorialActorMoving(em,guidance.SourceEntity);
            if(stage is 3 or 4)
            {
                selected=true;
                selection=default;selectionMin=new float3(float.MaxValue);selectionMax=new float3(float.MinValue);requiredSelection=0;moving=false;
                foreach(var specialist in em.GetBuffer<CampaignMissionGroundedSignalSpecialist>(root,true))
                {
                    if(!em.Exists(specialist.Entity) || !em.HasComponent<LocalTransform>(specialist.Entity)){selected=false;continue;}
                    // A specialist already aboard the extraction APC has completed the boarding action.
                    if(stage==4 && em.HasComponent<UnitTransportPassenger>(specialist.Entity) &&
                        em.GetComponentData<UnitTransportPassenger>(specialist.Entity).Transport==extraction.Carrier)continue;
                    float3 position=em.GetComponentData<LocalTransform>(specialist.Entity).Position;
                    selection+=position;selectionMin=math.min(selectionMin,position);selectionMax=math.max(selectionMax,position);requiredSelection++;
                    selected &= em.HasComponent<SelectedUnitTag>(specialist.Entity);
                    moving |= IsTutorialActorMoving(em,specialist.Entity);
                }
                if(requiredSelection>0)selection/=requiredSelection;
                else {selection=em.GetComponentData<LocalTransform>(guidance.SourceEntity).Position;selectionMin=selectionMax=selection;}
            }
            bool relayAttack=stage==2 && guidance.RecommendationKind==AssistantRecommendationKind.Attack;
            var action=stage==2?relayAttack?UiTutorialBattleAction.Attack:UiTutorialBattleAction.Move:stage is 3 or 5?UiTutorialBattleAction.Move:UiTutorialBattleAction.None;
            if(stage==3 && mission.RecoveryHoldMilliseconds>0 || stage==5 && extraction.SecureHoldMilliseconds>0)action=UiTutorialBattleAction.Watch;
            target=new UiMissionTutorialTarget(selection,guidance.WorldPosition,
                !selected,moving,math.max(1,requiredSelection),action,
                executingAttack:relayAttack && IsTutorialAttackInProgress(em,guidance.SourceEntity,mission.Relay),areaRadius:stage==3?mission.HardwareRadius:8,
                selectionLabelKey:stage is 3 or 4?"ui.aria.select_specialists":"ui.aria.select_group",
                dragSelection:stage is 3 or 4 && requiredSelection>1,
                selectionMin:selectionMin-new float3(3,0,3),selectionMax:selectionMax+new float3(3,3,3));
            return true;
        }
        private static string GroundedSignalStatus(EntityManager em,Entity root)
        {
            var mission=em.GetComponentData<CampaignMissionGroundedSignalState>(root);
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            int stage=GroundedStage(in mission,in facts);
            string text=GameText.Get("mission.grounded_signal.hud.stage."+stage);
            if(stage==2 && em.HasComponent<CampaignMissionGuidanceProjectionComponent>(root) &&
                em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).RecommendationKind==AssistantRecommendationKind.Move)
                text=GameText.Get("mission.grounded_signal.hud.stage.2.approach");
            if(stage==2 && em.HasComponent<CampaignMissionGuidanceProjectionComponent>(root))
            {
                var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
                if(guidance.SourceEntity==extraction.Carrier)
                    text=GameText.Get(guidance.RecommendationKind==AssistantRecommendationKind.Move?
                        "mission.grounded_signal.hud.stage.2.apc_approach":"mission.grounded_signal.hud.stage.2.apc");
            }
            if(stage==3)text+="\n"+GameText.Format("mission.grounded_signal.hud.recovery","Hardware recovery: {0}/6 s",mission.RecoveryHoldMilliseconds/1000);
            if(stage==5)text+="\n"+GameText.Format("mission.grounded_signal.hud.exit","Exit secure: {0}/6 s",facts.ExtractionSecureMilliseconds/1000);
            return text;
        }
        private static UiMissionResultPopupModel LocalizeGroundedSignalResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            string reason="integrity";
            int terminalLosses=0;
            bool hasRoot=TryGetMissionRoot(out var em,out var root);
            if(hasRoot && em.HasComponent<CampaignMissionGroundedSignalState>(root))
                reason=em.GetComponentData<CampaignMissionGroundedSignalState>(root).Failure switch
                {GroundedSignalFailure.SpecialistLost=>"passenger_lost",GroundedSignalFailure.CarrierLost=>"carrier_lost",GroundedSignalFailure.AircraftLost=>"aircraft_lost",GroundedSignalFailure.TerminalLost=>"terminal_lost",GroundedSignalFailure.Deadline=>"timeout",_=>"integrity"};
            if(hasRoot && em.Exists(root) && em.HasBuffer<CampaignMissionGroundedSignalProtectedMember>(root))
                foreach(var member in em.GetBuffer<CampaignMissionGroundedSignalProtectedMember>(root,true))
                    if(member.HealthInitialized!=0 && (!em.Exists(member.Entity) || !em.HasComponent<UnitHealth>(member.Entity) || em.GetComponentData<UnitHealth>(member.Entity).Current<=0))terminalLosses++;
            if(facts.GroundedSignalTerminalLost!=0)terminalLosses=math.max(1,terminalLosses);
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.grounded_signal.result."+(victory?"victory":"defeat")),GameText.Get("mission.grounded_signal.result.subtitle"),
                GameText.Get("mission.grounded_signal.result."+(victory?"success":reason)),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),
                model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired,
                settlementFailed:model.SettlementFailed,
                extraction:new UiMissionExtractionResultDetails(facts.ExtractionPassengersDelivered,facts.CivilianLossCount,facts.ExtractionCarrierLegCount,
                    facts.ExtractionCarrierLost!=0,facts.ExtractionAircraftLost!=0,facts.ExtractionTimedOut!=0,facts.SquadLossCount,facts.ElapsedMilliseconds),
                civilianLossCount:terminalLosses);
        }
    }
}
