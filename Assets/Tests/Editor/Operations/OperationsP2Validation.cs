#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP2Validation
    {
        public const string PassMarker = OperationsP2Checks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsP2Checks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsP2Validation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
