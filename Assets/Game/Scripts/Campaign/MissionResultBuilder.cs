using System;
using System.Linq;

public static class MissionResultBuilder
{
    public static MissionResultData Build(MissionConfig mission, GameRuntimeStats.Snapshot snapshot)
    {
        if (mission == null)
            throw new ArgumentNullException(nameof(mission));

        var objectiveManager = new ObjectiveManager();
        objectiveManager.Initialize(mission);
        ObjectiveRuntimeState[] objectiveStates = objectiveManager.Evaluate(snapshot).ToArray();

        bool victory = objectiveManager.HasWon;
        int stars = victory ? 1 : 0;
        if (victory)
        {
            foreach (StarGoalConfig starGoal in mission.StarGoals)
            {
                if (starGoal == null)
                    continue;

                int currentAmount = ObjectiveManager.ResolveProgress(starGoal.Type, snapshot);
                if (ObjectiveManager.IsComplete(starGoal.Type, currentAmount, starGoal.TargetAmount))
                    stars++;
            }
        }

        return new MissionResultData(
            mission.MissionId,
            mission.DisplayName,
            victory,
            stars,
            snapshot.EnemySoldiersDead,
            snapshot.OwnSoldiersDead,
            snapshot.BuildingsBuilt,
            snapshot.OilExtracted + snapshot.FuelProduced,
            objectiveStates);
    }
}
