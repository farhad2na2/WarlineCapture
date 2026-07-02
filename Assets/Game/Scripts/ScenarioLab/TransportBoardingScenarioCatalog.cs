using System;
using System.Collections.Generic;

namespace Game.Runtime
{
    public enum TransportBoardingScenarioKind
    {
        GroundVehicleBoardAndExit = 0,
        HelicopterBoardAndRopeExit = 1,
        HelicopterAirPickup = 2,
        HelicopterGroundExitAudit = 3,
        PlaneRampBoardAndExit = 4,
        PlaneSoldierAirdrop = 5,
        PlaneVehicleCargoGroundExit = 6,
        PlaneVehicleCargoAirdrop = 7,
        PlaneMixedLoadAirdrop = 8,
        RejectionCases = 9,
        NextCleanup = 10,
        CameraProofPath = 11
    }

    public enum TransportBoardingScenarioVisualMode
    {
        GroundVehicle = 0,
        Helicopter = 1,
        Plane = 2,
        NegativeCase = 3,
        Cleanup = 4,
        CameraProof = 5
    }

    public enum TransportBoardingScenarioExitMode
    {
        Ground = 0,
        Rope = 1,
        Parachute = 2,
        CargoDrop = 3,
        MixedAirdrop = 4,
        AuditOnly = 5,
        NegativeOnly = 6
    }

    public readonly struct TransportBoardingScenarioDescriptor
    {
        public TransportBoardingScenarioDescriptor(
            string scenarioId,
            string displayName,
            string description,
            TransportBoardingScenarioKind kind,
            TransportBoardingScenarioVisualMode visualMode,
            TransportBoardingScenarioExitMode exitMode,
            bool automatedProofRequired,
            bool visualProofRequired)
        {
            ScenarioId = scenarioId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Kind = kind;
            VisualMode = visualMode;
            ExitMode = exitMode;
            AutomatedProofRequired = automatedProofRequired;
            VisualProofRequired = visualProofRequired;
        }

        public string ScenarioId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public TransportBoardingScenarioKind Kind { get; }
        public TransportBoardingScenarioVisualMode VisualMode { get; }
        public TransportBoardingScenarioExitMode ExitMode { get; }
        public bool AutomatedProofRequired { get; }
        public bool VisualProofRequired { get; }
    }

    public static class TransportBoardingScenarioCatalog
    {
        public const string Tb001GroundVehicleBoardExitId = "TB-001_GroundVehicleTransport_BoardAndGroundExit";
        public const string Tb002HelicopterBoardRopeExitId = "TB-002_HelicopterTransport_BoardAndRopeExit";
        public const string Tb003HelicopterAirPickupId = "TB-003_HelicopterTransport_AirPickupBeforeBoarding";
        public const string Tb004HelicopterGroundExitAuditId = "TB-004_HelicopterTransport_GroundExitBehaviorAudit";
        public const string Tb005PlaneRampBoardGroundExitId = "TB-005_TransportPlane_RampBoardAndGroundExit";
        public const string Tb006PlaneSoldierAirdropId = "TB-006_TransportPlane_SoldierAirdrop";
        public const string Tb007PlaneVehicleCargoGroundExitId = "TB-007_TransportPlane_VehicleCargoGroundExit";
        public const string Tb008PlaneVehicleCargoAirdropId = "TB-008_TransportPlane_VehicleCargoAirdrop";
        public const string Tb009PlaneMixedLoadAirdropId = "TB-009_TransportPlane_MixedLoadAirdrop";
        public const string Tb010RejectionCasesId = "TB-010_TransportBoarding_RejectionCases";
        public const string Tb011NextCleanupId = "TB-011_TransportBoarding_NextCleanup";
        public const string Tb012CameraProofPathId = "TB-012_TransportBoarding_CameraProofPath";

        private static readonly TransportBoardingScenarioDescriptor[] Scenarios =
        {
            new(
                Tb001GroundVehicleBoardExitId,
                "Ground vehicle board and exit",
                "Soldiers board a ground vehicle transport, become hidden passengers, then disembark to valid adjacent cells.",
                TransportBoardingScenarioKind.GroundVehicleBoardAndExit,
                TransportBoardingScenarioVisualMode.GroundVehicle,
                TransportBoardingScenarioExitMode.Ground,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb002HelicopterBoardRopeExitId,
                "Helicopter board and rope exit",
                "Soldiers board a landed helicopter transport, then exit through the production rope disembark flow.",
                TransportBoardingScenarioKind.HelicopterBoardAndRopeExit,
                TransportBoardingScenarioVisualMode.Helicopter,
                TransportBoardingScenarioExitMode.Rope,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb003HelicopterAirPickupId,
                "Helicopter air pickup before boarding",
                "An airborne helicopter receives a pickup landing command and prevents boarding until it is physically landed.",
                TransportBoardingScenarioKind.HelicopterAirPickup,
                TransportBoardingScenarioVisualMode.Helicopter,
                TransportBoardingScenarioExitMode.Rope,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb004HelicopterGroundExitAuditId,
                "Helicopter ground exit behavior audit",
                "Documents current production behavior: helicopter disembark starts rope flow even from an initially landed state.",
                TransportBoardingScenarioKind.HelicopterGroundExitAudit,
                TransportBoardingScenarioVisualMode.Helicopter,
                TransportBoardingScenarioExitMode.AuditOnly,
                automatedProofRequired: true,
                visualProofRequired: false),
            new(
                Tb005PlaneRampBoardGroundExitId,
                "Transport plane ramp board and ground exit",
                "Soldiers use the resolved rear ramp approach, board the transport plane, then exit through the production ramp flow.",
                TransportBoardingScenarioKind.PlaneRampBoardAndExit,
                TransportBoardingScenarioVisualMode.Plane,
                TransportBoardingScenarioExitMode.Ground,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb006PlaneSoldierAirdropId,
                "Transport plane soldier airdrop",
                "Soldiers board a transport plane, then exit while airborne through the production parachute airdrop flow.",
                TransportBoardingScenarioKind.PlaneSoldierAirdrop,
                TransportBoardingScenarioVisualMode.Plane,
                TransportBoardingScenarioExitMode.Parachute,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb007PlaneVehicleCargoGroundExitId,
                "Transport plane vehicle cargo ground exit",
                "A vehicle cargo passenger boards the transport plane cargo slot and exits on the ground through the ramp flow.",
                TransportBoardingScenarioKind.PlaneVehicleCargoGroundExit,
                TransportBoardingScenarioVisualMode.Plane,
                TransportBoardingScenarioExitMode.Ground,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb008PlaneVehicleCargoAirdropId,
                "Transport plane vehicle cargo airdrop",
                "A vehicle cargo passenger boards the transport plane and exits while airborne through the cargo drop flow.",
                TransportBoardingScenarioKind.PlaneVehicleCargoAirdrop,
                TransportBoardingScenarioVisualMode.Plane,
                TransportBoardingScenarioExitMode.CargoDrop,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb009PlaneMixedLoadAirdropId,
                "Transport plane mixed load airdrop",
                "A mixed soldier and vehicle load exits an airborne transport plane with the correct passenger counts by kind.",
                TransportBoardingScenarioKind.PlaneMixedLoadAirdrop,
                TransportBoardingScenarioVisualMode.Plane,
                TransportBoardingScenarioExitMode.MixedAirdrop,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb010RejectionCasesId,
                "Transport boarding rejection cases",
                "Full transport, wrong passenger kind, airborne boarding, blocked exit, and missing visual prefab use production reason codes.",
                TransportBoardingScenarioKind.RejectionCases,
                TransportBoardingScenarioVisualMode.NegativeCase,
                TransportBoardingScenarioExitMode.NegativeOnly,
                automatedProofRequired: true,
                visualProofRequired: false),
            new(
                Tb011NextCleanupId,
                "Transport boarding Next cleanup",
                "Repeated scenario switching cleans passengers, transports, drop visuals, camera targets, and overlay state before the next run.",
                TransportBoardingScenarioKind.NextCleanup,
                TransportBoardingScenarioVisualMode.Cleanup,
                TransportBoardingScenarioExitMode.AuditOnly,
                automatedProofRequired: true,
                visualProofRequired: true),
            new(
                Tb012CameraProofPathId,
                "Transport boarding camera proof path",
                "Camera beats frame approach, boarding, hidden passenger state, exit, landing or settle, and final cleanup.",
                TransportBoardingScenarioKind.CameraProofPath,
                TransportBoardingScenarioVisualMode.CameraProof,
                TransportBoardingScenarioExitMode.AuditOnly,
                automatedProofRequired: false,
                visualProofRequired: true)
        };

        public static IReadOnlyList<TransportBoardingScenarioDescriptor> All => Scenarios;

        public static bool IsTransportBoardingScenarioId(string scenarioId)
        {
            return TryGetScenario(scenarioId, out _);
        }

        public static bool TryGetScenario(string scenarioId, out TransportBoardingScenarioDescriptor descriptor)
        {
            if (!string.IsNullOrWhiteSpace(scenarioId))
            {
                for (int i = 0; i < Scenarios.Length; i++)
                {
                    TransportBoardingScenarioDescriptor candidate = Scenarios[i];
                    if (string.Equals(candidate.ScenarioId, scenarioId, StringComparison.Ordinal))
                    {
                        descriptor = candidate;
                        return true;
                    }
                }
            }

            descriptor = default;
            return false;
        }
    }
}
