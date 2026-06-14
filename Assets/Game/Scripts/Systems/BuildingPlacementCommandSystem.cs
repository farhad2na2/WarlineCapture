using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingPlacementCommandSystem
{
    internal readonly struct Context
    {
        public readonly BuildingPlacementStartupSystem StartupSystem;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingPlacementSessionSystem SessionSystem;
        public readonly BuildingPlacementSessionSystem.Context SessionContext;
        public readonly Action<string> LogWarning;

        public Context(
            BuildingPlacementStartupSystem startupSystem,
            BuildingDefinitionSystem definitionSystem,
            BuildingPlacementSessionSystem sessionSystem,
            BuildingPlacementSessionSystem.Context sessionContext,
            Action<string> logWarning)
        {
            StartupSystem = startupSystem;
            DefinitionSystem = definitionSystem;
            SessionSystem = sessionSystem;
            SessionContext = sessionContext;
            LogWarning = logWarning;
        }
    }

    public void BeginSoldierBasePlacement(Context context)
    {
        BeginConfiguredPlacement(
            context,
            context.StartupSystem.SoldierBaseDefinition,
            "BuildingPlacementCommandSystem is missing the Soldier Base spawnable prefab reference.");
    }

    public void BeginSoldierTentPlacement(Context context)
    {
        BeginConfiguredPlacement(
            context,
            context.StartupSystem.SoldierTentDefinition,
            "BuildingPlacementCommandSystem is missing the Soldier Tent spawnable prefab reference.");
    }

    public void BeginFactoryPlacement(Context context)
    {
        BeginConfiguredPlacement(
            context,
            context.StartupSystem.FactoryDefinition,
            "BuildingPlacementCommandSystem is missing the Factory spawnable prefab reference.");
    }

    public bool BeginPlacementForConfiguredSpawnable(Context context, GameObject prefab)
    {
        if (context.DefinitionSystem == null ||
            !context.DefinitionSystem.TryGetConfiguredDefinition(prefab, out BuildingDefinition definition))
        {
            return false;
        }

        BeginPlacement(context, definition);
        return true;
    }

    public bool ConfirmBuildingPlacement(Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext);
    }

    public int EnqueueConfirmBuildingPlacement(EntityManager em)
    {
        return EnqueueUiPlacementCommand(
            em,
            BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
            clearBuildingSelection: true);
    }

    public bool EnqueueAndProcessConfirmBuildingPlacement(EntityManager em, Context context)
    {
        int requestId = EnqueueConfirmBuildingPlacement(em);
        ProcessPendingUiPlacementCommands(em, context);
        return TryGetUiPlacementCommandResult(em, requestId, out BuildingUiPlacementCommandResultElement result) &&
               result.Accepted != 0;
    }

    public bool RotateBuildingPlacement(Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.RotateBuildingPlacement(context.SessionContext);
    }

    public int EnqueueRotateBuildingPlacement(EntityManager em)
    {
        return EnqueueUiPlacementCommand(
            em,
            BuildingUiPlacementCommandRequestElement.KindRotatePlacement,
            clearBuildingSelection: true);
    }

    public bool EnqueueAndProcessRotateBuildingPlacement(EntityManager em, Context context)
    {
        int requestId = EnqueueRotateBuildingPlacement(em);
        ProcessPendingUiPlacementCommands(em, context);
        return TryGetUiPlacementCommandResult(em, requestId, out BuildingUiPlacementCommandResultElement result) &&
               result.Accepted != 0;
    }

    public void CancelBuildingPlacement(Context context)
    {
        context.SessionSystem?.CancelBuildingPlacement(context.SessionContext);
    }

    public int EnqueueCancelBuildingPlacement(EntityManager em)
    {
        return EnqueueUiPlacementCommand(
            em,
            BuildingUiPlacementCommandRequestElement.KindCancelPlacement,
            clearBuildingSelection: true);
    }

    public void EnqueueAndProcessCancelBuildingPlacement(EntityManager em, Context context)
    {
        EnqueueCancelBuildingPlacement(em);
        ProcessPendingUiPlacementCommands(em, context);
    }

    public void ExitBuildMode(Context context)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext);
    }

    public void ExitBuildMode(Context context, bool clearBuildingSelection)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext, clearBuildingSelection);
    }

    public int EnqueueExitBuildMode(EntityManager em, bool clearBuildingSelection = true)
    {
        return EnqueueUiPlacementCommand(
            em,
            BuildingUiPlacementCommandRequestElement.KindExitBuildMode,
            clearBuildingSelection);
    }

    public void EnqueueAndProcessExitBuildMode(EntityManager em, Context context, bool clearBuildingSelection = true)
    {
        EnqueueExitBuildMode(em, clearBuildingSelection);
        ProcessPendingUiPlacementCommands(em, context);
    }

    public bool TryGetUiPlacementCommandResult(
        EntityManager em,
        int requestId,
        out BuildingUiPlacementCommandResultElement result)
    {
        result = default;
        Entity queueEntity = EnsureUiPlacementCommandEntity(em);
        DynamicBuffer<BuildingUiPlacementCommandResultElement> results =
            em.GetBuffer<BuildingUiPlacementCommandResultElement>(queueEntity);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].RequestId == requestId)
            {
                result = results[i];
                return true;
            }
        }

        return false;
    }

    public void ProcessPendingUiPlacementCommands(EntityManager em, Context context)
    {
        Entity queueEntity = EnsureUiPlacementCommandEntity(em);
        DynamicBuffer<BuildingUiPlacementCommandRequestElement> requests =
            em.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity);
        if (requests.Length == 0)
            return;

        using NativeList<BuildingUiPlacementCommandRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
        for (int i = 0; i < requests.Length; i++)
            pendingRequests.Add(requests[i]);
        requests.Clear();

        DynamicBuffer<BuildingUiPlacementCommandResultElement> results =
            em.GetBuffer<BuildingUiPlacementCommandResultElement>(queueEntity);
        results.Clear();

        NativeArray<BuildingUiPlacementCommandRequestElement> pendingArray = pendingRequests.AsArray();
        for (int i = 0; i < pendingArray.Length; i++)
        {
            BuildingUiPlacementCommandRequestElement request = pendingArray[i];
            bool accepted = ProcessUiPlacementCommand(context, request, out byte resultCode);
            results = em.GetBuffer<BuildingUiPlacementCommandResultElement>(queueEntity);
            results.Add(new BuildingUiPlacementCommandResultElement
            {
                RequestId = request.RequestId,
                RequestKind = request.RequestKind,
                Accepted = accepted ? (byte)1 : (byte)0,
                ResultCode = resultCode
            });
        }
    }

    public void NotifyPlacementUiPointerDown(Context context)
    {
        context.SessionSystem?.NotifyPlacementUiPointerDown(context.SessionContext);
    }

    public void SetActivePlacementCost(Context context, int cost)
    {
        context.SessionSystem?.SetActivePlacementCost(context.SessionContext, cost);
    }

    private static void BeginConfiguredPlacement(Context context, BuildingDefinition definition, string missingPrefabWarning)
    {
        if (definition == null || definition.Prefab == null)
        {
            context.LogWarning?.Invoke(missingPrefabWarning);
            return;
        }

        BeginPlacement(context, definition);
    }

    private static void BeginPlacement(Context context, BuildingDefinition definition)
    {
        context.SessionSystem?.BeginPlacement(context.SessionContext, definition);
    }

    private static bool ProcessUiPlacementCommand(
        Context context,
        BuildingUiPlacementCommandRequestElement request,
        out byte resultCode)
    {
        if (context.SessionSystem == null)
        {
            resultCode = BuildingUiPlacementCommandResultElement.MissingSession;
            return false;
        }

        switch (request.RequestKind)
        {
            case BuildingUiPlacementCommandRequestElement.KindConfirmPlacement:
                if (context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext))
                {
                    resultCode = BuildingUiPlacementCommandResultElement.Completed;
                    return true;
                }

                resultCode = BuildingUiPlacementCommandResultElement.Rejected;
                return false;

            case BuildingUiPlacementCommandRequestElement.KindCancelPlacement:
                context.SessionSystem.CancelBuildingPlacement(context.SessionContext);
                resultCode = BuildingUiPlacementCommandResultElement.Completed;
                return true;

            case BuildingUiPlacementCommandRequestElement.KindRotatePlacement:
                if (context.SessionSystem.RotateBuildingPlacement(context.SessionContext))
                {
                    resultCode = BuildingUiPlacementCommandResultElement.Completed;
                    return true;
                }

                resultCode = BuildingUiPlacementCommandResultElement.Rejected;
                return false;

            case BuildingUiPlacementCommandRequestElement.KindExitBuildMode:
                context.SessionSystem.ExitBuildMode(
                    context.SessionContext,
                    request.ClearBuildingSelection != 0);
                resultCode = BuildingUiPlacementCommandResultElement.Completed;
                return true;

            default:
                resultCode = BuildingUiPlacementCommandResultElement.Rejected;
                return false;
        }
    }

    private static int EnqueueUiPlacementCommand(
        EntityManager em,
        byte requestKind,
        bool clearBuildingSelection)
    {
        Entity queueEntity = EnsureUiPlacementCommandEntity(em);
        BuildingUiPlacementCommandQueueComponent queue =
            em.GetComponentData<BuildingUiPlacementCommandQueueComponent>(queueEntity);
        queue.LastRequestId++;
        em.SetComponentData(queueEntity, queue);
        em.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity).Add(new BuildingUiPlacementCommandRequestElement
        {
            RequestId = queue.LastRequestId,
            RequestKind = requestKind,
            ClearBuildingSelection = clearBuildingSelection ? (byte)1 : (byte)0
        });
        return queue.LastRequestId;
    }

    private static Entity EnsureUiPlacementCommandEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
        if (!query.IsEmptyIgnoreFilter)
        {
            Entity existing = query.GetSingletonEntity();
            EnsureUiPlacementCommandBuffers(em, existing);
            return existing;
        }

        Entity entity = em.CreateEntity(typeof(BuildingUiPlacementCommandQueueComponent));
        em.SetName(entity, "BuildingUiPlacementCommands");
        EnsureUiPlacementCommandBuffers(em, entity);
        return entity;
    }

    private static void EnsureUiPlacementCommandBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<BuildingUiPlacementCommandRequestElement>(entity))
            em.AddBuffer<BuildingUiPlacementCommandRequestElement>(entity);
        if (!em.HasBuffer<BuildingUiPlacementCommandResultElement>(entity))
            em.AddBuffer<BuildingUiPlacementCommandResultElement>(entity);
    }
}
