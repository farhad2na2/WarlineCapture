using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionExtractionGateway
    {
        private static readonly FixedString64Bytes AirliftId="saga.ch01.m04.airlift";
        private static bool TryFindExtractionDefinition(in CampaignMissionCatalogComponent catalog,in CampaignMissionRuntimeComponent runtime,out int index)
        {
            index=-1;if(!catalog.Blob.IsCreated || catalog.SourceVersion!=runtime.SourceVersion)return false;
            for(int i=0;i<catalog.Blob.Value.Missions.Length;i++)
            {
                ref var candidate=ref catalog.Blob.Value.Missions[i];
                if(candidate.Extraction.Enabled!=0 && candidate.MissionId.Equals(runtime.MissionId) &&
                    candidate.ScenarioId.Equals(runtime.ScenarioId) && candidate.OperationMapId.Equals(runtime.OperationMapId))
                {index=i;return true;}
            }
            return false;
        }
        private static bool TryFindExtractionAnchor(ref OperationMapBlob map,in FixedString64Bytes id,out OperationMapAnchorBlob anchor)
        {anchor=default;for(int i=0;i<map.Anchors.Length;i++)if(map.Anchors[i].Id.Equals(id)){anchor=map.Anchors[i];return true;}return false;}
        public bool IsExtractionGuideContext()
        {
            if(TryGetMissionRoot(out var em,out var root) && em.GetComponentData<CampaignMissionRuntimeComponent>(root).MissionId.Equals(AirliftId) &&
                TryGetBoundary(out var shell,out var boundary) && shell.GetComponentData<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>(boundary).ActiveRoute==UIRoute.Match) return true;
            return UiShellReadModelAdapter.TryReadCampaignOperations(out var campaign) && campaign.SelectedMission.MissionId==AirliftId.ToString();
        }
        public bool TryReadMissionExtraction(out UiMissionExtractionModel model)
        {
            model=default;
            if(!TryGetMissionRoot(out var em,out var root) || !em.HasComponent<CampaignMissionExtractionState>(root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            if(!runtime.MissionId.Equals(AirliftId) || runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None ||
                extraction.Ready==0 || !extraction.SessionToken.Equals(runtime.SessionToken) || extraction.AttemptOrdinal!=runtime.AttemptOrdinal || extraction.SourceVersion!=runtime.SourceVersion) return false;
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
            if(!TryFindExtractionDefinition(in catalog,in runtime,out int index)) return false;
            ref var config=ref catalog.Blob.Value.Missions[index].Extraction;
            int lesson=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).GuidanceId-55000;
            model=new UiMissionExtractionModel(facts.ExtractionPassengersAboard,facts.ExtractionPassengersDelivered,config.RequiredPassengers,facts.ExtractionCarrierLegCount,
                facts.ExtractionSecureMilliseconds/1000,math.max(0,(config.DeadlineMilliseconds-facts.ElapsedMilliseconds+999)/1000),lesson,
                facts.ExtractionContested!=0,extraction.DepartureCleared!=0);return true;
        }
        public bool TryRequestExtractionAction(UiMissionExtractionAction action)
        {
            if(!TryReadMissionExtraction(out var model) || !TryGetMissionRoot(out var em,out var root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            using var gameplay=em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if(gameplay.CalculateEntityCount()!=1 || gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive==0) return false;
            if(action==UiMissionExtractionAction.ContinuePlan)
            {
                // Acknowledgement applies only to explanation one. Real transport lessons advance on ECS facts.
                if(model.Lesson!=1) return false;
                var requests=em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
                if(requests.Length>=8)return false;
                requests.Add(new CampaignMissionGuidanceAcknowledgementRequestElement {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,GuidanceId=55001});return true;
            }
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            float3 target=action==UiMissionExtractionAction.FocusDeparture || action==UiMissionExtractionAction.ShowLesson && model.Lesson>=11 ? extraction.DepartureCenter : extraction.LandingCenter;
            if(action==UiMissionExtractionAction.FocusTeam || action==UiMissionExtractionAction.ShowLesson && model.Lesson<=5)
            {
                var catalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
                if(!TryFindExtractionDefinition(in catalog,in runtime,out int index))return false;
                using var map=em.CreateEntityQuery(ComponentType.ReadOnly<OperationMapMetadataComponent>());
                if(map.CalculateEntityCount()!=1)return false;
                var metadata=map.GetSingleton<OperationMapMetadataComponent>();
                if(!TryFindExtractionAnchor(ref metadata.Blob.Value,catalog.Blob.Value.Missions[index].Extraction.RescueAnchorId,out var anchor))return false;
                target=anchor.Position;
            }
            using var focus=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));if(focus.CalculateEntityCount()!=1)return false;
            em.SetComponentData(focus.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent {Requested=1,Smooth=1,SmoothTimeSeconds=.5f,World=target});return true;
        }
    }
}
