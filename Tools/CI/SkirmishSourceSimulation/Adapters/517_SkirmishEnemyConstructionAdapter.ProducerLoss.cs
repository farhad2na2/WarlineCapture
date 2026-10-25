using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;using Unity.Entities;
namespace Game.Runtime{internal static partial class SkirmishEnemyConstructionAdapter{
internal static void RecordProducerLoss(EntityManager em,Entity session,Entity structure)
        {
            if(!TryActive(em,session,out _,out _) || !em.Exists(structure) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(structure) || !em.HasComponent<UnitHealth>(structure) ||
                !em.HasComponent<SkirmishStructureIdentityComponent>(structure) || em.GetComponentData<UnitHealth>(structure).Current>0 ||
                em.GetComponentData<SkirmishStructureIdentityComponent>(structure).Producer!=SkirmishProducerKind.Barracks ||
                !em.HasBuffer<SkirmishResearchQueueItem>(session))return;
            var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(structure);
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            if(owner.FactionId!=2 || owner.IsStructure!=1 || !owner.SessionId.Equals(attempt) || owner.StableObjectId.IsEmpty)return;
            SkirmishResearchQueueItem latest=default;
            foreach(var row in em.GetBuffer<SkirmishResearchQueueItem>(session))
                if(row.FactionId==2 && row.Kind==SkirmishResearchKind.Readiness && row.ResearchId>latest.ResearchId)latest=row;
            if(latest.ResearchId==0 || latest.Phase!=SkirmishResearchPhase.Lost)return;
            if(!em.HasComponent<SkirmishEnemyConstructionIntentV1Component>(session))
                em.AddComponentData(session,new SkirmishEnemyConstructionIntentV1Component{Version=1,SessionId=attempt});
            var i=em.GetComponentData<SkirmishEnemyConstructionIntentV1Component>(session);
            if(i.FailedResearchId==latest.ResearchId && !i.PreviousProducerStableId.IsEmpty)return;
            i.FailedResearchId=latest.ResearchId;i.PreviousProducerStableId=owner.StableObjectId;em.SetComponentData(session,i);
        }
static bool TryActive(EntityManager em,Entity session,out SkirmishResolvedSetup setup,out float now)
        {
            now=0;
            if(!TryPolicy(em,session,out setup) || !SkirmishEnemyPlannerService.TryReadActiveClock(em,session,out now))return false;
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            return state.SpawnComplete==1 && !state.SessionId.IsEmpty;
        }
static bool TryPolicy(EntityManager em,Entity session,out SkirmishResolvedSetup setup)
        {
            setup=null;
            if(em==default || !em.Exists(session) || !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishEnemyPlannerV1Component>(session))return false;
            setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(setup?.EnemyPolicy==null || setup.EnemyFaction!=2 || (setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault&&!SkirmishEnemyObjectivePolicyScope.Supports(setup)))return false;
            var p=em.GetComponentData<SkirmishEnemyPlannerV1Component>(session);
            return p.Initialized==1 && p.PolicyVersion==setup.EnemyPolicy.Version && p.PolicyHash==setup.EnemyPolicy.Hash &&
                p.PolicyFingerprint.ToString()==setup.EnemyPolicy.Fingerprint;
        }
}}
