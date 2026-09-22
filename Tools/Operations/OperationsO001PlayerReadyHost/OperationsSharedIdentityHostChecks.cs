using System;
using Game.Configs;
using Game.Operations.Contracts;

internal static class OperationsSharedIdentityHostChecks
{
    public static void AssertAgrees()
    {
        string[] maps =
        {
            "opmap.operations.old_quarter",
            "opmap.operations.civic_center",
            "opmap.operations.industrial_belt",
            "opmap.operations.river_crossing",
            "opmap.operations.highland_approach",
            "opmap.operations.airport_perimeter"
        };
        for (int index = 0; index < maps.Length; index++)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(maps[index]) ||
                !OperationsIdentityRules.IsValidOperationMapId(maps[index]))
                throw new InvalidOperationException(maps[index]);
        }

        if (OperationMapIdentityRules.IsValidOperationMapId("opmap.operations.unknown"))
            throw new InvalidOperationException("unknown map");
        if (!OperationMapIdentityRules.IsValidOperationMapId("opmap.skirmish.desert_base_01") ||
            !OperationMapIdentityRules.IsValidOperationMapId("opmap.ch01.district_edge_01"))
            throw new InvalidOperationException("existing map");
        if (OperationMapIdentityRules.IsValidOperationMapId("opmap.campaign.district_edge_01"))
            throw new InvalidOperationException("campaign map");

        for (int number = 1; number <= 60; number++)
        {
            string scenario = "scenario.operations.o" + number.ToString("000");
            if (!OperationMapIdentityRules.IsValidScenarioId(scenario) ||
                !OperationsIdentityRules.IsValidScenarioId(scenario))
                throw new InvalidOperationException(scenario);
        }

        if (OperationMapIdentityRules.IsValidScenarioId("scenario.operations.o000") ||
            OperationMapIdentityRules.IsValidScenarioId("scenario.operations.o061") ||
            OperationMapIdentityRules.IsValidScenarioId("scenario.operations.o1"))
            throw new InvalidOperationException("scenario range");
        if (!OperationMapIdentityRules.IsValidScenarioId("scenario.skirmish.desert_base_standard") ||
            !OperationMapIdentityRules.IsValidScenarioId("scenario.ch01.m01.first_contact"))
            throw new InvalidOperationException("existing scenario");
    }
}
