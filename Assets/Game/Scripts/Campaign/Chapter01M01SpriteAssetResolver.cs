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
    public const string M01AiProductionManifestPath = "Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.json";
    public const string M01ProductionStrategicBackgroundAssetId = "strategic.ch01.m01.background";
    public const string M01ProductionTacticalPlateAAssetId = "iso.ch01.m01.plate_a.ground";
    public const string M01ProductionSelectionMarkerAssetId = "marker.selection.ring";
    public const string M01ProductionMoveDestinationMarkerAssetId = "marker.move.destination";
    public const string M01ProductionAttackTargetMarkerAssetId = "marker.attack.target";
    public const string M01ProductionEnemyReadabilityMarkerAssetId = "marker.enemy.readability";
    public const string M01ProductionEnemyHealthBarAssetId = "marker.enemy.health_bar";

    private static readonly Dictionary<string, Sprite> SpriteCache = new();
#if UNITY_EDITOR
    private const string SoldierAnimationManifestV2Path = "Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json";
    private const string SoldierAnimationManifestV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest_v5.json";
    private const string Soldier8DirectionManifestV32Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v32.json";
    private const string Soldier8DirectionManifestV31Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_8dir_soldier_manifest_v31.json";
    private const string SoldierDirectionLockedManifestV29Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json";
    private const string SoldierDirectionLockedManifestV28Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v28.json";
    private const string SoldierBakedShadowManifestV17Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v17.json";
    private const string M01AcceptedTacticalGroundV29Runtime16x9Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_runtime_16x9_1920x1080.png";
    private const string M01AcceptedTacticalGroundV29Runtime20x9Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_runtime_20x9_2400x1080.png";
    private const string M01AcceptedTacticalGroundV29Runtime21x9Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_runtime_21x9_2520x1080.png";
    private const string M01AcceptedTacticalGroundV29Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v29_overscan_pot_4096x2048.png";
    private const string M01AcceptedTacticalGroundV6Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png";
    private const string M01AcceptedShadowAtlasV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/AnimationV5/unit_shadow_animation_atlas_v5_strong.png";
    private const string M01TargetMatchPlayerIdleAtlasV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/TargetMatchV5/player_rifle_squad_idle_facings_atlas_v5.png";
    private const string M01TargetMatchEnemyIdleAtlasV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/TargetMatchV5/enemy_patrol_idle_facings_atlas_v5.png";
    private const string M01TargetMatchShadowAtlasV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/TargetMatchV5/unit_shadow_facings_atlas_v5_strong.png";
    private const string M01AcceptedSelectionMarkerV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/selection_ring_v5.png";
    private const string M01AcceptedEnemyReadabilityMarkerV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/enemy_readability_ring_v5.png";
    private const string M01AcceptedEnemyHealthBarV5Path = "Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/enemy_health_bar_v5.png";
    private const string IndividualSoldierSheetPath = "Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png";
    private const string SoldierIdleSeSpriteName = "Unit_Chr_Soldier_Male_02_Idle_SE";
    private const string SoldierMoveSeSpriteName = "Unit_Chr_Soldier_Male_02_Run_SE";
    private const string SoldierAttackSeSpriteName = "Unit_Chr_Soldier_Male_02_Aim_SE";
    private const string SoldierDamagedSeSpriteName = "Unit_Chr_Soldier_Male_02_Hit_SE";
    private static SoldierAnimationManifestV2 CachedSoldierManifestV2;
    private static string CachedSoldierManifestV2Json;
    private static Soldier8DirectionManifestV32 CachedSoldierManifestV32;
    private static string CachedSoldierManifestV32Json;
    private static Soldier8DirectionManifestV31 CachedSoldierManifestV31;
    private static string CachedSoldierManifestV31Json;
    private static SoldierDirectionLockedManifestV29 CachedSoldierManifestV29;
    private static string CachedSoldierManifestV29Json;
    private static SoldierDirectionLockedManifestV28 CachedSoldierManifestV28;
    private static string CachedSoldierManifestV28Json;
    private static SoldierBakedShadowManifestV17 CachedSoldierManifestV17;
    private static string CachedSoldierManifestV17Json;
    private static AiProductionAssetManifest CachedAiProductionManifest;
    private static string CachedAiProductionManifestJson;
#endif

    public readonly struct M01SoldierAnimationFrame
    {
        public readonly Texture2D Texture;
        public readonly Texture2D ShadowTexture;
        public readonly Texture2D FactionMaskTexture;
        public readonly Rect AtlasRect;
        public readonly Vector2 TextureScale;
        public readonly Vector2 TextureOffset;
        public readonly Vector2 ShadowTextureScale;
        public readonly Vector2 ShadowTextureOffset;
        public readonly Vector2 FactionMaskTextureScale;
        public readonly Vector2 FactionMaskTextureOffset;
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
            Texture2D shadowTexture,
            Texture2D factionMaskTexture,
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
            ShadowTexture = shadowTexture;
            FactionMaskTexture = factionMaskTexture;
            AtlasRect = atlasRect;
            TextureScale = texture != null && texture.width > 0 && texture.height > 0
                ? new Vector2(atlasRect.width / texture.width, atlasRect.height / texture.height)
                : Vector2.one;
            TextureOffset = texture != null && texture.width > 0 && texture.height > 0
                ? new Vector2(atlasRect.x / texture.width, 1f - ((atlasRect.y + atlasRect.height) / texture.height))
                : Vector2.zero;
            ShadowTextureScale = shadowTexture != null && shadowTexture.width > 0 && shadowTexture.height > 0
                ? new Vector2(atlasRect.width / shadowTexture.width, atlasRect.height / shadowTexture.height)
                : TextureScale;
            ShadowTextureOffset = shadowTexture != null && shadowTexture.width > 0 && shadowTexture.height > 0
                ? new Vector2(atlasRect.x / shadowTexture.width, 1f - ((atlasRect.y + atlasRect.height) / shadowTexture.height))
                : TextureOffset;
            FactionMaskTextureScale = factionMaskTexture != null && factionMaskTexture.width > 0 && factionMaskTexture.height > 0
                ? new Vector2(atlasRect.width / factionMaskTexture.width, atlasRect.height / factionMaskTexture.height)
                : TextureScale;
            FactionMaskTextureOffset = factionMaskTexture != null && factionMaskTexture.width > 0 && factionMaskTexture.height > 0
                ? new Vector2(atlasRect.x / factionMaskTexture.width, 1f - ((atlasRect.y + atlasRect.height) / factionMaskTexture.height))
                : TextureOffset;
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

        if (TryResolveM01ProductionSpritePath(spriteId, out string productionAssetPath) &&
            TryCreateSpriteFromPng(productionAssetPath, out sprite))
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

    public static bool TryGetM01ProductionTacticalGroundSprite(out Sprite sprite)
    {
        sprite = null;
#if UNITY_EDITOR
        if (TryCreateSpriteFromPng(ResolveM01V29RuntimeTacticalGroundPath(), out sprite))
            return true;

        if (TryCreateSpriteFromPng(M01AcceptedTacticalGroundV29Path, out sprite))
            return true;

        if (TryCreateSpriteFromPng(M01AcceptedTacticalGroundV6Path, out sprite))
            return true;

        if (!TryGetM01ProductionAssetPath(M01ProductionTacticalPlateAAssetId, out string path))
            return false;

        if (!TryCreateSpriteFromPng(path, out sprite))
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return sprite != null;
#else
        return false;
#endif
    }

    public static bool TryGetM01ProductionTacticalGroundSprites(out Sprite[] sprites)
    {
        sprites = System.Array.Empty<Sprite>();
#if UNITY_EDITOR
        if (!TryLoadAiProductionManifest(out AiProductionAssetManifest manifest) ||
            manifest.tactical_maps == null ||
            manifest.tactical_maps.Length == 0)
        {
            return false;
        }

        List<Sprite> resolved = new();
        for (int i = 0; i < manifest.tactical_maps.Length; i++)
        {
            string path = manifest.tactical_maps[i] != null ? ResolveTacticalMapRuntimePath(manifest.tactical_maps[i]) : null;
            if (string.IsNullOrEmpty(path))
                continue;

            if (!TryCreateSpriteFromPng(path, out Sprite sprite))
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                resolved.Add(sprite);
        }

        sprites = resolved.ToArray();
        return sprites.Length == manifest.tactical_maps.Length;
#else
        return false;
#endif
    }

    public static bool TryGetM01ProductionStrategicBackgroundSprite(out Sprite sprite)
    {
        sprite = null;
#if UNITY_EDITOR
        if (!TryGetM01ProductionAssetPath(M01ProductionStrategicBackgroundAssetId, out string path))
            return false;

        if (!TryCreateSpriteFromPng(path, out sprite))
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return sprite != null;
#else
        return false;
#endif
    }

    public static bool TryGetM01ProductionMarkerTexture(string markerAssetId, out Texture2D texture)
    {
        texture = null;
#if UNITY_EDITOR
        if (TryGetAcceptedMarkerPath(markerAssetId, out string acceptedMarkerPath))
        {
            if (!TryLoadTextureFromPng(acceptedMarkerPath, out texture))
                return false;

            if (markerAssetId == M01ProductionEnemyReadabilityMarkerAssetId ||
                markerAssetId == M01ProductionEnemyHealthBarAssetId)
            {
                RemoveBrightMarkerArtifacts(texture);
            }

            return true;
        }

        if (!TryGetM01ProductionAssetPath(markerAssetId, out string path))
            return false;

        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return texture != null;
#else
        return false;
#endif
    }

    public static bool TryGetM01ProductionAssetPath(string assetId, out string assetPath)
    {
        assetPath = null;
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetId) || !TryLoadAiProductionManifest(out AiProductionAssetManifest manifest))
            return false;

        if (manifest.strategic != null && manifest.strategic.asset_id == assetId)
        {
            assetPath = manifest.strategic.runtime_file;
            return !string.IsNullOrEmpty(assetPath);
        }

        if (TryFindTacticalMapPath(manifest, assetId, out assetPath) ||
            TryFindManifestEntryPath(manifest.markers, assetId, out assetPath) ||
            TryFindManifestEntryPath(manifest.buildings, assetId, out assetPath))
        {
            return true;
        }
#endif
        return false;
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
        if (TryGetM01V32SharedSoldierFrame(runtimeEntityId, visualState, facing, elapsedSeconds, out frame))
            return true;

        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            return false;
        }

        if (TryGetM01V31SharedSoldierFrame(runtimeEntityId, visualState, facing, elapsedSeconds, out frame))
            return true;

        if (TryGetM01V29SharedSoldierFrame(runtimeEntityId, visualState, facing, elapsedSeconds, out frame))
            return true;

        if (TryGetM01V28DirectionLockedSoldierFrame(runtimeEntityId, visualState, elapsedSeconds, out frame))
            return true;

        if (TryGetM01V17BakedSoldierFrame(runtimeEntityId, visualState, facing, elapsedSeconds, out frame))
            return true;

        if (runtimeEntityId != Chapter01M01PlayableRuntime.EnemyPatrolEntityId &&
            visualState == MissionRuntimeSpriteVisualState.Idle &&
            TryGetM01TargetMatchIdleFrame(runtimeEntityId, facing, out frame))
        {
            return true;
        }

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

        Texture2D texture = LoadTexture(faction.runtime_atlas);
        if (texture == null)
            texture = LoadTexture(faction.review_atlas);
        if (texture == null)
            return false;
        Texture2D shadowTexture = TryResolveAcceptedShadowAtlas(manifest, out string shadowAtlasPath)
            ? LoadTexture(shadowAtlasPath)
            : null;

        int frameSlot = ResolveFrameSlot(clip, elapsedSeconds);
        SoldierAnimationFrameV2 sourceFrame = ResolveFrame(clip, frameSlot);
        if (sourceFrame == null || sourceFrame.atlas_rect == null || sourceFrame.atlas_rect.Length < 4)
            return false;

        Rect atlasRect = new(sourceFrame.atlas_rect[0], sourceFrame.atlas_rect[1], sourceFrame.atlas_rect[2], sourceFrame.atlas_rect[3]);
        Vector2 pivot = ResolveVector2(clip.pivot_normalized_unity_sprite, new Vector2(0.5f, 0.054688f));
        Rect normalizedBounds = ResolveRect(sourceFrame.normalized_bounds, new Rect(0f, 0f, 1f, 1f));
        frame = new M01SoldierAnimationFrame(
            texture,
            shadowTexture,
            null,
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
        CachedSoldierManifestV32 = null;
        CachedSoldierManifestV32Json = null;
        CachedSoldierManifestV31 = null;
        CachedSoldierManifestV31Json = null;
        CachedSoldierManifestV29 = null;
        CachedSoldierManifestV29Json = null;
        CachedSoldierManifestV28 = null;
        CachedSoldierManifestV28Json = null;
        CachedSoldierManifestV17 = null;
        CachedSoldierManifestV17Json = null;
        CachedAiProductionManifest = null;
        CachedAiProductionManifestJson = null;
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

    private static bool TryResolveM01ProductionSpritePath(string spriteId, out string assetPath)
    {
        assetPath = null;
        if (!TryResolveM01ProductionAssetId(spriteId, out string assetId))
            return false;

        return TryGetM01ProductionAssetPath(assetId, out assetPath);
    }

    private static bool TryResolveM01ProductionAssetId(string spriteId, out string assetId)
    {
        assetId = null;
        if (string.IsNullOrEmpty(spriteId))
            return false;

        if (spriteId == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId ||
            spriteId == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId + Chapter01M01SpritePresenterCatalog.IdleStateSuffix ||
            spriteId == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId + Chapter01M01SpritePresenterCatalog.MoveStateSuffix ||
            spriteId == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId + Chapter01M01SpritePresenterCatalog.AttackStateSuffix)
        {
            assetId = "building.command_support.intact";
            return true;
        }

        if (spriteId == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId + Chapter01M01SpritePresenterCatalog.DamagedStateSuffix)
        {
            assetId = "building.command_support.damaged";
            return true;
        }

        if (spriteId == Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId + Chapter01M01SpritePresenterCatalog.DeathStateSuffix)
        {
            assetId = "building.command_support.destroyed";
            return true;
        }

        return false;
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

    private static bool TryGetM01V32SharedSoldierFrame(
        string runtimeEntityId,
        MissionRuntimeSpriteVisualState visualState,
        string facing,
        float elapsedSeconds,
        out M01SoldierAnimationFrame frame)
    {
        frame = default;
        if (runtimeEntityId != Chapter01M01PlayableRuntime.PlayerSquadEntityId &&
            runtimeEntityId != Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            return false;
        }

        if (!TryLoadSoldierManifestV32(out Soldier8DirectionManifestV32 manifest) ||
            manifest.frames == null ||
            manifest.frames.Length == 0)
        {
            return false;
        }

        string stateId = ResolveSoldierAnimationStateId(visualState);
        string directionKey = ResolveV32DirectionKey(facing);
        Soldier8DirectionFrameV32[] stateFrames = CollectV32Frames(manifest.frames, stateId, directionKey);
        if (stateFrames.Length == 0 && stateId != "idle")
            stateFrames = CollectV32Frames(manifest.frames, "idle", directionKey);
        if (stateFrames.Length == 0)
            return false;

        float fps = ResolveV28Fps(stateId);
        int frameSlot = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps);
        frameSlot %= stateFrames.Length;

        Soldier8DirectionFrameV32 sourceFrame = stateFrames[Mathf.Clamp(frameSlot, 0, stateFrames.Length - 1)];
        if (sourceFrame.rect == null || sourceFrame.rect.Length < 4)
            return false;

        string bodyAtlasPath = !string.IsNullOrEmpty(sourceFrame.base_atlas)
            ? sourceFrame.base_atlas
            : manifest.atlases != null ? manifest.atlases.body_shadow_pot : null;
        string maskAtlasPath = !string.IsNullOrEmpty(sourceFrame.faction_mask_atlas)
            ? sourceFrame.faction_mask_atlas
            : manifest.atlases != null ? manifest.atlases.faction_mask_pot_optional_technical : null;

        Texture2D bodyTexture = LoadTexture(bodyAtlasPath);
        Texture2D maskTexture = LoadTexture(maskAtlasPath);
        if (bodyTexture == null)
            return false;

        Rect atlasRect = new(sourceFrame.rect[0], sourceFrame.rect[1], sourceFrame.rect[2], sourceFrame.rect[3]);
        float cellSize = Mathf.Max(1f, atlasRect.width);
        Vector2 pivot = ResolvePivotPixels(sourceFrame.pivot, new Vector2(128f, 212f), cellSize);
        frame = new M01SoldierAnimationFrame(
            bodyTexture,
            null,
            maskTexture,
            atlasRect,
            pivot,
            new Rect(0f, 0f, 1f, 1f),
            directionKey,
            $"{sourceFrame.state}_v32_8dir",
            sourceFrame.state_frame,
            fps,
            true);
        return true;
    }

    private static bool TryLoadSoldierManifestV32(out Soldier8DirectionManifestV32 manifest)
    {
        manifest = null;
        string json = ReadTextAssetOrFile(Soldier8DirectionManifestV32Path);
        if (string.IsNullOrEmpty(json))
            return false;

        if (CachedSoldierManifestV32 != null && CachedSoldierManifestV32Json == json)
        {
            manifest = CachedSoldierManifestV32;
            return true;
        }

        CachedSoldierManifestV32 = JsonUtility.FromJson<Soldier8DirectionManifestV32>(json);
        CachedSoldierManifestV32Json = json;
        manifest = CachedSoldierManifestV32;
        return manifest != null && manifest.frames != null && manifest.frames.Length > 0;
    }

    private static Soldier8DirectionFrameV32[] CollectV32Frames(Soldier8DirectionFrameV32[] frames, string state, string directionKey)
    {
        List<Soldier8DirectionFrameV32> matches = new();
        for (int i = 0; i < frames.Length; i++)
        {
            Soldier8DirectionFrameV32 frame = frames[i];
            if (frame != null && frame.state == state && frame.direction_key == directionKey)
                matches.Add(frame);
        }

        matches.Sort((a, b) => a.state_frame.CompareTo(b.state_frame));
        return matches.ToArray();
    }

    private static string ResolveV32DirectionKey(string facing)
    {
        return NormalizeFacing(facing) switch
        {
            "NW" => "up_left",
            "NE" => "up_right",
            "SW" => "down_left",
            "SE" => "down_right",
            _ => "up_right"
        };
    }

    private static bool TryGetM01V31SharedSoldierFrame(
        string runtimeEntityId,
        MissionRuntimeSpriteVisualState visualState,
        string facing,
        float elapsedSeconds,
        out M01SoldierAnimationFrame frame)
    {
        frame = default;
        if (runtimeEntityId != Chapter01M01PlayableRuntime.PlayerSquadEntityId &&
            runtimeEntityId != Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            return false;
        }

        if (!TryLoadSoldierManifestV31(out Soldier8DirectionManifestV31 manifest) ||
            manifest.frames == null ||
            manifest.frames.Length == 0)
        {
            return false;
        }

        string stateId = ResolveSoldierAnimationStateId(visualState);
        string directionKey = ResolveV31DirectionKey(runtimeEntityId, facing);
        Soldier8DirectionFrameV31[] stateFrames = CollectV31Frames(manifest.frames, stateId, directionKey);
        if (stateFrames.Length == 0 && stateId != "idle")
            stateFrames = CollectV31Frames(manifest.frames, "idle", directionKey);
        if (stateFrames.Length == 0)
            return false;

        float fps = ResolveV28Fps(stateId);
        int frameSlot = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps);
        frameSlot %= stateFrames.Length;

        Soldier8DirectionFrameV31 sourceFrame = stateFrames[Mathf.Clamp(frameSlot, 0, stateFrames.Length - 1)];
        if (sourceFrame.rect == null || sourceFrame.rect.Length < 4)
            return false;

        string bodyAtlasPath = !string.IsNullOrEmpty(sourceFrame.base_atlas)
            ? sourceFrame.base_atlas
            : manifest.atlases != null ? manifest.atlases.body_shadow_pot : null;

        string maskAtlasPath = !string.IsNullOrEmpty(sourceFrame.faction_mask_atlas)
            ? sourceFrame.faction_mask_atlas
            : manifest.atlases != null ? manifest.atlases.faction_mask_pot_optional_technical : null;

        Texture2D bodyTexture = LoadTexture(bodyAtlasPath);
        Texture2D maskTexture = LoadTexture(maskAtlasPath);
        if (bodyTexture == null)
            return false;

        Rect atlasRect = new(sourceFrame.rect[0], sourceFrame.rect[1], sourceFrame.rect[2], sourceFrame.rect[3]);
        float cellSize = Mathf.Max(1f, atlasRect.width);
        Vector2 pivot = ResolvePivotPixels(sourceFrame.pivot, new Vector2(128f, 212f), cellSize);
        frame = new M01SoldierAnimationFrame(
            bodyTexture,
            null,
            maskTexture,
            atlasRect,
            pivot,
            new Rect(0f, 0f, 1f, 1f),
            directionKey,
            $"{sourceFrame.state}_v31_8dir",
            sourceFrame.state_frame,
            fps,
            true);
        return true;
    }

    private static bool TryLoadSoldierManifestV31(out Soldier8DirectionManifestV31 manifest)
    {
        manifest = null;
        string json = ReadTextAssetOrFile(Soldier8DirectionManifestV31Path);
        if (string.IsNullOrEmpty(json))
            return false;

        if (CachedSoldierManifestV31 != null && CachedSoldierManifestV31Json == json)
        {
            manifest = CachedSoldierManifestV31;
            return true;
        }

        CachedSoldierManifestV31 = JsonUtility.FromJson<Soldier8DirectionManifestV31>(json);
        CachedSoldierManifestV31Json = json;
        manifest = CachedSoldierManifestV31;
        return manifest != null && manifest.frames != null && manifest.frames.Length > 0;
    }

    private static Soldier8DirectionFrameV31[] CollectV31Frames(Soldier8DirectionFrameV31[] frames, string state, string directionKey)
    {
        List<Soldier8DirectionFrameV31> matches = new();
        for (int i = 0; i < frames.Length; i++)
        {
            Soldier8DirectionFrameV31 frame = frames[i];
            if (frame != null && frame.state == state && frame.direction_key == directionKey)
                matches.Add(frame);
        }

        matches.Sort((a, b) => a.state_frame.CompareTo(b.state_frame));
        return matches.ToArray();
    }

    private static string ResolveV31DirectionKey(string runtimeEntityId, string facing)
    {
        return NormalizeFacing(facing) switch
        {
            "NW" => "up_left",
            "NE" => "up_right",
            "SW" => "down_left",
            "SE" => "down_right",
            _ => "up_right"
        };
    }

    private static bool TryGetM01V29SharedSoldierFrame(
        string runtimeEntityId,
        MissionRuntimeSpriteVisualState visualState,
        string facing,
        float elapsedSeconds,
        out M01SoldierAnimationFrame frame)
    {
        frame = default;
        if (runtimeEntityId != Chapter01M01PlayableRuntime.PlayerSquadEntityId &&
            runtimeEntityId != Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
        {
            return false;
        }

        if (!TryLoadSoldierManifestV29(out SoldierDirectionLockedManifestV29 manifest) ||
            manifest.frames == null ||
            manifest.frames.Length == 0)
        {
            return false;
        }

        string stateId = ResolveSoldierAnimationStateId(visualState);
        string directionKey = ResolveV29DirectionKey(runtimeEntityId, facing);
        SoldierDirectionLockedFrameV29[] stateFrames = CollectV29Frames(manifest.frames, stateId, directionKey);
        if (stateFrames.Length == 0 && stateId != "idle")
            stateFrames = CollectV29Frames(manifest.frames, "idle", directionKey);
        if (stateFrames.Length == 0)
            return false;

        float fps = ResolveV28Fps(stateId);
        int frameSlot = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps);
        frameSlot %= stateFrames.Length;

        SoldierDirectionLockedFrameV29 sourceFrame = stateFrames[Mathf.Clamp(frameSlot, 0, stateFrames.Length - 1)];
        if (sourceFrame.rect == null || sourceFrame.rect.Length < 4)
            return false;

        string bodyAtlasPath = !string.IsNullOrEmpty(sourceFrame.base_atlas)
            ? sourceFrame.base_atlas
            : manifest.atlases != null ? manifest.atlases.body_shadow_pot : null;
        string maskAtlasPath = !string.IsNullOrEmpty(sourceFrame.faction_mask_atlas)
            ? sourceFrame.faction_mask_atlas
            : manifest.atlases != null ? manifest.atlases.faction_mask_pot : null;

        Texture2D bodyTexture = LoadTexture(bodyAtlasPath);
        Texture2D maskTexture = LoadTexture(maskAtlasPath);
        if (bodyTexture == null || maskTexture == null)
            return false;

        Rect atlasRect = new(sourceFrame.rect[0], sourceFrame.rect[1], sourceFrame.rect[2], sourceFrame.rect[3]);
        float cellSize = Mathf.Max(1f, atlasRect.width);
        Vector2 pivot = ResolvePivotPixels(sourceFrame.pivot, new Vector2(128f, 212f), cellSize);
        frame = new M01SoldierAnimationFrame(
            bodyTexture,
            null,
            maskTexture,
            atlasRect,
            pivot,
            new Rect(0f, 0f, 1f, 1f),
            directionKey,
            $"{sourceFrame.state}_v29_shared_mask",
            sourceFrame.state_frame,
            fps,
            true);
        return true;
    }

    private static bool TryLoadSoldierManifestV29(out SoldierDirectionLockedManifestV29 manifest)
    {
        manifest = null;
        string json = ReadTextAssetOrFile(SoldierDirectionLockedManifestV29Path);
        if (string.IsNullOrEmpty(json))
            return false;

        if (CachedSoldierManifestV29 != null && CachedSoldierManifestV29Json == json)
        {
            manifest = CachedSoldierManifestV29;
            return true;
        }

        CachedSoldierManifestV29 = JsonUtility.FromJson<SoldierDirectionLockedManifestV29>(json);
        CachedSoldierManifestV29Json = json;
        manifest = CachedSoldierManifestV29;
        return manifest != null && manifest.frames != null && manifest.frames.Length > 0;
    }

    private static SoldierDirectionLockedFrameV29[] CollectV29Frames(SoldierDirectionLockedFrameV29[] frames, string state, string directionKey)
    {
        List<SoldierDirectionLockedFrameV29> matches = new();
        for (int i = 0; i < frames.Length; i++)
        {
            SoldierDirectionLockedFrameV29 frame = frames[i];
            if (frame != null && frame.state == state && frame.direction_key == directionKey)
                matches.Add(frame);
        }

        matches.Sort((a, b) => a.state_frame.CompareTo(b.state_frame));
        return matches.ToArray();
    }

    private static string ResolveV29DirectionKey(string runtimeEntityId, string facing)
    {
        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId)
            return "screen_locked_D";
        if (runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
            return "screen_locked_B";

        return NormalizeFacing(facing) switch
        {
            "NW" => "screen_locked_A",
            "NE" => "screen_locked_B",
            "SW" => "screen_locked_C",
            "SE" => "screen_locked_D",
            _ => "screen_locked_A"
        };
    }

    private static bool TryGetM01TargetMatchIdleFrame(string runtimeEntityId, string facing, out M01SoldierAnimationFrame frame)
    {
        frame = default;
        string atlasPath;
        if (runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId)
            atlasPath = M01TargetMatchPlayerIdleAtlasV5Path;
        else if (runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
            atlasPath = M01TargetMatchEnemyIdleAtlasV5Path;
        else
            return false;

        Texture2D texture = LoadTexture(atlasPath);
        if (texture == null)
            return false;

        Texture2D shadowTexture = LoadTexture(M01TargetMatchShadowAtlasV5Path);
        string facingId = NormalizeFacing(facing);
        int facingSlot = ResolveTargetMatchFacingSlot(facingId);
        Rect atlasRect = new(facingSlot * 256f, 0f, 256f, 256f);
        frame = new M01SoldierAnimationFrame(
            texture,
            shadowTexture,
            null,
            atlasRect,
            new Vector2(0.5f, 0.1796875f),
            new Rect(0f, 0f, 1f, 1f),
            facingId,
            "idle_targetmatch_v5",
            0,
            1f,
            false);
        return true;
    }

    private static bool TryGetM01V28DirectionLockedSoldierFrame(
        string runtimeEntityId,
        MissionRuntimeSpriteVisualState visualState,
        float elapsedSeconds,
        out M01SoldierAnimationFrame frame)
    {
        frame = default;
        if (!TryLoadSoldierManifestV28(out SoldierDirectionLockedManifestV28 manifest))
            return false;

        SoldierDirectionLockedUnitV28 unit = runtimeEntityId == Chapter01M01PlayableRuntime.PlayerSquadEntityId
            ? manifest.player
            : runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId
                ? manifest.enemy
                : null;
        if (unit == null || unit.frames == null || unit.frames.Length == 0)
            return false;

        string stateId = ResolveSoldierAnimationStateId(visualState);
        string directionKey = "screen_locked_A";
        SoldierDirectionLockedFrameV28[] stateFrames = CollectV28Frames(unit.frames, stateId, directionKey);
        if (stateFrames.Length == 0 && stateId != "idle")
            stateFrames = CollectV28Frames(unit.frames, "idle", directionKey);
        if (stateFrames.Length == 0)
            return false;

        Texture2D texture = LoadTexture(unit.review_atlas);
        if (texture == null)
            return false;

        float fps = ResolveV28Fps(stateId);
        int frameSlot = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * fps);
        if (stateFrames.Length > 0)
            frameSlot %= stateFrames.Length;

        SoldierDirectionLockedFrameV28 sourceFrame = stateFrames[Mathf.Clamp(frameSlot, 0, stateFrames.Length - 1)];
        if (sourceFrame.rect == null || sourceFrame.rect.Length < 4)
            return false;

        Rect atlasRect = new(sourceFrame.rect[0], sourceFrame.rect[1], sourceFrame.rect[2], sourceFrame.rect[3]);
        Vector2 pivot = ResolvePivotPixels(sourceFrame.pivot, new Vector2(128f, 212f), 256f);
        frame = new M01SoldierAnimationFrame(
            texture,
            null,
            null,
            atlasRect,
            pivot,
            new Rect(0f, 0f, 1f, 1f),
            directionKey,
            $"{sourceFrame.state}_v28_direction_locked",
            sourceFrame.state_frame,
            fps,
            true);
        return true;
    }

    private static bool TryLoadSoldierManifestV28(out SoldierDirectionLockedManifestV28 manifest)
    {
        manifest = null;
        string json = ReadTextAssetOrFile(SoldierDirectionLockedManifestV28Path);
        if (string.IsNullOrEmpty(json))
            return false;

        if (CachedSoldierManifestV28 != null && CachedSoldierManifestV28Json == json)
        {
            manifest = CachedSoldierManifestV28;
            return true;
        }

        CachedSoldierManifestV28 = JsonUtility.FromJson<SoldierDirectionLockedManifestV28>(json);
        CachedSoldierManifestV28Json = json;
        manifest = CachedSoldierManifestV28;
        return manifest != null && manifest.player != null && manifest.enemy != null;
    }

    private static SoldierDirectionLockedFrameV28[] CollectV28Frames(SoldierDirectionLockedFrameV28[] frames, string state, string directionKey)
    {
        List<SoldierDirectionLockedFrameV28> matches = new();
        for (int i = 0; i < frames.Length; i++)
        {
            SoldierDirectionLockedFrameV28 frame = frames[i];
            if (frame != null && frame.state == state && frame.direction_key == directionKey)
                matches.Add(frame);
        }

        matches.Sort((a, b) => a.state_frame.CompareTo(b.state_frame));
        return matches.ToArray();
    }

    private static float ResolveV28Fps(string state)
    {
        return state == "run" ? 12f : 6f;
    }

    private static Vector2 ResolvePivotPixels(float[] pivot, Vector2 fallback, float cellSize)
    {
        Vector2 pixelPivot = ResolveVector2(pivot, fallback);
        return new Vector2(pixelPivot.x / cellSize, 1f - (pixelPivot.y / cellSize));
    }

    private static bool TryGetM01V17BakedSoldierFrame(
        string runtimeEntityId,
        MissionRuntimeSpriteVisualState visualState,
        string facing,
        float elapsedSeconds,
        out M01SoldierAnimationFrame frame)
    {
        frame = default;
        if (!TryGetM01SoldierFaction(runtimeEntityId, out string factionId) ||
            !TryLoadSoldierManifestV17(out SoldierBakedShadowManifestV17 manifest) ||
            manifest.animation_assets == null)
        {
            return false;
        }

        SoldierAnimationFactionV2 faction = manifest.animation_assets.Resolve(factionId);
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

        Texture2D texture = LoadTexture(faction.review_atlas);
        if (texture == null)
            texture = LoadTexture(faction.runtime_atlas);
        if (texture == null)
            return false;

        int frameSlot = ResolveFrameSlot(clip, elapsedSeconds);
        SoldierAnimationFrameV2 sourceFrame = ResolveFrame(clip, frameSlot);
        if (sourceFrame == null || sourceFrame.atlas_rect == null || sourceFrame.atlas_rect.Length < 4)
            return false;

        Rect atlasRect = new(sourceFrame.atlas_rect[0], sourceFrame.atlas_rect[1], sourceFrame.atlas_rect[2], sourceFrame.atlas_rect[3]);
        Vector2 pivot = ResolveVector2(clip.pivot_normalized_unity_sprite, new Vector2(0.5f, 0.1796875f));
        frame = new M01SoldierAnimationFrame(
            texture,
            null,
            null,
            atlasRect,
            pivot,
            ResolveRect(sourceFrame.normalized_bounds, new Rect(0f, 0f, 1f, 1f)),
            clip.facing,
            $"{clip.state}_v17_baked_shadow",
            sourceFrame.frame_index,
            Mathf.Max(1f, clip.suggested_fps),
            clip.loop);
        return true;
    }

    private static bool TryLoadSoldierManifestV17(out SoldierBakedShadowManifestV17 manifest)
    {
        manifest = null;
        string json = ReadTextAssetOrFile(SoldierBakedShadowManifestV17Path);
        if (string.IsNullOrEmpty(json))
            return false;

        if (CachedSoldierManifestV17 != null && CachedSoldierManifestV17Json == json)
        {
            manifest = CachedSoldierManifestV17;
            return true;
        }

        CachedSoldierManifestV17 = JsonUtility.FromJson<SoldierBakedShadowManifestV17>(json);
        CachedSoldierManifestV17Json = json;
        manifest = CachedSoldierManifestV17;
        return manifest != null && manifest.animation_assets != null;
    }

    private static bool TryLoadSoldierManifestV2(out SoldierAnimationManifestV2 manifest)
    {
        manifest = null;
        string json = ReadTextAssetOrFile(SoldierAnimationManifestV5Path);
        if (string.IsNullOrEmpty(json))
            json = ReadTextAssetOrFile(SoldierAnimationManifestV2Path);
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

    private static string ResolveM01V29RuntimeTacticalGroundPath()
    {
        float aspect = ResolveRuntimeAspect();
        if (aspect >= 2.28f && File.Exists(ProjectFilePath(M01AcceptedTacticalGroundV29Runtime21x9Path)))
            return M01AcceptedTacticalGroundV29Runtime21x9Path;
        if (aspect >= 2.05f && File.Exists(ProjectFilePath(M01AcceptedTacticalGroundV29Runtime20x9Path)))
            return M01AcceptedTacticalGroundV29Runtime20x9Path;
        if (File.Exists(ProjectFilePath(M01AcceptedTacticalGroundV29Runtime16x9Path)))
            return M01AcceptedTacticalGroundV29Runtime16x9Path;

        return M01AcceptedTacticalGroundV29Path;
    }

    private static float ResolveRuntimeAspect()
    {
        if (Screen.width > 0 && Screen.height > 0)
            return Screen.width / Mathf.Max(1f, Screen.height);
        if (Camera.main != null && Camera.main.aspect > 0.0001f)
            return Camera.main.aspect;
        return 16f / 9f;
    }

    private static bool TryLoadAiProductionManifest(out AiProductionAssetManifest manifest)
    {
        manifest = null;
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(M01AiProductionManifestPath);
        string json = asset != null ? asset.text : File.Exists(M01AiProductionManifestPath) ? File.ReadAllText(M01AiProductionManifestPath) : null;
        if (string.IsNullOrEmpty(json))
            return false;

        if (CachedAiProductionManifest != null && CachedAiProductionManifestJson == json)
        {
            manifest = CachedAiProductionManifest;
            return true;
        }

        CachedAiProductionManifest = JsonUtility.FromJson<AiProductionAssetManifest>(json);
        CachedAiProductionManifestJson = json;
        manifest = CachedAiProductionManifest;
        return manifest != null;
    }

    private static bool TryFindTacticalMapPath(AiProductionAssetManifest manifest, string assetId, out string assetPath)
    {
        assetPath = null;
        if (manifest.tactical_maps == null)
            return false;

        for (int i = 0; i < manifest.tactical_maps.Length; i++)
        {
            AiProductionTacticalMapEntry entry = manifest.tactical_maps[i];
            if (entry == null || entry.asset_id != assetId)
                continue;

            assetPath = ResolveTacticalMapRuntimePath(entry);
            return !string.IsNullOrEmpty(assetPath);
        }

        return false;
    }

    private static string ResolveTacticalMapRuntimePath(AiProductionTacticalMapEntry entry)
    {
        if (entry == null)
            return null;

        return !string.IsNullOrEmpty(entry.runtime_source)
            ? entry.runtime_source
            : entry.runtime_pot;
    }

    private static bool TryFindManifestEntryPath(AiProductionRuntimeAssetEntry[] entries, string assetId, out string assetPath)
    {
        assetPath = null;
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            AiProductionRuntimeAssetEntry entry = entries[i];
            if (entry == null || entry.asset_id != assetId)
                continue;

            assetPath = entry.runtime_file;
            return !string.IsNullOrEmpty(assetPath);
        }

        return false;
    }

    private static bool TryGetAcceptedMarkerPath(string markerAssetId, out string assetPath)
    {
        assetPath = markerAssetId switch
        {
            M01ProductionSelectionMarkerAssetId => M01AcceptedSelectionMarkerV5Path,
            M01ProductionEnemyReadabilityMarkerAssetId => M01AcceptedEnemyReadabilityMarkerV5Path,
            M01ProductionEnemyHealthBarAssetId => M01AcceptedEnemyHealthBarV5Path,
            _ => null
        };

        return !string.IsNullOrEmpty(assetPath) && File.Exists(ProjectFilePath(assetPath));
    }

    private static bool TryResolveAcceptedShadowAtlas(SoldierAnimationManifestV2 manifest, out string assetPath)
    {
        assetPath = null;
        if (manifest != null &&
            manifest.shadows != null &&
            !string.IsNullOrEmpty(manifest.shadows.review_atlas) &&
            File.Exists(ProjectFilePath(manifest.shadows.review_atlas)))
        {
            assetPath = manifest.shadows.review_atlas;
            return true;
        }

        if (File.Exists(ProjectFilePath(M01AcceptedShadowAtlasV5Path)))
        {
            assetPath = M01AcceptedShadowAtlasV5Path;
            return true;
        }

        return false;
    }

    private static Texture2D LoadTexture(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture != null)
            return texture;

        return TryLoadTextureFromPng(assetPath, out texture) ? texture : null;
    }

    private static bool TryLoadTextureFromPng(string assetPath, out Texture2D texture)
    {
        texture = null;
        if (string.IsNullOrEmpty(assetPath) || Path.GetExtension(assetPath).ToLowerInvariant() != ".png")
            return false;

        string fullPath = ProjectFilePath(assetPath);
        if (!File.Exists(fullPath))
            return false;

        byte[] bytes = File.ReadAllBytes(fullPath);
        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            name = Path.GetFileNameWithoutExtension(assetPath),
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        if (ImageConversion.LoadImage(texture, bytes))
            return true;

        Object.DestroyImmediate(texture);
        texture = null;
        return false;
    }

    private static void RemoveBrightMarkerArtifacts(Texture2D texture)
    {
        if (texture == null)
            return;

        Color32[] pixels = texture.GetPixels32();
        bool changed = false;
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.a == 0)
                continue;

            if (pixel.r >= 205 && pixel.g >= 205 && pixel.b >= 205)
            {
                pixels[i] = new Color32(pixel.r, pixel.g, pixel.b, 0);
                changed = true;
            }
        }

        if (!changed)
            return;

        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
    }

    private static string ReadTextAssetOrFile(string assetPath)
    {
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (asset != null)
            return asset.text;

        string fullPath = ProjectFilePath(assetPath);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
    }

    private static string ProjectFilePath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
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

    private static int ResolveTargetMatchFacingSlot(string facing)
    {
        return facing switch
        {
            "NE" => 0,
            "SE" => 1,
            "SW" => 2,
            "NW" => 3,
            _ => 1
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

        string fullPath = ProjectFilePath(assetPath);
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
        public SoldierShadowManifestV2 shadows;
    }

    [System.Serializable]
    private sealed class SoldierBakedShadowManifestV17
    {
        public SoldierAnimationFactionsV2 animation_assets;
    }

    [System.Serializable]
    private sealed class Soldier8DirectionManifestV32
    {
        public Soldier8DirectionAtlasesV32 atlases;
        public Soldier8DirectionFrameV32[] frames;
    }

    [System.Serializable]
    private sealed class Soldier8DirectionAtlasesV32
    {
        public string body_shadow_pot;
        public string clean_body_pot;
        public string faction_mask_pot_optional_technical;
        public string idle_body_shadow;
        public string idle_faction_mask_optional_technical;
    }

    [System.Serializable]
    private sealed class Soldier8DirectionFrameV32
    {
        public int index;
        public string direction_key;
        public string screen_space_read;
        public string state;
        public int state_frame;
        public float[] rect;
        public float[] pivot;
        public string base_atlas;
        public string faction_mask_atlas;
    }

    [System.Serializable]
    private sealed class Soldier8DirectionManifestV31
    {
        public Soldier8DirectionAtlasesV31 atlases;
        public Soldier8DirectionFrameV31[] frames;
    }

    [System.Serializable]
    private sealed class Soldier8DirectionAtlasesV31
    {
        public string body_shadow_pot;
        public string clean_body_pot;
        public string faction_mask_pot_optional_technical;
        public string idle_body_shadow;
        public string idle_faction_mask_optional_technical;
    }

    [System.Serializable]
    private sealed class Soldier8DirectionFrameV31
    {
        public int index;
        public string direction_key;
        public string screen_space_read;
        public string state;
        public int state_frame;
        public float[] rect;
        public float[] pivot;
        public string base_atlas;
        public string faction_mask_atlas;
    }

    [System.Serializable]
    private sealed class SoldierDirectionLockedManifestV29
    {
        public SoldierDirectionLockedAtlasesV29 atlases;
        public SoldierDirectionLockedFrameV29[] frames;
    }

    [System.Serializable]
    private sealed class SoldierDirectionLockedAtlasesV29
    {
        public string body_shadow_pot;
        public string faction_mask_pot;
    }

    [System.Serializable]
    private sealed class SoldierDirectionLockedFrameV29
    {
        public int index;
        public string direction_key;
        public string screen_space_read;
        public string state;
        public int state_frame;
        public float[] rect;
        public float[] pivot;
        public string base_atlas;
        public string faction_mask_atlas;
    }

    [System.Serializable]
    private sealed class SoldierDirectionLockedManifestV28
    {
        public SoldierDirectionLockedUnitV28 player;
        public SoldierDirectionLockedUnitV28 enemy;
    }

    [System.Serializable]
    private sealed class SoldierDirectionLockedUnitV28
    {
        public string screen_space_read;
        public string review_atlas;
        public string idle_direction_locked_atlas;
        public SoldierDirectionLockedFrameV28[] frames;
    }

    [System.Serializable]
    private sealed class SoldierDirectionLockedFrameV28
    {
        public int index;
        public string direction_key;
        public string screen_space_read;
        public string state;
        public int state_frame;
        public int source_pose_index;
        public float[] rect;
        public float[] pivot;
        public float[] body_bbox;
        public float[] fit_size;
    }

    [System.Serializable]
    private sealed class AiProductionAssetManifest
    {
        public AiProductionRuntimeAssetEntry strategic;
        public AiProductionTacticalMapEntry[] tactical_maps;
        public AiProductionRuntimeAssetEntry[] markers;
        public AiProductionRuntimeAssetEntry[] buildings;
    }

    [System.Serializable]
    private sealed class AiProductionRuntimeAssetEntry
    {
        public string asset_id;
        public string runtime_file;
    }

    [System.Serializable]
    private sealed class AiProductionTacticalMapEntry
    {
        public string asset_id;
        public string runtime_source;
        public string runtime_pot;
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
        public string review_atlas;
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

    [System.Serializable]
    private sealed class SoldierShadowManifestV2
    {
        public string review_atlas;
    }
#endif
}
