using System;using Game.Components;using Game.Simulation;using Game.Skirmish.Contracts;using Unity.Entities;using Unity.Mathematics;
namespace Game.Runtime {
 internal readonly struct SourceAriaHomeThreat {internal readonly Entity Entity;internal readonly float3 PublicPosition;internal SourceAriaHomeThreat(Entity e,float3 p){Entity=e;PublicPosition=p;}}
 internal static class SourceAriaPublicHomeThreat {
  internal static SourceAriaHomeThreat? Read(EntityManager em,Entity session,PublicCommandCatalog.View publicCatalog){
   var s=em.GetComponentData<SkirmishExpandedSessionComponent>(session);if(s.CatalogId.ToString()!="S002"||s.IsLegacy!=0)throw new NotSupportedException("Only current source S002 public home threat scope");
   var match=em.GetComponentData<SkirmishMatchState>(session);var bases=SkirmishPublicObjectiveProjection.Read(em,match);if(!bases.PlayerPositionKnown)throw new NotSupportedException("Current known mission home position required");
   float closest=AriaPublicHomeThreatPolicy.RadiusSquared;SourceAriaHomeThreat? chosen=null;
   foreach(var target in publicCatalog.Targets){
    // Catalog entries already passed source alive/faction/current-fog visibility.
    // Native excludes its projected labelled base; logical profile excludes the
    // same designated objective identity, independently of pixel coincidence.
    if(target.Entity==match.EnemyMainBase)continue;
    if(!target.PublicKnowledge.HasValue||target.PublicKnowledge.Value.Sight!=SkirmishContactSight.Visible)throw new NotSupportedException("Current source public hostile knowledge required; no hidden transform fallback");
    var p=target.PublicKnowledge.Value.LastSeenPosition;if(!math.all(math.isfinite(p)))throw new NotSupportedException("Nonfinite source public contact");
    if(AriaPublicHomeThreatPolicy.Consider(new AriaGroundPosition(p.x,p.y,p.z),bases.PlayerPosition,ref closest))chosen=new SourceAriaHomeThreat(target.Entity,p);
   }
   return chosen;
  }
 }
}
