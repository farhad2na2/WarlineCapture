using System;
using Game.Composition;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class MatchLaunchRouteValidationRunner
    {
        public static void Run()
        {
            try
            {
                ValidateAuthoritativeRouteAndIdempotence();
                ValidateLaunchKeepsShellPresentationAlive();
                ValidateMissingAndAmbiguousBoundariesFailClosed();
                Debug.Log("[MatchLaunchRouteValidation] result=Passed tests=6");
                Debug.Log("Application will terminate with return code 0");
                Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[MatchLaunchRouteValidation] result=Failed");
                Exit(1);
            }
        }

        private static void ValidateLaunchKeepsShellPresentationAlive()
        {
            World previousWorld = World.DefaultGameObjectInjectionWorld;
            using World world = new("MatchLaunchRouteValidation-PresentationLifetime");
            GameObject shellRoot = new("MatchLaunchRouteValidation-ShellRoot");
            GameObject launchSource = new("MatchLaunchRouteValidation-LaunchSource");
            try
            {
                World.DefaultGameObjectInjectionWorld = world;
                Entity boundary = CreateShellBoundary(world.EntityManager);
                UiShellStateComponent shellState = world.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
                shellState.IsTransitionRunning = 1;
                world.EntityManager.SetComponentData(boundary, shellState);

                shellRoot.AddComponent<UIRouterView>();
                launchSource.transform.SetParent(shellRoot.transform, false);

                var command = new MatchLaunchCommand(new QuickCustomGameConfigStore());
                command.LaunchMatch(launchSource.transform);

                Require(shellRoot.activeSelf,
                    "Match launch disabled the shell presentation before the authoritative route could be consumed.");
                DynamicBuffer<UiShellRouteRequestComponent> requests =
                    world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
                Require(requests.Length == 1 && requests[0].Intent == UiShellRouteIntent.EnterMatch,
                    "Match launch did not retain its authoritative route while a menu transition was completing.");
            }
            finally
            {
                World.DefaultGameObjectInjectionWorld = previousWorld;
                UnityEngine.Object.DestroyImmediate(launchSource);
                UnityEngine.Object.DestroyImmediate(shellRoot);
            }
        }

        private static void ValidateAuthoritativeRouteAndIdempotence()
        {
            using World world = new("MatchLaunchRouteValidation-Authoritative");
            EntityManager entityManager = world.EntityManager;
            Entity boundary = CreateShellBoundary(entityManager);
            var command = new MatchLaunchCommand(new QuickCustomGameConfigStore());

            Require(command.QueueMatchRoute(entityManager), "Initial Match route request was rejected.");
            DynamicBuffer<UiShellRouteRequestComponent> requests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            Require(requests.Length == 1, "Initial Match route request count was not one.");
            Require(requests[0].Intent == UiShellRouteIntent.EnterMatch, "Initial route intent was not EnterMatch.");
            Require(requests[0].Route == UIRoute.Match, "Initial route target was not Match.");
            Require(requests[0].PushHistory == 0, "Initial Match route unexpectedly retained menu history.");

            Require(command.QueueMatchRoute(entityManager), "Idempotent Match route request was rejected.");
            Require(requests.Length == 1, "Idempotent Match route request appended a duplicate.");

            using EntityQuery matchStartQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MatchStartStateComponent>());
            Require(matchStartQuery.IsEmptyIgnoreFilter,
                "Launch command bypassed the shell owner by creating a Match-start boundary.");

            UiShellStateComponent shellState = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            shellState.ActiveRoute = UIRoute.Match;
            entityManager.SetComponentData(boundary, shellState);
            requests.Clear();
            Require(command.QueueMatchRoute(entityManager), "Already-active Match route was rejected.");
            Require(requests.Length == 0, "Already-active Match route appended a redundant request.");
        }

        private static void ValidateMissingAndAmbiguousBoundariesFailClosed()
        {
            var command = new MatchLaunchCommand(new QuickCustomGameConfigStore());
            using (World missingWorld = new("MatchLaunchRouteValidation-Missing"))
            {
                Require(!command.QueueMatchRoute(missingWorld.EntityManager),
                    "Missing UI-shell boundary did not fail closed.");
            }

            using World ambiguousWorld = new("MatchLaunchRouteValidation-Ambiguous");
            CreateShellBoundary(ambiguousWorld.EntityManager);
            CreateShellBoundary(ambiguousWorld.EntityManager);
            Require(!command.QueueMatchRoute(ambiguousWorld.EntityManager),
                "Ambiguous UI-shell boundaries did not fail closed.");
        }

        private static Entity CreateShellBoundary(EntityManager entityManager)
        {
            Entity boundary = entityManager.CreateEntity(
                typeof(UiShellRootComponent),
                typeof(UiShellStateComponent));
            entityManager.SetComponentData(boundary, new UiShellStateComponent
            {
                CurrentMode = UiShellMode.MainMenu,
                ActiveRoute = UIRoute.QuickCustomSetup,
                Phase = UiShellTransitionPhase.MenuReady
            });
            entityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
            return boundary;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
}
