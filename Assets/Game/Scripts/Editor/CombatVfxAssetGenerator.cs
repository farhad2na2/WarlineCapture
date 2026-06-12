using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Generates muzzle flash and bullet impact VFX assets (textures, materials, prefabs)
/// per weapon archetype, then assigns them - together with matching tracer settings -
/// to every unit config based on its weapon / display name.
/// Run via: Tools > Game > Generate Combat VFX (Muzzle Flash + Impact).
/// Safe to re-run; existing assets are overwritten in place.
/// </summary>
public static class CombatVfxAssetGenerator
{
    private const string RootFolder = "Assets/Game/Effects/Combat";
    private const string TexturesFolder = RootFolder + "/Textures";
    private const string MaterialsFolder = RootFolder + "/Materials";
    private const string PrefabsFolder = RootFolder + "/Prefabs";
    private const string ConfigsFolder = "Assets/Game/Configs/Prefabs";
    private const string ParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";

    private sealed class WeaponVfxProfile
    {
        public string PrefabId;              // muzzle/impact prefab key (profiles may share)
        public float FlashScale = 1f;        // muzzle flash size multiplier
        public int FlashSparks = 5;
        public int SmokeCount = 2;
        public float ImpactScale = 1f;
        public int ImpactSparks = 8;
        public int ImpactDust = 4;
        public bool ExplosiveImpact; // fireball + smoke column instead of plain sparks
        public Color TraceColor = new(1f, 0.62f, 0.25f, 1f);
        public float TraceWidth = 0.14f;
        public float TraceScrollSpeed = 24f;
        public float TraceDashDensity = 4f;
        public float TraceVisibleSeconds = 0.1f;
        public int TracerEveryNthShot = 3;
        public float MuzzleHeight = 0.95f;
        public float MuzzleForward = 0.5f;
    }

    private static readonly Dictionary<string, WeaponVfxProfile> Profiles = new()
    {
        ["Pistol"] = new WeaponVfxProfile
        {
            PrefabId = "Pistol", FlashScale = 0.6f, FlashSparks = 3, SmokeCount = 1,
            ImpactScale = 0.7f, ImpactSparks = 5, ImpactDust = 2,
            TraceColor = new Color(1f, 0.9f, 0.6f, 1f), TraceWidth = 0.07f,
            TraceScrollSpeed = 18f, TraceDashDensity = 1f, TraceVisibleSeconds = 0.07f,
            TracerEveryNthShot = 1, MuzzleHeight = 0.95f, MuzzleForward = 0.35f
        },
        ["Smg"] = new WeaponVfxProfile
        {
            PrefabId = "Smg", FlashScale = 0.7f, FlashSparks = 4, SmokeCount = 1,
            ImpactScale = 0.8f, ImpactSparks = 6, ImpactDust = 2,
            TraceColor = new Color(1f, 0.82f, 0.45f, 1f), TraceWidth = 0.08f,
            TraceScrollSpeed = 26f, TraceDashDensity = 3f, TraceVisibleSeconds = 0.07f,
            TracerEveryNthShot = 2, MuzzleHeight = 0.95f, MuzzleForward = 0.4f
        },
        ["Rifle"] = new WeaponVfxProfile
        {
            PrefabId = "Rifle", FlashScale = 0.85f, FlashSparks = 4, SmokeCount = 2,
            ImpactScale = 0.9f, ImpactSparks = 7, ImpactDust = 3,
            TraceColor = new Color(1f, 0.75f, 0.35f, 1f), TraceWidth = 0.1f,
            TraceScrollSpeed = 24f, TraceDashDensity = 2f, TraceVisibleSeconds = 0.08f,
            TracerEveryNthShot = 2, MuzzleHeight = 0.95f, MuzzleForward = 0.5f
        },
        ["MachineGun"] = new WeaponVfxProfile
        {
            PrefabId = "MachineGun", FlashScale = 1f, FlashSparks = 5, SmokeCount = 2,
            ImpactScale = 1f, ImpactSparks = 8, ImpactDust = 4,
            TraceColor = new Color(1f, 0.62f, 0.25f, 1f), TraceWidth = 0.14f,
            TraceScrollSpeed = 24f, TraceDashDensity = 4f, TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 3, MuzzleHeight = 0.95f, MuzzleForward = 0.5f
        },
        ["Sniper"] = new WeaponVfxProfile
        {
            PrefabId = "Sniper", FlashScale = 1.1f, FlashSparks = 5, SmokeCount = 3,
            ImpactScale = 1f, ImpactSparks = 8, ImpactDust = 3,
            TraceColor = new Color(0.75f, 0.85f, 1f, 1f), TraceWidth = 0.12f,
            TraceScrollSpeed = 12f, TraceDashDensity = 1f, TraceVisibleSeconds = 0.16f,
            TracerEveryNthShot = 1, MuzzleHeight = 1f, MuzzleForward = 0.6f
        },
        ["Rocket"] = new WeaponVfxProfile
        {
            PrefabId = "Rocket", FlashScale = 3f, FlashSparks = 14, SmokeCount = 12,
            ImpactScale = 4f, ImpactSparks = 24, ImpactDust = 14, ExplosiveImpact = true,
            TraceColor = new Color(1f, 0.55f, 0.2f, 1f), TraceWidth = 0.45f,
            TraceScrollSpeed = 6f, TraceDashDensity = 1f, TraceVisibleSeconds = 0.4f,
            TracerEveryNthShot = 1, MuzzleHeight = 1f, MuzzleForward = 0.5f
        },
        ["HeavyMg"] = new WeaponVfxProfile
        {
            PrefabId = "HeavyMg", FlashScale = 1.3f, FlashSparks = 6, SmokeCount = 3,
            ImpactScale = 1.2f, ImpactSparks = 10, ImpactDust = 5,
            TraceColor = new Color(1f, 0.5f, 0.2f, 1f), TraceWidth = 0.18f,
            TraceScrollSpeed = 28f, TraceDashDensity = 4f, TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 2, MuzzleHeight = 1.6f, MuzzleForward = 1f
        },
        ["TankCannon"] = new WeaponVfxProfile
        {
            PrefabId = "TankCannon", FlashScale = 4.5f, FlashSparks = 20, SmokeCount = 16,
            ImpactScale = 5f, ImpactSparks = 30, ImpactDust = 18, ExplosiveImpact = true,
            TraceColor = new Color(1f, 0.7f, 0.4f, 1f), TraceWidth = 0.6f,
            TraceScrollSpeed = 8f, TraceDashDensity = 1f, TraceVisibleSeconds = 0.3f,
            TracerEveryNthShot = 1, MuzzleHeight = 1.5f, MuzzleForward = 3.2f
        },
        ["Minigun"] = new WeaponVfxProfile
        {
            PrefabId = "Minigun", FlashScale = 1f, FlashSparks = 5, SmokeCount = 2,
            ImpactScale = 1f, ImpactSparks = 8, ImpactDust = 4,
            TraceColor = new Color(1f, 0.55f, 0.25f, 1f), TraceWidth = 0.16f,
            TraceScrollSpeed = 30f, TraceDashDensity = 5f, TraceVisibleSeconds = 0.12f,
            TracerEveryNthShot = 3, MuzzleHeight = 0.2f, MuzzleForward = 1.3f
        },
        ["JetCannon"] = new WeaponVfxProfile
        {
            PrefabId = "JetCannon", FlashScale = 1.2f, FlashSparks = 6, SmokeCount = 2,
            ImpactScale = 1.5f, ImpactSparks = 10, ImpactDust = 5,
            TraceColor = new Color(1f, 0.7f, 0.35f, 1f), TraceWidth = 0.2f,
            TraceScrollSpeed = 30f, TraceDashDensity = 2f, TraceVisibleSeconds = 0.12f,
            TracerEveryNthShot = 1, MuzzleHeight = 0f, MuzzleForward = 2f
        },
        ["DroneGun"] = new WeaponVfxProfile
        {
            PrefabId = "LightGun", FlashScale = 0.8f, FlashSparks = 4, SmokeCount = 1,
            ImpactScale = 0.8f, ImpactSparks = 6, ImpactDust = 3,
            TraceColor = new Color(1f, 0.8f, 0.4f, 1f), TraceWidth = 0.1f,
            TraceScrollSpeed = 26f, TraceDashDensity = 3f, TraceVisibleSeconds = 0.08f,
            TracerEveryNthShot = 2, MuzzleHeight = 0.2f, MuzzleForward = 0.6f
        },
        ["PlaneGun"] = new WeaponVfxProfile
        {
            PrefabId = "LightGun", FlashScale = 0.8f, FlashSparks = 4, SmokeCount = 1,
            ImpactScale = 0.8f, ImpactSparks = 6, ImpactDust = 3,
            TraceColor = new Color(1f, 0.8f, 0.4f, 1f), TraceWidth = 0.1f,
            TraceScrollSpeed = 26f, TraceDashDensity = 3f, TraceVisibleSeconds = 0.08f,
            TracerEveryNthShot = 2, MuzzleHeight = 1.2f, MuzzleForward = 1.6f
        }
    };

    private static Material _flashMat;
    private static Material _sparkMat;
    private static Material _impactFlashMat;
    private static Material _smokeMat;
    private static Material _dustMat;

    [MenuItem("Tools/Game/Generate Combat VFX (Muzzle Flash + Impact)")]
    public static void Generate()
    {
        EnsureFolders();

        Texture2D glowTex = SaveTexture("Tex_Vfx_SoftGlow", BuildSoftGlowTexture(128));
        Texture2D flashTex = SaveTexture("Tex_Vfx_MuzzleFlashStar", BuildMuzzleFlashStarTexture(256));
        Texture2D smokeTex = SaveTexture("Tex_Vfx_SmokeSoft", BuildSmokeTexture(128));

        _flashMat = SaveMaterial("Mat_Vfx_MuzzleFlash_Additive", flashTex, new Color(3.2f, 2.1f, 1.0f, 1f), additive: true);
        _sparkMat = SaveMaterial("Mat_Vfx_Spark_Additive", glowTex, new Color(3.5f, 1.7f, 0.55f, 1f), additive: true);
        _impactFlashMat = SaveMaterial("Mat_Vfx_ImpactFlash_Additive", glowTex, new Color(3.0f, 2.2f, 1.2f, 1f), additive: true);
        _smokeMat = SaveMaterial("Mat_Vfx_Smoke_Alpha", smokeTex, new Color(0.45f, 0.44f, 0.42f, 1f), additive: false);
        _dustMat = SaveMaterial("Mat_Vfx_Dust_Alpha", smokeTex, new Color(0.55f, 0.48f, 0.38f, 1f), additive: false);

        // Build one muzzle + impact prefab per distinct PrefabId.
        var muzzlePrefabs = new Dictionary<string, GameObject>();
        var impactPrefabs = new Dictionary<string, GameObject>();
        foreach (WeaponVfxProfile profile in Profiles.Values)
        {
            if (muzzlePrefabs.ContainsKey(profile.PrefabId))
                continue;
            muzzlePrefabs[profile.PrefabId] = BuildMuzzleFlashPrefab(profile);
            impactPrefabs[profile.PrefabId] = BuildBulletImpactPrefab(profile);
        }

        int applied = ApplyToAllUnitConfigs(muzzlePrefabs, impactPrefabs);
        int resynced = ResyncUnitPrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CombatVfxAssetGenerator] Generated {muzzlePrefabs.Count} muzzle + {impactPrefabs.Count} impact prefabs, applied weapon VFX profiles to {applied} unit configs, resynced {resynced} unit prefabs.");
    }

    // ---------------------------------------------------------------- classification

    private static WeaponVfxProfile Classify(string assetName, UnitGridAuthoringConfig config)
    {
        string weapon = (config.WeaponDisplayName ?? string.Empty).ToLowerInvariant();
        string lowerName = assetName.ToLowerInvariant();

        // Units with their own projectile systems handle VFX themselves.
        if (lowerName.Contains("missle_launcher") || lowerName.Contains("missile_launcher"))
            return null;

        // Unarmed / melee-ish units get no gun VFX.
        if (lowerName.Contains("civilian") || lowerName.Contains("bombsuit"))
            return null;

        if (weapon.Contains("sniper"))
            return Profiles["Sniper"];
        if (weapon.Contains("machine gun"))
            return Profiles["MachineGun"];
        if (weapon.Contains("smg"))
            return Profiles["Smg"];
        if (weapon.Contains("pistol"))
            return Profiles["Pistol"];
        if (weapon.Contains("rpg") || weapon.Contains("rocket"))
            return Profiles["Rocket"];
        if (weapon.Contains("rifle"))
            return Profiles["Rifle"];

        // Vehicles classified by asset name.
        if (lowerName.Contains("tank_usa"))
            return Profiles["TankCannon"];
        if (lowerName.Contains("helicopter_attack"))
            return Profiles["Minigun"];
        if (lowerName.Contains("jet_"))
            return Profiles["JetCannon"];
        if (lowerName.Contains("apc_heavy"))
            return Profiles["HeavyMg"];
        if (lowerName.Contains("light_armored"))
            return Profiles["HeavyMg"];
        if (lowerName.Contains("drone"))
            return Profiles["DroneGun"];
        if (lowerName.Contains("plane_transport"))
            return Profiles["PlaneGun"];

        return null;
    }

    private static int ApplyToAllUnitConfigs(
        Dictionary<string, GameObject> muzzlePrefabs,
        Dictionary<string, GameObject> impactPrefabs)
    {
        int applied = 0;
        string[] guids = AssetDatabase.FindAssets("t:UnitGridAuthoringConfig", new[] { ConfigsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = Path.GetFileNameWithoutExtension(path);
            if (!assetName.Contains("UnitGrid"))
                continue;

            var config = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(path);
            if (config == null || !config.CanAttack)
                continue;

            WeaponVfxProfile profile = Classify(assetName, config);
            if (profile == null)
            {
                Debug.Log($"[CombatVfxAssetGenerator] Skipped (no weapon profile): {assetName}");
                continue;
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("muzzleFlashPrefab").objectReferenceValue = muzzlePrefabs[profile.PrefabId];
            serialized.FindProperty("attackImpactPrefab").objectReferenceValue = impactPrefabs[profile.PrefabId];
            serialized.FindProperty("muzzleFlashHeightOffset").floatValue = profile.MuzzleHeight;
            serialized.FindProperty("muzzleFlashForwardOffset").floatValue = profile.MuzzleForward;
            serialized.FindProperty("attackTraceColor").colorValue = profile.TraceColor;
            serialized.FindProperty("attackTraceWidth").floatValue = profile.TraceWidth;
            serialized.FindProperty("attackTraceScrollSpeed").floatValue = profile.TraceScrollSpeed;
            serialized.FindProperty("attackTraceDashDensity").floatValue = profile.TraceDashDensity;
            serialized.FindProperty("attackTraceVisibleSeconds").floatValue = profile.TraceVisibleSeconds;
            serialized.FindProperty("attackTracerEveryNthShot").intValue = profile.TracerEveryNthShot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            applied++;
        }

        return applied;
    }

    /// <summary>
    /// Re-runs the config -> serialized-field sync on every unit prefab so baked
    /// entity data can never use stale values, then saves the prefabs.
    /// </summary>
    private static int ResyncUnitPrefabs()
    {
        int count = 0;
        var applyMethod = typeof(UnitGridAuthoring).GetMethod(
            "ApplyConfigIfAvailable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (applyMethod == null)
        {
            Debug.LogWarning("[CombatVfxAssetGenerator] Could not reflect UnitGridAuthoring.ApplyConfigIfAvailable; skipping prefab resync.");
            return 0;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Prefabs" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            var authoring = prefab.GetComponent<UnitGridAuthoring>();
            if (authoring == null)
                continue;

            applyMethod.Invoke(authoring, null);
            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);
            count++;
        }

        return count;
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

    private static GameObject BuildMuzzleFlashPrefab(WeaponVfxProfile profile)
    {
        float s = profile.FlashScale;
        // Root +Z points from the muzzle toward the target (set by the runtime).
        var root = new GameObject("Vfx_MuzzleFlash_" + profile.PrefabId);

        // Bright star-shaped flash, 1-2 frames.
        ParticleSystem flash = AddParticleSystem(root, "Flash", _flashMat, ParticleSystemRenderMode.Billboard);
        ConfigureMain(flash, duration: 0.1f, lifeMin: 0.045f, lifeMax: 0.075f, speedMin: 0f, speedMax: 0f, sizeMin: 0.45f * s, sizeMax: 0.75f * s, maxParticles: 4);
        RandomizeRotation(flash);
        SetBurst(flash, 2);
        DisableShape(flash);
        FadeOut(flash, new Color(1f, 0.95f, 0.8f, 1f));

        // Short hot sparks flying forward out of the barrel.
        ParticleSystem sparks = AddParticleSystem(root, "Sparks", _sparkMat, ParticleSystemRenderMode.Stretch, stretchLengthScale: 6f);
        ConfigureMain(sparks, duration: 0.1f, lifeMin: 0.06f, lifeMax: 0.14f, speedMin: 7f, speedMax: 12f, sizeMin: 0.03f * s, sizeMax: 0.06f * s, maxParticles: profile.FlashSparks * 2);
        SetBurst(sparks, profile.FlashSparks);
        SetCone(sparks, angle: 9f, radius: 0.02f);
        FadeOut(sparks, new Color(1f, 0.8f, 0.45f, 1f));

        // Lingering smoke puff drifting forward/up. Bigger weapons = more smoke.
        ParticleSystem smoke = AddParticleSystem(root, "Smoke", _smokeMat, ParticleSystemRenderMode.Billboard);
        ConfigureMain(smoke, duration: 0.1f, lifeMin: 0.45f, lifeMax: 0.8f, speedMin: 0.5f, speedMax: 1.1f, sizeMin: 0.16f * s, sizeMax: 0.28f * s, maxParticles: profile.SmokeCount * 2);
        RandomizeRotation(smoke);
        SetBurst(smoke, profile.SmokeCount);
        SetCone(smoke, angle: 22f, radius: 0.03f * s);
        FadeOut(smoke, new Color(1f, 1f, 1f, 0.30f));
        GrowOverLifetime(smoke, 2.6f);
        AddUpwardDrift(smoke, 0.5f);

        if (profile.ExplosiveImpact)
        {
            // Cannon/rocket launch blast: a fireball bursting out of the barrel...
            ParticleSystem blast = AddParticleSystem(root, "BlastFire", _sparkMat, ParticleSystemRenderMode.Billboard);
            ConfigureMain(blast, duration: 0.1f, lifeMin: 0.12f, lifeMax: 0.28f, speedMin: 4f, speedMax: 9f, sizeMin: 0.35f * s, sizeMax: 0.6f * s, maxParticles: 16);
            RandomizeRotation(blast);
            SetBurst(blast, 8);
            SetCone(blast, angle: 16f, radius: 0.06f * s);
            FadeOut(blast, new Color(1f, 0.55f, 0.2f, 1f));
            GrowOverLifetime(blast, 1.8f);

            // ...plus one big near-instant glow ball swallowing the muzzle.
            ParticleSystem glow = AddParticleSystem(root, "BlastGlow", _impactFlashMat, ParticleSystemRenderMode.Billboard);
            ConfigureMain(glow, duration: 0.1f, lifeMin: 0.1f, lifeMax: 0.16f, speedMin: 0f, speedMax: 0f, sizeMin: 0.8f * s, sizeMax: 1.1f * s, maxParticles: 4);
            RandomizeRotation(glow);
            SetBurst(glow, 2);
            DisableShape(glow);
            FadeOut(glow, new Color(1f, 0.8f, 0.5f, 1f));
            GrowOverLifetime(glow, 1.6f);
        }

        return SavePrefab(root);
    }

    private static GameObject BuildBulletImpactPrefab(WeaponVfxProfile profile)
    {
        float s = profile.ImpactScale;
        // Root +Z points from the impact back toward the shooter (set by the runtime).
        var root = new GameObject("Vfx_BulletImpact_" + profile.PrefabId);

        // Hit flash at torso height.
        ParticleSystem flash = AddParticleSystem(root, "Flash", _impactFlashMat, ParticleSystemRenderMode.Billboard, localPosition: new Vector3(0f, 0.75f, 0f));
        ConfigureMain(flash, duration: 0.1f, lifeMin: 0.04f, lifeMax: 0.07f, speedMin: 0f, speedMax: 0f, sizeMin: 0.22f * s, sizeMax: 0.38f * s, maxParticles: 4);
        RandomizeRotation(flash);
        SetBurst(flash, 1);
        DisableShape(flash);
        FadeOut(flash, new Color(1f, 0.92f, 0.75f, 1f));

        // Ricochet sparks bouncing back toward the shooter, pulled down by gravity.
        ParticleSystem sparks = AddParticleSystem(root, "Sparks", _sparkMat, ParticleSystemRenderMode.Stretch, stretchLengthScale: 5f, localPosition: new Vector3(0f, 0.75f, 0f));
        ConfigureMain(sparks, duration: 0.1f, lifeMin: 0.15f, lifeMax: 0.35f, speedMin: 3f, speedMax: 7f, sizeMin: 0.025f * s, sizeMax: 0.05f * s, maxParticles: profile.ImpactSparks * 2);
        SetGravity(sparks, 1.2f);
        SetBurst(sparks, profile.ImpactSparks);
        SetCone(sparks, angle: 35f, radius: 0.03f);
        FadeOut(sparks, new Color(1f, 0.75f, 0.4f, 1f));

        // Dust kicked up near the ground.
        ParticleSystem dust = AddParticleSystem(root, "Dust", _dustMat, ParticleSystemRenderMode.Billboard, localPosition: new Vector3(0f, 0.15f, 0f));
        ConfigureMain(dust, duration: 0.1f, lifeMin: 0.4f, lifeMax: 0.8f, speedMin: 0.8f, speedMax: 1.6f, sizeMin: 0.22f * s, sizeMax: 0.42f * s, maxParticles: profile.ImpactDust * 2);
        RandomizeRotation(dust);
        SetBurst(dust, profile.ImpactDust);
        SetCone(dust, angle: 55f, radius: 0.06f * s, rotateUp: true);
        FadeOut(dust, new Color(1f, 1f, 1f, 0.38f));
        GrowOverLifetime(dust, 2.2f);

        if (profile.ExplosiveImpact)
        {
            // Shockwave: one huge flat flash that balloons out and dies instantly.
            ParticleSystem shockwave = AddParticleSystem(root, "Shockwave", _impactFlashMat, ParticleSystemRenderMode.Billboard, localPosition: new Vector3(0f, 0.5f, 0f));
            ConfigureMain(shockwave, duration: 0.1f, lifeMin: 0.16f, lifeMax: 0.24f, speedMin: 0f, speedMax: 0f, sizeMin: 0.9f * s, sizeMax: 1.2f * s, maxParticles: 4);
            RandomizeRotation(shockwave);
            SetBurst(shockwave, 2);
            DisableShape(shockwave);
            FadeOut(shockwave, new Color(1f, 0.85f, 0.6f, 1f));
            GrowOverLifetime(shockwave, 3.2f);

            // Fireball: a roiling ball of additive orange glow expanding outward.
            ParticleSystem fireball = AddParticleSystem(root, "Fireball", _sparkMat, ParticleSystemRenderMode.Billboard, localPosition: new Vector3(0f, 0.4f, 0f));
            ConfigureMain(fireball, duration: 0.1f, lifeMin: 0.25f, lifeMax: 0.5f, speedMin: 1.5f, speedMax: 4f, sizeMin: 0.55f * s, sizeMax: 0.95f * s, maxParticles: 20);
            RandomizeRotation(fireball);
            SetBurst(fireball, 10);
            SetSphere(fireball, 0.18f * s);
            FadeOut(fireball, new Color(1f, 0.6f, 0.25f, 1f));
            GrowOverLifetime(fireball, 2.2f);

            // Burning embers thrown high and pulled down by gravity.
            ParticleSystem embers = AddParticleSystem(root, "Embers", _sparkMat, ParticleSystemRenderMode.Stretch, stretchLengthScale: 4f, localPosition: new Vector3(0f, 0.4f, 0f));
            ConfigureMain(embers, duration: 0.1f, lifeMin: 0.5f, lifeMax: 1.1f, speedMin: 5f, speedMax: 11f, sizeMin: 0.05f * s, sizeMax: 0.1f * s, maxParticles: 30);
            SetGravity(embers, 1.6f);
            SetBurst(embers, 14);
            SetCone(embers, angle: 50f, radius: 0.1f * s, rotateUp: true);
            FadeOut(embers, new Color(1f, 0.65f, 0.3f, 1f));

            // Thick smoke column rising from the blast for a couple of seconds.
            ParticleSystem column = AddParticleSystem(root, "SmokeColumn", _smokeMat, ParticleSystemRenderMode.Billboard, localPosition: new Vector3(0f, 0.4f, 0f));
            ConfigureMain(column, duration: 0.1f, lifeMin: 1.4f, lifeMax: 2.4f, speedMin: 1.5f, speedMax: 3f, sizeMin: 0.6f * s, sizeMax: 0.9f * s, maxParticles: 16);
            RandomizeRotation(column);
            SetBurst(column, 8);
            SetCone(column, angle: 14f, radius: 0.12f * s, rotateUp: true);
            FadeOut(column, new Color(0.85f, 0.82f, 0.78f, 0.55f));
            GrowOverLifetime(column, 3.5f);
            AddUpwardDrift(column, 1f);
        }

        return SavePrefab(root);
    }

    private static void SetSphere(ParticleSystem ps, float radius)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
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
        main.maxParticles = Mathf.Max(4, maxParticles);
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
        // All three axes must use the same curve mode (TwoConstants here).
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(upwardSpeed * 0.6f, upwardSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }
}
