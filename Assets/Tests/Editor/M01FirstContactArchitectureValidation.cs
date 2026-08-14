#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using UnityEngine;

public static class M01FirstContactArchitectureValidation
{
    public const string PassMarker =
        "[M01FirstContactArchitectureValidation] result=Passed suites=3 contractSuites=23 sourceGrowthTests=17 architectureSuites=23";

    public static void RunFocusedValidation()
    {
        try
        {
            RunSuite(M01FirstContactContractValidation.RunFocusedValidation);
            RunSuite(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            RunSuite(ArchitectureHardeningCloseoutValidationRunner.RunFocusedValidation);

            Debug.Log(PassMarker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("[M01FirstContactArchitectureValidation] result=Failed");
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
