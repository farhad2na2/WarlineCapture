namespace Game.Components
{
    public static class FactionIdentity
    {
        public const byte NeutralFactionId = 0;
        public const byte PlayerFactionId = 1;
        public const byte EnemyFactionId = 2;

        public static bool IsNeutral(byte factionId)
        {
            return factionId == NeutralFactionId;
        }

        public static bool IsPlayerControlled(byte factionId)
        {
            return factionId == PlayerFactionId;
        }

        public static bool IsAiControlledByDefault(byte factionId)
        {
            return factionId != NeutralFactionId && factionId != PlayerFactionId;
        }

        public static bool IsHostileToPlayer(byte factionId)
        {
            return factionId != NeutralFactionId && factionId != PlayerFactionId;
        }

        public static bool CanAutoTargetForCombat(byte sourceFactionId, byte targetFactionId)
        {
            return sourceFactionId != NeutralFactionId &&
                   targetFactionId != NeutralFactionId &&
                   sourceFactionId != targetFactionId;
        }

        public static byte ResolveDefaultTargetFaction(byte factionId)
        {
            return IsPlayerControlled(factionId) ? EnemyFactionId : PlayerFactionId;
        }
    }
}
