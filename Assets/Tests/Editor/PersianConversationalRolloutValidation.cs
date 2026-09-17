using System;
using Game.Editor;
using UnityEngine;

public static class PersianConversationalRolloutValidation
{
    public static void Run()
    {
        try
        {
            BuildDrawerV3PrefabBuilder.Build();
            PersianConversationalToneImporter.Validate();
            PersianConversationalToneImporter.ValidateAudio();
            Action[] suites = {
                StartThroughM02LocalizationTests.RunFocusedValidation,
                M03PresentationTests.RunFocusedValidation,
                M02EstablishBaseNarrativeTests.RunFocusedValidation,
                M04ComicAudioRegressionTests.RunFocusedValidation,
                MatchCommandAudioFeedbackTests.RunFocusedValidation,
                FirstLaunchNarrativeTextCatalogTests.RunFocusedValidation
            };
            foreach (var suite in suites)
            {
                using (ValidationExit.SuppressProcessExit())
                {
                    ValidationExit.ClearLastExitCode();
                    suite();
                    if (ValidationExit.LastExitCode.HasValue && ValidationExit.LastExitCode.Value != 0) throw new InvalidOperationException(suite.Method.DeclaringType?.Name);
                }
            }
            Debug.Log("[PersianConversationalRolloutValidation] result=Passed suites=6 captions,layout,voice-routing,comic-timing");
            ValidationExit.Passed();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("[PersianConversationalRolloutValidation] result=Failed");
            ValidationExit.Failed();
        }
    }
}
