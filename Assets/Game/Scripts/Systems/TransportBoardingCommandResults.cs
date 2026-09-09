using Unity.Collections;
using Unity.Mathematics;
using Game.Tactical.Contracts;

namespace Game.Runtime
{
    public partial struct TransportBoardingCommandSystem
    {
        public readonly struct Result
        {
            public readonly bool Accepted;
            public readonly TacticalCommandReasonCode ReasonCode;
            public readonly int2 MarkerCell;
            public readonly float3 MarkerPosition;
            public readonly byte MarkerFactionId;
            public readonly FixedString512Bytes Message;

            private Result(bool accepted, TacticalCommandReasonCode reasonCode, int2 markerCell, float3 markerPosition, byte markerFactionId, FixedString512Bytes message)
            {
                Accepted = accepted;
                ReasonCode = reasonCode;
                MarkerCell = markerCell;
                MarkerPosition = markerPosition;
                MarkerFactionId = markerFactionId;
                Message = message;
            }

            public static Result Rejected()
            {
                return Rejected(TacticalCommandReasonCode.CommandUnavailable);
            }

            public static Result Rejected(TacticalCommandReasonCode reasonCode, string message = null)
            {
                string displayMessage = !string.IsNullOrWhiteSpace(message)
                    ? message
                    : ResolveReasonText(reasonCode);
                return new Result(false, reasonCode, default, default, 0, new FixedString512Bytes(displayMessage ?? string.Empty));
            }

            public static Result AcceptedAt(int2 markerCell, float3 markerPosition, byte markerFactionId, string message = null)
            {
                return new Result(true, TacticalCommandReasonCode.None, markerCell, markerPosition, markerFactionId, new FixedString512Bytes(message ?? string.Empty));
            }
        }

        private readonly struct DisembarkResult
        {
            public readonly bool Accepted;
            public readonly TacticalCommandReasonCode ReasonCode;
            public readonly bool ShowFeedback;
            public readonly FixedString512Bytes Message;

            private DisembarkResult(bool accepted, TacticalCommandReasonCode reasonCode, bool showFeedback, FixedString512Bytes message)
            {
                Accepted = accepted;
                ReasonCode = reasonCode;
                ShowFeedback = showFeedback;
                Message = message;
            }

            public static DisembarkResult Success(string message = null)
            {
                return new DisembarkResult(true, TacticalCommandReasonCode.None, false, new FixedString512Bytes(message ?? string.Empty));
            }

            public static DisembarkResult Rejected(TacticalCommandReasonCode reasonCode, bool showFeedback = true, string message = null)
            {
                string displayMessage = !string.IsNullOrWhiteSpace(message)
                    ? message
                    : ResolveReasonText(reasonCode);
                return new DisembarkResult(false, reasonCode, showFeedback, new FixedString512Bytes(displayMessage ?? string.Empty));
            }
        }

    }
}
