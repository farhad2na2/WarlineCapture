using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SplashScreenSystem : UIScreenSystem
{
    [SerializeField] private Image logoImage;
    [SerializeField] private Image loadingBarFill;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private UILoadingTips loadingTips;
    [SerializeField] private string defaultStatusText = "LOADING ASSETS...";

    private bool _loadComplete;

    public override void Show()
    {
        base.Show();
        _loadComplete = false;
        SetProgress(0f);
        SetStatus(defaultStatusText);
        RefreshTip();
    }

    public override void Hide()
    {
        base.Hide();
    }

    public void Bind(UILoadingTips tips)
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
        return _loadComplete;
    }

    public void MarkLoadComplete()
    {
        _loadComplete = true;
        SetProgress(1f);
    }
}
