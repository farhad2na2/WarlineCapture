using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionTutorialTargetGateway
    {
        public bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target)
        {
            if(!TryReadMissionTutorialTargetCore(out target))return false;
            if(target.NeedsSelection)target=WithTutorialSelectionBounds(target);
            return true;
        }
        private bool TryReadMissionTutorialTargetCore(out UiMissionTutorialTarget target)
        {
            target=default;
            if (!TryGetMissionRoot(out var em,out var root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(runtime.Outcome!=MissionOutcomeKind.None ||
                !em.HasComponent<CampaignMissionGuidanceProjectionComponent>(root)) return false;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(guidance.Active==0) return false;
            if (TryResolveEarlyMissionTutorialTarget(em, root, runtime, guidance, out target)) return true;
            if (runtime.Phase != MissionPhaseKind.Engage) return false;
            if((runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes(CampaignMissionSequence.SupplyLine)) && guidance.GuidanceId>=76001 && guidance.GuidanceId<=76004 || runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes(CampaignMissionSequence.Gridlock))) &&
                (guidance.GuidanceId>=75001 && guidance.GuidanceId<=75010 || guidance.GuidanceId>=76001 && guidance.GuidanceId<=76004) && em.Exists(guidance.SourceEntity) && em.HasComponent<LocalTransform>(guidance.SourceEntity))
            {
                target=new UiMissionTutorialTarget(em.GetComponentData<LocalTransform>(guidance.SourceEntity).Position,guidance.WorldPosition,
                    !em.HasComponent<SelectedUnitTag>(guidance.SourceEntity),
                    IsTutorialActorMoving(em,guidance.SourceEntity) && (!em.HasComponent<UnitResourceHauler>(guidance.SourceEntity) || em.HasComponent<ManualMoveOrderTag>(guidance.SourceEntity)),
                    battleAction:guidance.CanExecute==0?UiTutorialBattleAction.Watch:UiTutorialBattleAction.Move,areaRadius:5);
                return true;
            }
            if(runtime.MissionId.Equals(AirliftId) && em.HasComponent<CampaignMissionExtractionState>(root))
                return ResolveExtractionTutorialTarget(em,root,guidance.GuidanceId-55000,out target);
            if(runtime.MissionId.Equals(BreachId) && em.HasComponent<CampaignMissionBreachState>(root) && em.Exists(guidance.SourceEntity) && em.HasComponent<LocalTransform>(guidance.SourceEntity))
            {
                var breach = em.GetComponentData<CampaignMissionBreachState>(root);
                bool recovering = guidance.GuidanceId == 65008 && breach.FriendlyAtArchive != 0;
                target=new UiMissionTutorialTarget(em.GetComponentData<LocalTransform>(guidance.SourceEntity).Position,guidance.WorldPosition,
                    !em.HasComponent<SelectedUnitTag>(guidance.SourceEntity),IsTutorialActorMoving(em,guidance.SourceEntity),
                    battleAction: recovering ? UiTutorialBattleAction.Watch : UiTutorialBattleAction.None,
                    executingAttack: IsTutorialAttackInProgress(em, guidance.SourceEntity, guidance.TargetEntity),
                    areaRadius: breach.ArchiveRadius);return true;
            }
            if(guidance.GuidanceId<45001 || guidance.GuidanceId>45012 || !em.Exists(guidance.SourceEntity) ||
                !em.HasComponent<LocalTransform>(guidance.SourceEntity)) return false;
            var actor=guidance.SourceEntity;
            bool moving = IsTutorialActorMoving(em, actor);
            if (guidance.GuidanceId == 45005)
                moving |= IsSelectedTutorialGroupMoving(em, runtime.SessionToken);
            target=new UiMissionTutorialTarget(em.GetComponentData<LocalTransform>(actor).Position,guidance.WorldPosition,
                !em.HasComponent<SelectedUnitTag>(actor),moving,1,
                guidance.GuidanceId<45010 ? UiTutorialBattleAction.None :
                guidance.RecommendationKind==AssistantRecommendationKind.Move ? UiTutorialBattleAction.Move :
                guidance.RecommendationKind==AssistantRecommendationKind.DefensiveAlert ? UiTutorialBattleAction.Hold : UiTutorialBattleAction.Watch, selectionLabelKey:"ui.aria.select_defenders");
            return true;
        }

        private static bool TryResolveEarlyMissionTutorialTarget(EntityManager em, Entity root,
            CampaignMissionRuntimeComponent runtime, CampaignMissionGuidanceProjectionComponent guidance,
            out UiMissionTutorialTarget target)
        {
            target = default;
            if (!runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes("saga.ch01.m01.first_contact")) &&
                !runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes("saga.ch01.m02.establish_base"))) return false;
            Entity actor = guidance.SourceEntity, hostile = guidance.TargetEntity;
            using var query = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(Faction), typeof(LocalTransform));
            using var units = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var unit in units)
            {
                if (!em.GetComponentData<CampaignMissionUnitRoleComponent>(unit).SessionToken.Equals(runtime.SessionToken) ||
                    em.HasComponent<UnitHealth>(unit) && em.GetComponentData<UnitHealth>(unit).Current <= 0) continue;
                if (FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(unit).Id))
                { if (!em.Exists(actor)) actor = unit; }
                else if (!em.Exists(hostile)) hostile = unit;
            }
            if (!em.Exists(actor) || !em.HasComponent<LocalTransform>(actor)) return false;
            float3 position = em.GetComponentData<LocalTransform>(actor).Position;
            float3 destination = guidance.HasWorldPosition != 0 ? guidance.WorldPosition : position;
            if (guidance.RecommendationKind is AssistantRecommendationKind.Attack or AssistantRecommendationKind.DefensiveAlert &&
                em.Exists(hostile) && em.HasComponent<LocalTransform>(hostile))
                destination = em.GetComponentData<LocalTransform>(hostile).Position;
            if (guidance.RecommendationKind == AssistantRecommendationKind.DefensiveAlert && !em.Exists(hostile))
            {
                using var maps = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                if (maps.CalculateEntityCount() != 1) return false;
                var map = maps.GetSingleton<OperationMapMetadataComponent>();
                if (!map.Blob.IsCreated || !TryFindExtractionAnchor(
                    ref map.Blob.Value, guidance.TargetId, out var anchor)) return false;
                destination = anchor.Position;
            }
            target = new UiMissionTutorialTarget(position, destination,
                !em.HasComponent<SelectedUnitTag>(actor), IsTutorialActorMoving(em, actor) ||
                IsSelectedTutorialGroupMoving(em, runtime.SessionToken),
                executingAttack: IsTutorialAttackInProgress(em, actor, hostile));
            return true;
        }

        internal static bool ResolveExtractionTutorialTarget(EntityManager em,Entity root,int step,out UiMissionTutorialTarget target)
        {
            target=default;
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            Entity actor=step is 8 or 9 or 10 or 11 or 12 ? extraction.Aircraft : extraction.Carrier;
            float3 team=default,missing=default; int count=0,selected=0; bool moving=false;
            var members=em.GetBuffer<CampaignMissionExtractionMember>(root,true);
            foreach(var member in members)
            {
                if(member.Kind!=1 || !TryGetLiveExtractionPosition(em,member.Entity,out var position)) continue;
                if(em.HasComponent<UnitTransportPassenger>(member.Entity) &&
                    TryGetLiveExtractionPosition(em,em.GetComponentData<UnitTransportPassenger>(member.Entity).Transport,out var aboard)) position=aboard;
                team+=position; count++;
                if(IsExtractionSelectionSatisfied(em,member.Entity,actor,step)) selected++; else missing=position;
                moving|=IsTutorialActorMoving(em,member.Entity);
            }
            if(count==0) return false;
            team/=count;
            bool teamStep=step is 4 or 5 or 9;
            if(!TryGetLiveExtractionPosition(em,actor,out var actorPosition)) return false;
            float3 destination=step is 6 or 7 or 10 ? extraction.LandingCenter : step>=11 ? extraction.DepartureCenter : team;
            if(step==3) destination=team+new float3(12,0,0); // Stop beside the pickup group, not on top of it.
            if(step==5) destination=actorPosition;
            if(step==9 && !TryGetLiveExtractionPosition(em,extraction.Aircraft,out destination)) return false;
            target=new UiMissionTutorialTarget(teamStep ? (selected>0 && selected<count ? missing : team) : actorPosition,destination,
                teamStep ? selected!=count : !em.HasComponent<SelectedUnitTag>(actor),
                teamStep ? moving : IsTutorialActorMoving(em,actor),teamStep?count:1,
                selectionLabelKey:teamStep?"ui.aria.select_specialists":step>=8?"ui.aria.select_helicopter":"ui.aria.select_carrier");
            return true;
        }

        private static int ReadExtractionSelectionCount(byte step)
        {
            if(step is not (4 or 5 or 9) || !TryGetMissionRoot(out var em,out var root) ||
                !em.GetComponentData<CampaignMissionRuntimeComponent>(root).MissionId.Equals(AirliftId) ||
                !em.HasBuffer<CampaignMissionExtractionMember>(root)) return -1;
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            Entity actor=step==9 ? extraction.Aircraft : extraction.Carrier;
            int count=0;
            foreach(var member in em.GetBuffer<CampaignMissionExtractionMember>(root,true))
                if(member.Kind==1 && IsExtractionSelectionSatisfied(em,member.Entity,actor,step)) count++;
            return count;
        }
        private static bool IsExtractionSelectionSatisfied(EntityManager em,Entity person,Entity actor,int step) =>
            em.HasComponent<SelectedUnitTag>(person) || (step is 5 or 9 &&
            em.HasComponent<UnitTransportPassenger>(person) && em.GetComponentData<UnitTransportPassenger>(person).Transport==actor);
        private static int cachedExtractionSelectionCount=-1;

        internal static bool IsSelectedTutorialGroupMoving(EntityManager em, Unity.Collections.FixedString64Bytes session)
        {
            // A formation's first soldier can arrive while the rest are still moving.
            // Keep the arrival instruction until the selected mission group has stopped.
            using var query = em.CreateEntityQuery(typeof(SelectedUnitTag), typeof(CampaignMissionUnitRoleComponent),
                typeof(Faction), typeof(UnitHealth));
            using var units = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var unit in units)
                if (em.GetComponentData<CampaignMissionUnitRoleComponent>(unit).SessionToken.Equals(session) &&
                    FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(unit).Id) &&
                    em.GetComponentData<UnitHealth>(unit).Current > 0 && IsTutorialActorMoving(em, unit)) return true;
            return false;
        }

        internal static bool IsTutorialAttackInProgress(EntityManager em, Entity actor, Entity target)
        {
            if (!em.Exists(actor) || !em.Exists(target) || !em.HasComponent<EngageTarget>(actor) ||
                em.HasComponent<UnitHealth>(target) && em.GetComponentData<UnitHealth>(target).Current <= 0) return false;
            var order = em.GetComponentData<EngageTarget>(actor);
            return order.IsCommanded != 0 && order.Target == target;
        }

        private static bool IsTutorialActorMoving(EntityManager em,Entity actor) =>
            em.HasComponent<UnitPathRequest>(actor) || em.HasComponent<UnitPathFollow>(actor);
    }
}
