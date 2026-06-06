using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class WarlineCaptureLegacyGameStartSystem : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        new ActiveMissionSession().Clear();
        WarlineCaptureGameLaunchUtility.StartExistingGameplayAndHideRouter(this);
    }
}
