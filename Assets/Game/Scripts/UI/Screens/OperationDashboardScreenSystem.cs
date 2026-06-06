using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OperationDashboardScreenSystem : MonoBehaviour
{
    private static readonly string[] DistrictIds = { "north_bridge", "old_market", "port_breach" };
    private bool _buttonsBound;

    private void Awake()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        BindButtons();
        Refresh();
    }

    public void RefreshForTests()
    {
        BindButtons();
        Refresh();
    }

    private void BindButtons()
    {
        if (_buttonsBound)
            return;

        for (int i = 0; i < DistrictIds.Length; i++)
        {
            string districtId = DistrictIds[i];
            Button button = Find<Button>($"StatusCard_{i + 1}");
            if (button != null)
                button.onClick.AddListener(() => OpenDistrict(districtId));
        }

        Button endDayButton = Find<Button>("HeroPanel/UnavailableButton");
        if (endDayButton != null)
        {
            endDayButton.interactable = true;
            endDayButton.onClick.AddListener(HandleEndDay);
        }

        _buttonsBound = true;
    }

    private void Refresh()
    {
        OperationSaveData state = WarlineCaptureOperationRuntime.State;
        SetText("HeroPanel/EyebrowText", "PERSISTENT OPERATION");
        SetText("HeroPanel/HeroTitleText", $"DAY {state.operationDay} CITY PRESSURE");
        SetText("HeroPanel/BodyText", $"Live district state is active. Supplies {state.operationSupplies}. Select a district to inspect tactical actions, or resolve the next operation day.");
        SetText("HeroPanel/UnavailableButton/LabelText", "END DAY");

        for (int i = 0; i < DistrictIds.Length; i++)
        {
            DistrictStateData district = WarlineCaptureOperationRuntime.FindDistrict(DistrictIds[i]);
            string path = $"StatusCard_{i + 1}";
            SetText($"{path}/TitleText", FormatDistrictName(district.districtId));
            SetText($"{path}/StatusText", OperationMetricText.FormatDistrictStatus(district));
            SetText($"{path}/BodyText", OperationMetricText.FormatDashboardSummary(district));
        }

        DistrictStateData hotDistrict = FindHighestThreatDistrict(state);
        OperationEventData latestEvent = LatestEvent(state);
        SetText("FeedRow_1/TagText", latestEvent != null ? "EVENT" : "DAY");
        SetText("FeedRow_1/BodyText", latestEvent != null ? $"{latestEvent.title}: {latestEvent.body}" : $"Operation day {state.operationDay}. Passive pressure rises when the day advances.");
        SetText("FeedRow_2/TagText", "HOT ZONE");
        SetText("FeedRow_2/BodyText", $"{FormatDistrictName(hotDistrict.districtId)}. {OperationMetricText.FormatPressureLine(hotDistrict)}. {OperationMetricText.FormatPrimaryLine(hotDistrict)}.");
        SetText("FeedRow_3/TagText", "NEXT");
        SetText("FeedRow_3/BodyText", $"Patrol, scan, aid, repair, evacuate, outpost, or raid from District Detail. Completed actions {state.completedActions}.");
        SetText("ImplementationNotePanel/TitleText", "LIVE OPERATION STATE");
        SetText("ImplementationNotePanel/BodyText", "OperationService is bound. District cards expose all secondary metrics; End Day applies city pressure.");
    }

    private void OpenDistrict(string districtId)
    {
        WarlineCaptureOperationRuntime.SelectDistrict(districtId);
        WarlineCaptureRouter router = FindRouter();
        if (router != null)
            router.GoTo(WarlineCaptureRoute.DistrictDetail);
    }

    private void HandleEndDay()
    {
        WarlineCaptureOperationRuntime.EndDay();
        Refresh();
        WarlineCaptureOperationModalFlow modalFlow = GetComponentInParent<WarlineCaptureOperationModalFlow>();
        if (modalFlow != null)
            modalFlow.ShowEndOfDayReport(WarlineCaptureOperationRuntime.State, Refresh);
    }

    private static DistrictStateData FindHighestThreatDistrict(OperationSaveData state)
    {
        DistrictStateData highest = state.districts[0];
        foreach (DistrictStateData district in state.districts)
        {
            if (district != null && district.threat > highest.threat)
                highest = district;
        }

        return highest;
    }

    private static OperationEventData LatestEvent(OperationSaveData state)
    {
        if (state?.pendingEvents == null)
            return null;

        for (int i = state.pendingEvents.Length - 1; i >= 0; i--)
        {
            if (state.pendingEvents[i] != null)
                return state.pendingEvents[i];
        }

        return null;
    }

    private void SetText(string path, string value)
    {
        TMP_Text text = Find<TMP_Text>(path);
        if (text != null)
            text.text = value;
    }

    private T Find<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private WarlineCaptureRouter FindRouter()
    {
        WarlineCaptureRouter rootRouter = transform.root.GetComponent<WarlineCaptureRouter>();
        if (rootRouter != null)
            return rootRouter;

        WarlineCaptureRouter router = GetComponentInParent<WarlineCaptureRouter>();
        if (router != null)
            return router;

        foreach (WarlineCaptureRouter candidate in Resources.FindObjectsOfTypeAll<WarlineCaptureRouter>())
        {
            if (candidate != null && candidate.isActiveAndEnabled)
                return candidate;
        }

        return null;
    }

    public static string FormatDistrictName(string districtId)
    {
        return districtId switch
        {
            "north_bridge" => "NORTH BRIDGE",
            "old_market" => "OLD MARKET",
            "port_breach" => "PORT BREACH",
            _ => districtId.Replace('_', ' ').ToUpperInvariant()
        };
    }
}
