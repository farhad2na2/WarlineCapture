#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Gate 3 trace: [SkirmishGateUnits] event=available at match start and event=used before the terminal probe.
    /// Log/trace only: it subscribes to AriaCommandEvidence.Recorded (accepted commands carrying a verified ARIA
    /// input receipt) and reads components; it issues no order and changes no gameplay state.
    ///
    /// Available (player unit types): the player's starting roster (setup.Forces for the player faction) plus
    /// every role in the compiled SkirmishProductionCatalog (the same catalog the production service, the
    /// build drawer and the enemy economy use: profile-allowed role overlays with an authored unit prefab and
    /// producer), plus each producer structure those roles need that the player does NOT start with (it must
    /// be placed: building.helipad, building.airport, ...). Types are role ids (role.*) and structure ids (building.*).
    /// Not modelled: readiness gating (roles stay listed before research unlocks them) and stocks/caps.
    /// Roles sharing one runtime prefab key (role.convoy_logic and role.logistics_truck both use
    /// Unit_Veh_Truck_Tray) are resolved by the catalog overlay's own RoleKind, so a role the army
    /// profile does not allow is never listed through another role's prefab.
    ///
    /// Support (LogisticsSupport: role.logistics_truck, role.tanker) haul Oil/Fuel automatically once
    /// fielded; they count as automatic when a player unit of that role carries a haul order
    /// (UnitResourceHaulOrder, sampled every 2 s). They are reported separately (automatic=) and are not
    /// "unused" when they worked; ARIA-input use of them is still listed under types= if it happens.
    ///
    /// Used: a type counts when ARIA input produced an accepted order for a unit of that role (Move/AttackMove/
    /// ReturnToBase: selected, ManualMoveOrderTag units; Group/Selection/Army orders: role of each group whose
    /// accepted-order ledger entry advanced), queued it (Production reservation) or placed it (Placement: a new
    /// player structure id). Per order cost is O(selected units) or O(groups); nothing runs per frame except one
    /// cheap pending-flag check.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private const string GateUnitsLog = "[SkirmishGateUnits]";

        private sealed class GateUnitsState
        {
            public string Catalog, SessionId;
            public Entity Session;
            public FixedString64Bytes SessionKey;
            public readonly SortedSet<string> Available = new(StringComparer.Ordinal);
            public readonly SortedSet<string> Used = new(StringComparer.Ordinal);
            public readonly HashSet<Entity> SeenStructures = new();
            public readonly Dictionary<uint, ulong> GroupSequences = new();
            public readonly SortedSet<string> Support = new(StringComparer.Ordinal);
            public readonly SortedSet<string> Automatic = new(StringComparer.Ordinal);
            public double NextSupportPoll;
            public uint LastReservation;
            public bool ArmyPending, PlacementPending;
            public double PlacementUntil;
        }

        private static GateUnitsState gateUnits;

        /// <summary>A role is producible only through its own catalog overlay; a shared runtime prefab
        /// key (convoy logic and logistics truck) never lists the other role.</summary>
        public static bool GateCatalogListsRole(SkirmishRoleKind kind, SkirmishRoleKind overlayKind) => kind == overlayKind;

        /// <summary>Support roles haul Oil/Fuel on their own once fielded.</summary>
        public static bool GateSupportRole(SkirmishRoleKind kind) =>
            SkirmishRoleIds.Category(kind) == SkirmishPopulationCategory.LogisticsSupport;

        private static string GateProducerStructure(SkirmishProducerKind producer) => producer switch
        {
            SkirmishProducerKind.Barracks => SkirmishStructureIds.Barracks,
            SkirmishProducerKind.GroundStaging => SkirmishStructureIds.GroundStaging,
            SkirmishProducerKind.Helipad => SkirmishStructureIds.Helipad,
            SkirmishProducerKind.Airport => SkirmishStructureIds.Airport,
            SkirmishProducerKind.SmallAirport => SkirmishStructureIds.AirportSmall,
            SkirmishProducerKind.IntelStation => SkirmishStructureIds.SatelliteDish,
            _ => null
        };

        /// <summary>The gate-3 event=available model (pure): the player's starting roles, every role the
        /// compiled catalog produces, and each producer structure of those roles the player does not start
        /// with. Adds to <paramref name="available"/>; returns the number of producible roles.</summary>
        public static int GateAvailableTypes(SkirmishResolvedSetup setup, SkirmishProductionCatalog catalog, ISet<string> available)
        {
            var startingStructures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var structure in setup.Structures ?? Array.Empty<SkirmishResolvedStructureEntry>())
                if (structure.FactionId == setup.PlayerFaction && !string.IsNullOrEmpty(structure.StructureId))
                    startingStructures.Add(structure.StructureId);
            foreach (var force in setup.Forces ?? Array.Empty<SkirmishResolvedForceEntry>())
                if (force.FactionId == setup.PlayerFaction && !string.IsNullOrEmpty(force.RoleId))
                    available.Add(force.RoleId);
            int producible = 0;
            for (int k = 1; k <= (int)SkirmishRoleKind.ConvoyLogic; k++)
            {
                var kind = (SkirmishRoleKind)k;
                string prefabKey;
                try { prefabKey = SkirmishRoleCatalogConfig.RuntimePrefabKey(kind); }
                catch (Exception) { continue; }
                if (string.IsNullOrEmpty(prefabKey) || !catalog.TryRole(prefabKey, out var overlay)) continue;
                // One prefab can serve two roles; only the catalog's own role is producible.
                if (!GateCatalogListsRole(kind, overlay.RoleKind)) continue;
                available.Add(SkirmishRoleIds.ToId(kind));
                producible++;
                string structureId = GateProducerStructure(overlay.Producer);
                if (structureId != null && !startingStructures.Contains(structureId)) available.Add(structureId);
            }
            return producible;
        }

        private static void StopGateUnits()
        {
            AriaCommandEvidence.Recorded -= GateOnCommand;
            EditorApplication.update -= GateUnitsTick;
            gateUnits = null;
        }

        /// <summary>Call once when ARIA begins (stage 1 -> 2, not startup-only). Logs event=available.</summary>
        private static void BeginGateUnits(EntityManager em, Entity session)
        {
            StopGateUnits();
            try
            {
                if (!em.HasComponent<SkirmishExpandedSessionComponent>(session) || !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                    !em.HasComponent<SkirmishProductionCatalogRecord>(session))
                    throw new InvalidOperationException("session has no expanded state/setup/production catalog");
                var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                var catalog = em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
                if (setup == null || catalog == null) throw new InvalidOperationException("setup or catalog is null");
                var state = new GateUnitsState { Catalog = catalogId, Session = session };
                var key = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                state.SessionKey = key;
                state.SessionId = key.ToString();
                if (string.IsNullOrEmpty(state.SessionId) || state.SessionId.IndexOfAny(new[] { ' ', '\t' }) >= 0)
                    throw new InvalidOperationException("session id is empty or contains whitespace");

                int producible = GateAvailableTypes(setup, catalog, state.Available);
                if (producible == 0) throw new InvalidOperationException("production catalog lists no role");
                foreach (var id in state.Available)
                    if (SkirmishRoleIds.TryParse(id, out var supportKind) && GateSupportRole(supportKind))
                        state.Support.Add(id);

                // Baselines so only later changes count as ARIA's.
                using (var structures = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishStructureIdentityComponent)))
                using (var entities = structures.ToEntityArray(Allocator.Temp))
                    foreach (var entity in entities) state.SeenStructures.Add(entity);
                if (em.HasBuffer<SkirmishProductionReservation>(session))
                    foreach (var reservation in em.GetBuffer<SkirmishProductionReservation>(session))
                        if (reservation.ReservationId > state.LastReservation) state.LastReservation = reservation.ReservationId;
                ReadArmyOrderAdvances(em, state, record: false);

                gateUnits = state;
                AriaCommandEvidence.Recorded += GateOnCommand;
                EditorApplication.update += GateUnitsTick;
                Debug.Log(GateUnitsLog + " event=available catalog=" + state.Catalog + " session=" + state.SessionId +
                    " types=" + string.Join(";", state.Available));
                Debug.Log("[SkirmishGateUnitsModel] catalog=" + state.Catalog + " available=setup.Forces(player)+SkirmishProductionCatalog.TryRole(overlay.RoleKind==kind)" +
                    "+producerStructuresNotInStartingStructures readinessGating=0 stocksAndCaps=0 used=AriaCommandEvidence.Recorded(verifiedReceipt)" +
                    "+SelectedManualMove+GroupRoleLedger+ProductionReservation+NewPlayerStructure support=" + string.Join(";", state.Support) +
                    " automatic=UnitResourceHaulOrder(player,2s)");
            }
            catch (Exception error)
            {
                StopGateUnits();
                Debug.LogWarning("[SkirmishGateUnitsModel] catalog=" + catalogId + " result=Unavailable reason=" + error.Message);
            }
        }

        /// <summary>Call at match end, before the terminal probe. Logs event=used once per session.</summary>
        private static void EmitGateUnitsUsed(EntityManager em)
        {
            var gate = gateUnits;
            if (gate == null) return;
            try { GateResolvePending(em, gate, force: true); }
            catch (Exception error) { Debug.LogWarning("[SkirmishGateUnitsModel] catalog=" + gate.Catalog + " pendingResolve=" + error.Message); }
            try { GatePollSupport(em, gate); }
            catch (Exception error) { Debug.LogWarning("[SkirmishGateUnitsModel] catalog=" + gate.Catalog + " supportPoll=" + error.Message); }
            var unused = new SortedSet<string>(gate.Available, StringComparer.Ordinal);
            unused.ExceptWith(gate.Used);
            unused.ExceptWith(gate.Automatic);
            Debug.Log(GateUnitsLog + " event=used catalog=" + gate.Catalog + " session=" + gate.SessionId +
                " types=" + string.Join(";", gate.Used) + " automatic=" + string.Join(";", gate.Automatic) +
                " unused=" + string.Join(";", unused));
            StopGateUnits();
        }

        private static void GateOnCommand(string operation, uint receipt, bool verified)
        {
            var gate = gateUnits;
            if (gate == null || !verified) return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;
            switch (operation)
            {
                case "Move":
                case "AttackMove":
                    GateNoteSelected(em, gate, requireManualMove: true);
                    break;
                case "ReturnToBase":
                    GateNoteSelected(em, gate, requireManualMove: false);
                    break;
                case "GroupOrder":
                case "SelectionOrder":
                case "ArmyAttack":
                    gate.ArmyPending = true; // ArmyAttack stamps its ledger entry after the event; resolve next tick.
                    break;
                case "Production":
                    GateNoteProduction(em, gate);
                    break;
                case "Placement":
                    gate.PlacementPending = true;
                    gate.PlacementUntil = EditorApplication.timeSinceStartup + 60d;
                    break;
            }
        }

        private static void GateUnitsTick()
        {
            var gate = gateUnits;
            if (gate == null) return;
            bool supportDue = gate.Support.Count > gate.Automatic.Count && EditorApplication.timeSinceStartup >= gate.NextSupportPoll;
            if (!gate.ArmyPending && !gate.PlacementPending && !supportDue) return;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            if (supportDue)
            {
                gate.NextSupportPoll = EditorApplication.timeSinceStartup + 2d; // bounded poll, not per frame
                try { GatePollSupport(world.EntityManager, gate); }
                catch (Exception error)
                {
                    gate.NextSupportPoll = double.MaxValue;
                    Debug.LogWarning("[SkirmishGateUnitsModel] catalog=" + gate.Catalog + " supportPoll=" + error.Message);
                }
                if (!gate.ArmyPending && !gate.PlacementPending) return;
            }
            try { GateResolvePending(world.EntityManager, gate, force: false); }
            catch (Exception error)
            {
                gate.ArmyPending = gate.PlacementPending = false;
                Debug.LogWarning("[SkirmishGateUnitsModel] catalog=" + gate.Catalog + " pendingResolve=" + error.Message);
            }
        }

        private static double gateNextStructurePoll;

        private static void GateResolvePending(EntityManager em, GateUnitsState gate, bool force)
        {
            if (gate.ArmyPending)
            {
                gate.ArmyPending = false;
                ReadArmyOrderAdvances(em, gate, record: true);
            }
            if (gate.PlacementPending)
            {
                double now = EditorApplication.timeSinceStartup;
                if (!force && now < gateNextStructurePoll) return;
                gateNextStructurePoll = now + 0.5d; // bounded poll, not per frame
                bool found = false;
                using (var structures = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishStructureIdentityComponent)))
                using (var entities = structures.ToEntityArray(Allocator.Temp))
                    foreach (var entity in entities)
                    {
                        if (!gate.SeenStructures.Add(entity)) continue;
                        var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                        if (owned.FactionId != 1 || !owned.SessionId.Equals(gate.SessionKey)) continue;
                        gate.Used.Add(em.GetComponentData<SkirmishStructureIdentityComponent>(entity).StructureId.ToString());
                        found = true;
                    }
                if (found || now > gate.PlacementUntil || force) gate.PlacementPending = false;
            }
        }

        // Support roles working on their own: a player unit of that role carries a resource haul order.
        private static void GatePollSupport(EntityManager em, GateUnitsState gate)
        {
            if (gate.Support.Count == 0 || gate.Automatic.Count >= gate.Support.Count) return;
            using var query = em.CreateEntityQuery(typeof(UnitResourceHaulOrder), typeof(SkirmishUnitRoleComponent), typeof(SkirmishAttemptOwnedComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in entities)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId != 1 || !owned.SessionId.Equals(gate.SessionKey)) continue;
                string id = SkirmishRoleIds.ToId(em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role);
                if (gate.Support.Contains(id)) gate.Automatic.Add(id);
            }
        }

        // Selected player units that now carry the manual-move order (or any selected unit for ReturnToBase).
        private static void GateNoteSelected(EntityManager em, GateUnitsState gate, bool requireManualMove)
        {
            using var query = em.CreateEntityQuery(typeof(SelectedUnitTag), typeof(SkirmishUnitRoleComponent), typeof(SkirmishAttemptOwnedComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in entities)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owned.FactionId != 1 || !owned.SessionId.Equals(gate.SessionKey)) continue;
                if (requireManualMove && !em.HasComponent<ManualMoveOrderTag>(unit)) continue;
                gate.Used.Add(SkirmishRoleIds.ToId(em.GetComponentData<SkirmishUnitRoleComponent>(unit).Role));
            }
        }

        private static void GateNoteProduction(EntityManager em, GateUnitsState gate)
        {
            if (!em.HasBuffer<SkirmishProductionReservation>(gate.Session)) return;
            var reservations = em.GetBuffer<SkirmishProductionReservation>(gate.Session);
            uint highest = gate.LastReservation;
            for (int i = 0; i < reservations.Length; i++)
            {
                var reservation = reservations[i];
                if (reservation.ReservationId <= gate.LastReservation || reservation.FactionId != 1) continue;
                gate.Used.Add(SkirmishRoleIds.ToId(reservation.Role));
                if (reservation.ReservationId > highest) highest = reservation.ReservationId;
            }
            gate.LastReservation = highest;
        }

        // Groups are role-homogeneous (SkirmishArmyGroupRecord.Role). A group whose accepted-order ledger
        // sequence advanced since the last look received an accepted order through a verified ARIA command.
        private static void ReadArmyOrderAdvances(EntityManager em, GateUnitsState gate, bool record)
        {
            if (!em.HasBuffer<SkirmishArmyGroupRecord>(gate.Session)) return;
            var ledger = SkirmishAcceptedArmyOrders.For(em);
            var groups = em.GetBuffer<SkirmishArmyGroupRecord>(gate.Session);
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (group.FactionId != 1 || !ledger.TryRead(gate.SessionId, group.GroupId, out var intent)) continue;
                gate.GroupSequences.TryGetValue(group.GroupId, out ulong seen);
                if (intent.Sequence <= seen) continue;
                gate.GroupSequences[group.GroupId] = intent.Sequence;
                if (record) gate.Used.Add(SkirmishRoleIds.ToId(group.Role));
            }
        }
    }
}
#endif
