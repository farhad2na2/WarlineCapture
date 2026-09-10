using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;

public sealed class CampaignReturnFlowTests
{
    [TestCase(UIRoute.Campaign, UIRoute.Campaign)]
    [TestCase(UIRoute.MissionBriefing, UIRoute.Campaign)]
    [TestCase(UIRoute.QuickCustomSetup, UIRoute.QuickCustomSetup)]
    [TestCase(UIRoute.Operations, UIRoute.Operations)]
    [TestCase(UIRoute.DistrictDetail, UIRoute.Operations)]
    [TestCase(UIRoute.MainMenu, UIRoute.MainMenu)]
    public void MatchReturnPreservesGameModeThroughBothLoadingTransitions(UIRoute origin, UIRoute expected)
    {
        using var world = new World("Game mode return");
        world.CreateSystem<UiShellStateSystem>();
        var em = world.EntityManager;
        using var roots = em.CreateEntityQuery(typeof(UiShellRootComponent)); var root = roots.GetSingletonEntity();
        em.SetComponentData(root, new UiShellStateComponent { CurrentMode = UiShellMode.MainMenu, ActiveRoute = origin, Phase = UiShellTransitionPhase.MenuReady });
        var flow = world.CreateSystem<UiShellFlowSystem>();
        void Route(UiShellRouteIntent intent, UIRoute route)
        { em.GetBuffer<UiShellRouteRequestComponent>(root).Add(new UiShellRouteRequestComponent {Intent=intent,Route=route}); flow.Update(world.Unmanaged); }
        void Complete(UiShellCommandKind kind, bool loaded)
        {
            var state = em.GetComponentData<UiShellStateComponent>(root);
            em.GetBuffer<UiShellTransitionCompleteComponent>(root).Add(new UiShellTransitionCompleteComponent { Kind=kind, SequenceId=state.TransitionSequenceId });
            if(loaded) em.SetComponentData(root, new UiShellLoadingProgressComponent {IsComplete=1,Progress01=1});
            flow.Update(world.Unmanaged);
        }
        Route(UiShellRouteIntent.EnterMatch, UIRoute.Match);
        Assert.AreEqual(expected, em.GetComponentData<UiShellStateComponent>(root).MatchReturnRoute);
        Complete(UiShellCommandKind.ShowLoading, true); Complete(UiShellCommandKind.EnterMatchHud, false);
        Assert.AreEqual(UiShellMode.MatchHud, em.GetComponentData<UiShellStateComponent>(root).CurrentMode);
        Route(UiShellRouteIntent.EnterMatch, UIRoute.Match);
        Assert.AreEqual(expected, em.GetComponentData<UiShellStateComponent>(root).MatchReturnRoute, "Retry must retain the original game mode.");
        Complete(UiShellCommandKind.ShowLoading, true); Complete(UiShellCommandKind.EnterMatchHud, false);
        Route(UiShellRouteIntent.ReturnToMainMenu, UIRoute.MainMenu);
        Assert.AreEqual(expected, em.GetComponentData<UiShellStateComponent>(root).ActiveRoute);
        Complete(UiShellCommandKind.ShowLoading, true);
        var final = em.GetComponentData<UiShellStateComponent>(root);
        Assert.AreEqual(expected, final.ActiveRoute); Assert.AreEqual(UiShellMode.MainMenu, final.CurrentMode);
        var commands = em.GetBuffer<UiShellPresentationCommandComponent>(root);
        Assert.AreEqual(UiShellCommandKind.EnterMenu, commands[commands.Length-1].Kind);
        Assert.AreEqual(expected, commands[commands.Length-1].Route);
    }
}
