using System;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Import alongside all three actual reducers/test suites. Invokes the actual
    // NUnit methods through their suites; JsonUtility remains the real Unity API.
    public static class SkirmishSharedObjectiveReducerValidation
    {
        public static void RunFocusedValidation()
        {
            try
            {
                SkirmishFrontlineReducerTests.RunFocusedValidation();
                SkirmishBreakthroughReducerTests.RunFocusedValidation();
                SkirmishConvoyReducerTests.RunFocusedValidation();
                Debug.Log("[SkirmishSharedObjectiveReducerValidation] result=Passed fc=13 bt=15 ce=14 tests=42 actualUnityJson=1 nativeGameplayCertified=0");
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishSharedObjectiveReducerValidation] result=Failed\n" + error);
                throw;
            }
        }
    }
}
