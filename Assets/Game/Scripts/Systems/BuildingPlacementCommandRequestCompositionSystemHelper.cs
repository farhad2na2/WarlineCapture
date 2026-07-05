using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Tactical.Contracts;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementCommandRequestCompositionSystemHelper
    {
        internal readonly struct Context
        {
            public readonly BuildingPlacementStartupSystemHelper StartupSystem;
            public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
            public readonly BuildingPlacementSessionCompositionSystemHelper SessionSystem;
            public readonly BuildingPlacementSessionCompositionSystemHelper.Context SessionContext;
            public readonly Action<string> LogWarning;

            public Context(
                BuildingPlacementStartupSystemHelper startupSystem,
                BuildingDefinitionPrefabSystemHelper definitionSystem,
                BuildingPlacementSessionCompositionSystemHelper sessionSystem,
                BuildingPlacementSessionCompositionSystemHelper.Context sessionContext,
                Action<string> logWarning)
            {
                StartupSystem = startupSystem;
                DefinitionSystem = definitionSystem;
                SessionSystem = sessionSystem;
                SessionContext = sessionContext;
                LogWarning = logWarning;
            }
        }

        public int EnqueueBeginConfiguredPlacement(EntityManager em, string buildingId)
        {
            return EnqueueUiPlacementCommand(
                em,
                BuildingUiPlacementCommandRequestElement.KindBeginConfiguredPlacement,
                clearBuildingSelection: true,
                BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId));
        }

        public bool EnqueueAndProcessBeginConfiguredPlacement(EntityManager em, Context context, string buildingId)
        {
            int requestId = EnqueueBeginConfiguredPlacement(em, buildingId);
            ProcessPendingUiPlacementCommands(em, context);
            return TryGetUiPlacementCommandResult(em, requestId, out BuildingUiPlacementCommandResultElement result) &&
                   result.Accepted != 0;
        }

        public bool EnqueueAndProcessBeginPlacementForConfiguredSpawnable(EntityManager em, Context context, GameObject prefab)
        {
            string buildingId = BuildingDefinitionPrefabSystemHelper.GetSpawnableLookupKey(prefab);
            if (string.IsNullOrEmpty(buildingId))
                return false;

            return EnqueueAndProcessBeginConfiguredPlacement(em, context, buildingId);
        }

        public bool EnqueueAndProcessBeginSoldierBasePlacement(EntityManager em, Context context)
        {
            string buildingId = ResolveDefinitionLookupKey(context.StartupSystem?.SoldierBaseDefinition);
            if (string.IsNullOrEmpty(buildingId))
                return false;

            return EnqueueAndProcessBeginConfiguredPlacement(em, context, buildingId);
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
            ProcessPendingUiPlacementCommands(em, context, queueEntity);
        }

        public void ProcessPendingUiPlacementCommandsIfPresent(EntityManager em, Context context)
        {
            if (!TryGetUiPlacementCommandEntity(em, out Entity queueEntity))
                return;

            ProcessPendingUiPlacementCommands(em, context, queueEntity);
        }

        public bool HasPendingUiPlacementCommands(EntityManager em)
        {
            if (!TryGetUiPlacementCommandEntity(em, out Entity queueEntity))
                return false;

            DynamicBuffer<BuildingUiPlacementCommandRequestElement> requests =
                em.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity);
            return requests.Length > 0;
        }

        private void ProcessPendingUiPlacementCommands(EntityManager em, Context context, Entity queueEntity)
        {
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
                    ResultCode = resultCode,
                    ReasonCode = (int)ToReasonCode(resultCode)
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
                    if (context.SessionSystem.ConfirmBuildingPlacement(
                            context.SessionContext,
                            out BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason confirmFailureReason))
                    {
                        resultCode = BuildingUiPlacementCommandResultElement.Completed;
                        return true;
                    }

                    resultCode = ToConfirmFailureResultCode(confirmFailureReason);
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

                case BuildingUiPlacementCommandRequestElement.KindBeginConfiguredPlacement:
                    if (TryResolveConfiguredPlacementDefinition(context, request.BuildingId, out BuildingDefinition definition))
                    {
                        BeginPlacement(context, definition);
                        resultCode = BuildingUiPlacementCommandResultElement.Completed;
                        return true;
                    }

                    resultCode = BuildingUiPlacementCommandResultElement.MissingConfig;
                    return false;

                default:
                    resultCode = BuildingUiPlacementCommandResultElement.Rejected;
                    return false;
            }
        }

        private static bool TryResolveConfiguredPlacementDefinition(
            Context context,
            FixedString128Bytes buildingId,
            out BuildingDefinition definition)
        {
            definition = null;
            if (context.DefinitionSystem == null)
                return false;

            string normalizedBuildingId = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId.ToString());
            if (string.IsNullOrEmpty(normalizedBuildingId) ||
                !context.DefinitionSystem.TryGetConfiguredSpawnable(normalizedBuildingId, out var spawnable))
            {
                return false;
            }

            return context.DefinitionSystem.TryGetConfiguredDefinition(spawnable.Prefab, out definition) &&
                   definition != null &&
                   definition.Prefab != null;
        }

        private static byte ToConfirmFailureResultCode(BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason reason)
        {
            return reason switch
            {
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.MissingActivePlacement =>
                    BuildingUiPlacementCommandResultElement.MissingActivePlacement,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.BlockedPlacement =>
                    BuildingUiPlacementCommandResultElement.BlockedPlacement,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.InvalidPlacement =>
                    BuildingUiPlacementCommandResultElement.InvalidPlacement,
                BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.NotEnoughMoney =>
                    BuildingUiPlacementCommandResultElement.NotEnoughMoney,
                _ => BuildingUiPlacementCommandResultElement.Rejected
            };
        }

        private static TacticalCommandReasonCode ToReasonCode(byte resultCode)
        {
            return resultCode switch
            {
                BuildingUiPlacementCommandResultElement.Completed => TacticalCommandReasonCode.None,
                BuildingUiPlacementCommandResultElement.BlockedPlacement => TacticalCommandReasonCode.TargetBlocked,
                BuildingUiPlacementCommandResultElement.InvalidPlacement => TacticalCommandReasonCode.TargetUnreachable,
                BuildingUiPlacementCommandResultElement.NotEnoughMoney => TacticalCommandReasonCode.InsufficientResources,
                BuildingUiPlacementCommandResultElement.MissingActivePlacement => TacticalCommandReasonCode.BuildUnavailable,
                BuildingUiPlacementCommandResultElement.MissingConfig => TacticalCommandReasonCode.BuildUnavailable,
                _ => TacticalCommandReasonCode.CommandUnavailable
            };
        }

        private static int EnqueueUiPlacementCommand(
            EntityManager em,
            byte requestKind,
            bool clearBuildingSelection,
            string buildingId = "")
        {
            Entity queueEntity = EnsureUiPlacementCommandEntity(em);
            BuildingUiPlacementCommandQueueComponent queue =
                em.GetComponentData<BuildingUiPlacementCommandQueueComponent>(queueEntity);
            queue.LastRequestId++;
            em.SetComponentData(queueEntity, queue);
            em.GetBuffer<BuildingUiPlacementCommandRequestElement>(queueEntity).Add(new BuildingUiPlacementCommandRequestElement
            {
                RequestId = queue.LastRequestId,
                BuildingId = new FixedString128Bytes(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(buildingId)),
                RequestKind = requestKind,
                ClearBuildingSelection = clearBuildingSelection ? (byte)1 : (byte)0
            });
            return queue.LastRequestId;
        }

        private static string ResolveDefinitionLookupKey(BuildingDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            string displayKey = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(definition.DisplayName);
            if (!string.IsNullOrEmpty(displayKey))
                return displayKey;

            return definition.Prefab != null
                ? BuildingDefinitionPrefabSystemHelper.GetSpawnableLookupKey(definition.Prefab)
                : string.Empty;
        }

        private static Entity EnsureUiPlacementCommandEntity(EntityManager em)
        {
            if (TryGetUiPlacementCommandEntity(em, out Entity existing))
                return existing;

            Entity entity = em.CreateEntity(typeof(BuildingUiPlacementCommandQueueComponent));
            em.SetName(entity, "BuildingUiPlacementCommands");
            EnsureUiPlacementCommandBuffers(em, entity);
            return entity;
        }

        private static bool TryGetUiPlacementCommandEntity(EntityManager em, out Entity entity)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                entity = query.GetSingletonEntity();
                EnsureUiPlacementCommandBuffers(em, entity);
                return true;
            }

            entity = Entity.Null;
            return false;
        }

        private static void EnsureUiPlacementCommandBuffers(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<BuildingUiPlacementCommandRequestElement>(entity))
                em.AddBuffer<BuildingUiPlacementCommandRequestElement>(entity);
            if (!em.HasBuffer<BuildingUiPlacementCommandResultElement>(entity))
                em.AddBuffer<BuildingUiPlacementCommandResultElement>(entity);
        }
    }
}
