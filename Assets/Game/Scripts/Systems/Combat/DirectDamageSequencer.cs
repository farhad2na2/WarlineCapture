namespace Game.Runtime.Combat
{
    public enum StandardAttackCommit : byte { MissingAttacker, BaseOnly, ClearTarget, Windup, Shot }

    public struct DirectDamageSource
    {
        public int Index, Version, CellX, CellZ;
        public float X, Y, Z;
    }

    public struct DirectDamageAggregate
    {
        public int TotalDamage;
        public DirectDamageSource LastSource, ObservationSource;
        public byte HasMixedObservationSources, ShowImpactVfx;
    }

    // Ordered direct-fire reduction shared by ECS and offline replay. The host owns
    // candidate order, target eligibility, snapshot health and delayed health writes.
    public static class DirectDamageSequencer
    {
        public static int ApplyCover(int damage, ushort permille)
        {
            if (damage <= 0) return 0;
            if (permille == 0 || permille >= 1000) return damage;
            return (int)System.Math.Max(1L, ((long)damage * permille + 500L) / 1000L);
        }

        // Bypass intentionally preserves the original damage value, including negatives.
        public static int ApplyToTarget(int damage, bool coverEligible, ushort permille) =>
            coverEligible ? ApplyCover(damage, permille) : damage;

        public static StandardAttackCommit Decide(StandardAttackKind kind, bool attackerExists, int predictedHealth)
        {
            if (!attackerExists) return StandardAttackCommit.MissingAttacker;
            if (kind == StandardAttackKind.ClearTarget) return StandardAttackCommit.ClearTarget;
            if (kind == StandardAttackKind.None) return StandardAttackCommit.BaseOnly;
            // Windup occurs before the predicted-health gate in the native commit loop.
            if (kind == StandardAttackKind.Windup) return StandardAttackCommit.Windup;
            return predictedHealth <= 0 ? StandardAttackCommit.ClearTarget : StandardAttackCommit.Shot;
        }

        public static StandardAttackState CommittedState(in StandardAttackState original,
            in StandardAttackResult plan, StandardAttackCommit decision)
        {
            if (decision == StandardAttackCommit.MissingAttacker) return original;
            if (decision == StandardAttackCommit.Shot) return plan.State;
            var state = plan.BaseState;
            if (decision == StandardAttackCommit.Windup)
            {
                state.CooldownRemaining = plan.State.CooldownRemaining;
                state.AnimationTimeRemaining = plan.State.AnimationTimeRemaining;
            }
            return state;
        }

        public static int RemainingHealth(int health, int damage)
        {
            // Retain existing unchecked int arithmetic; this extraction is not a balance change.
            int remaining = unchecked(health - damage);
            return remaining > 0 ? remaining : 0;
        }

        // Called only for a committed positive-damage hit, after cover adjustment.
        public static int Accumulate(ref DirectDamageAggregate aggregate, bool hasAggregate,
            int predictedHealth, int damage, in DirectDamageSource source, byte showTracer)
        {
            if (hasAggregate)
            {
                aggregate.TotalDamage = unchecked(aggregate.TotalDamage + damage);
                if (aggregate.ObservationSource.Index != source.Index || aggregate.ObservationSource.Version != source.Version)
                    aggregate.HasMixedObservationSources = 1;
                aggregate.LastSource = source;
                aggregate.ShowImpactVfx |= showTracer;
            }
            else
                aggregate = new DirectDamageAggregate
                {
                    TotalDamage = damage, LastSource = source, ObservationSource = source, ShowImpactVfx = showTracer
                };
            return RemainingHealth(predictedHealth, damage);
        }
    }
}
