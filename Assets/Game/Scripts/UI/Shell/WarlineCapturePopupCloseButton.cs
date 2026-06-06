using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WarlineCapturePopupCloseButton : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TacticalCommandMode commandModeToClear = TacticalCommandMode.None;

    public Button CloseButton => closeButton;
    public GameObject PopupRoot => popupRoot;
    public TacticalCommandMode CommandModeToClear => commandModeToClear;

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePopup);
    }

    public void ClosePopup()
    {
        if (commandModeToClear != TacticalCommandMode.None)
        {
            BattleHudGameplayBridge bridge = BattleHudGameplayBridge.ResolveActive();
            if (bridge != null)
                bridge.ClearStickyCommandMode(commandModeToClear);
            else
                new MatchOverlayCommandTabFeedbackSystem().ClearCommandMode(null);
        }

        GameObject target = popupRoot != null ? popupRoot : gameObject;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
