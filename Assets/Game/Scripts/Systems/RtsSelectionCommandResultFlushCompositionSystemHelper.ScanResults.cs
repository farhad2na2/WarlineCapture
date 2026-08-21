using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class RtsSelectionCommandResultFlushCompositionSystemHelper
    {
        private static Vector2 SelectionPointerPosition(Context context) =>
            context.InputSystem != null ? context.InputSystem.LastPointerPosition : default;

        private static TacticalCommandResult ToScanCommandResult(
            RtsSelectionCommandResultElement result)
        {
            if (result.Accepted == 0)
                return TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode);

            if (result.DeferredToSource != 0)
            {
                return TacticalCommandResult.Success(
                    GameText.Get("tactical.feedback.scan_ordered", "SCAN ORDERED: SCANNER EN ROUTE"));
            }

            string contacts = result.RevealedCount == 1
                ? GameText.Get("tactical.feedback.scan_one_contact", "1 CONTACT")
                : GameText.Format("tactical.feedback.scan_contacts", "{0} CONTACTS", result.RevealedCount);
            return TacticalCommandResult.Success(
                GameText.Format("tactical.feedback.scan_complete", "SCAN COMPLETE: {0}", contacts));
        }
    }
}
