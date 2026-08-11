namespace Game.Runtime
{
    public static class PerformanceDiagnosticsCapturePolicy
    {
        public static bool SuppressLogging { get; private set; }

        public static void SetSuppressLogging(bool suppress) => SuppressLogging = suppress;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBeforeSubsystemRegistration() => SuppressLogging = false;
    }
}
