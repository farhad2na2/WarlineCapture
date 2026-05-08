using UnityEngine;

[CreateAssetMenu(fileName = "OperationActionConfigSet", menuName = "WarlineCapture/Operation/Action Config Set")]
public sealed class OperationActionConfigSet : ScriptableObject
{
    public OperationActionConfig[] actionConfigs = OperationActionConfig.CreateDefaults();
    public OperationDistrictActionModifier[] districtModifiers = CreateDefaultDistrictModifiers();
    public OperationDistrictEventRule[] eventRules = CreateDefaultEventRules();

    public OperationActionConfig[] GetActionConfigs()
    {
        return actionConfigs == null || actionConfigs.Length == 0
            ? OperationActionConfig.CreateDefaults()
            : actionConfigs;
    }

    public OperationDistrictActionModifier[] GetDistrictModifiers()
    {
        return districtModifiers ?? System.Array.Empty<OperationDistrictActionModifier>();
    }

    public OperationDistrictEventRule[] GetEventRules()
    {
        return eventRules == null || eventRules.Length == 0
            ? CreateDefaultEventRules()
            : eventRules;
    }

    public static OperationDistrictActionModifier[] CreateDefaultDistrictModifiers()
    {
        return new[]
        {
            new OperationDistrictActionModifier(
                "old_market",
                OperationActionType.Aid,
                3,
                0,
                0,
                1,
                "Old Market Aid Distribution",
                "Medical and water distribution stabilized crowded market blocks and recovered one operation supply.",
                trustDelta: 4,
                infrastructureDelta: 3,
                heatDelta: -1,
                civilianRiskDelta: -4),
            new OperationDistrictActionModifier(
                "port_breach",
                OperationActionType.Raid,
                -2,
                -4,
                0,
                0,
                "Port Breach Strike",
                "Raid pressure disrupted a port-side command cell, but stability took a short-term hit.",
                OperationEventSeverity.Warning,
                securityDelta: 4,
                infrastructureDelta: -2,
                enemyInfluenceDelta: -5,
                heatDelta: 3,
                civilianRiskDelta: 4),
            new OperationDistrictActionModifier(
                "port_breach",
                OperationActionType.Scan,
                0,
                0,
                4,
                0,
                "Port Sensor Sweep",
                "Drone scan mapped container-lane movement and improved port intel confidence.",
                enemyInfluenceDelta: -1,
                heatDelta: 1),
            new OperationDistrictActionModifier(
                "north_bridge",
                OperationActionType.BuildOutpost,
                1,
                -2,
                0,
                0,
                "Bridge Outpost Established",
                "A checkpoint outpost locked down bridge access and gave patrol teams faster response coverage.",
                OperationEventSeverity.Warning,
                securityDelta: 4,
                infrastructureDelta: 1,
                enemyInfluenceDelta: -3,
                heatDelta: 2,
                civilianRiskDelta: -1),
            new OperationDistrictActionModifier(
                "old_market",
                OperationActionType.Evacuate,
                -2,
                0,
                0,
                0,
                "Market Evacuation Corridor",
                "Evacuation corridors reduced immediate crowd risk but created political pressure among shop owners.",
                OperationEventSeverity.Warning,
                trustDelta: -3,
                heatDelta: 1,
                civilianRiskDelta: -5),
            new OperationDistrictActionModifier(
                "port_breach",
                OperationActionType.Repair,
                2,
                -1,
                0,
                0,
                "Port Utility Repair",
                "Repair crews restored port utilities under security escort, reducing civilian exposure.",
                infrastructureDelta: 5,
                heatDelta: -1,
                civilianRiskDelta: -3)
        };
    }

    public static OperationDistrictEventRule[] CreateDefaultEventRules()
    {
        return new[]
        {
            new OperationDistrictEventRule(
                OperationDistrictMetric.Heat,
                65,
                82,
                OperationEventCategory.Risk,
                "District Heat Rising",
                "{district} heat reached {value}. Expect faster enemy reaction if pressure is ignored.",
                "District Heat Critical",
                "{district} heat reached {value}. Enemy response cells are close to open escalation."),
            new OperationDistrictEventRule(
                OperationDistrictMetric.CivilianRisk,
                65,
                82,
                OperationEventCategory.Civilian,
                "Civilian Risk Elevated",
                "{district} civilian risk reached {value}. Aid or evacuation should be prioritized.",
                "Civilian Risk Critical",
                "{district} civilian risk reached {value}. Civilian losses are likely without immediate intervention."),
            new OperationDistrictEventRule(
                OperationDistrictMetric.EnemyInfluence,
                75,
                90,
                OperationEventCategory.Risk,
                "Enemy Influence Entrenched",
                "{district} enemy influence reached {value}. Patrol, scan, or raid pressure is recommended.",
                "Enemy Influence Critical",
                "{district} enemy influence reached {value}. Hostile command can reinforce nearby objectives.")
        };
    }
}
