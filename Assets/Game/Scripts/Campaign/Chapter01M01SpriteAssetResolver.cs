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
    private const string SoldierAnimationManifestV2Path = "Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json";
    private const string IndividualSoldierSheetPath = "Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png";
    private const string SoldierIdleSeSpriteName = "Unit_Chr_Soldier_Male_02_Idle_SE";
    private const string SoldierMoveSeSpriteName = "Unit_Chr_Soldier_Male_02_Run_SE";
    private const string SoldierAttackSeSpriteName = "Unit_Chr_Soldier_Male_02_Aim_SE";
    private const string SoldierDamagedSeSpriteName = "Unit_Chr_Soldier_Male_02_Hit_SE";
    private static SoldierAnimationManifestV2 CachedSoldierManifestV2;
    private static string CachedSoldierManifestV2Json;
#endif

    public readonly struct M01SoldierAnimationFrame
    {
        public readonly Texture2D Texture;
        public readonly Rect AtlasRect;
        public readonly Vector2 TextureScale;
        public readonly Vector2 TextureOffset;
        public readonly Vector2 PivotNormalizedUnitySprite;
        public readonly Rect NormalizedBounds;
        public readonly string Facing;
        public readonly string State;
        public readonly int FrameIndex;
        public readonly float Fps;
        public readonly bool Loop;
        public readonly string FrameKey;

        public M01SoldierAnimationFrame(
            Texture2D texture,
            Rect atlasRect,
            Vector2 pivotNormalizedUnitySprite,
            Rect normalizedBounds,
            string facing,
            string state,
            int frameIndex,
            float fps,
            bool loop)
        {
            Texture = texture;
            AtlasRect = atlasRect;
            TextureScale = texture != null && texture.width > 0 && texture.height > 0
                ? new Vector2(atlasRect.width / texture.width, atlasRect.height / texture.height)
                : Vector2.one;
            TextureOffset = texture != null && texture.width > 0 && texture.height > 0
                ? new Vector2(atlasRect.x / texture.width, 1f - ((atlasRect.y + atlasRect.height) / texture.height))
                : Vector2.zero;
            PivotNormalizedUnitySprite = pivotNormalizedUnitySprite;
            NormalizedBounds = normalizedBounds;
            Facing = facing;
            State = state;
            FrameIndex = frameIndex;
            Fps = fps;
            Loop = loop;
            FrameKey = $"{state}.{facing}.{frameIndex}";
        }
    }

    public static bool TryGetSprite(string spriteId, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(spriteId))
            return false;

        if (SpriteCache.TryGetValue(spriteId, out sprite))
            return sprite != null;

#if UNITY_EDITOR
        if (TryResolveM01V2SoldierSprite(spriteId, out sprite))
        {
            SpriteCache[spriteId] = sprite;
            return true;
        }

        if (IsM01SoldierSpriteId(spriteId))
            return false;

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

    public static bool TryGetM01SoldierAnimationFrame(
        string runtimeEntityId,
        MissionRuntimeSpriteVisualState visualState,
        string facing,
        float elapsedSeconds,
        out M01SoldierAnimationFrame frame)
    {
        frame = default;
#if UNITY_EDITOR
        if (!TryGetM01SoldierFaction(runtimeEntityId, out string factionId) ||
            !TryLoadSoldierManifestV2(out SoldierAnimationManifestV2 manifest))
        {
            return false;
        }

        SoldierAnimationFactionV2 faction = manifest.factions.Resolve(factionId);
        if (faction == null || faction.animations == null || faction.animations.Length == 0)
            return false;

        string stateId = ResolveSoldierAnimationStateId(visualState);
        string facingId = NormalizeFacing(facing);
        SoldierAnimationClipV2 clip = FindClip(faction, stateId, facingId) ??
                                      FindClip(faction, stateId, "SE") ??
                                      FindClip(faction, "idle", facingId) ??
                                      FindClip(faction, "idle", "SE");
        if (clip == null || clip.frames == null || clip.frames.Length == 0)
            return false;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(faction.runtime_atlas);
        if (texture == null)
            return false;

        int frameSlot = ResolveFrameSlot(clip, elapsedSeconds);
        SoldierAnimationFrameV2 sourceFrame = ResolveFrame(clip, frameSlot);
        if (sourceFrame == null || sourceFrame.atlas_rect == null || sourceFrame.atlas_rect.Length < 4)
            return false;

        Rect atlasRect = new(sourceFrame.atlas_rect[0], sourceFrame.atlas_rect[1], sourceFrame.atlas_rect[2], sourceFrame.atlas_rect[3]);
        Vector2 pivot = ResolveVector2(clip.pivot_normalized_unity_sprite, new Vector2(0.5f, 0.054688f));
        Rect normalizedBounds = ResolveRect(sourceFrame.normalized_bounds, new Rect(0f, 0f, 1f, 1f));
        frame = new M01SoldierAnimationFrame(
            texture,
            atlasRect,
            pivot,
            normalizedBounds,
            clip.facing,
            clip.state,
            sourceFrame.frame_index,
            Mathf.Max(1f, clip.suggested_fps),
            clip.loop);
        return true;
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
#if UNITY_EDITOR
        CachedSoldierManifestV2 = null;
        CachedSoldierManifestV2Json = null;
#endif
    }

#if UNITY_EDITOR
    private static bool TryResolveM01V2SoldierSprite(string spriteId, out Sprite sprite)
    {
        sprite = null;
        if (!TryResolveM01SoldierVisualState(spriteId, out string runtimeEntityId, out MissionRuntimeSpriteVisualState visualState))
            return false;

        if (!TryGetM01SoldierAnimationFrame(runtimeEntityId, visualState, "SE", 0f, out M01SoldierAnimationFrame frame) ||
            frame.Texture == null)
        {
            return false;
        }

        Rect unityRect = new(
            frame.AtlasRect.x,
            frame.Texture.height - frame.AtlasRect.y - frame.AtlasRect.height,
            frame.AtlasRect.width,
            frame.AtlasRect.height);
        sprite = Sprite.Create(
            frame.Texture,
            unityRect,
            frame.PivotNormalizedUnitySprite,
            256f,
            1u,
            SpriteMeshType.FullRect);
        sprite.name = $"{Path.GetFileNameWithoutExtension(frame.Texture.name)}_{frame.State}_{frame.Facing}_{frame.FrameIndex:00}";
        return true;
    }

    private static bool TryResolveM01SoldierVisualState(string spriteId, out string runtimeEntityId, out MissionRuntimeSpriteVisualState visualState)
    {
        runtimeEntityId = null;
        visualState = MissionRuntimeSpriteVisualState.Idle;
        if (string.IsNullOrEmpty(spriteId))
            return false;

        if (TryMatchSoldierSpriteId(spriteId, Chapter01M01PlayableRuntime.PlayerSquadEntityId, out visualState))
        {
            runtimeEntityId = Chapter01M01PlayableRuntime.PlayerSquadEntityId;
            return true;
        }

        if (TryMatchSoldierSpriteId(spriteId, Chapter01M01PlayableRuntime.EnemyPatrolEntityId, out visualState))
        {
            runtimeEntityId = Chapter01M01PlayableRuntime.EnemyPatrolEntityId;
            return true;
        }

        return false;
    }

    private static bool TryMatchSoldierSpriteId(string spriteId, string runtimeEntityId, out MissionRuntimeSpriteVisualState visualState)
    {
        visualState = MissionRuntimeSpriteVisualState.Idle;
        if (spriteId == runtimeEntityId || spriteId == runtimeEntityId + Chapter01M01SpritePresenterCatalog.IdleStateSuffix)
            return true;
        if (spriteId == runtimeEntityId + Chapter01M01SpritePresenterCatalog.MoveStateSuffix)
        {
            visualState = MissionRuntimeSpriteVisualState.Move;
            return true;
        }
        if (spriteId == runtimeEntityId + Chapter01M01SpritePresenterCatalog.AttackStateSuffix)
        {
            visualState = MissionRuntimeSpriteVisualState.Attack;
            return true;
        }
        if (spriteId == runtimeEntityId + Chapter01M01SpritePresenterCatalog.DamagedStateSuffix)
        {
            visualState = MissionRuntimeSpriteVisualState.Damaged;
            return true;
        }
        if (spriteId == runtimeEntityId + Chapter01M01SpritePresenterCatalog.DeathStateSuffix)
        {
            visualState = MissionRuntimeSpriteVisualState.Destroyed;
            return true;
        }

        return false;
    }

    private static bool IsM01SoldierSpriteId(string spriteId)
    {
        return TryResolveM01SoldierVisualState(spriteId, out _, out _);
    }

    private static bool TryGetM01SoldierFaction(string runtimeEntityId, out string factionId)
    {
        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId)
        {
            factionId = "player_rifle_squad";
            return true;
        }

        if (runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            factionId = "enemy_patrol";
            return true;
        }

        factionId = null;
        return false;
    }

    private static bool TryLoadSoldierManifestV2(out SoldierAnimationManifestV2 manifest)
    {
        manifest = null;
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SoldierAnimationManifestV2Path);
        string json = asset != null ? asset.text : File.Exists(SoldierAnimationManifestV2Path) ? File.ReadAllText(SoldierAnimationManifestV2Path) : null;
        if (string.IsNullOrEmpty(json))
            return false;

        if (CachedSoldierManifestV2 != null && CachedSoldierManifestV2Json == json)
        {
            manifest = CachedSoldierManifestV2;
            return true;
        }

        CachedSoldierManifestV2 = JsonUtility.FromJson<SoldierAnimationManifestV2>(json);
        CachedSoldierManifestV2Json = json;
        manifest = CachedSoldierManifestV2;
        return manifest != null && manifest.factions != null;
    }

    private static SoldierAnimationClipV2 FindClip(SoldierAnimationFactionV2 faction, string stateId, string facingId)
    {
        for (int i = 0; i < faction.animations.Length; i++)
        {
            SoldierAnimationClipV2 clip = faction.animations[i];
            if (clip != null &&
                string.Equals(clip.state, stateId, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(clip.facing, facingId, System.StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }

        return null;
    }

    private static string ResolveSoldierAnimationStateId(MissionRuntimeSpriteVisualState visualState)
    {
        return visualState switch
        {
            MissionRuntimeSpriteVisualState.Move => "run",
            MissionRuntimeSpriteVisualState.Attack => "fire",
            MissionRuntimeSpriteVisualState.Damaged => "damaged",
            MissionRuntimeSpriteVisualState.Destroyed => "death",
            _ => "idle"
        };
    }

    private static string NormalizeFacing(string facing)
    {
        return facing switch
        {
            "NE" => "NE",
            "NW" => "NW",
            "SW" => "SW",
            _ => "SE"
        };
    }

    private static int ResolveFrameSlot(SoldierAnimationClipV2 clip, float elapsedSeconds)
    {
        int frameCount = clip.frames != null ? clip.frames.Length : 0;
        if (frameCount <= 1)
            return 0;

        float fps = Mathf.Max(1f, clip.suggested_fps);
        int rawSlot = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps);
        int orderLength = clip.frame_order != null && clip.frame_order.Length > 0 ? clip.frame_order.Length : frameCount;
        int orderSlot = clip.loop ? rawSlot % orderLength : Mathf.Min(rawSlot, orderLength - 1);
        if (clip.frame_order != null && clip.frame_order.Length > orderSlot)
            return Mathf.Clamp(clip.frame_order[orderSlot], 0, frameCount - 1);

        return Mathf.Clamp(orderSlot, 0, frameCount - 1);
    }

    private static SoldierAnimationFrameV2 ResolveFrame(SoldierAnimationClipV2 clip, int frameSlot)
    {
        if (clip.frames == null || clip.frames.Length == 0)
            return null;
        for (int i = 0; i < clip.frames.Length; i++)
        {
            SoldierAnimationFrameV2 frame = clip.frames[i];
            if (frame != null && frame.frame_index == frameSlot)
                return frame;
        }

        return clip.frames[Mathf.Clamp(frameSlot, 0, clip.frames.Length - 1)];
    }

    private static Vector2 ResolveVector2(float[] values, Vector2 fallback)
    {
        return values != null && values.Length >= 2 ? new Vector2(values[0], values[1]) : fallback;
    }

    private static Rect ResolveRect(float[] values, Rect fallback)
    {
        return values != null && values.Length >= 4 ? new Rect(values[0], values[1], values[2] - values[0], values[3] - values[1]) : fallback;
    }

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

    [System.Serializable]
    private sealed class SoldierAnimationManifestV2
    {
        public SoldierAnimationFactionsV2 factions;
    }

    [System.Serializable]
    private sealed class SoldierAnimationFactionsV2
    {
        public SoldierAnimationFactionV2 player_rifle_squad;
        public SoldierAnimationFactionV2 enemy_patrol;

        public SoldierAnimationFactionV2 Resolve(string factionId)
        {
            return factionId switch
            {
                "player_rifle_squad" => player_rifle_squad,
                "enemy_patrol" => enemy_patrol,
                _ => null
            };
        }
    }

    [System.Serializable]
    private sealed class SoldierAnimationFactionV2
    {
        public string runtime_atlas;
        public int[] atlas_size;
        public int[] atlas_cell_size;
        public int atlas_columns;
        public SoldierAnimationClipV2[] animations;
    }

    [System.Serializable]
    private sealed class SoldierAnimationClipV2
    {
        public string facing;
        public string state;
        public int frame_count;
        public int[] frame_order;
        public int suggested_fps;
        public bool loop;
        public SoldierAnimationFrameV2[] frames;
        public int[] pivot_px;
        public int[] foot_anchor_px;
        public float[] pivot_normalized_unity_sprite;
        public int contact_band_height_px;
    }

    [System.Serializable]
    private sealed class SoldierAnimationFrameV2
    {
        public int frame_index;
        public string runtime_file;
        public int[] atlas_rect;
        public int[] pivot_px;
        public int[] foot_anchor_px;
        public int[] alpha_bounds_px;
        public int[] contact_bounds_px;
        public float[] normalized_bounds;
    }
#endif
}
