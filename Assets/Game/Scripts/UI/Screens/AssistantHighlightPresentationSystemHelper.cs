using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal sealed class AssistantHighlightPresentationSystemHelper
    {
        private const string WorldRingName = "AriaAssistantPreviewHighlightRuntime";
        private const int WorldRingSegments = 96;
        private const float WorldRingRadius = 2.35f;
        private const float WorldRingHeightOffset = 0.38f;
        private const float WorldRingWidth = 0.18f;

        private Image _panelPulse;
        private GameObject _worldRingRoot;
        private LineRenderer _worldRingRenderer;
        private Material _worldRingMaterial;
        private uint _lastVersion = uint.MaxValue;

        public UiAssistantHighlightModel LastAppliedModel { get; private set; } = UiAssistantHighlightModel.Empty;

        public void Bind(Image panelPulse)
        {
            _panelPulse = panelPulse;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
            ApplyVisual(UiAssistantHighlightModel.Empty);
        }

        public void Unbind()
        {
            _panelPulse = null;
            DestroyObject(_worldRingRoot);
            DestroyObject(_worldRingMaterial);
            _worldRingRoot = null;
            _worldRingRenderer = null;
            _worldRingMaterial = null;
            _lastVersion = uint.MaxValue;
            LastAppliedModel = UiAssistantHighlightModel.Empty;
        }

        public void ApplyReadModel(UiAssistantHighlightModel model)
        {
            if (_lastVersion == model.Version)
                return;

            _lastVersion = model.Version;
            LastAppliedModel = model;
            ApplyVisual(model);
        }

        private void ApplyVisual(UiAssistantHighlightModel model)
        {
            if (_panelPulse != null)
            {
                _panelPulse.gameObject.SetActive(model.Active);
                float strength = Mathf.Clamp01(model.Strength);
                _panelPulse.color = new Color(0.45f, 0.95f, 1f, 0.18f + strength * 0.32f);
            }

            ApplyWorldRing(model);
        }

        private void ApplyWorldRing(UiAssistantHighlightModel model)
        {
            if (!model.Active)
            {
                if (_worldRingRoot != null)
                    _worldRingRoot.SetActive(false);
                return;
            }

            EnsureWorldRing();
            if (_worldRingRoot == null || _worldRingRenderer == null)
                return;

            Vector3 center = new(model.WorldX, model.WorldY + WorldRingHeightOffset, model.WorldZ);
            float radius = Mathf.Max(0.35f, WorldRingRadius * (0.75f + Mathf.Clamp01(model.Strength) * 0.25f));
            WriteWorldRing(center, radius);
            _worldRingRoot.SetActive(true);
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
            _worldRingRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _worldRingRenderer.allowOcclusionWhenDynamic = false;
            _worldRingRenderer.startColor = new Color(0.34f, 1f, 0.95f, 0.92f);
            _worldRingRenderer.endColor = new Color(0.34f, 0.72f, 1f, 0.92f);
            if (_worldRingMaterial != null)
                _worldRingRenderer.sharedMaterial = _worldRingMaterial;
            _worldRingRoot.SetActive(false);
        }

        private void WriteWorldRing(Vector3 center, float radius)
        {
            for (int i = 0; i < WorldRingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / WorldRingSegments;
                _worldRingRenderer.SetPosition(
                    i,
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
