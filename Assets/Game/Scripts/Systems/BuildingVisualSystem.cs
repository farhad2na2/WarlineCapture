using System.Collections.Generic;
using System.Globalization;
using Unity.Entities;
using UnityEngine;

public sealed partial class BuildingVisualSystem : SystemBase
{
    private const string BaseColorProperty = "_BaseColor";
    private const string LegacyColorProperty = "_Color";
    private const string EmissionColorProperty = "_EmissionColor";
    private const string AccentColorProperty = "_AccentColor";

    public sealed class AnimatedPart
    {
        public Transform Transform;
        public Vector3 BaseLocalEulerAngles;
        public Vector3 Axis;
        public float AngleLimit;
        public float PhaseOffset;
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public void ApplyMarkerColor(Renderer[] renderers, Color color, MaterialPropertyBlock propertyBlock)
    {
        if (renderers == null || propertyBlock == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(propertyBlock);
            Material material = renderer.sharedMaterial;
            if (material != null && material.HasProperty(BaseColorProperty))
                propertyBlock.SetColor(BaseColorProperty, color);
            if (material != null && material.HasProperty(LegacyColorProperty))
                propertyBlock.SetColor(LegacyColorProperty, color);
            if (material != null && material.HasProperty(EmissionColorProperty))
                propertyBlock.SetColor(EmissionColorProperty, color);
            if (material != null && material.HasProperty(AccentColorProperty))
                propertyBlock.SetColor(AccentColorProperty, color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void SetTransformVisible(Transform target, bool visible)
    {
        if (target == null)
            return;

        if (target.gameObject.activeSelf == visible)
            return;

        target.gameObject.SetActive(visible);
    }

    public Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;
        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    public AnimatedPart[] FindAnimatedBuildingParts(Transform root)
    {
        if (root == null)
            return null;

        List<AnimatedPart> matches = null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (!TryParseAnimatedPartName(child.name, out Vector3 axis, out float angleLimit))
                continue;

            matches ??= new List<AnimatedPart>();
            matches.Add(new AnimatedPart
            {
                Transform = child,
                BaseLocalEulerAngles = child.localEulerAngles,
                Axis = axis,
                AngleLimit = angleLimit,
                PhaseOffset = matches.Count * 0.35f
            });
        }

        return matches?.ToArray();
    }

    public void UpdateAnimatedBuildingParts(AnimatedPart[] animatedParts, bool active, float time)
    {
        if (animatedParts == null)
            return;

        for (int i = 0; i < animatedParts.Length; i++)
        {
            AnimatedPart part = animatedParts[i];
            if (part?.Transform == null)
                continue;

            Vector3 localEuler = part.BaseLocalEulerAngles;
            if (active)
            {
                float angle = Mathf.Sin((time * 1.5f) + part.PhaseOffset) * part.AngleLimit;
                localEuler += part.Axis * angle;
            }

            part.Transform.localEulerAngles = localEuler;
        }
    }

    private static bool TryParseAnimatedPartName(string name, out Vector3 axis, out float angleLimit)
    {
        axis = Vector3.zero;
        angleLimit = 0f;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore <= 0 || lastUnderscore >= name.Length - 1)
            return false;

        string angleToken = name[(lastUnderscore + 1)..];
        if (!float.TryParse(angleToken, NumberStyles.Float, CultureInfo.InvariantCulture, out angleLimit))
            return false;

        int axisUnderscore = name.LastIndexOf('_', lastUnderscore - 1);
        if (axisUnderscore <= 0 || axisUnderscore >= lastUnderscore - 1)
            return false;

        string axisToken = name[(axisUnderscore + 1)..lastUnderscore];
        axis = axisToken switch
        {
            "X" => Vector3.right,
            "Y" => Vector3.up,
            "Z" => Vector3.forward,
            _ => Vector3.zero
        };

        return axis != Vector3.zero && angleLimit > 0f;
    }
}
