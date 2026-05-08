public sealed class OperationCommandFeedScreenController : OperationLedgerScreenController
{
    protected override void Refresh()
    {
        OperationSaveData state = WarlineCaptureOperationRuntime.State;
        OperationEventData latest = EventAt(state, 0);
        OperationIntelEvidenceData latestEvidence = OperationIntelArchive.Latest(state);
        int eventCount = CountEvents(state);

        SetText("HeroPanel/EyebrowText", "LOCAL COMMAND FEED");
        SetText("HeroPanel/HeroTitleText", eventCount == 0 ? "LOCAL FEED ACTIVE" : $"{eventCount} LOCAL UPDATES");
        SetText("HeroPanel/BodyText", latest != null
            ? $"{latest.title}: {latest.body}"
            : "Parallel UI shell is active. Operation reports, rewards, and system notices will post here.");
        SetText("HeroPanel/UnavailableButton/LabelText", "LOCAL ONLY");

        SetText("StatusCard_1/TitleText", "SYSTEM NOTICES");
        SetText("StatusCard_1/StatusText", "ONLINE");
        SetText("StatusCard_1/BodyText", "Parallel UI shell initialized and route state is local.");

        SetText("StatusCard_2/TitleText", "OPERATION FEED");
        SetText("StatusCard_2/StatusText", latest != null ? FormatEventStatus(latest, "NO REPORTS") : "NO REPORTS");
        SetText("StatusCard_2/BodyText", latest != null ? latest.body : "Operation reports appear after district actions.");

        SetText("StatusCard_3/TitleText", "INTEL ARCHIVE");
        SetText("StatusCard_3/StatusText", latestEvidence != null ? FormatEvidenceStatus(latestEvidence) : "NO INTEL");
        SetText("StatusCard_3/BodyText", latestEvidence != null ? latestEvidence.body : "Scan actions will archive district evidence.");

        BindFeed(1, latest, "SYSTEM", "Parallel UI shell initialized.");
        BindFeed(2, EventAt(state, 1), "OPERATION", $"Supplies {state.operationSupplies}. Completed actions {state.completedActions}.");
        BindEvidenceFeed(3, latestEvidence, "INTEL", "No intel evidence archived yet.");

        SetText("ImplementationNotePanel/TitleText", "LIVE LOCAL FEED");
        SetText("ImplementationNotePanel/BodyText", "Command Feed now mirrors recent Operation reports and local system notices.");
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
        SetText($"{path}/BodyText", evidence != null ? $"{evidence.title}: {evidence.body}" : fallbackBody);
    }
}
