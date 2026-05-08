using System;
using UnityEngine;

[Serializable]
public sealed class OperationDistrictEventRule
{
    public OperationDistrictMetric metric;
    public int warningThreshold = 65;
    public int criticalThreshold = 85;
    public OperationEventCategory category = OperationEventCategory.Pressure;
    public string warningTitle;
    [TextArea(2, 4)] public string warningBody;
    public string criticalTitle;
    [TextArea(2, 4)] public string criticalBody;

    public OperationDistrictEventRule()
    {
    }

    public OperationDistrictEventRule(
        OperationDistrictMetric metric,
        int warningThreshold,
        int criticalThreshold,
        OperationEventCategory category,
        string warningTitle,
        string warningBody,
        string criticalTitle,
        string criticalBody)
    {
        this.metric = metric;
        this.warningThreshold = Mathf.Clamp(warningThreshold, 0, 100);
        this.criticalThreshold = Mathf.Clamp(Math.Max(warningThreshold, criticalThreshold), 0, 100);
        this.category = category;
        this.warningTitle = warningTitle;
        this.warningBody = warningBody;
        this.criticalTitle = criticalTitle;
        this.criticalBody = criticalBody;
    }

    public bool TryCreateEvent(DistrictStateData district, int operationDay, out OperationEventData operationEvent)
    {
        operationEvent = null;
        if (district == null)
            return false;

        int value = GetMetricValue(district, metric);
        OperationEventSeverity severity;
        string title;
        string body;
        if (value >= criticalThreshold)
        {
            severity = OperationEventSeverity.Critical;
            title = criticalTitle;
            body = criticalBody;
        }
        else if (value >= warningThreshold)
        {
            severity = OperationEventSeverity.Warning;
            title = warningTitle;
            body = warningBody;
        }
        else
        {
            return false;
        }

        string metricName = FormatMetric(metric);
        string districtName = FormatDistrictName(district.districtId);
        operationEvent = new OperationEventData
        {
            eventId = $"operation.alert.{operationDay}.{district.districtId}.{metric.ToString().ToLowerInvariant()}",
            districtId = district.districtId,
            category = category,
            severity = severity,
            operationDay = Mathf.Max(1, operationDay),
            sourceMetric = metricName,
            metricValue = value,
            title = string.IsNullOrWhiteSpace(title) ? $"{districtName} {metricName} Alert" : title,
            body = string.IsNullOrWhiteSpace(body)
                ? $"{districtName} {metricName.ToLowerInvariant()} reached {value}."
                : body.Replace("{district}", districtName).Replace("{metric}", metricName).Replace("{value}", value.ToString())
        };
        return true;
    }

    public static int GetMetricValue(DistrictStateData district, OperationDistrictMetric metric)
    {
        return metric switch
        {
            OperationDistrictMetric.Stability => district.stability,
            OperationDistrictMetric.Threat => district.threat,
            OperationDistrictMetric.Intel => district.intel,
            OperationDistrictMetric.Trust => district.trust,
            OperationDistrictMetric.Security => district.security,
            OperationDistrictMetric.Infrastructure => district.infrastructure,
            OperationDistrictMetric.EnemyInfluence => district.enemyInfluence,
            OperationDistrictMetric.Heat => district.heat,
            OperationDistrictMetric.CivilianRisk => district.civilianRisk,
            _ => 0
        };
    }

    private static string FormatMetric(OperationDistrictMetric metric)
    {
        return metric switch
        {
            OperationDistrictMetric.EnemyInfluence => "Enemy Influence",
            OperationDistrictMetric.CivilianRisk => "Civilian Risk",
            _ => metric.ToString()
        };
    }

    private static string FormatDistrictName(string districtId)
    {
        return string.IsNullOrWhiteSpace(districtId)
            ? "Unknown District"
            : districtId.Replace('_', ' ').ToUpperInvariant();
    }
}
