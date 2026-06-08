using UnityEngine;

internal sealed class BuildingFactionVisualSystem
{
    public readonly struct Context
    {
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock PropertyBlock;
        public readonly float TintStrength;

        public Context(
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock propertyBlock,
            float tintStrength)
        {
            FactionVisualSettings = factionVisualSettings;
            PropertyBlock = propertyBlock;
            TintStrength = Mathf.Clamp01(tintStrength);
        }
    }

    public void CacheBuildingRenderers(RuntimeBuildingEntity building, Transform visualRoot, Transform destroyedVisual)
    {
        if (building == null || visualRoot == null)
            return;

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            building.FactionVisualRenderers = null;
            building.FactionVisualBaseColors = null;
            return;
        }

        bool excludeRunwayRenderers = building.Definition != null && building.Definition.HasRunway;
        var visibleRenderers = new System.Collections.Generic.List<Renderer>(renderers.Length);
        var baseColors = new System.Collections.Generic.List<Color>(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsUnderTransform(renderer.transform, destroyedVisual))
                continue;
            if (excludeRunwayRenderers && IsRunwayVisual(renderer.transform))
                continue;

            visibleRenderers.Add(renderer);
            baseColors.Add(ResolveBaseColor(renderer));
        }

        building.FactionVisualRenderers = visibleRenderers.ToArray();
        building.FactionVisualBaseColors = baseColors.ToArray();
    }

    public void ApplyOwnerFaction(Context context, RuntimeBuildingEntity building)
    {
        if (building == null)
            return;

        if (!building.HasOwnerFaction)
        {
            Clear(context, building);
            return;
        }

        Color factionColor = ResolveFactionColor(context.FactionVisualSettings, building.OwnerFactionId);
        ApplyTint(context, building, factionColor);
    }

    public void Clear(Context context, RuntimeBuildingEntity building)
    {
        if (building?.FactionVisualRenderers == null)
            return;

        ApplyTint(context, building, Color.white, clearTint: true);
    }

    private void ApplyTint(Context context, RuntimeBuildingEntity building, Color factionColor, bool clearTint = false)
    {
        Renderer[] renderers = building.FactionVisualRenderers;
        Color[] baseColors = building.FactionVisualBaseColors;
        if (renderers == null || renderers.Length == 0 || context.PropertyBlock == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Color baseColor = baseColors != null && i < baseColors.Length ? baseColors[i] : ResolveBaseColor(renderer);
            Color target = clearTint
                ? baseColor
                : Color.Lerp(baseColor, factionColor, context.TintStrength);

            renderer.GetPropertyBlock(context.PropertyBlock);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                context.PropertyBlock.SetColor("_BaseColor", target);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
                context.PropertyBlock.SetColor("_Color", target);
            renderer.SetPropertyBlock(context.PropertyBlock);
        }
    }

    private static Color ResolveFactionColor(FactionVisualSettings factionVisualSettings, byte ownerFactionId)
    {
        if (factionVisualSettings != null)
            return factionVisualSettings.GetColor(ownerFactionId);

        return ownerFactionId switch
        {
            0 => new Color(0.82f, 0.82f, 0.82f, 1f),
            1 => new Color(0.12f, 0.72f, 1f, 1f),
            _ => new Color(0.92f, 0.2f, 0.16f, 1f)
        };
    }

    private static Color ResolveBaseColor(Renderer renderer)
    {
        Material material = renderer != null ? renderer.sharedMaterial : null;
        if (material == null)
            return Color.white;
        if (material.HasProperty("_BaseColor"))
            return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color"))
            return material.GetColor("_Color");
        return Color.white;
    }

    private static bool IsUnderTransform(Transform child, Transform possibleParent)
    {
        if (child == null || possibleParent == null)
            return false;

        Transform current = child;
        while (current != null)
        {
            if (current == possibleParent)
                return true;
            current = current.parent;
        }

        return false;
    }

    private static bool IsRunwayVisual(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.IndexOf("Runway", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            current = current.parent;
        }

        return false;
    }
}
