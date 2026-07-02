using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [RequireComponent(typeof(Button))]
    public sealed class UIGameStartButtonView : MonoBehaviour
    {
        private Button _button;
        private IMatchLaunchCommand _launchCommand;

        public void BindMatchLaunchCommand(IMatchLaunchCommand launchCommand)
        {
            _launchCommand = launchCommand;
        }

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
            _launchCommand?.LaunchMatch(this);
        }
    }
}
