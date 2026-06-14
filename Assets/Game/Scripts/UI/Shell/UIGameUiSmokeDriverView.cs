using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIGameUiSmokeDriverView : MonoBehaviour
{
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float loadingDurationSeconds = 2f;
    [SerializeField] private float stableHoldSeconds = 0.25f;

    private bool hasStarted;
    private bool isCompletingLoading;

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
        StartCoroutine(RunLoadingGate());
    }

    private IEnumerator RunLoadingGate()
    {
        yield return WaitForBoundary();

        while (isActiveAndEnabled)
        {
            if (!isCompletingLoading &&
                TryGetState(out UiShellStateModel state) &&
                state.CurrentMode == UiShellMode.Loading &&
                !state.IsTransitionRunning &&
                TryGetLoading(out UiShellLoadingProgressModel loading) &&
                !loading.IsComplete)
            {
                yield return AnimateLoadingToComplete();
            }

            yield return null;
        }
    }

    private IEnumerator AnimateLoadingToComplete()
    {
        float elapsed = 0f;
        while (elapsed < loadingDurationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / loadingDurationSeconds);
            isCompletingLoading = true;
            SetLoading(progress, "Loading command shell", progress >= 1f);
            yield return null;
        }

        SetLoading(1f, "Command shell ready", true);
        isCompletingLoading = false;
    }

    private IEnumerator WaitForBoundary()
    {
        while (!UiShellRuntimeGateway.TryReadShellState(out _))
            yield return null;
    }

    private void SetLoading(float progress01, string status, bool complete)
    {
        UiShellRuntimeGateway.TrySetLoadingProgress(progress01, status, complete);
    }

    private void EnqueueRoute(UiShellRouteIntent intent, UIRoute route)
    {
        UiShellRuntimeGateway.TryEnqueueRouteRequest(intent, route, pushHistory: false);
    }

    private bool TryGetState(out UiShellStateModel state)
    {
        return UiShellRuntimeGateway.TryReadShellState(out state);
    }

    private bool TryGetLoading(out UiShellLoadingProgressModel loading)
    {
        return UiShellRuntimeGateway.TryReadLoadingProgress(out loading);
    }
}
