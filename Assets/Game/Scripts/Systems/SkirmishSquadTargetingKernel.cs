namespace Game.Runtime
{
    public struct SquadTargetIdentity
    {
        public int Index, Version;
        public SquadTargetIdentity(int index, int version) { Index = index; Version = version; }
    }
    public struct SquadTargetCandidate
    {
        public SquadTargetIdentity Entity;
        public byte Faction;
        public int Health, Damage;
        public float X, Z;
    }
    public struct SquadTargetMember
    {
        public bool Eligible;
        public float X, Z, Range;
    }
    public struct SquadTargetInput
    {
        public byte Faction;
        public float OwnBaseX, OwnBaseZ, DefenseRadius, ElapsedSeconds, FirstAttackSeconds;
        public SquadTargetIdentity OpposingBase;
    }
    public struct SquadTargetResult
    {
        public SquadTargetIdentity Target;
        public bool Threatened;
    }
    // Candidate/member order is observable: equal-distance candidates keep the first hit.
    public interface ISquadTargetContext
    {
        int CandidateCount { get; }
        int MemberCount { get; }
        SquadTargetCandidate Candidate(int index);
        SquadTargetMember Member(int index);
    }
    public static class SkirmishSquadTargetingKernel
    {
        public static SquadTargetResult Select<T>(ref T context, in SquadTargetInput input)
            where T : struct, ISquadTargetContext
        {
            var result = new SquadTargetResult { Target = input.OpposingBase };
            float best = input.DefenseRadius * input.DefenseRadius;
            for (int i = 0; i < context.CandidateCount; i++)
            {
                var candidate = context.Candidate(i);
                if (candidate.Faction == 0 || candidate.Faction == input.Faction || candidate.Health <= 0 || candidate.Damage <= 0) continue;
                float distance = DistanceSq(candidate.X, candidate.Z, input.OwnBaseX, input.OwnBaseZ);
                if (distance >= best) continue;
                best = distance; result.Target = candidate.Entity; result.Threatened = true;
            }
            if (!result.Threatened)
            {
                best = float.MaxValue;
                for (int m = 0; m < context.MemberCount; m++)
                {
                    var member = context.Member(m);
                    if (!member.Eligible) continue;
                    float radius = member.Range + 8;
                    for (int i = 0; i < context.CandidateCount; i++)
                    {
                        var candidate = context.Candidate(i);
                        if (candidate.Faction == 0 || candidate.Faction == input.Faction || candidate.Health <= 0 || candidate.Damage <= 0) continue;
                        float distance = DistanceSq(candidate.X, candidate.Z, member.X, member.Z);
                        if (distance >= radius * radius || distance >= best) continue;
                        best = distance; result.Target = candidate.Entity; result.Threatened = true;
                    }
                }
            }
            if (!result.Threatened && input.ElapsedSeconds < input.FirstAttackSeconds) result.Target = default;
            return result;
        }
        private static float DistanceSq(float ax, float az, float bx, float bz)
        {
            // Materialize the same float2 subtraction as the native math.distancesq call.
            var delta = new ScalarPair { X = ax - bx, Z = az - bz };
            return delta.X * delta.X + delta.Z * delta.Z;
        }
        private struct ScalarPair { public float X, Z; }
    }
}
