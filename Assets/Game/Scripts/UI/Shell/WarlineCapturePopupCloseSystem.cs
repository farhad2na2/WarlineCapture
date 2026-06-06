using UnityEngine;

[DisallowMultipleComponent]
public sealed class WarlineCapturePopupCloseSystem : MonoBehaviour
{
    [SerializeField] private WarlineCapturePopupCloseView closeView;

    public WarlineCapturePopupCloseView CloseView => closeView;

    private void Awake()
    {
        if (closeView == null)
            closeView = GetComponent<WarlineCapturePopupCloseView>();
    }

    private void OnEnable()
    {
        if (closeView == null)
            closeView = GetComponent<WarlineCapturePopupCloseView>();

        if (closeView != null && closeView.CloseButton != null)
            closeView.CloseButton.onClick.AddListener(ClosePopup);
    }

    private void OnDisable()
    {
        if (closeView != null && closeView.CloseButton != null)
            closeView.CloseButton.onClick.RemoveListener(ClosePopup);
    }

    public void ClosePopup()
    {
        if (closeView != null && closeView.CommandModeToClear != TacticalCommandMode.None)
        {
            BattleHudRuntimeFeedbackView view = BattleHudRuntimeFeedbackSystem.ResolveActiveView();
            if (view != null)
                BattleHudRuntimeFeedbackSystem.ClearStickyCommandMode(view, closeView.CommandModeToClear);
            else
                new MatchOverlayCommandTabFeedbackSystem().ClearCommandMode(null);
        }

        GameObject target = closeView != null && closeView.PopupRoot != null ? closeView.PopupRoot : gameObject;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
