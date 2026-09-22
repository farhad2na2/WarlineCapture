using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiSupplyLineGateway
    {
        private static UiMissionResultPopupModel LocalizeSupplyLineResult(in UiMissionResultPopupModel model,in CampaignMissionAttemptFactsComponent facts)
        {
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            string reason=facts.SupplyFailure switch {SupplyLineFailure.SquadLost=>"squad",SupplyLineFailure.HaulerLost=>"hauler",SupplyLineFailure.LinkLost=>"link",SupplyLineFailure.Deadline=>"deadline",_=>"integrity"};
            return new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,
                GameText.Get("mission.supply_line.result."+(victory?"victory":"defeat")),GameText.Get("mission.supply_line.name"),
                GameText.Get(victory?"mission.supply_line.result.success":"mission.supply_line.failure."+reason),
                model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,
                victory?model.RewardsText:GameText.Get("mission.m03.result.no_reward"),
                GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),model.PrimaryActionEnabled,model.RetryVisible,model.FirstClear,model.DebriefRequired);
        }
        private static bool TryFormatSupplyLineResources(out string oil,out string fuel)
        {
            oil="0";fuel=null;
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionSupplyLineState>(root) || !em.HasBuffer<CampaignMissionSupplyLineLink>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var supply=em.GetComponentData<CampaignMissionSupplyLineState>(root);
            if(!runtime.MissionId.Equals(CampaignMissionSequence.SupplyLine) || runtime.Outcome!=MissionOutcomeKind.None || !supply.SessionToken.Equals(runtime.SessionToken) || supply.AttemptOrdinal!=runtime.AttemptOrdinal || supply.SourceVersion!=runtime.SourceVersion)return false;
            int stored=(int)Unity.Mathematics.math.floor(supply.StoredFuel);
            float totalOil=0;
            foreach(var link in em.GetBuffer<CampaignMissionSupplyLineLink>(root,true))
                if(em.Exists(link.Entity) && em.HasComponent<BuildingResourceStorageComponent>(link.Entity))totalOil+=em.GetComponentData<BuildingResourceStorageComponent>(link.Entity).StoredOilBarrels;
            foreach(var member in em.GetBuffer<CampaignMissionSupplyLineMember>(root,true))
                if(member.Kind==1 && em.Exists(member.Entity) && em.HasComponent<UnitResourceHauler>(member.Entity))totalOil+=em.GetComponentData<UnitResourceHauler>(member.Entity).CargoOilBarrels;
            oil=((int)Unity.Mathematics.math.floor(totalOil)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            fuel=stored.ToString(System.Globalization.CultureInfo.InvariantCulture);return true;
        }
        public bool TryReadSupplyLine(out int storedFuel,out int civilianReserve,out bool canAllocate)
        {
            storedFuel=civilianReserve=0;canAllocate=false;
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionSupplyLineState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var s=em.GetComponentData<CampaignMissionSupplyLineState>(root);
            if(!runtime.MissionId.Equals(CampaignMissionSequence.SupplyLine) || runtime.Outcome!=MissionOutcomeKind.None || !s.SessionToken.Equals(runtime.SessionToken) || s.AttemptOrdinal!=runtime.AttemptOrdinal || s.SourceVersion!=runtime.SourceVersion)return false;
            storedFuel=(int)Unity.Mathematics.math.floor(s.StoredFuel);civilianReserve=s.AllocatedCivilianBarrels;
            canAllocate=s.Ready!=0 && s.Failure==SupplyLineFailure.None && runtime.Phase==MissionPhaseKind.Engage && storedFuel>=20 && civilianReserve==0;
            return true;
        }
        public bool TryAllocateSupplyLineReserve()
        {
            if(!TryReadSupplyLine(out _,out _,out bool canAllocate) || !canAllocate || !TryGetMissionRoot(out var em,out var root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            em.SetComponentData(root,new CampaignMissionSupplyLineAllocationRequest {Pending=1,SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});return true;
        }
    }
}
