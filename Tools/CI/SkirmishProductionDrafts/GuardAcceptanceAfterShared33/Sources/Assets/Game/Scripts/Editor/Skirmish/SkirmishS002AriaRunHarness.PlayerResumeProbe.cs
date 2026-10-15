#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using Game.UI.Contracts;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool playerResumeProbeActive, playerResumeProbeDone;
        private static int playerResumeStep;
        private static int playerResumeLoggedStep;
        private static double playerResumeDue, playerResumeDeadline;
        private static double playerResumeDiagnosticDue;
        private static Vector2 playerResumeContinuePoint;
        private static double playerResumeContinueStableAt, playerResumeContinueAttemptAt;
        private static double qualificationEconomyDiagnosticDue;
        private static double playerResumeWorldDiagnosticDue;
        private static bool fabricationSelectionOnly;
        private static int fabricationSelectionStep;
        private static AriaTouchInputUiSystemHelper playerResumeTouch;
        private static string playerResumeSessionId;
        private static float playerResumeSavedElapsed;
        private static uint playerResumeReceipt;
        private static bool playerResumeCombat;
        private static bool playerResumeBoarded;
        private static int playerResumeBoardStep, playerResumePassengerCount;
        private static int playerResumeUnloadRelocations;
        private static Vector3 playerResumeUnloadSite;
        private static readonly List<Entity> playerResumeRestoredPassengers = new();
        private static uint playerResumeCombatGestures;
        private static uint playerResumeCarriedGestures;
        private static int playerResumeCombatAttackReceipts;
        private static bool playerResumeCombatInputCarryPending;
        private static SkirmishRoleKind playerResumePaidRole;
        private static int playerResumePaidCost;
        private static bool playerResumePaidNeedsDelivery;
        private static SkirmishReadinessStage playerResumeRequiredReadiness;
        private static bool playerResumeGuardReady, playerResumeLogisticsReady;
        private static readonly List<Entity> playerResumeOldEntities = new();
        private static bool paidGroundSeen, paidGroundMoved, paidGroundFired, paidGroundDamaged, paidGroundDeathReleased;
        private static Vector3 paidGroundFirstPosition;
        private static float paidGroundDisplacement;
        public static void RunS005EditorSaveResumeEn() => RunS005EditorSaveResume("en");
        public static void RunS005EditorSaveResumeFa() => RunS005EditorSaveResume("fa-IR");
        public static void RunS005EditorFabricationSelectionProbe()
        {
            RunS005EditorSaveResumeEn();
            Environment.SetEnvironmentVariable("WARLINE_S005_FABRICATION_SELECTION_PROBE", "1");
        }
        public static void RunS005EditorCombatSaveResumeEn()
        {
            RunEditorJourney("en", true, false, "S005", 130368);
            playerResumeProbeDone = false;
            Environment.SetEnvironmentVariable("WARLINE_S005_PLAYER_RESUME_PROBE", "1");
            Environment.SetEnvironmentVariable("WARLINE_S005_COMBAT_RESUME_PROBE", "1");
        }
        public static void RunS005EditorHeavyApcSaveResumeEn()
        {
            RunEditorJourney("en", true, false, "S005", 155926);
            playerResumeProbeDone = false;
            Environment.SetEnvironmentVariable("WARLINE_S005_PLAYER_RESUME_PROBE", "1");
            Environment.SetEnvironmentVariable("WARLINE_S005_COMBAT_RESUME_PROBE", "1");
            Environment.SetEnvironmentVariable("WARLINE_S005_SAVE_ROLE", "ApcHeavy");
        }
        public static void RunS005EditorBoardedApcSaveResumeEn()
        {
            RunS005EditorHeavyApcSaveResumeEn();
            Environment.SetEnvironmentVariable("WARLINE_S005_COMBAT_RESUME_PROBE", null);
            Environment.SetEnvironmentVariable("WARLINE_S005_BOARDED_RESUME_PROBE", "1");
        }
        public static void RunS005EditorBoardedRecoveryProbeEn()
        {
            RunS005EditorBoardedApcSaveResumeEn();
            Environment.SetEnvironmentVariable("WARLINE_S005_NATIVE_RECOVERY_ONLY", "1");
        }
        public static void RunS005EditorTankSaveResumeEn()
        {
            RunS005EditorHeavyApcSaveResumeEn();
            Environment.SetEnvironmentVariable("WARLINE_S005_SAVE_ROLE", "Tank");
        }
        public static void RunS005EditorSiegeSaveResumeEn()
        {
            RunS005EditorHeavyApcSaveResumeEn();
            Environment.SetEnvironmentVariable("WARLINE_S005_SAVE_ROLE", "Siege");
        }
        public static void RunS005EditorAirportPlacementProbe()
        {
            RunS005EditorSiegeSaveResumeEn();
            Environment.SetEnvironmentVariable("WARLINE_S005_AIRPORT_PLACEMENT_PROBE", "1");
        }
        public static void RunS005EditorStrikeCycleProbe()
        {
            RunS005EditorAirportPlacementProbe();
            Environment.SetEnvironmentVariable("WARLINE_S005_FIXED_WING_CYCLE", "1");
        }
        private static void RunS005EditorSaveResume(string language)
        {
            RunEditorJourney(language, false, false, "S005");
            playerResumeProbeDone = false;
            Environment.SetEnvironmentVariable("WARLINE_S005_PLAYER_RESUME_PROBE", "1");
        }
        private static void StartPlayerResumeProbe(EntityManager em, Entity session)
        {
            playerResumeProbeActive = true;
            fabricationSelectionOnly = Environment.GetEnvironmentVariable("WARLINE_S005_FABRICATION_SELECTION_PROBE") == "1";
            Environment.SetEnvironmentVariable("WARLINE_S005_FABRICATION_SELECTION_PROBE", null);
            fabricationSelectionStep = 0;
            playerResumeWorldDiagnosticDue = 0;
            paidGroundSeen = paidGroundMoved = paidGroundFired = paidGroundDamaged = paidGroundDeathReleased = false;
            paidGroundDisplacement = 0;
            playerResumeStep = 0; playerResumeReceipt = 0;
            playerResumeCombat = Environment.GetEnvironmentVariable("WARLINE_S005_COMBAT_RESUME_PROBE") == "1";
            playerResumeCombatGestures = 0;
            playerResumeCarriedGestures = 0;
            playerResumeCombatAttackReceipts = 0;
            playerResumeCombatInputCarryPending = false;
            playerResumeBoarded = Environment.GetEnvironmentVariable("WARLINE_S005_BOARDED_RESUME_PROBE") == "1";
            playerResumeBoardStep = playerResumePassengerCount = 0;
            playerResumeUnloadRelocations = 0;
            playerResumeUnloadSite = default;
            playerResumeRestoredPassengers.Clear();
            playerResumePaidRole = Environment.GetEnvironmentVariable("WARLINE_S005_SAVE_ROLE") switch {
                "ApcHeavy" => SkirmishRoleKind.ApcHeavy,
                "Tank" => SkirmishRoleKind.Tank,
                "Siege" => SkirmishRoleKind.Siege,
                _ => SkirmishRoleKind.AntiAir
            };
            if (!Game.Configs.SkirmishRoleOverlayCatalog.TryGet(
                Game.Configs.SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(), playerResumePaidRole, out var paidRole))
                throw new InvalidOperationException("Paid role overlay missing.");
            playerResumePaidCost = paidRole.MaterialsCost;
            ResetFixedWingCycleProbe();
            ResetPaidGroundIndustryProbe(em);
            ResetAirportPlacementProbe();
            playerResumeRequiredReadiness = Game.Configs.SkirmishProductionEligibility.RequiredReadiness(playerResumePaidRole);
            playerResumePaidNeedsDelivery = playerResumePaidRole != SkirmishRoleKind.AntiAir;
            playerResumeLogisticsReady = !playerResumeBoarded &&
                playerResumePaidRole != SkirmishRoleKind.Tank && playerResumePaidRole != SkirmishRoleKind.Siege;
            playerResumeGuardReady = !playerResumePaidNeedsDelivery;
            airProbeDefenseSlot = airProbeDefenseStep = airProbeDefenseMapStep = airProbeDefenseMapAttempts = 0;
            airProbeDefensePage = airProbeDefenseOrders = 0;
            airProbeDefensePaging.Reset();ResetS005GuardObservation();
            playerResumeLoggedStep = -1;
            playerResumeDiagnosticDue = 0;
            playerResumeContinueStableAt = playerResumeContinueAttemptAt = 0;
            qualificationEconomyDiagnosticDue = 0;
            playerResumeSessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString();
            playerResumeDeadline = EditorApplication.timeSinceStartup +
                (playerResumePaidRole is SkirmishRoleKind.Tank or SkirmishRoleKind.Siege ? 1080 :
                    playerResumeCombat || playerResumeBoarded ? 600 : 180);
            playerResumeDue = EditorApplication.timeSinceStartup + 1;
            playerResumeOldEntities.Clear();
            using var query = em.CreateEntityQuery(new EntityQueryDesc { Any = new[] {
                ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(), ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                ComponentType.ReadOnly<OperationMapBuildingComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities) playerResumeOldEntities.Add(entity);
            playerResumeTouch = new AriaTouchInputUiSystemHelper();
            AriaCommandEvidence.Begin();
            if (!playerResumeTouch.Start()) throw new InvalidOperationException("Save/Resume Touch unavailable.");
            var runner = new GameObject("S005PlayerSaveResumeInputProbe");
            UnityEngine.Object.DontDestroyOnLoad(runner); runner.AddComponent<SkirmishS005PlayerResumeFrame>();
        }
        internal static void TickPlayerResumeFrame()
        {
            if (!playerResumeProbeActive) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (playerResumeLoggedStep != playerResumeStep)
                {
                    playerResumeLoggedStep = playerResumeStep;
                    Debug.Log("[S005PlayerSaveResume] phase=Input step=" + playerResumeStep);
                    ScreenCapture.CaptureScreenshot(LiveTracePath() + ".save-step-" + playerResumeStep + ".png");
                }
                if (now > playerResumeDeadline) throw new InvalidOperationException("Player Save/Resume timed out at step=" + playerResumeStep);
                if (playerResumeStep == -1)
                {
                    TickCombatBeforePlayerSave();
                    return;
                }
                playerResumeTouch.Tick(Time.unscaledTime);
                if (playerResumeTouch.PlayerInterrupted || playerResumeTouch.UnexpectedSamples != 0) throw new InvalidOperationException("Save/Resume input interrupted.");
                if (playerResumeTouch.IsBusy || now < playerResumeDue) return;
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                bool hasSession = SkirmishLaunchProjection.TryGet(em, out Entity session, out var match);
                // A terminal result stops simulation and timeScale. Fail before
                // the inactive-simulation guard so setup probes cannot wait
                // silently behind a Defeat screen until their wall watchdog.
                if (playerResumeStep == 0 && hasSession && match.Phase == SkirmishPhase.Finished)
                    throw new InvalidOperationException("Mission ended during qualification setup: " + match.Outcome);
                if (TickS005OpeningCameraCalibration(em, hasSession, session)) return;
                if (playerResumeStep == -2)
                {
                    TickBoardedPlayerSave(em, now);
                    return;
                }
                if (playerResumeStep == 0)
                {
                    if (!hasSession || !SkirmishLaunchProjection.IsSimulationActive(em) || !Mathf.Approximately(Time.timeScale, 1)) return;
                    if (fabricationSelectionOnly) { TickFabricationSelectionProbe(em, now); return; }
                    if (airportCameraNavigationOnly) { TickAirportCameraNavigationProbe(now); return; }
                    if ((fixedWingCycleOnly || airportPlacementOnly) && now >= qualificationEconomyDiagnosticDue)
                    {
                        qualificationEconomyDiagnosticDue = now + 15;
                        float elapsed = SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match);
                        AppendQualificationEconomyDiagnostic(em, elapsed);
                        AppendActorDiagnostic(em, elapsed);
                    }
                    if (!playerResumeGuardReady)
                    {
                        var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                        var commands = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                        if (hud == null || commands == null) return;
                        helipadProbeTouch = playerResumeTouch;
                        playerResumeGuardReady = GuardPresentedBase(now, hud, commands);
                        playerResumeDue = Math.Max(now + .1, helipadProbeNext);
                        if (playerResumeGuardReady)
                        {
                            helipadProbeTouch = null;
                            Debug.Log("[S005RoleLifecycle] phase=ProducerDefense input=Touch groups=" +
                                airProbeDefenseOrders + " pages=" + (airProbeDefensePage + 1) +
                                " producerRelative=" + (fixedWingCycleOnly ? 1 : 0));
                        }
                        return;
                    }
                    if (fixedWingCycleOnly && !SkirmishProductionService.HasLivingProducer(em, session,
                        SkirmishProducerKind.GroundStaging, 1))
                        throw new InvalidOperationException("Ground Staging destroyed during paid aircraft setup; native production cannot continue.");
                    // Start one extra factory, then fund the fleet. Native708's
                    // full industry purchase first delayed the early Oil fleet.
                    // The later refinery/store stay near the original Oil source.
                    if (fixedWingCycleOnly && !paidIndustryWarmReady &&
                        !TickPaidGroundIndustryProbe(em, now, true)) return;
                    if (!playerResumeLogisticsReady)
                    {
                        int live = 0;
                        // Count authored starting Oil trucks as well as paid
                        // replacements. Native709 left 1,316 produced fuel in
                        // its two refineries at the deadline. Fund materials with
                        // four Oil trucks / two Tankers, then switch to two/four
                        // after paid aircraft delivery, within support cap6.
                        int requiredTrucks = fixedWingCycleOnly ? FixedWingQualificationOilTrucks : 1;
                        if (fixedWingCycleOnly)
                        {
                            using var oilActors = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                                typeof(SkirmishUnitRoleComponent), typeof(UnitHealth));
                            using var oilUnits = oilActors.ToEntityArray(Allocator.Temp);
                            foreach (var oil in oilUnits)
                            {
                                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(oil);
                                if (owner.FactionId == 1 && owner.CapacityReleased == 0 &&
                                    owner.SessionId.ToString() == playerResumeSessionId &&
                                    em.GetComponentData<SkirmishUnitRoleComponent>(oil).Role == SkirmishRoleKind.LogisticsTruck &&
                                    em.GetComponentData<UnitHealth>(oil).Current > 0) live++;
                            }
                        }
                        if (em.HasBuffer<SkirmishProductionReservation>(session))
                            foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                                if (receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.LogisticsTruck && receipt.MaterialsPaid == 100)
                                {
                                    if (receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing) return;
                                    if (!fixedWingCycleOnly && receipt.Phase == SkirmishReservationPhase.Live && HasLivingPaidTruck(em, receipt.ReservationId)) live++;
                                }
                        if (live >= requiredTrucks)
                        {
                            playerResumeLogisticsReady = true;
                            Debug.Log("[S005RoleLifecycle] phase=LogisticsBeforeResearch role=LogisticsTruck cost=100 count=" + live + " input=Touch");
                            return;
                        }
                        var logisticsDrawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                        var logisticsCatalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                        Button truck = logisticsDrawer != null && logisticsDrawer.IsOpen
                            ? logisticsCatalog?.ResolveCatalogTarget(BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tray")
                            : UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>()?.BuildButton;
                        if (!PlayerResumeTap(truck)) PlayerResumeScrollTo(truck);
                        return;
                    }
                    // Fuel delivery must run while industry and research are paid for.
                    // Buying one Tanker after the Airport left the native Strike
                    // below its round-trip reserve for the entire 18-minute match.
                    if (fixedWingCycleOnly && !EnsurePaidFuelStorage(em, now)) return;
                    int setupTankers = FixedWingQualificationTankers;
                    if (fixedWingCycleOnly && CountCommittedFixedWingTankers(em, session) < setupTankers)
                    {
                        var fuelDrawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                        TouchFixedWingCatalog(fuelDrawer, BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tanker");
                        return;
                    }
                    if (playerResumePaidRole == SkirmishRoleKind.Siege &&
                        !TickPaidGroundIndustryProbe(em, now)) return;
                    if (paidIndustryReady && Environment.GetEnvironmentVariable("WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY") == "1")
                    {
                        var clearanceDrawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                        if (clearanceDrawer != null && clearanceDrawer.IsOpen)
                        { PlayerResumeTap(clearanceDrawer.CloseButton); return; }
                        if (Environment.GetEnvironmentVariable("WARLINE_S005_PAID_FUEL_CLICK_ONLY") == "1")
                        { TickStartingFuelClickProbe(now); return; }
                        ScreenCapture.CaptureScreenshot(LiveTracePath() + ".placement-clearance-paid.png");
                        SkirmishBuildingPlacementProbe.InspectSharedSceneryClearance(LiveTracePath() + ".placement-clearance-paid.json");
                        Debug.Log("[S005PaidBuildingClearance] result=Passed input=Touch industryTargets=3Factories,OilPump,Refinery,FuelStore budgetsUnchanged=1 sourceGeometryModified=0 completeMatch=NotClaimed");
                        Finish(false, "sharedPaidBuildingClearanceOnly");
                        return;
                    }
                    if (!TickFixedWingLocalFuelSupplyThroughInput(em, now)) return;
                    if (!TickFixedWingFactorySupplyThroughInput(em, now)) return;
                    if (playerResumePaidNeedsDelivery &&
                        UiShellRuntimeGateway.TryReadSkirmishReadiness(out var readiness) &&
                        readiness.Stage < (int)playerResumeRequiredReadiness)
                    {
                        if (readiness.InProgress || !readiness.CanUpgrade) return;
                        var researchDrawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                        PlayerResumeTap(researchDrawer != null && researchDrawer.IsOpen ? researchDrawer.ReadinessButton :
                            UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>()?.BuildButton);
                        return;
                    }
                    if (airportPlacementOnly)
                    {
                        TickAirportPlacementProbe(em, session, now);
                        return;
                    }
                    if (em.HasBuffer<SkirmishProductionReservation>(session))
                        foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                            if (receipt.FactionId == 1 && receipt.Role == playerResumePaidRole && receipt.MaterialsPaid == playerResumePaidCost &&
                                (receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing ||
                                playerResumePaidNeedsDelivery && receipt.Phase == SkirmishReservationPhase.Live))
                            {
                                if (playerResumePaidNeedsDelivery)
                                {
                                    if (receipt.Phase != SkirmishReservationPhase.Live) return;
                                    bool delivered = false;
                                    using var paidActors = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(UnitHealth));
                                    using var paidEntities = paidActors.ToEntityArray(Allocator.Temp);
                                    foreach (var actor in paidEntities)
                                        if (em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).ReservationId == receipt.ReservationId &&
                                            em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == playerResumePaidRole &&
                                            em.GetComponentData<UnitHealth>(actor).Current > 0) delivered = true;
                                    if (!delivered) throw new InvalidOperationException("Paid " + playerResumePaidRole + " delivery missing.");
                                    Debug.Log("[S005RoleLifecycle] phase=PaidDelivery role=" + playerResumePaidRole +
                                        " cost=" + playerResumePaidCost + " input=Touch receipt=" + receipt.ReservationId);
                                }
                                playerResumeReceipt = receipt.ReservationId;
                                if (playerResumeCombat)
                                {
                                    playerResumeCombatGestures = playerResumeTouch.CompletedGestures;
                                    playerResumeTouch.Dispose(); playerResumeTouch = null;
                                    playerResumeStep = -1;
                                    // Recruitment and paid research can consume most of
                                    // the setup watchdog. Give combat its own bounded
                                    // window within the unchanged normal match clock.
                                    float elapsed = SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match);
                                    playerResumeDeadline = now + Math.Min(600d, Math.Max(0d, 1080d - elapsed)) + 60d;
                                    StartS004ManualProbe();
                                }
                                else playerResumeStep = playerResumeBoarded ? -2 : 1;
                                return;
                            }
                    var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                    var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                    Button button = drawer != null && drawer.IsOpen ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Vehicles,
                        Game.Configs.SkirmishRoleCatalogConfig.RuntimePrefabKey(playerResumePaidRole))
                        : UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>()?.BuildButton;
                    if (!PlayerResumeTap(button)) PlayerResumeScrollTo(button);
                    return;
                }
                if (playerResumeStep == 1)
                {
                    var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                    if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return; }
                    if (!UiShellRuntimeGateway.TryReadSkirmish(out var model)) return;
                    if (model.Finished || match.Phase == SkirmishPhase.Finished)
                        throw new InvalidOperationException("Mission ended before native Pause and Save.");
                    // A combat camera gesture can leave the public full-map panel
                    // open when this driver takes over. Close it with normal input
                    // before trying the HUD Pause button behind that panel.
                    var fullMap = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
                    if (fullMap != null && fullMap.IsOpen)
                    { PlayerResumeTap(fullMap.CloseAction); return; }
                    if (model.Paused)
                    {
                        // Boarding can complete between observing the first passenger
                        // and the actual native Pause touch. Freeze the assertion at
                        // the same paused state that Save & Exit will capture.
                        if (playerResumeBoarded)
                        {
                            Entity carrier = PlayerResumeCarrier(em);
                            var passengers = em.GetBuffer<UnitTransportPassengerElement>(carrier);
                            if (passengers.Length < 1) throw new InvalidOperationException("No boarded passengers at paused save.");
                            foreach (var entry in passengers) VerifyBoardedPassenger(em, entry.Passenger, carrier);
                            playerResumePassengerCount = passengers.Length;
                            Debug.Log("[S005PassengerSave] phase=PausedSnapshot passengers=" + playerResumePassengerCount);
                        }
                        playerResumeStep = 2;
                        playerResumeSavedElapsed = SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match);
                        return;
                    }
                    foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (button.name == "PauseButton" && PlayerResumeTap(button)) return;
                    return;
                }
                if (playerResumeStep == 2)
                {
                    if (!hasSession || !UiShellRuntimeGateway.TryReadSkirmish(out var model) || !model.Paused || !model.CanSaveMatch)
                        throw new InvalidOperationException("Player save control unavailable while paused.");
                    if (Mathf.Abs(SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match) - playerResumeSavedElapsed) > .05f)
                        throw new InvalidOperationException("Paused clock advanced.");
                    if (model.SaveFailed) throw new InvalidOperationException("Native player capture rejected.");
                    var pause = UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
                    if (pause == null) return;
                    ScreenCapture.CaptureScreenshot(LiveTracePath() + ".save-pause.png");
                    if (PlayerResumeTap(pause.RestartButton)) playerResumeStep = 3;
                    return;
                }
                if (playerResumeStep == 3)
                {
                    // A Continue retry can return here while the accepted launch
                    // is still tearing down setup. Once its session appears,
                    // observe recovery instead of waiting for setup again.
                    // The next step still checks the checkpoint, attempt identity,
                    // paid receipt, clock and removal of the old battlefield.
                    if (hasSession && playerResumeContinueAttemptAt > 0)
                    {
                        playerResumeStep = 4;
                        Debug.Log("[S005PlayerSaveResume] phase=ContinueObservedAfterRetry input=Touch");
                        return;
                    }
                    if (hasSession)
                    {
                        if (UiShellRuntimeGateway.TryReadSkirmish(out var model) && model.SaveFailed) throw new InvalidOperationException("Player Save & Exit failed.");
                        return;
                    }
                    if (!SkirmishNativePlayerSaveService.TryRead("S005", out var saved, out _) || saved.SessionId != playerResumeSessionId)
                        throw new InvalidOperationException("Saved attempt identity was not preserved.");
                    // Destroyed static blockers keep cleanup bounds until a fresh
                    // grid exists. They must have no battlefield state before Continue.
                    foreach (Entity old in playerResumeOldEntities)
                        if (em.Exists(old) && (em.HasComponent<SkirmishAttemptOwnedComponent>(old) ||
                            em.HasComponent<RuntimeBuildingCombatInfo>(old) || em.HasComponent<OperationMapBuildingComponent>(old) ||
                            em.HasComponent<Unity.Transforms.LocalTransform>(old) || em.HasComponent<UnitHealth>(old))) return;
                    foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (!setup.IsBaseAssaultSetup || setup.SelectedScenarioId != "S005") continue;
                        foreach (var button in setup.GetComponentsInChildren<Button>())
                        {
                            var label = button.GetComponentInChildren<TMP_Text>();
                            // Persian text is shaped into presentation glyphs by the native UI.
                            // Select the saved setup's semantic control, then dispatch a real touch.
                            if (button.name != "LaunchMissionButton" || label == null || string.IsNullOrWhiteSpace(label.text)) continue;
                            // Setup animates into place after Save & Exit. A hit
                            // at dispatch time is not proof that the same control
                            // remains under the finger when the touch ends.
                            if (!TryPresentedButtonPoint(button, out var point))
                            { playerResumeContinueStableAt = 0; return; }
                            if (playerResumeContinueStableAt == 0 || Vector2.Distance(point, playerResumeContinuePoint) > 1f)
                            {
                                playerResumeContinuePoint = point;
                                playerResumeContinueStableAt = now;
                                return;
                            }
                            if (now - playerResumeContinueStableAt < .5) return;
                            ScreenCapture.CaptureScreenshot(LiveTracePath() + ".continue-setup.png");
                            if (PlayerResumeTap(button))
                            { playerResumeContinueAttemptAt = now; playerResumeStep = 4; }
                            return;
                        }
                    }
                    return;
                }
                if (!hasSession)
                {
                    // Retry only while the original saved setup remains visible;
                    // never invoke LaunchMatch or restore state directly.
                    if (now - playerResumeContinueAttemptAt >= 2)
                        foreach (var setup in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                            if (setup.IsBaseAssaultSetup && setup.SelectedScenarioId == "S005")
                            {
                                playerResumeContinueStableAt = 0;
                                playerResumeStep = 3;
                                Debug.Log("[S005PlayerSaveResume] phase=ContinueRetry input=Touch reason=SavedSetupStillVisible");
                                break;
                            }
                    return;
                }
                if (match.Phase != SkirmishPhase.Playing || !SkirmishLaunchProjection.IsSimulationActive(em)) return;
                if (!em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(session)) throw new InvalidOperationException("Continue launched a fresh match.");
                var recovery = em.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(session);
                if (!recovery.Complete) return;
                if (em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString() != playerResumeSessionId ||
                    SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match) < playerResumeSavedElapsed || !Mathf.Approximately(Time.timeScale, 1))
                    throw new InvalidOperationException("Restored identity, clock or normal speed differs.");
                foreach (Entity old in playerResumeOldEntities)
                    if (em.Exists(old)) throw new InvalidOperationException("Old battlefield cleanup remains after fresh recovery.");
                if (playerResumeBoarded && !VerifyAndUnloadRestoredPassengers(em, now)) return;
                int receiptMatches = 0;
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                    if (receipt.ReservationId == playerResumeReceipt && receipt.MaterialsPaid == playerResumePaidCost && receipt.Role == playerResumePaidRole) receiptMatches++;
                if (receiptMatches != 1 || playerResumeTouch.CompletedGestures + playerResumeCombatGestures < 5) throw new InvalidOperationException("Paid receipt or native input was not preserved: receipts=" + receiptMatches +
                    " currentGestures=" + playerResumeTouch.CompletedGestures + " earlierGestures=" + playerResumeCombatGestures);
                if (!AriaCommandEvidence.TryReadAudit(playerResumeTouch.CompletedGestures + playerResumeCombatGestures, playerResumeTouch.UnexpectedSamples,
                    out int receiptViolations) || receiptViolations != 0)
                    throw new InvalidOperationException("Save/Resume native command receipt audit failed.");
                Debug.Log("[S005PlayerSaveResume] result=Passed input=Touch gestures=" + playerResumeTouch.CompletedGestures +
                    " oldPhysicalGone=" + playerResumeOldEntities.Count + " paidReceiptPreserved=1 sameAttempt=1 savedElapsed=" + playerResumeSavedElapsed +
                    " paidRole=" + playerResumePaidRole + " paidCost=" + playerResumePaidCost + " combatGestures=" + playerResumeCombatGestures + " combatSave=" + (playerResumeCombat ? 1 : 0) +
                    " boardedSave=" + (playerResumeBoarded ? 1 : 0) + " passengers=" + playerResumePassengerCount +
                    " commands=" + AriaCommandEvidence.AcceptedCommands + " receiptViolations=" + receiptViolations + " resumedMissionAcceptance=Pending");
                ScreenCapture.CaptureScreenshot(LiveTracePath() + ".resumed-opening.png");
                // Continuing the same battle also continues its real input history.
                // Restored attack orders can legitimately win before a new order.
                // Carry only after identity, recovery and the complete pre-save /
                // Save/Continue receipt stream have passed the checks above.
                if (playerResumeCombat)
                {
                    playerResumeCarriedGestures = playerResumeCombatGestures + playerResumeTouch.CompletedGestures;
                    playerResumeCombatInputCarryPending = true;
                    Debug.Log("[S005CombatSave] phase=VerifiedInputCarry sameAttempt=1 touches=" + playerResumeCarriedGestures +
                        " attacks=" + playerResumeCombatAttackReceipts + " commands=" + AriaCommandEvidence.AcceptedCommands);
                }
                playerResumeProbeDone = true; playerResumeProbeActive = false;
                playerResumeTouch.Dispose(); playerResumeTouch = null;
                // The separately verified Save/Continue stream is preserved in
                // the trace. ARIA owns the next audit and its own driver counts.
                if (!playerResumeCombat) AriaCommandEvidence.End();
                if (Environment.GetEnvironmentVariable("WARLINE_S005_NATIVE_RECOVERY_ONLY") == "1")
                {
                    // This focused checkpoint case never enters the win ledger.
                    // Complete mission/result/return evidence is a separate gate.
                    Finish(false, "nativePaidBoardedPassengerRecoveryOnly");
                    return;
                }
                if (!playerResumeCombat) AriaCommandEvidence.Begin();
                next = now + 1;
            }
            catch (Exception exception)
            {
                Debug.LogError("[S005PlayerSaveResume] result=Failed step=" + playerResumeStep + " reason=" + exception.Message);
                playerResumeProbeActive = false; playerResumeTouch?.Dispose(); playerResumeTouch = null;
                Finish(true, "playerSaveResume=" + exception.Message);
            }
        }
        private static Entity PlayerResumeCarrier(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent),
                typeof(Faction), typeof(UnitHealth), typeof(Unity.Transforms.LocalTransform));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).ReservationId == playerResumeReceipt &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == SkirmishRoleKind.ApcHeavy &&
                    em.GetComponentData<Faction>(actor).Id == 1 && em.GetComponentData<UnitHealth>(actor).Current > 0)
                    return actor;
            throw new InvalidOperationException("Live paid friendly transport missing.");
        }
        // Observation only: the paid actor must move/fire under the visible
        // manual controls. This never creates an actor, order, resource or win.
        private static void ObservePaidGroundRole(EntityManager em, Entity session)
        {
            if (Environment.GetEnvironmentVariable("WARLINE_S005_PLAYER_RESUME_PROBE") != "1" ||
                playerResumePaidRole is not (SkirmishRoleKind.Tank or SkirmishRoleKind.Siege)) return;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                typeof(SkirmishUnitRoleComponent), typeof(UnitHealth), typeof(Unity.Transforms.LocalTransform));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (owner.FactionId != 1 || owner.ReservationId != playerResumeReceipt ||
                    owner.SessionId.ToString() != playerResumeSessionId ||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role != playerResumePaidRole) continue;
                Vector3 position = (Vector3)em.GetComponentData<Unity.Transforms.LocalTransform>(actor).Position;
                if (!paidGroundSeen) { paidGroundSeen = true; paidGroundFirstPosition = position; }
                paidGroundDisplacement = Mathf.Max(paidGroundDisplacement, Vector3.Distance(position, paidGroundFirstPosition));
                bool commanded = em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor) &&
                    SkirmishArmyGroupSystem.TryGet(em, session,
                        em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId, out var group) &&
                    group.LastOrder is SkirmishGroupOrderKind.Move or SkirmishGroupOrderKind.Attack;
                paidGroundMoved |= commanded && paidGroundDisplacement > 2f;
                paidGroundFired |= em.HasComponent<UnitAttackTraceComponent>(actor) &&
                    em.GetComponentData<UnitAttackTraceComponent>(actor).ShotCounter > 0 ||
                    em.HasComponent<GroundMissileInFlightComponent>(actor);
                var health = em.GetComponentData<UnitHealth>(actor);
                paidGroundDamaged |= health.Current < health.Max;
                paidGroundDeathReleased |= health.Current <= 0 && owner.CapacityReleased != 0;
            }
        }
        private static void ReportPaidGroundRoleObservation()
        {
            if (Environment.GetEnvironmentVariable("WARLINE_S005_PLAYER_RESUME_PROBE") != "1" ||
                playerResumePaidRole is not (SkirmishRoleKind.Tank or SkirmishRoleKind.Siege)) return;
            Debug.Log("[S005PaidGroundRoleObservation] result=" + (paidGroundSeen && paidGroundMoved && paidGroundFired ? "Passed" : "NotObserved") +
                " role=" + playerResumePaidRole + " paidCost=" + playerResumePaidCost +
                " seen=" + (paidGroundSeen ? 1 : 0) + " commandedMovement=" + (paidGroundMoved ? 1 : 0) +
                " displacement=" + paidGroundDisplacement + " nativeFire=" + (paidGroundFired ? 1 : 0) +
                " receivedDamage=" + (paidGroundDamaged ? 1 : 0) + " deathReleased=" + (paidGroundDeathReleased ? 1 : 0) +
                " input=Touch fullRosterCertificate=0");
        }
        private static bool HasLivingPaidTruck(EntityManager em, uint receiptId)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent), typeof(UnitHealth));
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (owner.FactionId == 1 && owner.ReservationId == receiptId &&
                    owner.SessionId.ToString() == playerResumeSessionId &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == SkirmishRoleKind.LogisticsTruck &&
                    em.GetComponentData<UnitHealth>(actor).Current > 0) return true;
            }
            return false;
        }
        private static bool PlayerResumeWorldTap(EntityManager em, Entity carrier, double now)
        {
            Vector3 site = (Vector3)em.GetComponentData<Unity.Transforms.LocalTransform>(carrier).Position + Vector3.up;
            return PlayerResumeWorldPointTap(site, now);
        }
        private static bool PlayerResumeWorldPointTap(Vector3 site, double now)
        {
            Vector3 screen = Camera.main != null ? Camera.main.WorldToScreenPoint(site) : Vector3.zero;
            var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null) events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = screen }, hits);
            if (popup != null && popup.IsOpen || screen.z <= 0 || !Screen.safeArea.Contains(screen) || hits.Count > 0)
            {
                if (now >= playerResumeWorldDiagnosticDue)
                {
                    playerResumeWorldDiagnosticDue = now + 2;
                    Debug.Log("[S005WorldSelectionDiagnostic] world=" + site + " screen=" + screen +
                        " popup=" + (popup != null && popup.IsOpen) + " firstHit=" +
                        (hits.Count > 0 ? hits[0].gameObject.name : "none") + " camera=" + Camera.main?.transform.position);
                }
                helipadProbeTouch = playerResumeTouch;
                FocusPresentedGuardSite(now, site);
                playerResumeDue = Math.Max(now + .1, helipadProbeNext);
                helipadProbeTouch = null;
                return false;
            }
            if (!playerResumeTouch.TryGesture(screen, screen, .15f, 0, Time.unscaledTime)) return false;
            airProbeDefenseMapAttempts = 0;
            playerResumeDue = now + .8;
            Debug.Log("[S005PassengerSave] phase=TransportTarget input=Touch point=" + screen);
            return true;
        }
        private static void TickBoardedPlayerSave(EntityManager em, double now)
        {
            Entity carrier = PlayerResumeCarrier(em);
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (playerResumeBoardStep > 0 && drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return; }
            var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (tray == null || controls == null || !UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return;
            if (playerResumeBoardStep == 0)
            {
                if (!SkirmishLaunchProjection.TryGet(em, out var session, out _) ||
                    !SkirmishExpandedPresentedOrders.TryReadPage(em, session, out _, out _, out _, out _, out _, out _, out _, out _,
                        out var s0, out var s1, out var s2, out var s3, out var s4)) return;
                var slots = new[] { s0, s1, s2, s3, s4 };
                int rifle = Array.FindIndex(slots, slot => slot.Rifle && slot.Alive > 0);
                if (rifle < 0)
                {
                    if (page.NextPage) { PlayerResumeSwipeSquadPage(tray, true); return; }
                    foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                        if (receipt.FactionId == 1 && receipt.Role == SkirmishRoleKind.Rifle &&
                            receipt.Phase is SkirmishReservationPhase.Reserved or SkirmishReservationPhase.Producing) return;
                    var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                    Button recruit = drawer != null && drawer.IsOpen
                        ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Soldiers, "Unit_Chr_Soldier_Male_02_Alt_04") : controls.BuildButton;
                    if (!PlayerResumeTap(recruit)) PlayerResumeScrollTo(recruit);
                    return;
                }
                if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return; }
                if (page.SelectedGroups > 0) { PlayerResumeDeselectSquads(tray); return; }
                if (PlayerResumeTapSquadSlot(tray, rifle)) playerResumeBoardStep = 1;
            }
            else if (playerResumeBoardStep == 1)
            {
                if (page.SelectedGroups < 1) return;
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command) ||
                    command.ActiveCommandMode != Game.Tactical.Contracts.TacticalCommandMode.Board)
                { PlayerResumeTap(controls.CommandWheelPanel?.NextBoardButton); return; }
                if (PlayerResumeWorldTap(em, carrier, now)) playerResumeBoardStep = 2;
            }
            else if (playerResumeBoardStep == 2)
            {
                if (!em.HasBuffer<UnitTransportPassengerElement>(carrier) || em.GetBuffer<UnitTransportPassengerElement>(carrier).Length < 1) return;
                playerResumeBoardStep = 3;
                playerResumeDue = now + 3;
            }
            else
            {
                var passengers = em.GetBuffer<UnitTransportPassengerElement>(carrier);
                if (passengers.Length < 1) throw new InvalidOperationException("Boarded passengers lost before save.");
                foreach (var entry in passengers) VerifyBoardedPassenger(em, entry.Passenger, carrier);
                playerResumePassengerCount = passengers.Length;
                Debug.Log("[S005PassengerSave] phase=Boarded input=Touch passengers=" + playerResumePassengerCount);
                ScreenCapture.CaptureScreenshot(LiveTracePath() + ".boarded.png");
                playerResumeOldEntities.Clear();
                using var query = em.CreateEntityQuery(new EntityQueryDesc { Any = new[] {
                    ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(), ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                    ComponentType.ReadOnly<OperationMapBuildingComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities) playerResumeOldEntities.Add(entity);
                playerResumeBoardStep = 0;
                playerResumeStep = 1;
            }
        }
        private static void VerifyBoardedPassenger(EntityManager em, Entity passenger, Entity carrier)
        {
            if (!em.Exists(passenger) || !em.HasComponent<UnitTransportPassenger>(passenger) ||
                em.GetComponentData<UnitTransportPassenger>(passenger).Transport != carrier ||
                !em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
                throw new InvalidOperationException("Boarded passenger links or hidden visuals missing.");
            foreach (var hidden in em.GetBuffer<UnitTransportHiddenVisualScale>(passenger))
                if (!em.Exists(hidden.Visual) || em.HasComponent<Unity.Transforms.LocalTransform>(hidden.Visual) &&
                    em.GetComponentData<Unity.Transforms.LocalTransform>(hidden.Visual).Scale != 0)
                    throw new InvalidOperationException("Boarded passenger visual was not hidden.");
        }
        private static bool VerifyAndUnloadRestoredPassengers(EntityManager em, double now)
        {
            Entity carrier = PlayerResumeCarrier(em);
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (playerResumeBoardStep == 0)
            {
                if (!em.HasBuffer<UnitTransportPassengerElement>(carrier) ||
                    em.GetBuffer<UnitTransportPassengerElement>(carrier).Length != playerResumePassengerCount)
                    throw new InvalidOperationException("Restored passenger count differs: expected=" + playerResumePassengerCount +
                        " actual=" + (em.HasBuffer<UnitTransportPassengerElement>(carrier) ? em.GetBuffer<UnitTransportPassengerElement>(carrier).Length : -1));
                foreach (var entry in em.GetBuffer<UnitTransportPassengerElement>(carrier))
                { VerifyBoardedPassenger(em, entry.Passenger, carrier); playerResumeRestoredPassengers.Add(entry.Passenger); }
                Debug.Log("[S005PassengerSave] phase=Restored passengers=" + playerResumePassengerCount + " links=1 hidden=1");
                playerResumeBoardStep = 1;
            }
            if (playerResumeBoardStep == 1)
            {
                if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command) ||
                    command.ActiveCommandMode is not (Game.Tactical.Contracts.TacticalCommandMode.None or Game.Tactical.Contracts.TacticalCommandMode.Select))
                { PlayerResumeTap(controls?.SelectButton); return false; }
                if (PlayerResumeWorldTap(em, carrier, now)) playerResumeBoardStep = 2;
                return false;
            }
            if (playerResumeBoardStep == 2 || playerResumeBoardStep == 3)
            {
                // A delivered touch does not establish selection. Wait for the
                // native selection result before opening passenger controls.
                if (!em.HasComponent<SelectedUnitTag>(carrier))
                {
                    if (now >= playerResumeWorldDiagnosticDue)
                    {
                        playerResumeWorldDiagnosticDue = now + 10;
                        Debug.Log("[S005PassengerSave] phase=SelectionPending input=Touch carrier=" + carrier);
                        ScreenCapture.CaptureScreenshot(LiveTracePath() + ".restored-carrier-selection.png");
                    }
                    playerResumeBoardStep = 1;
                    return false;
                }
                string name = playerResumeBoardStep == 2 ? "PassengerChip" : "ExitAllButton";
                foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (button.name == name)
                    {
                        if (PlayerResumeTap(button)) playerResumeBoardStep++;
                        else PlayerResumeScrollTo(button);
                        return false;
                    }
                return false;
            }
            if (playerResumeBoardStep == 5)
            {
                if (!em.HasComponent<SelectedUnitTag>(carrier))
                {
                    if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var selection) ||
                        selection.ActiveCommandMode != Game.Tactical.Contracts.TacticalCommandMode.Select)
                    { PlayerResumeTap(controls?.SelectButton); return false; }
                    PlayerResumeWorldTap(em, carrier, now);
                    return false;
                }
                if (PlayerResumeTap(controls?.MoveButton)) playerResumeBoardStep = 6;
                return false;
            }
            if (playerResumeBoardStep == 6)
            {
                var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                if (hud == null || !UiShellRuntimeGateway.TryReadMatchHudCommandState(out var movement) ||
                    movement.ActiveCommandMode != Game.Tactical.Contracts.TacticalCommandMode.Move) return false;
                Vector3 forward = hud.EnemyBaseObjectivePosition - hud.PlayerBaseObjectivePosition;
                forward.y = 0;
                if (forward.sqrMagnitude < 1) return false;
                forward.Normalize();
                playerResumeUnloadSite = hud.PlayerBaseObjectivePosition + forward * (75 + 12 * playerResumeUnloadRelocations) -
                    Vector3.Cross(Vector3.up, forward) * 30;
                if (PlayerResumeWorldPointTap(playerResumeUnloadSite, now))
                {
                    playerResumeUnloadRelocations++;
                    playerResumeBoardStep = 7;
                    Debug.Log("[S005PassengerSave] phase=RelocateForUnload input=Touch destination=" + playerResumeUnloadSite);
                }
                return false;
            }
            if (playerResumeBoardStep == 7)
            {
                if (Vector3.Distance(em.GetComponentData<Unity.Transforms.LocalTransform>(carrier).Position,
                    playerResumeUnloadSite) > 8) return false;
                playerResumeBoardStep = 1;
                return false;
            }
            if (em.GetBuffer<UnitTransportPassengerElement>(carrier).Length != 0)
            {
                if (playerResumeUnloadRelocations >= 2)
                    throw new InvalidOperationException("Native Exit All did not unload after two frontage relocations.");
                // Dense source geometry or live occupancy can prevent a local
                // unload. Use normal Move/selection/drawer controls to retry
                // on public frontage; never force a passenger out of its buffer.
                playerResumeBoardStep = 5;
                playerResumeDue = now + 2;
                return false;
            }
            foreach (var passenger in playerResumeRestoredPassengers)
            {
                if (!em.Exists(passenger) || em.HasComponent<Disabled>(passenger) || em.HasComponent<UnitTransportPassenger>(passenger) ||
                    em.HasBuffer<UnitTransportHiddenVisualScale>(passenger) && em.GetBuffer<UnitTransportHiddenVisualScale>(passenger).Length != 0)
                    throw new InvalidOperationException("Unloaded passenger still hidden or linked.");
            }
            Debug.Log("[S005PassengerSave] result=Passed restoredAndUnloaded=" + playerResumeRestoredPassengers.Count + " input=Touch");
            ScreenCapture.CaptureScreenshot(LiveTracePath() + ".unloaded.png");
            return true;
        }
        private static void TickCombatBeforePlayerSave()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated || manualTouch == null) return;
            var em = world.EntityManager;
            if (!SkirmishLaunchProjection.TryGet(em, out var session, out var match) ||
                !UiShellRuntimeGateway.TryReadSkirmish(out var model)) return;
            if (manualTouch.PlayerInterrupted || manualTouch.UnexpectedSamples != 0 || manualTouch.PhysicalInterventions != 0)
                throw new InvalidOperationException("Combat controls interrupted before save.");
            if (model.Finished) throw new InvalidOperationException("Combat ended before save.");
            if (manualTouch.IsBusy || manualAttackReceipts < 1 || model.EnemyHealth >= 800) return;
            if ((playerResumePaidRole is SkirmishRoleKind.Tank or SkirmishRoleKind.Siege) &&
                (!paidGroundMoved || !paidGroundFired)) return;
            playerResumeCombatGestures += manualTouch.CompletedGestures;
            playerResumeCombatAttackReceipts = manualAttackReceipts;
            Debug.Log("[S005CombatSave] phase=BattleObserved input=Touch attacks=" + manualAttackReceipts +
                " enemyBaseHp=" + model.EnemyHealth + " elapsed=" +
                SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match));
            StopS004ManualProbe();
            foreach (var runner in UnityEngine.Object.FindObjectsByType<SkirmishS004ManualInputFrame>(FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(runner.gameObject);
            playerResumeTouch = new AriaTouchInputUiSystemHelper();
            if (!playerResumeTouch.Start()) throw new InvalidOperationException("Combat save Touch handover rejected.");
            // Include the delivered paid unit and every current battle-owned entity
            // in the old-world cleanup assertion, rather than the startup roster.
            playerResumeOldEntities.Clear();
            using var query = em.CreateEntityQuery(new EntityQueryDesc { Any = new[] {
                ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(), ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                ComponentType.ReadOnly<OperationMapBuildingComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities) playerResumeOldEntities.Add(entity);
            playerResumeStep = 1;
            playerResumeDue = EditorApplication.timeSinceStartup + .75;
        }
        private static void PlayerResumeScrollTo(Button button)
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            var scroll = button.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null) return;
            var viewport = scroll.viewport;
            var center = ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var local = viewport.InverseTransformPoint(center);
            var bounds = viewport.rect;
            if (bounds.Contains(local)) return;
            bool up = local.y < bounds.yMin;
            var canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 from = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(
                bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .25f : .75f), 0)));
            Vector2 to = RectTransformUtility.WorldToScreenPoint(camera, viewport.TransformPoint(new Vector3(
                bounds.xMin + bounds.width * .25f, bounds.yMin + bounds.height * (up ? .75f : .25f), 0)));
            if (!Screen.safeArea.Contains(from) || !Screen.safeArea.Contains(to)) return;
            if (playerResumeTouch.TryGesture(from, to, .5f, .9f, Time.unscaledTime))
            {
                playerResumeDue = EditorApplication.timeSinceStartup + 1.5;
                Debug.Log("[S005PlayerSaveResume] phase=ScrollCatalog input=Touch target=" + button.name);
            }
        }
        private static bool PlayerResumeTap(Button button)
        {
            if (!TryPresentedButtonPoint(button, out var point))
            {
                if (button != null && EditorApplication.timeSinceStartup > playerResumeDiagnosticDue)
                {
                    playerResumeDiagnosticDue = EditorApplication.timeSinceStartup + 10;
                    var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                    UnityEngine.EventSystems.EventSystem.current?.RaycastAll(new UnityEngine.EventSystems.PointerEventData(
                        UnityEngine.EventSystems.EventSystem.current) { position = point }, hits);
                    Debug.Log("[S005PlayerSaveResume] phase=BlockedTouch step=" + playerResumeStep + " button=" + button.name +
                        " active=" + button.IsActive() + " interactable=" + button.IsInteractable() + " point=" + point +
                        " screen=" + Screen.width + "x" + Screen.height + " safe=" + Screen.safeArea +
                        " firstHit=" + (hits.Count == 0 ? "none" : hits[0].gameObject.name));
                }
                return false;
            }
            bool started = playerResumeTouch.TryGesture(point, point, .15f, 0, Time.unscaledTime);
            if (started)
            {
                Debug.Log("[S005PlayerSaveResume] phase=Touch step=" + playerResumeStep + " button=" + button.name + " point=" + point);
                playerResumeDue = EditorApplication.timeSinceStartup + .75;
            }
            return started;
        }
    }
    public sealed class SkirmishS005PlayerResumeFrame : MonoBehaviour
    { private void Update() => SkirmishS002AriaRunHarness.TickPlayerResumeFrame(); }
}
#endif
