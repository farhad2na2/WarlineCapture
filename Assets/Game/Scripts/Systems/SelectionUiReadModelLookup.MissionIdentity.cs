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
        private static readonly FixedString64Bytes RescueSpecialistRole = "role.friendly.specialist";
        internal static bool IsRescueSpecialist(EntityManager em,Entity entity) =>
            em.HasComponent<CampaignMissionUnitRoleComponent>(entity) &&
            em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(RescueSpecialistRole);
        public string ResolveFocusedUnitName(EntityManager entityManager, Entity entity)
        {
            if (IsRescueSpecialist(entityManager,entity)) return Game.Configs.GameText.Get("mission.m04.specialist.name","Specialist");
            if (entityManager.HasComponent<UnitDisplayInfo>(entity))
            {
                string configuredName = entityManager.GetComponentData<UnitDisplayInfo>(entity).Name.ToString();
                if (!string.IsNullOrWhiteSpace(configuredName))
                    return configuredName;
            }

            byte factionId = entityManager.HasComponent<Faction>(entity)
                ? entityManager.GetComponentData<Faction>(entity).Id
                : FactionIdentity.NeutralFactionId;
            if (!entityManager.HasComponent<UnitMove>(entity))
                return FactionIdentity.IsPlayerControlled(factionId) ? "Player Unit" : "Enemy Unit";

            bool isVehicle = IsVehicleUnit(entityManager, entity);
            if (!isVehicle)
                return FactionIdentity.IsPlayerControlled(factionId) ? "Soldier" : "Enemy Soldier";

            bool canAttack = entityManager.HasComponent<UnitCombat>(entity) && entityManager.GetComponentData<UnitCombat>(entity).CanAttack != 0;
            if (canAttack)
                return FactionIdentity.IsPlayerControlled(factionId) ? "Heavy APC" : "Enemy Heavy APC";

            float speed = entityManager.HasComponent<UnitMove>(entity) ? entityManager.GetComponentData<UnitMove>(entity).Speed : 0f;
            if (speed >= 10.5f)
                return FactionIdentity.IsPlayerControlled(factionId) ? "APC 02" : "Enemy APC 02";

            return FactionIdentity.IsPlayerControlled(factionId) ? "APC 01" : "Enemy APC 01";
        }

        public string ResolveFocusedUnitDescription(EntityManager entityManager, Entity entity)
        {
            if (IsRescueSpecialist(entityManager,entity)) return Game.Configs.GameText.Get("mission.m04.specialist.description","Rescue passenger. Keep all four specialists safe.");
            if (entityManager.HasComponent<UnitDisplayInfo>(entity))
            {
                string configuredDescription = entityManager.GetComponentData<UnitDisplayInfo>(entity).Description.ToString();
                if (!string.IsNullOrWhiteSpace(configuredDescription))
                    return configuredDescription;
            }

            byte factionId = entityManager.HasComponent<Faction>(entity)
                ? entityManager.GetComponentData<Faction>(entity).Id
                : FactionIdentity.NeutralFactionId;
            bool movable = entityManager.HasComponent<UnitMove>(entity);
            bool isVehicle = IsVehicleForVisibleSelection(entityManager, entity);
            bool canAttack = entityManager.HasComponent<UnitCombat>(entity) && entityManager.GetComponentData<UnitCombat>(entity).CanAttack != 0;

            if (FactionIdentity.IsPlayerControlled(factionId))
            {
                if (!movable)
                    return "Player-controlled unit.";
                if (isVehicle)
                    return canAttack
                        ? "Heavy combat APC. Faster than the base APC and can attack enemies."
                        : "Support APC vehicle. Mobile but cannot attack, and will retreat when attacked.";

                return "Player soldier. Click ground to issue a move order.";
            }

            if (!movable)
                return "Enemy unit. Read-only info.";
            if (isVehicle)
                return canAttack ? "Enemy combat vehicle." : "Enemy support vehicle.";
            return "Enemy mobile unit. Read-only info.";
        }

    }
}
