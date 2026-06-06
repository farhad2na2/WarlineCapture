using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WarlineCapturePopupCloseView : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TacticalCommandMode commandModeToClear = TacticalCommandMode.None;

    public Button CloseButton => closeButton;
    public GameObject PopupRoot => popupRoot;
    public TacticalCommandMode CommandModeToClear => commandModeToClear;
}
