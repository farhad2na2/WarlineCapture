using System;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

internal static class MarkerLifecycleFixture
{
    private static void Need(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static void Main()
    {
        int next = 1;
        var em = new EntityManager { LogicalAllocatorAdmitted = true,
            AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 } };
        var enemy = em.CreateEntity();
        var dead = em.CreateEntity();
        em.AddComponentData(enemy, new SkirmishAttemptOwnedComponent { FactionId = 2 });
        em.AddComponentData(enemy, new UnitHealth { Current = 5 });
        em.AddComponentData(dead, new UnitHealth { Current = 0 });
        var knowledge = em.AddBuffer<SkirmishContactKnowledgeElement>(enemy);
        knowledge.Add(new SkirmishContactKnowledgeElement { ObserverFactionId = 1,
            Sight = SkirmishContactSight.LastSeen, LastSeenHealth = 4,
            LastSeenPosition = new Unity.Mathematics.float3(4, 0, 6), LastObservedTimeSeconds = 7 });
        var markers = new DynamicBuffer<MatchHudMinimapMarkerElement>(new[] {
            new MatchHudMinimapMarkerElement { SourceEntity = enemy, FactionId = 2 },
            new MatchHudMinimapMarkerElement { SourceEntity = dead, FactionId = 1 }
        });
        var prune = new OriginalMarkerCollectorProbe.PruneDeadMarkersJob {
            Health = new ComponentLookup<UnitHealth>(em),
            FogOwned = new ComponentLookup<SkirmishAttemptOwnedComponent>(em),
            FogSight = new ComponentLookup<SkirmishContactSightComponent>(em),
            FogKnowledge = new BufferLookup<SkirmishContactKnowledgeElement>(em),
            Now = 10, Markers = markers
        };
        prune.Execute();
        Need(markers.Length == 1 && markers[0].SourceEntity == Entity.Null && markers[0].IsLastSeen == 1 &&
             markers[0].Position.x == 4 && markers[0].AgeSeconds == 3, "last-seen conversion and dead removal");
        var source = new NativeList<MatchHudMinimapMarkerElement>(4, Allocator.Temp);
        source.Add(markers[0]); source.Add(new MatchHudMinimapMarkerElement { FactionId = 1 });
        var publish = new OriginalMarkerCollectorProbe.PublishMarkersJob { MaxMarkers = 1, Source = source, Destination = markers };
        publish.Execute();
        Need(markers.Length == 1 && markers[0].IsLastSeen == 1, "published marker limit and order");
        source.Dispose();
        Console.WriteLine("[S003OriginalMarkerLifecycleFixture] result=Passed cases=3 fixtureOnly=1 scheduler=0 readiness=0");
    }
}
