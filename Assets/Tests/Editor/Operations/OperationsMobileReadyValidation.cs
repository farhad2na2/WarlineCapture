#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsMobileReadyValidation
    {
        public const string PassMarker = OperationsMobileReadyChecks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsMobileReadyChecks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsMobileReadyValidation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
