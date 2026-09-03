using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        private static readonly Color V3Cyan = new(0.00f, 0.79f, 0.95f, 1f);
        private static readonly Color V3CyanSoft = new(0.18f, 0.92f, 1f, 0.78f);
        private static readonly Color V3Lime = new(0.43f, 0.94f, 0.20f, 1f);
        private static readonly Color V3GuidanceYellow = new(1f, 0.72f, 0.02f, 1f);
        private static readonly Color V3GuidancePanelTop = new(0.11f, 0.095f, 0.025f, 0.98f);
        private static readonly Color V3GuidancePanelBottom = new(0.025f, 0.035f, 0.038f, 0.98f);

        private bool _localUiCueActive;
        private void ApplyScreenTargetIndicator(UiAssistantHighlightModel model)
        {
            _commandCueActive = ShouldShowCommandCue(model);
            // World guidance owns a real world-space ring. Projecting a second cue onto the
            // overlay made ground markers render over the build drawer and ARIA panel. Only
            // UI targets get a screen-space focus frame; world targets remain behind the HUD.
            _screenTargetActive = false;
            _screenTargetWorld = new Vector3(model.WorldX, model.WorldY, model.WorldZ);
            EnsureScreenTargetIndicator();
            if (_screenTargetIndicator != null)
            {
                _screenTargetIndicator.gameObject.SetActive(
                    _screenTargetActive || _commandCueActive);
                if (_screenTargetLabel != null)
                    _screenTargetLabel.text = ResolveIndicatorText(model, _commandCueActive);
            }
            if (_screenTargetActive || _commandCueActive)
                Tick();
        }

        private void EnsureScreenTargetIndicator()
        {
            if (_screenTargetIndicator != null || _panelPulse == null)
                return;

            Canvas[] canvases = _panelPulse.GetComponentsInParent<Canvas>(true);
            if (canvases.Length == 0)
                return;

            _screenTargetCanvas = canvases[canvases.Length - 1];
            GameObject indicator = new(
                "AriaAssistantTargetIndicatorRuntime",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(V3GradientGraphic));
            indicator.transform.SetParent(_screenTargetCanvas.transform, false);
            indicator.transform.SetAsLastSibling();
            indicator.layer = _screenTargetCanvas.gameObject.layer;
            _screenTargetIndicator = indicator.GetComponent<RectTransform>();
            _screenTargetIndicator.anchorMin = new Vector2(0.5f, 0.5f);
            _screenTargetIndicator.anchorMax = new Vector2(0.5f, 0.5f);
            _screenTargetIndicator.pivot = new Vector2(0.5f, 0.5f);
            _screenTargetIndicator.sizeDelta = new Vector2(260f, 120f);

            Canvas isolatedCanvas = indicator.GetComponent<Canvas>();
            isolatedCanvas.overrideSorting = true;
            isolatedCanvas.sortingOrder = _screenTargetCanvas.sortingOrder + 50;
            isolatedCanvas.worldCamera = _screenTargetCanvas.worldCamera;
            _screenTargetGroup = indicator.GetComponent<CanvasGroup>();
            _screenTargetGroup.alpha = 1f;
            _screenTargetGroup.interactable = false;
            _screenTargetGroup.blocksRaycasts = false;
            V3GradientGraphic background = indicator.GetComponent<V3GradientGraphic>();
            background.ConfigureCorners(
                Color.clear,
                Color.clear,
                Color.clear,
                Color.clear,
                V3GuidanceYellow,
                7f);
            background.raycastTarget = false;

            GameObject caption = new(
                "TopBorderCaption",
                typeof(RectTransform),
                typeof(V3GradientGraphic));
            caption.transform.SetParent(indicator.transform, false);
            caption.layer = indicator.layer;
            RectTransform captionRect = caption.GetComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0.5f, 1f);
            captionRect.anchorMax = new Vector2(0.5f, 1f);
            captionRect.pivot = new Vector2(0.5f, 0.5f);
            captionRect.anchoredPosition = Vector2.zero;
            captionRect.sizeDelta = new Vector2(288f, 64f);
            V3GradientGraphic captionBackground = caption.GetComponent<V3GradientGraphic>();
            captionBackground.ConfigureCorners(
                V3GuidancePanelTop,
                Color.Lerp(V3GuidancePanelTop, V3GuidanceYellow, 0.08f),
                V3GuidancePanelBottom,
                V3GuidancePanelBottom,
                V3GuidanceYellow,
                4f);
            captionBackground.raycastTarget = false;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(caption.transform, false);
            labelObject.layer = indicator.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);
            _screenTargetLabel = labelObject.GetComponent<TextMeshProUGUI>();
            _screenTargetLabel.text = GameLocalization.Get("ui.hud.aria_target", "ARIA TARGET");
            _screenTargetLabel.fontStyle = FontStyles.Bold;
            _screenTargetLabel.fontSize = 36f;
            _screenTargetLabel.enableAutoSizing = true;
            _screenTargetLabel.fontSizeMin = 24f;
            _screenTargetLabel.fontSizeMax = 36f;
            _screenTargetLabel.color = V3GuidanceYellow;
            _screenTargetLabel.alignment = TextAlignmentOptions.Center;
            _screenTargetLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _screenTargetLabel.raycastTarget = false;
            V3LocalizedTextBinding localizedLabel = labelObject.AddComponent<V3LocalizedTextBinding>();
            localizedLabel.Configure("ui.hud.aria_target", "ARIA TARGET");
            indicator.SetActive(false);
        }

        private static RectTransform CreateScreenCueRect(
            Transform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            child.layer = parent.gameObject.layer;
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static RectTransform CreateScreenCueText(
            Transform parent,
            string name,
            string value,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float size,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            child.layer = parent.gameObject.layer;
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontStyle = FontStyles.Bold;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return rect;
        }

        private static void CreateScreenCueSolid(
            Transform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            RectTransform rect = CreateScreenCueRect(parent, name, offsetMin, offsetMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void ApplyWorldRing(UiAssistantHighlightModel model, bool visible)
        {
            if (!model.Active || !visible)
            {
                if (_worldRingRoot != null)
                    _worldRingRoot.SetActive(false);
                return;
            }

            EnsureWorldRing();
            if (_worldRingRoot == null || _worldRingRenderer == null)
                return;

            Vector3 center = new(
                model.WorldX, model.WorldY + WorldRingHeightOffset, model.WorldZ);
            float radius = Mathf.Max(
                0.35f,
                WorldRingRadius * (0.75f + Mathf.Clamp01(model.Strength) * 0.25f));
            WriteWorldMarker(center, radius);
            _worldRingRoot.SetActive(true);
        }

        private static void SetAnchoredPositionIfChanged(RectTransform target, Vector2 position)
        {
            if ((target.anchoredPosition - position).sqrMagnitude > 0.25f)
                target.anchoredPosition = position;
        }

        private static void SetAnchorsIfChanged(RectTransform target, Vector2 anchor)
        {
            if ((target.anchorMin - anchor).sqrMagnitude <= 0.000001f &&
                (target.anchorMax - anchor).sqrMagnitude <= 0.000001f)
                return;

            target.anchorMin = anchor;
            target.anchorMax = anchor;
        }

        private void ShowScreenTargetFallback()
        {
            if (_screenTargetIndicator == null)
                return;

            SetAnchorsIfChanged(_screenTargetIndicator, new Vector2(0.5f, 0.26f));
            SetAnchoredPositionIfChanged(_screenTargetIndicator, Vector2.zero);
            _screenTargetIndicator.localScale = Vector3.one;
            if (!_screenTargetIndicator.gameObject.activeSelf)
                _screenTargetIndicator.gameObject.SetActive(true);
        }

        private void EnsureWorldRing()
        {
            if (_worldRingRoot != null && _worldRingRenderer != null)
                return;

            DestroyObject(_worldRingRoot);
            _worldRingRoot = new GameObject(WorldRingName);
            _worldRingMaterial = CreateWorldRingMaterial();
            _worldRingRenderer = _worldRingRoot.AddComponent<LineRenderer>();
            ConfigureWorldLine(_worldRingRenderer, true, WorldRingSegments, WorldRingWidth, V3CyanSoft);

            _worldAccentRenderers = new LineRenderer[WorldAccentSegmentCount];
            for (int index = 0; index < _worldAccentRenderers.Length; index++)
            {
                GameObject segment = new($"AccentSegment{index + 1:00}");
                segment.transform.SetParent(_worldRingRoot.transform, false);
                _worldAccentRenderers[index] = segment.AddComponent<LineRenderer>();
                ConfigureWorldLine(_worldAccentRenderers[index], false, 7, 0.34f, V3Lime);
            }

            _worldBracketRenderers = new LineRenderer[WorldBracketCount];
            for (int index = 0; index < _worldBracketRenderers.Length; index++)
            {
                GameObject bracket = new($"CornerBracket{index + 1:00}");
                bracket.transform.SetParent(_worldRingRoot.transform, false);
                _worldBracketRenderers[index] = bracket.AddComponent<LineRenderer>();
                ConfigureWorldLine(_worldBracketRenderers[index], false, 3, 0.24f, V3Cyan);
            }

            _worldCrosshairRenderers = new LineRenderer[2];
            for (int index = 0; index < _worldCrosshairRenderers.Length; index++)
            {
                GameObject crosshair = new(index == 0 ? "CrosshairHorizontal" : "CrosshairVertical");
                crosshair.transform.SetParent(_worldRingRoot.transform, false);
                _worldCrosshairRenderers[index] = crosshair.AddComponent<LineRenderer>();
                ConfigureWorldLine(_worldCrosshairRenderers[index], false, 2, 0.14f, V3Lime);
            }
            _worldRingRoot.SetActive(false);
        }

        private void ConfigureWorldLine(
            LineRenderer line,
            bool loop,
            int positionCount,
            float width,
            Color color)
        {
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = positionCount;
            line.widthMultiplier = width;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            line.allowOcclusionWhenDynamic = false;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = 80;
            if (_worldRingMaterial != null)
                line.sharedMaterial = _worldRingMaterial;
        }

        private void WriteWorldMarker(Vector3 center, float radius)
        {
            for (int index = 0; index < WorldRingSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / WorldRingSegments;
                _worldRingRenderer.SetPosition(
                    index,
                    new Vector3(
                        center.x + Mathf.Cos(angle) * radius,
                        center.y,
                        center.z + Mathf.Sin(angle) * radius));
            }

            if (_worldAccentRenderers != null)
            {
                const float accentSweep = Mathf.PI * 0.16f;
                for (int segmentIndex = 0; segmentIndex < _worldAccentRenderers.Length; segmentIndex++)
                {
                    LineRenderer segment = _worldAccentRenderers[segmentIndex];
                    float start = segmentIndex * Mathf.PI * 2f / _worldAccentRenderers.Length + Mathf.PI * 0.045f;
                    for (int pointIndex = 0; pointIndex < segment.positionCount; pointIndex++)
                    {
                        float t = pointIndex / (float)(segment.positionCount - 1);
                        float angle = start + accentSweep * t;
                        float accentRadius = radius * 0.82f;
                        segment.SetPosition(
                            pointIndex,
                            new Vector3(
                                center.x + Mathf.Cos(angle) * accentRadius,
                                center.y + 0.015f,
                                center.z + Mathf.Sin(angle) * accentRadius));
                    }
                }
            }

            if (_worldBracketRenderers != null)
            {
                for (int index = 0; index < _worldBracketRenderers.Length; index++)
                {
                    float angle = index * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                    Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 tangent = new(-radial.z, 0f, radial.x);
                    float outer = radius * 1.25f;
                    Vector3 corner = center + radial * outer;
                    _worldBracketRenderers[index].SetPosition(0, corner - radial * radius * 0.28f);
                    _worldBracketRenderers[index].SetPosition(1, corner);
                    _worldBracketRenderers[index].SetPosition(2, corner - tangent * radius * 0.28f);
                }
            }

            if (_worldCrosshairRenderers != null)
            {
                float arm = radius * 0.36f;
                _worldCrosshairRenderers[0].SetPosition(0, center + Vector3.left * arm);
                _worldCrosshairRenderers[0].SetPosition(1, center + Vector3.right * arm);
                _worldCrosshairRenderers[1].SetPosition(0, center + Vector3.back * arm);
                _worldCrosshairRenderers[1].SetPosition(1, center + Vector3.forward * arm);
            }
        }

        private static Material CreateWorldRingMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            var material = new Material(shader)
            {
                name = "AriaAssistantPreviewHighlightMaterial",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Transparent + 120
            };
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            material.SetColor("_Color", Color.white);
            material.SetColor("_BaseColor", Color.white);
            return material;
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
