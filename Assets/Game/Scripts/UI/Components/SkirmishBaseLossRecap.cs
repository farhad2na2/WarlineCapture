using Game.Skirmish.Contracts;

namespace Game.UI.Runtime
{
    /// <summary>
    /// What destroyed the main base, for the result recap: the role of the last attacker the player
    /// saw hitting the base, only if that hit was recent when the base fell. Never guesses.
    /// </summary>
    public struct SkirmishBaseLossRecap
    {
        public const float RecentSeconds = 10f;
        private SkirmishRoleKind role;
        private float seenAt;

        public void Observe(SkirmishRoleKind attackerRole, bool attackerVisible, bool baseHit, float now)
        {
            if (attackerVisible && baseHit && attackerRole != SkirmishRoleKind.None) { role = attackerRole; seenAt = now; }
        }

        public SkirmishRoleKind Resolve(bool baseLost, float endedAt) =>
            baseLost && role != SkirmishRoleKind.None && endedAt - seenAt >= 0f && endedAt - seenAt <= RecentSeconds
                ? role : SkirmishRoleKind.None;

        public void Reset() => this = default;

        /// <summary>Infantry roles are named as a squad ("an enemy ROCKETEER squad").</summary>
        public static bool IsSquadRole(SkirmishRoleKind role) =>
            role == SkirmishRoleKind.Rifle || role == SkirmishRoleKind.Gunner || role == SkirmishRoleKind.Marksman ||
            role == SkirmishRoleKind.Breacher || role == SkirmishRoleKind.Rocketeer;

        public static string RecapKey(SkirmishRoleKind role) =>
            IsSquadRole(role) ? "ui.skirmish.result.base_lost_to_squad" : "ui.skirmish.result.base_lost_to_unit";

        public static string RecapFallback(SkirmishRoleKind role) =>
            IsSquadRole(role) ? "Your main base was destroyed by an enemy {0} squad." : "Your main base was destroyed by an enemy {0}.";
    }
}
