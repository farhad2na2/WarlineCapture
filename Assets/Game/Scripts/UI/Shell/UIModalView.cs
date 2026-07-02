using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class UIModalView : MonoBehaviour
    {
        [SerializeField] private GameObject modalOverlay;
        [SerializeField] private GameObject placeholderContent;
        [SerializeField] private TMP_Text placeholderTitleText;
        [SerializeField] private TMP_Text placeholderBodyText;
        [SerializeField] private Button closeButton;

        public bool IsModalOpen => modalOverlay != null && modalOverlay.activeSelf;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseModal);

            CloseModal();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseModal);
        }

        public void ShowModal(GameObject modalContent)
        {
            if (modalOverlay == null)
                modalOverlay = gameObject;

            modalOverlay.SetActive(true);
            if (modalContent != null)
                modalContent.SetActive(true);
        }

        public void ShowPlaceholder(string title, string body)
        {
            if (placeholderTitleText != null)
                placeholderTitleText.text = title;

            if (placeholderBodyText != null)
                placeholderBodyText.text = body;

            ShowModal(placeholderContent);
        }

        public void CloseModal()
        {
            if (placeholderContent != null)
                placeholderContent.SetActive(false);

            if (modalOverlay != null)
                modalOverlay.SetActive(false);
        }
    }
}
