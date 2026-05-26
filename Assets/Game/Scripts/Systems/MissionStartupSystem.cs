using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class MissionStartupSystem
{
    private readonly MissionCameraSystem _missionCameraSystem = new();

    public readonly struct Result
    {
        public readonly bool ActiveFixedTacticalMission;

        public Result(bool activeFixedTacticalMission)
        {
            ActiveFixedTacticalMission = activeFixedTacticalMission;
        }
    }

    public Result Initialize(
        World world,
        Camera worldCamera,
        DayNightSystem dayNight,
        IReadOnlyList<GameObject> legacyVisualRootsDisabledForM01)
    {
        UpdateActiveMission(world);
        bool activeFixedTacticalMission = Chapter01M01PlayableRuntime.IsActiveMission();
        ApplyM01ProductionSceneVisibility(legacyVisualRootsDisabledForM01, activeFixedTacticalMission);
        ApplyFixedTacticalMissionGuardrails(dayNight, activeFixedTacticalMission);
        DisableGenericAIPlansForFixedTacticalMission(world, activeFixedTacticalMission);
        return new Result(activeFixedTacticalMission);
    }

    public void UpdateActiveMission(World world)
    {
        Chapter01M01PlayableRuntime.TryInitializeActiveMission(world, out _);
    }

    public bool FocusInitialCamera(
        World world,
        SelectionUiCameraSystem selectionUiCameraSystem,
        Camera worldCamera,
        MissionCameraSystem.TryResolveFactionSpawnCell resolveFactionSpawnCell,
        byte fallbackFactionId)
    {
        return _missionCameraSystem.FocusInitialCamera(
            world,
            selectionUiCameraSystem,
            worldCamera,
            resolveFactionSpawnCell,
            fallbackFactionId);
    }

    public bool ApplyM01ProductionCameraPoseForCurrentAspect(World world, Camera worldCamera)
    {
        return _missionCameraSystem.ApplyM01ProductionCameraPoseForCurrentAspect(world, worldCamera);
    }

    public void ApplyM01ProductionCameraPoseIfActive(World world, Camera worldCamera)
    {
        _missionCameraSystem.ApplyM01ProductionCameraPoseIfActive(world, worldCamera);
    }

    private void ApplyFixedTacticalMissionGuardrails(DayNightSystem dayNight, bool activeFixedTacticalMission)
    {
        if (dayNight == null)
            return;

        dayNight.SetRuntimeVisualsEnabled(!activeFixedTacticalMission);
    }

    public void DisableGenericAIPlansForFixedTacticalMission(World world, bool activeFixedTacticalMission)
    {
        if (!activeFixedTacticalMission || world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        DisableAIBuildPlans(em);
        DisableAIProductionPlans(em);
        DisableAISquadPlans(em);
    }

    private void DisableAIBuildPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIBuildPlan>(entity))
                continue;

            AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void DisableAIProductionPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIProductionPlan>(entity))
                continue;

            AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void DisableAISquadPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AISquadPlan>(entity))
                continue;

            AISquadPlan plan = em.GetComponentData<AISquadPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void ApplyM01ProductionSceneVisibility(
        IReadOnlyList<GameObject> legacyVisualRootsDisabledForM01,
        bool activeFixedTacticalMission)
    {
        if (legacyVisualRootsDisabledForM01 == null)
            return;

        for (int i = 0; i < legacyVisualRootsDisabledForM01.Count; i++)
        {
            GameObject visualRoot = legacyVisualRootsDisabledForM01[i];
            if (visualRoot != null)
                visualRoot.SetActive(!activeFixedTacticalMission);
        }
    }

}
