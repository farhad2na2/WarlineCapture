using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    public enum SkirmishAcceptedArmyOrderArgumentKind : byte { None, Terrain, Entity }
    public readonly struct SkirmishAcceptedArmyOrderIntent
    {
        public readonly ulong Sequence;
        public readonly SkirmishPublicArmyOrderGoal PublicGoal;
        public readonly SkirmishAcceptedArmyOrderArgumentKind ArgumentKind;
        public readonly int CellX,CellY;
        public readonly float WorldX,WorldY,WorldZ;
        public readonly Entity Target;
        internal SkirmishAcceptedArmyOrderIntent(ulong sequence,SkirmishPublicArmyOrderGoal goal,SkirmishAcceptedArmyOrderArgumentKind kind,int x,int y,float3 position,Entity target){Sequence=sequence;PublicGoal=goal;ArgumentKind=kind;CellX=x;CellY=y;WorldX=position.x;WorldY=position.y;WorldZ=position.z;Target=target;}
    }
    // A last-accepted-command ledger, not a cache of queries or predicted orders.
    public sealed class SkirmishAcceptedArmyOrderLedger
    {
        readonly Dictionary<string,Dictionary<uint,SkirmishAcceptedArmyOrderIntent>> sessions=new();
        ulong sequence;
        readonly ulong worldSequenceNumber;
        public SkirmishAcceptedArmyOrderLedger(ulong worldSequenceNumber){if(worldSequenceNumber==0)throw new ArgumentOutOfRangeException(nameof(worldSequenceNumber));this.worldSequenceNumber=worldSequenceNumber;}
        public bool ObserveResult(bool accepted,string session,uint group,in SkirmishPublicArmyOrderGoal goal,SkirmishAcceptedArmyOrderArgumentKind kind,int x,int y,float3 position,Entity target)
        {
            if(!accepted)return false;
            if(string.IsNullOrEmpty(session)||group==0)throw new ArgumentException("Actual accepted session/group required");
            if(goal.Known&&(goal.SessionId!=session||goal.WorldSequenceNumber!=worldSequenceNumber))throw new InvalidOperationException("Accepted goal/session mismatch");
            if(!sessions.TryGetValue(session,out var groups)){groups=new Dictionary<uint,SkirmishAcceptedArmyOrderIntent>();sessions.Add(session,groups);}
            var associated=kind==SkirmishAcceptedArmyOrderArgumentKind.Terrain||kind==SkirmishAcceptedArmyOrderArgumentKind.Entity?goal:default;
            groups[group]=new SkirmishAcceptedArmyOrderIntent(checked(++sequence),associated,kind,x,y,position,target);return true;
        }
        public bool TryRead(string session,uint group,out SkirmishAcceptedArmyOrderIntent intent){intent=default;return session!=null&&sessions.TryGetValue(session,out var groups)&&groups.TryGetValue(group,out intent);}
        public bool Matches(string session,uint group,in SkirmishPublicArmyOrderGoal goal)
        {
            if(!goal.Known||goal.Kind==SkirmishPublicArmyOrderGoalKind.HomeDefense||!TryRead(session,group,out var intent))return false;
            if(intent.ArgumentKind!=SkirmishAcceptedArmyOrderArgumentKind.Terrain&&intent.ArgumentKind!=SkirmishAcceptedArmyOrderArgumentKind.Entity)return false;
            return intent.PublicGoal.Equals(goal)||(intent.ArgumentKind==SkirmishAcceptedArmyOrderArgumentKind.Entity&&intent.PublicGoal.Kind==SkirmishPublicArmyOrderGoalKind.VisibleObjective&&intent.PublicGoal.SamePublicObjective(goal));
        }
        public void RetainSessionGroups(string session,IEnumerable<uint> liveGroups)
        {
            if(session==null||liveGroups==null)throw new ArgumentNullException();
            var live=new HashSet<uint>(liveGroups);
            if(sessions.TryGetValue(session,out var groups)){var dead=new List<uint>();foreach(var id in groups.Keys)if(!live.Contains(id))dead.Add(id);foreach(var id in dead)groups.Remove(id);if(groups.Count==0)sessions.Remove(session);}
        }
        public void RetireSession(string session){if(session!=null)sessions.Remove(session);}
        public void RetainOnlyCurrentSession(string current){var removed=new List<string>();foreach(var id in sessions.Keys)if(id!=current)removed.Add(id);foreach(var id in removed)sessions.Remove(id);}
    }
    public static class SkirmishAcceptedArmyOrders
    {
        static readonly ConditionalWeakTable<object,SkirmishAcceptedArmyOrderLedger> worlds=new();
        public static SkirmishAcceptedArmyOrderLedger For(EntityManager em)=>worlds.GetValue(em.World,_=>new SkirmishAcceptedArmyOrderLedger(em.WorldUnmanaged.SequenceNumber));
        // Caller has established the one current SkirmishMatchState in this World.
        public static void ObserveCurrentSession(EntityManager em,Entity current)
        {
            string live=null;
            if(current!=Entity.Null&&em.Exists(current)&&em.HasComponent<SkirmishExpandedSessionComponent>(current)){
                var state=em.GetComponentData<SkirmishExpandedSessionComponent>(current);
                if(state.Phase==SkirmishSessionPhase.Playing)live=state.SessionId.ToString();
            }
            if(worlds.TryGetValue(em.World,out var ledger))ledger.RetainOnlyCurrentSession(live);
            AriaCommandEvidence.RetainCurrentPublicSession(em.WorldUnmanaged.SequenceNumber,live);
        }
        public static SkirmishPublicArmyOrderGoal PublicGoal(EntityManager em,Entity session,SkirmishPublicArmyOrderGoalKind kind,int publishedObjectiveIdentity,float x,float z)
        {
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            return new SkirmishPublicArmyOrderGoal(state.SessionId.ToString(),kind,publishedObjectiveIdentity,x,z,em.WorldUnmanaged.SequenceNumber);
        }
        public static int ReadMask(EntityManager em,Entity session,in SkirmishPublicArmyOrderGoal goal,int bareAttackMask,uint group0,uint group1,uint group2,uint group3,uint group4)
        {
            ObserveCurrentSession(em,session);
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);var sessionId=state.SessionId.ToString();var ledger=For(em);
            if(state.Phase!=SkirmishSessionPhase.Playing){ledger.RetireSession(sessionId);AriaCommandEvidence.RetirePublicGoals(sessionId,em.WorldUnmanaged.SequenceNumber);return 0;}
            if(!goal.Known||goal.WorldSequenceNumber!=em.WorldUnmanaged.SequenceNumber||goal.SessionId!=sessionId)return 0;
            var live=new List<uint>();var records=em.GetBuffer<SkirmishArmyGroupRecord>(session);
            for(int i=0;i<records.Length;i++)if(records[i].AliveCount>0)live.Add(records[i].GroupId);
            ledger.RetainSessionGroups(sessionId,live);
            uint[] slots={group0,group1,group2,group3,group4};int mask=0;
            for(int i=0;i<slots.Length;i++)if((bareAttackMask&(1<<i))!=0&&ledger.Matches(sessionId,slots[i],goal))mask|=1<<i;
            return mask;
        }
        public static SkirmishPublicArmyOrderGoal AssociateAcceptedGoal(in SkirmishPublicArmyOrderGoal requested,SkirmishAcceptedArmyOrderArgumentKind kind,string actualSession,ulong actualWorld,Entity actualTarget,Entity actualDesignatedBase)
        {
            if(!requested.Known||requested.SessionId!=actualSession||requested.WorldSequenceNumber!=actualWorld)return default;
            if(kind==SkirmishAcceptedArmyOrderArgumentKind.Terrain)return requested.Kind==SkirmishPublicArmyOrderGoalKind.VisibleObjective?default:requested;
            if(kind!=SkirmishAcceptedArmyOrderArgumentKind.Entity||requested.Kind!=SkirmishPublicArmyOrderGoalKind.VisibleObjective||actualDesignatedBase==Entity.Null||actualTarget!=actualDesignatedBase)return default;
            return requested;
        }
        public static void RecordAccepted(EntityManager em,Entity session,uint group,SkirmishAcceptedArmyOrderArgumentKind kind,int2 cell,float3 position,Entity target)
        {
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var actualBase=em.HasComponent<SkirmishMatchState>(session)?em.GetComponentData<SkirmishMatchState>(session).EnemyMainBase:Entity.Null;
            var goal=AssociateAcceptedGoal(AriaCommandEvidence.CurrentPublicGoal,kind,state.SessionId.ToString(),em.WorldUnmanaged.SequenceNumber,target,actualBase);
            For(em).ObserveResult(true,state.SessionId.ToString(),group,goal,kind,cell.x,cell.y,position,target);
        }
    }
}
