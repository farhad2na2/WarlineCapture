using UnityEngine;

public static class BalanceOutcomeClassifier
{
    public const string Good = "Good";
    public const string Watch = "Watch";
    public const string Problem = "Problem";
    public const string InvalidRun = "InvalidRun";

    public static void Classify(BalanceMetrics metrics)
    {
        if (metrics == null)
            return;

        metrics.MatchDurationClassification = ClassifyMatchDuration(metrics.SampledDurationSeconds);
        metrics.EconomyActivityClassification = ClassifyEconomyActivity(metrics);
        metrics.CasualtyClassification = ClassifyCasualties(metrics);
        metrics.OverallClassification = ResolveOverall(
            metrics.MatchDurationClassification,
            metrics.EconomyActivityClassification,
            metrics.CasualtyClassification);
    }

    public static string ClassifyMatchDuration(float seconds)
    {
        if (seconds <= 0f)
            return InvalidRun;

        float minutes = seconds / 60f;
        if (minutes >= 8f && minutes <= 14f)
            return Good;
        if ((minutes >= 6f && minutes < 8f) || (minutes > 14f && minutes <= 18f))
            return Watch;
        return Problem;
    }

    public static string ClassifyEconomyActivity(BalanceMetrics metrics)
    {
        if (metrics == null)
            return InvalidRun;

        int productionActivity = Mathf.Max(0, metrics.VehiclesOrdered) +
                                 Mathf.Max(0, metrics.SoldiersOrdered) +
                                 Mathf.Max(0, metrics.AmmoOrdered) +
                                 Mathf.Max(0, metrics.BuildingsBuilt);

        if (metrics.OilExtracted <= 0 && metrics.FuelProduced <= 0 && productionActivity <= 0)
            return Watch;

        return Good;
    }

    public static string ClassifyCasualties(BalanceMetrics metrics)
    {
        if (metrics == null)
            return InvalidRun;

        int ownDeaths = Mathf.Max(0, metrics.OwnSoldiersDead);
        int enemyDeaths = Mathf.Max(0, metrics.EnemySoldiersDead);

        if (ownDeaths == 0 && enemyDeaths == 0)
            return Watch;

        if (ownDeaths > enemyDeaths * 3 && ownDeaths >= 6)
            return Problem;

        return Good;
    }

    private static string ResolveOverall(params string[] classifications)
    {
        bool hasWatch = false;

        foreach (string classification in classifications)
        {
            if (classification == InvalidRun)
                return InvalidRun;
            if (classification == Problem)
                return Problem;
            if (classification == Watch)
                hasWatch = true;
        }

        return hasWatch ? Watch : Good;
    }
}
