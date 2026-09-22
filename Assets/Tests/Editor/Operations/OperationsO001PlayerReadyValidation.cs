#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsO001PlayerReadyValidation
    {
        public const string PassMarker = OperationsO001PlayerReadyChecks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsO001PlayerReadyChecks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsO001PlayerShellValidation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
