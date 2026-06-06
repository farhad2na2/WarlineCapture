using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DistrictDetailScreenSystem : MonoBehaviour
{
    private const string RaidMissionId = "saga.ch01.m05.breach_assault";
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

        BindAction("StatusCard_1", OperationActionType.Patrol);
        BindAction("StatusCard_2", OperationActionType.Scan);
        BindAction("StatusCard_3", OperationActionType.Raid);
        BindAction("StatusCard_4", OperationActionType.Repair);
        BindAction("StatusCard_5", OperationActionType.Evacuate);
        BindAction("StatusCard_6", OperationActionType.BuildOutpost);

        Button raidButton = Find<Button>("HeroPanel/UnavailableButton");
        if (raidButton != null)
        {
            raidButton.interactable = true;
            raidButton.onClick.AddListener(() => ApplyAction(OperationActionType.Raid));
        }

        _buttonsBound = true;
    }

    private void BindAction(string path, OperationActionType actionType)
    {
        Button button = Find<Button>(path);
        if (button != null)
            button.onClick.AddListener(() => ApplyAction(actionType));
    }

    private void Refresh()
    {
        DistrictStateData district = WarlineCaptureOperationRuntime.SelectedDistrict;
        string districtName = OperationDashboardScreenSystem.FormatDistrictName(district.districtId);

        SetText("HeroPanel/EyebrowText", "DISTRICT ACTIONS");
        SetText("HeroPanel/HeroTitleText", districtName);
        SetText("HeroPanel/BodyText", OperationMetricText.FormatDistrictHero(district, WarlineCaptureOperationRuntime.State.operationSupplies));
        SetText("HeroPanel/UnavailableButton/LabelText", "RUN RAID");

        SetText("StatusCard_1/TitleText", "PATROL");
        SetText("StatusCard_1/StatusText", "THREAT -5");
        SetText("StatusCard_1/BodyText", "Send ground patrols to reduce visible threat pressure.");

        SetText("StatusCard_2/TitleText", "DRONE SCAN");
        SetText("StatusCard_2/StatusText", "INTEL +12 / SUPPLY -1");
        SetText("StatusCard_2/BodyText", "Reveal stronger intel before committing a raid team.");

        SetText("StatusCard_3/TitleText", "RAID");
        SetText("StatusCard_3/StatusText", "MISSION / SUPPLY -2");
        SetText("StatusCard_3/BodyText", "Starts the Breach Assault mission briefing from this district.");

        SetText("StatusCard_4/TitleText", "REPAIR");
        SetText("StatusCard_4/StatusText", "INFRA +12 / SUPPLY -1");
        SetText("StatusCard_4/BodyText", "Restore critical services and reduce civilian exposure.");

        SetText("StatusCard_5/TitleText", "EVACUATE");
        SetText("StatusCard_5/StatusText", "RISK -15 / SUPPLY -1");
        SetText("StatusCard_5/BodyText", "Move civilians out of danger at a trust cost.");

        SetText("StatusCard_6/TitleText", "BUILD OUTPOST");
        SetText("StatusCard_6/StatusText", "SECURITY +14 / SUPPLY -2");
        SetText("StatusCard_6/BodyText", "Create forward response coverage and reduce influence.");

        SetText("FeedRow_1/TagText", "TRUST");
        SetText("FeedRow_1/BodyText", $"{districtName}. {OperationMetricText.FormatPrimaryLine(district)}. {OperationMetricText.FormatIntelLine(district)}.");
        SetText("FeedRow_2/TagText", "SECURITY");
        SetText("FeedRow_2/BodyText", OperationMetricText.FormatPressureLine(district));
        SetText("FeedRow_3/TagText", "INTEL");
        SetText("FeedRow_3/BodyText", $"Intel confidence {district.intel}. Civilian risk {district.civilianRisk}. Scans improve raid certainty.");
        SetText("ImplementationNotePanel/TitleText", "LIVE DISTRICT STATE");
        SetText("ImplementationNotePanel/BodyText", "Actions mutate OperationService state. Raid seeds Chapter 1; support actions tune trust, security, infrastructure, heat, influence, and civilian risk.");
    }

    private void ApplyAction(OperationActionType actionType)
    {
        if (actionType == OperationActionType.Raid)
        {
            ShowRaidConfirmation();
            return;
        }

        OperationActionResult result = WarlineCaptureOperationRuntime.ApplyAction(actionType);
        Refresh();

        if (actionType == OperationActionType.Scan)
        {
            WarlineCaptureOperationModalFlow modalFlow = GetComponentInParent<WarlineCaptureOperationModalFlow>();
            if (modalFlow != null)
                modalFlow.ShowIntelReveal(WarlineCaptureOperationRuntime.SelectedDistrict, Refresh);
        }
    }

    private void ShowRaidConfirmation()
    {
        WarlineCaptureOperationModalFlow modalFlow = GetComponentInParent<WarlineCaptureOperationModalFlow>();
        if (modalFlow != null)
        {
            modalFlow.ShowConfirmRaid(WarlineCaptureOperationRuntime.SelectedDistrict, ExecuteRaid);
            return;
        }

        ExecuteRaid();
    }

    private void ExecuteRaid()
    {
        OperationActionResult result = WarlineCaptureOperationRuntime.ApplyAction(OperationActionType.Raid);
        Refresh();
        if (!result.StartsRaidMission)
            return;

        WarlineCaptureMissionSession.BeginMission(RaidMissionId, WarlineCaptureRoute.OperationDashboard);
        WarlineCaptureRouter router = FindRouter();
        if (router != null)
            router.GoTo(WarlineCaptureRoute.MissionBriefing);
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
}
