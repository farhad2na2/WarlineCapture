using UnityEngine;
using UnityEngine.UI;

public sealed class BuildDrawerPanelView : MonoBehaviour
{
    [SerializeField] private GameObject drawerRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private BattleHudRuntimeFeedbackView runtimeFeedbackView;
    private bool _appliedBuildMode;

    public bool IsOpen => drawerRoot != null && drawerRoot.activeSelf;

    private void Awake()
    {
        if (runtimeFeedbackView == null)
            runtimeFeedbackView = GetComponent<BattleHudRuntimeFeedbackView>();

        if (openButton != null)
            openButton.onClick.AddListener(Open);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Close();
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (drawerRoot != null)
            drawerRoot.SetActive(true);
        ApplyBuildMode();
    }

    public void Close()
    {
        if (drawerRoot != null)
            drawerRoot.SetActive(false);
        ClearBuildMode();
    }

    public void Toggle()
    {
        if (drawerRoot == null)
            return;

        bool shouldOpen = !drawerRoot.activeSelf;
        drawerRoot.SetActive(shouldOpen);
        if (shouldOpen)
            ApplyBuildMode();
        else
            ClearBuildMode();
    }

    private void ApplyBuildMode()
    {
        BattleHudRuntimeFeedbackView view = runtimeFeedbackView != null ? runtimeFeedbackView : BattleHudRuntimeFeedbackSystem.ResolveActiveView();
        BattleHudRuntimeFeedbackSystem.ApplyCommandMode(view, TacticalCommandMode.Build);
        _appliedBuildMode = view != null;
    }

    private void ClearBuildMode()
    {
        if (!_appliedBuildMode)
            return;

        BattleHudRuntimeFeedbackView view = runtimeFeedbackView != null ? runtimeFeedbackView : BattleHudRuntimeFeedbackSystem.ResolveActiveView();
        BattleHudRuntimeFeedbackSystem.ClearCommandMode(view);
        _appliedBuildMode = false;
    }
}
