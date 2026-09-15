using System;
using UnityEngine;

public static class M03RadarToolbarAndVoiceValidation
{
    public static void Run()
    {
        try
        {
            using (ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode(); M03RadarPingTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0) throw new Exception("Radar transaction regression failed.");
                ValidationExit.ClearLastExitCode(); MatchCommandAudioFeedbackTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0) throw new Exception("Command voice regression failed.");
                ValidationExit.ClearLastExitCode(); MissionReadinessArchitectureValidation.Run();
                if (ValidationExit.LastExitCode != 0) throw new Exception("Mission architecture regression failed.");
            }
            Debug.Log("[M03RadarToolbarAndVoiceValidation] result=Passed radar,command-audio,architecture");
            ValidationExit.Passed();
        }
        catch (Exception error)
        { Debug.LogException(error); Debug.LogError("[M03RadarToolbarAndVoiceValidation] result=Failed"); ValidationExit.Failed(); }
    }
}
