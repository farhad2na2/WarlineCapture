using System;
using System.Diagnostics;

namespace Game.UI.Contracts
{
    public enum UiDiagnosticSeverity { Info, Warning, Error }

    // Composition supplies the sink. Conditional calls remove both logging and message formatting from release players.
    public static class UiDiagnostics
    {
        private static Action<UiDiagnosticSeverity, string, UnityEngine.Object> sink;
        public static void Bind(Action<UiDiagnosticSeverity, string, UnityEngine.Object> value) => sink = value;
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message, UnityEngine.Object context = null) => sink?.Invoke(UiDiagnosticSeverity.Info, message, context);
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(string message, UnityEngine.Object context = null) => sink?.Invoke(UiDiagnosticSeverity.Warning, message, context);
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Error(string message, UnityEngine.Object context = null) => sink?.Invoke(UiDiagnosticSeverity.Error, message, context);
    }
}
