using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public enum SkirmishOriginalObjectiveDisposition : byte { Live = 0, Destroyed = 1, Evacuated = 2, Delivered = 3 }

    [Serializable]
    public struct SkirmishObjectiveCollectorClock
    {
        // Persist with reducer state and original-ID tombstones. Never recompute
        // elapsed from Time.time, wall time or a fresh zero after checkpoint load.
        public double LastAuthoritativeSeconds, FractionalTicks;
    }

    [Serializable]
    public struct SkirmishOriginalObjectiveTombstone
    {
        public string StableOriginalId;
        public SkirmishOriginalObjectiveDisposition Disposition;
    }

    [Serializable]
    public struct SkirmishObjectiveCollectorOriginalRow
    {
        public string StableOriginalId, ObjectiveStableId;
        public SkirmishObjectiveRoleKind Role;
        public byte Faction, IsStructure;
        public SkirmishRoleKind UnitRole;
        public SkirmishPopulationCategory Category;
        public SkirmishOriginalObjectiveDisposition Disposition;
    }

    // This snapshot is an internal rules input, never ARIA/AI telemetry. Only a
    // separate visibility-filtered public objective presentation may be exposed.
    public sealed class SkirmishNativeObjectiveFacts
    {
        public SkirmishFrontlineTickFacts Frontline;
        public SkirmishBreakthroughTickFacts Breakthrough;
        public SkirmishConvoyTickFacts Convoy;
        public SkirmishObjectiveCollectorClock NextClock;
        public SkirmishOriginalObjectiveTombstone[] NextTombstones;
    }

    /// <summary>
    /// Opt-in read-only native collector; not an auto-created ECS system. Startup
    /// must explicitly designate native original actors before Seal(), after shared
    /// spawn completion. Missing originals, life, typed areas or clock fail closed.
    /// Does not step reducers, issue orders, remove actors, debit capacity, publish
    /// results, alter fog, feed ARIA, or invent native measurement evidence.
    /// </summary>
    public sealed class SkirmishNativeObjectiveCollector
    {
        private sealed class Original
        {
            public string Id;
            public Entity Entity;
            public SkirmishObjectiveRoleKind Role;
            public byte Faction;
            public byte IsStructure;
            public string ObjectiveStableId;
            public SkirmishRoleKind UnitRole;
            public SkirmishPopulationCategory Category;
            public SkirmishOriginalObjectiveDisposition Disposition;
        }
        private readonly FixedString64Bytes sessionId;
        private readonly SkirmishObjectiveKind kind;
        private readonly SkirmishCompiledObjectiveAreas areas;
        private readonly List<Original> originals;
        private readonly FixedString64Bytes definitionId;
        private readonly uint setupHash;
        private readonly int contentVersion;
        private SkirmishNativeObjectiveFacts pendingFacts;
        private SkirmishOriginalObjectiveTombstone[] pendingTombstones;

        private SkirmishNativeObjectiveCollector(in SkirmishExpandedSessionComponent state,
            SkirmishCompiledObjectiveAreas footprints, List<Original> sealedOriginals)
        { sessionId=state.SessionId; kind=state.ObjectiveKind; definitionId=state.DefinitionId;setupHash=state.SetupHash;
          contentVersion=state.ContentVersion;areas=footprints; originals=sealedOriginals; }

        // No automatic "first currently visible rifles" selection. Native spawn
        // ownership must already mark the exact twelve originals / three trucks.
        public static bool TrySeal(EntityManager em, Entity session, SkirmishCompiledObjectiveAreas footprints,
            string expectedLogicalFingerprint, out SkirmishNativeObjectiveCollector collector, out string error)
        {
            collector=null;
            if (!Session(em,session,out var state,out error) || state.SpawnComplete==0 ||
                footprints==null || footprints.ObjectiveKind!=state.ObjectiveKind ||
                string.IsNullOrWhiteSpace(expectedLogicalFingerprint) || footprints.LogicalFingerprint!=expectedLogicalFingerprint)
            { error=error??"Exact compiled area binding and completed native spawn required."; return false; }
            if(!em.HasComponent<SkirmishObjectiveClockComponent>(session)||
                em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds!=0)
            {error="Seal original identities at native startup before objective time advances; recovery needs explicit validated rebinding.";return false;}
            var required=state.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl ? 2 :
                state.ObjectiveKind==SkirmishObjectiveKind.Breakthrough ? 12 : state.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort ? 3 : 0;
            if(required==0){error="Unsupported collector objective.";return false;}
            if(!ReadOwned(em,state.SessionId,out var actors,out error))return false;
            var result=new List<Original>();
            bool playerBase=false,enemyBase=false;
            foreach(var pair in actors)
            {
                Entity entity=pair.Value;
                if(!em.HasComponent<SkirmishObjectiveRoleComponent>(entity))continue;
                var role=em.GetComponentData<SkirmishObjectiveRoleComponent>(entity);
                bool selected=state.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl ?
                    role.Role==SkirmishObjectiveRoleKind.PlayerBase||role.Role==SkirmishObjectiveRoleKind.EnemyBase :
                    role.Role==(state.ObjectiveKind==SkirmishObjectiveKind.Breakthrough ? SkirmishObjectiveRoleKind.BreakthroughDesignated : SkirmishObjectiveRoleKind.ConvoyTruck);
                if(!selected)continue;
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if(!em.HasComponent<SkirmishSharedActorTag>(entity)||!em.HasComponent<UnitHealth>(entity)||
                    em.GetComponentData<UnitHealth>(entity).Current<=0||owned.ReservationId!=0||owned.CapacityReleased!=0||role.FactionId!=owned.FactionId||
                    !em.HasComponent<Faction>(entity)||em.GetComponentData<Faction>(entity).Id!=owned.FactionId)
                {error="Original designation must bind a healthy original shared actor.";return false;}
                if(state.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl)
                {
                    bool player=role.Role==SkirmishObjectiveRoleKind.PlayerBase;
                    if(owned.IsStructure==0||!em.HasComponent<SkirmishStructureIdentityComponent>(entity)||
                        em.GetComponentData<SkirmishStructureIdentityComponent>(entity).DesignatedBase==0||
                        owned.FactionId!=(player?(byte)1:(byte)2)||(player?playerBase:enemyBase))
                    {error="Exactly one original designated base per faction required.";return false;}
                    if(player)playerBase=true;else enemyBase=true;
                }
                else
                {
                    if(owned.FactionId!=1||owned.IsStructure!=0||role.StableObjectId!=owned.StableObjectId||
                        !em.HasComponent<SkirmishUnitRoleComponent>(entity))
                    {error="Original objective role must bind its own stable native actor ID.";return false;}
                    var unit=em.GetComponentData<SkirmishUnitRoleComponent>(entity);
                    if(state.ObjectiveKind==SkirmishObjectiveKind.Breakthrough ?
                        unit.Role!=SkirmishRoleKind.Rifle||unit.Category!=SkirmishPopulationCategory.Infantry :
                        unit.Role!=SkirmishRoleKind.ConvoyLogic||unit.Category!=SkirmishPopulationCategory.ObjectiveSupport)
                    {error="Original role is not the required rifle/objective truck.";return false;}
                }
                if(role.StableObjectId.IsEmpty){error="Original objective-role identity missing.";return false;}
                var unitRole=em.HasComponent<SkirmishUnitRoleComponent>(entity)?em.GetComponentData<SkirmishUnitRoleComponent>(entity):default;
                result.Add(new Original{Id=pair.Key,Entity=entity,Role=role.Role,Faction=owned.FactionId,
                    IsStructure=owned.IsStructure,ObjectiveStableId=role.StableObjectId.ToString(),UnitRole=unitRole.Role,Category=unitRole.Category});
            }
            if(result.Count!=required){error="Exact original designated actor count missing.";return false;}
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Id,b.Id));
            collector=new SkirmishNativeObjectiveCollector(state,footprints,result);
            error=null;return true;
        }

        public string[] OriginalStableIds()
        {var ids=new string[originals.Count];for(int i=0;i<ids.Length;i++)ids[i]=originals[i].Id;return ids;}
        public bool IsOriginalEntity(Entity entity)
        {foreach(var original in originals)if(original.Entity==entity)return true;return false;}

        public bool TryExportCheckpointRows(EntityManager em,Entity session,
            out SkirmishObjectiveCollectorOriginalRow[] rows,out string error)
        {
            rows=null;
            if(!SessionMatches(em,session,out _,out error))return false;
            if(pendingFacts!=null){error="Cannot checkpoint an uncommitted objective fact snapshot.";return false;}
            if(!ReadOwned(em,sessionId,out var actors,out error))return false;
            var staged=new SkirmishObjectiveCollectorOriginalRow[originals.Count];
            for(int i=0;i<originals.Count;i++)
            {
                var original=originals[i];
                bool present=actors.TryGetValue(original.Id,out var current);
                if(present&&(current!=original.Entity||!ExactOriginal(em,original,out error)))return false;
                if(original.Disposition==SkirmishOriginalObjectiveDisposition.Live&&
                    (!present||!em.HasComponent<UnitHealth>(current)||em.GetComponentData<UnitHealth>(current).Current<=0||
                     em.HasComponent<UnitDeathAnimationComponent>(current)||em.GetComponentData<SkirmishAttemptOwnedComponent>(current).CapacityReleased!=0))
                {error="A live original needs committed native life evidence before checkpoint.";return false;}
                if(original.Disposition==SkirmishOriginalObjectiveDisposition.Destroyed&&present&&
                    (!em.HasComponent<UnitHealth>(current)||(em.GetComponentData<UnitHealth>(current).Current>0&&!em.HasComponent<UnitDeathAnimationComponent>(current))))
                {error="Destroyed original was resurrected.";return false;}
                if((original.Disposition==SkirmishOriginalObjectiveDisposition.Evacuated||original.Disposition==SkirmishOriginalObjectiveDisposition.Delivered)&&present)
                {error="Successful departure must finish native removal before checkpoint.";return false;}
                staged[i]=new SkirmishObjectiveCollectorOriginalRow{StableOriginalId=original.Id,ObjectiveStableId=original.ObjectiveStableId,
                    Role=original.Role,Faction=original.Faction,IsStructure=original.IsStructure,UnitRole=original.UnitRole,
                    Category=original.Category,Disposition=original.Disposition};
            }
            rows=staged;error=null;return true;
        }

        // The caller first validates checksum-covered rows against the immutable
        // compiled original plan and restores authoritative clock/reducer state.
        // This method binds only identities; payload geometry is never consumed.
        public static bool TryRebindCheckpoint(EntityManager em,Entity session,SkirmishCompiledObjectiveAreas footprints,
            string expectedLogicalFingerprint,SkirmishObjectiveCollectorOriginalRow[] rows,
            out SkirmishNativeObjectiveCollector collector,out string error)
        {
            collector=null;
            if(!Session(em,session,out var state,out error)||footprints==null||footprints.ObjectiveKind!=state.ObjectiveKind||
                string.IsNullOrWhiteSpace(expectedLogicalFingerprint)||footprints.LogicalFingerprint!=expectedLogicalFingerprint)
            {error=error??"Recovery requires actual compiled objective areas.";return false;}
            int required=state.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl?2:
                state.ObjectiveKind==SkirmishObjectiveKind.Breakthrough?12:state.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort?3:0;
            if(required==0||rows==null||rows.Length!=required){error="Recovery original count mismatch.";return false;}
            if(!ReadOwned(em,state.SessionId,out var actors,out error))return false;
            var result=new List<Original>();var ids=new HashSet<string>(StringComparer.Ordinal);bool playerBase=false,enemyBase=false;
            foreach(var row in rows)
            {
                if(string.IsNullOrWhiteSpace(row.StableOriginalId)||System.Text.Encoding.UTF8.GetByteCount(row.StableOriginalId)>61||
                    string.IsNullOrWhiteSpace(row.ObjectiveStableId)||System.Text.Encoding.UTF8.GetByteCount(row.ObjectiveStableId)>61||!ids.Add(row.StableOriginalId)||
                    (byte)row.Disposition>(byte)SkirmishOriginalObjectiveDisposition.Delivered)
                {error="Invalid/duplicate checkpoint original identity.";return false;}
                bool fc=state.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl;
                if(fc)
                {
                    bool player=row.Role==SkirmishObjectiveRoleKind.PlayerBase;
                    if((!player&&row.Role!=SkirmishObjectiveRoleKind.EnemyBase)||row.IsStructure!=1||row.Faction!=(player?1:2)||
                        (player?playerBase:enemyBase)||row.Disposition==SkirmishOriginalObjectiveDisposition.Evacuated||row.Disposition==SkirmishOriginalObjectiveDisposition.Delivered)
                    {error="Invalid checkpoint base original.";return false;}
                    if(player)playerBase=true;else enemyBase=true;
                }
                else
                {
                    bool bt=state.ObjectiveKind==SkirmishObjectiveKind.Breakthrough;
                    if(row.Faction!=1||row.IsStructure!=0||row.ObjectiveStableId!=row.StableOriginalId||
                        row.Role!=(bt?SkirmishObjectiveRoleKind.BreakthroughDesignated:SkirmishObjectiveRoleKind.ConvoyTruck)||
                        row.UnitRole!=(bt?SkirmishRoleKind.Rifle:SkirmishRoleKind.ConvoyLogic)||
                        row.Category!=(bt?SkirmishPopulationCategory.Infantry:SkirmishPopulationCategory.ObjectiveSupport)||
                        row.Disposition==(bt?SkirmishOriginalObjectiveDisposition.Delivered:SkirmishOriginalObjectiveDisposition.Evacuated))
                    {error="Invalid checkpoint mobile original.";return false;}
                }
                actors.TryGetValue(row.StableOriginalId,out var entity);
                var original=new Original{Id=row.StableOriginalId,Entity=entity,Role=row.Role,Faction=row.Faction,IsStructure=row.IsStructure,
                    ObjectiveStableId=row.ObjectiveStableId,UnitRole=row.UnitRole,Category=row.Category,Disposition=row.Disposition};
                result.Add(original);
            }
            foreach(var pair in actors)
            {
                if(!em.HasComponent<SkirmishObjectiveRoleComponent>(pair.Value))continue;
                var actual=em.GetComponentData<SkirmishObjectiveRoleComponent>(pair.Value).Role;
                bool selected=state.ObjectiveKind==SkirmishObjectiveKind.FrontlineControl?
                    actual==SkirmishObjectiveRoleKind.PlayerBase||actual==SkirmishObjectiveRoleKind.EnemyBase:
                    actual==(state.ObjectiveKind==SkirmishObjectiveKind.Breakthrough?SkirmishObjectiveRoleKind.BreakthroughDesignated:SkirmishObjectiveRoleKind.ConvoyTruck);
                if(selected&&!ids.Contains(pair.Key)){error="Fresh world contains an unplanned original objective actor.";return false;}
            }
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Id,b.Id));
            var candidate=new SkirmishNativeObjectiveCollector(state,footprints,result);
            // Reuse the exact same native life and terminal-removal rules as save.
            if(!candidate.TryExportCheckpointRows(em,session,out _,out error))return false;
            collector=candidate;error=null;return true;
        }

        // Observed zero HP/death animation is retained before the shared lifecycle
        // removes that actor. Disappearance alone never becomes a destroyed fact.
        public bool TryRecordDestroyedBeforeRemoval(EntityManager em, Entity session, Entity actor, out string error)
        {
            if(!SessionMatches(em,session,out _,out error))return false;
            if(pendingFacts!=null){error="Commit/reject the pending fact snapshot before lifecycle mutation.";return false;}
            foreach(var original in originals)if(original.Entity==actor)
            {
                if(!ExactOriginal(em,original,out error))return false;
                if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||
                    !em.HasComponent<UnitHealth>(actor)||
                    (em.GetComponentData<UnitHealth>(actor).Current>0&&!em.HasComponent<UnitDeathAnimationComponent>(actor)))
                {error="Actual shared death evidence required before removal.";return false;}
                if(original.Disposition==SkirmishOriginalObjectiveDisposition.Destroyed){error=null;return true;}
                if(original.Disposition!=SkirmishOriginalObjectiveDisposition.Live)
                {error="Successful-removal tombstone cannot become a death.";return false;}
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(owned.SessionId!=sessionId||owned.StableObjectId.ToString()!=original.Id)
                {error="Original native ownership changed.";return false;}
                original.Disposition=SkirmishOriginalObjectiveDisposition.Destroyed;error=null;return true;
            }
            error="Unknown original actor.";return false;
        }

        // Only the actual reducer's terminal subject transition authorizes a
        // successful-removal tombstone. Lifecycle/capacity removal is a separate
        // owner and must run exactly once without treating evacuation as death.
        public bool TryRecordEvacuated(EntityManager em,Entity session,string id,in SkirmishBreakthroughState state,out string error)
        {
            if(kind!=SkirmishObjectiveKind.Breakthrough){error="Wrong objective.";return false;}
            if(state.OriginalSubjects!=null)foreach(var subject in state.OriginalSubjects)
                if(subject.StableOriginalId==id&&subject.Status==SkirmishBreakthroughSubjectStatus.Evacuated)
                    return RecordSuccessfulRemoval(em,session,id,SkirmishOriginalObjectiveDisposition.Evacuated,out error);
            error="No matching actual reducer evacuation transition.";return false;
        }
        public bool TryRecordDelivered(EntityManager em,Entity session,string id,in SkirmishConvoyState state,out string error)
        {
            if(kind!=SkirmishObjectiveKind.ConvoyEscort){error="Wrong objective.";return false;}
            if(state.OriginalTrucks!=null)foreach(var subject in state.OriginalTrucks)
                if(subject.StableOriginalId==id&&subject.Status==SkirmishConvoySubjectStatus.Delivered)
                    return RecordSuccessfulRemoval(em,session,id,SkirmishOriginalObjectiveDisposition.Delivered,out error);
            error="No matching actual reducer delivery transition.";return false;
        }
        private bool RecordSuccessfulRemoval(EntityManager em,Entity session,string id,SkirmishOriginalObjectiveDisposition disposition,out string error)
        {
            if(!SessionMatches(em,session,out _,out error))return false;
            if(pendingFacts!=null){error="Commit/reject the pending fact snapshot before lifecycle mutation.";return false;}
            foreach(var original in originals)if(original.Id==id)
            {
                if(!ExactOriginal(em,original,out error))return false;
                if(original.Disposition!=SkirmishOriginalObjectiveDisposition.Live||!em.Exists(original.Entity)||
                    !em.HasComponent<UnitHealth>(original.Entity)||em.GetComponentData<UnitHealth>(original.Entity).Current<=0||
                    em.HasComponent<UnitDeathAnimationComponent>(original.Entity))
                {error="Successful removal requires the original still-living actor exactly once.";return false;}
                if(!em.HasComponent<SkirmishAttemptOwnedComponent>(original.Entity))
                {error="Original successful-removal ownership missing.";return false;}
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(original.Entity);
                if(owned.SessionId!=sessionId||owned.StableObjectId.ToString()!=original.Id||owned.FactionId!=1||
                    owned.CapacityReleased!=0||!OnGroundNotPassenger(em,original.Entity)||
                    !TryWorldPosition(em,original.Entity,out var position,out error))
                {error=error??"Original successful-removal identity/passenger state changed.";return false;}
                var role=disposition==SkirmishOriginalObjectiveDisposition.Evacuated ? SkirmishObjectiveAreaRole.BreakthroughExit : SkirmishObjectiveAreaRole.ConvoyDestination;
                if(!areas.Contains(role,position.x,position.z)){error="Original left the objective before lifecycle commit.";return false;}
                original.Disposition=disposition;error=null;return true;
            }
            error="Unknown original stable ID.";return false;
        }

        // Caller stages this snapshot, steps the matching reducer, and commits
        // NextClock/NextTombstones ONLY after reducer validation succeeds. Repeated
        // same-clock collection yields zero delta; no ECS or ledger writes here.
        public bool TryCollect(EntityManager em,Entity session,in SkirmishObjectiveCollectorClock cursor,
            out SkirmishNativeObjectiveFacts facts,out string error)
        {
            facts=null;
            pendingFacts=null;pendingTombstones=null;
            if(!SessionMatches(em,session,out var state,out error)||!em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {error=error??"Authoritative objective clock missing.";return false;}
            var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            if(clock.Playing!=(state.Phase==SkirmishSessionPhase.Playing?(byte)1:(byte)0))
            {error="Clock/session phase mismatch.";return false;}
            if(!TryProjectClock(cursor,clock,out var nextClock,out long delta,out error))return false;
            if(!ReadOwned(em,sessionId,out var actors,out error))return false;
            var staged=new SkirmishNativeObjectiveFacts{NextClock=nextClock,NextTombstones=new SkirmishOriginalObjectiveTombstone[originals.Count]};
            bool playing=clock.Playing!=0,paused=clock.Paused!=0;
            staged.Frontline=new SkirmishFrontlineTickFacts{Playing=playing,Paused=paused,SimulationDeltaTicks=delta};
            staged.Breakthrough=new SkirmishBreakthroughTickFacts{Playing=playing,Paused=paused,SimulationDeltaTicks=delta};
            staged.Convoy=new SkirmishConvoyTickFacts{Playing=playing,Paused=paused,SimulationDeltaTicks=delta};
            if(kind==SkirmishObjectiveKind.Breakthrough)staged.Breakthrough.OriginalSubjects=new SkirmishBreakthroughSubjectFacts[originals.Count];
            if(kind==SkirmishObjectiveKind.ConvoyEscort)staged.Convoy.OriginalTrucks=new SkirmishConvoySubjectFacts[originals.Count];
            for(int i=0;i<originals.Count;i++)
            {
                var original=originals[i];var disposition=original.Disposition;bool alive=false,dismounted=false;float3 position=default;
                if(disposition==SkirmishOriginalObjectiveDisposition.Live)
                {
                    if(!actors.TryGetValue(original.Id,out Entity entity)||entity!=original.Entity||!em.HasComponent<UnitHealth>(entity))
                    {error="Original disappeared/replaced without authoritative lifecycle tombstone: "+original.Id;return false;}
                    if(!ExactOriginal(em,original,out error))return false;
                    alive=em.GetComponentData<UnitHealth>(entity).Current>0&&!em.HasComponent<UnitDeathAnimationComponent>(entity);
                    if(!alive)disposition=SkirmishOriginalObjectiveDisposition.Destroyed;
                    else if(kind!=SkirmishObjectiveKind.FrontlineControl)
                    {
                        dismounted=OnGroundNotPassenger(em,entity);
                        if(dismounted&&!TryWorldPosition(em,entity,out position,out error))return false;
                    }
                }
                staged.NextTombstones[i]=new SkirmishOriginalObjectiveTombstone{StableOriginalId=original.Id,Disposition=disposition};
                if(kind==SkirmishObjectiveKind.FrontlineControl)
                {
                    if(original.Role==SkirmishObjectiveRoleKind.PlayerBase)staged.Frontline.PlayerOriginalBaseAlive=alive;
                    else staged.Frontline.EnemyOriginalBaseAlive=alive;
                }
                else if(kind==SkirmishObjectiveKind.Breakthrough)
                    staged.Breakthrough.OriginalSubjects[i]=new SkirmishBreakthroughSubjectFacts{StableOriginalId=original.Id,AuthoritativeLifeKnown=true,
                        Alive=alive,Dismounted=dismounted,InsideExit=alive&&dismounted&&areas.Contains(SkirmishObjectiveAreaRole.BreakthroughExit,position.x,position.z)};
                else staged.Convoy.OriginalTrucks[i]=new SkirmishConvoySubjectFacts{StableOriginalId=original.Id,AuthoritativeLifeKnown=true,
                    Alive=alive,InsideDestination=alive&&dismounted&&areas.Contains(SkirmishObjectiveAreaRole.ConvoyDestination,position.x,position.z)};
            }
            if(kind==SkirmishObjectiveKind.FrontlineControl)staged.Frontline.OriginalBaseFactsPresent=true;
            if(kind!=SkirmishObjectiveKind.ConvoyEscort)
                foreach(var pair in actors)
                {
                    Entity actor=pair.Value;
                    bool removed=false;
                    foreach(var tombstone in staged.NextTombstones)if(tombstone.StableOriginalId==pair.Key&&
                        tombstone.Disposition!=SkirmishOriginalObjectiveDisposition.Live){removed=true;break;}
                    if(removed)continue;
                    if(!em.HasComponent<SkirmishUnitRoleComponent>(actor))continue;
                    var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                    if(role.Category!=SkirmishPopulationCategory.Infantry||!CombatInfantry(role.Role))continue;
                    if(!em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<UnitHealth>(actor))
                    {error="Combat infantry native life/ownership facts missing.";return false;}
                    var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if(owner.CapacityReleased!=0||em.GetComponentData<UnitHealth>(actor).Current<=0||
                        em.HasComponent<UnitDeathAnimationComponent>(actor)||!OnGroundNotPassenger(em,actor))continue;
                    if((owner.FactionId!=1&&owner.FactionId!=2)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=owner.FactionId)
                    {error="Unknown/mismatched native combat faction.";return false;}
                    if(!TryWorldPosition(em,actor,out var position,out error))return false;
                    bool player=owner.FactionId==1;
                    if(kind==SkirmishObjectiveKind.FrontlineControl)
                    {
                        if(areas.Contains(SkirmishObjectiveAreaRole.FrontlineA,position.x,position.z))Increment(ref staged.Frontline.ZoneA,player);
                        if(areas.Contains(SkirmishObjectiveAreaRole.FrontlineB,position.x,position.z))Increment(ref staged.Frontline.ZoneB,player);
                        if(areas.Contains(SkirmishObjectiveAreaRole.FrontlineC,position.x,position.z))Increment(ref staged.Frontline.ZoneC,player);
                    }
                    else
                    {
                        if(areas.Contains(SkirmishObjectiveAreaRole.BreakthroughCorridorA,position.x,position.z))Increment(ref staged.Breakthrough.CorridorA,player);
                        if(areas.Contains(SkirmishObjectiveAreaRole.BreakthroughCorridorB,position.x,position.z))Increment(ref staged.Breakthrough.CorridorB,player);
                    }
                }
            pendingFacts=staged;pendingTombstones=(SkirmishOriginalObjectiveTombstone[])staged.NextTombstones.Clone();
            facts=staged;error=null;return true;
        }

        public static bool TryProjectClock(in SkirmishObjectiveCollectorClock cursor,in SkirmishObjectiveClockComponent clock,
            out SkirmishObjectiveCollectorClock next,out long delta,out string error)
        {
            next=cursor;delta=0;error="Invalid/regressing authoritative objective clock.";
            double now=clock.ElapsedSeconds;
            if(clock.Playing>1||clock.Paused>1||double.IsNaN(now)||double.IsInfinity(now)||now<0||double.IsNaN(cursor.LastAuthoritativeSeconds)||
                double.IsInfinity(cursor.LastAuthoritativeSeconds)||cursor.LastAuthoritativeSeconds<0||now<cursor.LastAuthoritativeSeconds||
                double.IsNaN(cursor.FractionalTicks)||double.IsInfinity(cursor.FractionalTicks)||cursor.FractionalTicks<0||cursor.FractionalTicks>=1)return false;
            if((clock.Playing==0||clock.Paused!=0)&&now!=cursor.LastAuthoritativeSeconds)
            {error="Paused/nonplaying authoritative clock advanced.";return false;}
            double total=(now-cursor.LastAuthoritativeSeconds)*TimeSpan.TicksPerSecond+cursor.FractionalTicks;
            if(total>=long.MaxValue)return false;
            delta=(long)Math.Floor(total);next.LastAuthoritativeSeconds=now;next.FractionalTicks=total-delta;error=null;return true;
        }

        public bool TryCommitCollectedTombstones(SkirmishNativeObjectiveFacts facts,out string error)
        {
            error="Invalid/reordered or regressing original tombstone commit.";
            if(facts==null||!ReferenceEquals(facts,pendingFacts)||pendingTombstones==null||facts.NextTombstones==null||facts.NextTombstones.Length!=originals.Count)return false;
            var staged=facts.NextTombstones;
            for(int i=0;i<originals.Count;i++)
                if(staged[i].StableOriginalId!=pendingTombstones[i].StableOriginalId||staged[i].Disposition!=pendingTombstones[i].Disposition||
                    staged[i].StableOriginalId!=originals[i].Id||
                    (originals[i].Disposition!=SkirmishOriginalObjectiveDisposition.Live&&staged[i].Disposition!=originals[i].Disposition)||
                    (staged[i].Disposition!=originals[i].Disposition&&staged[i].Disposition!=SkirmishOriginalObjectiveDisposition.Destroyed))return false;
            for(int i=0;i<originals.Count;i++)originals[i].Disposition=staged[i].Disposition;
            pendingFacts=null;pendingTombstones=null;
            error=null;return true;
        }
        private bool SessionMatches(EntityManager em,Entity session,out SkirmishExpandedSessionComponent state,out string error)
        {
            if(!Session(em,session,out state,out error))return false;
            if(state.SessionId!=sessionId||state.ObjectiveKind!=kind||state.DefinitionId!=definitionId||state.SetupHash!=setupHash||state.ContentVersion!=contentVersion)
            {error="Collector attempt/objective/setup identity changed.";return false;}
            return true;
        }
        private bool ExactOriginal(EntityManager em,Original original,out string error)
        {
            error="Sealed original native identity changed.";Entity actor=original.Entity;
            if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<SkirmishSharedActorTag>(actor)||
                !em.HasComponent<SkirmishObjectiveRoleComponent>(actor)||!em.HasComponent<Faction>(actor)||
                em.GetComponentData<Faction>(actor).Id!=original.Faction)return false;
            var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var role=em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);
            if(owned.SessionId!=sessionId||owned.StableObjectId.ToString()!=original.Id||owned.FactionId!=original.Faction||
                owned.IsStructure!=original.IsStructure||owned.ReservationId!=0||role.Role!=original.Role||role.FactionId!=original.Faction||
                role.StableObjectId.ToString()!=original.ObjectiveStableId)return false;
            if(original.IsStructure!=0)
            {
                if(!em.HasComponent<SkirmishStructureIdentityComponent>(actor)||em.GetComponentData<SkirmishStructureIdentityComponent>(actor).DesignatedBase==0)return false;
            }
            else if(!em.HasComponent<SkirmishUnitRoleComponent>(actor)||em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role!=original.UnitRole||
                em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category!=original.Category)return false;
            error=null;return true;
        }
        private static bool Session(EntityManager em,Entity session,out SkirmishExpandedSessionComponent state,out string error)
        {
            state=default;error="Actual expanded session missing.";
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(state.IsLegacy!=0||state.SessionId.IsEmpty)return false;
            error=null;return true;
        }
        private static bool ReadOwned(EntityManager em,FixedString64Bytes attempt,out Dictionary<string,Entity> actors,out string error)
        {
            actors=new Dictionary<string,Entity>(StringComparer.Ordinal);error=null;
            using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)
            {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);if(owned.SessionId!=attempt)continue;
                string id=owned.StableObjectId.ToString();
                if(string.IsNullOrWhiteSpace(id)||actors.ContainsKey(id)){error="Missing/duplicate session-owned stable actor ID.";return false;}
                actors.Add(id,entity);
            }
            return true;
        }
        private static bool OnGroundNotPassenger(EntityManager em,Entity actor) => !em.HasComponent<Disabled>(actor)&&
            !em.HasComponent<UnitTransportPassenger>(actor)&&!em.HasComponent<UnitTransportCargoPassenger>(actor)&&
            !em.HasComponent<UnitTransportParachuteDropComponent>(actor)&&!em.HasComponent<UnitTransportRopeDropComponent>(actor);
        private static bool CombatInfantry(SkirmishRoleKind role) => role==SkirmishRoleKind.Rifle||role==SkirmishRoleKind.Gunner||
            role==SkirmishRoleKind.Marksman||role==SkirmishRoleKind.Breacher||role==SkirmishRoleKind.Rocketeer;

        // Compute from current native local/parent values, avoiding a stale
        // LocalToWorld read before the transform playback group has run.
        private static bool TryWorldPosition(EntityManager em,Entity actor,out float3 position,out string error)
        {
            position=default;error="Missing/nonfinite/cyclic native actor transform.";
            var seen=new HashSet<Entity>();float4x4 matrix=float4x4.identity;Entity current=actor;
            while(current!=Entity.Null)
            {
                if(!em.Exists(current)||!seen.Add(current)||seen.Count>64||!em.HasComponent<LocalTransform>(current))return false;
                var local=em.GetComponentData<LocalTransform>(current).ToMatrix();
                if(em.HasComponent<PostTransformMatrix>(current))local=math.mul(local,em.GetComponentData<PostTransformMatrix>(current).Value);
                matrix=math.mul(local,matrix);
                current=em.HasComponent<Parent>(current)?em.GetComponentData<Parent>(current).Value:Entity.Null;
            }
            position=matrix.c3.xyz;if(!math.all(math.isfinite(position)))return false;error=null;return true;
        }
        private static void Increment(ref SkirmishFrontlineZoneFacts zone,bool player)
        {if(player)zone.PlayerEligibleInfantry++;else zone.EnemyEligibleInfantry++;}
        private static void Increment(ref SkirmishBreakthroughCorridorFacts zone,bool player)
        {if(player)zone.PlayerEligibleInfantry++;else zone.EnemyEligibleInfantry++;}
    }
}
