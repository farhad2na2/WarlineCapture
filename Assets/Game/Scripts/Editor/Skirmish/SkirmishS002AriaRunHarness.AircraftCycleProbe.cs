using System;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static double airProbePurchaseAt, airProbeServiceAt, airProbeSortieAt, airProbeLastStatusAt;
        private static int airProbePageTurns;

        private static void TickAircraftCycleProbe(double now, SkirmishMatchView skirmish,
            BuildDrawerView drawer, BuildDrawerCatalogRuntimeView catalog,
            MatchOverlayCommandControlsView controls)
        {
            if (!UiShellRuntimeGateway.TryReadSkirmish(out UiSkirmishModel model)) return;
            if (now - airProbeLastStatusAt >= 10d)
            {
                airProbeLastStatusAt = now;
                Debug.Log("[SkirmishS003AircraftCycleProbe] stage=" + helipadProbeStage +
                    " materials=" + model.OwnMaterials + " truck=" + model.LogisticsTruckCommitted +
                    " canQueueAir=" + model.CanQueueAir + " airPending=" + model.AirRecruitPending +
                    " airLive=" + model.OwnAttackAirLive + " airActive=" + model.OwnAttackAirActive +
                    " airLanded=" + model.OwnAttackAirLanded + " fuel=" + model.OwnAirFuel);
            }
            if (model.Finished) { EndHelipadProbe(false, "matchEndedBeforeSecondSortie"); return; }
            if (!model.PadPresent) { EndHelipadProbe(false, "padLostBeforeSecondSortie"); return; }

            if (helipadProbeStage == 10)
            {
                if (model.CanQueueAir || model.AirRecruitPending || model.OwnAttackAirLive > 0)
                { helipadProbeStage = 11; return; }
                if (!model.LogisticsTruckCommitted && model.CanAffordLogisticsTruck &&
                    now - airProbePurchaseAt >= 6d)
                    TouchProbeCatalog(now, drawer, catalog, controls,
                        BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tray");
                return;
            }
            if (helipadProbeStage == 11)
            {
                if (model.AirRecruitPending || model.OwnAttackAirLive > 0)
                { helipadProbeStage = 12; return; }
                if (model.CanQueueAir && now - airProbePurchaseAt >= 6d)
                    TouchProbeCatalog(now, drawer, catalog, controls,
                        BuildDrawerCategory.Aircrafts, "Unit_Veh_Helicopter_Attack_Small");
                return;
            }
            if (helipadProbeStage == 12)
            {
                if (model.OwnAttackAirLive == 0) return;
                Debug.Log("[SkirmishS003AircraftCycleProbe] paidDelivery=Observed aircraft=" + model.OwnAttackAirLive);
                helipadProbeStage = 13;
            }
            if (helipadProbeStage == 13)
            {
                if (drawer != null && drawer.IsOpen)
                { TapHelipadButton(drawer.CloseButton, now); return; }
                if (!SelectPresentedAircraft(now)) return;
                helipadProbeStage = 14;
                return;
            }
            if (helipadProbeStage == 14)
            {
                if (TapHelipadButton(skirmish.EnemyFocusButton, now)) helipadProbeStage = 15;
                return;
            }
            if (helipadProbeStage == 15)
            {
                if (!skirmish.EnemyBaseMarkerVisible) return;
                if (TapHelipadButton(controls?.AttackButton, now)) helipadProbeStage = 16;
                return;
            }
            if (helipadProbeStage == 16)
            {
                if (TouchEnemyBase(now, skirmish))
                { helipadProbeStage = 17; airProbeSortieAt = now; }
                return;
            }
            if (helipadProbeStage == 17)
            {
                if (model.OwnAttackAirActive == 0) return;
                if (now - airProbeSortieAt < 15d) return;
                Debug.Log("[SkirmishS003AircraftCycleProbe] firstSortie=Observed active=" + model.OwnAttackAirActive);
                helipadProbeStage = 18;
            }
            if (helipadProbeStage == 18)
            {
                if (!SelectPresentedAircraft(now)) return;
                var panel = UnityEngine.Object.FindAnyObjectByType<MatchHudSelectionPanelView>(FindObjectsInactive.Include);
                if (TapHelipadButton(panel?.PresentedReturnButton, now)) helipadProbeStage = 19;
                return;
            }
            if (helipadProbeStage == 19)
            {
                if (model.OwnAttackAirActive != 0 || model.OwnAttackAirLanded == 0) return;
                Debug.Log("[SkirmishS003AircraftCycleProbe] touchdown=Observed landed=" + model.OwnAttackAirLanded);
                airProbeServiceAt = now + 5d;
                helipadProbeStage = 20;
            }
            if (helipadProbeStage == 20)
            {
                if (now < airProbeServiceAt || model.OwnAirFuel <= 0) return;
                Debug.Log("[SkirmishS003AircraftCycleProbe] homeFuelAvailable=" + model.OwnAirFuel);
                helipadProbeStage = 21;
            }
            if (helipadProbeStage == 21)
            {
                if (!SelectPresentedAircraft(now)) return;
                helipadProbeStage = 22;
                return;
            }
            if (helipadProbeStage == 22)
            {
                if (TapHelipadButton(skirmish.EnemyFocusButton, now)) helipadProbeStage = 23;
                return;
            }
            if (helipadProbeStage == 23)
            {
                if (!skirmish.EnemyBaseMarkerVisible) return;
                if (TapHelipadButton(controls?.AttackButton, now)) helipadProbeStage = 24;
                return;
            }
            if (helipadProbeStage == 24)
            {
                if (TouchEnemyBase(now, skirmish)) helipadProbeStage = 25;
                return;
            }
            if (helipadProbeStage == 25 && model.OwnAttackAirActive > 0)
            {
                Debug.Log("[SkirmishS003AircraftCycleProbe] secondSortie=Observed active=" + model.OwnAttackAirActive);
                EndHelipadProbe(true, "paidDeliveryTakeoffReturnTouchdownFuelAndSecondSortie");
            }
        }

        private static void TouchProbeCatalog(double now, BuildDrawerView drawer,
            BuildDrawerCatalogRuntimeView catalog, MatchOverlayCommandControlsView controls,
            BuildDrawerCategory category, string prefabKey)
        {
            Button button = drawer != null && drawer.IsOpen
                ? catalog?.ResolveCatalogTarget(category, prefabKey)
                : controls?.BuildButton;
            if (TapHelipadButton(button, now))
            {
                if (button != null && button.name == "BuildButton") airProbePurchaseAt = now;
            }
            else if (button != null) ScrollHelipadCatalogTo(button, now);
        }

        private static bool SelectPresentedAircraft(double now)
        {
            if (UiShellRuntimeGateway.TryReadMatchHudSelection(out var selected) &&
                selected.Visible && selected.IsAircraft) return true;
            if (!UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return false;
            var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            if (tray == null) return false;
            for (int slot = 0; slot < 4; slot++)
                if ((page.AirMask & (1 << slot)) != 0)
                { TapHelipadButton(tray.VisibleCardButton(slot), now); return false; }
            if (page.NextPage && airProbePageTurns++ < 12)
                TapHelipadButton(tray.VisibleCardButton(4), now);
            return false;
        }

        private static bool TouchEnemyBase(double now, SkirmishMatchView skirmish)
        {
            if (!skirmish.EnemyBaseMarkerVisible) return false;
            Vector2 point = skirmish.VisibleEnemyBasePoint;
            if (!Screen.safeArea.Contains(point)) return false;
            if (!helipadProbeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
            { EndHelipadProbe(false, "enemyBaseTouchRejected"); return false; }
            Debug.Log("[SkirmishS003AircraftCycleProbe] targetEnemyBase=" + point);
            helipadProbeNext = now + 1d;
            return true;
        }
    }
}
