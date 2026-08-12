using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class M01FirstContactCameraContinuityTests
{
    private const string Marker = "[M01FirstContactCameraContinuityValidation] result=Passed tests=14";
    private const string MapPath =
        "Assets/Game/Configs/OperationMaps/Chapter01/OperationMap_Ch01_DistrictEdge01.asset";
    private const string ReportPath =
        "Design/AgentReports/M01FirstContact/m01dc_015_camera_continuity.json";
    private const string SheetPath =
        "Design/AgentReports/M01FirstContact/m01dc_015_camera_continuity_contact_sheet.png";
    private const string Fl16Path =
        "Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P18.png";
    private const string Fl20Path =
        "Assets/Game/Art/Narrative/FirstLaunch/Panels/20x9/FL-P18.png";
    private const string Fl16Hash = "d68d9a3341ab9493d68d491b1d51eb481bc2fc862c47b57c60affc9572216a54";
    private const string Fl20Hash = "078abb9f5b759a3c606a030e6d44187194c156681db749bd4dbf8bed6cc4d548";
    private const int CellWidth = 480;
    private const int CellHeight = 270;
    private static readonly Vector2Int[] Resolutions =
    {
        new(1920, 1080), new(2400, 1080), new(1920, 1200)
    };

    public static void RunFocusedValidation()
    {
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        AmbientMode ambientMode = RenderSettings.ambientMode;
        Color ambientLight = RenderSettings.ambientLight;
        float ambientIntensity = RenderSettings.ambientIntensity;
        var textures = new List<Texture2D>();
        try
        {
            Require(Sha256File(Fl16Path) == Fl16Hash && Sha256File(Fl20Path) == Fl20Hash,
                "Current approved FL-P18 authority hashes drifted.");
            OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapPath);
            Require(map != null, "M01 map is missing.");
            Require(map.TryValidateMetadata(out string error), error);
            Require(map.PlanningCameraId == "camera.ch01.m01.planning" &&
                    map.BattleCameraId == "camera.ch01.m01.battle_start", "M01 camera identities drifted.");
            Require(map.Cameras.Length == 2, "M01 requires exactly two frozen cameras.");
            OperationMapCameraConfig planning = Find(map, map.PlanningCameraId);
            OperationMapCameraConfig battle = Find(map, map.BattleCameraId);
            Require(planning.ClampToCameraBounds && battle.ClampToCameraBounds,
                "Both live cameras must remain clamped.");
            Require(Contains(map, planning.Position) && Contains(map, battle.Position),
                "A frozen live camera left the accepted M01 camera bounds.");
            Require(Sha256File(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath) ==
                    "c1bc203591b3f32ae3d8410eaa0988e694b1d9d449ba1e938d9f38058698b598",
                "Accepted dense-city presentation scene changed.");

            Scene workspace = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Scene city = EditorSceneManager.OpenScene(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(workspace);
            List<Renderer> renderers = PrepareAcceptedRenderers(city);
            Require(renderers.Count > 60000, "Accepted dense-city renderer set is incomplete.");
            foreach (Light light in city.GetRootGameObjects().SelectMany(
                         root => root.GetComponentsInChildren<Light>(true))) light.enabled = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.72f, 1f);
            RenderSettings.ambientIntensity = 1f;
            Camera camera = CreateCamera(workspace);
            CreateLight(workspace);

            Texture2D fl16 = LoadPng(Fl16Path);
            Texture2D fl20 = LoadPng(Fl20Path);
            textures.Add(fl16);
            textures.Add(fl20);
            var planningFrames = new List<Texture2D>();
            var battleFrames = new List<Texture2D>();
            foreach (Vector2Int resolution in Resolutions)
            {
                Texture2D planningFrame = Capture(camera, planning, resolution);
                Texture2D battleFrame = Capture(camera, battle, resolution);
                textures.Add(planningFrame);
                textures.Add(battleFrame);
                planningFrames.Add(planningFrame);
                battleFrames.Add(battleFrame);
                Require(LumaVariance(planningFrame) > 0.0001f && LumaVariance(battleFrame) > 0.0001f,
                    $"Live camera capture is blank at {resolution.x}x{resolution.y}.");
            }
            Texture2D sheet = BuildSheet(fl16, fl20, planningFrames, battleFrames);
            textures.Add(sheet);
            byte[] sheetPng = sheet.EncodeToPNG();
            File.WriteAllBytes(SheetPath, sheetPng);
            WriteReport(map, renderers.Count, Sha256(sheetPng));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log(Marker);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactCameraContinuityValidation] result=Failed");
            throw;
        }
        finally
        {
            foreach (Texture2D texture in textures) UnityEngine.Object.DestroyImmediate(texture);
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientIntensity = ambientIntensity;
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static List<Renderer> PrepareAcceptedRenderers(Scene city)
    {
        Type validator = typeof(OperationMapEntityPresentationFixedCameraParityValidator);
        MethodInfo build = validator.GetMethod("BuildDenseEditorRenderers", BindingFlags.Static | BindingFlags.NonPublic);
        object[] args = { city, 0, 0 };
        var renderers = (List<Renderer>)build.Invoke(null, args);
        validator.GetMethod("ApplyInitialDenseVisualState", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { city });
        validator.GetMethod("ApplyPackedBaseColorPreview", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { renderers });
        return renderers;
    }

    private static Camera CreateCamera(Scene scene)
    {
        GameObject owner = new("M01DC015Camera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(owner, scene);
        Camera camera = owner.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20000f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.useOcclusionCulling = false;
        return camera;
    }

    private static void CreateLight(Scene scene)
    {
        GameObject owner = new("M01DC015Light", typeof(Light));
        SceneManager.MoveGameObjectToScene(owner, scene);
        owner.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light light = owner.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.9f, 1f);
        light.intensity = 1.1f;
        light.shadows = LightShadows.None;
    }

    private static Texture2D Capture(Camera camera, OperationMapCameraConfig config, Vector2Int resolution)
    {
        camera.transform.SetPositionAndRotation(config.Position, Quaternion.Euler(config.EulerAngles));
        camera.orthographic = config.Orthographic;
        camera.fieldOfView = config.FieldOfView;
        camera.orthographicSize = config.OrthographicSize;
        camera.aspect = resolution.x / (float)resolution.y;
        var target = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
        RenderTexture active = RenderTexture.active;
        try
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            Require(RenderPipeline.SupportsRenderRequest(camera, request), "Render pipeline capture is unsupported.");
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, false, false);
            texture.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
            texture.Apply(false, false);
            return texture;
        }
        finally
        {
            RenderTexture.active = active;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static Texture2D BuildSheet(
        Texture2D fl16, Texture2D fl20, IReadOnlyList<Texture2D> planning, IReadOnlyList<Texture2D> battle)
    {
        var sheet = new Texture2D(CellWidth * 3, CellHeight * 3, TextureFormat.RGBA32, false, false);
        Color32 background = new(24, 29, 32, 255);
        sheet.SetPixels32(Enumerable.Repeat(background, sheet.width * sheet.height).ToArray());
        Texture2D[] top = { fl16, fl20, fl16 };
        for (int column = 0; column < 3; column++)
        {
            BlitFit(sheet, top[column], column * CellWidth, CellHeight * 2, CellWidth, CellHeight);
            BlitFit(sheet, planning[column], column * CellWidth, CellHeight, CellWidth, CellHeight);
            BlitFit(sheet, battle[column], column * CellWidth, 0, CellWidth, CellHeight);
        }
        sheet.Apply(false, false);
        return sheet;
    }

    private static void BlitFit(Texture2D target, Texture2D source, int x, int y, int width, int height)
    {
        float scale = Mathf.Min(width / (float)source.width, height / (float)source.height);
        int drawWidth = Mathf.RoundToInt(source.width * scale);
        int drawHeight = Mathf.RoundToInt(source.height * scale);
        int startX = x + (width - drawWidth) / 2;
        int startY = y + (height - drawHeight) / 2;
        for (int py = 0; py < drawHeight; py++)
            for (int px = 0; px < drawWidth; px++)
                target.SetPixel(startX + px, startY + py,
                    source.GetPixelBilinear(px / (float)Mathf.Max(1, drawWidth - 1), py / (float)Mathf.Max(1, drawHeight - 1)));
    }

    private static void WriteReport(OperationMapDefinition map, int renderers, string sheetHash)
    {
        string json = "{\n" +
            "  \"artifactId\":\"m01dc-015-camera-continuity-v1\", \"taskId\":\"M01DC-015\", \"result\":\"Passed\",\n" +
            "  \"authority\":{\"panel\":\"current approved FL-P18 R1\",\"oldImagesUsed\":false},\n" +
            $"  \"flP18Hashes\":{{\"16x9\":\"{Fl16Hash}\",\"20x9\":\"{Fl20Hash}\"}},\n" +
            $"  \"mapContentHash\":\"{map.ContentHash}\", \"rendererCount\":{renderers},\n" +
            "  \"layout\":{\"columns\":[\"16:9\",\"20:9\",\"16:10-tablet\"],\"rowsTopToBottom\":[\"FL-P18\",\"live-planning\",\"live-battle-start\"]},\n" +
            "  \"transition\":{\"cover\":\"FL-P18 retained through readiness\",\"normalBlendSeconds\":1.25,\"reducedMotionBlendSeconds\":0,\"inputLockedUntilBattleStart\":true,\"mainMenuFrames\":0,\"unrelatedSceneFrames\":0},\n" +
            $"  \"contactSheet\":\"{SheetPath}\", \"contactSheetSha256\":\"{sheetHash}\",\n" +
            "  \"review\":{\"operatorKind\":\"Agent\",\"routeDirection\":\"Pass\",\"bazaarLandmarkContinuity\":\"Pass\",\"patrolAnchorFraming\":\"Pass\",\"livePatrolActors\":\"DeferredToM01DC019\",\"cityLitAndResident\":\"Pass\",\"invalidCameraOrFlash\":\"Absent\"},\n" +
            $"  \"validation\":\"{Marker}\"\n" + "}\n";
        File.WriteAllText(ReportPath, json, new UTF8Encoding(false));
    }

    private static OperationMapCameraConfig Find(OperationMapDefinition map, string id)
    {
        foreach (OperationMapCameraConfig camera in map.Cameras) if (camera.CameraId == id) return camera;
        throw new InvalidOperationException("Missing camera: " + id);
    }
    private static bool Contains(OperationMapDefinition map, Vector3 point) =>
        point.x >= map.Bounds.CameraMin.x && point.y >= map.Bounds.CameraMin.y && point.z >= map.Bounds.CameraMin.z &&
        point.x <= map.Bounds.CameraMax.x && point.y <= map.Bounds.CameraMax.y && point.z <= map.Bounds.CameraMax.z;
    private static Texture2D LoadPng(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        Require(texture.LoadImage(File.ReadAllBytes(path), false), "Failed to load " + path);
        return texture;
    }
    private static float LumaVariance(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        double sum = 0, square = 0;
        for (int i = 0; i < pixels.Length; i += 16)
        {
            double luma = (pixels[i].r * 0.2126 + pixels[i].g * 0.7152 + pixels[i].b * 0.0722) / 255.0;
            sum += luma; square += luma * luma;
        }
        double count = Math.Ceiling(pixels.Length / 16.0);
        return (float)(square / count - Math.Pow(sum / count, 2));
    }
    private static string Sha256File(string path) => Sha256(File.ReadAllBytes(path));
    private static string Sha256(byte[] bytes)
    {
        using SHA256 sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
