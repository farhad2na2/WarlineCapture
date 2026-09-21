using System;

namespace Game.Operations.Contracts
{
    public enum OperationsRosterRoleKind : byte
    {
        None = 0,
        RifleInfantry = 1,
        ReconInfantry = 2,
        SupportInfantry = 3,
        RepairSpecialist = 4,
        AntiArmorInfantry = 5,
        AntiAirInfantry = 6,
        Apc = 7,
        Tank = 8,
        TransportHelicopter = 9,
        LightVehicle = 10,
        CargoTruck = 11,
        Civilian = 12
    }

    public enum OperationsAvailabilityKind : byte
    {
        Missing = 0,
        PresentInSource = 1,
        CampaignBound = 2,
        SemanticOnly = 3
    }

    public readonly struct OperationsRosterRoleRecord
    {
        public OperationsRosterRoleRecord(
            OperationsRosterRoleKind role,
            string semanticId,
            OperationsAvailabilityKind availability,
            string verifiedTypeOrPath,
            string notes)
        {
            if (role == OperationsRosterRoleKind.None)
                throw new ArgumentOutOfRangeException(nameof(role));
            OperationsContractText.RequireToken(semanticId, nameof(semanticId));
            OperationsContractText.RequireEvidence(verifiedTypeOrPath, nameof(verifiedTypeOrPath));
            OperationsContractText.RequireEvidence(notes, nameof(notes));
            Role = role;
            SemanticId = semanticId;
            Availability = availability;
            VerifiedTypeOrPath = verifiedTypeOrPath;
            Notes = notes;
        }

        public OperationsRosterRoleKind Role { get; }
        public string SemanticId { get; }
        public OperationsAvailabilityKind Availability { get; }
        public string VerifiedTypeOrPath { get; }
        public string Notes { get; }

        public bool IsResolvedForAuthoring =>
            Availability is OperationsAvailabilityKind.CampaignBound or OperationsAvailabilityKind.PresentInSource;
    }

    public readonly struct OperationsFeatureRecord
    {
        public OperationsFeatureRecord(
            string featureId,
            OperationsAvailabilityKind availability,
            string verifiedTypeOrPath,
            string notes)
        {
            OperationsContractText.RequireId(featureId, nameof(featureId), OperationsIdentityRules.IsValidFeatureId);
            OperationsContractText.RequireEvidence(verifiedTypeOrPath, nameof(verifiedTypeOrPath));
            OperationsContractText.RequireEvidence(notes, nameof(notes));
            FeatureId = featureId;
            Availability = availability;
            VerifiedTypeOrPath = verifiedTypeOrPath;
            Notes = notes;
        }

        public string FeatureId { get; }
        public OperationsAvailabilityKind Availability { get; }
        public string VerifiedTypeOrPath { get; }
        public string Notes { get; }
    }

    public static class OperationsRosterLedger
    {
        public static readonly OperationsRosterRoleRecord[] Roles =
        {
            new(
                OperationsRosterRoleKind.RifleInfantry,
                "role.operations.rifle_infantry",
                OperationsAvailabilityKind.CampaignBound,
                "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab + unit.jrc.rifle_* in M01FirstContactConfigBuilder",
                "Verified Campaign M01/M02 spawn keys and prefab files. Semantic Operations role still needs a dedicated roster bind."),
            new(
                OperationsRosterRoleKind.ReconInfantry,
                "role.operations.recon_infantry",
                OperationsAvailabilityKind.SemanticOnly,
                "No dedicated recon-infantry prefab or unit.jrc.recon_* key found",
                "Unit_Veh_Drone and Unit_Chr_Ghillie_Male_01 exist but are not authored as recon infantry. Do not infer capability from mesh names."),
            new(
                OperationsRosterRoleKind.SupportInfantry,
                "role.operations.support_infantry",
                OperationsAvailabilityKind.SemanticOnly,
                "No dedicated support-infantry unitConfigKey found",
                "Campaign uses rifle squads for command groups. Support is a planned Operations role, not an existing type."),
            new(
                OperationsRosterRoleKind.RepairSpecialist,
                "role.operations.repair_specialist",
                OperationsAvailabilityKind.Missing,
                "No TacticalCommandMode.Repair and no repair-specialist prefab",
                "Building/UI repair labels exist. There is no verified tactical repair interaction or specialist roster key."),
            new(
                OperationsRosterRoleKind.AntiArmorInfantry,
                "role.operations.anti_armor_infantry",
                OperationsAvailabilityKind.PresentInSource,
                "Assets/Game/Prefabs/Characters/Unit_Chr_Ghillie_Male_01.prefab",
                "M03 guide copy names this Ghillie Rocketeer. Present as a prefab; not yet bound as an Operations role."),
            new(
                OperationsRosterRoleKind.AntiAirInfantry,
                "role.operations.anti_air_infantry",
                OperationsAvailabilityKind.Missing,
                "No infantry AA prefab; Unit_Veh_Missle_Launcher_Air is a vehicle",
                "Do not substitute the ground-to-air launcher vehicle for authored anti-air infantry."),
            new(
                OperationsRosterRoleKind.Apc,
                "role.operations.apc",
                OperationsAvailabilityKind.CampaignBound,
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Fast.prefab used as role.friendly.rescue_apc in M04AirliftConfigBuilder",
                "APC Fast/Heavy/Slow prefabs exist. Operations must bind a certified key, not invent one."),
            new(
                OperationsRosterRoleKind.Tank,
                "role.operations.tank",
                OperationsAvailabilityKind.PresentInSource,
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab + SkirmishStressRecipe.TankPrefab",
                "Prefab exists and is used by Skirmish stress and M03 class copy. Not Campaign-bound as an Operations role."),
            new(
                OperationsRosterRoleKind.TransportHelicopter,
                "role.operations.transport_helicopter",
                OperationsAvailabilityKind.CampaignBound,
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab used as role.friendly.airlift in M04AirliftConfigBuilder",
                "Shared airlift extraction exists as Campaign M04. Operations must extract generic facts rather than reuse M04 mission IDs."),
            new(
                OperationsRosterRoleKind.LightVehicle,
                "role.operations.light_vehicle",
                OperationsAvailabilityKind.PresentInSource,
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab",
                "Present as a prefab and Skirmish stress line. No Operations role bind yet."),
            new(
                OperationsRosterRoleKind.CargoTruck,
                "role.operations.cargo_truck",
                OperationsAvailabilityKind.PresentInSource,
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab and Unit_Veh_Truck_Canopy.prefab",
                "Logistics trucks exist. Hostile cargo-truck escort/interdiction identities are not authored."),
            new(
                OperationsRosterRoleKind.Civilian,
                "role.operations.civilian",
                OperationsAvailabilityKind.CampaignBound,
                "Assets/Game/Prefabs/Characters/Unit_Chr_Civilian_*.prefab + role.civilian.protected + FactionIdentity.NeutralFactionId=0",
                "Civilians are present and M04 uses civilian prefabs. Neutral escort/follow is not a verified shared mechanic.")
        };

        public static readonly OperationsFeatureRecord[] Features =
        {
            new(
                "feature.operation_map",
                OperationsAvailabilityKind.CampaignBound,
                "Game.Configs.MissionDefinitionConfig.RequiredFeatureIds / OperationMapIdentityRules",
                "Existing Campaign feature ID. Shared map IDs currently reject the operations namespace."),
            new(
                "feature.unit_catalog",
                OperationsAvailabilityKind.CampaignBound,
                "Game.Configs.MissionDefinitionConfig.RequireUnitCatalogReady",
                "Existing Campaign feature ID. Operations still needs a dedicated roster manifest."),
            new(
                "feature.tactical_commands",
                OperationsAvailabilityKind.CampaignBound,
                "Game.Tactical.Contracts.TacticalCommandMode",
                "Select/Move/Attack/Hold/Stop/Scan/Board exist. Repair/Interact/Escort commands do not."),
            new(
                "feature.tactical.scan",
                OperationsAvailabilityKind.PresentInSource,
                "Game.Tactical.Contracts.TacticalCommandMode.Scan + ScanIntelCommandSystem",
                "Scan command exists. Operations SCAN objectives still need site-binding and duration rules."),
            new(
                "feature.tactical.hold",
                OperationsAvailabilityKind.PresentInSource,
                "Game.Tactical.Contracts.TacticalCommandMode.Hold",
                "Hold command exists. Operations HOLD-zone capture is a new objective rule."),
            new(
                "feature.tactical.board",
                OperationsAvailabilityKind.CampaignBound,
                "Game.Tactical.Contracts.TacticalCommandMode.Board + M04 extraction",
                "Boarding exists. Operations AIRLIFT/EXTRACT must not call Campaign settlement."),
            new(
                "feature.tactical.repair",
                OperationsAvailabilityKind.Missing,
                "No TacticalCommandMode.Repair",
                "Required by REPAIR family. Blocker for P2/P4 until a shared interaction exists."),
            new(
                "feature.tactical.interact",
                OperationsAvailabilityKind.Missing,
                "No TacticalCommandMode.Interact",
                "Required by evidence/scan-site channel interactions."),
            new(
                "feature.operations.civilian_escort",
                OperationsAvailabilityKind.Missing,
                "No OperationsCivilianEscortOrderSystem",
                "MISSION_IMPLEMENTATION names this as a new shared mechanic."),
            new(
                "feature.operations.evidence_carry",
                OperationsAvailabilityKind.Missing,
                "No OperationsEvidenceCarrySystem",
                "MISSION_IMPLEMENTATION names this as a new shared mechanic."),
            new(
                "feature.operations.partial_outcome",
                OperationsAvailabilityKind.Missing,
                "Game.Missions.Contracts.MissionOutcomeKind is Victory/Defeat only",
                "Campaign outcomes cannot express Partial/Withdrawn/TechnicalFailure."),
            new(
                "feature.operations.run_state",
                OperationsAvailabilityKind.Missing,
                "PlayerProfileSaveData has no operations envelope",
                "SaveDataModel is a shared seam; P0 ships an Operations-owned DTO only."),
            new(
                "feature.operations.id_namespace",
                OperationsAvailabilityKind.Missing,
                "Game.Configs.OperationMapIdentityRules.IsValidOperationMapId / IsValidScenarioId",
                "Verified on main: operations IDs are rejected. Shared-validator extension is a Game PM blocker.")
        };

        public static string[] BaselineRequiredFeatureIds => new[]
        {
            "feature.operation_map",
            "feature.unit_catalog",
            "feature.tactical_commands",
            "feature.operations.id_namespace"
        };

        public static bool TryGetRole(OperationsRosterRoleKind role, out OperationsRosterRoleRecord record)
        {
            for (int index = 0; index < Roles.Length; index++)
            {
                if (Roles[index].Role == role)
                {
                    record = Roles[index];
                    return true;
                }
            }

            record = default;
            return false;
        }

        public static int CountResolvedRoles()
        {
            int count = 0;
            for (int index = 0; index < Roles.Length; index++)
                if (Roles[index].IsResolvedForAuthoring)
                    count++;
            return count;
        }
    }
}
