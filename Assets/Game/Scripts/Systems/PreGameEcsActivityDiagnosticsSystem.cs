using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PreGameEcsActivityDiagnosticsSystem : ISystem
    {
        private const int LogIntervalFrames = 120;

        private EntityQuery _unitQuery;
        private EntityQuery _modelReferenceQuery;
        private EntityQuery _healthBarQuery;
        private EntityQuery _pathRequestQuery;
        private EntityQuery _pathFollowQuery;
        private EntityQuery _aiBuildPlanQuery;
        private EntityQuery _aiProductionPlanQuery;
        private EntityQuery _aiSquadQuery;
        private EntityQuery _threatDetectorQuery;
        private EntityQuery _buildingCombatQuery;
        private EntityQuery _initialSpawnConfigQuery;
        private EntityQuery _initialSpawnInitializedQuery;
        private int _nextLogFrame;

        public void OnCreate(ref SystemState state)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            state.Enabled = false;
            return;
#endif
            _unitQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>());
            _modelReferenceQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitModelInstanceReference>());
            _healthBarQuery = state.GetEntityQuery(ComponentType.ReadOnly<HealthBarFill>());
            _pathRequestQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitPathRequest>());
            _pathFollowQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitPathFollow>());
            _aiBuildPlanQuery = state.GetEntityQuery(ComponentType.ReadOnly<AIBuildPlan>());
            _aiProductionPlanQuery = state.GetEntityQuery(ComponentType.ReadOnly<AIProductionPlan>());
            _aiSquadQuery = state.GetEntityQuery(ComponentType.ReadOnly<AISquad>());
            _threatDetectorQuery = state.GetEntityQuery(ComponentType.ReadOnly<ThreatDetector>());
            _buildingCombatQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeBuildingCombatTag>());
            _initialSpawnConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            _initialSpawnInitializedQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested != 0)
                return;

            if (Time.frameCount < _nextLogFrame)
                return;

            _nextLogFrame = Time.frameCount + LogIntervalFrames;
            int units = _unitQuery.CalculateEntityCount();
            int models = _modelReferenceQuery.CalculateEntityCount();
            int healthBars = _healthBarQuery.CalculateEntityCount();
            int buildings = _buildingCombatQuery.CalculateEntityCount();
            int pathRequests = _pathRequestQuery.CalculateEntityCount();
            int pathFollowers = _pathFollowQuery.CalculateEntityCount();
            int aiBuildPlans = _aiBuildPlanQuery.CalculateEntityCount();
            int aiProductionPlans = _aiProductionPlanQuery.CalculateEntityCount();
            int aiSquads = _aiSquadQuery.CalculateEntityCount();
            int threatDetectors = _threatDetectorQuery.CalculateEntityCount();

            if (units == 0 &&
                models == 0 &&
                healthBars == 0 &&
                buildings == 0 &&
                pathRequests == 0 &&
                pathFollowers == 0 &&
                aiBuildPlans == 0 &&
                aiProductionPlans == 0 &&
                aiSquads == 0 &&
                threatDetectors == 0)
            {
                return;
            }

            Debug.Log(
                $"[PerfDiag:ECS:PreGame] frame={Time.frameCount} " +
                $"units={units} models={models} " +
                $"healthBars={healthBars} buildings={buildings} " +
                $"pathRequests={pathRequests} pathFollowers={pathFollowers} " +
                $"aiBuildPlans={aiBuildPlans} aiProductionPlans={aiProductionPlans} " +
                $"aiSquads={aiSquads} threatDetectors={threatDetectors} " +
                $"initialSpawnConfigs={_initialSpawnConfigQuery.CalculateEntityCount()} initializedSpawnConfigs={_initialSpawnInitializedQuery.CalculateEntityCount()} " +
                $"focused={(Application.isFocused ? 1 : 0)} vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate}");
        }
    }
}
