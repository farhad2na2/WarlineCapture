using System;using Game.Components;using Unity.Entities;using Unity.Collections;namespace Game.Runtime{
// Exact original diagnostic no-provider/no-inherited-record branch. Never issues a native birth certificate.
internal readonly struct SourceOpeningBirthContext{
 static void RequireLiveZeroProviders(EntityManager em){using var all=em.CreateEntityQuery(new EntityQueryDesc{Options=EntityQueryOptions.IncludePrefab|EntityQueryOptions.IncludeDisabledEntities});using var entities=all.ToEntityArray(Allocator.Temp);foreach(var entity in entities)if(em.HasComponentNamed(entity,"Game.Runtime.RuntimeActorBirthSourceProvider"))throw new NotSupportedException("Native birth source provider appeared outside qualified zero-provider domain");}
 internal static SourceOpeningBirthContext CaptureScenario(EntityManager em,Entity actualGrid,FixedString64Bytes sessionId,FixedString64Bytes stableId,GridConfig layout){
 Entity session=Entity.Null;using var query=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));using var entities=query.ToEntityArray(Allocator.Temp);foreach(var entity in entities)if(em.GetComponentData<SkirmishExpandedSessionComponent>(entity).SessionId.Equals(sessionId)){if(session!=Entity.Null)return default;session=entity;}
 if(stableId.IsEmpty||em==null||session==Entity.Null||!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||!em.Exists(actualGrid)||!em.HasComponent<GridConfig>(actualGrid))return default;var id=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;if(id.IsEmpty)return default;
 var actual=em.GetComponentData<GridConfig>(actualGrid);if(actual.Width!=layout.Width||actual.Height!=layout.Height||actual.CellSize!=layout.CellSize||!Unity.Mathematics.math.all(actual.Origin==layout.Origin))throw new NotSupportedException("Original birth creator selected grid/layout differs");RequireLiveZeroProviders(em);return default;
 }
 internal void StampFresh(EntityManager em,Entity actor,Entity actualGrid){if(!em.Exists(actor))return;RequireLiveZeroProviders(em);RuntimeActorBirthContext.RequireNoRecordProfile(em,actor);}
}}
