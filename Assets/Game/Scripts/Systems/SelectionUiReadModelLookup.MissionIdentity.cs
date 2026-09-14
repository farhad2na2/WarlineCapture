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
        public static bool IsRescueSpecialist(EntityManager em,Entity entity) =>
            em.HasComponent<CampaignMissionUnitRoleComponent>(entity) &&
            em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals(RescueSpecialistRole);
        internal static string ResolveGroupTitle(
            int unitCount,
            int soldierCount,
            int vehicleCount,
            int aircraftCount,
            int transportCount,
            int buildingCount, int specialistCount)
        {
            if (unitCount <= 0)
                return buildingCount == 1 ? GameText.Get("selection.title.one_structure", "1 STRUCTURE") : GameText.Get("selection.title.no_selection", "NO SELECTION");
            if (buildingCount > 0)
                return GameText.Get("selection.title.mixed_selection", "MIXED SELECTION");
            if (specialistCount == unitCount)
                return GameText.Format("mission.m04.specialist.group", "{0} SPECIALISTS", unitCount);
            if (soldierCount == unitCount)
                return unitCount == 1
                    ? GameText.Get("selection.title.one_soldier", "1 SOLDIER")
                    : GameText.Format("selection.title.soldiers", "{0} SOLDIERS", unitCount);
            if (transportCount == unitCount)
                return unitCount == 1
                    ? GameText.Get("selection.title.one_transport", "1 TRANSPORT")
                    : GameText.Format("selection.title.transports", "{0} TRANSPORTS", unitCount);
            if (aircraftCount == unitCount)
                return unitCount == 1
                    ? GameText.Get("selection.title.one_aircraft", "1 AIRCRAFT")
                    : GameText.Format("selection.title.aircraft", "{0} AIRCRAFT", unitCount);
            if (vehicleCount == unitCount)
                return unitCount == 1
                    ? GameText.Get("selection.title.one_vehicle", "1 VEHICLE")
                    : GameText.Format("selection.title.vehicles", "{0} VEHICLES", unitCount);
            if (aircraftCount > 0 && soldierCount + vehicleCount + transportCount > 0)
                return GameText.Get("selection.title.mixed_force", "MIXED FORCE");

            return GameText.Get("selection.title.mixed_squad", "MIXED SQUAD");
        }

        internal static string ResolveGroupSubtitle(
            int unitCount,
            int soldierCount,
            int vehicleCount,
            int aircraftCount,
            int transportCount,
            int buildingCount, int specialistCount)
        {
            if (unitCount <= 0)
                return buildingCount > 0 ? GameText.Get("selection.subtitle.building_group", "Building Group") : string.Empty;
            if (buildingCount > 0)
                return GameText.Format("selection.subtitle.units_structures", "{0} Units / {1} Structure", unitCount, buildingCount);
            if (specialistCount == unitCount)
                return GameText.Get("mission.m04.specialist.role", "Rescue passenger");
            if (soldierCount == unitCount)
                return GameText.Get("selection.subtitle.infantry_squad", "Infantry Squad");
            if (transportCount == unitCount)
                return GameText.Get("selection.subtitle.transport_group", "Transport Group");
            if (aircraftCount == unitCount)
                return GameText.Get("selection.subtitle.air_wing", "Air Wing");
            if (vehicleCount == unitCount)
                return GameText.Get("selection.subtitle.vehicle_squad", "Vehicle Squad");

            int groundCount = soldierCount + vehicleCount + transportCount;
            if (aircraftCount > 0 && groundCount > 0)
                return GameText.Format("selection.subtitle.ground_air", "{0} Ground / {1} Air", groundCount, aircraftCount);
            if (soldierCount > 0 && vehicleCount + transportCount > 0)
                return GameText.Format("selection.subtitle.infantry_vehicles", "{0} Infantry / {1} Vehicles", soldierCount, vehicleCount + transportCount);

            return GameText.Format("selection.subtitle.selected_units", "{0} Selected Units", unitCount);
        }

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
