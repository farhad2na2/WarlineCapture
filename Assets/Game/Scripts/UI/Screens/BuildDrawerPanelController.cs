using UnityEngine;
using UnityEngine.UI;

public sealed class BuildDrawerPanelController : MonoBehaviour
{
    [SerializeField] private GameObject drawerRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private BattleHudGameplayBridge gameplayBridge;
    private bool _appliedBuildMode;

    public bool IsOpen => drawerRoot != null && drawerRoot.activeSelf;

    private void Awake()
    {
        if (gameplayBridge == null)
            gameplayBridge = GetComponent<BattleHudGameplayBridge>();

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
        if (WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

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
        if (shouldOpen && WarlineCaptureMissionRules.TryRejectBuildForActiveMission())
            return;

        drawerRoot.SetActive(shouldOpen);
        if (shouldOpen)
            ApplyBuildMode();
        else
            ClearBuildMode();
    }

    private void ApplyBuildMode()
    {
        BattleHudGameplayBridge bridge = gameplayBridge != null ? gameplayBridge : BattleHudGameplayBridge.ResolveActive();
        bridge?.ApplyCommandMode(TacticalCommandMode.Build);
        _appliedBuildMode = bridge != null;
    }

    private void ClearBuildMode()
    {
        if (!_appliedBuildMode)
            return;

        BattleHudGameplayBridge bridge = gameplayBridge != null ? gameplayBridge : BattleHudGameplayBridge.ResolveActive();
        bridge?.ClearCommandMode();
        _appliedBuildMode = false;
    }
}
