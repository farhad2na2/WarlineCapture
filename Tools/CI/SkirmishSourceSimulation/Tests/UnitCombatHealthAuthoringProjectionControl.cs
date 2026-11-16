using System;
using System.Globalization;
using Game.Components;

internal static class UnitCombatHealthAuthoringProjectionControl
{
    private static int Main(string[] args)
    {
        if (args.Length != 5)
            return Fail("expected current getter inputs: aggro, chase-break, can-attack, auto-engage, max-health");

        int aggro = int.Parse(args[0], CultureInfo.InvariantCulture);
        float chaseBreak = float.Parse(args[1], CultureInfo.InvariantCulture);
        bool canAttack = args[2] == "1";
        bool allowAutoEngage = args[3] == "1";
        int maxHealth = int.Parse(args[4], CultureInfo.InvariantCulture);

        UnitCombat actualCombat = UnitCombatHealthAuthoringProjection.FromConfiguredCombatValues(
            aggro, chaseBreak, canAttack, allowAutoEngage);
        UnitHealth actualHealth = UnitCombatHealthAuthoringProjection.FromConfiguredMaxHealth(maxHealth);
        Require(actualCombat.AggroRangeCells == aggro, "current authored aggro value preserved");
        Require(actualCombat.ChaseBreakDistance == chaseBreak, "current authored chase-break value preserved");
        Require(actualCombat.CanAttack == (byte)(canAttack ? 1 : 0), "current authored CanAttack encoded");
        Require(actualCombat.AutoEngage == (byte)(allowAutoEngage ? 1 : 0), "current authored AutoEngage encoded");
        Require(actualHealth.Current == maxHealth && actualHealth.Max == maxHealth,
            "current authored max health projected to Current and Max");

        UnitCombat zeroCombat = UnitCombatHealthAuthoringProjection.FromConfiguredCombatValues(0, 0f, false, false);
        UnitHealth minimumHealth = UnitCombatHealthAuthoringProjection.FromConfiguredMaxHealth(1);
        Require(zeroCombat.AggroRangeCells == 0 && zeroCombat.ChaseBreakDistance == 0f &&
                zeroCombat.CanAttack == 0 && zeroCombat.AutoEngage == 0,
            "zero and false boundary values preserved");
        Require(minimumHealth.Current == 1 && minimumHealth.Max == 1, "minimum health boundary preserved");

        UnitCombat maximumCombat = UnitCombatHealthAuthoringProjection.FromConfiguredCombatValues(
            int.MaxValue, float.MaxValue, true, true);
        UnitHealth maximumHealth = UnitCombatHealthAuthoringProjection.FromConfiguredMaxHealth(int.MaxValue);
        Require(maximumCombat.AggroRangeCells == int.MaxValue && maximumCombat.ChaseBreakDistance == float.MaxValue &&
                maximumCombat.CanAttack == 1 && maximumCombat.AutoEngage == 1,
            "maximum input boundary values preserved");
        Require(maximumHealth.Current == int.MaxValue && maximumHealth.Max == int.MaxValue,
            "maximum health boundary preserved");

        RejectWrongExpected(actualCombat.AggroRangeCells != aggro + 1, "wrong aggro expected value rejected");
        RejectWrongExpected(actualCombat.AutoEngage != (byte)(allowAutoEngage ? 0 : 1),
            "wrong AutoEngage expected value rejected");
        RejectWrongExpected(actualHealth.Max != maxHealth + 1, "wrong health expected value rejected");

        Console.WriteLine("[UnitCombatHealthAuthoringProjectionControl] result=Passed current=Alt04 boundaries=0,1,MaxValue negativeControls=3 scope=UnitCombat,UnitHealth");
        return 0;
    }

    private static void RejectWrongExpected(bool mismatch, string label)
    {
        bool rejected = false;
        try { Require(!mismatch, label); }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected, label + " negative control");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("assertion failed: " + label);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine("[UnitCombatHealthAuthoringProjectionControl] result=Failed " + message);
        return 1;
    }
}
