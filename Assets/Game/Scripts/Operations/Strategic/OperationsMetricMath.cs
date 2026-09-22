using System;
using Game.Operations.Contracts;

namespace Game.Operations.Strategic
{
    /// <summary>
    /// Signed metric deltas. <see cref="OperationsDistrictMetricTuple"/> clamps inside
    /// its constructor, so it must not be used as a delta carrier.
    /// </summary>
    public readonly struct OperationsSignedMetricDelta
    {
        public OperationsSignedMetricDelta(
            int security,
            int trust,
            int infrastructure,
            int enemyInfluence,
            int intelConfidence,
            int heat,
            int supplyReadiness)
        {
            Security = security;
            Trust = trust;
            Infrastructure = infrastructure;
            EnemyInfluence = enemyInfluence;
            IntelConfidence = intelConfidence;
            Heat = heat;
            SupplyReadiness = supplyReadiness;
        }

        public int Security { get; }
        public int Trust { get; }
        public int Infrastructure { get; }
        public int EnemyInfluence { get; }
        public int IntelConfidence { get; }
        public int Heat { get; }
        public int SupplyReadiness { get; }

        public static OperationsSignedMetricDelta Zero => new(0, 0, 0, 0, 0, 0, 0);

        public static OperationsSignedMetricDelta Defeat => new(-4, -2, 0, 6, 0, 3, 0);

        public static OperationsSignedMetricDelta Withdrawn => new(-2, 0, 0, 3, 0, 0, 0);

        public static OperationsSignedMetricDelta IncidentExpiry => new(-4, 0, 0, 4, 0, 0, -3);

        public static OperationsSignedMetricDelta FinaleRecovery => new(2, 0, 0, -2, 0, 0, 2);

        public static OperationsSignedMetricDelta Victory(OperationsMissionFamilyKind family)
        {
            switch (family)
            {
                case OperationsMissionFamilyKind.Recon: return new(0, 1, 0, -2, 18, 2, 0);
                case OperationsMissionFamilyKind.Patrol: return new(10, 3, 0, -5, 4, 2, 0);
                case OperationsMissionFamilyKind.Raid: return new(8, 1, 0, -14, 6, 7, 0);
                case OperationsMissionFamilyKind.Rescue: return new(3, 14, 0, -3, 3, 3, 0);
                case OperationsMissionFamilyKind.Escort: return new(4, 6, 2, -3, 0, 2, 14);
                case OperationsMissionFamilyKind.Repair: return new(3, 5, 18, -2, 0, 2, 6);
                case OperationsMissionFamilyKind.Defense: return new(12, 5, 4, -8, 2, 3, 2);
                case OperationsMissionFamilyKind.Interdict: return new(7, 1, 0, -12, 6, 5, 4);
                case OperationsMissionFamilyKind.Seize: return new(10, 2, 4, -10, 3, 5, 8);
                case OperationsMissionFamilyKind.Airlift: return new(4, 10, 0, -3, 2, 4, 10);
                case OperationsMissionFamilyKind.Breach: return new(8, 1, 0, -16, 8, 8, 0);
                case OperationsMissionFamilyKind.Finale: return new(12, 8, 10, -20, 5, -10, 10);
                default: throw new ArgumentOutOfRangeException(nameof(family));
            }
        }

        public static OperationsSignedMetricDelta HalfTowardZero(OperationsSignedMetricDelta value) => new(
            TowardZero(value.Security),
            TowardZero(value.Trust),
            TowardZero(value.Infrastructure),
            TowardZero(value.EnemyInfluence),
            TowardZero(value.IntelConfidence),
            TowardZero(value.Heat),
            TowardZero(value.SupplyReadiness));

        public static OperationsSignedMetricDelta operator +(OperationsSignedMetricDelta left, OperationsSignedMetricDelta right) =>
            new(
                left.Security + right.Security,
                left.Trust + right.Trust,
                left.Infrastructure + right.Infrastructure,
                left.EnemyInfluence + right.EnemyInfluence,
                left.IntelConfidence + right.IntelConfidence,
                left.Heat + right.Heat,
                left.SupplyReadiness + right.SupplyReadiness);

        public static int Clamp(int value)
        {
            if (value < 0)
                return 0;
            return value > 100 ? 100 : value;
        }

        private static int TowardZero(int value) => value / 2;
    }

    public readonly struct OperationsMetricApplication
    {
        public OperationsMetricApplication(
            OperationsSignedMetricDelta requested,
            OperationsSignedMetricDelta applied,
            OperationsDistrictComponent district)
        {
            Requested = requested;
            Applied = applied;
            District = district;
        }

        public OperationsSignedMetricDelta Requested { get; }
        public OperationsSignedMetricDelta Applied { get; }
        public OperationsDistrictComponent District { get; }
    }

    public static class OperationsMetricMath
    {
        public static OperationsMetricApplication Apply(
            OperationsDistrictComponent district,
            OperationsSignedMetricDelta delta)
        {
            int security = district.Security + delta.Security;
            int trust = district.Trust + delta.Trust;
            int infrastructure = district.Infrastructure + delta.Infrastructure;
            int enemy = district.EnemyInfluence + delta.EnemyInfluence;
            int intel = district.IntelConfidence + delta.IntelConfidence;
            int heat = district.Heat + delta.Heat;
            int supply = district.SupplyReadiness + delta.SupplyReadiness;

            district.Security = OperationsSignedMetricDelta.Clamp(security);
            district.Trust = OperationsSignedMetricDelta.Clamp(trust);
            district.Infrastructure = OperationsSignedMetricDelta.Clamp(infrastructure);
            district.EnemyInfluence = OperationsSignedMetricDelta.Clamp(enemy);
            district.IntelConfidence = OperationsSignedMetricDelta.Clamp(intel);
            district.Heat = OperationsSignedMetricDelta.Clamp(heat);
            district.SupplyReadiness = OperationsSignedMetricDelta.Clamp(supply);
            district.ChangeVersion++;

            OperationsSignedMetricDelta applied = new(
                district.Security - (security - delta.Security),
                district.Trust - (trust - delta.Trust),
                district.Infrastructure - (infrastructure - delta.Infrastructure),
                district.EnemyInfluence - (enemy - delta.EnemyInfluence),
                district.IntelConfidence - (intel - delta.IntelConfidence),
                district.Heat - (heat - delta.Heat),
                district.SupplyReadiness - (supply - delta.SupplyReadiness));

            return new OperationsMetricApplication(delta, applied, district);
        }

        public static OperationsSignedMetricDelta Harm(OperationsMissionResult result)
        {
            int deaths = result.CivilianDeaths > 10 ? 10 : result.CivilianDeaths;
            int destroyed = 0;
            OperationsObjectiveFact[] sites = result.ProtectedSiteFacts ?? Array.Empty<OperationsObjectiveFact>();
            for (int index = 0; index < sites.Length; index++)
            {
                if (sites[index].Failed)
                    destroyed++;
            }

            if (destroyed > 3)
                destroyed = 3;

            int security = 0;
            if (result.InitialTaskForceCount > 0)
            {
                int percent = (result.TaskForceLosses * 100) / result.InitialTaskForceCount;
                int steps = percent / 25;
                if (steps > 4)
                    steps = 4;
                security = -steps;
            }

            return new OperationsSignedMetricDelta(security, (-2 * deaths) + (-4 * destroyed), -5 * destroyed, 0, 0, 0, 0);
        }
    }
}
