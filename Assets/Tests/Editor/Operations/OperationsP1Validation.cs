#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP1Validation
    {
        public const string PassMarker = OperationsP1Checks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsP1Checks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsP1Validation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
