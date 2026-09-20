namespace Game.Runtime
{
    public sealed partial class SelectionHudFeedbackUiSystemHelper
    {
        private static string ToOrderText(SelectionUiReadModelLookup.FocusedUnitUiStatus status)
        {
            return status switch
            {
                SelectionUiReadModelLookup.FocusedUnitUiStatus.ReturningToBase => Text("selection.order.returning_to_base", "Returning to base"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched => Text("selection.order.missile_launched", "Missile launched"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.AirspaceClear => Text("selection.order.airspace_clear", "Airspace clear"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.TrackingAirTarget => Text("selection.order.tracking_air_target", "Tracking air target"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.InterceptingMissile => Text("selection.order.intercepting_missile", "Intercepting missile"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.AirDefenseReloading => Text("selection.order.reloading", "Reloading"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.Engaged => Text("selection.order.engaging_target", "Engaging target"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.Moving => Text("selection.order.moving", "Moving"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.AttackMoving => Text("selection.order.attack_move", "Advancing and engaging"),
                SelectionUiReadModelLookup.FocusedUnitUiStatus.Holding => Text("mission.m03.order.holding", "Holding position"),
                _ => Text("selection.order.idle", "Idle")
            };
        }

    }
}
