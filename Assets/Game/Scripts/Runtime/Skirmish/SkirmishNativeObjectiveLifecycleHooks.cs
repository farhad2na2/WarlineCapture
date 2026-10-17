using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // One explicit attempt owner. Startup attaches it only after exact compiled
    // typed areas and actual native original-role annotations are available.
    public sealed class SkirmishNativeObjectiveRecord : IComponentData
    {
        public SkirmishNativeObjectiveCollector Collector;
        public SkirmishObjectiveCollectorClock Clock;
        public SkirmishCompiledObjectiveAreas Areas;
        public string MissionViewProfileFingerprint;
        public SkirmishFrontlineState Frontline;
        public SkirmishBreakthroughState Breakthrough;
        public SkirmishConvoyState Convoy;
    }

    public static class SkirmishNativeObjectiveLifecycleHooks
    {
        // Caller obtains Areas from the selected definition/source/layout compiler,
        // never from scene-name guesses. The view identity must be a per-mission
        // reviewed logical profile digest; S005 engineering scalars are not defaults.
        public static bool TryAttachAfterActualSpawn(EntityManager em,Entity session,SkirmishCompiledObjectiveAreas areas,
            string expectedLogicalFingerprint,string missionViewProfileFingerprint,out string error)
        {
            error="Actual compiled source/areas/per-mission view profile required.";
            if(em.HasComponent<SkirmishNativeObjectiveRecord>(session)||!Hex(missionViewProfileFingerprint))return false;
            if(!SkirmishNativeObjectiveCollector.TrySeal(em,session,areas,expectedLogicalFingerprint,out var collector,out error))return false;
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(state.SizeId!=SkirmishSizeId.Standard&&state.SizeId!=SkirmishSizeId.War&&state.SizeId!=SkirmishSizeId.LargeWar)
            {error="Canonical objective size missing.";return false;}
            var record=new SkirmishNativeObjectiveRecord{Collector=collector,Areas=areas,MissionViewProfileFingerprint=missionViewProfileFingerprint};
            string Id(SkirmishObjectiveAreaRole role)
            {if(!areas.TryRead(role,out var area))throw new System.ArgumentException("Required typed objective area missing.");return area.StableAreaId;}
            if(state.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl)
                record.Frontline=SkirmishFrontlineReducer.Create(state.SizeId,Id(SkirmishObjectiveAreaRole.FrontlineA),Id(SkirmishObjectiveAreaRole.FrontlineB),Id(SkirmishObjectiveAreaRole.FrontlineC));
            else if(state.ObjectiveKind==SkirmishObjectiveKind.Breakthrough)
                record.Breakthrough=SkirmishBreakthroughReducer.Create(state.SizeId,Id(SkirmishObjectiveAreaRole.BreakthroughCorridorA),Id(SkirmishObjectiveAreaRole.BreakthroughCorridorB),collector.OriginalStableIds());
            else if(state.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort)
                record.Convoy=SkirmishConvoyReducer.Create(state.SizeId,Id(SkirmishObjectiveAreaRole.ConvoyDestination),collector.OriginalStableIds());
            else return false;
            em.AddComponentObject(session,record);error=null;return true;
        }

        // Concrete call seams: UnitDeathSystem.FinalizeDeath before entity-tree
        // destruction, and BuildingCombatUtilitySystemHelper.DestroyEntity before
        // deleting a combat entity. Ordinary Campaign/non-original actors are no-ops.
        public static bool TryBeforeDestroyedActorRemoved(EntityManager em,Entity actor,out string error)
        {
            error=null;
            if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor))return true;
            var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            using var query=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            using var sessions=query.ToEntityArray(Allocator.Temp);Entity found=Entity.Null;
            foreach(var session in sessions)
            {
                var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                if(state.IsLegacy!=0||state.SessionId!=owned.SessionId||state.Phase!=SkirmishSessionPhase.Playing||state.ObjectiveKind==SkirmishObjectiveKind.BaseAssault)continue;
                if(found!=Entity.Null){error="Duplicate native objective session.";return false;}found=session;
            }
            if(found==Entity.Null)return true;
            if(!em.HasComponent<SkirmishNativeObjectiveRecord>(found))
            {error="Native objective owner missing before actor removal.";FailClosed(em,found);return false;}
            var record=em.GetComponentObject<SkirmishNativeObjectiveRecord>(found);
            if(record?.Collector==null){error="Native objective collector missing.";FailClosed(em,found);return false;}
            if(!record.Collector.IsOriginalEntity(actor))return true;
            if(record.Collector.TryRecordDestroyedBeforeRemoval(em,found,actor,out error))return true;
            FailClosed(em,found);return false;
        }
        private static void FailClosed(EntityManager em,Entity session)
        {
            if(!em.HasComponent<SkirmishObjectiveStateComponent>(session))return;
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            objective.Terminal=1;objective.Outcome=SkirmishOutcomeKind.TechnicalFailure;
            objective.Reason=SkirmishEndReasonKind.ContentFailure;objective.State=SkirmishObjectiveStateKind.TechnicalFailure;
            em.SetComponentData(session,objective);
        }
        private static bool Hex(string value)
        {if(value==null||value.Length!=64)return false;foreach(char c in value)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))return false;return true;}
    }
}
