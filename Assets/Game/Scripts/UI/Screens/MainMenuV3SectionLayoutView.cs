using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    [Serializable]
    public struct MainMenuV3HorizontalResponsiveTarget
    {
        [SerializeField] private RectTransform target;
        [SerializeField, Range(0f, 1f)] private float positionFactor;
        [SerializeField, Range(0f, 1f)] private float widthFactor;

        public MainMenuV3HorizontalResponsiveTarget(
            RectTransform targetRect,
            float horizontalPositionFactor,
            float horizontalWidthFactor = 0f)
        {
            target = targetRect;
            positionFactor = Mathf.Clamp01(horizontalPositionFactor);
            widthFactor = Mathf.Clamp01(horizontalWidthFactor);
        }

        public RectTransform Target => target;
        public float PositionFactor => positionFactor;
        public float WidthFactor => widthFactor;
    }

    public enum MainMenuV3SectionAlignment : byte
    {
        TopLeft,
        TopRight,
        Center,
        BottomCenter
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MainMenuV3SectionLayoutView : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new(1672f, 941f);
        [SerializeField] private MainMenuV3SectionAlignment alignment;
        [SerializeField] private bool expandToCanvasWidth;
        [SerializeField] private RectTransform[] rightAnchoredTargets = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform[] centerAnchoredTargets = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform[] widthExpandedTargets = Array.Empty<RectTransform>();
        [SerializeField] private MainMenuV3HorizontalResponsiveTarget[] horizontalResponsiveTargets =
            Array.Empty<MainMenuV3HorizontalResponsiveTarget>();

        private RectTransform _rectTransform;
        // These authored reference-space values must survive prefab serialization. The
        // DrivenRectTransformTracker can serialize a driven target at its preview position
        // (often x=0 when the prefab is saved outside a Canvas), so recapturing from the
        // target on the next load loses the real wide-screen anchor.
        [SerializeField, HideInInspector] private Vector2[] rightTargetBasePositions = Array.Empty<Vector2>();
        [SerializeField, HideInInspector] private Vector2[] centerTargetBasePositions = Array.Empty<Vector2>();
        [SerializeField, HideInInspector] private Vector2[] widthTargetBaseSizes = Array.Empty<Vector2>();
        [SerializeField, HideInInspector] private Vector2[] horizontalTargetBasePositions = Array.Empty<Vector2>();
        [SerializeField, HideInInspector] private Vector2[] horizontalTargetBaseSizes = Array.Empty<Vector2>();
        private Vector2 _lastCanvasSize;
        private DrivenRectTransformTracker _previewTracker;
        private bool _applying;

        public Vector2 ReferenceResolution => referenceResolution;
        public MainMenuV3SectionAlignment Alignment => alignment;
        public bool ExpandToCanvasWidth => expandToCanvasWidth;
        public RectTransform[] RightAnchoredTargets => rightAnchoredTargets;
        public MainMenuV3HorizontalResponsiveTarget[] HorizontalResponsiveTargets => horizontalResponsiveTargets;
        public float LastAppliedScale { get; private set; }
        public float LastAppliedExtraWidth { get; private set; }

        public bool TryGetAuthoredBasePosition(RectTransform target, out Vector2 position)
        {
            int index = Array.IndexOf(rightAnchoredTargets, target);
            if (index >= 0 && index < rightTargetBasePositions.Length)
            {
                position = rightTargetBasePositions[index];
                return true;
            }

            index = Array.IndexOf(centerAnchoredTargets, target);
            if (index >= 0 && index < centerTargetBasePositions.Length)
            {
                position = centerTargetBasePositions[index];
                return true;
            }

            for (int i = 0; i < horizontalResponsiveTargets.Length; i++)
            {
                if (horizontalResponsiveTargets[i].Target != target || i >= horizontalTargetBasePositions.Length)
                    continue;

                position = horizontalTargetBasePositions[i];
                return true;
            }

            position = default;
            return false;
        }

        public void Configure(
            Vector2 authoredReferenceResolution,
            MainMenuV3SectionAlignment sectionAlignment,
            RectTransform[] targetsAnchoredToRight = null,
            bool shouldExpandToCanvasWidth = false,
            RectTransform[] targetsAnchoredToCenter = null,
            RectTransform[] targetsExpandedAcrossWidth = null,
            MainMenuV3HorizontalResponsiveTarget[] horizontalTargets = null)
        {
            referenceResolution = authoredReferenceResolution;
            alignment = sectionAlignment;
            expandToCanvasWidth = shouldExpandToCanvasWidth;
            rightAnchoredTargets = targetsAnchoredToRight ?? Array.Empty<RectTransform>();
            centerAnchoredTargets = targetsAnchoredToCenter ?? Array.Empty<RectTransform>();
            widthExpandedTargets = targetsExpandedAcrossWidth ?? Array.Empty<RectTransform>();
            horizontalResponsiveTargets = horizontalTargets ?? Array.Empty<MainMenuV3HorizontalResponsiveTarget>();
            CaptureResponsiveTargetBaseLayout();
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_applying || referenceResolution.x <= 0f || referenceResolution.y <= 0f)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            RectTransform parentRect = transform.parent as RectTransform;
            if (canvasRect == null || parentRect == null || canvasRect.rect.width <= 0f || canvasRect.rect.height <= 0f)
                return;

            _applying = true;
            try
            {
                if (_rectTransform == null)
                    _rectTransform = (RectTransform)transform;
                TrackDrivenLayoutProperties();
                if (rightTargetBasePositions.Length != rightAnchoredTargets.Length ||
                    centerTargetBasePositions.Length != centerAnchoredTargets.Length ||
                    widthTargetBaseSizes.Length != widthExpandedTargets.Length ||
                    horizontalTargetBasePositions.Length != horizontalResponsiveTargets.Length ||
                    horizontalTargetBaseSizes.Length != horizontalResponsiveTargets.Length)
                {
                    CaptureResponsiveTargetBaseLayout();
                }

                Vector2 canvasSize = canvasRect.rect.size;
                float scale = Mathf.Min(
                    canvasSize.x / referenceResolution.x,
                    canvasSize.y / referenceResolution.y);
                if (scale <= 0f)
                    return;

                Vector2 pivot = ResolvePivot(alignment);
                Vector3 targetWorldPosition = ResolveCanvasAnchorWorldPosition(canvasRect, alignment);
                _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rectTransform.pivot = pivot;
                float extraWidth = Mathf.Max(0f, canvasSize.x / scale - referenceResolution.x);
                _rectTransform.sizeDelta = new Vector2(
                    referenceResolution.x + (expandToCanvasWidth ? extraWidth : 0f),
                    referenceResolution.y);
                _rectTransform.localScale = Vector3.one * scale;
                _rectTransform.localRotation = Quaternion.identity;
                _rectTransform.position = targetWorldPosition;

                ApplyRightAnchoredTargetOffset(extraWidth);
                ApplyCenterAnchoredTargetOffset(extraWidth);
                ApplyWidthExpandedTargets(extraWidth);
                ApplyHorizontalResponsiveTargets(extraWidth);
                LastAppliedScale = scale;
                LastAppliedExtraWidth = extraWidth;
                _lastCanvasSize = canvasSize;
            }
            finally
            {
                _applying = false;
            }
        }

        private void OnEnable()
        {
            if (rightTargetBasePositions.Length != rightAnchoredTargets.Length ||
                centerTargetBasePositions.Length != centerAnchoredTargets.Length ||
                widthTargetBaseSizes.Length != widthExpandedTargets.Length ||
                horizontalTargetBasePositions.Length != horizontalResponsiveTargets.Length ||
                horizontalTargetBaseSizes.Length != horizontalResponsiveTargets.Length)
            {
                CaptureResponsiveTargetBaseLayout();
            }
            RefreshLayout();
        }

        private void OnDisable()
        {
            _previewTracker.Clear();
        }

        private void Start()
        {
            // UIShellContentView stretches a freshly-instantiated section after OnEnable.
            // Re-apply once after mounting so the authored reference frame wins.
            RefreshLayout();
        }

        private void LateUpdate()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            if (_rectTransform == null)
                _rectTransform = (RectTransform)transform;

            Vector3 expectedPosition = ResolveCanvasAnchorWorldPosition(canvasRect, alignment);
            float expectedScale = Mathf.Min(
                canvasRect.rect.width / referenceResolution.x,
                canvasRect.rect.height / referenceResolution.y);
            float expectedExtraWidth = expectedScale > 0f
                ? Mathf.Max(0f, canvasRect.rect.width / expectedScale - referenceResolution.x)
                : 0f;
            Vector2 expectedSize = new(
                referenceResolution.x + (expandToCanvasWidth ? expectedExtraWidth : 0f),
                referenceResolution.y);
            bool mountMovedAfterInitialLayout =
                (_rectTransform.position - expectedPosition).sqrMagnitude > 0.01f ||
                Mathf.Abs(_rectTransform.localScale.x - expectedScale) > 0.001f ||
                _rectTransform.rect.size != expectedSize;

            // The shell's region layout settles after a section is instantiated. Its
            // parent can therefore move without changing the root Canvas dimensions.
            // Re-apply only when that late mount changes our authored reference frame.
            if (canvasRect.rect.size != _lastCanvasSize || mountMovedAfterInitialLayout)
                RefreshLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshLayout();
        }

        private void CaptureResponsiveTargetBaseLayout()
        {
            rightTargetBasePositions = new Vector2[rightAnchoredTargets.Length];
            for (int i = 0; i < rightAnchoredTargets.Length; i++)
            {
                RectTransform target = rightAnchoredTargets[i];
                rightTargetBasePositions[i] = target != null ? target.anchoredPosition : Vector2.zero;
            }

            centerTargetBasePositions = new Vector2[centerAnchoredTargets.Length];
            for (int i = 0; i < centerAnchoredTargets.Length; i++)
            {
                RectTransform target = centerAnchoredTargets[i];
                centerTargetBasePositions[i] = target != null ? target.anchoredPosition : Vector2.zero;
            }

            widthTargetBaseSizes = new Vector2[widthExpandedTargets.Length];
            for (int i = 0; i < widthExpandedTargets.Length; i++)
            {
                RectTransform target = widthExpandedTargets[i];
                widthTargetBaseSizes[i] = target != null ? target.sizeDelta : Vector2.zero;
            }


            horizontalTargetBasePositions = new Vector2[horizontalResponsiveTargets.Length];
            horizontalTargetBaseSizes = new Vector2[horizontalResponsiveTargets.Length];
            for (int i = 0; i < horizontalResponsiveTargets.Length; i++)
            {
                RectTransform target = horizontalResponsiveTargets[i].Target;
                horizontalTargetBasePositions[i] = target != null ? target.anchoredPosition : Vector2.zero;
                horizontalTargetBaseSizes[i] = target != null ? target.sizeDelta : Vector2.zero;
            }
        }

        private void TrackDrivenLayoutProperties()
        {
            // ExecuteAlways keeps the Scene/Game preview identical to Play Mode. Marking
            // the calculated transforms as driven prevents the current Game-view
            // resolution from becoming prefab overrides or dirtying the open scene.
            _previewTracker.Clear();
            _previewTracker.Add(this, _rectTransform, DrivenTransformProperties.All);

            for (int i = 0; i < rightAnchoredTargets.Length; i++)
            {
                if (rightAnchoredTargets[i] != null)
                    _previewTracker.Add(this, rightAnchoredTargets[i], DrivenTransformProperties.AnchoredPositionX);
            }

            for (int i = 0; i < centerAnchoredTargets.Length; i++)
            {
                if (centerAnchoredTargets[i] != null)
                    _previewTracker.Add(this, centerAnchoredTargets[i], DrivenTransformProperties.AnchoredPositionX);
            }

            for (int i = 0; i < widthExpandedTargets.Length; i++)
            {
                if (widthExpandedTargets[i] != null)
                    _previewTracker.Add(this, widthExpandedTargets[i], DrivenTransformProperties.SizeDeltaX);
            }


            for (int i = 0; i < horizontalResponsiveTargets.Length; i++)
            {
                RectTransform target = horizontalResponsiveTargets[i].Target;
                if (target != null)
                {
                    _previewTracker.Add(
                        this,
                        target,
                        DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX);
                }
            }
        }

        private void ApplyRightAnchoredTargetOffset(float extraWidth)
        {
            int count = Mathf.Min(rightAnchoredTargets.Length, rightTargetBasePositions.Length);
            for (int i = 0; i < count; i++)
            {
                RectTransform target = rightAnchoredTargets[i];
                if (target == null)
                    continue;

                Vector2 position = rightTargetBasePositions[i];
                position.x += extraWidth;
                target.anchoredPosition = position;
            }
        }

        private void ApplyCenterAnchoredTargetOffset(float extraWidth)
        {
            int count = Mathf.Min(centerAnchoredTargets.Length, centerTargetBasePositions.Length);
            for (int i = 0; i < count; i++)
            {
                RectTransform target = centerAnchoredTargets[i];
                if (target == null)
                    continue;

                Vector2 position = centerTargetBasePositions[i];
                position.x += extraWidth * .5f;
                target.anchoredPosition = position;
            }
        }

        private void ApplyWidthExpandedTargets(float extraWidth)
        {
            int count = Mathf.Min(widthExpandedTargets.Length, widthTargetBaseSizes.Length);
            for (int i = 0; i < count; i++)
            {
                RectTransform target = widthExpandedTargets[i];
                if (target == null)
                    continue;

                Vector2 size = widthTargetBaseSizes[i];
                size.x += extraWidth;
                target.sizeDelta = size;
            }
        }

        private void ApplyHorizontalResponsiveTargets(float extraWidth)
        {
            int count = Mathf.Min(
                horizontalResponsiveTargets.Length,
                Mathf.Min(horizontalTargetBasePositions.Length, horizontalTargetBaseSizes.Length));
            for (int i = 0; i < count; i++)
            {
                MainMenuV3HorizontalResponsiveTarget rule = horizontalResponsiveTargets[i];
                RectTransform target = rule.Target;
                if (target == null)
                    continue;

                Vector2 position = horizontalTargetBasePositions[i];
                position.x += extraWidth * rule.PositionFactor;
                target.anchoredPosition = position;

                Vector2 size = horizontalTargetBaseSizes[i];
                size.x += extraWidth * rule.WidthFactor;
                target.sizeDelta = size;
            }
        }

        private static Vector2 ResolvePivot(MainMenuV3SectionAlignment value)
        {
            return value switch
            {
                MainMenuV3SectionAlignment.TopLeft => new Vector2(0f, 1f),
                MainMenuV3SectionAlignment.TopRight => new Vector2(1f, 1f),
                MainMenuV3SectionAlignment.BottomCenter => new Vector2(0.5f, 0f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        private static Vector3 ResolveCanvasAnchorWorldPosition(
            RectTransform canvasRect,
            MainMenuV3SectionAlignment value)
        {
            var corners = new Vector3[4];
            canvasRect.GetWorldCorners(corners);
            return value switch
            {
                MainMenuV3SectionAlignment.TopLeft => corners[1],
                MainMenuV3SectionAlignment.TopRight => corners[2],
                MainMenuV3SectionAlignment.BottomCenter => (corners[0] + corners[3]) * 0.5f,
                _ => (corners[0] + corners[2]) * 0.5f
            };
        }
    }
}
