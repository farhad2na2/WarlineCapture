public sealed class OperationEventsScreenSystem : OperationLedgerScreenSystem
{
    protected override void Refresh()
    {
        OperationSaveData state = WarlineCaptureOperationRuntime.State;
        OperationEventData latest = EventAt(state, 0);
        DistrictStateData hotDistrict = HighestThreatDistrict(state);
        OperationIntelEvidenceData latestEvidence = OperationIntelArchive.Latest(state, hotDistrict?.districtId);

        SetText("HeroPanel/EyebrowText", "OPERATION EVENTS");
        SetText("HeroPanel/HeroTitleText", latest != null ? "ACTIVE CITY EVENT" : "NO ACTIVE EVENT");
        SetText("HeroPanel/BodyText", latest != null
            ? $"{latest.title}: {latest.body}"
            : "Scheduled challenge operations are offline. Local Operation pressure still produces city events.");
        SetText("HeroPanel/UnavailableButton/LabelText", "EVENT LEDGER");

        SetText("StatusCard_1/TitleText", "LATEST EVENT");
        SetText("StatusCard_1/StatusText", FormatEventStatus(latest, "NONE"));
        SetText("StatusCard_1/BodyText", latest != null ? latest.body : "No local Operation event has been produced.");

        SetText("StatusCard_2/TitleText", "HOT ZONE");
        SetText("StatusCard_2/StatusText", hotDistrict != null ? $"THREAT {hotDistrict.threat}" : "NO DISTRICT");
        SetText("StatusCard_2/BodyText", hotDistrict != null
            ? $"{OperationDashboardScreenSystem.FormatDistrictName(hotDistrict.districtId)} is the current highest-pressure district."
            : "Operation districts are not loaded.");

        SetText("StatusCard_3/TitleText", "DAY PRESSURE");
        SetText("StatusCard_3/StatusText", $"DAY {state.operationDay}");
        SetText("StatusCard_3/BodyText", latestEvidence != null
            ? $"{latestEvidence.title}: {latestEvidence.body}"
            : $"Supplies {state.operationSupplies}. Completed actions {state.completedActions}.");

        BindFeed(1, latest, "CALENDAR", "No event window scheduled.");
        BindFeed(2, EventAt(state, 1), "PRESSURE", hotDistrict != null ? $"Highest threat: {hotDistrict.threat}." : "No district pressure available.");
        BindEvidenceFeed(3, latestEvidence, "REWARD", "Event rewards wait for config validation.");

        SetText("ImplementationNotePanel/TitleText", "LIVE OPERATION EVENTS");
        SetText("ImplementationNotePanel/BodyText", "This screen now mirrors the Operation event ledger until seasonal event services are connected.");
    }

    private void BindFeed(int index, OperationEventData operationEvent, string fallbackTag, string fallbackBody)
    {
        string path = $"FeedRow_{index}";
        SetText($"{path}/TagText", FormatEventTag(operationEvent, fallbackTag));
        SetText($"{path}/BodyText", operationEvent != null ? $"{operationEvent.title}: {operationEvent.body}" : fallbackBody);
    }

    private void BindEvidenceFeed(int index, OperationIntelEvidenceData evidence, string fallbackTag, string fallbackBody)
    {
        string path = $"FeedRow_{index}";
        SetText($"{path}/TagText", evidence != null ? "INTEL" : fallbackTag);
        SetText($"{path}/BodyText", evidence != null ? $"{FormatEvidenceStatus(evidence)} - {evidence.title}: {evidence.body}" : fallbackBody);
    }

    private static DistrictStateData HighestThreatDistrict(OperationSaveData state)
    {
        if (state?.districts == null || state.districts.Length == 0)
            return null;

        DistrictStateData highest = null;
        foreach (DistrictStateData district in state.districts)
        {
            if (district != null && (highest == null || district.threat > highest.threat))
                highest = district;
        }

        return highest;
    }
}
