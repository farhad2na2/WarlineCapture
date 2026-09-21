#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP0Validation
    {
        public const string PassMarker = OperationsP0Checks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsP0Checks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsP0Validation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
