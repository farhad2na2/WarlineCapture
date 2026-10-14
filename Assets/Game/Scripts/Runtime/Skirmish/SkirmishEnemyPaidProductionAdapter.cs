using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // Planner intent only. The shared native queue remains the sole owner of costs,
    // stock, capacity, timing, delivery and refunds. No accepted request is delivery.
    public static class SkirmishEnemyPaidProductionAdapter
    {
        public static bool IsOutstanding(in SkirmishEnemyProductionIntentV1Component value) =>
            value.State == SkirmishEnemyProductionIntentState.RequestPending ||
            value.State == SkirmishEnemyProductionIntentState.AcceptedWaiting ||
            value.State == SkirmishEnemyProductionIntentState.Backoff;

        public static SkirmishEnemyProductionIntentState TryRequest(EntityManager em, Entity session,
            FixedString64Bytes actionKey, SkirmishRoleKind role, uint planRevision,
            SkirmishArmyProfileConfig army, float objectiveElapsed)
        {
            if (!TryActive(em, session, objectiveElapsed, out var intent)) return ReadState(em, session);
            // A newly scored role cannot consume, relabel or replace an outstanding request.
            if (IsOutstanding(intent)) return Tick(em, session, army, objectiveElapsed);
            if (intent.ActionKey.Equals(actionKey) && !actionKey.IsEmpty)
                return Tick(em, session, army, objectiveElapsed);
            if (actionKey.IsEmpty || role == SkirmishRoleKind.None || intent.NextRequestId < 0 ||
                intent.NextRequestId >= int.MaxValue - 1 || !TryExpectedMembers(em,session,role,out int members))
                return intent.State;
            int next = Math.Max(1,intent.NextRequestId);
            intent = new SkirmishEnemyProductionIntentV1Component {
                Version = 1, State = SkirmishEnemyProductionIntentState.Backoff,
                ActionKey = actionKey, Role = role, PlanRevision = planRevision,
                RequestId = next, NextRequestId = next + 1, ExpectedMembers = members, RetryNotBeforeElapsed = objectiveElapsed
            };
            em.SetComponentData(session, intent);
            return Tick(em, session, army, objectiveElapsed);
        }

        public static SkirmishEnemyProductionIntentState Tick(EntityManager em, Entity session,
            SkirmishArmyProfileConfig army, float objectiveElapsed)
        {
            if (!TryActive(em, session, objectiveElapsed, out var intent)) return ReadState(em, session);
            if (intent.State == SkirmishEnemyProductionIntentState.Idle ||
                intent.State == SkirmishEnemyProductionIntentState.Abandoned ||
                intent.State == SkirmishEnemyProductionIntentState.Lost) return intent.State;
            if (intent.ReservationId != 0)
            {
                ReconcileAccepted(em, session, ref intent);
                ConsumeExactCompletedRequest(em, session, in intent);
                em.SetComponentData(session, intent);return intent.State;
            }
            if (!TryRequestBuffer(em, out var boundary)) return intent.State; // Pipeline readiness is not rejection.
            var requests = em.GetBuffer<BuildingFactionUnitProductionRequest>(boundary);
            int match = -1;
            for (int i = 0; i < requests.Length; i++)
            {
                if (!Matches(em, session, in intent, requests[i])) continue;
                if (match >= 0) { Abandon(ref intent, "duplicate_exact_request"); em.SetComponentData(session, intent);return intent.State; }
                match = i;
            }
            if (match >= 0)
            {
                var request = requests[match];
                if (request.Status == BuildingFactionUnitProductionRequest.Pending)
                {
                    intent.State = SkirmishEnemyProductionIntentState.RequestPending;
                    em.SetComponentData(session, intent);return intent.State;
                }
                requests.RemoveAt(match); // Only this exact correlated, completed intent row.
                if (request.Status == BuildingFactionUnitProductionRequest.Succeeded)
                    Abandon(ref intent, "accepted_without_receipt_witness"); // Never guess latest receipt or pay twice.
                else
                    Reject(ref intent, SkirmishEnemyRejectDomain.NativeResult, request.ResultCode,
                        "native.unit_queue", objectiveElapsed);
                em.SetComponentData(session, intent);return intent.State;
            }
            if (intent.State == SkirmishEnemyProductionIntentState.RequestPending)
            {
                // Native request buffers are transient across Continue. Atomic acceptance
                // persists the receipt witness before the queue call returns; Reservation0
                // therefore permits only this SAME token to be redispatched, not a new intent.
                intent.State = SkirmishEnemyProductionIntentState.Backoff;
            }
            if (intent.State != SkirmishEnemyProductionIntentState.Backoff || objectiveElapsed < intent.RetryNotBeforeElapsed)
                return intent.State;
            var decision = EvaluateDispatch(em, session, intent.Role);
            if (!decision.Accepted)
            {
                Reject(ref intent, SkirmishEnemyRejectDomain.Feasibility, (ushort)decision.Reason,
                    decision.Field, objectiveElapsed);em.SetComponentData(session, intent);return intent.State;
            }
            // A token collision is not permission to consume another role's completed row.
            foreach (var request in requests)
                if (request.RequestOwner == session && request.RequestAttempt.Equals(Attempt(em,session)) &&
                    request.RequestId == intent.RequestId)
                { Abandon(ref intent, "request_token_collision");em.SetComponentData(session,intent);return intent.State; }
            intent.ExpectedMembers = decision.MemberCount;
            intent.State = SkirmishEnemyProductionIntentState.RequestPending;
            intent.LastRequestElapsed = objectiveElapsed;
            requests.Add(new BuildingFactionUnitProductionRequest {
                RequestOwner = session, RequestAttempt = Attempt(em, session), RequestId = intent.RequestId,
                FactionId = 2, UnitId = UnitKey(intent.Role), Status = BuildingFactionUnitProductionRequest.Pending
            });
            em.SetComponentData(session, intent);return intent.State;
        }

        // A pure feasibility query for policy terminal-intent retry decisions.
        // Income or a new plan score alone cannot bypass real capacity/queue locks.
        public static bool CanDispatchNow(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            if (em == default || !em.Exists(session) || !em.HasComponent<SkirmishObjectiveClockComponent>(session)) return false;
            float elapsed = em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
            if (!TryActive(em, session, elapsed, out _) || !TryRequestBuffer(em, out _)) return false;
            return EvaluateDispatch(em,session,role).Accepted;
        }

        static SkirmishProductionDecision EvaluateDispatch(EntityManager em, Entity session, SkirmishRoleKind role)
        {
            // Use the authoritative attempt profile, even if the caller supplied a broader army.
            var setup = em.HasComponent<SkirmishResolvedSetupRecord>(session)
                ? em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup : null;
            var actualArmy = setup == null ? null : SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            var decision = SkirmishProductionService.EvaluateQueue(em, session,
                SkirmishRoleIds.ToId(role), 1, actualArmy, 2);
            if (decision.Accepted && SkirmishProductionService.IsProducerLocked(em, session, decision.Producer, 2))
            { decision.Accepted = false; decision.Reason = SkirmishReasonCode.QueueLocked; decision.Field = "queue"; }
            if (decision.Accepted && ResearchBlocks(em, session, decision.Producer))
            { decision.Accepted = false; decision.Reason = SkirmishReasonCode.QueueLocked; decision.Field = "research"; }
            return decision;
        }

        // Called ONLY after the actual shared queue appended its native PendingProduction.
        // Parameters are that item's actual receipt, not a reservation scan or synthesized id.
        public static bool RecordAcceptedDispatch(EntityManager em,
            in BuildingFactionUnitProductionRequest request, SkirmishRoleKind actualRole,
            Entity receiptOwner, FixedString64Bytes receiptAttempt, uint receiptId)
        {
            if (request.FactionId != 2 || receiptOwner != request.RequestOwner ||
                !receiptAttempt.Equals(request.RequestAttempt) || receiptId == 0 ||
                !em.Exists(receiptOwner) || !em.HasComponent<SkirmishEnemyProductionIntentV1Component>(receiptOwner)) return false;
            var intent = em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(receiptOwner);
            if (intent.Version != 1 || intent.State != SkirmishEnemyProductionIntentState.RequestPending ||
                intent.Role != actualRole || !Matches(em, receiptOwner, in intent, request)) return false;
            if (!FindReceipt(em, receiptOwner, receiptId, out var receipt) || receipt.FactionId != 2 ||
                receipt.Role != actualRole || receipt.MemberCount != intent.ExpectedMembers ||
                receipt.ProducerRuntimeId <= 0 || (receipt.Phase != SkirmishReservationPhase.Reserved &&
                receipt.Phase != SkirmishReservationPhase.Producing))
            { Abandon(ref intent, "native_receipt_correlation_mismatch");em.SetComponentData(receiptOwner,intent);return false; }
            intent.ReservationId = receiptId;intent.State = SkirmishEnemyProductionIntentState.AcceptedWaiting;
            intent.LastObservedReservationPhase = (byte)receipt.Phase;
            intent.IdenticalRejectCount = 0;intent.RejectDomain = 0;intent.LastRejectCode = 0;intent.LastRejectField = default;
            intent.ProducerStableObjectId = ProducerStableId(em, receiptAttempt, receipt.ProducerRuntimeId);
            em.SetComponentData(receiptOwner, intent);return true;
        }

        public static int ReadDeliveredAliveCount(EntityManager em, Entity session,
            in SkirmishEnemyProductionIntentV1Component intent)
        {
            if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session) || intent.ReservationId == 0) return 0;
            if (!FindReceipt(em,session,intent.ReservationId,out var receipt) || receipt.FactionId != 2 ||
                receipt.Role != intent.Role || receipt.ProductionGroupId == 0) return 0;
            var attempt = Attempt(em,session);int count=0;var seen=new HashSet<FixedString64Bytes>();
            using var query=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),ComponentType.ReadOnly<SkirmishUnitRoleComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState });
            using var actors=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)
            {
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                if(!owner.SessionId.Equals(attempt)||owner.FactionId!=2||owner.ReservationId!=intent.ReservationId||
                    owner.IsStructure!=0||owner.CapacityReleased!=0||role.Role!=intent.Role||
                    !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor)||
                    !em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||
                    em.HasComponent<UnitSpawnTransitTag>(actor)||em.HasComponent<UnitDeathAnimationComponent>(actor))continue;
                var membership=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor);
                if(membership.GroupId!=receipt.ProductionGroupId||membership.Domain!=receipt.Category||role.Category!=receipt.Category)continue;
                if(owner.StableObjectId.IsEmpty||!seen.Add(owner.StableObjectId))return -1;
                count++;
            }
            return count;
        }

        static void ReconcileAccepted(EntityManager em,Entity session,ref SkirmishEnemyProductionIntentV1Component intent)
        {
            if(!FindReceipt(em,session,intent.ReservationId,out var receipt)||receipt.FactionId!=2||
                receipt.Role!=intent.Role||receipt.MemberCount!=intent.ExpectedMembers)
            { Abandon(ref intent,"accepted_receipt_missing_or_mismatched");return; }
            intent.LastObservedReservationPhase=(byte)receipt.Phase;
            if(receipt.Phase==SkirmishReservationPhase.Cancelled||receipt.Phase==SkirmishReservationPhase.Lost||receipt.Phase==SkirmishReservationPhase.Released)
            { intent.State=SkirmishEnemyProductionIntentState.Lost;return; }
            if(receipt.Phase==SkirmishReservationPhase.Reserved||receipt.Phase==SkirmishReservationPhase.Producing)
            { intent.State=SkirmishEnemyProductionIntentState.AcceptedWaiting;return; }
            if(receipt.Phase!=SkirmishReservationPhase.Live)
            { Abandon(ref intent,"accepted_receipt_invalid_phase");return; }
            int live=ReadDeliveredAliveCount(em,session,in intent);
            if(live<0||receipt.DeliveredMembers!=intent.ExpectedMembers||live>intent.ExpectedMembers)
            { Abandon(ref intent,"delivered_cohort_identity_mismatch");return; }
            // Native adoption may be Live before transport removes UnitSpawnTransitTag.
            // No ability/attack flag, inventory entry or native queue acknowledgement proves arrival.
            if(receipt.RemainingMembers<intent.ExpectedMembers)
            { intent.State=SkirmishEnemyProductionIntentState.Lost;return; }
            intent.State=live==intent.ExpectedMembers?SkirmishEnemyProductionIntentState.Delivered:
                SkirmishEnemyProductionIntentState.AcceptedWaiting;
        }

        static void ConsumeExactCompletedRequest(EntityManager em,Entity session,in SkirmishEnemyProductionIntentV1Component intent)
        {
            if(!TryRequestBuffer(em,out var boundary))return;
            var requests=em.GetBuffer<BuildingFactionUnitProductionRequest>(boundary);
            for(int i=requests.Length-1;i>=0;i--)
                if(Matches(em,session,in intent,requests[i])&&requests[i].Status!=BuildingFactionUnitProductionRequest.Pending)requests.RemoveAt(i);
        }
        static bool Matches(EntityManager em,Entity session,in SkirmishEnemyProductionIntentV1Component intent,in BuildingFactionUnitProductionRequest request) =>
            em.Exists(session)&&em.HasComponent<SkirmishExpandedSessionComponent>(session)&&request.RequestOwner==session&&
            request.RequestAttempt.Equals(Attempt(em,session))&&request.FactionId==2&&request.RequestId==intent.RequestId&&
            request.UnitId.Equals(UnitKey(intent.Role));
        static FixedString128Bytes UnitKey(SkirmishRoleKind role) =>
            new FixedString128Bytes(SpawnableKeyText.Normalize(SkirmishRoleCatalogConfig.RuntimePrefabKey(role)));
        static bool TryExpectedMembers(EntityManager em,Entity session,SkirmishRoleKind role,out int members)
        {
            members=0;if(!em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(setup==null||!SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,role,out var overlay))return false;
            members=overlay.Producer==SkirmishProducerKind.Barracks?SkirmishRoleIds.InfantrySquadMembers:Math.Max(1,overlay.SquadMembers);
            return true;
        }
        static FixedString64Bytes Attempt(EntityManager em,Entity session) => em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
        static bool TryRequestBuffer(EntityManager em,out Entity boundary)
        { using var q=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingFactionUnitProductionRequest));boundary=q.CalculateEntityCount()==1?q.GetSingletonEntity():Entity.Null;return boundary!=Entity.Null; }
        static bool FindReceipt(EntityManager em,Entity session,uint id,out SkirmishProductionReservation receipt)
        {
            receipt=default;if(!em.HasBuffer<SkirmishProductionReservation>(session))return false;int matches=0;
            foreach(var item in em.GetBuffer<SkirmishProductionReservation>(session))if(item.ReservationId==id){receipt=item;matches++;}
            return matches==1;
        }
        static FixedString64Bytes ProducerStableId(EntityManager em,FixedString64Bytes attempt,int runtimeId)
        {
            FixedString64Bytes result=default;using var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(RuntimeBuildingCombatInfo));using var actors=q.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors){var o=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var info=em.GetComponentData<RuntimeBuildingCombatInfo>(actor);if(o.SessionId.Equals(attempt)&&o.FactionId==2&&info.RuntimeBuildingId==runtimeId){if(!result.IsEmpty)return default;result=o.StableObjectId;}}
            return result;
        }
        static bool ResearchBlocks(EntityManager em,Entity session,SkirmishProducerKind producer)
        { if(!em.HasBuffer<SkirmishResearchQueueItem>(session))return false;foreach(var r in em.GetBuffer<SkirmishResearchQueueItem>(session))if(r.FactionId==2&&r.Producer==producer&&r.Phase==SkirmishResearchPhase.Researching)return true;return false; }
        static void Reject(ref SkirmishEnemyProductionIntentV1Component intent,SkirmishEnemyRejectDomain domain,ushort code,string field,float now)
        {
            var key=new FixedString64Bytes(field??string.Empty);
            bool same=intent.RejectDomain==(byte)domain&&intent.LastRejectCode==code&&intent.LastRejectField.Equals(key);
            intent.IdenticalRejectCount=(byte)(same?Math.Min(3,intent.IdenticalRejectCount+1):1);
            intent.RejectDomain=(byte)domain;intent.LastRejectCode=code;intent.LastRejectField=key;
            intent.State=intent.IdenticalRejectCount>=3?SkirmishEnemyProductionIntentState.Abandoned:SkirmishEnemyProductionIntentState.Backoff;
            intent.RetryNotBeforeElapsed=now+Math.Min(8,1<<(intent.IdenticalRejectCount-1));
        }
        static void Abandon(ref SkirmishEnemyProductionIntentV1Component intent,string field)
        { intent.State=SkirmishEnemyProductionIntentState.Abandoned;intent.LastRejectField=new FixedString64Bytes(field); }
        static SkirmishEnemyProductionIntentState ReadState(EntityManager em,Entity session) =>
            em!=default&&em.Exists(session)&&em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session)?em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session).State:SkirmishEnemyProductionIntentState.Idle;
        static bool TryActive(EntityManager em,Entity session,float elapsed,out SkirmishEnemyProductionIntentV1Component intent)
        {
            intent=default;if(em==default||!em.Exists(session)||!em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)||!em.HasComponent<SkirmishObjectiveClockComponent>(session)||
                float.IsNaN(elapsed)||float.IsInfinity(elapsed)||elapsed<0)return false;
            intent=em.GetComponentData<SkirmishEnemyProductionIntentV1Component>(session);
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            if(intent.Version!=1||state.IsLegacy!=0||state.Phase!=SkirmishSessionPhase.Playing||state.SpawnComplete==0||
                state.SessionId.IsEmpty||clock.Paused!=0||clock.Playing==0||Math.Abs(clock.ElapsedSeconds-elapsed)>.001f)return false;
            using var q=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));return q.IsEmptyIgnoreFilter||
                (q.CalculateEntityCount()==1&&q.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive!=0);
        }
    }
}
