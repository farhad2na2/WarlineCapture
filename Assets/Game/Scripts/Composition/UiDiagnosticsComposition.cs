using Game.UI.Contracts;
using UnityEngine;

namespace Game.Composition
{
    internal static class UiDiagnosticsComposition
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Bind() => UiDiagnostics.Bind(Report);

        private static void Report(UiDiagnosticSeverity severity, string message, Object context)
        {
            switch (severity)
            {
                case UiDiagnosticSeverity.Error: Debug.LogError(message, context); break;
                case UiDiagnosticSeverity.Warning: Debug.LogWarning(message, context); break;
                default: Debug.Log(message, context); break;
            }
        }
    }
}
