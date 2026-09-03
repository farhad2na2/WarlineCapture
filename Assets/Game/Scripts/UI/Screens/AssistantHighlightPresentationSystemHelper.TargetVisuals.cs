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
        private static readonly Color V3PanelTop = new(0.025f, 0.15f, 0.18f, 0.985f);
        private static readonly Color V3PanelBottom = new(0.005f, 0.035f, 0.045f, 0.985f);

        private bool _localUiCueActive;
        private void ApplyScreenTargetIndicator(UiAssistantHighlightModel model)
        {
            _commandCueActive = ShouldShowCommandCue(model);
            _screenTargetActive = model.Active &&
                                  model.TargetKind != UiSurfaceTargetKind &&
                                  !_commandCueActive &&
                                  !_pendingFirstShowMe;
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
            _screenTargetIndicator.pivot = new Vector2(0.5f, 0f);
            _screenTargetIndicator.sizeDelta = new Vector2(392f, 112f);

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
                Color.Lerp(V3PanelTop, Color.white, 0.06f),
                V3PanelTop,
                Color.Lerp(V3PanelBottom, V3Cyan, 0.05f),
                V3PanelBottom,
                V3Cyan,
                3f);
            background.raycastTarget = false;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(indicator.transform, false);
            labelObject.layer = indicator.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(82f, 12f);
            labelRect.offsetMax = new Vector2(-18f, -32f);
            _screenTargetLabel = labelObject.GetComponent<TextMeshProUGUI>();
            _screenTargetLabel.text = GameLocalization.Get("ui.hud.aria_target", "ARIA TARGET");
            _screenTargetLabel.fontStyle = FontStyles.Bold;
            _screenTargetLabel.fontSize = 31f;
            _screenTargetLabel.enableAutoSizing = true;
            _screenTargetLabel.fontSizeMin = 22f;
            _screenTargetLabel.fontSizeMax = 34f;
            _screenTargetLabel.color = Color.white;
            _screenTargetLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _screenTargetLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _screenTargetLabel.raycastTarget = false;

            RectTransform header = CreateScreenCueText(
                indicator.transform,
                "Header",
                "ARIA GUIDANCE  /  TARGET LOCK",
                new Vector2(84f, 76f),
                new Vector2(-18f, -8f),
                15f,
                V3CyanSoft,
                TextAlignmentOptions.TopLeft);
            header.SetAsLastSibling();

            RectTransform rail = CreateScreenCueRect(
                indicator.transform,
                "AccentRail",
                new Vector2(0f, 0f),
                new Vector2(8f, 112f));
            V3GradientGraphic railGraphic = rail.gameObject.AddComponent<V3GradientGraphic>();
            railGraphic.Configure(V3Cyan, V3Lime, Color.clear, 0f);
            railGraphic.raycastTarget = false;

            RectTransform reticle = CreateScreenCueRect(
                indicator.transform,
                "Reticle",
                new Vector2(20f, 27f),
                new Vector2(60f, 67f));
            V3RingGraphic ring = reticle.gameObject.AddComponent<V3RingGraphic>();
            ring.Configure(V3Cyan, 3f, 40);
            CreateScreenCueSolid(reticle, "Horizontal", new Vector2(-7f, 18.5f), new Vector2(47f, 21.5f), V3CyanSoft);
            CreateScreenCueSolid(reticle, "Vertical", new Vector2(18.5f, -7f), new Vector2(21.5f, 47f), V3CyanSoft);
            RectTransform core = CreateScreenCueRect(reticle, "Core", new Vector2(15f, 15f), new Vector2(25f, 25f));
            Image coreImage = core.gameObject.AddComponent<Image>();
            coreImage.color = V3Lime;
            coreImage.raycastTarget = false;

            CreateScreenCueSolid(
                indicator.transform,
                "BottomRule",
                new Vector2(8f, 5f),
                new Vector2(384f, 8f),
                V3Lime);

            RectTransform pointer = CreateScreenCueRect(
                indicator.transform,
                "Pointer",
                new Vector2(178f, 0f),
                new Vector2(214f, 20f));
            V3PolygonGraphic pointerGraphic = pointer.gameObject.AddComponent<V3PolygonGraphic>();
            pointerGraphic.ConfigureResponsive(
                new[] { new Vector2(0f, 0f), new Vector2(36f, 0f), new Vector2(18f, 20f) },
                V3PanelBottom,
                V3Cyan,
                3f,
                new Vector2(36f, 20f));
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
