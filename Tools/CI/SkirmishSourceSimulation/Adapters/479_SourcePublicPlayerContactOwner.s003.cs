using System;using System.Collections.Generic;using Game.Components;using Game.Skirmish.Contracts;using Game.Simulation;using Unity.Collections;using Unity.Entities;using Unity.Transforms;
namespace Game.Runtime {
// The ordinary source PLAYER marker pass only. No hostile transform is queried.
// Logical scope is all source player markers; native rendered map clipping stays native.
internal sealed class SourcePublicPlayerContactOwner {
 readonly EntityManager em;readonly Entity session;readonly GroundMissionContext legacyContext;readonly bool legacy;readonly bool s003Context;readonly bool expandedAirContext;readonly List<Row> rows=new List<Row>();double nextRefresh,lastUpdate=double.NegativeInfinity;
 static int nextIdentity;readonly int identity;uint generation;int markerFrame,lastOwnerFrame;float markerAriaTime,lastOwnerAriaTime;double markerWorldTime;
 internal readonly struct Snapshot{internal readonly int Identity,Frame;internal readonly uint Generation;internal readonly float AriaTime;internal readonly double WorldTime;internal readonly AriaGroundPosition[] Players;
 internal Snapshot(int identity,int frame,uint generation,float time,double worldTime,AriaGroundPosition[] players){Identity=identity;Frame=frame;Generation=generation;AriaTime=time;WorldTime=worldTime;Players=players;}}
 void Publish(int frame,float ariaTime){generation=checked(generation+1);markerFrame=frame;markerAriaTime=ariaTime;markerWorldTime=em.WorldUnmanaged.Time.ElapsedTime;}

 readonly struct Row{internal readonly Entity Source;internal readonly AriaGroundPosition Position;internal Row(Entity e,AriaGroundPosition p){Source=e;Position=p;}}
 internal SourcePublicPlayerContactOwner(EntityManager em,Entity session,bool s003Context=false){this.s003Context=s003Context;this.em=em??throw new ArgumentNullException(nameof(em));this.session=session;identity=System.Threading.Interlocked.Increment(ref nextIdentity);if(identity<=0)throw new InvalidOperationException("Logical data observation identity exhausted");}
 internal SourcePublicPlayerContactOwner(EntityManager em,Entity session,in GroundMissionContext legacyContext):this(em,session){if(legacyContext.ValidatedExpandedAirBaseAssault){SourceGroundMissionFamily.RequireLogicalSourceSession(em,session,legacyContext);this.legacyContext=legacyContext;expandedAirContext=true;}else{if(!legacyContext.LegacySourceBound||legacyContext.CatalogId!="S073"||legacyContext.ObservationScope!=AriaS002Scope.LogicalPublicData)throw new NotSupportedException("Exact original S073 legacy public context required");this.legacyContext=legacyContext;legacy=true;}}
 void Guard(){if(legacy){if(!em.Exists(session)||!em.HasComponent<SkirmishMatchState>(session)||em.HasComponent<SkirmishExpandedSessionComponent>(session)||em.HasComponent<SkirmishResolvedSetupRecord>(session))throw new NotSupportedException("Same original non-expanded S073 session required");var match=em.GetComponentData<SkirmishMatchState>(session);if(match.SessionId.ToString()!=legacyContext.SessionId||match.ScenarioIndex!=3||match.Phase!=SkirmishPhase.Playing||match.StartupFailure!=SkirmishStartupFailureCode.None||match.Outcome!=SkirmishOutcome.None||match.Reason!=SkirmishEndReason.None)throw new NotSupportedException("Active original S073 legacy match identity changed");legacyContext.RequireCompatibleObservation(new AriaS002Observation{Scope=AriaS002Scope.LogicalPublicData,SourceScenarioIndex=3});}else if(expandedAirContext)SourceGroundMissionFamily.RequireLogicalSourceSession(em,session,legacyContext);else SourceGroundMissionFamily.RequireSession(em,session,s003Context);using var campaign=em.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));if(!campaign.IsEmptyIgnoreFilter)throw new NotSupportedException("Campaign/defense marker roster not admitted");}
 internal void UpdateAfterFog(int frame,float ariaTime){Guard();
  if(frame<=0||frame<lastOwnerFrame||!float.IsFinite(ariaTime)||ariaTime<lastOwnerAriaTime)throw new InvalidOperationException("Actual ordered frame/ARIA clock required");
  lastOwnerFrame=frame;lastOwnerAriaTime=ariaTime;bool changed=false;double now=em.WorldUnmanaged.Time.ElapsedTime;if(now<lastUpdate)throw new InvalidOperationException("Source public marker clock moved backward");lastUpdate=now;
  if(now<nextRefresh){for(int i=rows.Count-1;i>=0;i--){var source=rows[i].Source;
   if(source!=Entity.Null&&em.HasComponent<SkirmishAttemptOwnedComponent>(source)&&!SkirmishPublicObjectiveProjection.PlayerVisible(em,source)){
    if(SkirmishFogService.TryGetKnowledge(em,source,1,out var record)&&record.Sight==SkirmishContactSight.LastSeen&&record.LastSeenHealth>0){var p=record.LastSeenPosition;rows[i]=new Row(Entity.Null,new AriaGroundPosition(p.x,p.y,p.z));changed=true;}else {rows.RemoveAt(i);changed=true;}continue;
   }
   if(source!=Entity.Null&&(!em.HasComponent<UnitHealth>(source)||em.GetComponentData<UnitHealth>(source).Current<=0)){rows.RemoveAt(i);changed=true;}
  }if(changed)Publish(frame,ariaTime);return;}
  nextRefresh=now+AriaPublicPlayerMarkerPolicy.RefreshIntervalSeconds;rows.Clear();
  using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<LocalTransform>(),ComponentType.ReadOnly<Faction>()},Options=EntityQueryOptions.IncludeDisabledEntities});using var entities=query.ToEntityArray(Allocator.Temp);
  foreach(var e in entities){bool owner=em.HasComponent<SupportPassengerOwnerComponent>(e);if(!AriaPublicPlayerMarkerPolicy.Include(rows.Count,AriaPublicPlayerMarkerPolicy.MaxMarkers,em.GetComponentData<Faction>(e).Id,em.HasComponent<SkirmishAttemptOwnedComponent>(e),em.HasComponent<Disabled>(e),em.HasComponent<UnitTransportPassenger>(e),owner,owner&&em.GetComponentData<SupportPassengerOwnerComponent>(e).Landed!=0,em.GetComponentData<UnitHealth>(e).Current))continue;
   var p=em.GetComponentData<LocalTransform>(e).Position;rows.Add(new Row(e,new AriaGroundPosition(p.x,p.y,p.z)));
  }
  Publish(frame,ariaTime);
 }
 internal Snapshot ReadSnapshot(){var players=ReadCurrent();if(generation==0)throw new InvalidOperationException("Actual source marker projection has not completed");return new Snapshot(identity,markerFrame,generation,markerAriaTime,markerWorldTime,players);}
 internal AriaGroundPosition[] ReadCurrent(){if(lastUpdate!=em.WorldUnmanaged.Time.ElapsedTime)throw new InvalidOperationException("Source marker owner must run after Fog at this source clock before ARIA reads");var result=new AriaGroundPosition[rows.Count];for(int i=0;i<rows.Count;i++)result[i]=rows[i].Position;return result;}
}
}
