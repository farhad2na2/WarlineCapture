extern alias ActualCore;
using Unity.Entities;
using Game.Components;
namespace Game.Runtime
{
    internal sealed partial class RuntimeDefinition
    {
        internal bool HasLocalBounds, CanAttack, MaterialFabricationEnabled;
        internal ActualCore::UnityEngine.Bounds LocalBounds;
        internal int OilStorageCapacity, FuelStorageCapacity, MaterialFabricationMaterialsOutputPerCycle;
        internal float OilBarrelsPerDay, FuelBarrelsPerDay, MaterialFabricationOilConsumedPerCycle, MaterialFabricationCycleDurationSeconds;
        internal MaterialFabricationOutputCapacityPolicyCode MaterialFabricationOutputCapacityPolicy;
        internal ThreatDetectionKind ThreatDetectionKind;
        internal int ThreatDetectionRadiusCells, AttackDamage, MaxConcurrentAttacks, AttackTracerEveryNthShot;
        internal float AttackRange, AttackCooldownSeconds, AttackTraceWidth, AttackTraceScrollSpeed, AttackTraceDashDensity, AttackTraceVisibleSeconds;
        internal float MuzzleFlashHeightOffset, MuzzleFlashForwardOffset;
        internal ActualCore::UnityEngine.Color AttackTraceColor;
        internal ActualCore::UnityEngine.GameObject AttackImpactPrefab, MuzzleFlashPrefab;
    }
}
