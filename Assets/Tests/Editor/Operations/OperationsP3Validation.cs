#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP3Validation
    {
        public const string PassMarker = OperationsP3Checks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsP3Checks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsP3Validation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
