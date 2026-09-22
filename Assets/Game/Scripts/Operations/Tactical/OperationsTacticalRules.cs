using System;
using Game.Operations.Contracts;

namespace Game.Operations.Tactical
{
    public enum OperationsTacticalFaction : byte
    {
        Player = 0,
        Hostile = 1,
        Neutral = 2
    }

    public enum OperationsTacticalBodyKind : byte
    {
        Infantry = 0,
        Vehicle = 1,
        Cargo = 2,
        Site = 3,
        Evidence = 4
    }

    public enum OperationsWaveTriggerKind : byte
    {
        FirstRequiredCompletion = 0,
        FinalRequiredActivation = 1,
        NodeCompletion = 2,
        NodeActivation = 3,
        Elapsed = 4
    }

    public enum OperationsTacticalFactKind : byte
    {
        Observed = 0,
        ScanConfirmed = 1,
        InteractCompleted = 2,
        Repaired = 3,
        CargoDelivered = 4,
        ExitReached = 5,
        SiteDestroyed = 6,
        UnitDied = 7,
        WaveWarned = 8,
        WaveSpawned = 9
    }

    public enum OperationsTacticalNodePhase : byte
    {
        Inactive = 0,
        Active = 1,
        Complete = 2,
        Failed = 3
    }

    public enum OperationsTacticalRejectKind : byte
    {
        None = 0,
        OutOfRange = 1,
        BlockedLineOfSight = 2,
        NotEligible = 3,
        NodeInactive = 4,
        HostilePresent = 5,
        InsufficientMaterials = 6,
        AlreadyConsumed = 7,
        UnknownTarget = 8,
        SiteDestroyed = 9,
        RouteLocked = 10,
        Terminal = 11,
        PreconditionFailed = 12,
        NotObserved = 13,
        Duplicate = 14
    }

    /// <summary>
    /// Package 2 distances and durations. One tactical tick is one second.
    /// Shared combat AI is not simulated here; these are the objective rules only.
    /// </summary>
    public static class OperationsTacticalRules
    {
        public const int TicksPerSecond = 1;
        public const float ScanMeters = 8f;
        /// <summary>Channel length for Scan. Shortened for Regular pacing (≥1 decision / 20–30s).</summary>
        public const int ScanSeconds = 6;
        public const float ObserveMeters = 16f;
        public const float HoldMeters = 12f;
        /// <summary>
        /// Hold must be re-issued within this many ticks or progress freezes (AFK cut).
        /// Hard deadlines and Partial predicates stay on authored mission budgets.
        /// </summary>
        public const int HoldRefreshSeconds = 5;
        public const float InteractMeters = 6f;
        public const float InteractThreatMeters = 12f;
        public const int InteractSeconds = 6;
        public const float RepairMeters = 6f;
        public const float RepairThreatMeters = 12f;
        /// <summary>Repair channel length. Shortened for Regular pacing; deadlines unchanged.</summary>
        public const int RepairSeconds = 18;
        public const int RepairMaterialCost = 40;
        public const int RepairHealthPercent = 75;
        public const int ThreeSiteMaterialFloor = 120;
        public const float InfantryMetersPerTick = 4f;
        public const float CargoMetersPerTick = 3f;
        public const int EscortUnloadSeconds = 10;
        public const int ExtractMinimumInfantry = 2;
        public const float ExtractArrivalMeters = 3f;
        public const float ExitSecureMeters = 12f;
        public const int WaveAWarningSeconds = 30;
        public const int WaveBWarningSeconds = 45;
        public const int MinimumWarningSeconds = 20;
        public const float SpawnOccupationMeters = 8f;

        public const float AttackMeters = 12f;

        public static bool IsPackageVerb(OperationsObjectiveRuleKind rule) =>
            rule == OperationsObjectiveRuleKind.Scan ||
            rule == OperationsObjectiveRuleKind.Hold ||
            rule == OperationsObjectiveRuleKind.Interact ||
            rule == OperationsObjectiveRuleKind.Repair ||
            rule == OperationsObjectiveRuleKind.Escort ||
            rule == OperationsObjectiveRuleKind.Extract ||
            rule == OperationsObjectiveRuleKind.Clear ||
            rule == OperationsObjectiveRuleKind.Protect;

        public static void SplitBudget(int count, out int initial, out int waveA, out int waveB)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            initial = count / 2;
            waveA = count / 4;
            waveB = count - initial - waveA;
        }

        public static bool Within(float x0, float z0, float x1, float z1, float meters)
        {
            float dx = x0 - x1;
            float dz = z0 - z1;
            float limit = meters * meters;
            return (dx * dx) + (dz * dz) <= limit + 0.01f;
        }

        public static float Distance(float x0, float z0, float x1, float z1)
        {
            float dx = x0 - x1;
            float dz = z0 - z1;
            return (float)Math.Sqrt((dx * dx) + (dz * dz));
        }

        public static void StepToward(ref float x, ref float z, float targetX, float targetZ, float speed)
        {
            float dx = targetX - x;
            float dz = targetZ - z;
            float distSq = (dx * dx) + (dz * dz);
            float speedSq = speed * speed;
            if (distSq <= speedSq)
            {
                x = targetX;
                z = targetZ;
                return;
            }

            float dist = (float)Math.Sqrt(distSq);
            x += dx / dist * speed;
            z += dz / dist * speed;
        }

        public static bool SegmentsBlockSight(
            float ax, float az, float bx, float bz,
            float cx, float cz, float dx, float dz)
        {
            float o1 = Cross(ax, az, bx, bz, cx, cz);
            float o2 = Cross(ax, az, bx, bz, dx, dz);
            float o3 = Cross(cx, cz, dx, dz, ax, az);
            float o4 = Cross(cx, cz, dx, dz, bx, bz);
            return o1 * o2 < 0f && o3 * o4 < 0f;
        }

        private static float Cross(float ax, float az, float bx, float bz, float cx, float cz) =>
            ((bx - ax) * (cz - az)) - ((bz - az) * (cx - ax));
    }
}
