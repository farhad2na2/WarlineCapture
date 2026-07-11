using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public partial struct TransportBoardingCommandSystem
    {
        private static string ResolveSourceName(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity))
                return string.Empty;

            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (!string.IsNullOrWhiteSpace(sourceName))
                    return sourceName;
            }

            return em.GetName(entity);
        }

        private static bool IsRopeDisembarkTransport(EntityManager em, Entity transport)
        {
            if (!em.Exists(transport) || !em.HasComponent<UnitAirMovement>(transport))
                return false;

            string sourceName = ResolveSourceName(em, transport);
            return sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool StartRopeDisembarkTransport(
            EntityManager em,
            Entity transport,
            int2 referenceCell,
            UnitMoveOrderSystem moveOrderSystem,
            int totalDropCount = 0)
        {
            if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
                return false;

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            if (passengers.Length <= 0)
                return false;

            UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, transport);
            if (em.HasComponent<UnitAirMovement>(transport) &&
                em.HasComponent<UnitAirComponent>(transport) &&
                em.HasComponent<LocalTransform>(transport))
            {
                UnitAirMovement airMovement = em.GetComponentData<UnitAirMovement>(transport);
                UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
                LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
                float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
                if (airState.Airborne == 0)
                {
                    transform.Position.y = groundY + math.max(RopeDisembarkMinimumTakeoffHeight, airMovement.CruiseHeight);
                    em.SetComponentData(transport, transform);
                }

                airState.ReturningHome = 0;
                airState.Airborne = 1;
                airState.TakeoffRolling = 0;
                airState.LandingRolling = 0;
                airState.AttackRunActive = 0;
                airState.ReturnApproachInitialized = 0;
                em.SetComponentData(transport, airState);
            }

            UnitTransportRopeDisembarkRequest request = new()
            {
                ReferenceCell = referenceCell,
                NextDropAt = 0f,
                DropIntervalSeconds = RopeDisembarkDropIntervalSeconds,
                TotalDropCount = math.max(0, totalDropCount)
            };

            if (em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
                em.SetComponentData(transport, request);
            else
                em.AddComponentData(transport, request);

            return true;
        }

        private static void RequestPlaneDoorOpen(EntityManager em, Entity transport)
        {
            if (!em.Exists(transport) || !em.HasComponent<UnitTransportPlaneDoorState>(transport))
                return;

            var request = new UnitTransportPlaneDoorOpenRequest { RemainingSeconds = PlaneDoorOpenSeconds };
            if (em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport))
                em.SetComponentData(transport, request);
            else
                em.AddComponentData(transport, request);
        }

        private static DisembarkResult TryStartPlaneAirdrop(
            EntityManager em,
            Entity transport,
            DynamicBuffer<UnitTransportPassengerElement> passengers,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            int2 fallbackReferenceCell,
            int2 requestedDropCell,
            byte hasRequestedDropCell,
            int maxDropCount)
        {
            if (!CanStartPlaneAirdrop(em, transport, out TacticalCommandReasonCode reasonCode))
                return DisembarkResult.Rejected(reasonCode);

            int dropCount = math.min(maxDropCount, passengers.Length);
            if (dropCount <= 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

            int2 dropReferenceCell = hasRequestedDropCell != 0 ? requestedDropCell : fallbackReferenceCell;
            if (!TryValidateAirdropReferenceCell(grid, walkable, dropReferenceCell, out TacticalCommandReasonCode dropCellReason))
                return DisembarkResult.Rejected(dropCellReason, message: ResolveAirdropRejectedMessage(dropCellReason));

            TransportBoardingCapacitySystemHelper.CountLoadedPassengerKinds(
                em,
                transport,
                passengers,
                dropCount,
                out int soldierDropCount,
                out int vehicleDropCount);
            if (soldierDropCount <= 0 && vehicleDropCount <= 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

            if (!TryValidatePlaneAirdropPassengers(
                    em,
                    transport,
                    passengers,
                    dropCount,
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    dropReferenceCell,
                    out TacticalCommandReasonCode airdropReason,
                    out string airdropMessage))
            {
                return DisembarkResult.Rejected(airdropReason, message: airdropMessage);
            }

            SetPlaneAirdropRequest(em, transport, dropReferenceCell, soldierDropCount, vehicleDropCount);
            RequestPlaneDoorOpen(em, transport);
            return DisembarkResult.Success("Airdrop in progress.");
        }

        public static bool TryIssueDeployDisembark(
            EntityManager em,
            Entity transport,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem,
            EntityQuery gridPathingQuery,
            int2 requestedDropCell,
            Entity attackTarget,
            int2 attackTargetCell,
            float3 attackTargetPosition,
            byte attackAfterDeploy,
            out TacticalCommandReasonCode reasonCode)
        {
            reasonCode = TacticalCommandReasonCode.None;
            if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                reasonCode = TacticalCommandReasonCode.InvalidTransport;
                return false;
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            List<Entity> passengerSnapshot = new(passengers.Length);
            List<Entity> attackDeployPassengers = attackAfterDeploy != 0
                ? CollectAttackDeployPassengers(em, passengers)
                : null;
            if (attackAfterDeploy != 0)
            {
                if (attackDeployPassengers == null || attackDeployPassengers.Count <= 0)
                {
                    reasonCode = TacticalCommandReasonCode.TargetNotAttackable;
                    return false;
                }

                passengerSnapshot.AddRange(attackDeployPassengers);
            }
            else
            {
                for (int i = 0; i < passengers.Length; i++)
                    passengerSnapshot.Add(passengers[i].Passenger);
            }

            DisembarkResult result = TryDisembarkTransport(
                em,
                transport,
                transportCapacitySystem,
                moveOrderSystem,
                gridPathingQuery,
                requestedDropCell,
                hasRequestedDropCell: 1,
                allowedPassengers: attackDeployPassengers);

            reasonCode = result.ReasonCode;
            if (!result.Accepted)
                return false;

            if (attackAfterDeploy != 0)
            {
                MarkDeployPassengersForAttack(
                    em,
                    passengerSnapshot,
                    attackTarget,
                    attackTargetCell,
                    attackTargetPosition);
            }

            return true;
        }

        private static void MarkDeployPassengersForAttack(
            EntityManager em,
            List<Entity> passengers,
            Entity attackTarget,
            int2 attackTargetCell,
            float3 attackTargetPosition)
        {
            if (attackTarget == Entity.Null || !em.Exists(attackTarget))
                return;

            if (em.HasComponent<LocalTransform>(attackTarget))
                attackTargetPosition = em.GetComponentData<LocalTransform>(attackTarget).Position;
            if (em.HasComponent<UnitGrid>(attackTarget))
                attackTargetCell = em.GetComponentData<UnitGrid>(attackTarget).Cell;

            UnitTransportDeployAttackTarget attack = new()
            {
                TargetEntity = attackTarget,
                TargetCell = attackTargetCell,
                TargetPosition = attackTargetPosition
            };

            for (int i = 0; i < passengers.Count; i++)
            {
                Entity passenger = passengers[i];
                if (!em.Exists(passenger))
                    continue;

                if (em.HasComponent<UnitTransportDeployAttackTarget>(passenger))
                    em.SetComponentData(passenger, attack);
                else
                    em.AddComponentData(passenger, attack);
            }
        }

        private static List<Entity> CollectAttackDeployPassengers(
            EntityManager em,
            DynamicBuffer<UnitTransportPassengerElement> passengers)
        {
            List<Entity> attackDeployPassengers = new();
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (UnitTransportAttackPayloadUtility.IsAttackDeployPassenger(em, passenger))
                    attackDeployPassengers.Add(passenger);
            }

            return attackDeployPassengers;
        }

        private static int FilterTransportPassengerBuffer(
            EntityManager em,
            DynamicBuffer<UnitTransportPassengerElement> passengers,
            List<Entity> allowedPassengers)
        {
            if (allowedPassengers == null || allowedPassengers.Count <= 0)
                return 0;

            List<Entity> allowed = new();
            List<Entity> deferred = new();
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (!em.Exists(passenger))
                    continue;

                if (ContainsEntity(allowedPassengers, passenger))
                    allowed.Add(passenger);
                else
                    deferred.Add(passenger);
            }

            passengers.Clear();
            AddPassengers(passengers, allowed);
            AddPassengers(passengers, deferred);

            return allowed.Count;
        }

        private static void AddPassengers(DynamicBuffer<UnitTransportPassengerElement> passengers, List<Entity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
                passengers.Add(new UnitTransportPassengerElement { Passenger = entities[i] });
        }

        private static bool ContainsEntity(List<Entity> entities, Entity entity)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i] == entity)
                    return true;
            }

            return false;
        }

        private static void CancelDeployAttackForManualDisembark(EntityManager em, Entity transport, Entity passenger = default)
        {
            if (!em.Exists(transport))
                return;

            ClearDeployAttackIntent(em, transport);
            if (passenger != Entity.Null)
                ClearDeployAttackIntent(em, passenger);

            if (em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
                for (int i = 0; i < passengers.Length; i++)
                    ClearDeployAttackIntent(em, passengers[i].Passenger);
            }

            if (em.HasComponent<UnitAirComponent>(transport))
            {
                UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
                airState.AttackRunActive = 0;
                airState.ReturnApproachInitialized = 0;
                if (airState.Airborne != 0)
                    airState.ReturningHome = 1;
                em.SetComponentData(transport, airState);
            }
        }

        private static void ClearDeployAttackIntent(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            if (em.HasComponent<UnitTransportDeployOrder>(entity))
                em.RemoveComponent<UnitTransportDeployOrder>(entity);
            if (em.HasComponent<UnitTransportDeployAttackTarget>(entity))
                em.RemoveComponent<UnitTransportDeployAttackTarget>(entity);
            if (em.HasBuffer<UnitTransportPassengerElement>(entity))
            {
                DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(entity);
                for (int i = 0; i < passengers.Length; i++)
                    ClearDeployAttackIntent(em, passengers[i].Passenger);
            }
        }


    }
}
