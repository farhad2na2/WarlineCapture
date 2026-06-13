using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class PremiumWorldSelectionObjectOutlineView : MonoBehaviour
{
    private const string ShaderName = "WarlineCapture/Markers/SelectionObjectOutline";
    private const string BaseColorProperty = "_BaseColor";
    private const string EmissionColorProperty = "_EmissionColor";
    private const string OutlineWidthProperty = "_OutlineWidth";
    private const string OutlineAlphaProperty = "_OutlineAlpha";
    private const string RimAlphaProperty = "_RimAlpha";
    private const string RimPowerProperty = "_RimPower";
    private const string ScanStrengthProperty = "_ScanStrength";
    private const string ScanSpeedProperty = "_ScanSpeed";

    private readonly List<GameObject> _overlays = new();
    private Material _runtimeMaterial;
    private GameObject _target;

    public void Configure(GameObject target, Color baseColor, Color emissionColor, float outlineWidth)
    {
        if (target == null)
        {
            Hide();
            return;
        }

        EnsureMaterial(baseColor, emissionColor, outlineWidth);

        if (_target != target || _overlays.Count == 0)
        {
            ClearOverlays();
            _target = target;
            CreateOverlays(target);
        }

        for (int i = 0; i < _overlays.Count; i++)
        {
            if (_overlays[i] != null)
                _overlays[i].SetActive(true);
        }
    }

    public void Hide()
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (_overlays[i] != null)
                _overlays[i].SetActive(false);
        }
    }

    public void ClearOverlays()
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            GameObject overlay = _overlays[i];
            if (overlay == null)
                continue;

            if (Application.isPlaying)
                Destroy(overlay);
            else
                DestroyImmediate(overlay);
        }

        _overlays.Clear();
        _target = null;
    }

    private void CreateOverlays(GameObject target)
    {
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer sourceRenderer = renderers[i];
            if (!CanOutline(sourceRenderer))
                continue;

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            GameObject overlay = new("SelectionObjectOutline_" + sourceRenderer.name);
            overlay.layer = sourceRenderer.gameObject.layer;
            overlay.transform.SetParent(sourceRenderer.transform, false);
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one;

            MeshFilter filter = overlay.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer renderer = overlay.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _runtimeMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.renderingLayerMask = sourceRenderer.renderingLayerMask;
            renderer.sortingOrder = sourceRenderer.sortingOrder + 4;
            _overlays.Add(overlay);
        }
    }

    private static bool CanOutline(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled)
            return false;

        string path = renderer.transform.name;
        Transform current = renderer.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return !ContainsMarkerName(path, "SelectionMarker") &&
               !ContainsMarkerName(path, "FactionMarker") &&
               !ContainsMarkerName(path, "HealthBar") &&
               !ContainsMarkerName(path, "PlacementOutline") &&
               !ContainsMarkerName(path, "SelectionObjectOutline");
    }

    private static bool ContainsMarkerName(string path, string token)
    {
        return path.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void EnsureMaterial(Color baseColor, Color emissionColor, float outlineWidth)
    {
        if (_runtimeMaterial == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            _runtimeMaterial = new Material(shader)
            {
                name = "PremiumWorldSelectionObjectOutlineMaterial",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Transparent + 5
            };
        }

        _runtimeMaterial.SetColor(BaseColorProperty, baseColor);
        _runtimeMaterial.SetColor(EmissionColorProperty, emissionColor);
        _runtimeMaterial.SetFloat(OutlineWidthProperty, Mathf.Clamp(outlineWidth, 0.006f, 0.12f));
        _runtimeMaterial.SetFloat(OutlineAlphaProperty, 0.88f);
        _runtimeMaterial.SetFloat(RimAlphaProperty, 0.3f);
        _runtimeMaterial.SetFloat(RimPowerProperty, 2.2f);
        _runtimeMaterial.SetFloat(ScanStrengthProperty, 0.12f);
        _runtimeMaterial.SetFloat(ScanSpeedProperty, 0.24f);
    }

    private void OnDestroy()
    {
        ClearOverlays();
        if (_runtimeMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(_runtimeMaterial);
        else
            DestroyImmediate(_runtimeMaterial);
    }
}
