using Unity.Collections;
using Unity.Entities;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public static class ThreatWarningAudioEventUtility
    {
        public static bool TryEmit(
            EntityManager entityManager,
            ThreatWarningType warningType,
            float etaSeconds,
            int threatCount,
            float requestedAt)
        {
            if (!TryResolve(
                    warningType,
                    etaSeconds,
                    threatCount,
                    out string eventId,
                    out uint eventHash,
                    out AudioPlaybackPriority priority,
                    out float cooldownSeconds))
            {
                return false;
            }

            AudioEventRequestSystem.EnqueueOneShot(
                entityManager,
                new FixedString64Bytes(eventId),
                eventHash,
                new FixedString32Bytes("Voice"),
                priority,
                requestedAt,
                cooldownSeconds);
            return true;
        }

        public static bool TryResolve(
            ThreatWarningType warningType,
            float etaSeconds,
            int threatCount,
            out string eventId,
            out uint eventHash,
            out AudioPlaybackPriority priority,
            out float cooldownSeconds)
        {
            eventId = null;
            eventHash = 0u;
            priority = AudioPlaybackPriority.High;
            cooldownSeconds = 3f;

            if (warningType != ThreatWarningType.Ground && warningType != ThreatWarningType.Air)
                return false;

            bool critical = etaSeconds <= 0.01f || threatCount > 1;
            if (warningType == ThreatWarningType.Air)
            {
                eventId = AudioEventIds.VOARIAMessageWarningAirAttackType;
                eventHash = AudioEventIds.VOARIAMessageWarningAirAttackTypeHash;
            }
            else
            {
                eventId = AudioEventIds.VOARIAMessageWarningGroundAttackType;
                eventHash = AudioEventIds.VOARIAMessageWarningGroundAttackTypeHash;
            }

            if (critical)
            {
                priority = AudioPlaybackPriority.Critical;
                cooldownSeconds = 4f;
            }

            return true;
        }
    }
}
