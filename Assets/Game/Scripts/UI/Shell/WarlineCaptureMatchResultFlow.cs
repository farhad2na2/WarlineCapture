using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WarlineCaptureMatchResultFlow : MonoBehaviour
{
    [SerializeField] private WarlineCaptureRouter router;
    [SerializeField] private Transform modalOverlay;
    [SerializeField] private MissionResultPopupController missionResultPopupPrefab;

    private MissionResultPopupController _activePopup;

    public bool HasActivePopup => _activePopup != null && _activePopup.gameObject.activeInHierarchy;

    public void CompleteActiveMissionAndShowResult()
    {
        if (!WarlineCaptureMissionSession.HasActiveMission)
            return;

        MissionResultData result = WarlineCaptureMissionSession.BuildCurrentResult(GameRuntimeStats.GetSnapshot());
        RewardGrantResult[] rewards = ApplyRewardsAndPersist(WarlineCaptureMissionSession.ActiveMission, result);
        result = result.WithRewards(rewards);
        SagaProgressStore.ApplyMissionResult(result);
        ShowResult(result, WarlineCaptureMissionSession.ReturnRoute);
        WarlineCaptureMissionSession.Clear();
    }

    public void ShowResult(MissionResultData result, WarlineCaptureRoute returnRoute)
    {
        if (result == null)
            return;

        if (router == null)
            router = GetComponent<WarlineCaptureRouter>();

        if (router != null)
            router.GoTo(WarlineCaptureRoute.Match, false);

        if (modalOverlay == null)
            modalOverlay = transform;

        modalOverlay.gameObject.SetActive(true);

        if (_activePopup != null)
            DestroyPopup(_activePopup.gameObject);

        _activePopup = missionResultPopupPrefab != null
            ? Instantiate(missionResultPopupPrefab, modalOverlay, false)
            : null;

        if (_activePopup == null)
            return;

        RectTransform popupRect = _activePopup.transform as RectTransform;
        if (popupRect != null)
        {
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;
        }

        WirePopupButtons(_activePopup, returnRoute);
        _activePopup.Show(result);
    }

    public static bool TryCompleteActiveMissionFromLoadedScene()
    {
        if (!CanCompleteActiveMissionFromLoadedScene())
            return false;

        WarlineCaptureMatchResultFlow flow = FindLoadedFlow();
        if (flow == null)
            return false;

        SetLegacyCanvasActive(false, flow.gameObject.scene);
        InitialUnitsRuntimeState.PlayRequested = false;
        flow.gameObject.SetActive(true);
        flow.CompleteActiveMissionAndShowResult();
        return true;
    }

    public static bool CanCompleteActiveMissionFromLoadedScene()
    {
        if (!WarlineCaptureMissionSession.HasActiveMission)
            return false;

        World world = World.DefaultGameObjectInjectionWorld;
        if (Chapter01M01PlayableRuntime.TryEvaluateActiveMission(world, out Chapter01M01PlayableRuntime.Evaluation m01Evaluation) &&
            (!m01Evaluation.CommandSquadAlive || !m01Evaluation.ObjectiveComplete))
        {
            return false;
        }

        MissionResultData preview = WarlineCaptureMissionSession.BuildCurrentResult(GameRuntimeStats.GetSnapshot());
        return preview.Victory;
    }

    private void WirePopupButtons(MissionResultPopupController popup, WarlineCaptureRoute returnRoute)
    {
        Button continueButton = FindButton(popup.transform, "Frame/ButtonRow/ContinueButton");
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                CloseActivePopup();
                if (router != null)
                    router.GoTo(returnRoute, false);
            });
        }

        Button replayButton = FindButton(popup.transform, "Frame/ButtonRow/ReplayButton");
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() =>
            {
                CloseActivePopup();
                WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter(this);
            });
        }
    }

    private void CloseActivePopup()
    {
        if (_activePopup != null)
            DestroyPopup(_activePopup.gameObject);

        _activePopup = null;
        if (modalOverlay != null)
            modalOverlay.gameObject.SetActive(false);
    }

    private static RewardGrantResult[] ApplyRewardsAndPersist(MissionConfig mission, MissionResultData result)
    {
        if (mission == null || result == null)
            return System.Array.Empty<RewardGrantResult>();

        SaveService saveService = SaveService.CreateDefault();
        WarlineCaptureSaveData saveData = saveService.LoadProject();
        RewardGrantResult[] rewards = RewardService.GrantMissionRewards(saveData, mission, result);
        MissionHistoryService.RecordResult(saveData.profile, result.WithRewards(rewards));
        saveService.SaveProject(saveData);
        return rewards;
    }

    private static Button FindButton(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static void DestroyPopup(GameObject popup)
    {
        if (popup == null)
            return;

        if (Application.isPlaying)
            Destroy(popup);
        else
            DestroyImmediate(popup);
    }

    private static WarlineCaptureMatchResultFlow FindLoadedFlow()
    {
        foreach (WarlineCaptureMatchResultFlow flow in Resources.FindObjectsOfTypeAll<WarlineCaptureMatchResultFlow>())
        {
            if (flow != null && flow.gameObject.scene.IsValid() && flow.gameObject.scene.isLoaded)
                return flow;
        }

        return null;
    }

    private static void SetLegacyCanvasActive(bool active, Scene preferredScene)
    {
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded || gameObject.name != "UI_Canvas")
                continue;

            if (!preferredScene.IsValid() || scene == preferredScene)
                gameObject.SetActive(active);
        }
    }
}
