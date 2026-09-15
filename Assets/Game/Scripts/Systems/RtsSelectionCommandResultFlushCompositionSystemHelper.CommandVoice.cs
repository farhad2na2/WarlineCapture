using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed partial class RtsSelectionCommandResultFlushCompositionSystemHelper
    {
        internal static bool TryEmitCommandConfirmationVoice(Context context, RtsSelectionCommandIntentKind kind, bool accepted)
        {
            if (context.TryGetDefaultEntityManager?.Invoke(out EntityManager em) != true)
                return false;

            return TryEmitCommandConfirmationVoice(em, kind, accepted);
        }

        internal static bool TryEmitCommandConfirmationVoice(EntityManager em, RtsSelectionCommandIntentKind kind, bool accepted)
        {
            if (!TryResolveCommandConfirmationVoiceEvent(kind, accepted, out string eventId, out uint eventHash))
                return false;

            return TryEmitAriaVoice(em, eventId, eventHash);
        }

        internal static bool TryResolveCommandConfirmationVoiceEvent(
            RtsSelectionCommandIntentKind kind,
            bool accepted,
            out string eventId,
            out uint eventHash)
        {
            eventId = string.Empty;
            eventHash = 0u;
            if (!accepted)
                return false;

            switch (kind)
            {
                case RtsSelectionCommandIntentKind.Move:
                    eventId = AudioEventIds.VOARIAMessageTacticalBannerAcceptedMoveTitle;
                    eventHash = AudioEventIds.VOARIAMessageTacticalBannerAcceptedMoveTitleHash;
                    return true;
                case RtsSelectionCommandIntentKind.Attack:
                    eventId = AudioEventIds.VOARIAMessageTacticalBannerAcceptedAttackTitle;
                    eventHash = AudioEventIds.VOARIAMessageTacticalBannerAcceptedAttackTitleHash;
                    return true;
                case RtsSelectionCommandIntentKind.HoldPosition:
                    eventId = AudioEventIds.VOARIAMessageTacticalBannerAcceptedHoldTitle;
                    eventHash = AudioEventIds.VOARIAMessageTacticalBannerAcceptedHoldTitleHash;
                    return true;
                case RtsSelectionCommandIntentKind.Stop:
                    eventId = AudioEventIds.VOARIAMessageTacticalFeedbackStoppedSelectedUnits;
                    eventHash = AudioEventIds.VOARIAMessageTacticalFeedbackStoppedSelectedUnitsHash;
                    return true;
                case RtsSelectionCommandIntentKind.Scan:
                    eventId = AudioEventIds.VOARIAMessageTacticalBannerAcceptedScanTitle;
                    eventHash = AudioEventIds.VOARIAMessageTacticalBannerAcceptedScanTitleHash;
                    return true;
                default:
                    return false;
            }
        }
    }
}
