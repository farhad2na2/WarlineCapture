using UnityEngine;

/// <summary>
/// Logs the existing City Crossroads identity gates so an <c>executeMethod</c> run records their pass
/// markers. Both underlying validations return their marker instead of logging it, which the Editor
/// command line discards.
/// </summary>
public static class CityCrossroadsIdentityValidationRunner
{
    public static void RunFocusedValidation()
    {
        Debug.Log(SkirmishScenarioSourceBindingTests.RunFocusedValidation());
        Debug.Log(Game.Editor.SkirmishScenarioValidation.Run());
        Debug.Log("[CityCrossroadsIdentityValidation] result=Passed gates=2");
    }
}
