using TMPro;
using UnityEngine;

public abstract class OperationLedgerScreenController : MonoBehaviour
{
    public void RefreshForTests()
    {
        Refresh();
    }

    protected virtual void OnEnable()
    {
        Refresh();
    }

    protected abstract void Refresh();

    protected static OperationEventData EventAt(OperationSaveData state, int newestFirstIndex)
    {
        if (state?.pendingEvents == null || newestFirstIndex < 0)
            return null;

        int found = 0;
        for (int i = state.pendingEvents.Length - 1; i >= 0; i--)
        {
            OperationEventData operationEvent = state.pendingEvents[i];
            if (operationEvent == null)
                continue;

            if (found == newestFirstIndex)
                return operationEvent;

            found++;
        }

        return null;
    }

    protected static int CountEvents(OperationSaveData state)
    {
        if (state?.pendingEvents == null)
            return 0;

        int count = 0;
        foreach (OperationEventData operationEvent in state.pendingEvents)
        {
            if (operationEvent != null)
                count++;
        }

        return count;
    }

    protected static string FormatEventTag(OperationEventData operationEvent, string fallback)
    {
        return operationEvent != null ? operationEvent.category.ToString().ToUpperInvariant() : fallback;
    }

    protected static string FormatEventStatus(OperationEventData operationEvent, string fallback)
    {
        if (operationEvent == null)
            return fallback;

        string unread = operationEvent.unread ? "UNREAD" : "READ";
        if (!string.IsNullOrWhiteSpace(operationEvent.sourceMetric) && operationEvent.metricValue > 0)
            return $"{operationEvent.severity.ToString().ToUpperInvariant()} / {operationEvent.sourceMetric.ToUpperInvariant()} {operationEvent.metricValue} / {unread}";

        return $"{operationEvent.severity.ToString().ToUpperInvariant()} / {unread}";
    }

    protected static string FormatEvidenceStatus(OperationIntelEvidenceData evidence, string fallback = "NO INTEL")
    {
        if (evidence == null)
            return fallback;

        string unread = evidence.unread ? "UNREAD" : "READ";
        return $"{evidence.confidence}% / {unread}";
    }

    protected void SetText(string path, string value)
    {
        TMP_Text text = Find<TMP_Text>(path);
        if (text != null)
            text.text = value;
    }

    protected T Find<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }
}
