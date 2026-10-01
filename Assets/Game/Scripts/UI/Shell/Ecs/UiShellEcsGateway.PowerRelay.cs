using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiPowerRelayLandmarkGateway
    {
        public bool TryReadPowerRelayShelter(out UnityEngine.Vector3 center,out bool sheltered)
        {
            center=default;sheltered=false;
            if(!TryGetMissionRoot(out var em,out var root)||!em.HasComponent<CampaignMissionPowerRelayState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(!runtime.MissionId.Equals("saga.ch02.m04.power_relay")||runtime.Phase!=Game.Missions.Contracts.MissionPhaseKind.Engage||runtime.Outcome!=Game.Missions.Contracts.MissionOutcomeKind.None)return false;
            using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            if(maps.CalculateEntityCount()!=1)return false;
            var metadata=maps.GetSingleton<OperationMapMetadataComponent>();
            if(!metadata.Blob.IsCreated||!metadata.Blob.Value.OperationMapId.Equals("opmap.ch02.power_relay_refinery_review"))return false;
            var state=em.GetComponentData<CampaignMissionPowerRelayState>(root);
            center=new UnityEngine.Vector3(state.ShelterCell.x,.03f,state.ShelterCell.y);sheltered=state.FamiliesSheltered!=0;
            return state.Ready!=0;
        }
        private static UiMissionResultPopupModel LocalizePowerRelayResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.PowerRelayFailure switch {PowerRelayFailure.SquadLost=>"squad",PowerRelayFailure.EngineerLost=>"engineer",PowerRelayFailure.FamilyConvoyLost=>"families",PowerRelayFailure.FuelServiceLost=>"fuel",PowerRelayFailure.Deadline=>"deadline",_=>"integrity"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,GameText.Get("mission.power_relay.result."+(victory?"victory":"defeat")),GameText.Get("mission.power_relay.name"),
                GameText.Get(victory?"mission.power_relay.result.success":"mission.power_relay.failure."+reason),model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired);
        }
    }
}
