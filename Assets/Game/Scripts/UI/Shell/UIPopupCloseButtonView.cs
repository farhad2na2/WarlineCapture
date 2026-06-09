using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIPopupCloseButtonView : MonoBehaviour
{
    [SerializeField] private UIPopupCloseView closeView;

    public UIPopupCloseView CloseView => closeView;

    private void Awake()
    {
        if (closeView == null)
            closeView = GetComponent<UIPopupCloseView>();
    }

    private void OnEnable()
    {
        if (closeView == null)
            closeView = GetComponent<UIPopupCloseView>();

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
        }

        GameObject target = closeView != null && closeView.PopupRoot != null ? closeView.PopupRoot : gameObject;
        UIPopupMotionView motionView = target != null ? target.GetComponent<UIPopupMotionView>() : null;
        if (motionView != null && motionView.PlayHideAndDestroy(target))
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
