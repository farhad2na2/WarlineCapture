using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed partial class AssistantHighlightPresentationSystemHelper
    {
        private void ApplyScreenTargetIndicator(UiAssistantHighlightModel model)
        {
            _commandCueActive = ShouldShowCommandCue(model);
            _screenTargetActive = model.Active && !_commandCueActive && !_pendingFirstShowMe;
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
                typeof(Image));
            indicator.transform.SetParent(_screenTargetCanvas.transform, false);
            indicator.transform.SetAsLastSibling();
            indicator.layer = _screenTargetCanvas.gameObject.layer;
            _screenTargetIndicator = indicator.GetComponent<RectTransform>();
            _screenTargetIndicator.anchorMin = new Vector2(0.5f, 0.5f);
            _screenTargetIndicator.anchorMax = new Vector2(0.5f, 0.5f);
            _screenTargetIndicator.pivot = new Vector2(0.5f, 0f);
            _screenTargetIndicator.sizeDelta = new Vector2(310f, 96f);

            Canvas isolatedCanvas = indicator.GetComponent<Canvas>();
            isolatedCanvas.overrideSorting = true;
            isolatedCanvas.sortingOrder = _screenTargetCanvas.sortingOrder + 50;
            isolatedCanvas.worldCamera = _screenTargetCanvas.worldCamera;
            CanvasGroup group = indicator.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Image background = indicator.GetComponent<Image>();
            background.color = new Color(0.02f, 0.13f, 0.16f, 0.97f);
            background.raycastTarget = false;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(indicator.transform, false);
            labelObject.layer = indicator.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _screenTargetLabel = labelObject.GetComponent<TextMeshProUGUI>();
            _screenTargetLabel.text = "ARIA TARGET\n\u25bc";
            _screenTargetLabel.fontStyle = FontStyles.Bold;
            _screenTargetLabel.fontSize = 28f;
            _screenTargetLabel.enableAutoSizing = true;
            _screenTargetLabel.fontSizeMin = 18f;
            _screenTargetLabel.fontSizeMax = 30f;
            _screenTargetLabel.color = new Color(0.38f, 1f, 0.96f, 1f);
            _screenTargetLabel.alignment = TextAlignmentOptions.Center;
            _screenTargetLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _screenTargetLabel.raycastTarget = false;
            indicator.SetActive(false);
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
            WriteWorldRing(center, radius);
            _worldRingRoot.SetActive(true);
        }

        private static void SetAnchoredPositionIfChanged(RectTransform target, Vector2 position)
        {
            if ((target.anchoredPosition - position).sqrMagnitude > 0.25f)
                target.anchoredPosition = position;
        }

        private void EnsureWorldRing()
        {
            if (_worldRingRoot != null && _worldRingRenderer != null)
                return;

            DestroyObject(_worldRingRoot);
            _worldRingRoot = new GameObject(WorldRingName);
            _worldRingMaterial = CreateWorldRingMaterial();
            _worldRingRenderer = _worldRingRoot.AddComponent<LineRenderer>();
            _worldRingRenderer.useWorldSpace = true;
            _worldRingRenderer.loop = true;
            _worldRingRenderer.positionCount = WorldRingSegments;
            _worldRingRenderer.widthMultiplier = WorldRingWidth;
            _worldRingRenderer.numCornerVertices = 4;
            _worldRingRenderer.numCapVertices = 4;
            _worldRingRenderer.alignment = LineAlignment.View;
            _worldRingRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _worldRingRenderer.receiveShadows = false;
            _worldRingRenderer.lightProbeUsage = LightProbeUsage.Off;
            _worldRingRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _worldRingRenderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;
            _worldRingRenderer.allowOcclusionWhenDynamic = false;
            _worldRingRenderer.startColor = new Color(0.34f, 1f, 0.95f, 0.92f);
            _worldRingRenderer.endColor = new Color(0.34f, 0.72f, 1f, 0.92f);
            if (_worldRingMaterial != null)
                _worldRingRenderer.sharedMaterial = _worldRingMaterial;
            _worldRingRoot.SetActive(false);
        }

        private void WriteWorldRing(Vector3 center, float radius)
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
                renderQueue = (int)RenderQueue.Overlay
            };
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_ZTest", (int)CompareFunction.Always);
            material.SetColor("_Color", new Color(0.34f, 1f, 0.95f, 0.92f));
            material.SetColor("_BaseColor", new Color(0.34f, 1f, 0.95f, 0.92f));
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
