using System;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Game.Components;
using Game.Runtime;

namespace Game.Editor
{
    public static class SceneLifecycleValidationRunner
    {
        public static void Run()
        {
            try
            {
                using World world = new("SceneLifecycleValidationRunner");
                EntityManager em = world.EntityManager;
                var system = new SceneLifecycleSceneSystemHelper();

                Entity boundary = system.EnsureLifecycleEntity(em);
                Require(boundary != Entity.Null && em.Exists(boundary), "Scene lifecycle root was not created.");
                Require(em.HasComponent<SceneLifecycleRootComponent>(boundary), "Root marker is missing.");
                Require(em.HasComponent<SceneLifecycleQueueComponent>(boundary), "Queue component is missing.");
                Require(em.HasComponent<SceneLifecycleStateComponent>(boundary), "State component is missing.");
                Require(em.HasBuffer<SceneLifecycleRequestElement>(boundary), "Request buffer is missing.");
                Require(em.HasBuffer<SceneLifecycleResultElement>(boundary), "Result buffer is missing.");

                Require(system.QueueLoadMatch(em), "Match load request was not queued.");
                SceneLifecycleQueueComponent queue = em.GetComponentData<SceneLifecycleQueueComponent>(boundary);
                DynamicBuffer<SceneLifecycleRequestElement> requests = em.GetBuffer<SceneLifecycleRequestElement>(boundary);
                Require(queue.LastRequestId == 1, "Initial scene request id was not recorded.");
                Require(requests.Length == 1, "Expected one queued scene lifecycle request.");
                Require(requests[0].Kind == SceneLifecycleRequestKind.LoadAdditive, "Queued request is not a load request.");
                Require(requests[0].Scene == SceneLifecycleSceneId.Match, "Queued request is not for the Match scene.");
                Require(requests[0].ActivateOnLoad == 1, "Queued load request does not activate on load.");

                Require(system.QueueLoadMatch(em), "Duplicate match load request returned false.");
                queue = em.GetComponentData<SceneLifecycleQueueComponent>(boundary);
                requests = em.GetBuffer<SceneLifecycleRequestElement>(boundary);
                Require(queue.LastRequestId == 1, "Duplicate load request unexpectedly advanced request id.");
                Require(requests.Length == 1, "Duplicate load request unexpectedly appended another request.");

                Require(system.QueueUnloadMatch(em), "Unloaded duplicate match unload request returned false.");
                queue = em.GetComponentData<SceneLifecycleQueueComponent>(boundary);
                requests = em.GetBuffer<SceneLifecycleRequestElement>(boundary);
                Require(queue.LastRequestId == 1, "Ignored unload request unexpectedly advanced request id.");
                Require(requests.Length == 1, "Ignored unload request unexpectedly appended another request.");

                Debug.Log("[SceneLifecycleValidation] result=Passed tests=1");
                Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("[SceneLifecycleValidation] result=Failed");
                Exit(1);
            }
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
