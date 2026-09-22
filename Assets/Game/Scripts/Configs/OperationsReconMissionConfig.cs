using System;
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

        public bool TryValidate(out string error)
        {
            error = string.Empty;
            if (missionId != "operation.o001" || scenarioId != "scenario.operations.o001" ||
                operationMap == null || operationMap.OperationMapId != "opmap.operations.old_quarter" ||
                !operationMap.TryValidateMetadata(out error))
            { error = "Street Signals requires its validated Operations map and scenario identity. " + error; return false; }
            if (scanPositions == null || scanPositions.Length != 3 || scanSeconds != 15f ||
                evidenceSeconds != 15f || deadlineSeconds != 720f || !Finite(reinforcementWarningSeconds) || reinforcementWarningSeconds < 20f ||
                !Finite(evidenceReinforcementWarningSeconds) || evidenceReinforcementWarningSeconds < 20f ||
                !Finite(evidencePosition) || !Finite(exitPosition))
            { error = "Street Signals objective/timing contract is incomplete."; return false; }
            if (startupConfig == null || startupConfig.InitialUnitsConfig == null ||
                startupConfig.UnitPrefabRegistryConfig == null || startupConfig.InitialUnitsConfig.CreateFactionBases ||
                startupConfig.InitialUnitsConfig.BlockerCount != 0 || startupConfig.InitialUnitsConfig.EnableBlockerChurn)
            { error = "Street Signals requires an isolated finite-force startup configuration."; return false; }
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
            if (player != 16 || recon != 2 || enemy != 20 || waveA == 0 || waveB == 0)
            { error = "Street Signals requires FP_LIGHT (16 including 2 recon), EP_CELL (20), and both finite reserves."; return false; }
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
