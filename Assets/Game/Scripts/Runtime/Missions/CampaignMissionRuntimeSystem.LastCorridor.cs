using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes CorridorId = CampaignMissionSequence.LastCorridor;
        private bool TryAdvanceLastCorridor(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!runtime.MissionId.Equals(CorridorId)) return false;
            var em = system.EntityManager; var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if (runtime.Outcome != MissionOutcomeKind.None) return true;
            if (runtime.Phase == MissionPhaseKind.Preparing) { if (TryTransition(in runtime, MissionPhaseKind.InteractiveBrief, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var next)) em.SetComponentData(root, next); return true; }
            if (runtime.Phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0) { if (TryTransition(in runtime, MissionPhaseKind.FindSquad, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var next)) em.SetComponentData(root, next); return true; }
            if (facts.CommandSquadSpawned == 0 || !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return true;
            if (!em.HasComponent<CampaignMissionLastCorridorState>(root) || em.GetComponentData<CampaignMissionLastCorridorState>(root).Initialized == 0) InitializeLastCorridor(em, root, in runtime, ref metadata.Blob.Value);
            var m = em.GetComponentData<CampaignMissionLastCorridorState>(root); if (!CampaignMissionLastCorridorRuleUtility.Matches(in m, in runtime)) return true;
            ProjectCorridorBuildings(em, root, ref metadata.Blob.Value, ref m);
            var scope = new SupportFuelScopeComponent { Storage = m.Reserve, Required = 1 }; if (em.HasComponent<SupportFuelScopeComponent>(root)) em.SetComponentData(root, scope); else em.AddComponentData(root, scope);
            bool opening = em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) && em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).SessionToken.Equals(runtime.SessionToken) && em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage >= 6;
            bool active = runtime.Phase == MissionPhaseKind.Engage && opening && m.Ready != 0;
            ApplyCorridorActivationPolicy(em, root, active);
            int delta = active ? (int)math.round(SystemAPI.Time.DeltaTime * 1000) : 0; if (active) m.ElapsedMilliseconds = SaturatingAddMilliseconds(m.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
            var members = em.GetBuffer<CampaignMissionLastCorridorMember>(root); int initialized = 0, military = 0, defeated = 0, staff = 0, staffLost = 0, escorts = 0, losses = 0, reinforcements = 0; bool reinforcementsAtGate = true;
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i]; bool exists = em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity);
                bool repairedWreck = member.Kind == LastCorridorMemberKind.BrokenLink && m.LinkRecovered != 0;
                if (!exists && member.Dead == 0 && !repairedWreck) MarkCorridorIntegrity(ref m, "Original actor disappeared: " + member.Kind);
                if (exists && em.GetComponentData<UnitHealth>(member.Entity).Max > 0) member.HealthInitialized = 1;
                if (member.HealthInitialized != 0 || repairedWreck) initialized++;
                if (member.HealthInitialized != 0 && (!exists || em.GetComponentData<UnitHealth>(member.Entity).Current <= 0)) member.Dead = 1;
                members[i] = member;
                if (member.Kind is LastCorridorMemberKind.Blockade or LastCorridorMemberKind.RouteGuard) { military++; defeated += member.Dead; }
                if (member.Kind == LastCorridorMemberKind.CivicStaff) { staff++; staffLost += member.Dead; }
                if (member.Kind == LastCorridorMemberKind.Escort) { losses += member.Dead; if (member.Dead == 0) escorts++; }
                if (member.Kind == LastCorridorMemberKind.Reinforcement) { losses += member.Dead; if (member.Dead == 0) { reinforcements++; reinforcementsAtGate &= CitywideStoppedAt(em, member.Entity, m.ReinforcementGate, 10); } }
                if (member.Dead == 0) continue;
                if (member.Kind == LastCorridorMemberKind.Engineer) m.Failure = LastCorridorFailure.EngineerLost;
                if (member.Kind == LastCorridorMemberKind.KeyCarrier) m.Failure = LastCorridorFailure.KeysLost;
                if (member.Kind == LastCorridorMemberKind.MedicineCarrier && m.MedicineDelivered == 0) m.Failure = LastCorridorFailure.MedicineLost;
                if (member.Kind == LastCorridorMemberKind.FuelCarrier && m.FuelDelivered == 0) m.Failure = LastCorridorFailure.FuelCargoLost;
            }
            if (members.Length != 26 || military != 7 || staff != 4) MarkCorridorIntegrity(ref m, "Closed original roster mismatch");
            if (staffLost > 0) m.Failure = LastCorridorFailure.StaffLost;
            if (initialized == 26 && escorts == 0 && defeated < military) m.Failure = LastCorridorFailure.EscortLost;
            if (initialized == 26 && reinforcements == 0) m.Failure = LastCorridorFailure.ReinforcementsLost;
            if (m.ElapsedMilliseconds >= CampaignMissionLastCorridorRuleUtility.DeadlineMilliseconds) m.Failure = LastCorridorFailure.Deadline;
            m.MilitaryCleared = (byte)(military == 7 && defeated == 7 ? 1 : 0);
            if (m.MilitaryCleared != 0) m.CommsReady = 1;
            if (m.CargoLoaded == 0 && CorridorLive(em, m.Reserve) && em.HasComponent<BuildingResourceStorageComponent>(m.Reserve)) TryLoadCorridorFuel(em, in runtime, ref m);
            if (initialized == 26 && m.Civic != Entity.Null && m.Reserve != Entity.Null && m.Receiver != Entity.Null && m.CargoLoaded != 0) m.Ready = 1;
            bool repair = active && m.MilitaryCleared != 0 && m.LinkRecovered == 0 && CitywideStoppedAt(em, m.Engineer, m.RepairGate, 6) && CorridorLive(em, m.BrokenLink) && em.HasComponent<StaticGridBlocker>(m.BrokenLink);
            if (m.LinkRecovered == 0)
            {
                m.RepairMilliseconds = CampaignMissionLastCorridorRuleUtility.AdvanceHold(m.RepairMilliseconds, delta, repair);
                if (m.RepairMilliseconds >= 6000) TryRecoverCorridorLink(em, ref m);
            }
            if (m.LinkRecovered == 0 && !CorridorLive(em, m.BrokenLink)) MarkCorridorIntegrity(ref m, "Original broken link lost before recovery");
            if (active && m.MedicineDelivered == 0)
            {
                m.MedicineRouteStep = ObserveCorridorRoute(em, m.MedicineCarrier, in m, m.MedicineRouteStep, 2);
                bool qualified = m.LinkRecovered != 0 && m.MedicineRouteStep == 3 && CitywideStoppedAt(em, m.MedicineCarrier, m.MedicineGate, 8) && CorridorCargoMatches(em, m.MedicineCarrier, in runtime, CorridorCargoKind.Medicine, 20);
                m.MedicineMilliseconds = CampaignMissionLastCorridorRuleUtility.AdvanceHold(m.MedicineMilliseconds, delta, qualified);
                if (m.MedicineMilliseconds >= 6000) { DeliverCorridorCargo(em, m.MedicineCarrier); m.MedicineDelivered = 1; }
            }
            if (active && m.MedicineDelivered != 0 && m.FuelDelivered == 0)
            {
                SelectCorridorRoute(em, m.FuelCarrier, in m, ref m.FuelRoute);
                m.FuelRouteStep = ObserveCorridorRoute(em, m.FuelCarrier, in m, m.FuelRouteStep, m.FuelRoute);
                bool qualified = m.FuelRoute != 0 && m.FuelRouteStep == 3 && CitywideStoppedAt(em, m.FuelCarrier, m.FuelGate, 8) && CorridorCargoMatches(em, m.FuelCarrier, in runtime, CorridorCargoKind.Fuel, 40);
                m.FuelMilliseconds = CampaignMissionLastCorridorRuleUtility.AdvanceHold(m.FuelMilliseconds, delta, qualified);
                if (m.FuelMilliseconds >= 6000 && TryReceiveCorridorFuel(em, m.Receiver, m.FuelCarrier, in runtime)) { m.FuelDelivered = 1; m.DeliveredFuelBarrels = 40; }
            }
            if (m.ReinforcementsDelivered == 0) { m.ReinforcementMilliseconds = CampaignMissionLastCorridorRuleUtility.AdvanceHold(m.ReinforcementMilliseconds, delta, active && m.FuelDelivered != 0 && reinforcements > 0 && reinforcementsAtGate); if (m.ReinforcementMilliseconds >= 6000) m.ReinforcementsDelivered = 1; }
            if (CorridorLive(em, m.KeyCarrier) && em.HasComponent<LocalTransform>(m.KeyCarrier)) m.EngineerPickup = em.GetComponentData<LocalTransform>(m.KeyCarrier).Position;
            if (active && m.ReinforcementsDelivered != 0 && CitywideStoppedAt(em, m.Engineer, m.EngineerPickup, 8)) m.EngineerReady = 1;
            m.EngineerAboard = (byte)(CorridorEngineerAboard(em, in m) ? 1 : 0);
            if (active && m.ReinforcementsDelivered != 0 && m.EngineerAboard != 0) m.EngineerReady = 1;
            if (active && m.EngineerAboard != 0)
            {
                SelectCorridorRoute(em, m.KeyCarrier, in m, ref m.KeyRoute);
                m.KeyRouteStep = ObserveCorridorRoute(em, m.KeyCarrier, in m, m.KeyRouteStep, m.KeyRoute);
            }
            bool keyQualified = active && m.ReinforcementsDelivered != 0 && m.EngineerAboard != 0 && m.KeyRoute != 0 && m.KeyRouteStep == 3 && CitywideStoppedAt(em, m.KeyCarrier, m.KeyReceiver, 8) && CorridorCargoMatches(em, m.KeyCarrier, in runtime, CorridorCargoKind.Keys, 1);
            if (m.KeysDelivered == 0) { m.KeyMilliseconds = CampaignMissionLastCorridorRuleUtility.AdvanceHold(m.KeyMilliseconds, delta, keyQualified); if (m.KeyMilliseconds >= 6000) { DeliverCorridorCargo(em, m.KeyCarrier); m.KeysDelivered = 1; m.EngineerDelivered = 1; } }
            facts.ElapsedMilliseconds = m.ElapsedMilliseconds; facts.HostileTotalCount = military; facts.HostileDefeatedCount = defeated; facts.CivilianTotalCount = staff; facts.CivilianLossCount = staffLost; facts.SquadLossCount = losses; facts.CommandSquadAlive = (byte)(CorridorLive(em, m.Engineer) ? 1 : 0);
            facts.CorridorSuppliesDelivered = (byte)(m.MedicineDelivered + m.FuelDelivered + m.ReinforcementsDelivered); facts.CorridorLinkRecovered = m.LinkRecovered; facts.CorridorEngineerDelivered = m.EngineerDelivered; facts.CorridorKeysDelivered = m.KeysDelivered; facts.CorridorCommsReady = m.CommsReady; facts.CorridorFailure = m.Failure; facts.CorridorFuelReceived = m.DeliveredFuelBarrels;
            if (m.Failure == LastCorridorFailure.Integrity) facts.HostileRosterIntegrityFault = 1;
            em.SetComponentData(root, m); em.SetComponentData(root, facts);
            if (CampaignMissionLastCorridorRuleUtility.TryAdvance(in runtime, in facts, in m, opening, out var changed)) em.SetComponentData(root, changed);
            return true;
        }
        internal static bool CorridorLive(EntityManager em, Entity e) => NetworkLive(em, e);
        private static void MarkCorridorIntegrity(ref CampaignMissionLastCorridorState s, string detail) { if (s.Failure != LastCorridorFailure.Integrity) UnityEngine.Debug.LogError("[LastCorridorIntegrity] " + detail); s.Failure = LastCorridorFailure.Integrity; }
        internal static bool CorridorCargoMatches(EntityManager em, Entity carrier, in CampaignMissionRuntimeComponent r, CorridorCargoKind kind, int quantity)
        {
            if (!CorridorLive(em, carrier) || !em.HasComponent<CampaignMissionCorridorCargo>(carrier)) return false;
            var c = em.GetComponentData<CampaignMissionCorridorCargo>(carrier); return c.SessionToken.Equals(r.SessionToken) && c.AttemptOrdinal == r.AttemptOrdinal && c.SourceVersion == r.SourceVersion && c.Kind == kind && c.Quantity == quantity && c.Delivered == 0;
        }
        private static void DeliverCorridorCargo(EntityManager em, Entity carrier) { var c = em.GetComponentData<CampaignMissionCorridorCargo>(carrier); c.Delivered = 1; em.SetComponentData(carrier, c); }
        internal static bool TryReceiveCorridorFuel(EntityManager em, Entity receiver, Entity carrier, in CampaignMissionRuntimeComponent r)
        {
            if (!CorridorLive(em, receiver) || !em.HasComponent<BuildingResourceStorageComponent>(receiver) || !CorridorCargoMatches(em, carrier, in r, CorridorCargoKind.Fuel, 40)) return false;
            var storage = em.GetComponentData<BuildingResourceStorageComponent>(receiver);
            if (storage.OwnerFactionId != 1 || storage.FuelStorageCapacity - storage.StoredFuelBarrels < 40) return false;
            storage.StoredFuelBarrels += 40; storage.Version++; em.SetComponentData(receiver, storage); DeliverCorridorCargo(em, carrier); return true;
        }
        internal static bool TryLoadCorridorFuel(EntityManager em, in CampaignMissionRuntimeComponent r, ref CampaignMissionLastCorridorState m)
        {
            if (m.CargoLoaded != 0 || !CampaignMissionLastCorridorRuleUtility.Matches(in m, in r) || !CorridorLive(em, m.Reserve) || !em.HasComponent<BuildingResourceStorageComponent>(m.Reserve) || !CorridorCargoMatches(em, m.FuelCarrier, in r, CorridorCargoKind.Fuel, 0)) return false;
            var storage = em.GetComponentData<BuildingResourceStorageComponent>(m.Reserve);
            if (storage.OwnerFactionId != 1 || storage.StoredFuelBarrels - storage.CivilianFuelReserveBarrels < 40) return false;
            storage.StoredFuelBarrels -= 40; storage.Version++; em.SetComponentData(m.Reserve, storage);
            em.SetComponentData(m.FuelCarrier, new CampaignMissionCorridorCargo { SessionToken = r.SessionToken, AttemptOrdinal = r.AttemptOrdinal, SourceVersion = r.SourceVersion, Kind = CorridorCargoKind.Fuel, Quantity = 40 }); m.CargoLoaded = 1; return true;
        }
        internal static bool TryRecoverCorridorLink(EntityManager em, ref CampaignMissionLastCorridorState m)
        {
            if (m.LinkRecovered != 0 || m.MilitaryCleared == 0 || m.RepairMilliseconds < 6000 || !CitywideStoppedAt(em, m.Engineer, m.RepairGate, 6) || !CorridorLive(em, m.BrokenLink) || !em.HasComponent<StaticGridBlocker>(m.BrokenLink)) return false;
            em.DestroyEntity(m.BrokenLink); m.LinkRecovered = 1; return true;
        }
        internal static bool CorridorEngineerAboard(EntityManager em, in CampaignMissionLastCorridorState m)
        {
            if (!CorridorLive(em, m.Engineer) || !CorridorLive(em, m.KeyCarrier) || !em.HasComponent<UnitTransportPassenger>(m.Engineer) || em.GetComponentData<UnitTransportPassenger>(m.Engineer).Transport != m.KeyCarrier || !em.HasBuffer<UnitTransportPassengerElement>(m.KeyCarrier)) return false;
            foreach (var p in em.GetBuffer<UnitTransportPassengerElement>(m.KeyCarrier, true)) if (p.Passenger == m.Engineer) return true; return false;
        }
        private static byte ObserveCorridorRoute(EntityManager em, Entity actor, in CampaignMissionLastCorridorState s, byte step, byte route)
        {
            if (route == 0 || !CorridorLive(em, actor) || !em.HasComponent<LocalTransform>(actor)) return 0;
            return CampaignMissionLastCorridorRuleUtility.AdvanceRoute(step, em.GetComponentData<LocalTransform>(actor).Position, route == 1 ? s.MainEntry : s.AlternateEntry, route == 1 ? s.MainMiddle : s.AlternateMiddle, route == 1 ? s.MainExit : s.AlternateExit);
        }
        private static void SelectCorridorRoute(EntityManager em, Entity actor, in CampaignMissionLastCorridorState s, ref byte route)
        {
            if (route != 0 || !CorridorLive(em, actor) || !em.HasComponent<LocalTransform>(actor)) return; var p = em.GetComponentData<LocalTransform>(actor).Position;
            if (s.LinkRecovered != 0 && math.distancesq(p.xz, s.MainEntry.xz) <= 25) route = 1;
            else if (math.distancesq(p.xz, s.AlternateEntry.xz) <= 25) route = 2;
        }
        public static float3 CorridorRouteTarget(in CampaignMissionLastCorridorState s, CorridorCargoKind kind)
        {
            byte step = kind == CorridorCargoKind.Medicine ? s.MedicineRouteStep : kind == CorridorCargoKind.Fuel ? s.FuelRouteStep : s.KeyRouteStep;
            byte route = kind == CorridorCargoKind.Medicine ? (byte)2 : kind == CorridorCargoKind.Fuel ? s.FuelRoute : s.KeyRoute;
            if (step >= 3) return kind == CorridorCargoKind.Medicine ? s.MedicineGate : kind == CorridorCargoKind.Fuel ? s.FuelGate : s.KeyReceiver;
            bool main = route != 2; return step == 0 ? main ? s.MainEntry : s.AlternateEntry : step == 1 ? main ? s.MainMiddle : s.AlternateMiddle : main ? s.MainExit : s.AlternateExit;
        }
        internal static Entity ResolveCorridorEscort(EntityManager em, Entity root)
        {
            Entity best = Entity.Null; float range = 0; foreach (var p in em.GetBuffer<CampaignMissionLastCorridorMember>(root, true))
                if (p.Kind == LastCorridorMemberKind.Escort && p.Dead == 0 && CorridorLive(em, p.Entity) && em.HasComponent<UnitAttack>(p.Entity) && em.HasComponent<UnitCombat>(p.Entity)) { var weapon = em.GetComponentData<UnitAttack>(p.Entity); if (em.GetComponentData<UnitCombat>(p.Entity).CanAttack != 0 && weapon.Damage > 0 && weapon.Range > range) { range = weapon.Range; best = p.Entity; } } return best;
        }
        internal static Entity ResolveCorridorHostile(EntityManager em, Entity root)
        {
            foreach (var p in em.GetBuffer<CampaignMissionLastCorridorMember>(root, true)) if (p.Kind is LastCorridorMemberKind.Blockade or LastCorridorMemberKind.RouteGuard && p.Dead == 0 && CorridorLive(em, p.Entity)) return p.Entity; return Entity.Null;
        }
        internal static void InitializeLastCorridor(EntityManager em, Entity root, in CampaignMissionRuntimeComponent r, ref OperationMapBlob map)
        {
            var m = new CampaignMissionLastCorridorState { SessionToken = r.SessionToken, AttemptOrdinal = r.AttemptOrdinal, SourceVersion = r.SourceVersion, Initialized = 1 };
            string[] names = { "repair", "medicine_receiver", "fuel_receiver", "reinforcement_receiver", "engineer_pickup", "key_receiver", "main_entry", "main_middle", "main_exit", "alternate_entry", "alternate_middle", "alternate_exit" };
            var points = new float3[names.Length]; for (int i = 0; i < names.Length; i++) { if (!CampaignMissionSpawnSystem.TryFindAnchor(ref map, new FixedString64Bytes("anchor.ch05.m04." + names[i]), out var a)) MarkCorridorIntegrity(ref m, "Missing anchor " + names[i]); points[i] = a.Position; }
            m.RepairGate = points[0]; m.MedicineGate = points[1]; m.FuelGate = points[2]; m.ReinforcementGate = points[3]; m.EngineerPickup = points[4]; m.KeyReceiver = points[5]; m.MainEntry = points[6]; m.MainMiddle = points[7]; m.MainExit = points[8]; m.AlternateEntry = points[9]; m.AlternateMiddle = points[10]; m.AlternateExit = points[11];
            if (!em.HasBuffer<CampaignMissionLastCorridorMember>(root)) em.AddBuffer<CampaignMissionLastCorridorMember>(root); var roster = em.GetBuffer<CampaignMissionLastCorridorMember>(root); roster.Clear();
            using var query = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform)); using var entities = query.ToEntityArray(Allocator.Temp); using var commands = new EntityCommandBuffer(Allocator.Temp);
            foreach (var e in entities)
            {
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(e); if (!role.SessionToken.Equals(r.SessionToken)) continue;
                var kind = role.MissionRoleId.ToString() switch { "role.friendly.escort" => LastCorridorMemberKind.Escort, "role.friendly.engineer" => LastCorridorMemberKind.Engineer, "role.friendly.key_carrier" => LastCorridorMemberKind.KeyCarrier, "role.friendly.medicine" => LastCorridorMemberKind.MedicineCarrier, "role.friendly.fuel" => LastCorridorMemberKind.FuelCarrier, "role.friendly.reinforcement" => LastCorridorMemberKind.Reinforcement, "role.hostile.blockade" => LastCorridorMemberKind.Blockade, "role.hostile.route" => LastCorridorMemberKind.RouteGuard, "role.civilian.protected" => LastCorridorMemberKind.CivicStaff, "role.infrastructure.broken_link" => LastCorridorMemberKind.BrokenLink, _ => (LastCorridorMemberKind)255 };
                if ((byte)kind == 255) { MarkCorridorIntegrity(ref m, "Unknown original role " + role.MissionRoleId); continue; } roster.Add(new CampaignMissionLastCorridorMember { Entity = e, Kind = kind });
                if (kind == LastCorridorMemberKind.Engineer) m.Engineer = e; if (kind == LastCorridorMemberKind.KeyCarrier) m.KeyCarrier = e; if (kind == LastCorridorMemberKind.MedicineCarrier) m.MedicineCarrier = e; if (kind == LastCorridorMemberKind.FuelCarrier) m.FuelCarrier = e; if (kind == LastCorridorMemberKind.BrokenLink) m.BrokenLink = e;
                if (kind is LastCorridorMemberKind.Engineer or LastCorridorMemberKind.CivicStaff or LastCorridorMemberKind.BrokenLink or LastCorridorMemberKind.MedicineCarrier or LastCorridorMemberKind.FuelCarrier) QualifyCitywideEngineer(em, e);
                if (kind is LastCorridorMemberKind.CivicStaff or LastCorridorMemberKind.Blockade or LastCorridorMemberKind.RouteGuard or LastCorridorMemberKind.BrokenLink) { if (!em.HasComponent<CampaignMissionStationaryUnitTag>(e)) commands.AddComponent<CampaignMissionStationaryUnitTag>(e); }
                if (kind == LastCorridorMemberKind.BrokenLink) { if (!em.HasComponent<StaticGridBlocker>(e)) commands.AddComponent<StaticGridBlocker>(e); if (!em.HasComponent<GridBlockerSize>(e)) commands.AddComponent(e, new GridBlockerSize { Size = new int2(5, 5) }); }
                if (em.HasComponent<UnitMovementBehavior>(e)) { var movement = em.GetComponentData<UnitMovementBehavior>(e); movement.AllowIdleWander = 0; em.SetComponentData(e, movement); }
                if (kind is LastCorridorMemberKind.Blockade or LastCorridorMemberKind.RouteGuard or LastCorridorMemberKind.BrokenLink && em.HasComponent<UnitFuelConsumption>(e)) { var fuel = em.GetComponentData<UnitFuelConsumption>(e); fuel.Enabled = 0; em.SetComponentData(e, fuel); }
                if (kind is LastCorridorMemberKind.MedicineCarrier or LastCorridorMemberKind.FuelCarrier or LastCorridorMemberKind.KeyCarrier)
                {
                    var cargo = new CampaignMissionCorridorCargo { SessionToken = r.SessionToken, AttemptOrdinal = r.AttemptOrdinal, SourceVersion = r.SourceVersion, Kind = kind == LastCorridorMemberKind.MedicineCarrier ? CorridorCargoKind.Medicine : kind == LastCorridorMemberKind.FuelCarrier ? CorridorCargoKind.Fuel : CorridorCargoKind.Keys, Quantity = kind == LastCorridorMemberKind.MedicineCarrier ? 20 : kind == LastCorridorMemberKind.FuelCarrier ? 0 : 1 };
                    if (em.HasComponent<CampaignMissionCorridorCargo>(e)) em.SetComponentData(e, cargo); else commands.AddComponent(e, cargo);
                }
            }
            int count = roster.Length; commands.Playback(em);
            if (count != 26 || m.Engineer == Entity.Null || m.KeyCarrier == Entity.Null || m.MedicineCarrier == Entity.Null || m.FuelCarrier == Entity.Null || m.BrokenLink == Entity.Null) MarkCorridorIntegrity(ref m, "Original roster incomplete");
            if (!em.HasBuffer<CampaignMissionCorridorBuildingRequest>(root)) em.AddBuffer<CampaignMissionCorridorBuildingRequest>(root); var bindings = em.GetBuffer<CampaignMissionCorridorBuildingRequest>(root); bindings.Clear(); for (int i = 0; i < 3; i++) bindings.Add(default);
            if (em.HasComponent<CampaignMissionLastCorridorState>(root)) em.SetComponentData(root, m); else em.AddComponentData(root, m);
        }
        private static void ApplyCorridorActivationPolicy(EntityManager em, Entity root, bool active)
        {
            foreach (var member in em.GetBuffer<CampaignMissionLastCorridorMember>(root, true))
            { if (member.Dead != 0 || !em.Exists(member.Entity) || !em.HasComponent<ArmorBreakUnitActivationState>(member.Entity)) continue; var original = em.GetComponentData<ArmorBreakUnitActivationState>(member.Entity);
                if (em.HasComponent<UnitFuelConsumption>(member.Entity)) { var fuel = em.GetComponentData<UnitFuelConsumption>(member.Entity); byte enabled = active ? original.OriginalFuelEnabled : (byte)0; if (fuel.Enabled != enabled) { fuel.Enabled = enabled; em.SetComponentData(member.Entity, fuel); if (enabled != 0 && em.HasComponent<UnitFuelConsumptionState>(member.Entity)) em.SetComponentData(member.Entity, default(UnitFuelConsumptionState)); } }
                if (em.HasComponent<UnitCombat>(member.Entity)) { var combat = em.GetComponentData<UnitCombat>(member.Entity); combat.AutoEngage = active ? original.OriginalAutoEngage : (byte)0; em.SetComponentData(member.Entity, combat); }
            }
        }
    }
}
