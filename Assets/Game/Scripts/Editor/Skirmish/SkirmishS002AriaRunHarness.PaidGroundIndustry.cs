#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static int paidIndustryOpeningCount, paidIndustryBeforeConstruction, paidIndustryStep, paidIndustrySite, paidIndustryAddedTarget, paidIndustryOpeningOilCount, paidIndustryOpeningRefineryCount;
        private static bool paidIndustryTappedSite, paidIndustryReady, paidIndustryOilReady, paidIndustryWarmReady, paidIndustryFabricationReady, paidIndustryFuelReady;
        private static bool paidFuelStorageRecovery;
        private static int paidIndustryOpeningFuelStorageCount;
        private static bool paidIndustryLocalFuelStorageReady;
        private static Vector3? paidIndustryPlacementFocus;
        private static readonly List<Vector3> paidIndustryCandidateSites = new();
        private static Entity materialsPriorityTruck, materialsPriorityFactory;
        private static int materialsPriorityFactoryId, materialsPriorityStep, materialsPriorityAttempts;
        private static bool materialsPriorityPagingForward;
        private static bool materialsPriorityCloseMapPending;
        private static double materialsPriorityDue, materialsPriorityStartedAt, materialsPriorityAckAt;
        private static Entity localFuelTanker, localFuelStore;
        private static int localFuelSourceId, localFuelStoreId, localFuelStep, localFuelAttempts;
        private static bool localFuelReady, localFuelLoaded, localFuelPagingForward;
        private static float localFuelReceivedBefore;
        private static double localFuelStartedAt, localFuelAckAt;
        private static bool singleActorPagingForward = true;

        // Expanded squad cards add to selection. A single-vehicle action must
        // clear the visible selection first and observe exactly one native tag;
        // checking only whether the intended actor is included is insufficient.
        private static bool SelectOnlyPlayerResumeActorThroughInput(EntityManager em, Entity target)
        {
            if (!UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return false;
            var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
            if (tray == null) return false;
            using var query = em.CreateEntityQuery(typeof(SelectedUnitTag));
            using var selected = query.ToEntityArray(Allocator.Temp);
            if (target != Entity.Null && selected.Length == 1 && selected[0] == target && page.SelectedGroups == 1)
                return true;
            var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            if (popup != null && popup.IsOpen)
            {
                if (PlayerResumeTap(popup.CloseAction)) airProbeDefenseMapStep = airProbeDefenseMapAttempts = 0;
                return false;
            }
            if (selected.Length > 0 || page.SelectedGroups > 0)
            {
                if (PlayerResumeTap(tray.ClearSelectionButton))
                    Debug.Log("[S005SingleActorSelection] phase=Clear input=Touch previousNativeActors=" + selected.Length +
                        " previousGroups=" + page.SelectedGroups + " selectionInjected=0");
                return false;
            }
            if (target == Entity.Null) return true;
            if (!SkirmishLaunchProjection.TryGet(em, out var session, out _)) return false;
            for (int slot = 0; slot < 5; slot++)
                if (SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, slot, out var member) && member == target)
                { PlayerResumeTap(tray.VisibleCardButton(slot)); return false; }
            if (singleActorPagingForward && !page.NextPage) singleActorPagingForward = false;
            if (!singleActorPagingForward && !page.PreviousPage) singleActorPagingForward = true;
            PlayerResumeTap(singleActorPagingForward ? tray.NextPageButton : tray.PreviousPageButton);
            return false;
        }

        private static void TickSingleActorRetirementProbe(double now)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!SkirmishLaunchProjection.TryGet(em, out var session, out _)) return;
            var oil = new List<Entity>();
            int tankers = 0;
            using (var q = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent),
                typeof(UnitResourceHauler), typeof(UnitHealth)))
            using (var units = q.ToEntityArray(Allocator.Temp))
                foreach (var unit in units)
                {
                    var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                    if (owner.FactionId != 1 || owner.CapacityReleased != 0 || owner.SessionId.ToString() != playerResumeSessionId ||
                        em.GetComponentData<UnitHealth>(unit).Current <= 0) continue;
                    var role = em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role;
                    if (role == Game.Skirmish.Contracts.SkirmishRoleKind.LogisticsTruck) oil.Add(unit);
                    if (role == Game.Skirmish.Contracts.SkirmishRoleKind.Tanker) tankers++;
                }
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (singleActorRetirementStep == 0)
            {
                if (oil.Count < 2)
                {
                    foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
                        if (receipt.FactionId == 1 && receipt.Role == Game.Skirmish.Contracts.SkirmishRoleKind.LogisticsTruck &&
                            receipt.Phase is Game.Skirmish.Contracts.SkirmishReservationPhase.Reserved or Game.Skirmish.Contracts.SkirmishReservationPhase.Producing)
                            return;
                    TouchFixedWingCatalog(drawer, BuildDrawerCategory.Vehicles, "Unit_Veh_Truck_Tray"); return;
                }
                if (oil.Count != 2 || tankers != 1) throw new InvalidOperationException("Single-actor retirement fixture fleet is unexpected.");
                foreach (var unit in oil)
                {
                    if (em.HasComponent<UnitSpawnTransitTag>(unit)) return;
                    if (em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).ReservationId > 0) singleActorRetirementTarget = unit;
                    else singleActorRetirementSurvivor = unit;
                }
                if (singleActorRetirementTarget == Entity.Null || singleActorRetirementSurvivor == Entity.Null)
                    throw new InvalidOperationException("Normal paid/opening Oil trucks are missing.");
                singleActorRetirementStep = 1;
            }
            if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return; }
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (singleActorRetirementStep == 1)
            {
                // Deliberately recreate the additive-card state that native867
                // carried into retirement, using the same public card controls.
                foreach (var unit in oil)
                {
                    if (em.HasComponent<SelectedUnitTag>(unit)) continue;
                    if (!UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return;
                    var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                    for (int slot = 0; slot < 5; slot++)
                        if (SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, slot, out var member) && member == unit)
                        { PlayerResumeTap(tray?.VisibleCardButton(slot)); return; }
                    PlayerResumeTap(page.NextPage ? tray?.NextPageButton : tray?.PreviousPageButton);
                    return;
                }
                using var selected = em.CreateEntityQuery(typeof(SelectedUnitTag));
                if (selected.CalculateEntityCount() != 2) throw new InvalidOperationException("Mixed native Oil selection was not reproduced.");
                Debug.Log("[S005SingleActorRetirement] phase=MixedSelection nativeActors=2 input=Touch");
                singleActorRetirementStep = 2;
            }
            if (singleActorRetirementStep == 2)
            {
                if (!SelectOnlyPlayerResumeActorThroughInput(em, singleActorRetirementTarget)) return;
                var cargo = em.GetComponentData<UnitResourceHauler>(singleActorRetirementTarget);
                if (cargo.CargoOilBarrels > 0 || cargo.CargoFuelBarrels > 0) return;
                var wheel = controls?.CommandWheelPanel;
                if (wheel == null) return;
                if (!wheel.IsOpen) { PlayerResumeTap(wheel.OpenButton); return; }
                foreach (var button in wheel.GetComponentsInChildren<UnityEngine.UI.Button>())
                    if (button.name == "DestroySector" && PlayerResumeTap(button))
                    {
                        singleActorRetirementStep = 3;
                        Debug.Log("[S005SingleActorRetirement] phase=Retire selectedNativeActors=1 cargoEmptyObserved=1 input=Touch");
                        return;
                    }
                return;
            }
            bool released = !em.Exists(singleActorRetirementTarget) ||
                em.GetComponentData<SkirmishAttemptOwnedComponent>(singleActorRetirementTarget).CapacityReleased != 0;
            if (!released) return;
            Debug.Log("[S005SingleActorRetirement] phase=CleanupObserved oil=" + oil.Count + " tankers=" + tankers +
                " survivorExists=" + em.Exists(singleActorRetirementSurvivor) + " survivorHealth=" +
                (em.Exists(singleActorRetirementSurvivor) ? em.GetComponentData<UnitHealth>(singleActorRetirementSurvivor).Current : -1) +
                " survivorReleased=" + (em.Exists(singleActorRetirementSurvivor) ? em.GetComponentData<SkirmishAttemptOwnedComponent>(singleActorRetirementSurvivor).CapacityReleased : -1) +
                " receiptViolations=" + AriaCommandEvidence.Violations + " unexpected=" + playerResumeTouch.UnexpectedSamples +
                " interventions=" + playerResumeTouch.PhysicalInterventions + " target=" + singleActorRetirementTarget +
                " survivor=" + singleActorRetirementSurvivor);
            if (oil.Count != 1 || tankers != 1 || !em.Exists(singleActorRetirementSurvivor) ||
                em.GetComponentData<UnitHealth>(singleActorRetirementSurvivor).Current <= 0 ||
                AriaCommandEvidence.Violations != 0 || playerResumeTouch.UnexpectedSamples != 0 || playerResumeTouch.PhysicalInterventions != 0)
                throw new InvalidOperationException("Single-actor retirement changed the surviving fleet or failed its input audit.");
            Debug.Log("[S005SingleActorRetirement] result=Passed input=Touch additiveSelectionReproduced=1 exclusiveSelectionObserved=1 oilBefore=2 oilAfter=1 tankersAfter=1 nativeCleanupObserved=1 actorResourcesClockInjected=0 completeMatchCertified=0");
            ScreenCapture.CaptureScreenshot(LiveTracePath() + ".single-actor-retirement.png");
            playerResumeProbeActive = false;
            playerResumeTouch.Dispose(); playerResumeTouch = null;
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            Finish(false, "singleActorRetirementOnly");
        }

        // Short normal-input diagnostic: use the opening industry and native
        // Tanker rather than buying the full paid qualification setup.
        private static void TickStartingFuelClickProbe(double now)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!SkirmishLaunchProjection.TryGet(em, out var session, out _)) return;
            if (localFuelStep == 0)
            {
                using var q = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(UnitHealth));
                using var actors = q.ToEntityArray(Allocator.Temp);
                foreach (var actor in actors)
                {
                    var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if (owner.FactionId != 1 || owner.CapacityReleased != 0 || owner.SessionId.ToString() != playerResumeSessionId ||
                        em.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                    if (em.HasComponent<BuildingResourceStorageComponent>(actor))
                    {
                        var stock = em.GetComponentData<BuildingResourceStorageComponent>(actor);
                        if (stock.FuelStorageCapacity > 0 && stock.FuelBarrelsPerDay <= 0 && stock.OilBarrelsPerDay <= 0)
                        { localFuelStore = actor; localFuelStoreId = stock.RuntimeBuildingId; }
                    }
                    if (em.HasComponent<SkirmishUnitRoleComponent>(actor) &&
                        em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == Game.Skirmish.Contracts.SkirmishRoleKind.Tanker)
                        localFuelTanker = actor;
                }
                if (localFuelStore == Entity.Null || localFuelTanker == Entity.Null)
                    throw new InvalidOperationException("Starting fuel click diagnostic has no native store/Tanker.");
                localFuelStep = 1;
            }
            if (localFuelStep == 2)
            {
                if (now < localFuelAckAt) return;
                Debug.Log("[S005StartingFuelClick] result=ObservedOnly target=" + localFuelStoreId +
                    " selected=" + em.HasComponent<SelectedUnitTag>(localFuelTanker) + " order=" +
                    (em.HasComponent<UnitResourceHaulOrder>(localFuelTanker) ?
                    em.GetComponentData<UnitResourceHaulOrder>(localFuelTanker).DestinationBuildingId.ToString() : "none") +
                    " completeMatchCertified=0");
                Environment.SetEnvironmentVariable("WARLINE_SELECTION_CLICK_DIAGNOSTICS", null);
                Environment.SetEnvironmentVariable("WARLINE_S005_PAID_FUEL_CLICK_ONLY", null);
                Environment.SetEnvironmentVariable("WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY", null);
                playerResumeProbeActive = false;
                playerResumeTouch.Dispose(); playerResumeTouch = null;
                Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
                Finish(false, "startingFuelClickOnly");
                return;
            }
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) return;
            if (command.ActiveCommandMode is not (Game.Tactical.Contracts.TacticalCommandMode.None or Game.Tactical.Contracts.TacticalCommandMode.Select))
            { PlayerResumeTap(controls?.SelectButton); return; }
            if (!em.HasComponent<SelectedUnitTag>(localFuelTanker))
            {
                var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                if (tray == null || !UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return;
                for (int slot = 0; slot < 5; slot++)
                    if (SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, slot, out var member) && member == localFuelTanker)
                    { PlayerResumeTap(tray.VisibleCardButton(slot)); return; }
                if (localFuelPagingForward && !page.NextPage) localFuelPagingForward = false;
                if (!localFuelPagingForward && !page.PreviousPage) localFuelPagingForward = true;
                PlayerResumeTap(localFuelPagingForward ? tray.NextPageButton : tray.PreviousPageButton);
                return;
            }
            if (PlayerResumeWorldTap(em, localFuelStore, now)) { localFuelStep = 2; localFuelAckAt = now + 2; }
        }

        private static void ResetFixedWingLocalFuelSupplyProbe()
        {
            localFuelTanker = localFuelStore = Entity.Null;
            localFuelSourceId = localFuelStoreId = localFuelStep = localFuelAttempts = 0;
            localFuelReady = localFuelLoaded = false;
            localFuelPagingForward = true;
            localFuelReceivedBefore = 0;
            localFuelStartedAt = localFuelAckAt = 0;
        }

        // Native781's fleet converged on the opening refinery while the paid
        // refinery accumulated Fuel. A player can select a Tanker and tap the
        // paid receiving store to start its nearby route. Observe the resulting
        // order, loaded cargo and physical delivery; never inject any of them.
        private static bool TickFixedWingLocalFuelSupplyThroughInput(EntityManager em, double now)
        {
            if (!fixedWingCycleOnly || !paidIndustryReady || localFuelReady) return true;
            if (!SkirmishLaunchProjection.TryGet(em, out var session, out _) ||
                CountCommittedFixedWingTankers(em, session) < FixedWingQualificationTankers) return true;
            if (localFuelStep == 0)
            {
                Entity refinery = Entity.Null;
                localFuelSourceId = localFuelStoreId = 0;
                localFuelStore = Entity.Null;
                using (var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent),
                    typeof(SkirmishAttemptOwnedComponent), typeof(UnitHealth)))
                using (var buildings = query.ToEntityArray(Allocator.Temp))
                    foreach (var building in buildings)
                    {
                        var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(building);
                        var storage = em.GetComponentData<BuildingResourceStorageComponent>(building);
                        if (owner.FactionId != 1 || owner.CapacityReleased != 0 ||
                            owner.SessionId.ToString() != playerResumeSessionId ||
                            em.GetComponentData<UnitHealth>(building).Current <= 0) continue;
                        if (storage.FuelBarrelsPerDay > 0 && storage.RuntimeBuildingId > localFuelSourceId)
                        { refinery = building; localFuelSourceId = storage.RuntimeBuildingId; }
                        if (storage.FuelStorageCapacity > 0 && storage.FuelBarrelsPerDay <= 0 &&
                            storage.OilBarrelsPerDay <= 0 && storage.RuntimeBuildingId > localFuelStoreId)
                        { localFuelStore = building; localFuelStoreId = storage.RuntimeBuildingId; }
                    }
                if (refinery == Entity.Null || localFuelStore == Entity.Null)
                    throw new InvalidOperationException("Paid local refinery/store route is unavailable.");
                var stock = em.GetComponentData<BuildingResourceStorageComponent>(refinery);
                if (stock.StoredFuelBarrels - stock.ReservedFuelOutboundBarrels < 8) return false;
                using var trucks = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                    typeof(SkirmishUnitRoleComponent), typeof(UnitResourceHauler), typeof(UnitHealth));
                using var units = trucks.ToEntityArray(Allocator.Temp);
                foreach (var truck in units)
                {
                    var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(truck);
                    if (owner.FactionId != 1 || owner.CapacityReleased != 0 ||
                        owner.SessionId.ToString() != playerResumeSessionId ||
                        em.GetComponentData<UnitHealth>(truck).Current <= 0 ||
                        em.GetComponentData<SkirmishUnitRoleComponent>(truck).Role != Game.Skirmish.Contracts.SkirmishRoleKind.Tanker) continue;
                    var cargo = em.GetComponentData<UnitResourceHauler>(truck);
                    if (cargo.CargoOilBarrels > 0 || cargo.CargoFuelBarrels > 0) continue;
                    localFuelTanker = truck;
                    break;
                }
                if (localFuelTanker == Entity.Null) return false;
                localFuelReceivedBefore = em.GetComponentData<BuildingResourceStorageComponent>(localFuelStore).ReceivedHaulerFuelBarrels;
                localFuelStartedAt = now;
                localFuelStep = 1;
                Debug.Log("[S005LocalFuelRoute] phase=Begin source=" + localFuelSourceId + " destination=" + localFuelStoreId +
                    " tanker=" + em.GetComponentData<SkirmishAttemptOwnedComponent>(localFuelTanker).StableObjectId + " input=TouchPending");
            }
            if (!em.Exists(localFuelTanker) || !em.Exists(localFuelStore) || now - localFuelStartedAt > 90)
                throw new InvalidOperationException("Normal local Fuel route did not complete its physical delivery.");
            var cargoNow = em.GetComponentData<UnitResourceHauler>(localFuelTanker);
            if (localFuelStep == 3)
            {
                localFuelLoaded |= cargoNow.CargoFuelBarrels >= 8;
                float received = em.GetComponentData<BuildingResourceStorageComponent>(localFuelStore).ReceivedHaulerFuelBarrels;
                if (!localFuelLoaded || cargoNow.CargoFuelBarrels > 0 || received < localFuelReceivedBefore + 8) return false;
                localFuelReady = true;
                Debug.Log("[S005LocalFuelRoute] result=Delivered source=" + localFuelSourceId + " destination=" + localFuelStoreId +
                    " input=Touch nativeOrderObserved=1 loadedCargoObserved=1 receivedAdded=" + (received - localFuelReceivedBefore) +
                    " resourcesOrOrdersInjected=0");
                return true;
            }
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return false; }
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) return false;
            if (command.ActiveCommandMode is not (Game.Tactical.Contracts.TacticalCommandMode.None or Game.Tactical.Contracts.TacticalCommandMode.Select))
            { PlayerResumeTap(controls?.SelectButton); return false; }
            if (localFuelStep == 1)
            {
                if (!SelectOnlyPlayerResumeActorThroughInput(em, localFuelTanker)) return false;
                if (!em.HasComponent<SelectedUnitTag>(localFuelTanker))
                {
                    var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                    if (tray == null || !UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return false;
                    for (int slot = 0; slot < 5; slot++)
                        if (SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, session, slot, out var member) && member == localFuelTanker)
                        { PlayerResumeTap(tray.VisibleCardButton(slot)); return false; }
                    if (localFuelPagingForward && !page.NextPage) localFuelPagingForward = false;
                    if (!localFuelPagingForward && !page.PreviousPage) localFuelPagingForward = true;
                    PlayerResumeTap(localFuelPagingForward ? tray.NextPageButton : tray.PreviousPageButton);
                    return false;
                }
                if (!PlayerResumeWorldTap(em, localFuelStore, now)) return false;
                localFuelAttempts++;
                localFuelAckAt = now + 2;
                localFuelStep = 2;
                return false;
            }
            if (em.HasComponent<UnitResourceHaulOrder>(localFuelTanker))
            {
                var order = em.GetComponentData<UnitResourceHaulOrder>(localFuelTanker);
                if (order.SourceBuildingId == localFuelSourceId && order.DestinationBuildingId == localFuelStoreId &&
                    order.ResourceKind == (byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel)
                {
                    localFuelLoaded |= cargoNow.CargoFuelBarrels >= 8;
                    localFuelStep = 3;
                    Debug.Log("[S005LocalFuelRoute] phase=Assigned source=" + localFuelSourceId + " destination=" + localFuelStoreId +
                        " input=Touch nativeOrderObserved=1 resourcesOrOrdersInjected=0");
                    return false;
                }
            }
            if (now < localFuelAckAt) return false;
            if (localFuelAttempts >= 3)
                throw new InvalidOperationException("Normal local Fuel store tap was not acknowledged after three attempts.");
            localFuelStep = 1;
            return false;
        }

        [Serializable]
        private sealed class QualificationEconomyDiagnostic
        {
            public float elapsed;
            public List<string> values;
        }

        // Observation only: never advances the clock, supplies resources or issues commands.
        private static void AppendQualificationEconomyDiagnostic(EntityManager em, float elapsed)
        {
            var values = new List<string>();
            AddQualificationEconomyRows<FactionTacticalMaterialsComponent>(em, values, "Materials", v => v.FactionId == 1);
            AddQualificationEconomyRows<MaterialFabricationComponent>(em, values, "Fabrication", v => v.OwnerFactionId == 1);
            AddQualificationEconomyRows<BuildingResourceStorageComponent>(em, values, "Storage", v => v.OwnerFactionId == 1);
            AddQualificationEconomyRows<FactionFuelLogisticsTelemetryComponent>(em, values, "OilLogistics", v => v.FactionId == 1);
            AddQualificationEconomyRows<FactionMaterialFabricationTelemetryComponent>(em, values, "FabricationTelemetry", v => v.FactionId == 1);
            // Record the actual shared production owner, not just the logical
            // structure identity. Catalog rejection must be diagnosed against
            // the same prefab references and native lifecycle used to queue.
            using (var owners = em.CreateEntityQuery(typeof(SkirmishNativeBuildingCheckpointRecord)))
            using (var nativeEntities = owners.ToEntityArray(Allocator.Temp))
                foreach (var nativeEntity in nativeEntities)
                {
                    var building = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(nativeEntity).Building;
                    if (building == null || building.OwnerFactionId != 1) continue;
                    var slots = building.Definition?.ProductionSlots;
                    var keys = new List<string>();
                    if (slots != null)
                        foreach (var slot in slots)
                            keys.Add(slot.SpawnUnitPrefab == null ? "null" :
                                slot.SpawnUnitPrefab.name + ":" + slot.SpawnUnitPrefab.GetEntityId());
                    values.Add("NativeProducer=id:" + building.Id + ";name:" + building.Definition?.DisplayName +
                        ";destroyed:" + building.IsDestroyed + ";city:" + building.IsCityGenerated +
                        ";owned:" + building.HasOwnerFaction + ";entity:" + building.CombatEntity +
                        ";slots:" + string.Join(",", keys));
                }
            var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
            if (catalog != null && catalog.TryReadSelectedRequest(out var key, out var cost, out var failure, out var required))
                values.Add("CatalogRequest=key:" + key + ";cost:" + cost + ";failure:" + failure + ";required:" + required);
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeFactionUsableFuelSummary>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
                foreach (var fuel in em.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(entity, true))
                    values.Add("UsableFuel=" + JsonUtility.ToJson(fuel));
            var sample = new QualificationEconomyDiagnostic { elapsed = elapsed, values = values };
            System.IO.File.AppendAllText(LiveTracePath() + ".economy.jsonl", JsonUtility.ToJson(sample) + "\n");
        }

        private static void AddQualificationEconomyRows<T>(EntityManager em, List<string> rows,
            string kind, Func<T, bool> include) where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            using var values = query.ToComponentDataArray<T>(Allocator.Temp);
            foreach (var value in values)
                if (include(value)) rows.Add(kind + "=" + JsonUtility.ToJson(value));
        }

        private static int CountLivingPlayerFabrication(EntityManager em)
        {
            int count = 0;
            using var query = em.CreateEntityQuery(typeof(MaterialFabricationComponent), typeof(UnitHealth));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
                if (em.GetComponentData<MaterialFabricationComponent>(entity).OwnerFactionId == 1 &&
                    em.GetComponentData<UnitHealth>(entity).Current > 0) count++;
            return count;
        }

        private static int CountLivingPlayerOilSources(EntityManager em)
        {
            int count = 0;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent), typeof(UnitHealth));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId == 1 && storage.OilBarrelsPerDay > 0 &&
                    em.GetComponentData<UnitHealth>(entity).Current > 0) count++;
            }
            return count;
        }

        private static int CountLivingPlayerRefineries(EntityManager em)
        {
            int count = 0;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent), typeof(UnitHealth));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId == 1 && storage.FuelBarrelsPerDay > 0 &&
                    em.GetComponentData<UnitHealth>(entity).Current > 0) count++;
            }
            return count;
        }

        private static void ResetPaidGroundIndustryProbe(EntityManager em)
        {
            ResetFixedWingLocalFuelSupplyProbe();
            materialsPriorityTruck = materialsPriorityFactory = Entity.Null;
            materialsPriorityFactoryId = materialsPriorityStep = materialsPriorityAttempts = 0;
            materialsPriorityDue = materialsPriorityStartedAt = materialsPriorityAckAt = 0;
            materialsPriorityCloseMapPending = false;
            paidFuelStorageRecovery = false;
            paidIndustryPlacementFocus = null;
            paidIndustryOpeningFuelStorageCount = CountLivingPlayerFuelStores(em);
            // Native707's second refinery hauled to the distant opening store.
            // Buy a nearby receiving store for the aircraft strategy; preserve
            // opening-store behavior for the other recovery fixtures.
            paidIndustryLocalFuelStorageReady = !fixedWingCycleOnly;
            paidIndustryOpeningCount = CountLivingPlayerFabrication(em);
            paidIndustryOpeningOilCount = CountLivingPlayerOilSources(em);
            paidIndustryOpeningRefineryCount = CountLivingPlayerRefineries(em);
            paidIndustryWarmReady = !fixedWingCycleOnly;
            paidIndustryFabricationReady = false;
            paidIndustryFuelReady = !fixedWingCycleOnly;
            paidIndustryOilReady = !fixedWingCycleOnly;
            // Native851 funded the paid Strike too late for a second sortie.
            // Invest in both extra Factories before the fleet. The placement
            // strategy reserves the observed compact supply sites; all actual
            // construction still requires native input and normal clearance.
            paidIndustryAddedTarget = 2;
            paidIndustryBeforeConstruction = paidIndustryOpeningCount;
            paidIndustryStep = paidIndustrySite = 0;
            paidIndustryTappedSite = paidIndustryReady = false;
            paidIndustryCandidateSites.Clear();
        }

        // Player strategy only: select an own truck and tap an own factory through
        // the normal selection surface. Never writes cargo, reservations or orders.
        private static bool TickFixedWingFactorySupplyThroughInput(EntityManager em, double now)
        {
            if (!fixedWingCycleOnly || !paidIndustryReady || fixedWingStep >= 2) return true;
            // A deferred redirect can interrupt camera navigation while its map
            // is open (native862). Finish that owned UI flow before another
            // squad selection or purchase; use the normal visible close action.
            if (materialsPriorityCloseMapPending)
                return CloseDeferredFactoryCameraThroughInput();
            var placement = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
            if (placement != null && placement.HasPendingPlacement) return true;
            if (materialsPriorityTruck == Entity.Null)
            {
                // Finish the normal setup fleet purchases before redirecting
                // supply, so a catalog wait cannot interrupt native selection.
                if (!SkirmishLaunchProjection.TryGet(em, out var supplySession, out _) ||
                    CountCommittedFixedWingTankers(em, supplySession) < FixedWingQualificationTankers) return true;
                if (now < materialsPriorityDue) return true;
                using var factories = em.CreateEntityQuery(typeof(MaterialFabricationComponent),
                    typeof(BuildingResourceStorageComponent), typeof(SkirmishAttemptOwnedComponent), typeof(UnitHealth));
                using var buildings = factories.ToEntityArray(Allocator.Temp);
                foreach (var building in buildings)
                {
                    var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(building);
                    var recipe = em.GetComponentData<MaterialFabricationComponent>(building);
                    var stock = em.GetComponentData<BuildingResourceStorageComponent>(building);
                    if (owner.FactionId != 1 || owner.SessionId.ToString() != playerResumeSessionId ||
                        owner.CapacityReleased != 0 || recipe.ProductionEnabled == 0 ||
                        em.GetComponentData<UnitHealth>(building).Current <= 0 ||
                        stock.StoredOilBarrels - stock.ReservedOilOutboundBarrels + stock.ReservedOilInboundBarrels >= recipe.OilConsumedPerCycle)
                        continue;
                    materialsPriorityFactory = building;
                    materialsPriorityFactoryId = recipe.RuntimeBuildingId;
                    break;
                }
                if (materialsPriorityFactory == Entity.Null) return true;
                using var trucks = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                    typeof(SkirmishUnitRoleComponent), typeof(UnitResourceHauler), typeof(UnitHealth));
                using var units = trucks.ToEntityArray(Allocator.Temp);
                foreach (var truck in units)
                {
                    var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(truck);
                    var cargo = em.GetComponentData<UnitResourceHauler>(truck);
                    if (owner.FactionId != 1 || owner.SessionId.ToString() != playerResumeSessionId ||
                        owner.CapacityReleased != 0 || em.GetComponentData<UnitHealth>(truck).Current <= 0 ||
                        em.GetComponentData<SkirmishUnitRoleComponent>(truck).Role != Game.Skirmish.Contracts.SkirmishRoleKind.LogisticsTruck ||
                        cargo.CargoOilBarrels > 0 || cargo.CargoFuelBarrels > 0) continue;
                    if (em.HasComponent<UnitResourceHaulOrder>(truck))
                    {
                        var order = em.GetComponentData<UnitResourceHaulOrder>(truck);
                        bool mustKeepFactorySupply = false;
                        foreach (var building in buildings)
                        {
                            var currentRecipe = em.GetComponentData<MaterialFabricationComponent>(building);
                            if (order.DestinationBuildingId != currentRecipe.RuntimeBuildingId) continue;
                            var currentStock = em.GetComponentData<BuildingResourceStorageComponent>(building);
                            // Redirect only an empty pickup-phase truck whose old
                            // factory keeps a funded batch after its own inbound
                            // reservation is canceled by the normal new order.
                            mustKeepFactorySupply |= order.DestinationBuildingId == materialsPriorityFactoryId ||
                                currentStock.StoredOilBarrels - currentStock.ReservedOilOutboundBarrels +
                                currentStock.ReservedOilInboundBarrels - cargo.BarrelCapacity < currentRecipe.OilConsumedPerCycle;
                        }
                        if (mustKeepFactorySupply || order.Phase != 1) continue;
                    }
                    materialsPriorityTruck = truck;
                    break;
                }
                if (materialsPriorityTruck == Entity.Null)
                { materialsPriorityFactory = Entity.Null; materialsPriorityDue = now + 2; return true; }
                materialsPriorityStep = materialsPriorityAttempts = 0;
                materialsPriorityPagingForward = true;
                materialsPriorityStartedAt = now;
                Debug.Log("[S005MaterialsPriority] phase=Begin factory=" + materialsPriorityFactoryId +
                    " truck=" + em.GetComponentData<SkirmishAttemptOwnedComponent>(materialsPriorityTruck).StableObjectId +
                    " qualificationOilTarget=3 qualificationTankerTarget=3 input=TouchPending");
            }
            if (!em.Exists(materialsPriorityTruck) || !em.Exists(materialsPriorityFactory) || now - materialsPriorityStartedAt > 30)
            {
                Debug.Log("[S005MaterialsPriority] phase=Watchdog step=" + materialsPriorityStep +
                    " truckSelected=" + (em.Exists(materialsPriorityTruck) && em.HasComponent<SelectedUnitTag>(materialsPriorityTruck)) +
                    " expectedFactory=" + materialsPriorityFactoryId + " observedDestination=" +
                    (em.Exists(materialsPriorityTruck) && em.HasComponent<UnitResourceHaulOrder>(materialsPriorityTruck)
                        ? em.GetComponentData<UnitResourceHaulOrder>(materialsPriorityTruck).DestinationBuildingId : -1));
                throw new InvalidOperationException("Normal-input factory supply selection/delivery acknowledgment did not complete.");
            }
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return false; }
            var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if (!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)) return false;
            if (command.ActiveCommandMode is not (Game.Tactical.Contracts.TacticalCommandMode.None or Game.Tactical.Contracts.TacticalCommandMode.Select))
            { PlayerResumeTap(controls?.SelectButton); return false; }
            if (materialsPriorityStep == 0)
            {
                if (!SelectOnlyPlayerResumeActorThroughInput(em, materialsPriorityTruck)) return false;
                if (!em.HasComponent<SelectedUnitTag>(materialsPriorityTruck))
                {
                    var tray = UnityEngine.Object.FindAnyObjectByType<MatchHudSquadTrayView>();
                    if (tray == null || !SkirmishLaunchProjection.TryGet(em, out var selectionSession, out _) ||
                        !UiShellRuntimeGateway.TryReadExpandedSquadPage(out var page)) return false;
                    for (int slot = 0; slot < 5; slot++)
                        if (SkirmishExpandedPresentedOrders.TryGetPresentedMember(em, selectionSession, slot, out var member) &&
                            member == materialsPriorityTruck)
                        { PlayerResumeTap(tray.VisibleCardButton(slot)); return false; }
                    if (materialsPriorityPagingForward && !page.NextPage) materialsPriorityPagingForward = false;
                    if (!materialsPriorityPagingForward && !page.PreviousPage) materialsPriorityPagingForward = true;
                    PlayerResumeTap(materialsPriorityPagingForward ? tray.NextPageButton : tray.PreviousPageButton);
                    return false;
                }
                materialsPriorityStep = 1;
            }
            if (materialsPriorityStep == 1)
            {
                if (!SelectOnlyPlayerResumeActorThroughInput(em, materialsPriorityTruck)) return false;
                // Selection and camera gestures take real simulation time. An
                // empty pickup-phase truck can begin filling before the world
                // tap (native861); do not repeatedly redirect its live cargo.
                if (DeferUnsafeFactoryRedirect(em, now)) return false;
                if (PlayerResumeWorldTap(em, materialsPriorityFactory, now))
                {
                    materialsPriorityAttempts++;
                    materialsPriorityAckAt = now + 2;
                    materialsPriorityStep = 2;
                }
                return false;
            }
            if (!em.HasComponent<UnitResourceHaulOrder>(materialsPriorityTruck) ||
                em.GetComponentData<UnitResourceHaulOrder>(materialsPriorityTruck).DestinationBuildingId != materialsPriorityFactoryId)
            {
                if (now < materialsPriorityAckAt) return false;
                var observedCargo = em.GetComponentData<UnitResourceHauler>(materialsPriorityTruck);
                var observedOrder = em.HasComponent<UnitResourceHaulOrder>(materialsPriorityTruck)
                    ? em.GetComponentData<UnitResourceHaulOrder>(materialsPriorityTruck) : default;
                Debug.Log("[S005MaterialsPriority] phase=Unacknowledged factory=" + materialsPriorityFactoryId +
                    " truckSelected=" + em.HasComponent<SelectedUnitTag>(materialsPriorityTruck) +
                    " cargoOil=" + observedCargo.CargoOilBarrels + " observedDestination=" + observedOrder.DestinationBuildingId +
                    " haulPhase=" + observedOrder.Phase + " status=" +
                    (em.HasComponent<UnitResourceHaulStatus>(materialsPriorityTruck)
                        ? JsonUtility.ToJson(em.GetComponentData<UnitResourceHaulStatus>(materialsPriorityTruck)) : "none"));
                if (DeferUnsafeFactoryRedirect(em, now)) return false;
                ScreenCapture.CaptureScreenshot(LiveTracePath() + ".factory-" + materialsPriorityFactoryId +
                    "-attempt-" + materialsPriorityAttempts + ".png");
                // The native command can reject a temporarily unavailable bay.
                // Retry through the same visible selection and world tap; never
                // turn a gesture receipt into a fabricated order acknowledgment.
                if (materialsPriorityAttempts >= 3)
                    throw new InvalidOperationException("Normal-input factory supply order was not acknowledged after three visible attempts.");
                Debug.Log("[S005MaterialsPriority] phase=Retry attempt=" + materialsPriorityAttempts +
                    " factory=" + materialsPriorityFactoryId + " nativeOrderObserved=0 input=TouchPending");
                materialsPriorityStep = 0;
                return false;
            }
            Debug.Log("[S005MaterialsPriority] result=Assigned input=Touch factory=" + materialsPriorityFactoryId +
                " truck=" + em.GetComponentData<SkirmishAttemptOwnedComponent>(materialsPriorityTruck).StableObjectId +
                " nativeOrderObserved=1 cargoOrOrderInjected=0 defaultPolicyChanged=0");
            materialsPriorityTruck = materialsPriorityFactory = Entity.Null;
            materialsPriorityDue = now + 2;
            return true;
        }

        private static bool DeferUnsafeFactoryRedirect(EntityManager em, double now)
        {
            var cargo = em.GetComponentData<UnitResourceHauler>(materialsPriorityTruck);
            var order = em.HasComponent<UnitResourceHaulOrder>(materialsPriorityTruck)
                ? em.GetComponentData<UnitResourceHaulOrder>(materialsPriorityTruck) : default;
            // After a world tap, an observed matching destination remains an
            // acknowledgment even if loading has progressed. Before the tap,
            // an automatic assignment makes this optional redirect unnecessary.
            bool alreadyAssigned = order.DestinationBuildingId == materialsPriorityFactoryId;
            if (alreadyAssigned && materialsPriorityStep != 1) return false;
            var stock = em.GetComponentData<BuildingResourceStorageComponent>(materialsPriorityFactory);
            var recipe = em.GetComponentData<MaterialFabricationComponent>(materialsPriorityFactory);
            bool funded = stock.StoredOilBarrels - stock.ReservedOilOutboundBarrels +
                stock.ReservedOilInboundBarrels >= recipe.OilConsumedPerCycle;
            bool busy = cargo.CargoOilBarrels > 0 || cargo.CargoFuelBarrels > 0 ||
                em.HasComponent<UnitResourceHaulOrder>(materialsPriorityTruck) && order.Phase != 1;
            bool oldFactoryNeedsDelivery = false;
            using (var factories = em.CreateEntityQuery(typeof(MaterialFabricationComponent), typeof(BuildingResourceStorageComponent)))
            using (var buildings = factories.ToEntityArray(Allocator.Temp))
                foreach (var building in buildings)
                {
                    var previousRecipe = em.GetComponentData<MaterialFabricationComponent>(building);
                    if (previousRecipe.RuntimeBuildingId != order.DestinationBuildingId || previousRecipe.ProductionEnabled == 0) continue;
                    var previousStock = em.GetComponentData<BuildingResourceStorageComponent>(building);
                    oldFactoryNeedsDelivery = previousStock.StoredOilBarrels - previousStock.ReservedOilOutboundBarrels +
                        previousStock.ReservedOilInboundBarrels - cargo.BarrelCapacity < previousRecipe.OilConsumedPerCycle;
                }
            if (!alreadyAssigned && !funded && !busy && !oldFactoryNeedsDelivery) return false;
            string reason = alreadyAssigned ? "FactoryAlreadyAssigned" : funded ? "FactoryBatchFunded" :
                busy ? "TruckStartedDelivery" : "PreviousFactoryNeedsDelivery";
            Debug.Log("[S005MaterialsPriority] phase=Deferred reason=" + reason +
                " factory=" + materialsPriorityFactoryId + " cargoOil=" + cargo.CargoOilBarrels +
                " haulPhase=" + order.Phase + " orderAcknowledged=0 cargoOrOrderInjected=0 input=Touch");
            materialsPriorityTruck = materialsPriorityFactory = Entity.Null;
            materialsPriorityStep = materialsPriorityAttempts = 0;
            materialsPriorityDue = now + 2;
            materialsPriorityCloseMapPending = true;
            return true;
        }

        private static bool CloseDeferredFactoryCameraThroughInput()
        {
            var map = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            if (map != null && map.IsOpen)
            { PlayerResumeTap(map.CloseAction); return false; }
            airProbeDefenseMapStep = airProbeDefenseMapAttempts = 0;
            materialsPriorityCloseMapPending = false;
            Debug.Log("[S005MaterialsPriority] phase=DeferredCameraClosed input=Touch popupOpen=0");
            return true;
        }

        private static int CountLivingPlayerFuelStores(EntityManager em)
        {
            int count = 0;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent), typeof(UnitHealth));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId == 1 && storage.FuelStorageCapacity > 0 &&
                    storage.FuelBarrelsPerDay <= 0 && em.GetComponentData<UnitHealth>(entity).Current > 0) count++;
            }
            return count;
        }

        private static Vector3 FindNewestPlayerRefineryPosition(EntityManager em) =>
            FindNewestPlayerProductionPosition(em, false);

        private static Vector3 FindNewestPlayerOilSourcePosition(EntityManager em) =>
            FindNewestPlayerProductionPosition(em, true);

        private static Vector3 FindNewestPlayerProductionPosition(EntityManager em, bool oilSource)
        {
            int newest = -1;
            Vector3 position = default;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent), typeof(UnitHealth), typeof(LocalTransform));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId != 1 ||
                    (oilSource ? storage.OilBarrelsPerDay : storage.FuelBarrelsPerDay) <= 0 ||
                    em.GetComponentData<UnitHealth>(entity).Current <= 0 || storage.RuntimeBuildingId <= newest) continue;
                newest = storage.RuntimeBuildingId;
                position = em.GetComponentData<LocalTransform>(entity).Position;
            }
            if (newest < 0) throw new InvalidOperationException("No native " +
                (oilSource ? "oil source" : "refinery") + " for supply-relative placement.");
            return position;
        }

        private static bool EnsurePaidFuelStorage(EntityManager em, double now)
        {
            int living = 0;
            using var query = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent),
                typeof(SkirmishAttemptOwnedComponent), typeof(UnitHealth));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).FactionId == 1 &&
                    em.GetComponentData<SkirmishStructureIdentityComponent>(entity).StructureId.ToString() ==
                        Game.Skirmish.Contracts.SkirmishStructureIds.FuelBladder &&
                    em.GetComponentData<UnitHealth>(entity).Current > 0) living++;
            if (living > 0 && !paidFuelStorageRecovery) return true;
            if (!paidFuelStorageRecovery)
            {
                paidFuelStorageRecovery = true;
                paidIndustryStep = paidIndustrySite = paidIndustryBeforeConstruction = 0;
                paidIndustryTappedSite = false;
                Debug.Log("[S005PaidIndustry] phase=RecoverDestroyedFuelStorage input=Touch normalBudget=1");
            }
            if (!TickPaidIndustryBuilding(now, "Building_Fuel_Bladder", living, 1, "FuelBladder", 1, 0, 40)) return false;
            paidFuelStorageRecovery = false;
            paidIndustryStep = paidIndustrySite = 0;
            paidIndustryBeforeConstruction = paidIndustryOpeningCount;
            paidIndustryTappedSite = false;
            return true;
        }

        private static bool TickPaidGroundIndustryProbe(EntityManager em, double now, bool warmupOnly = false)
        {
            if (paidIndustryReady) return true;
            if (!paidIndustryWarmReady)
            {
                // Start the normal fabrication investment before buying the
                // logistics fleet so it can fund the remaining purchases.
                paidIndustryPlacementFocus ??= fixedWingCycleOnly
                    ? new Vector3(859.5f, 0, 622f) : FindNewestPlayerOilSourcePosition(em);
                int warmupAdded = paidIndustryAddedTarget;
                if (!TickPaidIndustryBuilding(now, "Building_Ammunition_Depot", CountLivingPlayerFabrication(em),
                    paidIndustryOpeningCount + warmupAdded, "Fabrication", warmupAdded, paidIndustryOpeningCount, 100)) return false;
                paidIndustryWarmReady = true;
                paidIndustryPlacementFocus = null;
                paidIndustryStep = paidIndustrySite = 0;
                paidIndustryTappedSite = false;
                paidIndustryBeforeConstruction = paidIndustryOpeningRefineryCount;
            }
            if (warmupOnly) return paidIndustryWarmReady;
            if (!paidIndustryOilReady)
            {
                // Native859 sampled these compact western supply pads on the
                // current Campaign source. They are search preferences only:
                // every purchase still requires the full live native validator,
                // eight-cell service spacing and normal touch confirmation.
                paidIndustryPlacementFocus ??= fixedWingCycleOnly
                    ? new Vector3(833.5f, 0, 604f) : FindNewestPlayerOilSourcePosition(em);
                if (!TickPaidIndustryBuilding(now, Game.Skirmish.Contracts.SkirmishStructureIds.VisualKey(Game.Skirmish.Contracts.SkirmishStructureIds.OilPump), CountLivingPlayerOilSources(em),
                    paidIndustryOpeningOilCount + 1, "OilPump", 1, paidIndustryOpeningOilCount, 100)) return false;
                paidIndustryOilReady = true;
                paidIndustryPlacementFocus = null;
                paidIndustryStep = paidIndustrySite = 0;
                paidIndustryTappedSite = false;
                paidIndustryBeforeConstruction = paidIndustryOpeningRefineryCount;
            }
            // Optional additional refinery/store purchases use the same normal
            // construction path when a qualification strategy requests them.
            if (!paidIndustryFuelReady)
            {
                // Choose the paid refinery near its oil supply, rather than HQ.
                // HQ proximity previously selected a legal site across a long
                // haul route and starved materials during aircraft research.
                paidIndustryPlacementFocus ??= fixedWingCycleOnly
                    ? new Vector3(834.5f, 0, 585.5f) : FindNewestPlayerOilSourcePosition(em);
                if (!TickPaidIndustryBuilding(now, "Building_Refinery", CountLivingPlayerRefineries(em),
                    paidIndustryOpeningRefineryCount + 1, "Refinery", 1, paidIndustryOpeningRefineryCount, 160)) return false;
                paidIndustryFuelReady = true;
                paidIndustryPlacementFocus = null;
                paidIndustryStep = paidIndustrySite = 0;
                paidIndustryTappedSite = false;
                paidIndustryBeforeConstruction = paidIndustryOpeningFuelStorageCount;
            }
            if (!paidIndustryLocalFuelStorageReady)
            {
                // A nearby receiving store keeps eight-barrel tanker loads from
                // being mostly consumed by the long trip to the original store.
                // Purchase and placement still use normal player input and rules.
                paidIndustryPlacementFocus ??= fixedWingCycleOnly
                    ? new Vector3(833f, 0, 563f) : FindNewestPlayerRefineryPosition(em);
                if (!TickPaidIndustryBuilding(now, "Building_Fuel_Bladder", CountLivingPlayerFuelStores(em),
                    paidIndustryOpeningFuelStorageCount + 1, "FuelBladder", 1, paidIndustryOpeningFuelStorageCount, 40)) return false;
                paidIndustryLocalFuelStorageReady = true;
                paidIndustryPlacementFocus = null;
                paidIndustryStep = paidIndustrySite = 0;
                paidIndustryTappedSite = false;
                paidIndustryBeforeConstruction = CountLivingPlayerFabrication(em);
            }
            if (!paidIndustryFabricationReady)
            {
                paidIndustryPlacementFocus ??= FindNewestPlayerOilSourcePosition(em);
                if (!TickPaidIndustryBuilding(now, "Building_Ammunition_Depot", CountLivingPlayerFabrication(em),
                    paidIndustryOpeningCount + paidIndustryAddedTarget, "Fabrication", paidIndustryAddedTarget,
                    paidIndustryOpeningCount, 100)) return false;
                paidIndustryFabricationReady = true;
                paidIndustryPlacementFocus = null;
                paidIndustryStep = paidIndustrySite = 0;
                paidIndustryTappedSite = false;
                paidIndustryBeforeConstruction = paidIndustryOpeningRefineryCount;
            }
            paidIndustryReady = true;
            return true;
        }

        private static bool TickPaidIndustryBuilding(double now, string prefabKey, int live, int goal,
            string kind, int added, int opening, int costEach)
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (live >= goal)
            {
                if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return false; }
                Debug.Log("[S005PaidIndustry] result=Built input=Touch kind=" + kind + " added=" + added + " costEach=" + costEach + " live=" + live +
                    " opening=" + opening + " normalBudget=1 campaignGeometryChanged=0");
                return true;
            }
            if (hud == null) return false;
            var placement = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
            if (paidIndustryStep == 0)
            {
                if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return false; }
                if (PlayerResumeTap(hud.PlayerFocusButton)) paidIndustryStep = 1;
                return false;
            }
            if (paidIndustryStep == 1)
            {
                if (placement != null && placement.HasPendingPlacement)
                {
                    if (!FindPlacementSourceCandidates(paidIndustryCandidateSites, 4, false, kind)) return false;
                    paidIndustrySite = 0;
                    paidIndustryTappedSite = false;
                    paidIndustryStep = 2;
                    return false;
                }
                var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                var button = drawer != null && drawer.IsOpen
                    ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Buildings, prefabKey)
                    : controls?.BuildButton;
                if (!PlayerResumeTap(button)) PlayerResumeScrollTo(button);
                return false;
            }
            if (paidIndustryStep == 3)
            {
                if (placement != null && placement.HasPendingPlacement) return false;
                if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return false; }
                // Await the real construction/component before another purchase.
                if (live > paidIndustryBeforeConstruction) paidIndustryStep = 0;
                return false;
            }
            if (placement == null || !placement.HasPendingPlacement)
                throw new InvalidOperationException("Paid " + kind + " placement closed before confirmation.");
            // The catalog popup can remain over the battlefield after selection.
            // Its normal close action preserves the active placement preview.
            if (drawer != null && drawer.IsOpen)
            {
                PlayerResumeTap(drawer.CloseButton);
                return false;
            }
            if (airportCameraPending)
            {
                FocusAirportSiteThroughMap(now);
                return false;
            }
            if (paidIndustryTappedSite && placement.ConfirmButton != null && placement.ConfirmButton.IsInteractable())
            {
                if (PlayerResumeTap(placement.ConfirmButton))
                {
                    paidIndustryBeforeConstruction = live;
                    paidIndustryStep = 3;
                }
                return false;
            }
            paidIndustryTappedSite = false;
            if (paidIndustrySite >= paidIndustryCandidateSites.Count)
                throw new InvalidOperationException(kind + " source-fit unresolved: native-validator candidates could not be committed through normal input.");
            Vector3 site = paidIndustryCandidateSites[paidIndustrySite++];
            Vector3 point = Camera.main != null ? Camera.main.WorldToScreenPoint(site) : Vector3.zero;
            if (point.z <= 0 || !Screen.safeArea.Contains(point))
            {
                Debug.Log("[S005PaidIndustry] phase=SiteSkipped reason=OutsideView kind=" + kind + " site=" + site);
                paidIndustrySite--;
                if (!BeginAirportCameraFocus(site, false)) paidIndustrySite++;
                return false;
            }
            var hits = new List<RaycastResult>();
            if (EventSystem.current != null)
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            if (hits.Count != 0)
            {
                Debug.Log("[S005PaidIndustry] phase=SiteSkipped reason=UiOcclusion kind=" + kind + " site=" + site +
                    " hit=" + hits[0].gameObject.name);
                paidIndustrySite--;
                if (!BeginAirportCameraFocus(site, false)) paidIndustrySite++;
                return false;
            }
            if (!playerResumeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                throw new InvalidOperationException("Paid fabrication site touch rejected.");
            paidIndustryTappedSite = true;
            playerResumeDue = now + 1;
            Debug.Log("[S005PaidIndustry] phase=Site input=Touch site=" + site);
            return false;
        }
    }
}
#endif
