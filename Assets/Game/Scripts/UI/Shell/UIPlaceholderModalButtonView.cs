using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [RequireComponent(typeof(Button))]
    public sealed class UIPlaceholderModalButtonView : MonoBehaviour
    {
        [SerializeField] private UIModalView modalController;
        [SerializeField] private string title;
        [SerializeField] private string body;

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
            if (modalController == null)
                modalController = GetComponentInParent<UIModalView>();

            modalController?.ShowPlaceholder(title, body);
        }
    }
}
