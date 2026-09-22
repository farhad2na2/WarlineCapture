#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP4Validation
    {
        public const string PassMarker = OperationsP4Checks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsP4Checks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsP4Validation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
