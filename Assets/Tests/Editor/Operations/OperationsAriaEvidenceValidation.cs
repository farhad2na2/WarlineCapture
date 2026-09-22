#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsAriaEvidenceValidation
    {
        public const string PassMarker = OperationsAriaEvidenceChecks.PassMarker;

        public static void RunFocusedValidation()
        {
            try
            {
                OperationsAriaEvidenceChecks.RunAll();
                Debug.Log(PassMarker);
            }
            catch (Exception exception)
            {
                Debug.LogError("[OperationsAriaEvidenceValidation] result=Failed");
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
