using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PremiumWorldSelectionFrameView : MonoBehaviour
    {
        private const string ShaderName = "WarlineCapture/Markers/SelectionHologram";
        private const string FallbackShaderName = "Sprites/Default";
        private const string BaseColorProperty = "_BaseColor";
        private const string LegacyColorProperty = "_Color";
        private const string EmissionColorProperty = "_EmissionColor";
        private const string AccentColorProperty = "_AccentColor";
        private const string AlphaProperty = "_Alpha";
        private const int CornerCount = 4;
        private const int CornerBracketCount = CornerCount * 2;
        private const int ScanBandCount = 3;

        private LineRenderer _footprintFrame;
        private LineRenderer _topRim;
        private readonly LineRenderer[] _cornerPosts = new LineRenderer[CornerCount];
        private readonly LineRenderer[] _cornerBrackets = new LineRenderer[CornerBracketCount];
        private readonly LineRenderer[] _scanBands = new LineRenderer[ScanBandCount];
        private Material _runtimeMaterial;

        public void Configure(
            Vector3 center,
            Quaternion rotation,
            Vector2 footprintSize,
            float surfaceY,
            float targetHeight,
            Color baseColor,
            Color accentColor)
        {
            EnsureRenderers();
            ConfigureMaterial(baseColor, accentColor);

            float width = Mathf.Max(0.1f, footprintSize.x);
            float depth = Mathf.Max(0.1f, footprintSize.y);
            float halfX = width * 0.5f;
            float halfZ = depth * 0.5f;
            float longestAxis = Mathf.Max(width, depth);
            float shortestHalfAxis = Mathf.Max(0.2f, Mathf.Min(halfX, halfZ));
            float groundY = surfaceY + 0.08f;
            float topY = surfaceY + Mathf.Clamp(targetHeight + 0.16f, 0.82f, 14f);
            float groundLineWidth = Mathf.Clamp(longestAxis * 0.0055f, 0.018f, 0.055f);
            float rimLineWidth = Mathf.Clamp(longestAxis * 0.01f, 0.034f, 0.105f);
            float postLineWidth = Mathf.Clamp(longestAxis * 0.008f, 0.03f, 0.09f);
            float bracketLength = Mathf.Clamp(longestAxis * 0.16f, 0.55f, shortestHalfAxis * 0.78f);

            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 baseCenter = new(center.x, groundY, center.z);
            Vector3 topCenter = new(center.x, topY, center.z);
            Vector3[] groundCorners =
            {
                baseCenter + right * halfX + forward * halfZ,
                baseCenter - right * halfX + forward * halfZ,
                baseCenter - right * halfX - forward * halfZ,
                baseCenter + right * halfX - forward * halfZ
            };
            Vector3[] topCorners =
            {
                topCenter + right * halfX + forward * halfZ,
                topCenter - right * halfX + forward * halfZ,
                topCenter - right * halfX - forward * halfZ,
                topCenter + right * halfX - forward * halfZ
            };

            ConfigureLoop(_footprintFrame, groundCorners, groundLineWidth, baseColor, alpha: 0.24f);
            ConfigureLoop(_topRim, topCorners, rimLineWidth, accentColor, alpha: 0.9f);

            for (int i = 0; i < CornerCount; i++)
            {
                ConfigureSegment(_cornerPosts[i], groundCorners[i], topCorners[i], postLineWidth, accentColor, alpha: 0.82f);
            }

            ConfigureCornerBrackets(
                groundCorners,
                right,
                forward,
                bracketLength,
                groundLineWidth * 1.12f,
                baseColor);
            ConfigureScanBands(
                topCenter,
                right,
                forward,
                halfX,
                halfZ,
                rimLineWidth * 0.62f,
                accentColor);
        }

        private void ConfigureCornerBrackets(
            Vector3[] corners,
            Vector3 right,
            Vector3 forward,
            float length,
            float lineWidth,
            Color color)
        {
            for (int i = 0; i < CornerCount; i++)
            {
                Vector3 corner = corners[i];
                Vector3 inwardA;
                Vector3 inwardB;
                switch (i)
                {
                    case 0:
                        inwardA = -right;
                        inwardB = -forward;
                        break;
                    case 1:
                        inwardA = right;
                        inwardB = -forward;
                        break;
                    case 2:
                        inwardA = right;
                        inwardB = forward;
                        break;
                    default:
                        inwardA = -right;
                        inwardB = forward;
                        break;
                }

                ConfigureSegment(_cornerBrackets[i * 2], corner, corner + inwardA * length, lineWidth, color, alpha: 0.38f);
                ConfigureSegment(_cornerBrackets[i * 2 + 1], corner, corner + inwardB * length, lineWidth, color, alpha: 0.38f);
            }
        }

        private void ConfigureScanBands(
            Vector3 topCenter,
            Vector3 right,
            Vector3 forward,
            float halfX,
            float halfZ,
            float lineWidth,
            Color color)
        {
            float usableX = Mathf.Max(0.12f, halfX * 0.82f);
            float usableZ = Mathf.Max(0.12f, halfZ * 0.52f);
            float[] offsets = { -0.42f, 0f, 0.42f };
            for (int i = 0; i < _scanBands.Length; i++)
            {
                Vector3 offset = forward * (usableZ * offsets[i]);
                Vector3 start = topCenter - right * usableX + offset;
                Vector3 end = topCenter + right * usableX + offset;
                ConfigureSegment(_scanBands[i], start, end, lineWidth, color, alpha: 0.2f);
            }
        }

        private void EnsureRenderers()
        {
            _footprintFrame ??= CreateLineRenderer("SelectionBoundary_FootprintFrame", loop: true);
            _topRim ??= CreateLineRenderer("SelectionBoundary_TopRim", loop: true);
            for (int i = 0; i < _cornerPosts.Length; i++)
                _cornerPosts[i] ??= CreateLineRenderer($"SelectionBoundary_CornerPost_{i}", loop: false);
            for (int i = 0; i < _cornerBrackets.Length; i++)
                _cornerBrackets[i] ??= CreateLineRenderer($"SelectionBoundary_CornerBracket_{i}", loop: false);
            for (int i = 0; i < _scanBands.Length; i++)
                _scanBands[i] ??= CreateLineRenderer($"SelectionBoundary_ScanBand_{i}", loop: false);
        }

        private LineRenderer CreateLineRenderer(string childName, bool loop)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Tile;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            line.material = ResolveMaterial();
            return line;
        }

        private Material ResolveMaterial()
        {
            if (_runtimeMaterial != null)
                return _runtimeMaterial;

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
                shader = Shader.Find(FallbackShaderName);

            _runtimeMaterial = new Material(shader)
            {
                name = "PremiumWorldSelectionBoundaryMaterial",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Transparent
            };
            return _runtimeMaterial;
        }

        private void ConfigureMaterial(Color baseColor, Color accentColor)
        {
            Material material = ResolveMaterial();
            material.SetColor(BaseColorProperty, baseColor);
            material.SetColor(LegacyColorProperty, baseColor);
            material.SetColor(EmissionColorProperty, baseColor * 1.35f);
            material.SetColor(AccentColorProperty, accentColor);
            material.SetFloat(AlphaProperty, Mathf.Clamp01(baseColor.a));
        }

        private static void ConfigureLoop(LineRenderer line, Vector3[] points, float width, Color color, float alpha)
        {
            line.loop = true;
            line.positionCount = points.Length;
            line.widthMultiplier = width;
            line.startColor = WithAlpha(color, alpha);
            line.endColor = WithAlpha(color, alpha);
            line.SetPositions(points);
        }

        private static void ConfigureSegment(
            LineRenderer line,
            Vector3 start,
            Vector3 end,
            float width,
            Color color,
            float alpha)
        {
            line.loop = false;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.startColor = WithAlpha(color, alpha);
            line.endColor = WithAlpha(color, alpha);
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(_runtimeMaterial);
            else
                DestroyImmediate(_runtimeMaterial);
        }
    }
}
