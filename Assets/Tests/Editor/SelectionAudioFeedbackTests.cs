using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionAudioFeedbackTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new SelectionAudioFeedbackTests();
            tests.UnitSelectionAudioEvents_ResolveByUnitType();
            passed++;
            tests.QueueSelection_EnqueuesAirSelectionAudioRequest();
            passed++;
            tests.NonUnitSelection_DoesNotEmitSelectionAudio();
            passed++;

            Debug.Log($"[SelectionAudioFeedbackValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[SelectionAudioFeedbackValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void UnitSelectionAudioEvents_ResolveByUnitType()
    {
        using World world = new("SelectionAudioFeedbackResolveTests");
        EntityManager em = world.EntityManager;

        AssertSelectionAudio(
            em,
            CreateMovableUnit(em, usesVehicleMotion: false, isAir: false),
            AudioEventIds.GameplayUnitSelectInfantry,
            AudioEventIds.GameplayUnitSelectInfantryHash);
        AssertSelectionAudio(
            em,
            CreateMovableUnit(em, usesVehicleMotion: true, isAir: false),
            AudioEventIds.GameplayUnitSelectVehicle,
            AudioEventIds.GameplayUnitSelectVehicleHash);
        AssertSelectionAudio(
            em,
            CreateMovableUnit(em, usesVehicleMotion: true, isAir: true),
            AudioEventIds.GameplayUnitSelectAir,
            AudioEventIds.GameplayUnitSelectAirHash);
    }

    [Test]
    public void QueueSelection_EnqueuesAirSelectionAudioRequest()
    {
        using World world = new("SelectionAudioFeedbackQueueTests");
        EntityManager em = world.EntityManager;
        Entity airUnit = CreateMovableUnit(em, usesVehicleMotion: true, isAir: true);

        var helper = new SelectionHudFeedbackUiSystemHelper();
        helper.QueueSelection(em, airUnit, new SelectionUiReadModelLookup());

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        DynamicBuffer<AudioPlaybackRequestElement> requests = em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.GameplayUnitSelectAir, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.GameplayUnitSelectAirHash, requests[0].EventHash);
        Assert.AreEqual("Gameplay", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.Medium, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
        Assert.AreEqual(airUnit, requests[0].SourceEntity);

        Entity feedbackEntity = helper.EnsureFeedbackQueue(em);
        DynamicBuffer<SelectionHudFeedbackElement> feedback =
            em.GetBuffer<SelectionHudFeedbackElement>(feedbackEntity);
        Assert.AreEqual(1, feedback.Length);
        Assert.AreEqual(SelectionHudFeedbackKind.Selection, feedback[0].Kind);
    }

    [Test]
    public void NonUnitSelection_DoesNotEmitSelectionAudio()
    {
        using World world = new("SelectionAudioFeedbackNonUnitTests");
        EntityManager em = world.EntityManager;
        Entity gridOnlyEntity = em.CreateEntity(typeof(UnitGrid));
        em.SetComponentData(gridOnlyEntity, new UnitGrid { Cell = int2.zero });

        Assert.IsFalse(SelectionHudFeedbackUiSystemHelper.TryEmitSelectionAudio(em, gridOnlyEntity));

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        DynamicBuffer<AudioPlaybackRequestElement> requests = em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(0, requests.Length);
    }

    private static void AssertSelectionAudio(
        EntityManager em,
        Entity entity,
        string expectedEventId,
        uint expectedEventHash)
    {
        Assert.IsTrue(SelectionHudFeedbackUiSystemHelper.TryResolveSelectionAudioEvent(
            em,
            entity,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }

    private static Entity CreateMovableUnit(EntityManager em, bool usesVehicleMotion, bool isAir)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior));
        em.SetComponentData(entity, new UnitGrid { Cell = int2.zero });
        em.SetComponentData(entity, new UnitMove
        {
            Speed = usesVehicleMotion ? 8f : 3f,
            WalkSpeed = usesVehicleMotion ? 8f : 3f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.SetComponentData(entity, new UnitFootprint
        {
            Size = usesVehicleMotion ? new int2(2, 2) : new int2(1, 1)
        });
        em.SetComponentData(entity, new UnitMovementBehavior
        {
            UsesVehicleMotion = (byte)(usesVehicleMotion ? 1 : 0)
        });

        if (isAir)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 12f,
                RunwayTaxiSpeed = 5f
            });
        }

        return entity;
    }
}
