using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class SkirmishEnemyConstructionCheckpoint
    {
        internal static bool HasAny(SkirmishNativeCheckpointParts.Part[] parts)
        {
            if(parts!=null)foreach(var p in parts)if(p?.Type==typeof(SkirmishEnemyConstructionIntentV1Component).FullName)return true;
            return false;
        }
        internal static bool Validate(SkirmishNativeCheckpointSnapshotService.Snapshot saved,SkirmishResolvedSetup setup)
        {
            // Optional additive part; older snapshots without it have no outstanding construction.
            foreach(var a in saved.Actors)if(HasAny(a.Parts))return false;
            foreach(var m in saved.MapReferences)if(HasAny(m.Parts))return false;
            foreach(var b in saved.Banks)if(HasAny(b.Parts))return false;
            if(!HasAny(saved.SessionParts))return true;
            if(setup?.EnemyPolicy==null || setup.EnemyFaction!=2 || setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishEnemyConstructionIntentV1Component i) ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishObjectiveClockComponent clock) ||
                i.Version!=1 || i.SessionId.ToString()!=saved.SessionId || i.RejectionCount>3 ||
                !float.IsFinite(i.RequestedAt) || i.RequestedAt<0 || i.RequestedAt>clock.ElapsedSeconds ||
                !float.IsFinite(i.RetryAfter) || i.RetryAfter<0 || i.PlanRevision>ReadRevision(saved.SessionParts) ||
                (i.FailedResearchId==0)!=i.PreviousProducerStableId.IsEmpty)return false;
            if(i.Phase==(byte)SkirmishEnemyConstructionPhase.Idle)
            {
                if(!i.ActionKey.IsEmpty || !i.BuildingId.IsEmpty || i.RequestId!=0 || i.MaterialsPaid!=0 || !i.ResultStableObjectId.IsEmpty)return false;
            }
            else
            {
                if(i.Phase!=(byte)SkirmishEnemyConstructionPhase.PaidPending && i.Phase!=(byte)SkirmishEnemyConstructionPhase.Delivered &&
                    i.Phase!=(byte)SkirmishEnemyConstructionPhase.RefundPending && i.Phase!=(byte)SkirmishEnemyConstructionPhase.Rejected)return false;
                if(i.RequestId<=0 || i.DesiredCount<1 || i.DesiredCount>2 || i.CandidateIndex<0 ||
                    i.CandidateIndex>=setup.EnemyPolicy.ConstructionPads.Count*9 || setup.EnemyPolicy.ConstructionPads[i.CandidateIndex/9].Id!=i.PadId.ToString() || math.any(i.Footprint<=0) || math.any(i.Origin<0) ||
                    !SkirmishEnemyConstructionAdapter.SavedIntentFits(setup,i))return false;
                if(i.Phase==(byte)SkirmishEnemyConstructionPhase.Rejected)
                {if(i.MaterialsPaid!=0 || !i.ResultStableObjectId.IsEmpty)return false;}
                else if(i.MaterialsPaid<=0)return false;
                if((i.Phase==(byte)SkirmishEnemyConstructionPhase.Delivered)==i.ResultStableObjectId.IsEmpty)return false;
            }
            foreach(var a in saved.Actors)if(HasAny(a.Parts))return false;
            foreach(var m in saved.MapReferences)if(HasAny(m.Parts))return false;
            foreach(var b in saved.Banks)if(HasAny(b.Parts))return false;
            return true;
        }
        static uint ReadRevision(SkirmishNativeCheckpointParts.Part[] parts) =>
            SkirmishNativeCheckpointParts.TryReadComponent(parts,out SkirmishEnemyPlannerV1Component p)?p.PlanRevision:0;
        // Pure saved-copy normalization; capture never refunds or alters paused native state.
        internal static SkirmishEnemyConstructionIntentV1Component NormalizeCapture(EntityManager em,
            SkirmishEnemyConstructionIntentV1Component i,Func<Entity,string> identity)
        {
            if(i.Phase!=(byte)SkirmishEnemyConstructionPhase.PaidPending)return i;
            using var q=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest));
            if(q.CalculateEntityCount()!=1)return i;
            int matches=0;
            foreach(var r in em.GetBuffer<BuildingRuntimeSpawnRequest>(q.GetSingletonEntity()))
            {
                if(r.RequestKind!=BuildingRuntimeSpawnRequest.KindSkirmishPaidConstruction || r.RequestId!=i.RequestId ||
                    !SkirmishEnemyConstructionAdapter.BoundaryKeyEquals(r.BuildingId,i.BuildingId) || math.any(r.PreferredOrigin!=i.Origin) || r.PlanEntity==Entity.Null ||
                    !em.Exists(r.PlanEntity) || !em.HasComponent<SkirmishExpandedSessionComponent>(r.PlanEntity) ||
                    !em.GetComponentData<SkirmishExpandedSessionComponent>(r.PlanEntity).SessionId.Equals(i.SessionId))continue;
                matches++;
                if(r.Status==BuildingRuntimeSpawnRequest.Failed)i.Phase=(byte)SkirmishEnemyConstructionPhase.RefundPending;
                if(r.Status==BuildingRuntimeSpawnRequest.Succeeded)
                    throw new InvalidOperationException("Paid construction success lacks atomic adopted checkpoint identity.");
            }
            if(matches>1)throw new InvalidOperationException("Duplicate paid construction checkpoint request.");
            return i;
        }
    }
}
