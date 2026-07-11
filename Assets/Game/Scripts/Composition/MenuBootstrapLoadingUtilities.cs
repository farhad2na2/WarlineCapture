using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Composition
{
    internal sealed partial class MenuBootstrapCompositionSystemHelper
    {
        public void OnApplicationFocus(bool hasFocus)
        {
            if (diagnosticsInitialized)
                performanceDiagnosticsSystem.OnApplicationFocus(hasFocus);
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            if (diagnosticsInitialized)
                performanceDiagnosticsSystem.OnApplicationPause(pauseStatus);
        }

        private static void SetLoading(EntityManager entityManager, Entity boundary, float progress01, bool complete)
        {
            SetLoading(
                entityManager,
                boundary,
                progress01,
                complete,
                complete ? "Command shell ready" : "Loading command shell");
        }

        private static void SetLoading(EntityManager entityManager, Entity boundary, float progress01, bool complete, string status)
        {
            entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
            {
                Progress01 = Mathf.Clamp01(progress01),
                Status = new FixedString64Bytes(ToFixed64Status(status)),
                IsComplete = complete ? (byte)1 : (byte)0
            });
        }

        private static string ToFixed64Status(string status)
        {
            const int MaxAsciiChars = 60;
            if (string.IsNullOrEmpty(status))
                return "Loading";
            return status.Length <= MaxAsciiChars ? status : status.Substring(0, MaxAsciiChars);
        }
    }
}
