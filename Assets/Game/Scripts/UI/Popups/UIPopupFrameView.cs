using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class UIPopupFrameView : MonoBehaviour
    {
        [SerializeField] private GameObject scrim;
        [SerializeField] private GameObject frame;
        [SerializeField] private GameObject header;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform buttonRow;

        public GameObject Scrim => scrim;
        public GameObject Frame => frame;
        public GameObject Header => header;
        public TMP_Text TitleText => titleText;
        public Button CloseButton => closeButton;
        public Transform BodyRoot => bodyRoot;
        public Transform ButtonRow => buttonRow;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Bind(string title)
        {
            if (titleText != null)
                titleText.text = title;
        }

        public void Show(string title)
        {
            Bind(title);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public void Configure(
            GameObject configuredScrim,
            GameObject configuredFrame,
            GameObject configuredHeader,
            TMP_Text configuredTitle,
            Button configuredClose,
            Transform configuredBody,
            Transform configuredButtonRow)
        {
            scrim = configuredScrim;
            frame = configuredFrame;
            header = configuredHeader;
            titleText = configuredTitle;
            closeButton = configuredClose;
            bodyRoot = configuredBody;
            buttonRow = configuredButtonRow;
        }
#endif
    }
}
