using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WarlineCaptureMatchResultFlow : MonoBehaviour
{
    [SerializeField] private WarlineCaptureRouter router;
    [SerializeField] private Transform modalOverlay;
    [SerializeField] private MissionResultPopupSystem missionResultPopupPrefab;

    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private MissionResultPopupSystem _activePopup;
    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;

    public bool HasActivePopup => _activePopup != null && _activePopup.gameObject.activeInHierarchy;

    public void CompleteActiveMissionAndShowResult()
    {
        if (!new ActiveMissionSession().HasActiveMission)
            return;

        MissionResultData result = new ActiveMissionSession().BuildCurrentResult(GameRuntimeStats.GetSnapshot());
        RewardGrantResult[] rewards = ApplyRewardsAndPersist(new ActiveMissionSession().ActiveMission, result);
        result = result.WithRewards(rewards);
        SagaProgressStore.ApplyMissionResult(result);
        ShowResult(result, new ActiveMissionSession().ReturnRoute);
        new ActiveMissionSession().Clear();
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
        var runtimeGameplayStateSystem = new RuntimeGameplayStateSystem();
        runtimeGameplayStateSystem.PlayRequested = false;
        flow.gameObject.SetActive(true);
        flow.CompleteActiveMissionAndShowResult();
        return true;
    }

    public static bool CanCompleteActiveMissionFromLoadedScene()
    {
        if (!new ActiveMissionSession().HasActiveMission)
            return false;

        World world = World.DefaultGameObjectInjectionWorld;
        if (Chapter01M01PlayableRuntime.TryEvaluateActiveMission(world, out Chapter01M01PlayableRuntime.Evaluation m01Evaluation) &&
            (!m01Evaluation.CommandSquadAlive || !m01Evaluation.ObjectiveComplete))
        {
            return false;
        }

        MissionResultData preview = new ActiveMissionSession().BuildCurrentResult(GameRuntimeStats.GetSnapshot());
        return preview.Victory;
    }

    private void WirePopupButtons(MissionResultPopupSystem popup, WarlineCaptureRoute returnRoute)
    {
        Button continueButton = FindButton(popup.transform, "Frame/ButtonRow/ContinueButton");
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                CloseActivePopup();
                QueueReturnToMenu();
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

    private void QueueReturnToMenu()
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
        {
            Debug.LogError("[MatchResult] Cannot return to menu because the UI shell boundary is missing.");
            return;
        }

        DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        routeRequests.Add(new UiShellRouteRequestComponent
        {
            Intent = UiShellRouteIntent.ReturnToMainMenu,
            Route = WarlineCaptureRoute.MainMenu,
            PushHistory = 0
        });

        if (sceneLifecycleSystem.QueueUnloadMatch(entityManager))
            Debug.Log("[MatchResult] submitted Match scene unload request.");
        else
            Debug.LogError("[MatchResult] failed to submit Match scene unload request.");
    }

    private bool TryGetBoundary(out EntityManager entityManager, out Entity boundary)
    {
        entityManager = default;
        boundary = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world || !hasBoundaryQuery)
        {
            cachedWorld = world;
            boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entityManager = world.EntityManager;
        boundary = boundaryQuery.GetSingletonEntity();
        return true;
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
