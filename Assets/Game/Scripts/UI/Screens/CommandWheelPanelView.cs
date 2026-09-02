using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed class CommandWheelPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject wheelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button scrimButton;
        [SerializeField] private Button wheelMoveButton;
        [SerializeField] private Button wheelAttackButton;
        [SerializeField] private Button moveCommandButton;
        [SerializeField] private Button attackCommandButton;
        [SerializeField] private V3RadialWedgeGraphic moveWedge;
        [SerializeField] private V3RadialWedgeGraphic attackWedge;
        [SerializeField] private RectTransform wheelTransform;
        [SerializeField] private GameObject targetingRoot;
        [SerializeField] private GameObject rangeBanner;
        [SerializeField] private RectTransform unitCard;
        [SerializeField] private GameObject instructionRoot;
        [SerializeField] private GameObject threatRoot;
        [SerializeField] private GameObject feedbackRoot;
        [SerializeField] private BattleHudRuntimeFeedbackView runtimeFeedbackView;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;
        private bool _appliedSpecialMode;
        private bool _listenersBound;
        private bool _feedbackWasVisibleBeforeOpen;
        private bool _hasFeedbackVisibilitySnapshot;
        private bool _unitCardPositionCached;
        private Vector2 _unitCardBasePosition;
        private bool _wheelPositionCached;
        private Vector2 _wheelBasePosition;
        private Vector3 _wheelBaseScale;

        public bool IsOpen => wheelRoot != null && wheelRoot.activeSelf;
        public Button OpenButton => openButton;
        public bool HasBoundListeners => _listenersBound;

        public void BindGameTextResolver(IGameTextResolver gameTextResolver)
        {
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
        }

        private void Awake()
        {
            if (runtimeFeedbackView == null)
                runtimeFeedbackView = GetComponent<BattleHudRuntimeFeedbackView>();

            BindListeners();
            Close();
        }

        public void BindRuntimeSectionReferences(Button runtimeOpenButton, GameObject runtimeThreatRoot)
        {
            if (_listenersBound && openButton != null)
                openButton.onClick.RemoveListener(Open);

            openButton = runtimeOpenButton;
            threatRoot = runtimeThreatRoot;

            if (_listenersBound && openButton != null)
                openButton.onClick.AddListener(Open);
        }

        private void BindListeners()
        {
            if (_listenersBound)
                return;

            _listenersBound = true;
            if (openButton != null)
                openButton.onClick.AddListener(Open);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (scrimButton != null)
                scrimButton.onClick.AddListener(Close);

            if (wheelMoveButton != null)
                wheelMoveButton.onClick.AddListener(OnWheelMoveClicked);

            if (wheelAttackButton != null)
                wheelAttackButton.onClick.AddListener(OnWheelAttackClicked);
        }

        private void OnDestroy()
        {
            if (openButton != null)
                openButton.onClick.RemoveListener(Open);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (scrimButton != null)
                scrimButton.onClick.RemoveListener(Close);

            if (wheelMoveButton != null)
                wheelMoveButton.onClick.RemoveListener(OnWheelMoveClicked);

            if (wheelAttackButton != null)
                wheelAttackButton.onClick.RemoveListener(OnWheelAttackClicked);

            _listenersBound = false;
        }

        public void Open()
        {
            if (!_hasFeedbackVisibilitySnapshot && feedbackRoot != null)
            {
                _feedbackWasVisibleBeforeOpen = feedbackRoot.activeSelf;
                _hasFeedbackVisibilitySnapshot = true;
            }
            if (wheelRoot != null)
                wheelRoot.SetActive(true);
            SetTargetingPreview(false);
            ApplySpecialMode();
            if (feedbackRoot != null)
                feedbackRoot.SetActive(false);
        }

        public void Close()
        {
            if (wheelRoot != null)
                wheelRoot.SetActive(false);
            ClearSpecialMode();
            if (_hasFeedbackVisibilitySnapshot && feedbackRoot != null)
                feedbackRoot.SetActive(_feedbackWasVisibleBeforeOpen);
            _hasFeedbackVisibilitySnapshot = false;
        }

        public void Toggle()
        {
            if (wheelRoot == null)
                return;

            if (wheelRoot.activeSelf)
                Close();
            else
                Open();
        }

        public void SetTargetingPreview(bool targeting)
        {
            if (targetingRoot != null)
                targetingRoot.SetActive(targeting);
            if (rangeBanner != null)
                rangeBanner.SetActive(targeting);
            if (instructionRoot != null)
                instructionRoot.SetActive(!targeting);
            if (threatRoot != null)
                threatRoot.SetActive(!targeting);
            if (unitCard != null)
            {
                if (!_unitCardPositionCached)
                {
                    _unitCardBasePosition = unitCard.anchoredPosition;
                    _unitCardPositionCached = true;
                }

                unitCard.anchoredPosition = _unitCardBasePosition + (targeting ? new Vector2(20f, -185f) : Vector2.zero);
            }
            if (wheelTransform != null)
            {
                if (!_wheelPositionCached)
                {
                    _wheelBasePosition = wheelTransform.anchoredPosition;
                    _wheelBaseScale = wheelTransform.localScale;
                    _wheelPositionCached = true;
                }

                // The target-confirmation rail and the permanent ARIA panel both
                // occupy the right side. Compact and shift the radial wheel only
                // for targeting so its outer edge meets, but never covers, ARIA.
                wheelTransform.anchoredPosition = _wheelBasePosition + (targeting ? new Vector2(235f, 35f) : Vector2.zero);
                wheelTransform.localScale = targeting ? _wheelBaseScale * .80f : _wheelBaseScale;
            }

            if (moveWedge != null)
            {
                moveWedge.SetPalette(
                    targeting ? new Color32(38, 52, 57, 255) : new Color32(14, 139, 185, 255),
                    targeting ? new Color32(5, 13, 16, 255) : new Color32(3, 57, 83, 255),
                    targeting ? new Color32(151, 164, 167, 255) : new Color32(0, 200, 238, 255));
            }

            if (attackWedge != null)
            {
                attackWedge.SetPalette(
                    targeting ? new Color32(243, 99, 19, 255) : new Color32(190, 54, 31, 255),
                    targeting ? new Color32(112, 24, 7, 255) : new Color32(78, 14, 12, 255),
                    targeting ? new Color32(255, 180, 36, 255) : new Color32(239, 67, 35, 255));
            }
        }

        private void OnWheelMoveClicked()
        {
            moveCommandButton?.onClick.Invoke();
            Close();
        }

        private void OnWheelAttackClicked()
        {
            attackCommandButton?.onClick.Invoke();
            SetTargetingPreview(true);
        }

        private void ApplySpecialMode()
        {
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandMode(
                runtimeFeedbackView,
                TacticalCommandMode.Special,
                _gameTextResolver);
            _appliedSpecialMode = runtimeFeedbackView != null;
        }

        private void ClearSpecialMode()
        {
            if (!_appliedSpecialMode)
                return;

            BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(runtimeFeedbackView, _gameTextResolver);
            _appliedSpecialMode = false;
        }
    }
}
