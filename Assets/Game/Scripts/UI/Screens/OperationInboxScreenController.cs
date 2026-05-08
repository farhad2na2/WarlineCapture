public sealed class OperationInboxScreenController : OperationLedgerScreenController
{
    protected override void Refresh()
    {
        OperationSaveData state = WarlineCaptureOperationRuntime.State;
        int eventCount = CountEvents(state);
        int unreadEvidenceCount = OperationIntelArchive.CountUnread(state);
        OperationIntelEvidenceData latestEvidence = OperationIntelArchive.Latest(state);

        SetText("HeroPanel/EyebrowText", "OPERATION REPORTS");
        SetText("HeroPanel/HeroTitleText", eventCount == 0 ? "INBOX READY" : $"{eventCount} FIELD REPORTS");
        SetText("HeroPanel/BodyText", eventCount == 0
            ? "Operation reports will appear here after district actions, scans, raids, and end-of-day pressure updates."
            : $"Day {state.operationDay} operation ledger is synced locally. {unreadEvidenceCount} unread intel evidence row(s) are ready for review.");
        SetText("HeroPanel/UnavailableButton/LabelText", "LOCAL LEDGER");

        BindCard(1, EventAt(state, 0), "LATEST REPORT", "No report yet", "Complete an Operation action to create a local report.");
        BindCard(2, EventAt(state, 1), "PREVIOUS REPORT", "No previous report", "Earlier district actions will appear here.");
        BindEvidenceCard(3, latestEvidence, "ARCHIVE REPORT", "No intel evidence", "Scan a district to archive evidence.");

        BindFeed(1, EventAt(state, 0), "OPERATION", "No Operation report has been generated yet.");
        BindFeed(2, EventAt(state, 1), "DISTRICT", $"Supplies {state.operationSupplies}. Completed actions {state.completedActions}.");
        BindEvidenceFeed(3, latestEvidence, "SYNC", "Operation reports and intel evidence are stored in the local split save.");

        SetText("ImplementationNotePanel/TitleText", "LIVE OPERATION INBOX");
        SetText("ImplementationNotePanel/BodyText", "This screen now reads pending Operation events from the saved Operation state.");
    }

    private void BindCard(int index, OperationEventData operationEvent, string fallbackTitle, string fallbackStatus, string fallbackBody)
    {
        string path = $"StatusCard_{index}";
        SetText($"{path}/TitleText", operationEvent != null ? operationEvent.title.ToUpperInvariant() : fallbackTitle);
        SetText($"{path}/StatusText", FormatEventStatus(operationEvent, fallbackStatus));
        SetText($"{path}/BodyText", operationEvent != null ? operationEvent.body : fallbackBody);
    }

    private void BindFeed(int index, OperationEventData operationEvent, string fallbackTag, string fallbackBody)
    {
        string path = $"FeedRow_{index}";
        SetText($"{path}/TagText", FormatEventTag(operationEvent, fallbackTag));
        SetText($"{path}/BodyText", operationEvent != null ? $"{operationEvent.title}: {operationEvent.body}" : fallbackBody);
    }

    private void BindEvidenceCard(int index, OperationIntelEvidenceData evidence, string fallbackTitle, string fallbackStatus, string fallbackBody)
    {
        string path = $"StatusCard_{index}";
        SetText($"{path}/TitleText", evidence != null ? evidence.title.ToUpperInvariant() : fallbackTitle);
        SetText($"{path}/StatusText", evidence != null ? FormatEvidenceStatus(evidence) : fallbackStatus);
        SetText($"{path}/BodyText", evidence != null ? evidence.body : fallbackBody);
    }

    private void BindEvidenceFeed(int index, OperationIntelEvidenceData evidence, string fallbackTag, string fallbackBody)
    {
        string path = $"FeedRow_{index}";
        SetText($"{path}/TagText", evidence != null ? "INTEL" : fallbackTag);
        SetText($"{path}/BodyText", evidence != null ? $"{evidence.title}: {evidence.body}" : fallbackBody);
    }
}
