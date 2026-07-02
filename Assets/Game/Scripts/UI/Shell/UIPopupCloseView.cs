using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class UIPopupCloseView : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TacticalCommandMode commandModeToClear = TacticalCommandMode.None;

        public Button CloseButton => closeButton;
        public GameObject PopupRoot => popupRoot;
        public TacticalCommandMode CommandModeToClear => commandModeToClear;
    }
}
