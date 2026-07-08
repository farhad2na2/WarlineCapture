using System;
using NUnit.Framework;
using UnityEngine;

public sealed class AssistantRolloutValidationTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(nameof(AssistantEcsDataContractTests), AssistantEcsDataContractTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(AssistantReadModelSystemTests), AssistantReadModelSystemTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(AssistantCommandIntentGatewayTests), AssistantCommandIntentGatewayTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(AssistantCommandIntentSystemTests), AssistantCommandIntentSystemTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(AssistantControlOwnerSystemTests), AssistantControlOwnerSystemTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(AssistantMessagePrioritySystemTests), AssistantMessagePrioritySystemTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(AssistantNarrationRequestSystemTests), AssistantNarrationRequestSystemTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(AssistantSettingsPersistenceSystemTests), AssistantSettingsPersistenceSystemTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(MatchHudAssistantUiSystemHelperTests), MatchHudAssistantUiSystemHelperTests.RunFocusedValidation, ref passed);
            RunValidationStep(nameof(SettingsPopupValidationTests), SettingsPopupValidationTests.RunFocusedValidation, ref passed);

            Debug.Log($"[AssistantRolloutValidation] result=Passed validations={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantRolloutValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AssistantRolloutFocusedValidation_PassesAllSlices()
    {
        RunFocusedValidation();
    }

    private static void RunValidationStep(string name, Action validation, ref int passed)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
        {
            validation();
        }

        int exitCode = ValidationExit.LastExitCode.GetValueOrDefault();
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"{name} failed with validation exit code {exitCode}.");
        }

        passed++;
        Debug.Log($"[AssistantRolloutValidation] step=Passed name={name}");
    }
}
