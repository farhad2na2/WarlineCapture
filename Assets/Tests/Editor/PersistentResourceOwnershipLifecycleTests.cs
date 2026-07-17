using System;
using System.Reflection;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;

public sealed class PersistentResourceOwnershipLifecycleTests
{
    [Test]
    public void RuntimeLogBuffer_SubsystemResetClearsStateAndAllowsReinitialization()
    {
        Type bufferType = typeof(MainMenuPlayUI).Assembly.GetType("Game.UI.Runtime.RuntimeLogBuffer", throwOnError: true);
        MethodInfo reset = bufferType.GetMethod(
            "ResetBeforeSubsystemRegistration",
            BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo initialize = bufferType.GetMethod(
            "InitializeBeforeSceneLoad",
            BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo initialized = bufferType.GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo entries = bufferType.GetField("Entries", BindingFlags.Static | BindingFlags.NonPublic);
        PropertyInfo count = entries.FieldType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);

        Assert.IsNotNull(reset);
        Assert.IsNotNull(initialize);
        Assert.IsNotNull(initialized);
        Assert.IsNotNull(entries);
        Assert.IsNotNull(count);

        try
        {
            reset.Invoke(null, null);
            initialize.Invoke(null, null);
            Assert.IsTrue((bool)initialized.GetValue(null));
            Assert.Greater((int)count.GetValue(entries.GetValue(null)), 0);

            reset.Invoke(null, null);
            Assert.IsFalse((bool)initialized.GetValue(null));
            Assert.AreEqual(0, (int)count.GetValue(entries.GetValue(null)));

            initialize.Invoke(null, null);
            Assert.IsTrue((bool)initialized.GetValue(null));
        }
        finally
        {
            reset.Invoke(null, null);
        }
    }

    [Test]
    public void UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World firstWorld = new(nameof(UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries) + "_First");
        World secondWorld = new(nameof(UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries) + "_Second");
        try
        {
            CreateShellBoundary(firstWorld, UIRoute.MainMenu);
            World.DefaultGameObjectInjectionWorld = firstWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.IsTrue(UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel first));
            Assert.AreEqual(UIRoute.MainMenu, first.ActiveRoute);

            CreateShellBoundary(secondWorld, UIRoute.Match);
            World.DefaultGameObjectInjectionWorld = secondWorld;
            Assert.IsTrue(UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel second));
            Assert.AreEqual(UIRoute.Match, second.ActiveRoute);

            firstWorld.Dispose();
            Assert.IsTrue(UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel afterFirstWorldDisposal));
            Assert.AreEqual(UIRoute.Match, afterFirstWorldDisposal.ActiveRoute);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (secondWorld.IsCreated)
                secondWorld.Dispose();
        }
    }

    private static void CreateShellBoundary(World world, UIRoute route)
    {
        Entity entity = world.EntityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent));
        world.EntityManager.SetComponentData(entity, new UiShellStateComponent
        {
            ActiveRoute = route,
            CurrentMode = route == UIRoute.Match ? UiShellMode.MatchHud : UiShellMode.MainMenu,
            Phase = route == UIRoute.Match ? UiShellTransitionPhase.MatchHudReady : UiShellTransitionPhase.MenuReady
        });
    }
}
