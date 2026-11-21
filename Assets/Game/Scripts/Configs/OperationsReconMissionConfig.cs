using System;
using Game.Operations.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct OperationsReconForceEntry
    {
        public string sourceKey;
        public Vector3 position;
        public int count;
        public byte faction;
        public bool recon;
        public bool patrol;
        // 0: initial, 1: after first scan, 2: after evidence recovery.
        public byte wave;
    }

    [CreateAssetMenu(menuName = "Game/Operations/Recon mission")]
    public sealed class OperationsReconMissionConfig : ScriptableObject
    {
        // W38: kept for existing callers; configs are now resolved per mission by OperationsMissionRegistry.
        public const string ResourcePath = "Operations/StreetSignals";
        public string missionId = "operation.o001";
        public string scenarioId = "scenario.operations.o001";
        public OperationMapDefinition operationMap;
        public BuildingPlacementSystemConfig startupConfig;
        public Vector3[] scanPositions = Array.Empty<Vector3>();
        public Vector3 evidencePosition;
        public Vector3 exitPosition;
        public Vector3[] patrolRoute = Array.Empty<Vector3>();
        public OperationsReconForceEntry[] forces = Array.Empty<OperationsReconForceEntry>();
        public float scanSeconds = 15f;
        public float evidenceSeconds = 15f;
        public float deadlineSeconds = 720f;
        public float reinforcementWarningSeconds = 30f;
        public float evidenceReinforcementWarningSeconds = 45f;
        // W38: the SCAN nodes this mission's graph requires (O001: three signals).
        public int requiredScans = 3;
        // W38: family HUD restrictions as data (defaults are O001's RECON values).
        public bool hudBuildingDisabled = true, hudProductionDisabled = true, hudEconomyDisabled = true,
            hudTransportDisabled = true, hudAirDisabled = true, hudHideLogisticsResources = true, hudHideUnrelatedControls = true;
        public int hudAvailableSquadMask = 1;

        // W38: finite package sizes keyed by the catalog's package tokens (player total, of which recon; enemy total).
        public static bool TryGetPackageSize(string token, out int units, out int recon)
        {
            switch (token)
            {
                case "FP_LIGHT": units = 16; recon = 2; return true;
                case "EP_CELL": units = 20; recon = 0; return true;
                default: units = recon = 0; return false;
            }
        }

        public byte HudFlags =>
            (byte)((hudBuildingDisabled ? 1 : 0) | (hudProductionDisabled ? 2 : 0) | (hudEconomyDisabled ? 4 : 0) | (hudTransportDisabled ? 8 : 0) |
                   (hudAirDisabled ? 16 : 0) | (hudHideLogisticsResources ? 32 : 0) | (hudHideUnrelatedControls ? 64 : 0));

        public bool TryValidate(out string error)
        {
            error = string.Empty;
            // W38: identity, map, family, packages and deadline come from the catalog row, not O001 literals.
            bool catalog = OperationsMissionRegistry.TryGetCatalogEntry(missionId, out var entry);
            if (!catalog || entry.Family != OperationsMissionFamilyKind.Recon || scenarioId != entry.ScenarioId ||
                operationMap == null || operationMap.OperationMapId != entry.OperationMapId ||
                !operationMap.TryValidateMetadata(out error))
            { error = "This Operations mission requires its validated catalog map and scenario identity. " + error; return false; }
            if (!TryGetPackageSize(entry.ForcePackageToken, out int expectedPlayer, out int expectedRecon) ||
                !TryGetPackageSize(entry.EnemyPackageToken, out int expectedEnemy, out _))
            { error = "This Operations mission's force packages have no finite size."; return false; }
            if (scanPositions == null || requiredScans < 1 || scanPositions.Length != requiredScans || scanSeconds != 15f ||
                evidenceSeconds != 15f || deadlineSeconds != entry.HardDeadlineSeconds || !Finite(reinforcementWarningSeconds) || reinforcementWarningSeconds < 20f ||
                !Finite(evidenceReinforcementWarningSeconds) || evidenceReinforcementWarningSeconds < 20f ||
                !Finite(evidencePosition) || !Finite(exitPosition))
            { error = "Operations recon objective/timing contract is incomplete."; return false; }
            if (startupConfig == null || startupConfig.InitialUnitsConfig == null ||
                startupConfig.UnitPrefabRegistryConfig == null || startupConfig.InitialUnitsConfig.CreateFactionBases ||
                startupConfig.InitialUnitsConfig.BlockerCount != 0 || startupConfig.InitialUnitsConfig.EnableBlockerChurn)
            { error = "Operations recon requires an isolated finite-force startup configuration."; return false; }
            int player = 0, recon = 0, enemy = 0, waveA = 0, waveB = 0;
            foreach (var force in forces ?? Array.Empty<OperationsReconForceEntry>())
            {
                if (string.IsNullOrWhiteSpace(force.sourceKey) || force.sourceKey.Length > 60 || force.count < 1 ||
                    force.count > 20 || force.faction is not (1 or 2) || force.wave > 2 || force.faction == 1 && force.wave != 0 ||
                    !Finite(force.position) || force.patrol && (force.faction != 2 || force.wave != 0 || patrolRoute == null || patrolRoute.Length < 2))
                { error = "Invalid finite force entry."; return false; }
                if (force.faction == 1) { player += force.count; if (force.recon) recon += force.count; }
                else { enemy += force.count; if (force.wave == 1) waveA += force.count; if (force.wave == 2) waveB += force.count; }
            }
            if (player != expectedPlayer || recon != expectedRecon || enemy != expectedEnemy || waveA == 0 || waveB == 0)
            { error = "Operations recon requires its catalog packages " + entry.ForcePackageToken + " (" + expectedPlayer + " including " + expectedRecon +
                " recon), " + entry.EnemyPackageToken + " (" + expectedEnemy + "), and both finite reserves."; return false; }
            for (int i = 0; i < scanPositions.Length; i++)
            {
                if (!Finite(scanPositions[i])) { error = "Invalid signal position."; return false; }
                for (int j = i + 1; j < scanPositions.Length; j++)
                    if (Vector3.Distance(scanPositions[i], scanPositions[j]) < 16f)
                    { error = "Scan sites must have distinct interaction zones."; return false; }
            }
            foreach (var point in patrolRoute ?? Array.Empty<Vector3>())
                if (!Finite(point)) { error = "Invalid patrol waypoint."; return false; }
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    }
}
