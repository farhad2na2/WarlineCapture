using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Chapter01M01SpriteAssetResolver
{
    public const string ManifestPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset";
    public const string ScaleContractPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_scale_contract.asset";

    private static readonly Dictionary<string, Sprite> SpriteCache = new();
#if UNITY_EDITOR
    private const string IndividualSoldierSheetPath = "Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png";
    private const string SoldierIdleSeSpriteName = "Unit_Chr_Soldier_Male_02_Idle_SE";
    private const string SoldierMoveSeSpriteName = "Unit_Chr_Soldier_Male_02_Run_SE";
    private const string SoldierAttackSeSpriteName = "Unit_Chr_Soldier_Male_02_Aim_SE";
    private const string SoldierDamagedSeSpriteName = "Unit_Chr_Soldier_Male_02_Hit_SE";
#endif

    public static bool TryGetSprite(string spriteId, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(spriteId))
            return false;

        if (SpriteCache.TryGetValue(spriteId, out sprite))
            return sprite != null;

#if UNITY_EDITOR
        if (TryResolveM01IndividualSoldierSprite(spriteId, out sprite))
        {
            SpriteCache[spriteId] = sprite;
            return true;
        }

        Chapter01TacticalAssetManifest manifest = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAssetManifest>(ManifestPath);
        if (manifest == null)
            return false;

        string manifestAssetId = spriteId;
        if (!manifest.TryGetEntry(manifestAssetId, out TacticalAssetManifestEntry entry))
        {
            string fallbackAssetId = ResolveM01StateFallbackAssetId(spriteId);
            if (fallbackAssetId == spriteId || !manifest.TryGetEntry(fallbackAssetId, out entry))
                return false;

            manifestAssetId = fallbackAssetId;
        }

        if (!TryCreateSpriteFromPng(entry.PlannedPath, out sprite))
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.PlannedPath);
        SpriteCache[spriteId] = sprite;
        if (manifestAssetId != spriteId)
            SpriteCache[manifestAssetId] = sprite;
        return sprite != null;
#else
        return false;
#endif
    }

    public static bool TryGetScale(string spriteId, out float scale)
    {
        scale = 1f;
#if UNITY_EDITOR
        Chapter01TacticalAssetManifest manifest = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAssetManifest>(ManifestPath);
        Chapter01TacticalScaleContract scaleContract = AssetDatabase.LoadAssetAtPath<Chapter01TacticalScaleContract>(ScaleContractPath);
        if (manifest == null || scaleContract == null || !manifest.TryGetEntry(spriteId, out TacticalAssetManifestEntry entry) || !entry.UsesScaleRole)
            return false;

        scale = scaleContract.GetScale(entry.ScaleRole);
        return true;
#else
        return false;
#endif
    }

    public static void ClearCache()
    {
        SpriteCache.Clear();
    }

#if UNITY_EDITOR
    private static bool TryResolveM01IndividualSoldierSprite(string spriteId, out Sprite sprite)
    {
        sprite = null;
        if (!TryResolveM01IndividualSoldierSpriteName(spriteId, out string spriteName))
            return false;

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(IndividualSoldierSheetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite candidate && candidate.name == spriteName)
            {
                sprite = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveM01IndividualSoldierSpriteName(string spriteId, out string spriteName)
    {
        spriteName = null;
        if (string.IsNullOrEmpty(spriteId))
            return false;

        bool coveredUnit =
            spriteId.StartsWith(Chapter01M01PlayableRuntime.PlayerSquadEntityId, System.StringComparison.Ordinal) ||
            spriteId.StartsWith(Chapter01M01PlayableRuntime.EnemyPatrolEntityId, System.StringComparison.Ordinal);
        if (!coveredUnit)
            return false;

        if (spriteId.EndsWith(Chapter01M01SpritePresenterCatalog.IdleStateSuffix, System.StringComparison.Ordinal))
            spriteName = SoldierIdleSeSpriteName;
        else if (spriteId.EndsWith(Chapter01M01SpritePresenterCatalog.MoveStateSuffix, System.StringComparison.Ordinal))
            spriteName = SoldierMoveSeSpriteName;
        else if (spriteId.EndsWith(Chapter01M01SpritePresenterCatalog.AttackStateSuffix, System.StringComparison.Ordinal))
            spriteName = SoldierAttackSeSpriteName;
        else if (spriteId.EndsWith(Chapter01M01SpritePresenterCatalog.DamagedStateSuffix, System.StringComparison.Ordinal))
            spriteName = SoldierDamagedSeSpriteName;

        return !string.IsNullOrEmpty(spriteName);
    }

    private static string ResolveM01StateFallbackAssetId(string spriteId)
    {
        if (string.IsNullOrEmpty(spriteId))
            return spriteId;

        string[] suffixes =
        {
            Chapter01M01SpritePresenterCatalog.IdleStateSuffix,
            Chapter01M01SpritePresenterCatalog.MoveStateSuffix,
            Chapter01M01SpritePresenterCatalog.AttackStateSuffix,
            Chapter01M01SpritePresenterCatalog.DamagedStateSuffix
        };

        for (int i = 0; i < suffixes.Length; i++)
        {
            string suffix = suffixes[i];
            if (spriteId.EndsWith(suffix, System.StringComparison.Ordinal))
                return spriteId.Substring(0, spriteId.Length - suffix.Length);
        }

        return spriteId;
    }

    private static bool TryCreateSpriteFromPng(string assetPath, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(assetPath) || Path.GetExtension(assetPath).ToLowerInvariant() != ".png")
            return false;

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        if (!File.Exists(fullPath))
            return false;

        byte[] bytes = File.ReadAllBytes(fullPath);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false)
        {
            name = Path.GetFileNameWithoutExtension(assetPath),
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        if (!ImageConversion.LoadImage(texture, bytes))
        {
            Object.DestroyImmediate(texture);
            return false;
        }

        float pixelsPerUnit = 100f;
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            pixelsPerUnit = importer.spritePixelsPerUnit;

        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            1u,
            SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return true;
    }
#endif
}
