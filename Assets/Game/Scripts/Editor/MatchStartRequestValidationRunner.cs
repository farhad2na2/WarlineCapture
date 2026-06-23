using System;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public static class MatchStartRequestValidationRunner
{
    public static void Run()
    {
        try
        {
            using World world = new("MatchStartRequestValidationRunner");
            EntityManager em = world.EntityManager;
            var system = new MatchStartRequestStartupSystemHelper();

            Require(system.QueueStartAfterMatchLoaded(em), "Initial match start request was not queued.");

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MatchStartBoundaryComponent>());
            Require(query.CalculateEntityCount() == 1, "Expected one match start boundary entity.");

            Entity boundary = query.GetSingletonEntity();
            Require(em.HasComponent<MatchStartQueueComponent>(boundary), "Boundary is missing queue component.");
            Require(em.HasBuffer<MatchStartRequestElement>(boundary), "Boundary is missing request buffer.");
            Require(em.HasBuffer<MatchStartResultElement>(boundary), "Boundary is missing result buffer.");
            Require(em.HasComponent<MatchStartProgressComponent>(boundary), "Boundary is missing progress component.");

            MatchStartQueueComponent queue = em.GetComponentData<MatchStartQueueComponent>(boundary);
            DynamicBuffer<MatchStartRequestElement> requests = em.GetBuffer<MatchStartRequestElement>(boundary);
            Require(queue.LastRequestId == 1, "Initial request id was not recorded.");
            Require(requests.Length == 1, "Initial request buffer length was not one.");
            Require(requests[0].RequestId == 1, "Initial request id does not match queue id.");
            Require(requests[0].RequireMatchLoaded == 1, "Initial request did not require match loaded.");

            Require(system.QueueStartAfterMatchLoaded(em), "Idempotent match start request returned false.");
            queue = em.GetComponentData<MatchStartQueueComponent>(boundary);
            requests = em.GetBuffer<MatchStartRequestElement>(boundary);
            Require(queue.LastRequestId == 1, "Idempotent request unexpectedly advanced request id.");
            Require(requests.Length == 1, "Idempotent request unexpectedly appended another request.");

            Debug.Log("[MatchStartRequestValidation] result=Passed tests=1");
            Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[MatchStartRequestValidation] result=Failed");
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
