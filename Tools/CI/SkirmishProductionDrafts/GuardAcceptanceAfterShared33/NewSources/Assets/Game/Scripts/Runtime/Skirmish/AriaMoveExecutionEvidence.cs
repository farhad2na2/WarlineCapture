using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    // Optional synchronous observation of the ordinary native Move result. This
    // never selects actors, issues orders, or changes public BA HUD contracts.
    public static class AriaMoveExecutionEvidence
    {
        [Serializable] public sealed class Member
        {
            public int entityIndex,entityVersion,goalX,goalZ;
            public string stableId;
            public Vector3 goalWorld;
        }
        [Serializable] public sealed class Row
        {
            public string attempt,error,operation;
            public int frame,requestFrame,requestId;
            public uint receipt,group;
            public bool accepted,completeGroup;
            public Vector2 screen;
            public Vector3 marker;
            public Member[] members=Array.Empty<Member>();
        }
        public static event Action<Row> Recorded;
        public static bool Active=>AriaCommandEvidence.Active&&Recorded!=null;
        static bool hasAttackRequest;
        static RtsSelectionCommandIntentRequestElement attackRequest;
        public readonly struct AttackRequestScope:IDisposable
        {
            readonly bool had;readonly RtsSelectionCommandIntentRequestElement previous;
            public AttackRequestScope(in RtsSelectionCommandIntentRequestElement request){had=hasAttackRequest;previous=attackRequest;hasAttackRequest=Active;attackRequest=request;}
            public void Dispose(){hasAttackRequest=had;attackRequest=previous;}
        }
        public static void ObserveAttackMove(EntityManager em,NativeArray<Entity> accepted,in SelectedMoveOrderCommandSystem.Result result)
        {if(hasAttackRequest)Observe(em,attackRequest,result,accepted,true);}
        public static void Observe(EntityManager em,in RtsSelectionCommandIntentRequestElement request,in SelectedMoveOrderCommandSystem.Result result,NativeArray<Entity> accepted=default,bool attackMove=false)
        {
            if(!Active)return;
            var row=new Row{frame=Time.frameCount,requestFrame=request.Frame,requestId=request.RequestId,receipt=request.InputReceipt,
                group=result.PublicAcceptedGroup,accepted=result.CommandResult.Accepted,screen=request.ScreenPosition,marker=result.MarkerPosition,operation=attackMove?"AttackMove":"Move"};
            try
            {
                using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
                using var grids=em.CreateEntityQuery(typeof(GridConfig));
                if(sessions.CalculateEntityCount()!=1||grids.CalculateEntityCount()!=1)throw new InvalidOperationException("Unique native session/grid required");
                var session=sessions.GetSingleton<SkirmishExpandedSessionComponent>();row.attempt=session.SessionId.ToString();
                var grid=grids.GetSingleton<GridConfig>();if(grid.CellSize<=0)throw new InvalidOperationException("Native grid size invalid");
                using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishArmyGroupMembershipComponent),typeof(UnitHealth),typeof(Faction));
                using var entities=selected.ToEntityArray(Allocator.Temp);
                var members=new System.Collections.Generic.List<Member>();
                var exactAccepted=new System.Collections.Generic.HashSet<Entity>();
                if(attackMove){if(!accepted.IsCreated)throw new InvalidOperationException("Actual Attack Move accepted list missing");foreach(var actor in accepted)if(!exactAccepted.Add(actor))throw new InvalidOperationException("Duplicate native accepted member");}
                foreach(var actor in entities)
                {
                    if(em.GetComponentData<UnitHealth>(actor).Current<=0)continue;
                    var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if(own.SessionId!=session.SessionId||own.FactionId!=1||own.CapacityReleased!=0||own.IsStructure!=0||
                        em.GetComponentData<Faction>(actor).Id!=1||em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=row.group||
                        own.StableObjectId.IsEmpty||!em.HasComponent<ManualMoveOrderTag>(actor)||!em.HasComponent<UnitTarget>(actor))
                        throw new InvalidOperationException("Actual selected member identity/order differs from complete result");
                    int2 goal=em.HasComponent<UnitLongDistanceMove>(actor)?em.GetComponentData<UnitLongDistanceMove>(actor).FinalGoal:em.GetComponentData<UnitTarget>(actor).Cell;
                    if(attackMove&&(!exactAccepted.Remove(actor)||!em.HasComponent<AttackMoveOrder>(actor)||!em.GetComponentData<AttackMoveOrder>(actor).Destination.Equals(goal)))
                        throw new InvalidOperationException("Selected member omitted or native Attack Move destination differs");
                    if(goal.x<0||goal.y<0||goal.x>=grid.Width||goal.y>=grid.Height)throw new InvalidOperationException("Actual member goal outside native grid");
                    members.Add(new Member{entityIndex=actor.Index,entityVersion=actor.Version,stableId=own.StableObjectId.ToString(),goalX=goal.x,goalZ=goal.y,
                        goalWorld=GridUtils.CellToWorldCenter(grid,goal)});
                }
                members.Sort((a,b)=>StringComparer.Ordinal.Compare(a.stableId,b.stableId));row.members=members.ToArray();
                if(attackMove&&exactAccepted.Count!=0)throw new InvalidOperationException("Native Attack Move accepted a member outside current selection");
                row.completeGroup=row.accepted&&row.group!=0&&row.members.Length>0;
            }
            catch(Exception error){row.error=error.Message;row.completeGroup=false;}
            Recorded?.Invoke(row);
        }
    }
}
