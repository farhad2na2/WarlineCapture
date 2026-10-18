using System;
using Game.Configs;
using Game.Composition;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.Tactical.Contracts;
using UnityEngine;
using UnityEngine.UI;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;
using Unity.Collections;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static double airProbePurchaseAt, airProbeServiceAt, airProbeSortieAt, airProbeLastStatusAt;
        private static int airProbePageTurns;
        private static int airProbeFirstFuel, airProbeSecondFuel, airProbeTouchdownFuel, airProbeRequiredReserve;
        private static int airProbeDefenseSlot, airProbeDefenseStep;
        private static int airProbeDefensePage, airProbeDefenseOrders;
        private static readonly S005ObservedSquadPageReceipt airProbeDefensePaging=new S005ObservedSquadPageReceipt();
        private static int airProbeDefenseMapStep;
        private static int airProbeDefenseMapAttempts;
        private static int airProbeCameraCommandsBefore, airProbeCameraAttackMaskBefore;
        private static int airProbeMapLayoutFrame = -1;
        private static MatchHudMinimapView airProbeMapLayoutOwner;
        private static Rect airProbeMapLayoutRect, airProbeViewportLayoutRect;

        // Observe two distinct game frames before deriving input coordinates. Opening
        // the popup can expose its view before its layout has a usable size.
        private static bool GuardMapLayoutReady(MatchHudMinimapView map, Camera eventCamera)
        {
            if (map.MapRect == null || map.ViewportRect == null ||
                !map.MapRect.gameObject.activeInHierarchy || !map.ViewportRect.gameObject.activeInHierarchy)
                return false;
            Rect ScreenRect(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector2 lo = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
                Vector2 hi = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
                return new Rect(lo, hi - lo);
            }
            Rect bounds = ScreenRect(map.MapRect), viewport = ScreenRect(map.ViewportRect);
            bool Usable(Rect rect) => float.IsFinite(rect.x) && float.IsFinite(rect.y) &&
                float.IsFinite(rect.width) && float.IsFinite(rect.height) && rect.width > 1f && rect.height > 1f;
            bool Same(Rect a, Rect b) => (a.position - b.position).sqrMagnitude <= .25f &&
                (a.size - b.size).sqrMagnitude <= .25f;
            if (!Usable(bounds) || !Usable(viewport)) { airProbeMapLayoutFrame = -1; return false; }
            bool ready = airProbeMapLayoutOwner == map && airProbeMapLayoutFrame >= 0 &&
                airProbeMapLayoutFrame < Time.frameCount && Same(bounds, airProbeMapLayoutRect) &&
                Same(viewport, airProbeViewportLayoutRect);
            airProbeMapLayoutOwner = map;
            airProbeMapLayoutFrame = Time.frameCount;
            airProbeMapLayoutRect = bounds;
            airProbeViewportLayoutRect = viewport;
            return ready;
        }

        private static bool FocusPresentedGuardSite(double now, Vector3 site)
        {
            var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            // The shell instantiates this popup only after the compact map tap.
            if (popup == null || !popup.IsOpen)
            {
                airProbeMapLayoutFrame = -1;
                foreach (var map in UnityEngine.Object.FindObjectsByType<MatchHudMinimapView>(FindObjectsSortMode.None))
                {
                    if (map.GetComponentInParent<MatchHudFullMapPopupView>() != null || map.MapRect == null) continue;
                    var canvas = map.GetComponentInParent<Canvas>();
                    Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(eventCamera,
                        map.MapRect.TransformPoint(map.MapRect.rect.center));
                    if (helipadProbeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                    { airProbeDefenseMapStep = 1; helipadProbeNext = now + 1d; }
                    return false;
                }
                return false;
            }
            if (airProbeDefenseMapStep == 2)
            {
                if (TapHelipadButton(popup.CloseAction, now)) airProbeDefenseMapStep = 0;
                return false;
            }
            var fullMap = popup.Minimap;
            if (fullMap == null) return false;
            var fullCanvas = fullMap.GetComponentInParent<Canvas>();
            Camera fullCamera = fullCanvas != null && fullCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? fullCanvas.worldCamera : null;
            if (!GuardMapLayoutReady(fullMap, fullCamera)) return false;
            if (!fullMap.TryGetPresentedMapPoint(site, fullCamera, out var goal)) return false;
            if (airProbeDefenseMapAttempts >= 3)
            {
                UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".camera-focus-failure.png");
                EndHelipadProbe(false, "baseGuardCameraFocusNotObserved"); return false;
            }
            Vector2 from = goal;
            bool drag = fullMap.ViewportRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(fullMap.ViewportRect, goal, fullCamera);
            if (drag)
                from = RectTransformUtility.WorldToScreenPoint(fullCamera,
                    fullMap.ViewportRect.TransformPoint(fullMap.ViewportRect.rect.center));
            Vector2 end = drag && (from - goal).sqrMagnitude <= 25f ? goal + Vector2.up * 12f : goal;
            if (Environment.GetEnvironmentVariable("WARLINE_S005_CAMERA_TOUCH_ONLY") == "1" && airProbeCameraCommandsBefore < 0)
            {
                airProbeCameraCommandsBefore = AriaCommandEvidence.AcceptedCommands;
                airProbeCameraAttackMaskBefore = UiShellRuntimeGateway.TryReadExpandedSquadPage(out var before) ? before.AttackOrderMask : -1;
            }
            if (helipadProbeTouch.TryGesture(from, end, .12f, drag ? .45f : 0f, Time.unscaledTime))
            {
                Debug.Log("[S005AircraftBaseGuard] cameraInput=FullMapTouch world=" + site +
                    " from=" + from + " to=" + end + " drag=" + drag +
                    " mapRect=" + airProbeMapLayoutRect + " viewportRect=" + airProbeViewportLayoutRect +
                    " camera=" + (Camera.main != null ? Camera.main.transform.position.ToString() : "missing"));
                airProbeDefenseMapAttempts++;
                airProbeDefenseMapStep = 2;
                helipadProbeNext = now + 1d;
            }
            return false;
        }

        private static bool GuardPresentedBase(double now, SkirmishMatchView skirmish,
            MatchOverlayCommandControlsView controls)
        {
            if (!UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return false;
            var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            if (tray == null) return false;
            if (airProbeDefenseSlot >= 5)
            {
                if (!fixedWingCycleOnly || !page.NextPage&&!airProbeDefensePaging.IsPending){StopS005GuardObservation();return true;}
                if (airProbeDefensePage >= 8)
                    throw new InvalidOperationException("Opening defense squad paging did not finish.");
                var receipt=airProbeDefensePaging.Observe(page.PageIndex,tray.LogicalPageIndex,page.RosterRevision,
                    helipadProbeTouch.IsBusy,helipadProbeTouch.CompletedGestures,now);
                if(receipt==S005SquadPageReceiptState.Advanced)
                {
                    airProbeDefensePage++;airProbeDefenseSlot=airProbeDefenseStep=0;
                    Debug.Log("[S005SquadPageReceipt] result=Advanced actualPage="+page.PageIndex+" renderedPage="+tray.LogicalPageIndex+" completedContacts="+helipadProbeTouch.CompletedGestures);
                }
                else if(receipt==S005SquadPageReceiptState.Failed)
                    throw new InvalidOperationException("Opening defense squad swipe did not advance the actual presented page: "+airProbeDefensePaging.Failure);
                else if(receipt==S005SquadPageReceiptState.Ready&&SwipeHelipadSquadPage(tray,true,now))
                {
                    airProbeDefensePaging.Dispatch(page.PageIndex,page.RosterRevision,helipadProbeTouch.CompletedGestures,now);
                    Debug.Log("[S005SquadPageReceipt] result=Dispatched actualPage="+page.PageIndex+" renderedPage="+tray.LogicalPageIndex+" targetPage="+(page.PageIndex+1));
                }
                return false;
            }
            int bit = 1 << airProbeDefenseSlot;
            if ((page.AssaultMask & bit) == 0) { airProbeDefenseSlot++; return false; }
            if (airProbeDefenseStep == 0)
            {
                if (page.SelectedGroups > 0) { DeselectHelipadSquads(tray, now); return false; }
                if (TapHelipadSquadSlot(tray, airProbeDefenseSlot, now)) airProbeDefenseStep = 1;
            }
            else if (airProbeDefenseStep == 1)
            {
                if ((page.SelectedMask & bit) == 0) return false;
                if (TapHelipadButton(controls?.AttackButton, now)) airProbeDefenseStep = 2;
            }
            else if (airProbeDefenseStep == 2)
            {
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command) ||
                    command.ActiveCommandMode != TacticalCommandMode.Attack) return false;
                // Use the same presented objective positions as the placement
                // probe. Screen-up points behind this rotated Campaign camera.
                Vector3 forward = skirmish.EnemyBaseObjectivePosition - skirmish.PlayerBaseObjectivePosition;
                forward.y = 0;
                if (forward.sqrMagnitude < 1f || Camera.main == null) return false;
                forward.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, forward);
                // Ground Staging sits ahead of HQ. Intercept at its front so
                // ranged attackers cannot shell the producer beyond the guards.
                Vector3 site = skirmish.PlayerBaseObjectivePosition + forward * 44f +
                    side * ((airProbeDefenseSlot - 2) * 6f);
                if (fixedWingCycleOnly)
                {
                    // Protect the player's actual producer, including native
                    // legal-placement relocation, using only owned information.
                    // HQ-relative points can leave a long-range attacker outside
                    // the defenders' range while it attacks Ground Staging.
                    var world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated ||
                        !SkirmishLaunchProjection.TryGet(world.EntityManager, out var session, out _)) return false;
                    var em = world.EntityManager;
                    var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                    bool found = false;
                    using var query = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent),
                        typeof(SkirmishAttemptOwnedComponent), typeof(LocalTransform), typeof(UnitHealth));
                    using var producers = query.ToEntityArray(Allocator.Temp);
                    foreach (var producer in producers)
                    {
                        var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(producer);
                        if (owner.FactionId != 1 || owner.CapacityReleased != 0 || !owner.SessionId.Equals(attempt) ||
                            em.GetComponentData<SkirmishStructureIdentityComponent>(producer).Producer != SkirmishProducerKind.GroundStaging ||
                            em.GetComponentData<UnitHealth>(producer).Current <= 0) continue;
                        site = (Vector3)em.GetComponentData<LocalTransform>(producer).Position + forward * 20f +
                            side * ((airProbeDefenseSlot - 2) * 6f);
                        found = true;
                        break;
                    }
                    if (!found) throw new InvalidOperationException("Own Ground Staging unavailable for opening defense.");
                }
                Vector3 screen = Camera.main.WorldToScreenPoint(site);
                var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
                if (popup != null && popup.IsOpen)
                    return FocusPresentedGuardSite(now, site);
                if (screen.z <= 0 || !Screen.safeArea.Contains(screen))
                    return FocusPresentedGuardSite(now, site);
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(
                        new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                        { position = screen }, hits);
                if (hits.Count > 0)
                    return FocusPresentedGuardSite(now, site);
                if (Environment.GetEnvironmentVariable("WARLINE_S005_CAMERA_TOUCH_ONLY") == "1")
                {
                    bool noOrders = AriaCommandEvidence.Active && airProbeCameraCommandsBefore >= 0 &&
                        AriaCommandEvidence.AcceptedCommands == airProbeCameraCommandsBefore &&
                        page.AttackOrderMask == airProbeCameraAttackMaskBefore && helipadProbeTouch.UnexpectedSamples == 0 &&
                        helipadProbeTouch.PhysicalInterventions == 0;
                    Debug.Log("[S005NativeCameraTouch] result=" + (noOrders ? "Passed" : "Failed") +
                        " cameraFocusObserved=1 attempts=" + airProbeDefenseMapAttempts + " troopCommandsAdded=" +
                        (AriaCommandEvidence.AcceptedCommands - airProbeCameraCommandsBefore) + " input=Touch");
                    EndHelipadProbe(noOrders, noOrders ? "nativeMapCameraFocusWithoutTroopOrders" : "mapCameraTouchIssuedTroopOrders");
                    return false;
                }
                Vector2 point = screen;
                if(planeCanaryActive)PrepareS005GuardMove(tray.ExpandedSlotGroupId(airProbeDefenseSlot),site,point,now);
                if (!helipadProbeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                { EndHelipadProbe(false, "baseGuardTouchRejected"); return false; }
                Debug.Log("[S005AircraftBaseGuard] input=Touch command=AttackMove slot=" + airProbeDefenseSlot +
                    " page=" + airProbeDefensePage + " world=" + site + " point=" + point +
                    " producerRelative=" + (fixedWingCycleOnly ? 1 : 0) + " ownInformationOnly=1");
                // In the normal paid-plane canary dispatch remains raw evidence.
                if(!planeCanaryActive)airProbeDefenseOrders++;
                helipadProbeNext = now + 1d;
                airProbeDefenseStep = 3;
                airProbeDefenseMapAttempts = 0;
            }
            else if (planeCanaryActive&&airProbeDefenseStep==3)
            {
                var accepted=ObserveS005GuardMove(now);
                if(accepted==S005GuardReceiptState.Waiting)return false;
                if(accepted==S005GuardReceiptState.Retry){airProbeDefenseStep=2;return false;}
                airProbeDefenseOrders++;airProbeDefenseStep=4;
            }
            else if (DeselectHelipadSquads(tray, now))
            { airProbeDefenseSlot++; airProbeDefenseStep = 0; }
            return false;
        }

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
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated &&
                    SkirmishLaunchProjection.TryGet(world.EntityManager, out var root, out _))
                {
                    var decision = SkirmishProductionService.EvaluateQueue(world.EntityManager, root,
                        SkirmishRoleIds.LogisticsTruck, 1,
                        SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.CombinedArms), 1);
                    Debug.Log("[S005LogisticsAvailability] accepted=" + decision.Accepted +
                        " reason=" + decision.Reason + " field=" + decision.Field);
                    if (decision.Reason == SkirmishReasonCode.MissingProducer)
                    { EndHelipadProbe(false, "groundProducerLostDuringAircraftCycle"); return; }
                }
                if (drawer != null && drawer.IsOpen && catalog != null &&
                    catalog.TryReadSelectedRequest(out var selectedKey, out var selectedCost,
                        out var selectedFailure, out var selectedProducer))
                    Debug.Log("[S005BuildAvailability] prefab=" + selectedKey + " materialsCost=" + selectedCost +
                        " failure=" + selectedFailure + " requiredProducer=" + selectedProducer);
            }
            if (model.Finished) { EndHelipadProbe(false, "matchEndedBeforeSecondSortie"); return; }
            if (!model.PadPresent) { EndHelipadProbe(false, "padLostBeforeSecondSortie"); return; }

            if (helipadProbeStage == 10)
            {
                if (model.AirRecruitPending || model.OwnAttackAirLive > 0)
                { helipadProbeStage = 11; return; }
                if (!model.LogisticsTruckCommitted)
                {
                    if (model.CanAffordLogisticsTruck && now - airProbePurchaseAt >= 6d)
                        TouchProbeCatalog(now, drawer, catalog, controls,
                            BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tray");
                    return;
                }
                // Establish fuel delivery before spending the remaining stock on
                // an aircraft. Its normal tanker capacity needs time to service
                // the long Campaign-map route; late refuelling can miss the match.
                if (!HasCommittedTanker())
                {
                    if (SkirmishRoleOverlayCatalog.TryGet(SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(),
                            SkirmishRoleKind.Tanker, out var tanker) &&
                        model.OwnMaterials >= tanker.MaterialsCost && now - airProbePurchaseAt >= 6d)
                        TouchProbeCatalog(now, drawer, catalog, controls,
                            BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tanker");
                    return;
                }
                if (model.CanQueueAir) { helipadProbeStage = 11; return; }
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
                if (now - airProbeSortieAt < 5d) return;
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
                airProbeTouchdownFuel = model.OwnAirFuel;
                airProbeRequiredReserve = RequiredSelectedAircraftRoundTripFuel();
                airProbeServiceAt = now + 5d;
                helipadProbeStage = 20;
            }
            if (helipadProbeStage == 20)
            {
                if (now < airProbeServiceAt) return;
                int reserve = airProbeRequiredReserve;
                if (reserve <= 0) { EndHelipadProbe(false, "aircraftFuelRouteUnavailable"); return; }
                if (model.OwnAirFuel < reserve || model.OwnAirFuel <= airProbeTouchdownFuel)
                {
                    // The real supply chain needs a paid tanker. Positive remaining
                    // stock alone cannot pay for another trip across the Campaign map.
                    if (!HasCommittedTanker() && now - airProbePurchaseAt >= 6d)
                        TouchProbeCatalog(now, drawer, catalog, controls,
                            BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tanker");
                    return;
                }
                if (drawer != null && drawer.IsOpen)
                { TapHelipadButton(drawer.CloseButton, now); return; }
                Debug.Log("[SkirmishS003AircraftCycleProbe] homeFuelAvailable=" + model.OwnAirFuel +
                    " tankerRefuelAdded=" + (model.OwnAirFuel - airProbeTouchdownFuel) + " roundTripReserve=" + reserve);
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

        private static int RequiredSelectedAircraftRoundTripFuel()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return 0;
            var em = world.EntityManager;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1) return 0;
            var grid = grids.GetSingleton<GridConfig>();
            using var targets = em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent), typeof(LocalTransform));
            using var bases = targets.ToEntityArray(Allocator.Temp);
            foreach (var target in bases)
            {
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(target).Role != SkirmishObjectiveRoleKind.EnemyBase) continue;
                int2 goal = GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(target).Position);
                using var selected = em.CreateEntityQuery(typeof(UnitAirComponent), typeof(UnitFuelConsumption),
                    typeof(UnitGrid), typeof(SelectedUnitTag));
                using var aircraft = selected.ToEntityArray(Allocator.Temp);
                foreach (var actor in aircraft)
                {
                    int2 delta = goal - em.GetComponentData<UnitGrid>(actor).Cell;
                    float perCell = em.GetComponentData<UnitFuelConsumption>(actor).AirFuelPerCell;
                    float routeCells = 2f * (math.abs(delta.x) + math.abs(delta.y));
                    var air = em.GetComponentData<UnitAirComponent>(actor);
                    if (air.UsesRunway != 0 && em.HasComponent<UnitMove>(actor))
                    {
                        // Qualification reserve only: include runway travel, approach/rejoin
                        // and a full turn using the current native flight geometry. This is
                        // conservative planning, not a guarantee or a gameplay fuel override.
                        float speed = em.GetComponentData<UnitMove>(actor).Speed;
                        float radius = math.max(.01f, speed) / math.radians(45f);
                        float runwayLength = math.distance(air.RunwayTakeoffPosition, air.RunwayLandingPosition);
                        float straight = math.max(grid.CellSize * 10f, speed * 1.5f);
                        float approach = math.max(math.max(grid.CellSize * 24f, runwayLength * 1.25f),
                            straight + radius * 1.5f + grid.CellSize * 6f);
                        float maneuverMetres = 2f * runwayLength + 4f * approach + 2f * math.PI * radius;
                        routeCells += math.SQRT2 * maneuverMetres / grid.CellSize;
                    }
                    return (int)math.ceil(routeCells * perCell) + 10;
                }
            }
            return 0;
        }

        private static SkirmishAircraftProbeTankerOwner airProbeTankerOwner;
        private static bool airProbeTankerCaptureAttempted;

        private static void ResetAircraftProbeTankerOwner()
        {
            airProbeTankerOwner = null;
            airProbeTankerCaptureAttempted = false;
        }

        private static bool HasCommittedTanker()
        {
            return airProbeTankerOwner != null &&
                airProbeTankerOwner.HasCommittedTanker(World.DefaultGameObjectInjectionWorld);
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
                    " target=" + em.HasComponent<UnitTarget>(actor) + " hold=" + em.HasComponent<HoldPositionOrderTag>(actor) +
                    " health=" + (em.HasComponent<UnitHealth>(actor) ? em.GetComponentData<UnitHealth>(actor).Current : -1) +
                    " canAttack=" + (em.HasComponent<UnitCombat>(actor) ? em.GetComponentData<UnitCombat>(actor).CanAttack : -1));
            }
            using var bases = em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent), typeof(UnitHealth));
            using var baseActors = bases.ToEntityArray(Allocator.Temp);
            foreach (var actor in baseActors)
                Debug.Log("[S005AircraftObjective] role=" + em.GetComponentData<SkirmishObjectiveRoleComponent>(actor).Role +
                    " health=" + em.GetComponentData<UnitHealth>(actor).Current +
                    " visible=" + Game.Runtime.SkirmishFogService.IsVisible(em, actor));
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
                { TapHelipadSquadSlot(tray, slot, now); return false; }
            if (page.NextPage && airProbePageTurns++ < 12)
                SwipeHelipadSquadPage(tray, true, now);
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
    // Editor-probe observation only. Capture the owning World/root/attempt once;
    // Entity index/version values alone can alias an Entity in a different World.
    public sealed class SkirmishAircraftProbeTankerOwner
    {
        private readonly World world;
        private readonly Entity session;
        private readonly SkirmishExpandedSessionComponent expected;
        private bool invalidated;

        private SkirmishAircraftProbeTankerOwner(World world, Entity session,
            SkirmishExpandedSessionComponent expected)
        {
            this.world = world;
            this.session = session;
            this.expected = expected;
        }

        public static bool TryCapture(World world, Entity session,
            out SkirmishAircraftProbeTankerOwner owner)
        {
            owner = null;
            if (world == null || !world.IsCreated || session == Entity.Null) return false;
            var em = world.EntityManager;
            if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return false;
            var current = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (current.SessionId.Length == 0 || !IsActive(current.Phase)) return false;
            owner = new SkirmishAircraftProbeTankerOwner(world, session, current);
            return true;
        }

        public bool Matches(World currentWorld, Entity currentSession)
        {
            if (invalidated) return false;
            if (!ReferenceEquals(world, currentWorld) || world == null || !world.IsCreated ||
                currentSession != session) { invalidated = true; return false; }
            var em = world.EntityManager;
            if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session))
            { invalidated = true; return false; }
            var current = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            bool matches = IsActive(current.Phase) && current.SessionId.Equals(expected.SessionId) &&
                current.SetupHash == expected.SetupHash && current.Seed == expected.Seed &&
                current.ContentVersion == expected.ContentVersion &&
                current.CatalogId.Equals(expected.CatalogId) && current.DefinitionId.Equals(expected.DefinitionId);
            if (!matches) invalidated = true;
            return matches;
        }

        public bool HasCommittedTanker(World currentWorld)
        {
            if (!Matches(currentWorld, session)) return false;
            var em = world.EntityManager;
            using var units = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(Faction), typeof(UnitHealth));
            using var actors = units.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (owned.SessionId.Equals(expected.SessionId) && owned.FactionId == 1 &&
                    owned.IsStructure == 0 && owned.CapacityReleased == 0 &&
                    em.GetComponentData<Faction>(actor).Id == 1 &&
                    em.GetComponentData<UnitHealth>(actor).Current > 0 &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == SkirmishRoleKind.Tanker)
                    return true;
            }
            if (!em.HasBuffer<SkirmishProductionReservation>(session)) return false;
            foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                if (receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.Tanker &&
                    receipt.MemberCount > receipt.DeliveredMembers && receipt.DeliveredMembers >= 0 &&
                    (receipt.Phase == SkirmishReservationPhase.Reserved ||
                     receipt.Phase == SkirmishReservationPhase.Producing)) return true;
            return false;
        }

        private static bool IsActive(SkirmishSessionPhase phase) =>
            phase == SkirmishSessionPhase.Playing;
    }

}
