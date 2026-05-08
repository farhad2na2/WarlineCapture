public static class SaveMigration
{
    public const int CurrentVersion = 1;

    public static WarlineCaptureSaveData Migrate(WarlineCaptureSaveData saveData)
    {
        WarlineCaptureSaveData data = saveData ?? new WarlineCaptureSaveData();
        data.saveVersion = CurrentVersion;
        data.profile ??= new PlayerProfileSaveData();
        data.saga ??= new SagaSaveData();
        data.operation ??= new OperationSaveData();
        data.settings ??= new SettingsSaveData();
        data.quickGame ??= new QuickGameSaveData();
        data.profile.blueprintParts ??= System.Array.Empty<BlueprintPartSaveData>();
        data.profile.ownedUnitUnlocks ??= System.Array.Empty<string>();
        data.profile.ownedBuildingUnlocks ??= System.Array.Empty<string>();
        data.profile.ownedSupportAbilityUnlocks ??= System.Array.Empty<string>();
        data.profile.ownedCosmetics ??= System.Array.Empty<string>();
        data.profile.claimedRewardTrackNodes ??= System.Array.Empty<string>();
        data.profile.missionHistory ??= System.Array.Empty<MissionHistoryEntrySaveData>();
        data.saga.missions ??= System.Array.Empty<SagaMissionProgressData>();
        if (data.operation.operationSupplies <= 0)
            data.operation.operationSupplies = 4;
        data.operation.districts ??= System.Array.Empty<DistrictStateData>();
        foreach (DistrictStateData district in data.operation.districts)
            NormalizeDistrict(district);

        data.operation.pendingEvents ??= System.Array.Empty<OperationEventData>();
        data.operation.intelEvidence ??= System.Array.Empty<OperationIntelEvidenceData>();
        foreach (OperationEventData operationEvent in data.operation.pendingEvents)
        {
            if (operationEvent == null)
                continue;

            if (operationEvent.operationDay <= 0)
                operationEvent.operationDay = data.operation.operationDay > 0 ? data.operation.operationDay : 1;
            if (string.IsNullOrWhiteSpace(operationEvent.eventId))
                operationEvent.eventId = $"operation.event.{operationEvent.operationDay}";
            operationEvent.metricValue = UnityEngine.Mathf.Clamp(operationEvent.metricValue, 0, 100);
        }
        foreach (OperationIntelEvidenceData evidence in data.operation.intelEvidence)
        {
            if (evidence == null)
                continue;

            if (evidence.operationDay <= 0)
                evidence.operationDay = data.operation.operationDay > 0 ? data.operation.operationDay : 1;
            if (string.IsNullOrWhiteSpace(evidence.evidenceId))
                evidence.evidenceId = $"operation.evidence.{evidence.operationDay}";
            evidence.confidence = UnityEngine.Mathf.Clamp(evidence.confidence, 0, 100);
        }
        return data;
    }

    private static void NormalizeDistrict(DistrictStateData district)
    {
        if (district == null)
            return;

        district.stability = UnityEngine.Mathf.Clamp(district.stability, 0, 100);
        district.threat = UnityEngine.Mathf.Clamp(district.threat, 0, 100);
        district.intel = UnityEngine.Mathf.Clamp(district.intel, 0, 100);

        bool hasSecondaryMetrics = district.trust > 0
            || district.security > 0
            || district.infrastructure > 0
            || district.enemyInfluence > 0
            || district.heat > 0
            || district.civilianRisk > 0;

        if (!hasSecondaryMetrics)
        {
            district.trust = district.stability;
            district.security = 100 - district.threat;
            district.infrastructure = district.stability;
            district.enemyInfluence = district.threat;
            district.heat = UnityEngine.Mathf.RoundToInt(district.threat * 0.65f);
            district.civilianRisk = UnityEngine.Mathf.RoundToInt((district.threat + (100 - district.stability)) * 0.5f);
        }

        district.trust = UnityEngine.Mathf.Clamp(district.trust, 0, 100);
        district.security = UnityEngine.Mathf.Clamp(district.security, 0, 100);
        district.infrastructure = UnityEngine.Mathf.Clamp(district.infrastructure, 0, 100);
        district.enemyInfluence = UnityEngine.Mathf.Clamp(district.enemyInfluence, 0, 100);
        district.heat = UnityEngine.Mathf.Clamp(district.heat, 0, 100);
        district.civilianRisk = UnityEngine.Mathf.Clamp(district.civilianRisk, 0, 100);
    }
}
