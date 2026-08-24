#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using UnityEngine;

public static class M02EstablishBaseContractValidation
{
    public const string PassMarker =
        "[M02EstablishBaseConsolidatedDataValidation] result=Passed suites=5";

    public static void RunFocusedValidation()
    {
        try
        {
            RunSuite(M02EstablishBaseContractTests.RunFocusedValidation);
            RunSuite(M02EstablishBaseScenarioTests.RunFocusedValidation);
            RunSuite(M02EstablishBaseBarracksProductionTests.RunFocusedValidation);
            RunSuite(M02EstablishBaseCanonicalDataTests.RunFocusedValidation);
            RunSuite(M02EstablishBaseOperationMapTests.RunFocusedValidation);
            Debug.Log(PassMarker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("[M02EstablishBaseConsolidatedDataValidation] result=Failed");
            Debug.LogException(exception);
            ValidationExit.Exit(1);
        }
    }

    private static void RunSuite(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
        {
            validation();
        }

        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
        {
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
        }
    }
}
#endif
