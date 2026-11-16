using System;using Game.Components;using Unity.Entities;using Unity.Collections;
namespace Game.Runtime {
internal static class SourceMovementContinuationAdmission {
 internal static void Require(EntityManager em,bool retryBound,bool attackBound){Check<UnitPathRetryCooldown>(em,retryBound,"UnitManualMoveRetrySystem");Check<AttackMoveOrder>(em,attackBound,"AttackMoveSystem.OnUpdate/ResumeTravel");}
 static void Check<T>(EntityManager em,bool bound,string owner)where T:struct{if(bound)return;using var q=em.CreateEntityQuery(typeof(T));using var e=q.ToEntityArray(Allocator.Temp);if(e.Length>0)throw new NotSupportedException("Unadmitted active source continuation: "+owner+" required by "+typeof(T).FullName+" on "+e[0]);}
}
internal sealed partial class AutonomousWorld {
 UnitManualMoveRetrySystem sourceManualRetry;SystemState sourceManualRetryState;AttackMoveSystem sourceAttackMove;SystemState sourceAttackMoveState;bool sourceMovementContinuationsBound;
 void InitializeActualMovementContinuations(){if(sourceMovementContinuationsBound)throw new InvalidOperationException("Original movement continuations bind once");sourceManualRetryState=FreshState();sourceManualRetryState.Begin();sourceManualRetry.OnCreate(ref sourceManualRetryState);sourceManualRetryState.End();sourceManualRetryState.LastSystemVersion=0;sourceAttackMoveState=FreshState();sourceAttackMoveState.Begin();sourceAttackMove.OnCreate(ref sourceAttackMoveState);sourceAttackMoveState.End();sourceAttackMoveState.LastSystemVersion=0;sourceMovementContinuationsBound=true;}
 void ValidateActualMovementContinuationActivation()=>SourceMovementContinuationAdmission.Require(Entities,sourceMovementContinuationsBound,sourceMovementContinuationsBound);
 void AdvanceActualManualMoveRetry(){sourceManualRetryState.Begin();if(sourceManualRetryState.Enabled&&sourceManualRetryState.RequiredQueriesPresent)sourceManualRetry.OnUpdate(ref sourceManualRetryState);sourceManualRetryState.End();}
 void AdvanceActualAttackMoveContinuation(){sourceAttackMoveState.Begin();if(sourceAttackMoveState.Enabled&&sourceAttackMoveState.RequiredQueriesPresent)sourceAttackMove.OnUpdate(ref sourceAttackMoveState);sourceAttackMoveState.End();}
}
}
