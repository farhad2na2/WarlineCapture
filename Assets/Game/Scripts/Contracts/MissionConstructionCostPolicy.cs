namespace Game.Tactical.Contracts
{
    // Version 2 is an authored complete Materials price list for the three
    // migrated defense missions. Version 1 retains M02/Skirmish's existing costs.
    public static class MissionConstructionCostPolicy
    {
        public const byte Legacy = 0, MaterialsOnly = 1, DefenseMaterialsV1 = 2;

        public static byte ForMission(string missionId) => missionId switch
        {
            "saga.ch01.m03.radar_warning" or "saga.ch04.m01.air_corridor" or
                "saga.ch04.m02.steel_push" => DefenseMaterialsV1,
            "saga.ch01.m02.establish_base" or "skirmish.base_assault" => MaterialsOnly,
            _ => Legacy
        };

        public static bool TryResolve(byte policy, int credits, int materials,
            out int effectiveCredits, out int effectiveMaterials)
        {
            effectiveCredits = credits;
            effectiveMaterials = materials;
            if (credits < 0 || materials < 0 || policy > DefenseMaterialsV1) return false;
            if (policy == Legacy) return true;
            effectiveCredits = 0;
            if (credits == 0) return true;
            if (policy == MaterialsOnly) return materials > 0;
            // Match both original components. A changed or new Credit price must
            // be deliberately authored, never silently dropped or converted 1:1.
            effectiveMaterials = (credits, materials) switch
            {
                (40000, 90) => 120, // Barracks
                (22000, 50) => 70,  // Guard Tower
                (6000, 15) => 20,   // Road barrier
                (10000, 20) => 30,  // Canonical four-rifle order
                _ => -1
            };
            return effectiveMaterials >= 0;
        }
    }
}
