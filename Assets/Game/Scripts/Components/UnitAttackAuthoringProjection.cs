using Unity.Mathematics;

namespace Game.Components
{
    /// <summary>
    /// Data-only mapping for the UnitAttack component authored by UnitGridAuthoring.
    /// Configured combat values arrive through the current authoring getters; trace
    /// values arrive from the current config-or-serialized-field selection. This
    /// projection owns only the same Unity.Mathematics floors as the original Baker.
    /// </summary>
    public static class UnitAttackAuthoringProjection
    {
        public static UnitAttack FromConfiguredValues(
            float range,
            float cooldownSeconds,
            int damage,
            float traceRed,
            float traceGreen,
            float traceBlue,
            float traceAlpha,
            float traceWidth,
            float traceScrollSpeed,
            float traceDashDensity,
            float traceVisibleSeconds,
            int tracerEveryNthShot)
        {
            return new UnitAttack
            {
                Range = range,
                CooldownSeconds = cooldownSeconds,
                Damage = damage,
                TraceColor = new float4(traceRed, traceGreen, traceBlue, traceAlpha),
                TraceWidth = math.max(0.01f, traceWidth),
                TraceScrollSpeed = math.max(0.1f, traceScrollSpeed),
                TraceDashDensity = math.max(1f, traceDashDensity),
                TraceVisibleSeconds = math.max(0.01f, traceVisibleSeconds),
                TracerEveryNthShot = math.max(1, tracerEveryNthShot)
            };
        }
    }
}
