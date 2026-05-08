#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureM01SpriteRendererCaptureBuilder
{
    private const string DefinitionPath = "Assets/Game/Data/TacticalMaps/Chapter01/iso.ch01.district_edge_01.asset";
    private const string CapturePath = "Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_SpriteRendererCapture.unity";
    private const string EntitySpriteRoot = "Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites";
    private const float SpriteDepthOffset = -0.04f;

    [MenuItem("WarlineCapture/Design/Capture Chapter01 M01 Sprite Renderer")]
    public static void BuildAndCapture()
    {
        WarlineCaptureM01TacticalValidationBuilder.Build();
        ForceReimportRuntimeSprites();
        AssetDatabase.Refresh();

        TacticalMapDefinition definition = AssetDatabase.LoadAssetAtPath<TacticalMapDefinition>(DefinitionPath);
        if (definition == null || definition.GroundSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURE_MISSING_DEFINITION path={DefinitionPath}");
            return;
        }

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(CapturePath)));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = BuildScene(definition);
        CaptureCamera(camera, CapturePath);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURED path={CapturePath} scene={ScenePath}");
    }

    private static void ForceReimportRuntimeSprites()
    {
        AssetDatabase.ImportAsset(EntitySpriteRoot + "/infantry_squad.png", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(EntitySpriteRoot + "/command_building.png", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
    }

    private static Camera BuildScene(TacticalMapDefinition definition)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("Chapter01_M01_SpriteRendererCapture");
        Camera camera = CreateCamera(definition);
        CreateGround(definition, root.transform);

        CreatePresenterSprite(definition, Chapter01M01PlayableRuntime.PlayerSquadEntityId, Chapter01M01PlayableRuntime.PlayerSpawnAnchorId, root.transform);
        CreatePresenterSprite(definition, Chapter01M01PlayableRuntime.EnemyPatrolEntityId, Chapter01M01PlayableRuntime.EnemySpawnAnchorId, root.transform);
        CreatePresenterSprite(definition, Chapter01M01PlayableRuntime.DecorCommandPointEntityId, Chapter01M01PlayableRuntime.DecorCommandPointEntityId, root.transform);
        return camera;
    }

    private static void CreateGround(TacticalMapDefinition definition, Transform parent)
    {
        GameObject ground = new("Ground");
        ground.transform.SetParent(parent, false);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localRotation = Quaternion.identity;
        ground.transform.localScale = Vector3.one;

        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = definition.GroundSprite;
        renderer.sortingOrder = 0;
    }

    private static void CreatePresenterSprite(TacticalMapDefinition definition, string runtimeEntityId, string anchorId, Transform parent)
    {
        if (!Chapter01M01SpritePresenterCatalog.TryCreatePresenter(runtimeEntityId, out MissionRuntimeSpritePresenter presenter) ||
            !definition.TryGetAnchor(anchorId, out TacticalMapAnchor anchor) ||
            !MissionRuntimeAtlasQuadPresentationSystem.TryResolveSprite(presenter, out Sprite sprite))
        {
            Debug.LogError($"WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURE_ENTITY_FAILED id={runtimeEntityId} anchor={anchorId}");
            return;
        }

        Vector2 world = definition.NormalizedToWorld(anchor.NormalizedPosition);
        GameObject obj = new($"Capture_{runtimeEntityId}");
        obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(world.x, world.y, SpriteDepthOffset);
        obj.transform.rotation = Quaternion.identity;
        float scale = Chapter01M01SpriteAssetResolver.TryGetScale(presenter.ManifestAssetId.ToString(), out float resolvedScale)
            ? resolvedScale
            : 1f;
        obj.transform.localScale = Vector3.one;

        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        Texture2D texture = sprite.texture;
        meshFilter.sharedMesh = CreateQuadMesh(texture.width / sprite.pixelsPerUnit * scale, texture.height / sprite.pixelsPerUnit * scale);
        meshRenderer.sharedMaterial = CreateTransparentMaterial(texture, runtimeEntityId == Chapter01M01PlayableRuntime.EnemyPatrolEntityId
            ? new Color(1f, 0.58f, 0.48f, 1f)
            : Color.white);
        meshRenderer.sortingOrder = runtimeEntityId == Chapter01M01PlayableRuntime.DecorCommandPointEntityId ? 22 : 24;
        Debug.Log($"WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURE_QUAD id={runtimeEntityId} texture={texture.name} size={texture.width}x{texture.height} scale={scale:F3}");
    }

    private static Mesh CreateQuadMesh(float width, float height)
    {
        Mesh mesh = new()
        {
            vertices = new[]
            {
                new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, -height * 0.5f, 0f),
                new Vector3(-width * 0.5f, height * 0.5f, 0f),
                new Vector3(width * 0.5f, height * 0.5f, 0f)
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            },
            triangles = new[] { 0, 2, 1, 2, 3, 1 }
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateTransparentMaterial(Texture2D texture, Color tint)
    {
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        Material material = new(shader);
        material.mainTexture = texture;
        material.color = tint;
        material.SetFloat("_ZWrite", 0f);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }

    private static Camera CreateCamera(TacticalMapDefinition definition)
    {
        GameObject obj = new("M01_SpriteRenderer_CloseCaptureCamera");
        Camera camera = obj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.039f, 0.040f, 1f);
        camera.orthographic = true;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        ResolveCaptureFrame(definition, out Vector2 center, out float orthographicSize);
        camera.orthographicSize = orthographicSize;
        camera.transform.position = new Vector3(center.x, center.y, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.enabled = true;
        return camera;
    }

    private static void ResolveCaptureFrame(TacticalMapDefinition definition, out Vector2 center, out float orthographicSize)
    {
        if (definition.TryGetAnchor(Chapter01M01PlayableRuntime.PlayerSpawnAnchorId, out TacticalMapAnchor player) &&
            definition.TryGetAnchor(Chapter01M01PlayableRuntime.EnemySpawnAnchorId, out TacticalMapAnchor enemy) &&
            definition.TryGetAnchor(Chapter01M01PlayableRuntime.DecorCommandPointEntityId, out TacticalMapAnchor command))
        {
            Vector2 playerWorld = definition.NormalizedToWorld(player.NormalizedPosition);
            Vector2 enemyWorld = definition.NormalizedToWorld(enemy.NormalizedPosition);
            Vector2 commandWorld = definition.NormalizedToWorld(command.NormalizedPosition);
            float infantryScale = Chapter01M01SpriteAssetResolver.TryGetScale(Chapter01M01PlayableRuntime.PlayerSquadEntityId, out float resolvedInfantryScale)
                ? resolvedInfantryScale
                : 0.07f;
            float commandScale = Chapter01M01SpriteAssetResolver.TryGetScale(Chapter01M01PlayableRuntime.DecorCommandPointEntityId, out float resolvedCommandScale)
                ? resolvedCommandScale
                : 0.14f;
            Vector2 infantryHalfExtents = new(299f / 100f * infantryScale * 0.5f, 255f / 100f * infantryScale * 0.5f);
            Vector2 commandHalfExtents = new(401f / 100f * commandScale * 0.5f, 376f / 100f * commandScale * 0.5f);
            Vector2 min = Vector2.Min(Vector2.Min(playerWorld - infantryHalfExtents, enemyWorld - infantryHalfExtents), commandWorld - commandHalfExtents);
            Vector2 max = Vector2.Max(Vector2.Max(playerWorld + infantryHalfExtents, enemyWorld + infantryHalfExtents), commandWorld + commandHalfExtents);
            const float padding = 0.14f;
            min -= Vector2.one * padding;
            max += Vector2.one * padding;
            center = (min + max) * 0.5f;
            Vector2 size = max - min;
            orthographicSize = Mathf.Max(size.y * 0.5f, size.x / (16f / 9f) * 0.5f, 0.72f);
            return;
        }

        center = definition.CameraDefaultCenter;
        orthographicSize = 0.82f;
    }

    private static void CaptureCamera(Camera camera, string assetPath)
    {
        if (camera == null)
        {
            Debug.LogError("WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURE_NO_CAMERA");
            return;
        }

        RenderTexture rt = new(1920, 1080, 24, RenderTextureFormat.ARGB32);
        Texture2D png = new(1920, 1080, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        try
        {
            camera.targetTexture = rt;
            RenderTexture.active = rt;
            camera.Render();
            png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            png.Apply();
            File.WriteAllBytes(ProjectPath(assetPath), png.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(png);
        }
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    }
}
#endif
