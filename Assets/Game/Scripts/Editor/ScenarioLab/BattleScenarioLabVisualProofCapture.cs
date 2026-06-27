#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class BattleScenarioLabVisualProofCapture
{
    public const string OutputFolder = "Design/VisualLockLayered/_BattleScenarioLab/AD-001";
    public const string ContactSheetPath = OutputFolder + "/ad001_visual_proof_contact_sheet.png";
    private const int CaptureWidth = 1280;
    private const int CaptureHeight = 720;

    [MenuItem("Warline Capture/Scenario Lab/Capture AD-001 Visual Proof")]
    public static void CaptureAd001VisualProof()
    {
        try
        {
            string[] outputs = CaptureProofSet();
            Debug.Log($"[BattleScenarioLab] AD-001 visual proof captured: {string.Join(", ", outputs)}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleScenarioLab] AD-001 visual proof capture failed: {ex}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static string[] CaptureProofSet()
    {
        Directory.CreateDirectory(OutputFolder);

        Scene scene = EditorSceneManager.OpenScene(BattleScenarioLabSceneBuilder.ScenePath, OpenSceneMode.Single);
        Require(scene.IsValid() && scene.isLoaded, $"Scene could not be loaded: {BattleScenarioLabSceneBuilder.ScenePath}");

        BattleScenarioLabSceneReferences references = Object.FindAnyObjectByType<BattleScenarioLabSceneReferences>();
        Require(references != null, "Missing BattleScenarioLabSceneReferences.");
        Require(references.ScenarioDefinition != null, "Manual scene has no scenario definition.");
        Require(references.ScenarioCamera != null, "Manual scene has no scenario camera.");

        BattleScenarioResult result = BattleScenarioAd001Runner.RunDefinition(references.ScenarioDefinition);
        Require(result.Passed, $"AD-001 did not pass before visual proof capture: {result.FailureReason}");

        GameObject ground = GameObject.Find("NeutralGroundPlane");
        if (ground != null)
            ground.SetActive(false);

        TextMesh label = CreateProofLabel(references.ScenarioCamera);
        string noSupportPath = CaptureVariant(references, label, "AD-001-A-NoSupport-Normal", "No radar support");
        string radarPath = CaptureVariant(references, label, "AD-001-B-RadarNear-Normal", "Radar near support");
        string fastRadarPath = CaptureVariant(references, label, "AD-001-D-RadarNear-FastThreat", "Radar near, fast threat");
        string contactSheetPath = CreateContactSheet(noSupportPath, radarPath, fastRadarPath);
        return new[] { noSupportPath, radarPath, fastRadarPath, contactSheetPath };
    }

    private static string CaptureVariant(
        BattleScenarioLabSceneReferences references,
        TextMesh label,
        string variantId,
        string labelText)
    {
        BattleScenarioVariant variant = FindVariant(references.ScenarioDefinition, variantId);
        ApplyVariantMarkers(references, variant);
        label.text = $"{variant.VariantId}\n{labelText}";
        ConfigureProofCamera(references.ScenarioCamera);

        string path = Path.Combine(OutputFolder, SanitizeFileName(variant.VariantId) + ".png");
        CaptureCamera(references.ScenarioCamera, path);
        return path;
    }

    private static BattleScenarioVariant FindVariant(BattleScenarioDefinition definition, string variantId)
    {
        BattleScenarioVariant[] variants = definition.ScenarioVariants;
        for (int i = 0; i < variants.Length; i++)
        {
            if (string.Equals(variants[i].VariantId, variantId, StringComparison.Ordinal))
                return variants[i];
        }

        throw new InvalidOperationException($"Missing AD-001 variant: {variantId}");
    }

    private static void ApplyVariantMarkers(BattleScenarioLabSceneReferences references, BattleScenarioVariant variant)
    {
        references.LauncherMarker.position = new Vector3(0f, 0.6f, 0f);
        references.LauncherMarker.localScale = Vector3.one * 8f;
        references.DefendedTargetMarker.position = new Vector3(-40f, 0.8f, 0f);
        references.DefendedTargetMarker.localScale = Vector3.one * 8f;
        references.IncomingThreatStartMarker.position = new Vector3(
            variant.IncomingThreatStartDistance,
            variant.IncomingThreatAltitude,
            0f);
        references.IncomingThreatStartMarker.localScale = Vector3.one * 8f;

        bool hasRadar = variant.SupportMode == BattleScenarioSupportMode.RadarNear ||
                        variant.SupportMode == BattleScenarioSupportMode.RadarFar ||
                        variant.SupportMode == BattleScenarioSupportMode.Combined;
        references.RadarMarker.gameObject.SetActive(hasRadar);
        if (hasRadar)
        {
            references.RadarMarker.position = new Vector3(variant.RadarDistanceFromLauncher, 0.6f, 0f);
            references.RadarMarker.localScale = Vector3.one * 8f;
        }
    }

    private static void ConfigureProofCamera(Camera camera)
    {
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.06f, 0.07f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 92f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 420f;
        camera.transform.position = new Vector3(70f, 210f, 0f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static TextMesh CreateProofLabel(Camera camera)
    {
        GameObject labelObject = new("ScenarioLabVisualProofLabel");
        labelObject.transform.position = new Vector3(-58f, 1.6f, -56f);
        labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleLeft;
        label.alignment = TextAlignment.Left;
        label.characterSize = 3.8f;
        label.fontSize = 42;
        label.color = new Color(0.9f, 1f, 0.88f, 1f);
        return label;
    }

    private static void CaptureCamera(Camera camera, string path)
    {
        RenderTexture target = new(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
            name = "BattleScenarioLabVisualProof"
        };
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();

            texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            texture.Apply(false, false);
            Require(HasVisiblePixels(texture), $"Visual proof capture is blank: {path}");
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (texture != null)
                Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }

    private static string CreateContactSheet(params string[] paths)
    {
        Texture2D[] captures = new Texture2D[paths.Length];
        Texture2D sheet = null;
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                captures[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Require(captures[i].LoadImage(File.ReadAllBytes(paths[i])), $"Could not load capture for contact sheet: {paths[i]}");
            }

            int padding = 18;
            int width = CaptureWidth * captures.Length + padding * (captures.Length + 1);
            int height = CaptureHeight + padding * 2;
            sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Fill(sheet, new Color32(10, 12, 14, 255));

            for (int i = 0; i < captures.Length; i++)
                Copy(captures[i], sheet, padding + i * (CaptureWidth + padding), padding);

            sheet.Apply(false, false);
            File.WriteAllBytes(ContactSheetPath, sheet.EncodeToPNG());
            return ContactSheetPath;
        }
        finally
        {
            for (int i = 0; i < captures.Length; i++)
            {
                if (captures[i] != null)
                    Object.DestroyImmediate(captures[i]);
            }

            if (sheet != null)
                Object.DestroyImmediate(sheet);
        }
    }

    private static bool HasVisiblePixels(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i += 97)
        {
            Color32 pixel = pixels[i];
            if (pixel.r > 24 || pixel.g > 24 || pixel.b > 24)
                return true;
        }

        return false;
    }

    private static void Fill(Texture2D texture, Color32 color)
    {
        Color32[] pixels = new Color32[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        texture.SetPixels32(pixels);
    }

    private static void Copy(Texture2D source, Texture2D destination, int x, int y)
    {
        Color32[] pixels = source.GetPixels32();
        destination.SetPixels32(x, y, source.width, source.height, pixels);
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
#endif
