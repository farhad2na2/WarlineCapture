using System;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Game.Components;
using Game.Runtime;

namespace Game.Editor
{
    public static class MatchStartRequestValidationRunner
    {
        public static void Run()
        {
            try
            {
                var system = new MatchStartRequestStartupSystemHelper();
                ValidateWorld(system, "FirstWorld");
                ValidateWorld(system, "SecondWorld");

                Debug.Log("[MatchStartRequestValidation] result=Passed tests=2");
                Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("[MatchStartRequestValidation] result=Failed");
                Exit(1);
            }
        }

        private static void ValidateWorld(MatchStartRequestStartupSystemHelper system, string worldName)
        {
            using World world = new($"MatchStartRequestValidationRunner-{worldName}");
            EntityManager em = world.EntityManager;

            Require(system.QueueStartAfterMatchLoaded(em), $"{worldName}: initial match start request was not queued.");

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MatchStartStateComponent>());
            Require(query.CalculateEntityCount() == 1, $"{worldName}: expected one match start boundary entity.");

            Entity boundary = query.GetSingletonEntity();
            Require(em.HasComponent<MatchStartQueueComponent>(boundary), $"{worldName}: boundary is missing queue component.");
            Require(em.HasBuffer<MatchStartRequestElement>(boundary), $"{worldName}: boundary is missing request buffer.");
            Require(em.HasBuffer<MatchStartResultElement>(boundary), $"{worldName}: boundary is missing result buffer.");
            Require(em.HasComponent<MatchStartProgressComponent>(boundary), $"{worldName}: boundary is missing progress component.");

            MatchStartQueueComponent queue = em.GetComponentData<MatchStartQueueComponent>(boundary);
            DynamicBuffer<MatchStartRequestElement> requests = em.GetBuffer<MatchStartRequestElement>(boundary);
            Require(queue.LastRequestId == 1, $"{worldName}: initial request id was not recorded.");
            Require(requests.Length == 1, $"{worldName}: initial request buffer length was not one.");
            Require(requests[0].RequestId == 1, $"{worldName}: initial request id does not match queue id.");
            Require(requests[0].RequireMatchLoaded == 1, $"{worldName}: initial request did not require match loaded.");

            Require(system.QueueStartAfterMatchLoaded(em), $"{worldName}: idempotent match start request returned false.");
            queue = em.GetComponentData<MatchStartQueueComponent>(boundary);
            requests = em.GetBuffer<MatchStartRequestElement>(boundary);
            Require(queue.LastRequestId == 1, $"{worldName}: idempotent request unexpectedly advanced request id.");
            Require(requests.Length == 1, $"{worldName}: idempotent request unexpectedly appended another request.");
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
