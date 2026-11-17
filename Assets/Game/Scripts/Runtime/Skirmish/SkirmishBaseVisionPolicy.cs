using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>
    /// Fog reveal and alert perimeter around a designated main base (Base Assault, both
    /// factions). The radius is the smallest that shows any reference direct-fire attacker
    /// able to hit the base, using UnitAttackSystem's own fire test (UnitAttackSystem.cs:267-275):
    /// centre distance &lt;= attackRange + attackerCombatRadius + targetCombatRadius + 0.25 cell.
    ///
    /// Reference attacker: the Battle Tank (Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab,
    /// attackRange 50, footprintCells 3 x 3). It is the longest-ranged direct-fire ground unit
    /// fielded by the Base Assault armies. Missile launchers and aircraft keep their own
    /// detector and warning paths and are not covered by this radius.
    /// </summary>
    public static class SkirmishBaseVisionPolicy
    {
        public const float ReferenceAttackRangeMetres = 50f;
        public static readonly int2 ReferenceAttackerFootprint = new(3, 3);

        /// <summary>Same rule as UnitAttackSystem.GetCombatRadius.</summary>
        public static float CombatRadius(int2 footprint, float cellSize)
        {
            int2 clamped = UnitFootprintUtility.ClampSize(footprint);
            float halfWidth = math.max(0f, (clamped.x - 1) * 0.5f * cellSize);
            float halfDepth = math.max(0f, (clamped.y - 1) * 0.5f * cellSize);
            return math.max(halfWidth, halfDepth);
        }

        public static float RevealRadius(int2 baseFootprint, float cellSize)
        {
            float cell = math.max(0f, cellSize);
            return ReferenceAttackRangeMetres + CombatRadius(ReferenceAttackerFootprint, cell) +
                   CombatRadius(baseFootprint, cell) + cell * 0.25f;
        }
    }
}
