using System;
using Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public enum DistrictOperationActionKind : byte
    {
        Patrol,
        DroneScan,
        Aid,
        Raid,
        Repair
    }

    [DisallowMultipleComponent]
    public sealed class DistrictDetailActionsScreenView : MonoBehaviour
    {
        [SerializeField] private UIShellRouteButtonView backRouteButton;
        [SerializeField] private RawImage districtImage;
        [SerializeField] private Image ariaPortrait;
        [SerializeField] private TMP_Text districtName;
        [SerializeField] private TMP_Text threatLabel;
        [SerializeField] private Slider intelConfidence;
        [SerializeField] private Button[] actionButtons;
        [SerializeField] private GameObject confirmRaidPopupPrefab;

        private readonly UnityEngine.Events.UnityAction[] _actionCallbacks = new UnityEngine.Events.UnityAction[5];
        private GameObject _activeModal;
        private bool _bindingsInstalled;

        public UIShellRouteButtonView BackRouteButton => backRouteButton;
        public RawImage DistrictImage => districtImage;
        public Image AriaPortrait => ariaPortrait;
        public TMP_Text DistrictName => districtName;
        public TMP_Text ThreatLabel => threatLabel;
        public Slider IntelConfidence => intelConfidence;
        public Button[] ActionButtons => actionButtons;
        public GameObject ConfirmRaidPopupPrefab => confirmRaidPopupPrefab;

        public event Action<DistrictOperationActionKind> ActionRequested;

        private void Awake() => RefreshBindings();
        private void OnEnable() => RefreshBindings();

        private void OnDestroy()
        {
            RemoveBindings();
            if (_activeModal != null)
                DestroyModalObject(_activeModal);
        }

        public void RefreshBindings()
        {
            RemoveBindings();
            if (actionButtons == null || actionButtons.Length < 5)
                return;

            _actionCallbacks[0] = () => RequestAction(DistrictOperationActionKind.Patrol, 0);
            _actionCallbacks[1] = () => RequestAction(DistrictOperationActionKind.DroneScan, 1);
            _actionCallbacks[2] = () => RequestAction(DistrictOperationActionKind.Aid, 2);
            _actionCallbacks[3] = OpenRaidConfirmation;
            _actionCallbacks[4] = () => RequestAction(DistrictOperationActionKind.Repair, 4);
            for (int i = 0; i < _actionCallbacks.Length; i++)
                actionButtons[i]?.onClick.AddListener(_actionCallbacks[i]);
            _bindingsInstalled = true;
        }

        private void RemoveBindings()
        {
            if (!_bindingsInstalled || actionButtons == null)
                return;
            int count = Mathf.Min(_actionCallbacks.Length, actionButtons.Length);
            for (int i = 0; i < count; i++)
                if (actionButtons[i] != null && _actionCallbacks[i] != null)
                    actionButtons[i].onClick.RemoveListener(_actionCallbacks[i]);
            _bindingsInstalled = false;
        }

        private void OpenRaidConfirmation()
        {
            if (confirmRaidPopupPrefab == null)
                return;
            CloseActiveModal();
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.rootCanvas.transform : transform.root;
            _activeModal = Instantiate(confirmRaidPopupPrefab, parent, false);
            _activeModal.name = confirmRaidPopupPrefab.name;
            if (_activeModal.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
            _activeModal.transform.SetAsLastSibling();
            ConfirmRaidV3PopupView popup = _activeModal.GetComponent<ConfirmRaidV3PopupView>();
            if (popup != null)
                popup.Confirmed += ConfirmRaid;
        }

        private void ConfirmRaid()
        {
            RequestAction(DistrictOperationActionKind.Raid, 3);
        }

        private void RequestAction(DistrictOperationActionKind action, int buttonIndex)
        {
            if (actionButtons == null || buttonIndex < 0 || buttonIndex >= actionButtons.Length)
                return;
            Button button = actionButtons[buttonIndex];
            if (button == null || !button.interactable)
                return;

            button.interactable = false;
            TMP_Text detail = FindText(button.transform, "Time");
            if (detail != null)
            {
                detail.text = GameLocalization.Get("ui.common.queued", "QUEUED");
                detail.color = new Color32(255, 255, 255, 255);
            }
            ActionRequested?.Invoke(action);
        }

        private void CloseActiveModal()
        {
            if (_activeModal == null)
                return;
            DestroyModalObject(_activeModal);
            _activeModal = null;
        }

        private static TMP_Text FindText(Transform root, string objectName)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                if (text != null && string.Equals(text.name, objectName, StringComparison.Ordinal))
                    return text;
            return null;
        }

        private static void DestroyModalObject(GameObject modal)
        {
            if (modal == null)
                return;
            if (Application.isPlaying)
                Destroy(modal);
            else
                DestroyImmediate(modal);
        }
    }
}
