#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        // Native883 spent924 aggregate factory-seconds waiting for Oil.
        // Buy four Oil trucks while funding aircraft, then use the existing
        // paid retirement/purchase flow to switch to two Oil / four Tankers.
        private const int FixedWingQualificationOilTrucks = 4;
        private const int FixedWingQualificationTankers = 2;
        private static bool fixedWingCycleOnly, fixedWingNativeFire;
        private static bool fixedWingMaterialProductionPaused;
        private static int fixedWingStep, fixedWingPages, fixedWingFirstFuel, fixedWingTouchdownFuel;
        private static uint fixedWingReceipt;
        private static double fixedWingSortieAt, fixedWingLastStatus;
        private static int fixedWingProductionFocusId, fixedWingProductionFocusCommands;
        private static bool fixedWingProductionFocusPending, fabricationCameraExcursionStarted, fabricationCameraExcursionReady;
        private static Entity fixedWingRetiredOilTruck;
        private static int fixedWingFleetStep;
        private static bool fixedWingFleetPagingForward;
        private static double fixedWingFleetStarted;
        private static bool fixedWingSelectionPagingForward;
        private static double fixedWingSelectionStarted;
        private static uint fixedWingReturnCommandReceipt;
        private static double fixedWingReturnStarted, fixedWingReturnAttemptAt, fixedWingReturnStableSince;
        private static Button fixedWingReturnStableButton;
        private static Vector2 fixedWingReturnStablePoint;
        private static int fixedWingReturnAttempts;

        private static void ResetFixedWingCycleProbe()
        {
            fixedWingCycleOnly = Environment.GetEnvironmentVariable("WARLINE_S005_FIXED_WING_CYCLE") == "1";
            Environment.SetEnvironmentVariable("WARLINE_S005_FIXED_WING_CYCLE", null);
            fixedWingStep = fixedWingPages = fixedWingFirstFuel = fixedWingTouchdownFuel = 0;
            fixedWingReceipt = 0;
            fixedWingSortieAt = fixedWingLastStatus = 0;
            fixedWingNativeFire = false;
            fixedWingMaterialProductionPaused = false;
            fixedWingProductionFocusId = -1;
            fixedWingProductionFocusPending = fabricationCameraExcursionStarted = fabricationCameraExcursionReady = false;
            fixedWingRetiredOilTruck = Entity.Null;
            fixedWingFleetStep = 0;
            fixedWingFleetPagingForward = true;
            fixedWingFleetStarted = 0;
            fixedWingSelectionPagingForward = true;
            fixedWingSelectionStarted = 0;
            fixedWingReturnCommandReceipt = 0;
            fixedWingReturnStarted = fixedWingReturnAttemptAt = fixedWingReturnStableSince = 0;
            fixedWingReturnStableButton = null;
            fixedWingReturnAttempts = 0;
        }

        private static void ObserveFixedWingReturnReceipt(string operation, uint receipt, bool verified)
        {
            if (fixedWingCycleOnly && fixedWingStep == 6 && operation == "ReturnToBase" && verified && receipt != 0)
                fixedWingReturnCommandReceipt = receipt;
        }

        private static Entity ReadPaidStrike(EntityManager em, Entity session)
        {
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(UnitAirComponent), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (owner.FactionId != 1 || !owner.SessionId.Equals(attempt) || owner.ReservationId == 0 ||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role != SkirmishRoleKind.Strike ||
                    em.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                bool paid = false;
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                    paid |= receipt.ReservationId == owner.ReservationId && receipt.FactionId == 1 &&
                        receipt.Role == SkirmishRoleKind.Strike && receipt.MaterialsPaid == 520 &&
                        receipt.Phase == SkirmishReservationPhase.Live;
                if (!paid || !em.HasComponent<SkirmishSharedActorTag>(actor))
                    throw new InvalidOperationException("Strike delivery lacks its native paid receipt/actor binding.");
                fixedWingReceipt = owner.ReservationId;
                fixedWingNativeFire |= em.HasComponent<UnitAttackTraceComponent>(actor) &&
                    em.GetComponentData<UnitAttackTraceComponent>(actor).ShotCounter > 0;
                return actor;
            }
            return Entity.Null;
        }

        private static int CountCommittedFixedWingTankers(EntityManager em, Entity session)
        {
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            int count = 0;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (owner.FactionId == 1 && owner.CapacityReleased == 0 && owner.SessionId.Equals(attempt) &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == SkirmishRoleKind.Tanker &&
                    em.GetComponentData<UnitHealth>(actor).Current > 0) count++;
            }
            foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                if (receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.Tanker &&
                    receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing) count++;
            return count;
        }

        private static void TouchFixedWingCatalog(BuildDrawerView drawer, BuildDrawerCategory category, string key)
        {
            var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            var button = drawer != null && drawer.IsOpen ? catalog?.ResolveCatalogTarget(category, key) : controls?.BuildButton;
            if (!PlayerResumeTap(button)) PlayerResumeScrollTo(button);
        }

        private static bool SelectFixedWingCard(EntityManager em, Entity session, Entity actor, double now)
        {
            if (fixedWingSelectionStarted == 0) fixedWingSelectionStarted = now;
            if (now - fixedWingSelectionStarted > 45)
                throw new InvalidOperationException("Paid Strike exclusive selection did not complete through normal input.");
            if (!SelectOnlyPlayerResumeActorThroughInput(em, actor)) return false;
            if (em.HasComponent<SelectedUnitTag>(actor) &&
                UiShellRuntimeGateway.TryReadMatchHudSelection(out var selected) && selected.Visible && selected.IsAircraft)
            {
                fixedWingSelectionStarted = 0;
                fixedWingPages = 0;
                fixedWingSelectionPagingForward = true;
                return true;
            }
            if (now - fixedWingSelectionStarted > 45 || fixedWingPages > 24)
                throw new InvalidOperationException("Paid Strike card selection did not complete through normal squad paging.");
            if (!UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return false;
            var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            if (tray == null) return false;
            for (int slot = 0; slot < 5; slot++)
                if ((page.AirMask & page.AssaultMask & (1 << slot)) != 0 &&
                    SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, slot, out var member) && member == actor)
                { PlayerResumeTapSquadSlot(tray, slot); return false; }
            // A local Tanker delivery can leave the last page visible while
            // the aircraft is on an earlier page. Search both directions and
            // verify the paid actor, rather than accepting any aircraft card.
            if (fixedWingSelectionPagingForward && !page.NextPage) fixedWingSelectionPagingForward = false;
            if (!fixedWingSelectionPagingForward && !page.PreviousPage) fixedWingSelectionPagingForward = true;
            if (PlayerResumeSwipeSquadPage(tray, fixedWingSelectionPagingForward))
            {
                fixedWingPages++;
                Debug.Log("[S005FixedWingSelection] phase=Page input=Touch direction=" +
                    (fixedWingSelectionPagingForward ? "Next" : "Previous") + " paidActorRequired=1");
            }
            return false;
        }

        private static void TickFixedWingCycleProbe(EntityManager em, Entity session, double now,
            SkirmishMatchView hud, BuildDrawerView drawer)
        {
            if (!UiShellRuntimeGateway.TryReadSkirmish(out var model)) return;
            if (model.Finished) throw new InvalidOperationException("Match ended before fixed-wing cycle completed.");
            if (!SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.Airport, 1))
                throw new InvalidOperationException("Airport was lost during fixed-wing cycle.");
            var actor = ReadPaidStrike(em, session);
            if (now - fixedWingLastStatus >= 10)
            {
                fixedWingLastStatus = now;
                float elapsed = SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session,
                    em.GetComponentData<SkirmishMatchState>(session));
                Debug.Log("[S005FixedWingCycle] phase=Progress stage=" + fixedWingStep + " materials=" + model.OwnMaterials +
                    " fuel=" + model.OwnAirFuel + " actor=" + (actor != Entity.Null ? 1 : 0) + " nativeFire=" + (fixedWingNativeFire ? 1 : 0) +
                    " elapsed=" + elapsed.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " selectedRoundTripReserve=" + RequiredSelectedAircraftRoundTripFuel());
                if (actor != Entity.Null)
                    Debug.Log("[S005FixedWingOwnState] elapsed=" + elapsed.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                        " air=" + JsonUtility.ToJson(em.GetComponentData<UnitAirComponent>(actor)) +
                        " pose=" + JsonUtility.ToJson(em.GetComponentData<LocalTransform>(actor)) +
                        " move=" + JsonUtility.ToJson(em.GetComponentData<UnitMove>(actor)) +
                        " target=" + (em.HasComponent<UnitTarget>(actor) ? JsonUtility.ToJson(em.GetComponentData<UnitTarget>(actor)) : "None") +
                        " engage=" + (em.HasComponent<EngageTarget>(actor) ? 1 : 0) +
                        " attackMove=" + (em.HasComponent<AttackMoveOrder>(actor) ? 1 : 0) +
                        " combat=" + (em.HasComponent<UnitCombat>(actor) ? JsonUtility.ToJson(em.GetComponentData<UnitCombat>(actor)) : "None") +
                        " selected=" + (em.HasComponent<SelectedUnitTag>(actor) ? 1 : 0) +
                        " readOnly=1 ownFactionOnly=1");
            }
            if (fixedWingStep == 0)
            {
                if (CountCommittedFixedWingTankers(em, session) < FixedWingQualificationTankers)
                { TouchFixedWingCatalog(drawer, BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tanker"); return; }
                fixedWingStep = 1;
            }
            if (fixedWingStep == 1)
            {
                if (actor == Entity.Null)
                {
                    bool committed = false;
                    foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                        committed |= receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.Strike &&
                            receipt.MaterialsPaid == 520 && receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing;
                    // Buy the aircraft at its actual price; fund each later
                    // Tanker before retiring its Oil truck. An 800-Materials
                    // combined reserve can be unreachable under full-batch caps.
                    if (!committed && model.OwnMaterials < 520) return;
                    TouchFixedWingCatalog(drawer, BuildDrawerCategory.Aircrafts, "Unit_Veh_Jet_01");
                    return;
                }
                var delivered = em.GetComponentData<UnitAirComponent>(actor);
                if (delivered.HomeInitialized == 0 || em.HasComponent<UnitSpawnTransitTag>(actor)) return;
                if (!em.HasComponent<LocalTransform>(actor) ||
                    math.distance(em.GetComponentData<LocalTransform>(actor).Position, delivered.HomePosition) > 1.5f) return;
                if (delivered.UsesRunway == 0) throw new InvalidOperationException("Paid Strike has no native runway home.");
                Debug.Log("[S005FixedWingCycle] phase=PaidDelivery role=Strike cost=520 nativeActor=1 runwayHome=1 parkingTransitComplete=1 atHome=1 receipt=" + fixedWingReceipt);
                fixedWingStep = 2;
            }
            if (actor == Entity.Null) throw new InvalidOperationException("Paid Strike was lost during fixed-wing cycle.");
            var air = em.GetComponentData<UnitAirComponent>(actor);
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (fixedWingStep == 2)
            {
                // Keep fabrication running until each paid fleet purchase is
                // complete, rather than require all purchases in one stockpile.
                if (!TickFixedWingFuelFleetThroughInput(em, session, now, drawer, controls)) return;
                if (!fixedWingMaterialProductionPaused)
                {
                    if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return; }
                    if (!PauseFixedWingMaterialProductionThroughInput(em, now, controls)) return;
                    fixedWingMaterialProductionPaused = true;
                    ResetFixedWingLocalFuelSupplyProbe();
                    Debug.Log("[S005FixedWingCycle] phase=FuelPriority allLivingFabricationPaused=1 input=Touch productionFlagsObserved=1");
                }
                if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return; }
                if (!TickFixedWingLocalFuelSupplyThroughInput(em, now)) return;
                if (!SelectFixedWingCard(em, session, actor, now)) return;
                fixedWingStep = 3;
            }
            if (fixedWingStep == 3)
            { if (PlayerResumeTap(hud.EnemyFocusButton)) fixedWingStep = 4; return; }
            if (fixedWingStep == 4 || fixedWingStep == 10)
            {
                int reserve = RequiredSelectedAircraftRoundTripFuel();
                if (reserve <= 0) throw new InvalidOperationException("Fixed-wing fuel route is unavailable.");
                if (model.OwnAirFuel < reserve) return;
                if (!hud.EnemyBaseMarkerVisible) return;
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state) || state.ActiveCommandMode != TacticalCommandMode.Attack)
                { PlayerResumeTap(controls?.AttackButton); return; }
                Vector2 point = hud.VisibleEnemyBasePoint;
                if (!Screen.safeArea.Contains(point)) return;
                if (!playerResumeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                    throw new InvalidOperationException("Fixed-wing target touch was rejected.");
                fixedWingFirstFuel = model.OwnAirFuel; fixedWingSortieAt = now;
                fixedWingStep = fixedWingStep == 4 ? 5 : 11;
                playerResumeDue = now + 1;
                return;
            }
            if (fixedWingStep == 5)
            {
                if (air.Airborne == 0 || air.TakeoffRolling != 0 || now - fixedWingSortieAt < 5 ||
                    model.OwnAirFuel >= fixedWingFirstFuel || !fixedWingNativeFire) return;
                Debug.Log("[S005FixedWingCycle] phase=FirstSortie airborne=1 nativeFire=1 fuelSpent=" + (fixedWingFirstFuel - model.OwnAirFuel));
                UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".fixed-wing-first-sortie.png");
                fixedWingStep = 6;
            }
            if (fixedWingStep == 6)
            {
                if (fixedWingReturnStarted == 0) fixedWingReturnStarted = now;
                if (now - fixedWingReturnStarted > 30)
                    throw new InvalidOperationException("Native Return command did not become accepted for the paid Strike within 30 seconds.");
                if (fixedWingReturnCommandReceipt != 0)
                {
                    bool returning = air.ReturningHome != 0 || (air.Airborne == 0 &&
                        math.distance(em.GetComponentData<LocalTransform>(actor).Position, air.HomePosition) <= 1.5f);
                    bool attackCleared = !em.HasComponent<AttackMoveOrder>(actor) && !em.HasComponent<EngageTarget>(actor) &&
                        !em.HasComponent<UnitTarget>(actor) && em.GetComponentData<UnitCombat>(actor).AutoEngage == 0;
                    if (!returning || !attackCleared)
                    {
                        if (now - fixedWingReturnAttemptAt > 3)
                            throw new InvalidOperationException("Accepted native Return did not preserve the paid Strike return state and clear its attack.");
                        return;
                    }
                    Debug.Log("[S005FixedWingCycle] phase=ReturnAccepted input=Touch nativeReceipt=" +
                        fixedWingReturnCommandReceipt + " paidActorObserved=1 attackCleared=1 autoEngageDisabled=1");
                    fixedWingStep = 7;
                    return;
                }
                if (fixedWingReturnAttemptAt > 0 && now - fixedWingReturnAttemptAt < 2) return;
                if (!SelectFixedWingCard(em, session, actor, now)) return;
                // The targeting mode remains active after an attack. Leave it
                // through the player's Select control before opening the wheel.
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var returnMode)) return;
                if (returnMode.ActiveCommandMode is not (TacticalCommandMode.None or TacticalCommandMode.Select))
                { PlayerResumeTap(controls?.SelectButton); return; }
                var wheel = controls?.CommandWheelPanel;
                if (wheel == null) return;
                if (!wheel.IsOpen)
                {
                    fixedWingReturnStableButton = null;
                    PlayerResumeTap(wheel.OpenButton);
                    return;
                }
                var button = wheel.NextReturnButton;
                if (!TryPresentedButtonPoint(button, out var point)) return;
                if (fixedWingReturnStableButton != button || Vector2.Distance(point, fixedWingReturnStablePoint) > 1f)
                {
                    fixedWingReturnStableButton = button;
                    fixedWingReturnStablePoint = point;
                    fixedWingReturnStableSince = now;
                    return;
                }
                if (now - fixedWingReturnStableSince < .5d) return;
                if (fixedWingReturnAttempts >= 4)
                    throw new InvalidOperationException("Four native Return touches failed to produce an accepted command.");
                ScreenCapture.CaptureScreenshot(LiveTracePath() + ".fixed-wing-return-wheel-attempt-" +
                    (fixedWingReturnAttempts + 1) + ".png");
                if (PlayerResumeTap(button))
                {
                    fixedWingReturnAttemptAt = now;
                    fixedWingReturnAttempts++;
                    fixedWingReturnStableButton = null;
                    Debug.Log("[S005FixedWingCycle] phase=ReturnTouch input=Touch attempt=" + fixedWingReturnAttempts +
                        " stableBeforeTouch=1 nativeAcceptance=Pending");
                }
                return;
            }
            if (fixedWingStep == 7)
            {
                if (air.Airborne != 0 || air.ReturningHome != 0 || air.LandingRolling != 0 ||
                    !em.HasComponent<LocalTransform>(actor) || math.distance(em.GetComponentData<LocalTransform>(actor).Position, air.HomePosition) > 1.5f) return;
                fixedWingTouchdownFuel = model.OwnAirFuel;
                Debug.Log("[S005FixedWingCycle] phase=Touchdown nativeLanding=1 atHome=1 fuel=" + fixedWingTouchdownFuel);
                UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".fixed-wing-touchdown.png");
                ResetFixedWingLocalFuelSupplyProbe();
                fixedWingStep = 8;
            }
            if (fixedWingStep == 8)
            {
                if (!TickFixedWingLocalFuelSupplyThroughInput(em, now) || !SelectFixedWingCard(em, session, actor, now)) return;
                if (model.OwnAirFuel <= fixedWingTouchdownFuel || model.OwnAirFuel < RequiredSelectedAircraftRoundTripFuel()) return;
                Debug.Log("[S005FixedWingCycle] phase=Refuel added=" + (model.OwnAirFuel - fixedWingTouchdownFuel));
                fixedWingStep = 9;
            }
            if (fixedWingStep == 9)
            { if (PlayerResumeTap(hud.EnemyFocusButton)) fixedWingStep = 10; return; }
            if (fixedWingStep == 11 && air.Airborne != 0 && air.TakeoffRolling == 0 &&
                now - fixedWingSortieAt >= 5 && model.OwnAirFuel < fixedWingFirstFuel)
            {
                if (AriaCommandEvidence.Violations != 0 || playerResumeTouch.UnexpectedSamples != 0 || playerResumeTouch.PhysicalInterventions != 0)
                    throw new InvalidOperationException("Fixed-wing input audit failed.");
                Debug.Log("[S005FixedWingCycle] result=Passed role=Strike cost=520 paidReceipt=" + fixedWingReceipt +
                    " input=Touch normalBudget=1 nativeFire=1 launch=1 return=1 touchdown=1 refuel=1 secondSortie=1 gestures=" +
                    playerResumeTouch.CompletedGestures + " commands=" + AriaCommandEvidence.AcceptedCommands +
                    " receiptViolations=0 acceptedNativeReturn=1 returnReceipt=" + fixedWingReturnCommandReceipt +
                    " bilateralCapacity=Pending fullRoster=Pending completeMatch=NotClaimed");
                playerResumeProbeActive = airportPlacementOnly = fixedWingCycleOnly = false;
                playerResumeTouch.Dispose(); playerResumeTouch = null;
                Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
                Finish(false, "paidStrikeNativeFlightReturnRefuelSecondSortieOnly");
            }
        }

        private static bool TickFixedWingFuelFleetThroughInput(EntityManager em, Entity session, double now,
            BuildDrawerView drawer, MatchOverlayCommandControlsView controls)
        {
            if (fixedWingFleetStep == 4) return true;
            if (fixedWingFleetStarted > 0 && now - fixedWingFleetStarted > 90)
                throw new InvalidOperationException("Normal paid four-Tanker fleet transition timed out.");
            int oil = 0, tankers = 0;
            uint newestPaidOil = 0;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(UnitResourceHauler), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owner.FactionId != 1 || owner.CapacityReleased != 0 ||
                    owner.SessionId.ToString() != playerResumeSessionId || em.GetComponentData<UnitHealth>(unit).Current <= 0) continue;
                var role = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role;
                if (role == SkirmishRoleKind.Tanker) tankers++;
                if (role != SkirmishRoleKind.LogisticsTruck) continue;
                oil++;
                var cargo = em.GetComponentData<UnitResourceHauler>(unit);
                if (fixedWingFleetStep == 0 && owner.ReservationId > newestPaidOil &&
                    cargo.CargoOilBarrels <= 0 && cargo.CargoFuelBarrels <= 0)
                { fixedWingRetiredOilTruck = unit; newestPaidOil = owner.ReservationId; }
            }
            if (fixedWingFleetStep == 0)
            {
                if (oil is not (3 or 4) || oil + tankers != 6)
                    throw new InvalidOperationException("Fleet transition requires the native funded Oil/fuel fleet within support cap6.");
                if (fixedWingRetiredOilTruck == Entity.Null || !UiShellRuntimeGateway.TryReadSkirmish(out var fleetModel) ||
                    fleetModel.OwnMaterials < 140) return false;
                fixedWingFleetStarted = now;
                fixedWingFleetStep = 1;
                Debug.Log("[S005FuelFleet] phase=Plan oil=" + oil + " tankers=" + tankers +
                    " targetOil=2 targetTankers=4 supportCap=6 tankerCost=140 input=TouchPending");
            }
            if (fixedWingFleetStep == 1)
            {
                if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return false; }
                if (!SelectOnlyPlayerResumeActorThroughInput(em, fixedWingRetiredOilTruck)) return false;
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) return false;
                if (command.ActiveCommandMode is not (TacticalCommandMode.None or TacticalCommandMode.Select))
                { PlayerResumeTap(controls?.SelectButton); return false; }
                if (!em.HasComponent<SelectedUnitTag>(fixedWingRetiredOilTruck))
                {
                    var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                    if (tray == null || !UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return false;
                    for (int slot = 0; slot < 5; slot++)
                        if (SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, slot, out var member) && member == fixedWingRetiredOilTruck)
                        { PlayerResumeTapSquadSlot(tray, slot); return false; }
                    if (fixedWingFleetPagingForward && !page.NextPage) fixedWingFleetPagingForward = false;
                    if (!fixedWingFleetPagingForward && !page.PreviousPage) fixedWingFleetPagingForward = true;
                    PlayerResumeSwipeSquadPage(tray, fixedWingFleetPagingForward);
                    return false;
                }
                var wheel = controls?.CommandWheelPanel;
                if (wheel == null) return false;
                if (!wheel.IsOpen) { PlayerResumeTap(wheel.OpenButton); return false; }
                var cargo = em.GetComponentData<UnitResourceHauler>(fixedWingRetiredOilTruck);
                if (cargo.CargoOilBarrels > 0 || cargo.CargoFuelBarrels > 0) return false;
                foreach (var button in wheel.GetComponentsInChildren<UnityEngine.UI.Button>())
                    if (button.name == "DestroySector" && PlayerResumeTap(button))
                    {
                        fixedWingFleetStep = 2;
                        Debug.Log("[S005FuelFleet] phase=RetireOilTruck input=Touch cargoEmptyObserved=1 selectedNativeActors=1 resourcesOrOrdersInjected=0");
                        return false;
                    }
                return false;
            }
            if (fixedWingFleetStep == 2)
            {
                bool released = !em.Exists(fixedWingRetiredOilTruck) ||
                    (em.HasComponent<SkirmishAttemptOwnedComponent>(fixedWingRetiredOilTruck) &&
                    em.GetComponentData<SkirmishAttemptOwnedComponent>(fixedWingRetiredOilTruck).CapacityReleased != 0);
                if (!released) return false;
                Debug.Log("[S005FuelFleet] phase=CapacityReleased nativeOil=" + oil + " nativeTankers=" + tankers +
                    " input=Touch observedCleanup=1");
                if (oil is not (2 or 3)) throw new InvalidOperationException("Native Oil fleet retirement released an unexpected count.");
                fixedWingFleetStep = 3;
            }
            if (fixedWingFleetStep == 3)
            {
                int targetTankers = 6 - oil;
                if (tankers < targetTankers)
                {
                    if (CountCommittedFixedWingTankers(em, session) < targetTankers)
                        TouchFixedWingCatalog(drawer, BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tanker");
                    return false;
                }
                if (tankers != targetTankers) throw new InvalidOperationException("Native support fleet exceeded its six slots.");
                if (oil > 2)
                {
                    // Retain three Oil trucks until the last Tanker is funded.
                    // Each paid native transition has its own UI watchdog; the
                    // unchanged normal match clock still bounds affordability.
                    fixedWingRetiredOilTruck = Entity.Null;
                    fixedWingFleetStarted = 0;
                    fixedWingFleetStep = 0;
                    return false;
                }
                bool paidFourth = false;
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                    paidFourth |= receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.Tanker &&
                        receipt.MaterialsPaid == 140 && receipt.Phase == SkirmishReservationPhase.Live;
                if (!paidFourth || oil != 2 || tankers != 4)
                    throw new InvalidOperationException("Four-Tanker fleet lacks native receipt/capacity state.");
                Debug.Log("[S005FuelFleet] result=Passed nativeOil=2 nativeTankers=4 supportUsed=6 supportCap=6 tankerCost=140 input=Touch normalBudget=1 resourcesOrOrdersInjected=0");
                fixedWingFleetStep = 4;
                return true;
            }
            return false;
        }

        private static bool PauseFixedWingMaterialProductionThroughInput(EntityManager em, double now,
            MatchOverlayCommandControlsView controls) =>
            SetFixedWingMaterialProductionThroughInput(em, now, controls, false);

        private static bool SetFixedWingMaterialProductionThroughInput(EntityManager em, double now,
            MatchOverlayCommandControlsView controls, bool enabled)
        {
            if (!SelectOnlyPlayerResumeActorThroughInput(em, Entity.Null)) return false;
            if (fixedWingProductionFocusPending)
            {
                if (AriaCommandEvidence.AcceptedCommands != fixedWingProductionFocusCommands)
                    throw new InvalidOperationException("Factory camera guidance issued a troop command.");
                fixedWingProductionFocusPending = false;
                Debug.Log("[S005FactoryCamera] phase=Observed input=Touch troopCommandsAdded=0");
            }
            Entity target = Entity.Null;
            int targetId = int.MaxValue;
            using (var query = em.CreateEntityQuery(typeof(MaterialFabricationComponent), typeof(UnitHealth), typeof(LocalTransform)))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                {
                    var fabrication = em.GetComponentData<MaterialFabricationComponent>(entity);
                    if (fabrication.OwnerFactionId != 1 || (fabrication.ProductionEnabled != 0) == enabled ||
                        em.GetComponentData<UnitHealth>(entity).Current <= 0 || fabrication.RuntimeBuildingId >= targetId) continue;
                    target = entity;
                    targetId = fabrication.RuntimeBuildingId;
                }
            if (target == Entity.Null) return true;
            if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) return false;
            // A neutral command mode also permits the normal building-selection
            // click. Repeated Select taps toggle explicit selection off again.
            if (command.ActiveCommandMode is not (TacticalCommandMode.None or TacticalCommandMode.Select))
            { PlayerResumeTap(controls?.SelectButton); return false; }
            var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var bootstrap = scene?.MatchBootstrap;
            if (bootstrap == null || !bootstrap.BuildingUiQuery.TryGetSelectedMaterialFabricationReadModel(
                bootstrap.BuildingUiQueryContext, out var selected) || selected.RuntimeBuildingId != targetId)
            {
                if (fixedWingProductionFocusId != targetId)
                {
                    // Return from the Airport view using the player's existing
                    // HQ camera control before selecting a home-area factory.
                    var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                    fixedWingProductionFocusCommands = AriaCommandEvidence.AcceptedCommands;
                    if (PlayerResumeTap(hud?.PlayerFocusButton))
                    {
                        fixedWingProductionFocusId = targetId;
                        fixedWingProductionFocusPending = true;
                    }
                    return false;
                }
                PlayerResumeWorldTap(em, target, now); return false;
            }
            if (selected.ProductionEnabled == enabled) return false;
            foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (button.name == "PassengerChip")
                {
                    // Extended factory details overflow the compact card. Bring
                    // its existing production control into view through a real
                    // ScrollRect gesture before tapping it.
                    if (!PlayerResumeTap(button)) { PlayerResumeScrollTo(button); return false; }
                    Debug.Log("[S005FixedWingCycle] phase=" + (enabled ? "ResumeFabrication" : "PauseFabrication") +
                        " id=" + targetId + " input=Touch");
                    return false;
                }
            return false;
        }

        private static void TickFabricationSelectionProbe(EntityManager em, double now)
        {
            if (!fabricationCameraExcursionReady)
            {
                if (!fabricationCameraExcursionStarted)
                {
                    // Reproduce the public runway-area camera view reached by
                    // the paid Airport probe, without buying or injecting one.
                    if (!BeginAirportCameraFocus(new Vector3(825.5f, 0, 671), false))
                        throw new InvalidOperationException("Factory camera excursion unavailable.");
                    fabricationCameraExcursionStarted = true;
                }
                if (airportCameraPending) { FocusAirportSiteThroughMap(now); return; }
                fabricationCameraExcursionReady = true;
                Debug.Log("[S005FabricationSelection] phase=AirportCameraExcursion input=Touch troopCommandsAdded=0");
            }
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (!SetFixedWingMaterialProductionThroughInput(em, now, controls, fabricationSelectionStep != 0)) return;
            if (fabricationSelectionStep++ == 0)
            {
                Debug.Log("[S005FabricationSelection] phase=Paused nativeFlagsObserved=1 input=Touch");
                UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".factory-paused.png");
                return;
            }
            if (AriaCommandEvidence.Violations != 0 || playerResumeTouch.UnexpectedSamples != 0 ||
                playerResumeTouch.PhysicalInterventions != 0 || playerResumeTouch.CompletedGestures < 2)
                throw new InvalidOperationException("Native fabrication selection input audit failed.");
            Debug.Log("[S005FabricationSelection] result=Passed pause=1 resume=1 nativeFlagsObserved=1 input=Touch" +
                " gestures=" + playerResumeTouch.CompletedGestures + " completeMatch=NotClaimed");
            UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".factory-resumed.png");
            fabricationSelectionOnly = playerResumeProbeActive = false;
            playerResumeTouch.Dispose(); playerResumeTouch = null;
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            Finish(false, "nativeFabricationSelectionPauseResumeOnly");
        }
    }
}
#endif
