using System;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    // Separate ECS fixture: source session values are copied, never changed in the mission world.
    internal static class SourceS004PublicThreatControls
    {
        internal static void Run(EntityManager source, Entity session)
        {
            var fixture=new EntityManager();
            var cloned=new Entity { Index=1, Version=1 };
            fixture.ImportEntity(cloned);
            fixture.AddComponentData(cloned,source.GetComponentObject<SkirmishResolvedSetupRecord>(session));
            fixture.AddComponentData(cloned,source.GetComponentObject<SkirmishLaunchDefinitionRecord>(session));
            var expanded=source.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var match=source.GetComponentData<SkirmishMatchState>(session);
            var clock=source.GetComponentData<SkirmishObjectiveClockComponent>(session);
            fixture.AddComponentData(cloned,expanded);
            fixture.AddComponentData(cloned,match);
            fixture.AddComponentData(cloned,clock);

            void Rejected(string label)
            {
                try { SourceAriaPublicHomeThreat.ReadS004(fixture,cloned); }
                catch (NotSupportedException e) when (e.Message=="Active exact S004 identity required") { return; }
                throw new InvalidOperationException("Threat source guard did not reject " + label);
            }

            var wrong=match;wrong.ScenarioIndex=0;fixture.SetComponentData(cloned,wrong);
            Rejected("wrong scenario");fixture.SetComponentData(cloned,match);
            wrong=match;wrong.Phase=SkirmishPhase.Finished;fixture.SetComponentData(cloned,wrong);
            Rejected("finished match");fixture.SetComponentData(cloned,match);
            var paused=clock;paused.Paused=1;fixture.SetComponentData(cloned,paused);
            Rejected("paused clock");fixture.SetComponentData(cloned,clock);
            var stateEntity=new Entity { Index=2, Version=1 };fixture.ImportEntity(stateEntity);
            fixture.AddComponentData(stateEntity,new RuntimeGameplayStateComponent { SimulationActive=0 });
            Rejected("inactive gameplay");
            fixture.SetComponentData(stateEntity,new RuntimeGameplayStateComponent { SimulationActive=1 });

            // Synthetic public contacts exercise the real fog/knowledge projection;
            // none are copied into the mission world.
            var home=new Entity { Index=3,Version=1 };fixture.ImportEntity(home);
            fixture.AddComponentData(home,LocalTransform.FromPosition(new float3(0,0,0)));
            match.PlayerMainBase=home;match.EnemyMainBase=Entity.Null;fixture.SetComponentData(cloned,match);
            var hostile=new Entity { Index=4,Version=1 };fixture.ImportEntity(hostile);
            fixture.AddComponentData(hostile,new SkirmishAttemptOwnedComponent { SessionId=expanded.SessionId,FactionId=2 });
            var knowledge=fixture.AddBuffer<SkirmishContactKnowledgeElement>(hostile);
            knowledge.Add(new SkirmishContactKnowledgeElement { ObserverFactionId=1,Sight=SkirmishContactSight.Visible,LastSeenPosition=new float3(5,0,5) });
            var visible=SourceAriaPublicHomeThreat.ReadS004(fixture,cloned);
            if (!visible.HasValue || visible.Value.Entity!=hostile ||
                SourceS004SupplySegment.EarlierPriority(visible.HasValue,true,20,2,0)!=SourceS004SupplySegment.PriorityBlock.PublicHomeThreat)
                throw new InvalidOperationException("Visible nearby hostile did not block supply");
            var sample=knowledge[0];sample.LastSeenPosition=new float3(200,0,0);knowledge[0]=sample;
            if (SourceAriaPublicHomeThreat.ReadS004(fixture,cloned).HasValue)
                throw new InvalidOperationException("Distant hostile counted as home threat");
            sample.Sight=SkirmishContactSight.LastSeen;sample.LastSeenPosition=new float3(5,0,5);knowledge[0]=sample;
            if (SourceAriaPublicHomeThreat.ReadS004(fixture,cloned).HasValue)
                throw new InvalidOperationException("Unseen hostile counted as public threat");
            if (SourceS004SupplySegment.EarlierPriority(false,true,20,1,0)!=SourceS004SupplySegment.PriorityBlock.OpeningDefense ||
                SourceS004SupplySegment.EarlierPriority(false,true,20,2,0)!=SourceS004SupplySegment.PriorityBlock.None)
                throw new InvalidOperationException("Opening defense priority mismatch");
        }
    }
}
