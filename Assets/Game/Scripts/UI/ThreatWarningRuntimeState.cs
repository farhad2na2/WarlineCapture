namespace Game.Scripts.UI
{
    public enum ThreatWarningType : byte
    {
        Ground = 1,
        Air = 2
    }

    public static class ThreatWarningRuntimeState
    {
        public static bool HasPendingWarning { get; private set; }
        public static ThreatWarningType PendingType { get; private set; }
        public static float PendingEtaSeconds { get; private set; }
        public static int PendingThreatCount { get; private set; }

        public static void RequestWarning(ThreatWarningType type, float etaSeconds, int threatCount)
        {
            HasPendingWarning = true;
            PendingType = type;
            PendingEtaSeconds = etaSeconds < 0f ? 0f : etaSeconds;
            PendingThreatCount = threatCount < 1 ? 1 : threatCount;
        }

        public static void ClearPendingWarning()
        {
            HasPendingWarning = false;
        }

        public static void Reset()
        {
            HasPendingWarning = false;
            PendingType = ThreatWarningType.Ground;
            PendingEtaSeconds = 0f;
            PendingThreatCount = 0;
        }
    }
}
