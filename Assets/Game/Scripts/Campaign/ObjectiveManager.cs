using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectiveManager
{
    private readonly List<ObjectiveRuntimeState> _states = new();
    private MissionConfig _mission;

    public IReadOnlyList<ObjectiveRuntimeState> States => _states;
    public bool HasMission => _mission != null;
    public bool HasWon { get; private set; }

    public void Initialize(MissionConfig mission)
    {
        _mission = mission ?? throw new ArgumentNullException(nameof(mission));
        _states.Clear();
        HasWon = false;

        foreach (ObjectiveConfig objective in _mission.Objectives)
        {
            if (objective == null)
                continue;

            _states.Add(new ObjectiveRuntimeState(
                objective.Id,
                objective.DisplayName,
                objective.Type,
                0,
                objective.TargetAmount,
                objective.Required,
                false));
        }
    }

    public IReadOnlyList<ObjectiveRuntimeState> Evaluate(GameRuntimeStats.Snapshot snapshot)
    {
        if (_mission == null)
            throw new InvalidOperationException("ObjectiveManager must be initialized with a MissionConfig before evaluation.");

        _states.Clear();
        bool allRequiredComplete = true;

        foreach (ObjectiveConfig objective in _mission.Objectives)
        {
            if (objective == null)
                continue;

            int currentAmount = ResolveProgress(objective.Type, snapshot);
            bool complete = IsComplete(objective.Type, currentAmount, objective.TargetAmount);
            if (objective.Required && !complete)
                allRequiredComplete = false;

            _states.Add(new ObjectiveRuntimeState(
                objective.Id,
                objective.DisplayName,
                objective.Type,
                currentAmount,
                objective.TargetAmount,
                objective.Required,
                complete));
        }

        HasWon = allRequiredComplete;
        return _states;
    }

    public static int ResolveProgress(ObjectiveType type, GameRuntimeStats.Snapshot snapshot)
    {
        return type switch
        {
            ObjectiveType.DestroyAllEnemies => snapshot.EnemySoldiersDead,
            ObjectiveType.SurviveDuration => snapshot.MissionElapsedSeconds,
            ObjectiveType.ProtectCivilianCount => snapshot.CiviliansProtected,
            ObjectiveType.BuildStructure => snapshot.BuildingsBuilt,
            ObjectiveType.CaptureOrDestroyBuilding => snapshot.CapturedOrDestroyedBuildings,
            ObjectiveType.KeepUnitLossesBelow => snapshot.OwnSoldiersDead,
            ObjectiveType.ReachResourceAmount => Mathf.Max(0, snapshot.OilExtracted + snapshot.FuelProduced),
            _ => 0
        };
    }

    public static bool IsComplete(ObjectiveType type, int currentAmount, int targetAmount)
    {
        int normalizedTarget = Mathf.Max(0, targetAmount);
        return type switch
        {
            ObjectiveType.KeepUnitLossesBelow => currentAmount <= normalizedTarget,
            _ => currentAmount >= normalizedTarget
        };
    }
}
