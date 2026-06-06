using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SagaMapScreenSystem : MonoBehaviour
{
    [SerializeField] private string defaultSelectedMissionId = "saga.ch01.m02.establish_base";
    [SerializeField] private WarlineCaptureSagaMissionNodeMetadata[] missionNodes = Array.Empty<WarlineCaptureSagaMissionNodeMetadata>();
    [SerializeField] private TMP_Text selectedTitleText;
    [SerializeField] private TMP_Text selectedBodyText;
    [SerializeField] private TMP_Text selectedStatusText;
    [SerializeField] private TMP_Text selectedStarsText;
    [SerializeField] private Sprite availableNodeSprite;
    [SerializeField] private Sprite lockedNodeSprite;
    [SerializeField] private Sprite selectedNodeSprite;

    private bool _buttonsBound;
    private bool _nodesRefreshing;
    private string _selectedMissionId;

    private void Awake()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        BindButtons();
        RefreshNodeStates();
        SelectMission(string.IsNullOrWhiteSpace(_selectedMissionId) ? defaultSelectedMissionId : _selectedMissionId);
    }

    public void RefreshForTests()
    {
        BindButtons();
        RefreshNodeStates();
        SelectMission(string.IsNullOrWhiteSpace(_selectedMissionId) ? defaultSelectedMissionId : _selectedMissionId);
    }

    public void SelectMissionForTests(string missionId)
    {
        SelectMission(missionId);
    }

    private void BindButtons()
    {
        if (_buttonsBound)
            return;

        if (missionNodes == null || missionNodes.Length == 0)
            missionNodes = GetComponentsInChildren<WarlineCaptureSagaMissionNodeMetadata>(true);

        for (int i = 0; i < missionNodes.Length; i++)
        {
            WarlineCaptureSagaMissionNodeMetadata metadata = missionNodes[i];
            if (metadata == null)
                continue;

            Button button = metadata.GetComponent<Button>();
            if (button == null)
                continue;

            button.onClick.AddListener(() => HandleNodeClick(metadata));
        }

        _buttonsBound = true;
    }

    private void RefreshNodeStates()
    {
        if (missionNodes == null)
            return;

        _nodesRefreshing = true;
        for (int i = 0; i < missionNodes.Length; i++)
        {
            WarlineCaptureSagaMissionNodeMetadata metadata = missionNodes[i];
            if (metadata == null)
                continue;

            bool unlocked = IsNodeUnlocked(metadata);
            Button button = metadata.GetComponent<Button>();
            if (button != null)
                button.interactable = true;

            Image background = metadata.GetComponent<Image>();
            if (background != null)
                background.sprite = unlocked ? GetUnlockedSprite(metadata) : lockedNodeSprite;

            Transform lockIcon = metadata.transform.Find("LockIcon");
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!unlocked);

            Transform starIcon = metadata.transform.Find("StarIcon");
            if (starIcon != null)
                starIcon.gameObject.SetActive(unlocked);
        }

        _nodesRefreshing = false;
    }

    private void HandleNodeClick(WarlineCaptureSagaMissionNodeMetadata metadata)
    {
        if (metadata == null)
            return;

        RefreshNodeStates();
        SelectMission(metadata.MissionId);
        if (!IsNodeUnlocked(metadata))
            return;

        if (HasExplicitRoute(metadata))
            return;

        WarlineCaptureMissionSession.BeginMission(metadata.MissionId, WarlineCaptureRoute.SagaMap);
        WarlineCaptureRouter router = FindRouter();
        if (router != null)
            router.GoTo(WarlineCaptureRoute.MissionBriefing);
    }

    private void SelectMission(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId))
            return;

        MissionConfig mission = ChapterOneMissionCatalog.GetMission(missionId);
        WarlineCaptureSagaMissionNodeMetadata metadata = FindMetadata(missionId);
        _selectedMissionId = missionId;

        int missionIndex = metadata != null && metadata.MissionIndex > 0 ? metadata.MissionIndex : FindMissionIndex(missionId);
        string titlePrefix = missionIndex > 0 ? $"1-{missionIndex} " : string.Empty;
        SetText(selectedTitleText, $"SELECTED: {titlePrefix}{mission.DisplayName.ToUpperInvariant()}");

        ObjectiveConfig primaryObjective = mission.Objectives.Length > 0 ? mission.Objectives[0] : null;
        string body = primaryObjective != null
            ? $"Primary: {primaryObjective.DisplayName}"
            : "Primary objective will be assigned during mission setup.";
        SetText(selectedBodyText, body);

        RefreshSelectedVisuals(metadata);

        bool locked = metadata != null && !IsNodeUnlocked(metadata);
        if (locked)
            SetText(selectedStatusText, metadata.LockedReason);
        else if (SagaProgressStore.IsCompleted(missionId))
            SetText(selectedStatusText, "COMPLETED");
        else
            SetText(selectedStatusText, "AVAILABLE");

        SetText(selectedStarsText, $"{SagaProgressStore.GetStars(missionId)} / 3 STARS");
    }

    private void RefreshSelectedVisuals(WarlineCaptureSagaMissionNodeMetadata selectedMetadata)
    {
        if (_nodesRefreshing)
            return;

        RefreshNodeStates();
        if (selectedMetadata == null || !IsNodeUnlocked(selectedMetadata))
            return;

        Image background = selectedMetadata.GetComponent<Image>();
        if (background != null && selectedNodeSprite != null)
            background.sprite = selectedNodeSprite;
    }

    private bool IsNodeUnlocked(WarlineCaptureSagaMissionNodeMetadata metadata)
    {
        if (metadata == null)
            return false;

        if (metadata.MissionIndex <= 2)
            return true;

        MissionConfig previousMission = GetMissionByIndex(metadata.MissionIndex - 1);
        return previousMission != null && SagaProgressStore.IsCompleted(previousMission.MissionId);
    }

    private Sprite GetUnlockedSprite(WarlineCaptureSagaMissionNodeMetadata metadata)
    {
        if (metadata != null && metadata.MissionId == _selectedMissionId && selectedNodeSprite != null)
            return selectedNodeSprite;

        return availableNodeSprite;
    }

    private static bool HasExplicitRoute(WarlineCaptureSagaMissionNodeMetadata metadata)
    {
        return metadata != null
            && metadata.GetComponent<ScreenRouteSystem>() != null
            && metadata.GetComponent<WarlineCaptureMissionSessionSystem>() != null;
    }

    private WarlineCaptureSagaMissionNodeMetadata FindMetadata(string missionId)
    {
        if (missionNodes == null)
            return null;

        for (int i = 0; i < missionNodes.Length; i++)
        {
            WarlineCaptureSagaMissionNodeMetadata metadata = missionNodes[i];
            if (metadata != null && metadata.MissionId == missionId)
                return metadata;
        }

        return null;
    }

    private static int FindMissionIndex(string missionId)
    {
        for (int i = 0; i < ChapterOneMissionCatalog.All.Count; i++)
        {
            if (ChapterOneMissionCatalog.All[i].MissionId == missionId)
                return i + 1;
        }

        return 0;
    }

    private static MissionConfig GetMissionByIndex(int missionIndex)
    {
        int zeroBased = missionIndex - 1;
        return zeroBased >= 0 && zeroBased < ChapterOneMissionCatalog.All.Count
            ? ChapterOneMissionCatalog.All[zeroBased]
            : null;
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

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
