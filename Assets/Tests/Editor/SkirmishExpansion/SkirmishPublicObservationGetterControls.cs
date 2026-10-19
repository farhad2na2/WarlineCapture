using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Game.Tests.Editor
{
    // Named getter fragment only. No actual mission, payment, order or live_api admission.
    [NonParallelizable]
    public sealed class SkirmishPublicObservationGetterControls
    {
        [SetUp]
        public void RequireStoppedEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Owned getter controls require stopped stable Editor.");
        }

        private sealed class Fixture : IDisposable
        {
            internal readonly World World = new World("PublicObservationGetterControls-Owned");
            internal EntityManager Em => World.EntityManager;
            internal readonly SkirmishArmyProfileConfig Army;
            internal readonly SkirmishResolvedSetup Setup;
            internal readonly Entity Session;
            private readonly List<Entity> entities = new List<Entity>();

            internal Fixture()
            {
                try
                {
                Army = ScriptableObject.CreateInstance<SkirmishArmyProfileConfig>();
                Army.hideFlags = HideFlags.HideAndDontSave;
                Army.ConfigureGroundManeuver();
                // Deliberate standalone fixture prices, not authored mission budgets.
                Setup = new SkirmishResolvedSetup { ArmyProfileId = SkirmishArmyProfileId.GroundManeuver, Readiness = SkirmishReadinessStage.Field,
                    LogisticsSupportCapEach = 4, RoleOverlays = new[] { new SkirmishRoleOverlay {
                        RoleId = SkirmishRoleIds.Rifle, RoleKind = SkirmishRoleKind.Rifle,
                        MaterialsCost = 37, SupplyCost = 1, Producer = SkirmishProducerKind.Barracks } } };
                Session = Entity();
                Em.AddComponentData(Session, new SkirmishExpandedSessionComponent { SessionId = "getter-owned", Phase = SkirmishSessionPhase.Playing });
                Em.AddComponentData(Session, new SkirmishEconomyStockComponent { FactionId = 1, Materials = 37, Fuel = 5 });
                Em.AddComponentData(Session, new SkirmishEnemyStockComponent { Materials = 997, Fuel = 991 });
                Em.AddComponentData(Session, new SkirmishCapacityComponent { FactionId = 1,
                    InfantryCap = 48, GroundCap = 8, AirCap = 2, SupplyCap = 128 });
                Em.AddComponentData(Session, new SkirmishBaseAssaultFactComponent { PlayerDesignatedAlive = 1, EnemyDesignatedAlive = 1 });
                Em.AddComponentObject(Session, new SkirmishResolvedSetupRecord { Setup = Setup });
                Em.AddBuffer<SkirmishProductionReservation>(Session);
                }
                catch { Dispose(); throw; }
            }

            internal Entity Entity() { var entity = Em.CreateEntity(); entities.Add(entity); return entity; }
            internal Entity Producer(string session = "getter-owned", byte faction = 1)
            {
                var entity = Entity();
                Em.AddComponentData(entity, new SkirmishAttemptOwnedComponent { SessionId = session, FactionId = faction, IsStructure = 1 });
                Em.AddComponentData(entity, new SkirmishStructureIdentityComponent { StructureId = SkirmishStructureIds.Barracks,
                    Producer = SkirmishProducerKind.Barracks });
                return entity;
            }
            internal Entity Tanker(string session, int health)
            {
                var actor = Entity();
                Em.AddComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = session, FactionId = 1 });
                Em.AddComponentData(actor, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Tanker,
                    Category = SkirmishPopulationCategory.LogisticsSupport });
                Em.AddComponentData(actor, new UnitHealth { Current = health, Max = 10 });
                return actor;
            }
            private object Component<T>(Entity entity) where T : unmanaged, IComponentData =>
                Em.HasComponent<T>(entity) ? (object)Em.GetComponentData<T>(entity) : typeof(T).FullName + ":absent";
            private List<object> Snapshot()
            {
                // Box unmanaged value copies; Unity JSON need not serialize IComponentData structs.
                var value = new List<object> { JsonUtility.ToJson(Setup), JsonUtility.ToJson(Army) };
                using var all = Em.CreateEntityQuery(new EntityQueryDesc {
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab });
                value.Add(all.CalculateEntityCount());
                foreach (var entity in entities)
                {
                    value.Add(entity); value.Add(Em.Exists(entity));
                    if (!Em.Exists(entity)) continue;
                    using var types = Em.GetComponentTypes(entity, Allocator.Temp);
                    value.Add(types.Length);
                    foreach (var type in types) value.Add(type.GetManagedType().AssemblyQualifiedName);
                    value.Add(Component<SkirmishExpandedSessionComponent>(entity));
                    value.Add(Component<SkirmishEconomyStockComponent>(entity));
                    value.Add(Component<SkirmishEnemyStockComponent>(entity));
                    value.Add(Component<SkirmishCapacityComponent>(entity));
                    value.Add(Component<SkirmishBaseAssaultFactComponent>(entity));
                    value.Add(Component<SkirmishResearchStateComponent>(entity));
                    value.Add(Component<SkirmishResultComponent>(entity));
                    value.Add(Component<SkirmishAttemptOwnedComponent>(entity));
                    value.Add(Component<SkirmishUnitRoleComponent>(entity));
                    value.Add(Component<UnitHealth>(entity));
                    value.Add(Component<SkirmishStructureIdentityComponent>(entity));
                    value.Add(Component<FactionEconomy>(entity));
                    value.Add(Component<FactionTacticalMaterialsComponent>(entity));
                    if (Em.HasBuffer<SkirmishProductionReservation>(entity))
                        foreach (var row in Em.GetBuffer<SkirmishProductionReservation>(entity, true))
                            value.Add(row);
                    if (Em.HasBuffer<BuildingConfiguredSpawnableReadModel>(entity))
                        foreach (var row in Em.GetBuffer<BuildingConfiguredSpawnableReadModel>(entity, true))
                            value.Add(row);
                }
                return value;
            }
            internal T Read<T>(Func<T> getter)
            {
                var before = Snapshot(); var defaultWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                try { return getter(); }
                finally {
                    CollectionAssert.AreEqual(before, Snapshot(), "Getter changed fixture values, entity membership or reservation/catalog rows.");
                    Assert.AreSame(defaultWorld, Unity.Entities.World.DefaultGameObjectInjectionWorld, "Getter replaced the application's default World.");
                }
            }
            internal SkirmishAriaPublicView Project() => Read(() => SkirmishAriaPublicProjection.FromSession(Em, Session, Army));
            public void Dispose() { try { if (World.IsCreated) World.Dispose(); } finally { if (Army != null) UnityEngine.Object.DestroyImmediate(Army); } }
        }

        [Test]
        public void OwnedArmyProfilesExposeTheirActualAllowedAndExcludedRoles()
        {
            var army = ScriptableObject.CreateInstance<SkirmishArmyProfileConfig>();
            try {
                army.ConfigureGroundManeuver();
                Assert.AreEqual("G", army.ProfileId); Assert.AreEqual(SkirmishArmyProfileId.GroundManeuver, army.Kind);
                Assert.AreEqual(1, army.ContentVersion);
                Assert.IsTrue(army.Allows(SkirmishRoleIds.Tank)); Assert.IsFalse(army.Allows(SkirmishRoleIds.AntiAir));
                Assert.IsFalse(army.AllowsOffensiveAir); Assert.IsFalse(army.AllowsAdvancedAir);
                Assert.IsFalse(army.Allows(null)); Assert.IsFalse(army.Allows("unknown.role"));
                army.ConfigureAirMobile();
                Assert.AreEqual("A", army.ProfileId); Assert.AreEqual(SkirmishArmyProfileId.AirMobile, army.Kind);
                Assert.IsFalse(army.Allows(SkirmishRoleIds.Tank)); Assert.IsTrue(army.Allows(SkirmishRoleIds.AntiAir));
                Assert.IsTrue(army.AllowsOffensiveAir); Assert.IsTrue(army.AllowsAdvancedAir);
                army.ConfigureCombinedArms();
                Assert.AreEqual("C", army.ProfileId); Assert.AreEqual(SkirmishArmyProfileId.CombinedArms, army.Kind);
                Assert.IsTrue(army.Allows(SkirmishRoleIds.Tank)); Assert.IsTrue(army.Allows(SkirmishRoleIds.Fighter));
                Assert.IsTrue(army.AllowsOffensiveAir); Assert.IsTrue(army.AllowsAdvancedAir);
            } finally { UnityEngine.Object.DestroyImmediate(army); }
        }

        [Test]
        public void UnknownCachedProfileIdsRefuseWithoutTouchingSharedSlots()
        {
            var fields = new[] { "cachedGround", "cachedAir", "cachedCombined" };
            var before = new object[fields.Length];
            for (int i = 0; i < fields.Length; i++) {
                var field = typeof(SkirmishArmyProfileConfig).GetField(fields[i], BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(field); before[i] = field.GetValue(null);
            }
            Assert.IsNull(SkirmishArmyProfileConfig.ResolveCached((SkirmishArmyProfileId)255));
            Assert.IsNull(SkirmishArmyProfileConfig.ResolveCached((SkirmishArmyProfileId)254));
            for (int i = 0; i < fields.Length; i++)
                Assert.AreSame(before[i], typeof(SkirmishArmyProfileConfig).GetField(fields[i], BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
            // Valid-ID lazy allocation is deliberately not exercised: it owns shared static ScriptableObjects.
        }

        [Test]
        public void ProjectionReadsOwnFactsAndFinishedStateWithoutEnemyWalletLeak()
        {
            using var f = new Fixture();
            var view = f.Project(); Assert.AreEqual(37, view.OwnMaterials); Assert.IsTrue(view.Playing); Assert.IsFalse(view.Finished);
            f.Em.SetComponentData(f.Session, new SkirmishBaseAssaultFactComponent { PlayerDesignatedAlive = 0, EnemyDesignatedAlive = 1 });
            f.Em.AddComponentData(f.Session, new SkirmishResultComponent());
            view = f.Project(); Assert.IsFalse(view.Playing); Assert.IsTrue(view.Finished);
            Assert.IsFalse(view.PlayerDesignatedAlive); Assert.IsTrue(view.EnemyDesignatedAlive); Assert.AreEqual(37, view.OwnMaterials);
        }

        [Test]
        public void RifleAffordabilityUsesExactFixturePriceWithoutSpending()
        {
            using var f = new Fixture();
            Assert.IsTrue(f.Project().CanAffordRifle);
            var stock = f.Em.GetComponentData<SkirmishEconomyStockComponent>(f.Session); stock.Materials = 36;
            f.Em.SetComponentData(f.Session, stock); Assert.IsFalse(f.Project().CanAffordRifle);
            stock.Materials = 37; f.Em.SetComponentData(f.Session, stock); Assert.IsTrue(f.Project().CanAffordRifle);
            Assert.IsTrue(f.Project().CanAffordRifle); // Repeated quotation cannot consume stock/capacity.
        }

        [Test]
        public void SharedMaterialsBankOverridesLegacyAndEnemyBalances()
        {
            using var f = new Fixture();
            var bank = f.Entity(); f.Em.AddComponentData(bank, new FactionEconomy { FactionId = 1 });
            f.Em.AddComponentData(bank, new FactionTacticalMaterialsComponent { FactionId = 1, Current = 19, Capacity = 100, Version = 7 });
            f.Em.AddComponent<SkirmishSharedMaterialsInitialized>(f.Session);
            Assert.AreEqual(19, f.Project().OwnMaterials);
            f.Em.DestroyEntity(bank); Assert.AreEqual(0, f.Project().OwnMaterials);
            // Raw projection still requires its legacy stock-component presence before calling the shared reader.
            f.Em.RemoveComponent<SkirmishEconomyStockComponent>(f.Session); Assert.AreEqual(0, f.Project().OwnMaterials);
        }

        [Test]
        public void QueueQuoteRefusesMissingDestroyedAndNullSetupSubjects()
        {
            using var f = new Fixture();
            var missing = f.Entity();
            SkirmishProductionDecision Read(Entity e) => f.Read(() => SkirmishProductionService.EvaluateQueue(f.Em, e, SkirmishRoleIds.Rifle, 1, f.Army, 1));
            var decision = Read(missing); Assert.IsFalse(decision.Accepted); Assert.AreEqual(SkirmishReasonCode.MissingResolvedSetup, decision.Reason);
            f.Em.DestroyEntity(missing); Assert.IsFalse(f.Em.Exists(missing));
            decision = Read(missing); Assert.IsFalse(decision.Accepted); Assert.AreEqual(SkirmishReasonCode.MissingResolvedSetup, decision.Reason);
            decision = Read(Unity.Entities.Entity.Null); Assert.IsFalse(decision.Accepted); Assert.AreEqual(SkirmishReasonCode.MissingResolvedSetup, decision.Reason);
            var noSetup = f.Entity(); f.Em.AddComponentObject(noSetup, new SkirmishResolvedSetupRecord());
            decision = Read(noSetup); Assert.IsFalse(decision.Accepted); Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, decision.Reason);
            // FromSession itself has no missing/stale-session refusal guard; this is the actual quote API's contract only.
        }

        [Test]
        public void QueueQuoteRequiresActualOwnedProducerAndDoesNotReserveOrPay()
        {
            using var f = new Fixture();
            SkirmishProductionDecision Read() => f.Read(() => SkirmishProductionService.EvaluateQueue(f.Em, f.Session, SkirmishRoleIds.Rifle, 1, f.Army, 1));
            var decision = Read(); Assert.IsFalse(decision.Accepted); Assert.AreEqual(SkirmishReasonCode.MissingProducer, decision.Reason);
            f.Producer("another-owned-fixture"); f.Producer("getter-owned", 2);
            decision = Read(); Assert.IsFalse(decision.Accepted); Assert.AreEqual(SkirmishReasonCode.MissingProducer, decision.Reason);
            f.Producer(); decision = Read(); Assert.IsTrue(decision.Accepted); Assert.AreEqual(SkirmishReasonCode.None, decision.Reason);
            Assert.AreEqual(37, decision.MaterialsCost); Assert.AreEqual(SkirmishRoleIds.InfantrySquadMembers, decision.MemberCount);
            Assert.AreEqual(0, f.Em.GetBuffer<SkirmishProductionReservation>(f.Session, true).Length);
            Assert.IsTrue(Read().Accepted);
        }

        [Test]
        public void ReadinessAndPadAffordabilityUseLiveStateAndUniqueAuthoredReadModel()
        {
            using var f = new Fixture(); f.Army.ConfigureAirMobile(); f.Setup.ArmyProfileId = SkirmishArmyProfileId.AirMobile;
            Assert.IsFalse(f.Project().ReadinessEligible);
            f.Em.AddComponentData(f.Session, new SkirmishResearchStateComponent { Readiness = SkirmishReadinessStage.Established });
            Assert.IsTrue(f.Project().ReadinessEligible); Assert.IsFalse(f.Project().CanBuildAirPad);
            var catalog = f.Entity(); f.Em.AddComponent<BuildingRuntimeStateTag>(catalog);
            var rows = f.Em.AddBuffer<BuildingConfiguredSpawnableReadModel>(catalog);
            var row = new BuildingConfiguredSpawnableReadModel { BuildingId = "Building_Helipad", MaterialsCost = 38, CanRequest = 1 };
            rows.Add(row); Assert.IsFalse(f.Project().CanBuildAirPad);
            row.MaterialsCost = 37; rows[0] = row; Assert.IsTrue(f.Project().CanBuildAirPad);
            row.CanRequest = 0; rows[0] = row; Assert.IsFalse(f.Project().CanBuildAirPad);
            row.CanRequest = 1; rows[0] = row;
            var duplicate = f.Entity(); f.Em.AddComponent<BuildingRuntimeStateTag>(duplicate); f.Em.AddBuffer<BuildingConfiguredSpawnableReadModel>(duplicate).Add(row);
            Assert.IsFalse(f.Project().CanBuildAirPad);
            f.Em.DestroyEntity(duplicate); f.Setup.Readiness = SkirmishReadinessStage.Established;
            f.Em.SetComponentData(f.Session, new SkirmishResearchStateComponent { Readiness = SkirmishReadinessStage.Field });
            Assert.IsTrue(f.Project().ReadinessEligible, "A lower live stage must not downgrade compiled readiness.");
        }

        [Test]
        public void TankerCommitmentsFilterOtherSessionDeadAndEnemyReservations()
        {
            using var f = new Fixture(); f.Tanker("getter-owned", 10); f.Tanker("another-owned-fixture", 10); f.Tanker("getter-owned", 0);
            var reservations = f.Em.GetBuffer<SkirmishProductionReservation>(f.Session);
            reservations.Add(new SkirmishProductionReservation { Role = SkirmishRoleKind.Tanker, FactionId = 1,
                MemberCount = 3, DeliveredMembers = 1, Phase = SkirmishReservationPhase.Producing });
            reservations.Add(new SkirmishProductionReservation { Role = SkirmishRoleKind.Tanker, FactionId = 2,
                MemberCount = 8, Phase = SkirmishReservationPhase.Reserved });
            reservations.Add(new SkirmishProductionReservation { Role = SkirmishRoleKind.Tanker, FactionId = 1,
                MemberCount = 8, Phase = SkirmishReservationPhase.Released });
            Assert.AreEqual(3, f.Project().TankersCommitted);
            Assert.IsFalse(f.Project().LogisticsTruckCommitted);
        }

        public static void RunFocusedValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Owned getter controls require stopped stable Editor.");
            var suite = new SkirmishPublicObservationGetterControls();
            suite.OwnedArmyProfilesExposeTheirActualAllowedAndExcludedRoles();
            suite.UnknownCachedProfileIdsRefuseWithoutTouchingSharedSlots();
            suite.ProjectionReadsOwnFactsAndFinishedStateWithoutEnemyWalletLeak();
            suite.RifleAffordabilityUsesExactFixturePriceWithoutSpending();
            suite.SharedMaterialsBankOverridesLegacyAndEnemyBalances();
            suite.QueueQuoteRefusesMissingDestroyedAndNullSetupSubjects();
            suite.QueueQuoteRequiresActualOwnedProducerAndDoesNotReserveOrPay();
            suite.ReadinessAndPadAffordabilityUseLiveStateAndUniqueAuthoredReadModel();
            suite.TankerCommitmentsFilterOtherSessionDeadAndEnemyReservations();
            Debug.Log("[SkirmishPublicObservationGetterControls] result=Passed methods=9 ownedFixturesOnly=1 liveApiAdmitted=0 missionReady=0");
        }
    }
}
