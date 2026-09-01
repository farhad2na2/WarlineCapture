using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Keeps the placement actions pinned to the right edge while the information
    /// panel absorbs additional ultrawide space. Authored values remain untouched
    /// at the 1076 px authored width that clears the live squad tray.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BuildPlacementConfirmationResponsiveLayoutView : MonoBehaviour
    {
        [SerializeField] private float referenceWidth = 1076f;
        [SerializeField] private RectTransform[] rightAnchoredTargets = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform[] widthExpandedTargets = Array.Empty<RectTransform>();

        private RectTransform _root;
        private Vector2[] _basePositions = Array.Empty<Vector2>();
        private Vector2[] _baseSizes = Array.Empty<Vector2>();
        private float _lastWidth = -1f;
        private bool _applying;

        public float ReferenceWidth => referenceWidth;
        public RectTransform[] RightAnchoredTargets => rightAnchoredTargets;

        public void Configure(
            float authoredReferenceWidth,
            RectTransform[] targetsAnchoredToRight,
            RectTransform[] targetsExpandedAcrossWidth)
        {
            referenceWidth = Mathf.Max(1f, authoredReferenceWidth);
            rightAnchoredTargets = targetsAnchoredToRight ?? Array.Empty<RectTransform>();
            widthExpandedTargets = targetsExpandedAcrossWidth ?? Array.Empty<RectTransform>();
            CaptureBaseLayout();
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_applying)
                return;
            _root ??= transform as RectTransform;
            if (_root == null)
                return;
            _applying = true;
            try
            {
                if (_basePositions.Length != rightAnchoredTargets.Length ||
                    _baseSizes.Length != widthExpandedTargets.Length)
                {
                    CaptureBaseLayout();
                }

                float width = _root.rect.width;
                float extraWidth = Mathf.Max(0f, width - referenceWidth);
                for (int i = 0; i < rightAnchoredTargets.Length; i++)
                {
                    RectTransform target = rightAnchoredTargets[i];
                    if (target != null)
                        target.anchoredPosition = _basePositions[i] + Vector2.right * extraWidth;
                }
                for (int i = 0; i < widthExpandedTargets.Length; i++)
                {
                    RectTransform target = widthExpandedTargets[i];
                    if (target != null)
                        target.sizeDelta = _baseSizes[i] + Vector2.right * extraWidth;
                }

                _lastWidth = width;
            }
            finally
            {
                _applying = false;
            }
        }

        private void OnEnable() => RefreshLayout();
        private void Start() => RefreshLayout();
        private void OnRectTransformDimensionsChange() => RefreshLayout();

        private void LateUpdate()
        {
            _root ??= transform as RectTransform;
            if (_root != null && !Mathf.Approximately(_lastWidth, _root.rect.width))
                RefreshLayout();
        }

        private void CaptureBaseLayout()
        {
            _basePositions = new Vector2[rightAnchoredTargets.Length];
            for (int i = 0; i < rightAnchoredTargets.Length; i++)
                _basePositions[i] = rightAnchoredTargets[i] != null
                    ? rightAnchoredTargets[i].anchoredPosition
                    : Vector2.zero;

            _baseSizes = new Vector2[widthExpandedTargets.Length];
            for (int i = 0; i < widthExpandedTargets.Length; i++)
                _baseSizes[i] = widthExpandedTargets[i] != null
                    ? widthExpandedTargets[i].sizeDelta
                    : Vector2.zero;
        }
    }
}
