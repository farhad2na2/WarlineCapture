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
    public sealed partial class SelectionUiReadModelLookup
    {
        public bool CanAttack(EntityManager entityManager, Entity entity)
        {
            return entityManager.Exists(entity) &&
                   entityManager.HasComponent<UnitCombat>(entity) &&
                   entityManager.GetComponentData<UnitCombat>(entity).CanAttack != 0;
        }

        public bool CanHoldPosition(EntityManager entityManager, Entity entity, out TacticalCommandReasonCode reason)
        {
            return CanAcceptImmediateSelectedUnitCommand(entityManager, entity, out reason);
        }

        public bool CanStop(EntityManager entityManager, Entity entity, out TacticalCommandReasonCode reason)
        {
            return CanAcceptImmediateSelectedUnitCommand(entityManager, entity, out reason);
        }

        public bool CanScan(EntityManager entityManager, Entity entity, out TacticalCommandReasonCode reason)
        {
            if (!CanAcceptImmediateSelectedUnitCommand(entityManager, entity, out reason))
                return false;

            if (IsSelectedUnitScanCapable(entityManager, entity))
            {
                reason = TacticalCommandReasonCode.None;
                return true;
            }

            reason = TacticalCommandReasonCode.ScanUnavailable;
            return false;
        }

        internal static bool CanAcceptImmediateSelectedUnitCommand(
            EntityManager entityManager,
            Entity entity,
            out TacticalCommandReasonCode reason)
        {
            if (!CanAcceptLivingOwnedUnit(entityManager, entity, out reason))
                return false;

            if (!entityManager.HasComponent<UnitMove>(entity) ||
                entityManager.HasComponent<UnitTransportPassenger>(entity))
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            reason = TacticalCommandReasonCode.None;
            return true;
        }

        private static bool CanAcceptLivingOwnedUnit(
            EntityManager entityManager,
            Entity entity,
            out TacticalCommandReasonCode reason)
        {
            if (entity == Entity.Null || !entityManager.Exists(entity))
            {
                reason = TacticalCommandReasonCode.NoSelection;
                return false;
            }

            if (!entityManager.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) ||
                entityManager.HasComponent<Disabled>(entity) ||
                entityManager.HasComponent<UnitDeathAnimationComponent>(entity))
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            if (entityManager.HasComponent<UnitHealth>(entity) &&
                entityManager.GetComponentData<UnitHealth>(entity).Current <= 0)
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            reason = TacticalCommandReasonCode.None;
            return true;
        }

        public static bool IsSelectedUnitScanCapable(EntityManager entityManager, Entity entity)
        {
            return entityManager.Exists(entity) &&
                   entityManager.HasComponent<UnitCombat>(entity) &&
                   entityManager.GetComponentData<UnitCombat>(entity).CanAttack != 0;
        }

        public static bool IsSelectedUnitScanSpecialist(EntityManager entityManager, Entity entity)
        {
            string source = ResolveScanCapabilitySource(entityManager, entity);
            if (ContainsToken(source, "Drone") ||
                ContainsToken(source, "Recon") ||
                ContainsToken(source, "Scout") ||
                ContainsToken(source, "Radar") ||
                ContainsToken(source, "Scan") ||
                ContainsToken(source, "Plane"))
            {
                return true;
            }

            return entityManager.HasComponent<UnitAirMovement>(entity) &&
                   (ContainsToken(source, "Jet") ||
                    ContainsToken(source, "Drone") ||
                    ContainsToken(source, "Aircraft") ||
                    ContainsToken(source, "Air"));
        }

        private static string ResolveScanCapabilitySource(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<UnitSourcePrefabKey>(entity))
            {
                string sourceKey = entityManager.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (!string.IsNullOrWhiteSpace(sourceKey))
                    return sourceKey;
            }

            if (entityManager.HasComponent<UnitDisplayInfo>(entity))
            {
                UnitDisplayInfo displayInfo = entityManager.GetComponentData<UnitDisplayInfo>(entity);
                string displayName = displayInfo.Name.ToString();
                string displayDescription = displayInfo.Description.ToString();
                return $"{displayName} {displayDescription}";
            }

            return entityManager.GetName(entity).ToString();
        }

    }
}
