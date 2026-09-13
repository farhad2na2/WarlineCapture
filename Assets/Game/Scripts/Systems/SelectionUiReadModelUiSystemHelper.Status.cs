using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed partial class SelectionUiReadModelUiSystemHelper
    {
        private static FocusedUnitUiStatus ToFocusedUnitUiStatus(int status)
        {
            return (SelectionUiReadModelLookup.FocusedUnitUiStatus)status switch
            {
                SelectionUiReadModelLookup.FocusedUnitUiStatus.Moving => FocusedUnitUiStatus.Moving,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.Engaged => FocusedUnitUiStatus.Engaged,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.ReturningToBase => FocusedUnitUiStatus.ReturningToBase,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched => FocusedUnitUiStatus.MissileLaunched,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.AirspaceClear => FocusedUnitUiStatus.AirspaceClear,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.TrackingAirTarget => FocusedUnitUiStatus.TrackingAirTarget,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.InterceptingMissile => FocusedUnitUiStatus.InterceptingMissile,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.AirDefenseReloading => FocusedUnitUiStatus.AirDefenseReloading,
                _ => FocusedUnitUiStatus.Idle
            };
        }

        private static TacticalCommandReasonCode ToReasonCode(int reason)
        {
            return System.Enum.IsDefined(typeof(TacticalCommandReasonCode), reason)
                ? (TacticalCommandReasonCode)reason
                : TacticalCommandReasonCode.CommandUnavailable;
        }
    }
}
