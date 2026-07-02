using UnityEngine;

namespace Game.UI.Runtime
{
    public enum UIShellRegionId
    {
        LoadingLayer = 0,
        HeaderRegion = 1,
        LeftRegion = 2,
        MiddleRegion = 3,
        RightRegion = 4,
        FooterRegion = 5,
        PopupLayer = 6,
        MenuBackgroundRegion = 7
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIShellRegionView : MonoBehaviour
    {
        [SerializeField] private UIShellRegionId regionId;
        [SerializeField] private RectTransform regionRoot;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector2 onScreenAnchoredPosition;
        [SerializeField] private Vector2 offScreenDirection;

        public UIShellRegionId RegionId => regionId;
        public RectTransform RegionRoot => regionRoot;
        public RectTransform ContentRoot => contentRoot;
        public CanvasGroup CanvasGroup => canvasGroup;
        public Vector2 OnScreenAnchoredPosition => onScreenAnchoredPosition;
        public Vector2 OffScreenDirection => offScreenDirection;

        private void Reset()
        {
            regionRoot = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (contentRoot == null && transform.childCount > 0)
                contentRoot = transform.GetChild(0) as RectTransform;

            CacheOnScreenPosition();
        }

        private void Awake()
        {
            if (regionRoot == null)
                regionRoot = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Configure(
            UIShellRegionId id,
            RectTransform root,
            RectTransform content,
            CanvasGroup group,
            Vector2 offscreen)
        {
            regionId = id;
            regionRoot = root;
            contentRoot = content;
            canvasGroup = group;
            offScreenDirection = offscreen;
            CacheOnScreenPosition();
        }

        public void CacheOnScreenPosition()
        {
            if (regionRoot == null)
                regionRoot = GetComponent<RectTransform>();

            if (regionRoot != null)
                onScreenAnchoredPosition = regionRoot.anchoredPosition;
        }

        public void ResetVisualState()
        {
            if (regionRoot == null)
                return;

            regionRoot.anchoredPosition = onScreenAnchoredPosition;
            regionRoot.localScale = Vector3.one;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void ClearContent()
        {
            if (contentRoot == null)
                return;

            for (int index = contentRoot.childCount - 1; index >= 0; index--)
                Destroy(contentRoot.GetChild(index).gameObject);
        }
    }
}
