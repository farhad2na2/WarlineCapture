using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SplashScreenController : WarlineCaptureScreenController
{
    [SerializeField] private Image logoImage;
    [SerializeField] private Image loadingBarFill;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private WarlineCaptureLoadingTips loadingTips;
    [SerializeField] private float minimumVisibleSeconds = 1.25f;
    [SerializeField] private float fakeLoadingSeconds = 3f;
    [SerializeField] private WarlineCaptureRoute routeAfterFakeLoad = WarlineCaptureRoute.MainMenu;
    [SerializeField] private string defaultStatusText = "LOADING ASSETS...";

    private float _shownAt;
    private bool _loadComplete;
    private Coroutine _fakeLoadingRoutine;
    private WarlineCaptureRouter _router;

    private void Awake()
    {
        _router = GetComponentInParent<WarlineCaptureRouter>(true);
    }

    public override void Show()
    {
        base.Show();
        _shownAt = Time.unscaledTime;
        _loadComplete = false;
        SetProgress(0f);
        SetStatus(defaultStatusText);
        RefreshTip();
        StartFakeLoading();
    }

    public override void Hide()
    {
        StopFakeLoading();
        base.Hide();
    }

    private void OnDisable()
    {
        StopFakeLoading();
    }

    public void Bind(WarlineCaptureLoadingTips tips)
    {
        loadingTips = tips;
        RefreshTip();
    }

    public void SetProgress(float progress01)
    {
        float progress = Mathf.Clamp01(progress01);
        if (loadingBarFill != null)
            loadingBarFill.fillAmount = progress;

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
    }

    public void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status ?? string.Empty;
    }

    public void RefreshTip(int index = 0)
    {
        if (tipText == null)
            return;

        string tip = loadingTips != null ? loadingTips.GetTip(index) : string.Empty;
        tipText.text = string.IsNullOrWhiteSpace(tip) ? "Prepare your squads before entering hostile districts." : tip;
    }

    public bool CanLeaveSplash()
    {
        return _loadComplete && Time.unscaledTime - _shownAt >= minimumVisibleSeconds;
    }

    public void MarkLoadComplete()
    {
        _loadComplete = true;
        SetProgress(1f);
    }

    private void StartFakeLoading()
    {
        StopFakeLoading();
        if (isActiveAndEnabled)
            _fakeLoadingRoutine = StartCoroutine(FakeLoadingRoutine());
    }

    private void StopFakeLoading()
    {
        if (_fakeLoadingRoutine == null)
            return;

        StopCoroutine(_fakeLoadingRoutine);
        _fakeLoadingRoutine = null;
    }

    private IEnumerator FakeLoadingRoutine()
    {
        float duration = Mathf.Max(0.01f, fakeLoadingSeconds);
        float startTime = Time.unscaledTime;

        while (true)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - startTime) / duration);
            SetProgress(progress);
            SetStatus($"LOADING ASSETS... {Mathf.RoundToInt(progress * 100f)}%");

            if (progress >= 1f)
                break;

            yield return null;
        }

        _fakeLoadingRoutine = null;
        MarkLoadComplete();

        if (_router == null)
            _router = GetComponentInParent<WarlineCaptureRouter>(true);

        if (_router != null && _router.HasActiveRoute && _router.ActiveRoute == Route)
            _router.GoTo(routeAfterFakeLoad, false);
    }
}
