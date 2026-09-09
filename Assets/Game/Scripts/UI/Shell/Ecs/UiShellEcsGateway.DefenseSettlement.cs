using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Collections;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static readonly FixedString64Bytes DefenseSettlementFailedReason="settlement-failed";
        private static bool HasDefenseSettlementFailure(EntityManager em,Entity root,
            in CampaignMissionRuntimeComponent runtime,in CampaignMissionResultComponent result)
        {
            if(!runtime.MissionId.Equals(RadarResultMissionId) && !runtime.MissionId.Equals(AirliftId) || runtime.Phase!=MissionPhaseKind.Result ||
                runtime.Outcome!=MissionOutcomeKind.Victory || result.Outcome!=MissionOutcomeKind.Victory ||
                result.SourceVersion==0 || !result.SessionToken.Equals(runtime.SessionToken) ||
                !result.MissionId.Equals(runtime.MissionId) || result.AttemptOrdinal!=runtime.AttemptOrdinal ||
                !em.HasBuffer<CampaignMissionSettlementResultElement>(root)) return false;
            var responses=em.GetBuffer<CampaignMissionSettlementResultElement>(root,true);
            for(int i=responses.Length-1;i>=0;i--)
            {
                var response=responses[i];
                if(response.SourceVersion!=result.SourceVersion || !response.SessionToken.Equals(result.SessionToken)) continue;
                return response.Accepted==0 && response.ReasonCode.Equals(DefenseSettlementFailedReason);
            }
            return false;
        }
        private static bool TryReadDefenseSettlementFailure(EntityManager em,Entity root,
            in CampaignMissionRuntimeComponent runtime,in CampaignMissionResultComponent result,out UiMissionResultPopupModel model)
        {
            model=default;
            if(!HasDefenseSettlementFailure(em,root,in runtime,in result)) return false;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            int seconds=result.ElapsedMilliseconds/1000;
            bool canRetry=em.HasBuffer<CampaignMissionSettlementRequestElement>(root) && em.GetBuffer<CampaignMissionSettlementRequestElement>(root,true).Length==0;
            model=new UiMissionResultPopupModel(result.SourceVersion,result.MissionId.ToString(),UiMissionResultOutcome.Victory,
                GameText.Get("mission.m03.save.failed"),GameText.Get(runtime.MissionId.Equals(AirliftId)?"mission.m04.name":"mission.m03.name"),GameText.Get("mission.m03.save.body"),
                result.Stars,$"{seconds/60:00}:{seconds%60:00}",result.SquadLossCount.ToString(),
                $"{facts.HostileDefeatedCount}/{facts.HostileTotalCount}",GameText.Get("mission.m03.save.pending"),
                GameText.Get("mission.m03.save.retry"),canRetry,false,false,true,
                new UiMissionDefenseResultDetails(facts.CivilianLossCount,facts.ForwardPostDamaged!=0,facts.ForwardPostDestroyed!=0,
                    facts.CoreBreached!=0,facts.HostileRosterIntegrityFault!=0),true);
            if(runtime.MissionId.Equals(AirliftId)) model=new UiMissionResultPopupModel(model.Version,model.MissionId,model.Outcome,model.Title,model.Subtitle,model.SummaryBody,
                model.Stars,model.ElapsedText,model.SquadLossText,model.EnemiesDefeatedText,model.RewardsText,model.PrimaryActionLabel,model.PrimaryActionEnabled,
                false,false,true,default,true,new UiMissionExtractionResultDetails(facts.ExtractionPassengersDelivered,facts.CivilianLossCount,facts.ExtractionCarrierLegCount,
                    facts.ExtractionCarrierLost!=0,facts.ExtractionAircraftLost!=0,facts.ExtractionTimedOut!=0));
            return true;
        }
        internal static bool TryRetryDefenseSettlement()
        {
            if(!TryGetMissionRoot(out EntityManager em,out Entity root) ||
                !em.HasComponent<CampaignMissionResultComponent>(root) || !em.HasBuffer<CampaignMissionSettlementRequestElement>(root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            var result=em.GetComponentData<CampaignMissionResultComponent>(root);
            if(!HasDefenseSettlementFailure(em,root,in runtime,in result)) return false;
            var requests=em.GetBuffer<CampaignMissionSettlementRequestElement>(root);
            if(requests.Length!=0) return false;
            requests.Add(new CampaignMissionSettlementRequestElement {MissionId=result.MissionId,SessionToken=result.SessionToken,
                AttemptOrdinal=result.AttemptOrdinal,SourceVersion=result.SourceVersion,Outcome=result.Outcome});
            return true;
        }
    }
}
