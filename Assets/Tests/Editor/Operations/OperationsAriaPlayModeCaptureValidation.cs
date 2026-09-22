#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsAriaPlayModeCaptureValidation
    {
        public const string PassMarker = OperationsAriaPlayModeCaptureChecks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsAriaPlayModeCaptureChecks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsAriaPlayModeCaptureValidation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
