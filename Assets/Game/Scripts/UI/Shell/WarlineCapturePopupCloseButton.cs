using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WarlineCapturePopupCloseButton : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject popupRoot;

    public Button CloseButton => closeButton;
    public GameObject PopupRoot => popupRoot;

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
        GameObject target = popupRoot != null ? popupRoot : gameObject;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
