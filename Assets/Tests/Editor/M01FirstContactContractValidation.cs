#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using UnityEngine;

public static class M01FirstContactContractValidation
{
    public const int ExpectedSuites = 23;
    public const string PassMarker =
        "[M01FirstContactConsolidatedContractValidation] result=Passed suites=23";

    public static void RunFocusedValidation()
    {
        try
        {
            RunSuite(M01FirstContactContractTests.RunFocusedValidation);
            RunSuite(M01FirstContactLaunchPayloadTests.RunFocusedValidation);
            RunSuite(M01FirstContactScenarioCompatibilityTests.RunFocusedValidation);
            RunSuite(M01FirstContactMissionRuleTests.RunFocusedValidation);
            RunSuite(M01FirstContactProgressStoreTests.RunFocusedValidation);
            RunSuite(M01FirstContactCanonicalDataTests.RunFocusedValidation);
            RunSuite(M01FirstContactMapSourceBindingTests.RunFocusedValidation);
            RunSuite(M01FirstContactOperationMapTests.RunFocusedValidation);
            RunSuite(M01FirstContactCameraMinimapTests.RunFocusedValidation);
            RunSuite(M01FirstContactAnchorTests.RunFocusedValidation);
            RunSuite(M01FirstContactCameraContinuityTests.RunFocusedValidation);
            RunSuite(M01FirstContactDenseCityReuseTests.RunFocusedValidation);
            RunSuite(M01FirstContactRuntimeOwnershipTests.RunFocusedValidation);
            RunSuite(M01FirstContactLaunchBootstrapTests.RunFocusedValidation);
            RunSuite(M01FirstContactObjectiveWriterTests.RunFocusedValidation);
            RunSuite(M01FirstContactResultRuleTests.RunFocusedValidation);
            RunSuite(M01FirstContactSettlementTests.RunFocusedValidation);
            RunSuite(M01FirstContactGuidanceTests.RunFocusedValidation);
            RunSuite(M01FirstContactFirstLaunchHandoffTests.RunFocusedValidation);
            RunSuite(M01FirstContactNarrativeTests.RunFocusedValidation);
            RunSuite(M01FirstContactCampaignUiTests.RunFocusedValidation);
            RunSuite(M01FirstContactMissionBriefingTests.RunFocusedValidation);
            RunSuite(M01FirstContactHudResultTests.RunFocusedValidation);

            Debug.Log(PassMarker);
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("[M01FirstContactConsolidatedContractValidation] result=Failed");
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
