using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    // Large planner values have their own archetype. Touch/output authority stays
    // on the original UI root used by the normal decision/input systems.
    public static class AriaSkirmishObservationStorage
    {
        public static int Sequence(EntityManager em,Entity boundary)=>em.HasComponent<UiShellStateComponent>(boundary)?em.GetComponentData<UiShellStateComponent>(boundary).TransitionSequenceId:0;
        static bool Find(EntityManager em,Entity boundary,out Entity storage)
        {
            storage=Entity.Null;
            if(!em.Exists(boundary)||!em.HasComponent<UiShellRootComponent>(boundary))return false;
            using var query=em.CreateEntityQuery(typeof(AriaSkirmishObservationOwner));
            using var rows=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in rows)
            {
                if(em.GetComponentData<AriaSkirmishObservationOwner>(entity).Boundary!=boundary)continue;
                if(storage!=Entity.Null)throw new InvalidOperationException("Duplicate ARIA storage for one UI root");
                storage=entity;
            }
            return storage!=Entity.Null;
        }
        public static bool TryGet(EntityManager em,Entity boundary,out Entity storage)
        {
            if(!Find(em,boundary,out storage))return false;
            if(!em.HasComponent<AriaSkirmishObservationComponent>(storage)||!em.HasComponent<AriaSkirmishPlanComponent>(storage))return false;
            return em.HasComponent<UiShellStateComponent>(boundary) &&
                em.GetComponentData<UiShellStateComponent>(boundary).ActiveRoute==UIRoute.Match &&
                em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value.Active &&
                em.GetComponentData<AriaSkirmishObservationOwner>(storage).TransitionSequence==Sequence(em,boundary);
        }
        public static void Publish(EntityManager em,Entity boundary,in AriaSkirmishObservation value)
        {
            if(!em.Exists(boundary)||!em.HasComponent<UiShellRootComponent>(boundary))throw new InvalidOperationException("Actual UI root required");
            using(var query=em.CreateEntityQuery(typeof(AriaSkirmishObservationOwner)))
            using(var rows=query.ToEntityArray(Allocator.Temp))
                foreach(var entity in rows)
                {
                    var root=em.GetComponentData<AriaSkirmishObservationOwner>(entity).Boundary;
                    if(!em.Exists(root)||!em.HasComponent<UiShellRootComponent>(root))em.DestroyEntity(entity);
                }
            if(!Find(em,boundary,out var storage))
            {
                storage=em.CreateEntity(typeof(AriaSkirmishObservationOwner),typeof(AriaSkirmishObservationComponent),typeof(AriaSkirmishPlanComponent));
                em.SetName(storage,"AriaSkirmishUiOwnedStorage");
                em.SetComponentData(storage,new AriaSkirmishObservationOwner{Boundary=boundary,TransitionSequence=Sequence(em,boundary)});
            }
            var owner=em.GetComponentData<AriaSkirmishObservationOwner>(storage);
            var prior=em.GetComponentData<AriaSkirmishObservationComponent>(storage).Value;
            bool scopeChanged=owner.TransitionSequence!=Sequence(em,boundary)||value.Frame<prior.Frame||value.Time<prior.Time;
            if(prior.Active&&(!value.Active||scopeChanged))
            {
                if(em.HasComponent<AriaPlaySessionComponent>(boundary))em.SetComponentData(boundary,default(AriaPlaySessionComponent));
                if(em.HasComponent<AriaPlayObservationComponent>(boundary))em.SetComponentData(boundary,default(AriaPlayObservationComponent));
            }
            if(!value.Active||scopeChanged)
                em.SetComponentData(storage,default(AriaSkirmishPlanComponent));
            owner.TransitionSequence=Sequence(em,boundary);em.SetComponentData(storage,owner);
            em.SetComponentData(storage,new AriaSkirmishObservationComponent{Value=value});
        }
        public static void ResetPlan(EntityManager em,Entity boundary,bool clearObservation)
        {
            if(!Find(em,boundary,out var storage))return;
            var owner=em.GetComponentData<AriaSkirmishObservationOwner>(storage);
            if(clearObservation||owner.TransitionSequence!=Sequence(em,boundary))em.SetComponentData(storage,default(AriaSkirmishObservationComponent));
            owner.TransitionSequence=Sequence(em,boundary);em.SetComponentData(storage,owner);
            em.SetComponentData(storage,default(AriaSkirmishPlanComponent));
        }
    }
}
