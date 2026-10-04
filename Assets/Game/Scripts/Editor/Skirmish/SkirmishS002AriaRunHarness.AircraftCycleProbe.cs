using System;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.Tactical.Contracts;
using UnityEngine;
using UnityEngine.UI;
using Game.Components;
using Unity.Entities;
using Unity.Collections;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static double airProbePurchaseAt, airProbeServiceAt, airProbeSortieAt, airProbeLastStatusAt;
        private static int airProbePageTurns;
        private static int airProbeFirstFuel, airProbeSecondFuel;

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
                TraceAircraftOrders();
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
                if (EnsureAircraftAttackMode(now, controls)) helipadProbeStage = 16;
                return;
            }
            if (helipadProbeStage == 16)
            {
                if (TouchEnemyBase(now, skirmish))
                { helipadProbeStage = 17; airProbeSortieAt = now; airProbeFirstFuel = model.OwnAirFuel; }
                return;
            }
            if (helipadProbeStage == 17)
            {
                if (model.OwnAttackAirActive == 0) return;
                if (now - airProbeSortieAt < 15d) return;
                if (model.OwnAirFuel >= airProbeFirstFuel) return;
                Debug.Log("[SkirmishS003AircraftCycleProbe] firstSortie=Observed active=" + model.OwnAttackAirActive);
                Debug.Log("[SkirmishAircraftFuelCycle] firstFuelSpent=" + (airProbeFirstFuel - model.OwnAirFuel));
                helipadProbeStage = 18;
            }
            if (helipadProbeStage == 18)
            {
                if (!SelectPresentedAircraft(now)) return;
                // The old panel buttons are hidden. Follow the player's Commands
                // button into the wheel and tap its visible Return action.
                var wheel = controls != null ? controls.CommandWheelPanel : null;
                bool wasOpen = wheel != null && wheel.IsOpen;
                if (TapHelipadButton(wheel?.NextReturnButton, now) && wasOpen) helipadProbeStage = 19;
                return;
            }
            if (helipadProbeStage == 19)
            {
                if (model.OwnAttackAirActive != 0 || model.OwnAttackAirLanded == 0 || !SelectedAircraftAtHome()) return;
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
                if (EnsureAircraftAttackMode(now, controls)) helipadProbeStage = 24;
                return;
            }
            if (helipadProbeStage == 24)
            {
                if (TouchEnemyBase(now, skirmish))
                { helipadProbeStage = 25; airProbeSecondFuel = model.OwnAirFuel; airProbeSortieAt = now; }
                return;
            }
            if (helipadProbeStage == 25 && model.OwnAttackAirActive > 0 &&
                now - airProbeSortieAt >= 5d && model.OwnAirFuel < airProbeSecondFuel)
            {
                Debug.Log("[SkirmishS003AircraftCycleProbe] secondSortie=Observed active=" + model.OwnAttackAirActive);
                Debug.Log("[SkirmishAircraftFuelCycle] secondFuelSpent=" + (airProbeSecondFuel - model.OwnAirFuel));
                EndHelipadProbe(true, "paidDeliveryTakeoffReturnTouchdownFuelAndSecondSortie");
            }
            else if (helipadProbeStage == 25 && now - airProbeSortieAt > 45d)
                EndHelipadProbe(false, "secondSortieNotLaunched");
        }

        private static bool EnsureAircraftAttackMode(double now, MatchOverlayCommandControlsView controls)
        {
            if (UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state) &&
                state.ActiveCommandMode == TacticalCommandMode.Attack) return true;
            TapHelipadButton(controls?.AttackButton, now);
            return false;
        }

        private static void TraceAircraftOrders()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(typeof(UnitAirComponent), typeof(SelectedUnitTag));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var air = em.GetComponentData<UnitAirComponent>(actor);
                Debug.Log("[S004AircraftOrders] entity=" + actor + " returning=" + air.ReturningHome +
                    " airborne=" + air.Airborne + " engage=" + em.HasComponent<EngageTarget>(actor) +
                    " target=" + em.HasComponent<UnitTarget>(actor) + " hold=" + em.HasComponent<HoldPositionOrderTag>(actor));
            }
            using var queues = em.CreateEntityQuery(typeof(UnitAttackOrderQueueComponent));
            if (queues.CalculateEntityCount() == 1)
            {
                var queue = queues.GetSingletonEntity();
                Debug.Log("[S004AircraftOrders] lastRequest=" + em.GetComponentData<UnitAttackOrderQueueComponent>(queue).LastRequestId);
                if (em.HasBuffer<UnitAttackOrderResultElement>(queue))
                    foreach (var result in em.GetBuffer<UnitAttackOrderResultElement>(queue, true))
                        Debug.Log("[S004AircraftOrders] request=" + result.RequestId + " issued=" + result.Issued +
                            " reason=" + result.ReasonCode + " message=" + result.Message);
            }
        }

        private static bool SelectedAircraftAtHome()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;
            var em = world.EntityManager;
            using var query = em.CreateEntityQuery(typeof(UnitAirComponent), typeof(SelectedUnitTag),
                typeof(Unity.Transforms.LocalTransform));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var air = em.GetComponentData<UnitAirComponent>(actor);
                if (air.HomeInitialized == 0 || air.Airborne != 0 || air.ReturningHome != 0 ||
                    Unity.Mathematics.math.distance(em.GetComponentData<Unity.Transforms.LocalTransform>(actor).Position,
                        air.HomePosition) > 1.5f) return false;
            }
            return actors.Length > 0;
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
            for (int slot = 0; slot < 5; slot++)
                if ((page.AirMask & page.AssaultMask & (1 << slot)) != 0)
                { TapHelipadButton(tray.VisibleCardButton(slot), now); return false; }
            if (page.NextPage && airProbePageTurns++ < 12)
                TapHelipadButton(tray.NextPageButton, now);
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
