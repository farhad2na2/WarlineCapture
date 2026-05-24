using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WarlineCaptureGameUiSmokeDriverView : MonoBehaviour
{
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float loadingDurationSeconds = 0.8f;
    [SerializeField] private float stableHoldSeconds = 0.25f;

    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;
    private bool hasStarted;

    public bool PlayOnStart => playOnStart;
    public float LoadingDurationSeconds => loadingDurationSeconds;
    public float StableHoldSeconds => stableHoldSeconds;

    public void Configure(bool autoPlay, float loadingDuration, float stableHold)
    {
        playOnStart = autoPlay;
        loadingDurationSeconds = Mathf.Max(0.01f, loadingDuration);
        stableHoldSeconds = Mathf.Max(0f, stableHold);
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        if (hasStarted)
            return;

        hasStarted = true;
        StartCoroutine(RunSmokeSequence());
    }

    private IEnumerator RunSmokeSequence()
    {
        yield return WaitForBoundary();
        yield return WaitForTransitionIdleOrPhase(UiShellTransitionPhase.ShowingLoading);
        yield return AnimateLoadingToComplete();
        yield return WaitForPhase(UiShellTransitionPhase.MenuReady);
        yield return Hold();

        EnqueueRoute(UiShellRouteIntent.EnterMatch, WarlineCaptureRoute.Match);
        yield return WaitForPhase(UiShellTransitionPhase.MatchHudReady);
        yield return Hold();

        EnqueuePopup(UiShellPopupIntent.Show);
        yield return WaitForPhase(UiShellTransitionPhase.PopupVisible);
        yield return Hold();

        EnqueuePopup(UiShellPopupIntent.Hide);
        yield return WaitForTransitionIdleOrPhase(UiShellTransitionPhase.Idle);
        yield return Hold();

        EnqueueRoute(UiShellRouteIntent.ReturnToMainMenu, WarlineCaptureRoute.MainMenu);
        yield return WaitForPhase(UiShellTransitionPhase.MenuReady);

        Debug.Log("WARLINECAPTURE_GAMEUI_STEP7_SMOKE_COMPLETE");
    }

    private IEnumerator AnimateLoadingToComplete()
    {
        float elapsed = 0f;
        while (elapsed < loadingDurationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / loadingDurationSeconds);
            SetLoading(progress, "Loading command shell", progress >= 1f);
            yield return null;
        }

        SetLoading(1f, "Command shell ready", true);
    }

    private IEnumerator WaitForBoundary()
    {
        while (!TryGetBoundary(out _, out _))
            yield return null;
    }

    private IEnumerator WaitForPhase(UiShellTransitionPhase phase)
    {
        while (!TryGetState(out UiShellStateComponent state) || state.Phase != phase || state.IsTransitionRunning != 0)
            yield return null;
    }

    private IEnumerator WaitForTransitionIdleOrPhase(UiShellTransitionPhase phase)
    {
        while (!TryGetState(out UiShellStateComponent state) || state.IsTransitionRunning != 0 || state.Phase != phase)
            yield return null;
    }

    private IEnumerator Hold()
    {
        if (stableHoldSeconds <= 0f)
            yield break;

        yield return new WaitForSecondsRealtime(stableHoldSeconds);
    }

    private void SetLoading(float progress01, string status, bool complete)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return;

        entityManager.SetComponentData(boundary, new UiShellLoadingProgressComponent
        {
            Progress01 = Mathf.Clamp01(progress01),
            Status = new FixedString64Bytes(status),
            IsComplete = complete ? (byte)1 : (byte)0
        });
    }

    private void EnqueueRoute(UiShellRouteIntent intent, WarlineCaptureRoute route)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return;

        DynamicBuffer<UiShellRouteRequestComponent> requests = entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        requests.Add(new UiShellRouteRequestComponent
        {
            Intent = intent,
            Route = route,
            PushHistory = 0
        });
    }

    private void EnqueuePopup(UiShellPopupIntent intent)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return;

        DynamicBuffer<UiShellPopupRequestComponent> requests = entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        requests.Add(new UiShellPopupRequestComponent
        {
            PopupKind = UiShellPopupKind.MissionResult,
            Intent = intent,
            PayloadId = 0
        });
    }

    private bool TryGetState(out UiShellStateComponent state)
    {
        state = default;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        state = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        return true;
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
            boundaryQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entityManager = world.EntityManager;
        boundary = boundaryQuery.GetSingletonEntity();
        return true;
    }
}
