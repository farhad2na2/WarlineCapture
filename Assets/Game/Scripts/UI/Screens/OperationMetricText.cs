public static class OperationMetricText
{
    public static string FormatDistrictStatus(DistrictStateData district)
    {
        return $"THREAT {district.threat} / HEAT {district.heat} / RISK {district.civilianRisk}";
    }

    public static string FormatPrimaryLine(DistrictStateData district)
    {
        return $"Trust {district.trust} / Security {district.security} / Infra {district.infrastructure}";
    }

    public static string FormatPressureLine(DistrictStateData district)
    {
        return $"Influence {district.enemyInfluence} / Heat {district.heat} / Civilian Risk {district.civilianRisk}";
    }

    public static string FormatIntelLine(DistrictStateData district)
    {
        return $"Stability {district.stability} / Intel {district.intel}";
    }

    public static string FormatDashboardSummary(DistrictStateData district)
    {
        return $"{FormatPrimaryLine(district)}. {FormatPressureLine(district)}. {FormatIntelLine(district)}.";
    }

    public static string FormatDistrictHero(DistrictStateData district, int supplies)
    {
        return $"{FormatPrimaryLine(district)}. {FormatPressureLine(district)}. Supplies {supplies}.";
    }
}
