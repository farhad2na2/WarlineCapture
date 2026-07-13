namespace Game.Runtime
{
    public static class PerformanceDiagnosticsCapturePolicy
    {
#if UNITY_EDITOR
        public static bool SuppressLogging { get; private set; }

        public static void SetSuppressLogging(bool suppress) => SuppressLogging = suppress;
#else
        public const bool SuppressLogging = false;
#endif
    }
}
