using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BuildPlacementValidityPanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform validitySurface;
        [SerializeField] private RectTransform minimapSurface;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject obstructionRow;

        private GameObject _suppressedAria;
        private bool _restoreAriaWhenHidden;

        public RectTransform ValiditySurface => validitySurface;
        public RectTransform MinimapSurface => minimapSurface;
        public TMP_Text StatusText => statusText;
        public GameObject ObstructionRow => obstructionRow;
        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > .99f;

        private void Awake()
        {
            CacheReferences();
            SetVisible(false);
        }

        private void OnDisable()
        {
            RestoreAria();
        }

        private void OnDestroy()
        {
            RestoreAria();
        }

        public static BuildPlacementValidityPanelView Ensure(GameObject prefab, RectTransform parent)
        {
            if (prefab == null || parent == null)
                return null;

            BuildPlacementValidityPanelView existing =
                parent.GetComponentInChildren<BuildPlacementValidityPanelView>(true);
            if (existing != null)
                return existing;

            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            RectTransform rect = instance.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(.5f, .5f);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            BuildPlacementValidityPanelView view = instance.GetComponent<BuildPlacementValidityPanelView>();
            view?.CacheReferences();
            view?.SetVisible(false);
            return view;
        }

        public void ApplyValidityState(bool hasPlacement, bool canConfirm)
        {
            bool shouldShow = hasPlacement && !canConfirm;
            SetVisible(shouldShow);
            if (shouldShow)
                SuppressAria();
            else
                RestoreAria();
        }

        public void ShowInvalidPreview()
        {
            ApplyValidityState(true, false);
        }

        public void Hide()
        {
            ApplyValidityState(false, false);
        }

        private void CacheReferences()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
        }

        private void SetVisible(bool visible)
        {
            CacheReferences();
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void SuppressAria()
        {
            if (_suppressedAria == null)
            {
                Transform searchRoot = transform.root;
                RectTransform[] candidates = searchRoot.GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < candidates.Length; i++)
                {
                    RectTransform candidate = candidates[i];
                    if (candidate != null && candidate.gameObject != gameObject &&
                        candidate.name == "AriaAssistantButton")
                    {
                        _suppressedAria = candidate.gameObject;
                        break;
                    }
                }
            }

            if (_suppressedAria == null)
                return;

            if (!_restoreAriaWhenHidden)
                _restoreAriaWhenHidden = _suppressedAria.activeSelf;
            if (_suppressedAria.activeSelf)
                _suppressedAria.SetActive(false);
        }

        private void RestoreAria()
        {
            if (_suppressedAria != null && _restoreAriaWhenHidden && !_suppressedAria.activeSelf)
                _suppressedAria.SetActive(true);
            _suppressedAria = null;
            _restoreAriaWhenHidden = false;
        }
    }
}
