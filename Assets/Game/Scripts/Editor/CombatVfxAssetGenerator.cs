using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates muzzle flash and bullet impact VFX assets (textures, materials, prefabs)
/// and assigns them to the soldier config.
/// Run via: Tools > Game > Generate Combat VFX (Muzzle Flash + Impact).
/// Safe to re-run; existing assets are overwritten in place.
/// </summary>
public static class CombatVfxAssetGenerator
{
    private const string RootFolder = "Assets/Game/Effects/Combat";
    private const string TexturesFolder = RootFolder + "/Textures";
    private const string MaterialsFolder = RootFolder + "/Materials";
    private const string PrefabsFolder = RootFolder + "/Prefabs";
    private const string ParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";

    private const string SoldierConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_01_Config.asset";

    [MenuItem("Tools/Game/Generate Combat VFX (Muzzle Flash + Impact)")]
    public static void Generate()
    {
        EnsureFolders();

        Texture2D glowTex = SaveTexture("Tex_Vfx_SoftGlow", BuildSoftGlowTexture(128));
        Texture2D flashTex = SaveTexture("Tex_Vfx_MuzzleFlashStar", BuildMuzzleFlashStarTexture(256));
        Texture2D smokeTex = SaveTexture("Tex_Vfx_SmokeSoft", BuildSmokeTexture(128));

        Material flashMat = SaveMaterial("Mat_Vfx_MuzzleFlash_Additive", flashTex, new Color(3.2f, 2.1f, 1.0f, 1f), additive: true);
        Material sparkMat = SaveMaterial("Mat_Vfx_Spark_Additive", glowTex, new Color(3.5f, 1.7f, 0.55f, 1f), additive: true);
        Material impactFlashMat = SaveMaterial("Mat_Vfx_ImpactFlash_Additive", glowTex, new Color(3.0f, 2.2f, 1.2f, 1f), additive: true);
        Material smokeMat = SaveMaterial("Mat_Vfx_Smoke_Alpha", smokeTex, new Color(0.45f, 0.44f, 0.42f, 1f), additive: false);
        Material dustMat = SaveMaterial("Mat_Vfx_Dust_Alpha", smokeTex, new Color(0.55f, 0.48f, 0.38f, 1f), additive: false);

        GameObject muzzleFlashPrefab = BuildMuzzleFlashPrefab(flashMat, sparkMat, smokeMat);
        GameObject impactPrefab = BuildBulletImpactPrefab(impactFlashMat, sparkMat, dustMat);

        AssignToSoldierConfig(muzzleFlashPrefab, impactPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CombatVfxAssetGenerator] Generated combat VFX assets in " + RootFolder + " and assigned them to " + SoldierConfigPath);
    }

    // ---------------------------------------------------------------- folders

    private static void EnsureFolders()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        Directory.CreateDirectory(Path.Combine(projectRoot, TexturesFolder));
        Directory.CreateDirectory(Path.Combine(projectRoot, MaterialsFolder));
        Directory.CreateDirectory(Path.Combine(projectRoot, PrefabsFolder));
        AssetDatabase.Refresh();
    }

    // ---------------------------------------------------------------- textures

    private static Texture2D SaveTexture(string name, Texture2D texture)
    {
        string path = TexturesFolder + "/" + name + ".png";
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        File.WriteAllBytes(Path.Combine(projectRoot, path), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D BuildSoftGlowTexture(int size)
    {
        var tex = NewTexture(size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = RadialDistance(x, y, size);
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), 2.4f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D BuildMuzzleFlashStarTexture(int size)
    {
        var tex = NewTexture(size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float theta = Mathf.Atan2(ny, nx);

                float glow = Mathf.Pow(Mathf.Clamp01(1f - r), 2.0f);
                // Six spikes radiating from the center, fading with distance.
                float spike = Mathf.Pow(Mathf.Abs(Mathf.Cos(theta * 3f)), 36f) * Mathf.Pow(Mathf.Clamp01(1f - r), 1.2f);
                float core = Mathf.Pow(Mathf.Clamp01(1f - r * 3.2f), 1.5f);

                float a = Mathf.Clamp01(core + glow * 0.85f + spike * 0.9f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D BuildSmokeTexture(int size)
    {
        var tex = NewTexture(size);
        const float noiseScale = 5.5f;
        float offsetX = 13.7f;
        float offsetY = 71.3f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = (y + 0.5f) / size;
                float r = RadialDistance(x, y, size);

                float noise =
                    Mathf.PerlinNoise(offsetX + u * noiseScale, offsetY + v * noiseScale) * 0.6f +
                    Mathf.PerlinNoise(offsetX + u * noiseScale * 2.3f, offsetY + v * noiseScale * 2.3f) * 0.4f;
                float falloff = Mathf.Pow(Mathf.Clamp01(1f - r), 1.8f);
                float a = Mathf.Clamp01(falloff * (0.45f + 0.75f * noise));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return tex;
    }

    private static Texture2D NewTexture(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };
    }

    private static float RadialDistance(int x, int y, int size)
    {
        float nx = (x + 0.5f) / size * 2f - 1f;
        float ny = (y + 0.5f) / size * 2f - 1f;
        return Mathf.Sqrt(nx * nx + ny * ny);
    }

    // ---------------------------------------------------------------- materials

    private static Material SaveMaterial(string name, Texture2D baseMap, Color hdrBaseColor, bool additive)
    {
        Shader shader = Shader.Find(ParticlesUnlitShaderName);
        if (shader == null)
        {
            Debug.LogError("[CombatVfxAssetGenerator] Shader not found: " + ParticlesUnlitShaderName);
            return null;
        }

        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;
        if (isNew)
            material = new Material(shader);
        else
            material.shader = shader;

        material.SetTexture("_BaseMap", baseMap);
        material.SetColor("_BaseColor", hdrBaseColor);
        material.SetFloat("_Surface", 1f); // transparent
        material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)RenderQueue.Transparent;

        if (isNew)
            AssetDatabase.CreateAsset(material, path);
        else
            EditorUtility.SetDirty(material);

        return material;
    }

    // ---------------------------------------------------------------- prefabs

    private static GameObject BuildMuzzleFlashPrefab(Material flashMat, Material sparkMat, Material smokeMat)
    {
        // Root +Z points from the muzzle toward the target (set by the runtime).
        var root = new GameObject("Vfx_MuzzleFlash_Rifle");

        // Bright star-shaped flash, 1-2 frames.
        ParticleSystem flash = AddParticleSystem(root, "Flash", flashMat, ParticleSystemRenderMode.Billboard);
        ConfigureMain(flash, duration: 0.1f, lifeMin: 0.045f, lifeMax: 0.075f, speedMin: 0f, speedMax: 0f, sizeMin: 0.45f, sizeMax: 0.75f, maxParticles: 4);
        RandomizeRotation(flash);
        SetBurst(flash, 2);
        DisableShape(flash);
        FadeOut(flash, new Color(1f, 0.95f, 0.8f, 1f));

        // Short hot sparks flying forward out of the barrel.
        ParticleSystem sparks = AddParticleSystem(root, "Sparks", sparkMat, ParticleSystemRenderMode.Stretch, stretchLengthScale: 6f);
        ConfigureMain(sparks, duration: 0.1f, lifeMin: 0.06f, lifeMax: 0.14f, speedMin: 7f, speedMax: 12f, sizeMin: 0.03f, sizeMax: 0.06f, maxParticles: 12);
        SetBurst(sparks, 5);
        SetCone(sparks, angle: 9f, radius: 0.02f);
        FadeOut(sparks, new Color(1f, 0.8f, 0.45f, 1f));

        // Lingering smoke puff drifting forward/up.
        ParticleSystem smoke = AddParticleSystem(root, "Smoke", smokeMat, ParticleSystemRenderMode.Billboard);
        ConfigureMain(smoke, duration: 0.1f, lifeMin: 0.45f, lifeMax: 0.8f, speedMin: 0.5f, speedMax: 1.1f, sizeMin: 0.16f, sizeMax: 0.28f, maxParticles: 6);
        RandomizeRotation(smoke);
        SetBurst(smoke, 2);
        SetCone(smoke, angle: 22f, radius: 0.03f);
        FadeOut(smoke, new Color(1f, 1f, 1f, 0.30f));
        GrowOverLifetime(smoke, 2.6f);
        AddUpwardDrift(smoke, 0.5f);

        return SavePrefab(root);
    }

    private static GameObject BuildBulletImpactPrefab(Material flashMat, Material sparkMat, Material dustMat)
    {
        // Root +Z points from the impact back toward the shooter (set by the runtime).
        var root = new GameObject("Vfx_BulletImpact");

        // Tiny hit flash at torso height.
        ParticleSystem flash = AddParticleSystem(root, "Flash", flashMat, ParticleSystemRenderMode.Billboard, localPosition: new Vector3(0f, 0.75f, 0f));
        ConfigureMain(flash, duration: 0.1f, lifeMin: 0.04f, lifeMax: 0.07f, speedMin: 0f, speedMax: 0f, sizeMin: 0.22f, sizeMax: 0.38f, maxParticles: 4);
        RandomizeRotation(flash);
        SetBurst(flash, 1);
        DisableShape(flash);
        FadeOut(flash, new Color(1f, 0.92f, 0.75f, 1f));

        // Ricochet sparks bouncing back toward the shooter, pulled down by gravity.
        ParticleSystem sparks = AddParticleSystem(root, "Sparks", sparkMat, ParticleSystemRenderMode.Stretch, stretchLengthScale: 5f, localPosition: new Vector3(0f, 0.75f, 0f));
        ConfigureMain(sparks, duration: 0.1f, lifeMin: 0.15f, lifeMax: 0.35f, speedMin: 3f, speedMax: 7f, sizeMin: 0.025f, sizeMax: 0.05f, maxParticles: 16);
        SetGravity(sparks, 1.2f);
        SetBurst(sparks, 8);
        SetCone(sparks, angle: 35f, radius: 0.03f);
        FadeOut(sparks, new Color(1f, 0.75f, 0.4f, 1f));

        // Dust kicked up near the ground.
        ParticleSystem dust = AddParticleSystem(root, "Dust", dustMat, ParticleSystemRenderMode.Billboard, localPosition: new Vector3(0f, 0.15f, 0f));
        ConfigureMain(dust, duration: 0.1f, lifeMin: 0.4f, lifeMax: 0.8f, speedMin: 0.8f, speedMax: 1.6f, sizeMin: 0.22f, sizeMax: 0.42f, maxParticles: 8);
        RandomizeRotation(dust);
        SetBurst(dust, 4);
        SetCone(dust, angle: 55f, radius: 0.06f, rotateUp: true);
        FadeOut(dust, new Color(1f, 1f, 1f, 0.38f));
        GrowOverLifetime(dust, 2.2f);

        return SavePrefab(root);
    }

    private static GameObject SavePrefab(GameObject root)
    {
        string path = PrefabsFolder + "/" + root.name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------------------------------------------------------------- particle helpers

    private static ParticleSystem AddParticleSystem(
        GameObject root,
        string name,
        Material material,
        ParticleSystemRenderMode renderMode,
        float stretchLengthScale = 1f,
        Vector3 localPosition = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPosition;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = renderMode;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (renderMode == ParticleSystemRenderMode.Stretch)
        {
            renderer.lengthScale = stretchLengthScale;
            renderer.velocityScale = 0f;
        }

        return ps;
    }

    private static void ConfigureMain(
        ParticleSystem ps,
        float duration,
        float lifeMin,
        float lifeMax,
        float speedMin,
        float speedMax,
        float sizeMin,
        float sizeMax,
        int maxParticles)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
    }

    private static void SetBurst(ParticleSystem ps, int count)
    {
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    private static void SetCone(ParticleSystem ps, float angle, float radius, bool rotateUp = false)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = radius;
        if (rotateUp)
            shape.rotation = new Vector3(-90f, 0f, 0f); // emit upward instead of along +Z
    }

    private static void DisableShape(ParticleSystem ps)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;
    }

    private static void RandomizeRotation(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
    }

    private static void SetGravity(ParticleSystem ps, float gravityModifier)
    {
        ParticleSystem.MainModule main = ps.main;
        main.gravityModifier = gravityModifier;
    }

    private static void FadeOut(ParticleSystem ps, Color tint)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(tint);

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void GrowOverLifetime(ParticleSystem ps, float endScale)
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 1f / endScale),
            new Keyframe(1f, 1f));

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(endScale, curve);
    }

    private static void AddUpwardDrift(ParticleSystem ps, float upwardSpeed)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(upwardSpeed * 0.6f, upwardSpeed);
    }

    // ---------------------------------------------------------------- config assignment

    private static void AssignToSoldierConfig(GameObject muzzleFlashPrefab, GameObject impactPrefab)
    {
        var config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(SoldierConfigPath);
        if (config == null)
        {
            Debug.LogWarning("[CombatVfxAssetGenerator] Soldier config not found at " + SoldierConfigPath + "; assign the prefabs manually.");
            return;
        }

        var serialized = new SerializedObject(config);
        serialized.FindProperty("muzzleFlashPrefab").objectReferenceValue = muzzleFlashPrefab;
        serialized.FindProperty("attackImpactPrefab").objectReferenceValue = impactPrefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }
}
