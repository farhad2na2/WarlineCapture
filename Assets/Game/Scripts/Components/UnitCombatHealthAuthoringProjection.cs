namespace Game.Components
{
    /// <summary>
    /// Shared data-only mapping for the UnitCombat and UnitHealth components authored
    /// by UnitGridAuthoring. Inputs are already the current authoring getter results;
    /// this projection adds no clamps or defaults of its own.
    /// </summary>
    public static class UnitCombatHealthAuthoringProjection
    {
        public static UnitCombat FromConfiguredCombatValues(
            int aggroRangeCells,
            float chaseBreakDistance,
            bool canAttack,
            bool allowAutoEngage)
        {
            return new UnitCombat
            {
                AggroRangeCells = aggroRangeCells,
                ChaseBreakDistance = chaseBreakDistance,
                CanAttack = (byte)(canAttack ? 1 : 0),
                AutoEngage = (byte)(allowAutoEngage ? 1 : 0)
            };
        }

        public static UnitHealth FromConfiguredMaxHealth(int maxHealth) =>
            new UnitHealth { Current = maxHealth, Max = maxHealth };
    }
}
