namespace Game.Runtime
{
    public static class PerformanceDiagnosticsCapturePolicy
    {
        public static bool SuppressLogging { get; private set; }

        public static void SetSuppressLogging(bool suppress) => SuppressLogging = suppress;
    }
}
