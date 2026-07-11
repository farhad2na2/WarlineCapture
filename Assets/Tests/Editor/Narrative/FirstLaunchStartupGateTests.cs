using System;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class FirstLaunchStartupGateTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            FirstLaunchStartupGateTests tests = new();
            tests.PendingAndFirstLaunchDisposition_KeepShellClosed(UiShellStartupDisposition.Pending);
            tests.PendingAndFirstLaunchDisposition_KeepShellClosed(UiShellStartupDisposition.FirstLaunch);
            tests.EnterMenuDisposition_UsesExistingMenuTransition();
            tests.EnterMatchRequest_FromClosedFirstLaunchGateUsesLoadingTransition();
            Debug.Log("[FirstLaunchStartupGateValidation] result=Passed tests=4");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[FirstLaunchStartupGateValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [TestCase(UiShellStartupDisposition.Pending)]
    [TestCase(UiShellStartupDisposition.FirstLaunch)]
    public void PendingAndFirstLaunchDisposition_KeepShellClosed(UiShellStartupDisposition disposition)
    {
        using World world = new("FirstLaunchStartupGate");
        Entity boundary = CreateBoundary(world, disposition);
        world.CreateSystem<UiShellFlowSystem>().Update(world.Unmanaged);
        UiShellStateComponent state = world.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
        Assert.AreEqual(UiShellMode.None, state.CurrentMode);
        Assert.AreEqual(0, world.EntityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary).Length);
    }

    [Test]
    public void EnterMenuDisposition_UsesExistingMenuTransition()
    {
        using World world = new("FirstLaunchEnterMenu");
        Entity boundary = CreateBoundary(world, UiShellStartupDisposition.EnterMenu);
        world.CreateSystem<UiShellFlowSystem>().Update(world.Unmanaged);
        UiShellStateComponent state = world.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
        Assert.AreEqual(UiShellMode.MainMenu, state.CurrentMode);
        Assert.AreEqual(UIRoute.MainMenu, state.ActiveRoute);
        Assert.Greater(world.EntityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary).Length, 0);
    }

    [Test]
    public void EnterMatchRequest_FromClosedFirstLaunchGateUsesLoadingTransition()
    {
        using World world = new("FirstLaunchEnterMatch");
        Entity boundary = CreateBoundary(world, UiShellStartupDisposition.FirstLaunch);
        world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
        {
            Intent = UiShellRouteIntent.EnterMatch,
            Route = UIRoute.Match
        });
        world.CreateSystem<UiShellFlowSystem>().Update(world.Unmanaged);
        UiShellStateComponent state = world.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
        Assert.AreEqual(UiShellMode.Loading, state.CurrentMode);
        Assert.AreEqual(UIRoute.Match, state.ActiveRoute);
        Assert.AreEqual(0, world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Length);
        Assert.GreaterOrEqual(world.EntityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary).Length, 2);
    }

    private static Entity CreateBoundary(World world, UiShellStartupDisposition disposition)
    {
        EntityManager manager = world.EntityManager;
        Entity boundary = manager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiShellStartupDispositionComponent),
            typeof(UiShellLoadingProgressComponent),
            typeof(MatchIntroTransitionComponent),
            typeof(UiShellActivePopupComponent));
        manager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.None,
            ActiveRoute = UIRoute.Splash,
            Phase = UiShellTransitionPhase.Idle
        });
        manager.SetComponentData(boundary, new UiShellStartupDispositionComponent { Value = disposition });
        manager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        manager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        manager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
        manager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        manager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
        manager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);
        return boundary;
    }
}
