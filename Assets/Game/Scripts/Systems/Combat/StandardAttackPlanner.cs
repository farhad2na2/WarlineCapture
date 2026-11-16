using System;

namespace Game.Runtime.Combat
{
    public enum StandardAttackKind : byte
    {
        None = 0,
        ClearTarget = 1,
        Windup = 2,
        Shot = 3
    }

    public struct StandardAttackState
    {
        public float CooldownRemaining;
        public float TraceTimeRemaining;
        public float TracePhase;
        public int ShotCounter;
        public float AnimationTimeRemaining;
    }

    // Snapshot after production targeting/eligibility checks. Coordinates are XZ world units.
    public struct StandardAttackInput
    {
        public StandardAttackState State;
        public int SelfHealth;
        public int TargetHealth;
        public byte HasTarget;
        public byte TargetExists;
        public byte TargetHasHealth;
        public byte TargetHasTransform;
        public byte TargetIsStaticGridBlocker;
        public byte IsDebugFireTarget;
        public byte HasAnimationOrder;
        public byte AircraftAttackWindowBlocked;
        public float AttackerX;
        public float AttackerZ;
        public float TargetX;
        public float TargetZ;
        public float SelfCombatRadius;
        public float TargetCombatRadius;
        public float Range;
        public float CooldownSeconds;
        public int Damage;
        public float TraceVisibleSeconds;
        public int TracerEveryNthShot;
        public float AttackAnimationSeconds;
    }

    public struct StandardAttackResult
    {
        public StandardAttackKind Kind;
        public StandardAttackState BaseState;
        public StandardAttackState State;
        public int Damage;
        public byte ShowTracer;
    }

    // Shared by the Burst job and standalone data checks. This plans one eligible actor;
    // targeting, animation ticking, predicted-health ordering and damage stay with the host.
    public static class StandardAttackPlanner
    {
        public static StandardAttackResult Plan(in StandardAttackInput input, float deltaTime, float cellSize)
        {
            StandardAttackState state = input.State;
            state.CooldownRemaining -= deltaTime;
            state.TraceTimeRemaining = Max(0f, state.TraceTimeRemaining - deltaTime);
            StandardAttackResult plan = new StandardAttackResult { BaseState = state, State = state };
            if (input.SelfHealth <= 0 || input.HasTarget == 0)
                return plan;
            if (input.TargetExists == 0 || input.TargetHasHealth == 0 || input.TargetHasTransform == 0 ||
                (input.TargetIsStaticGridBlocker != 0 && input.IsDebugFireTarget == 0) || input.TargetHealth <= 0)
            {
                plan.Kind = StandardAttackKind.ClearTarget;
                return plan;
            }
            float attackRange = Max(0f, input.Range);
            if (attackRange <= 0f || input.AircraftAttackWindowBlocked != 0)
                return plan;
            float dx = input.TargetX - input.AttackerX;
            float dz = input.TargetZ - input.AttackerZ;
            float range = attackRange + input.SelfCombatRadius + input.TargetCombatRadius + cellSize * 0.25f;
            // Preserve float3 dot's order with the original zeroed Y term.
            if ((dx * dx + 0f) + dz * dz > range * range || state.CooldownRemaining > 0f)
                return plan;
            if (state.AnimationTimeRemaining <= 0f && input.HasAnimationOrder != 0 &&
                input.State.CooldownRemaining <= 0f)
            {
                plan.Kind = StandardAttackKind.Windup;
                plan.State.AnimationTimeRemaining = Max(0.01f, input.AttackAnimationSeconds);
                plan.State.CooldownRemaining = Max(0.05f, Min(0.18f, input.CooldownSeconds * 0.5f));
                return plan;
            }
            plan.Kind = StandardAttackKind.Shot;
            plan.State.CooldownRemaining = Max(0.01f, input.CooldownSeconds);
            plan.State.ShotCounter = unchecked(plan.State.ShotCounter + 1);
            int tracerInterval = input.TracerEveryNthShot > 1 ? input.TracerEveryNthShot : 1;
            bool tracerShown = plan.State.ShotCounter % tracerInterval == 0;
            if (tracerShown)
            {
                plan.State.TraceTimeRemaining = Max(0.01f, input.TraceVisibleSeconds);
                float phase = plan.State.TracePhase + 0.371f;
                plan.State.TracePhase = phase - (float)Math.Floor(phase);
            }
            plan.State.AnimationTimeRemaining = Max(Max(0.01f, input.AttackAnimationSeconds), input.CooldownSeconds + 0.15f);
            plan.Damage = input.Damage;
            plan.ShowTracer = (byte)(tracerShown ? 1 : 0);
            return plan;
        }

        // Unity.Mathematics scalar min/max semantics, including NaN and equal values.
        private static float Max(float a, float b) => float.IsNaN(b) || a > b ? a : b;
        private static float Min(float a, float b) => float.IsNaN(b) || a < b ? a : b;
    }
}
