using System;
using Game.Components;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeUiActionRequestSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(AmountStepper_UpdatesSelectedInputAmountFromRecipeStep),
                test => test.AmountStepper_UpdatesSelectedInputAmountFromRecipeStep(),
                ref passed);
            RunValidationStep(
                nameof(Confirm_EnqueuesStartRequestForSelectedRecipeAndAmount),
                test => test.Confirm_EnqueuesStartRequestForSelectedRecipeAndAmount(),
                ref passed);
            RunValidationStep(
                nameof(QueueControls_EnqueueTypedExchangeRequests),
                test => test.QueueControls_EnqueueTypedExchangeRequests(),
                ref passed);

            Debug.Log($"[ResourceExchangeUiActionRequestValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeUiActionRequestValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AmountStepper_UpdatesSelectedInputAmountFromRecipeStep()
    {
        using World world = new(nameof(AmountStepper_UpdatesSelectedInputAmountFromRecipeStep));
        EntityManager em = world.EntityManager;
        Entity boundary = CreateUiBoundary(em, selectedAmount: 100);
        CreateSelectionInput(em);
        CreateResourceExchange(em);

        SystemHandle system = world.CreateSystem<UiActionRequestSystem>();

        EnqueueAction(em, boundary, UiActionKind.ResourceExchangeAmountIncrease);
        system.Update(world.Unmanaged);

        UiResourceExchangeStateComponent state = em.GetComponentData<UiResourceExchangeStateComponent>(boundary);
        Assert.AreEqual(200, state.SelectedInputAmount);

        EnqueueAction(em, boundary, UiActionKind.ResourceExchangeAmountDecrease);
        system.Update(world.Unmanaged);

        state = em.GetComponentData<UiResourceExchangeStateComponent>(boundary);
        Assert.AreEqual(100, state.SelectedInputAmount);
    }

    [Test]
    public void Confirm_EnqueuesStartRequestForSelectedRecipeAndAmount()
    {
        using World world = new(nameof(Confirm_EnqueuesStartRequestForSelectedRecipeAndAmount));
        EntityManager em = world.EntityManager;
        Entity boundary = CreateUiBoundary(em, selectedAmount: 200);
        CreateSelectionInput(em);
        Entity exchange = CreateResourceExchange(em);

        EnqueueAction(em, boundary, UiActionKind.ResourceExchangeConfirm);
        SystemHandle system = world.CreateSystem<UiActionRequestSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<ResourceExchangeRequestComponent> requests =
            em.GetBuffer<ResourceExchangeRequestComponent>(exchange);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(ResourceExchangeRequestKind.Start, requests[0].RequestKind);
        Assert.AreEqual(new FixedString128Bytes("exchange.convert_oil_materials.test"), requests[0].RecipeId);
        Assert.AreEqual(200, requests[0].InputAmount);
        Assert.AreEqual(1, requests[0].FactionId);
    }

    [Test]
    public void QueueControls_EnqueueTypedExchangeRequests()
    {
        using World world = new(nameof(QueueControls_EnqueueTypedExchangeRequests));
        EntityManager em = world.EntityManager;
        Entity boundary = CreateUiBoundary(em, selectedAmount: 100);
        CreateSelectionInput(em);
        Entity exchange = CreateResourceExchange(em);

        DynamicBuffer<UiActionRequestComponent> actions = em.GetBuffer<UiActionRequestComponent>(boundary);
        actions.Add(new UiActionRequestComponent { Kind = UiActionKind.ResourceExchangeQueueRush, PayloadId = 7 });
        actions.Add(new UiActionRequestComponent { Kind = UiActionKind.ResourceExchangeQueueCancel, PayloadId = 8 });
        actions.Add(new UiActionRequestComponent { Kind = UiActionKind.ResourceExchangeRushAll });
        actions.Add(new UiActionRequestComponent { Kind = UiActionKind.ResourceExchangeClearCompleted });

        SystemHandle system = world.CreateSystem<UiActionRequestSystem>();
        system.Update(world.Unmanaged);

        DynamicBuffer<ResourceExchangeRequestComponent> requests =
            em.GetBuffer<ResourceExchangeRequestComponent>(exchange);
        Assert.AreEqual(4, requests.Length);
        Assert.AreEqual(ResourceExchangeRequestKind.Rush, requests[0].RequestKind);
        Assert.AreEqual(7, requests[0].QueueItemId);
        Assert.AreEqual(1, requests[0].RushTickets);
        Assert.AreEqual(ResourceExchangeRequestKind.Cancel, requests[1].RequestKind);
        Assert.AreEqual(8, requests[1].QueueItemId);
        Assert.AreEqual(ResourceExchangeRequestKind.RushAll, requests[2].RequestKind);
        Assert.AreEqual(0, requests[2].RushTickets);
        Assert.AreEqual(ResourceExchangeRequestKind.ClearCompleted, requests[3].RequestKind);
    }

    private static Entity CreateUiBoundary(EntityManager em, int selectedAmount)
    {
        Entity boundary = em.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiDiagnosticsOverlayComponent),
            typeof(UiMatchHudPassengerDrawerStateComponent),
            typeof(UiMatchHudSquadTrayStateComponent),
            typeof(UiBuildDrawerStateComponent),
            typeof(UiResourceExchangeStateComponent));
        em.SetComponentData(boundary, new UiResourceExchangeStateComponent
        {
            ActiveTab = UiResourceExchangeTab.Export,
            SelectedRecipeSlot = 0,
            SelectedInputAmount = selectedAmount
        });
        em.AddBuffer<UiActionRequestComponent>(boundary);
        em.AddBuffer<UiShellPopupRequestComponent>(boundary);
        em.AddBuffer<UiShellRouteRequestComponent>(boundary);
        return boundary;
    }

    private static Entity CreateSelectionInput(EntityManager em)
    {
        Entity entity = em.CreateEntity(
            typeof(RtsSelectionInputStateComponent),
            typeof(RtsSelectionInputRequestQueueComponent));
        em.AddBuffer<RtsSelectionPointerRequestElement>(entity);
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(entity);
        em.AddBuffer<RtsSelectionCommandResultElement>(entity);
        return entity;
    }

    private static Entity CreateResourceExchange(EntityManager em)
    {
        Entity exchange = em.CreateEntity(
            typeof(ResourceExchangeEnabledComponent),
            typeof(ResourceExchangeRequestQueueComponent));
        em.SetComponentData(exchange, new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = 1,
            AllowRush = 1,
            MaxQueueItems = 4
        });
        em.SetComponentData(exchange, new ResourceExchangeRequestQueueComponent());
        em.AddBuffer<ResourceExchangeRecipeComponent>(exchange).Add(ExportOilRecipe());
        em.AddBuffer<ResourceExchangeRequestComponent>(exchange);
        return exchange;
    }

    private static void EnqueueAction(EntityManager em, Entity boundary, UiActionKind kind)
    {
        em.GetBuffer<UiActionRequestComponent>(boundary).Add(new UiActionRequestComponent
        {
            Kind = kind,
            PayloadId = 0
        });
    }

    private static ResourceExchangeRecipeComponent ExportOilRecipe()
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.convert_oil_materials.test"),
            DisplayName = new FixedString128Bytes("Export Oil"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Oil,
            InputAmountMin = 100,
            InputAmountMax = 300,
            InputStep = 100,
            OutputPerInput = 0.5f,
            DurationSecondsBase = 10f,
            Enabled = 1
        };
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeUiActionRequestSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeUiActionRequestSystemTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[ResourceExchangeUiActionRequestValidation] passed {name}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeUiActionRequestValidation] failed {name}\n{exception}");
            throw;
        }
    }
}
#endif
