#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Focused Editor validation for O001–O003 mobile-ready content.
    /// Pass marker is checks=8 (coach, escort, repair, result, partial, O003, practice).
    /// Landed presentation/pacing guards run in the same pass before that marker.
    /// </summary>
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
                Debug.LogError("[OperationsMobileReadyValidation] result=Failed " + exception.Message);
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
#endif
