using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Runtime;

public sealed class UnitTransportBoardingCapacityRulesTests
{
    public static void RunArchitectureCapacityRulesValidation()
    {
        try
        {
            RunTest(test => test.NormalizePassengerKind_MapsOnlyVehicleToVehicle());
            RunTest(test => test.ResolveCapacity_UsesCargoVehicleSlotsAndClampsNegativeValues());
            RunTest(test => test.ResolveCapacity_UsesPositiveCargoSoldierOverrideOtherwiseLegacyCapacity());
            RunTest(test => test.CountsTowardOccupancy_RejectsMissingPassenger());
            RunTest(test => test.CountsTowardOccupancy_PrefersMatchingCargoPassengerMetadata());
            RunTest(test => test.CountsTowardOccupancy_UsesMatchingBoardingTargetThenDefaultsToSoldier());
            Debug.Log("[TransportBoardingCapacityRules] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[TransportBoardingCapacityRules] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void NormalizePassengerKind_MapsOnlyVehicleToVehicle()
    {
        Assert.AreEqual(
            UnitTransportPassengerKind.Vehicle,
            UnitTransportBoardingCapacityRules.NormalizePassengerKind(UnitTransportPassengerKind.Vehicle));
        Assert.AreEqual(
            UnitTransportPassengerKind.Soldier,
            UnitTransportBoardingCapacityRules.NormalizePassengerKind(UnitTransportPassengerKind.Soldier));
        Assert.AreEqual(
            UnitTransportPassengerKind.Soldier,
            UnitTransportBoardingCapacityRules.NormalizePassengerKind(99));
    }

    [Test]
    public void ResolveCapacity_UsesCargoVehicleSlotsAndClampsNegativeValues()
    {
        UnitTransportCapacity legacyCapacity = new() { SoldierCapacity = 8 };

        Assert.AreEqual(
            0,
            UnitTransportBoardingCapacityRules.ResolveCapacity(
                legacyCapacity,
                false,
                default,
                UnitTransportPassengerKind.Vehicle));
        Assert.AreEqual(
            0,
            UnitTransportBoardingCapacityRules.ResolveCapacity(
                legacyCapacity,
                true,
                new UnitTransportCargoCapacity { VehicleCapacity = -2 },
                UnitTransportPassengerKind.Vehicle));
        Assert.AreEqual(
            3,
            UnitTransportBoardingCapacityRules.ResolveCapacity(
                legacyCapacity,
                true,
                new UnitTransportCargoCapacity { VehicleCapacity = 3 },
                UnitTransportPassengerKind.Vehicle));
    }

    [Test]
    public void ResolveCapacity_UsesPositiveCargoSoldierOverrideOtherwiseLegacyCapacity()
    {
        UnitTransportCapacity legacyCapacity = new() { SoldierCapacity = 8 };

        Assert.AreEqual(
            8,
            UnitTransportBoardingCapacityRules.ResolveCapacity(
                legacyCapacity,
                false,
                default,
                UnitTransportPassengerKind.Soldier));
        Assert.AreEqual(
            8,
            UnitTransportBoardingCapacityRules.ResolveCapacity(
                legacyCapacity,
                true,
                new UnitTransportCargoCapacity { SoldierCapacity = 0 },
                99));
        Assert.AreEqual(
            12,
            UnitTransportBoardingCapacityRules.ResolveCapacity(
                legacyCapacity,
                true,
                new UnitTransportCargoCapacity { SoldierCapacity = 12 },
                UnitTransportPassengerKind.Soldier));
        Assert.AreEqual(
            0,
            UnitTransportBoardingCapacityRules.ResolveCapacity(
                new UnitTransportCapacity { SoldierCapacity = -1 },
                false,
                default,
                UnitTransportPassengerKind.Soldier));
    }

    [Test]
    public void CountsTowardOccupancy_RejectsMissingPassenger()
    {
        Entity transport = new() { Index = 1, Version = 1 };

        Assert.IsFalse(UnitTransportBoardingCapacityRules.CountsTowardOccupancy(
            transport,
            UnitTransportPassengerKind.Soldier,
            false,
            true,
            new UnitTransportCargoPassenger
            {
                Transport = transport,
                PassengerKind = UnitTransportPassengerKind.Soldier
            },
            false,
            default));
    }

    [Test]
    public void CountsTowardOccupancy_PrefersMatchingCargoPassengerMetadata()
    {
        Entity transport = new() { Index = 1, Version = 1 };

        Assert.IsTrue(UnitTransportBoardingCapacityRules.CountsTowardOccupancy(
            transport,
            UnitTransportPassengerKind.Vehicle,
            true,
            true,
            new UnitTransportCargoPassenger
            {
                Transport = transport,
                PassengerKind = UnitTransportPassengerKind.Vehicle
            },
            true,
            new UnitTransportBoardingTarget
            {
                Transport = transport,
                PassengerKind = UnitTransportPassengerKind.Soldier
            }));
    }

    [Test]
    public void CountsTowardOccupancy_UsesMatchingBoardingTargetThenDefaultsToSoldier()
    {
        Entity transport = new() { Index = 1, Version = 1 };
        Entity otherTransport = new() { Index = 2, Version = 1 };
        UnitTransportCargoPassenger mismatchedCargo = new()
        {
            Transport = otherTransport,
            PassengerKind = UnitTransportPassengerKind.Vehicle
        };

        Assert.IsTrue(UnitTransportBoardingCapacityRules.CountsTowardOccupancy(
            transport,
            UnitTransportPassengerKind.Vehicle,
            true,
            true,
            mismatchedCargo,
            true,
            new UnitTransportBoardingTarget
            {
                Transport = transport,
                PassengerKind = UnitTransportPassengerKind.Vehicle
            }));
        Assert.IsTrue(UnitTransportBoardingCapacityRules.CountsTowardOccupancy(
            transport,
            UnitTransportPassengerKind.Soldier,
            true,
            true,
            mismatchedCargo,
            true,
            new UnitTransportBoardingTarget
            {
                Transport = otherTransport,
                PassengerKind = UnitTransportPassengerKind.Vehicle
            }));
    }

    private static void RunTest(Action<UnitTransportBoardingCapacityRulesTests> action)
    {
        action(new UnitTransportBoardingCapacityRulesTests());
    }
}
