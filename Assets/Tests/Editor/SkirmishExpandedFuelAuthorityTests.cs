using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SkirmishExpandedFuelAuthorityTests
{
    public static void RunFocusedAndExit()
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit()) RunFocusedValidation();
        int code = ValidationExit.LastExitCode ?? 2;
        Debug.Log("[SkirmishExpandedFuelAuthorityExit] code=" + code);
        UnityEditor.EditorApplication.Exit(code);
    }

    public static void RunFocusedValidation()
    {
        try
        {
            var failures = new List<string>();
            int cases = 0;
            foreach (bool migrated in new[] { false, true })
                foreach (bool matchingAttempt in new[] { false, true })
                    foreach (bool physicalAvailable in new[] { false, true })
                    {
                        string name = $"migrated={migrated} matching={matchingAttempt} physical={physicalAvailable}";
                        try { VerifyAuthority(migrated, matchingAttempt, physicalAvailable); }
                        catch (Exception e) { failures.Add(name + ": " + e.Message); }
                        cases++;
                    }
            var legacy = new VehicleFuelConsumptionSystemTests();
            legacy.GroundVehicleFuelHold_ZeroFuelClearsMovementAndStopsKinematics();
            legacy.GroundVehicleFuelHold_WithUsableFuelKeepsMovement();
            Debug.Log($"[SkirmishExpandedFuelAuthority] cases={cases} failed={failures.Count} existingHoldRegressions=2");
            Assert.IsEmpty(failures, string.Join("\n", failures));
            Debug.Log("[SkirmishExpandedFuelAuthority] result=Passed cases=8 existingHoldRegressions=2 missionReady=0");
            ValidationExit.Passed();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("[SkirmishExpandedFuelAuthority] result=Failed missionReady=0");
            ValidationExit.Failed();
        }
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(true, true, true)]
    public void GroundHoldUsesSameFuelAuthorityAsConsumption(bool migrated, bool matchingAttempt, bool physicalAvailable)
        => VerifyAuthority(migrated, matchingAttempt, physicalAvailable);

    static void VerifyAuthority(bool migrated, bool matchingAttempt, bool physicalAvailable)
    {
        using var world = new World("SkirmishExpandedFuelAuthority");
        var em = world.EntityManager;
        var session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupComponent),
            typeof(SkirmishEconomyStockComponent), typeof(SkirmishEnemyStockComponent));
        em.SetComponentData(session, new SkirmishExpandedSessionComponent { SessionId = new FixedString64Bytes("fuel-authority-attempt") });
        em.SetComponentData(session, new SkirmishResolvedSetupComponent { PlayerFaction = 1, EnemyFaction = 2 });
        // Opposite values make an authority mismatch observable in either direction.
        int logicalFuel = physicalAvailable ? 0 : 10;
        em.SetComponentData(session, new SkirmishEconomyStockComponent { Fuel = logicalFuel });
        em.SetComponentData(session, new SkirmishEnemyStockComponent { Fuel = logicalFuel });
        if (migrated) em.AddComponent<SkirmishSharedSupplyInitialized>(session);
        var store = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(store, new BuildingResourceStorageComponent
        { OwnerFactionId = 1, FuelStorageCapacity = 100, StoredFuelBarrels = physicalAvailable ? 10 : 0, Version = 7 });
        var unit = em.CreateEntity(typeof(UnitGrid), typeof(Faction), typeof(UnitMovementBehavior), typeof(UnitFuelConsumption),
            typeof(UnitFuelConsumptionState), typeof(SkirmishAttemptOwnedComponent));
        em.SetComponentData(unit, new UnitGrid { Cell = new int2(3, 4) });
        em.SetComponentData(unit, new Faction { Id = 1 });
        em.SetComponentData(unit, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(unit, new UnitFuelConsumption { Enabled = 1, GroundFuelPerCell = 1 });
        em.SetComponentData(unit, new SkirmishAttemptOwnedComponent
        { SessionId = new FixedString64Bytes(matchingAttempt ? "fuel-authority-attempt" : "other-attempt") });
        em.AddComponentData(unit, new UnitTarget { Cell = new int2(8, 9) });
        em.AddComponentData(unit, new UnitPathRequest { Goal = new int2(8, 9) });
        em.AddComponentData(unit, new UnitPathFollow { PathIndex = 3 });
        em.AddComponentData(unit, new UnitPathRange { Start = 2, Length = 6 });
        em.AddComponentData(unit, new UnitPathRetryCooldown { ResumeFrame = 99 });
        em.AddComponentData(unit, new UnitLongDistanceMove { FinalGoal = new int2(12, 13) });
        em.AddComponent<ManualMoveOrderTag>(unit);
        em.AddComponentData(unit, new UnitVehicleKinematics { CurrentSpeed = 8, StallSeconds = 2 });
        var system = world.CreateSystem<GroundVehicleFuelHoldSystem>();
        system.Update(world.Unmanaged);
        bool usesLogical = !migrated && matchingAttempt;
        bool shouldKeep = usesLogical ? logicalFuel > 0 : physicalAvailable;
        foreach (var type in new[] { typeof(UnitTarget), typeof(UnitPathRequest), typeof(UnitPathFollow), typeof(UnitPathRange),
                     typeof(UnitPathRetryCooldown), typeof(UnitLongDistanceMove), typeof(ManualMoveOrderTag) })
            Assert.AreEqual(shouldKeep, em.HasComponent(unit, ComponentType.ReadOnly(type)), type.Name);
        var motion = em.GetComponentData<UnitVehicleKinematics>(unit);
        Assert.AreEqual(shouldKeep ? 8 : 0, motion.CurrentSpeed);
        Assert.AreEqual(shouldKeep ? 2 : 0, motion.StallSeconds);
        Assert.AreEqual(logicalFuel, em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel, "Hold must not spend logical stock");
        Assert.AreEqual(physicalAvailable ? 10 : 0, em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels, "Hold must not spend physical stock");
        Assert.AreEqual(7, em.GetComponentData<BuildingResourceStorageComponent>(store).Version);
    }
}
