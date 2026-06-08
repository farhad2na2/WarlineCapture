using UnityEngine;
using UnityEngine.UI;

public sealed class CommandWheelPanelView : MonoBehaviour
{
    [SerializeField] private GameObject wheelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button scrimButton;
    [SerializeField] private BattleHudRuntimeFeedbackView runtimeFeedbackView;
    private bool _appliedSpecialMode;

    public bool IsOpen => wheelRoot != null && wheelRoot.activeSelf;

    private void Awake()
    {
        if (runtimeFeedbackView == null)
            runtimeFeedbackView = GetComponent<BattleHudRuntimeFeedbackView>();

        if (openButton != null)
            openButton.onClick.AddListener(Open);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (scrimButton != null)
            scrimButton.onClick.AddListener(Close);

        Close();
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (scrimButton != null)
            scrimButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (wheelRoot != null)
            wheelRoot.SetActive(true);
        ApplySpecialMode();
    }

    public void Close()
    {
        if (wheelRoot != null)
            wheelRoot.SetActive(false);
        ClearSpecialMode();
    }

    public void Toggle()
    {
        if (wheelRoot == null)
            return;

        bool shouldOpen = !wheelRoot.activeSelf;
        wheelRoot.SetActive(shouldOpen);
        if (shouldOpen)
            ApplySpecialMode();
        else
            ClearSpecialMode();
    }

    private void ApplySpecialMode()
    {
        BattleHudRuntimeFeedbackView view = runtimeFeedbackView != null ? runtimeFeedbackView : BattleHudRuntimeFeedbackSystem.ResolveActiveView();
        BattleHudRuntimeFeedbackSystem.ApplyCommandMode(view, TacticalCommandMode.Special);
        _appliedSpecialMode = view != null;
    }

    private void ClearSpecialMode()
    {
        if (!_appliedSpecialMode)
            return;

        BattleHudRuntimeFeedbackView view = runtimeFeedbackView != null ? runtimeFeedbackView : BattleHudRuntimeFeedbackSystem.ResolveActiveView();
        BattleHudRuntimeFeedbackSystem.ClearCommandMode(view);
        _appliedSpecialMode = false;
    }
}
