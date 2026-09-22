using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Focused host/Editor validation for Operations mobile-ready O001–O003 presentation + pacing.
    /// </summary>
    public static class OperationsMobileReadyValidation
    {
        public static void RunFocusedValidation()
        {
            try
            {
                OperationsMobileReadyChecks.RunAll();
                Debug.Log(OperationsMobileReadyChecks.PassMarker);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[OperationsMobileReadyValidation] result=Failed " + exception.Message);
                throw;
            }
        }
    }
}
