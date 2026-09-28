namespace Game.Runtime
{
    public static class MissionProductProfileMigration
    {
        public const int Version = 1;
        public static void Normalize(PlayerProfileSaveData profile)
        {
            if (profile == null || profile.missionProductMigrationVersion >= Version) return;
            // Preserve all earned balances, unlocks, parts and settlement receipts.
            // Legacy account Materials/Fuel/Intel are archival data; tactical
            // initialization and affordability never read these profile fields.
            profile.missionProductMigrationVersion = Version;
        }
    }
}
