using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class MissileTrailVfxView : MonoBehaviour
{
    private const float SmokeTrailSeconds = 0.62f;
    private const float CoreTrailSeconds = 0.18f;
    private const int MissingFrameTolerance = 2;

    private sealed class TrailInstance
    {
        public GameObject Root;
        public TrailRenderer SmokeTrail;
        public TrailRenderer CoreTrail;
        public int LastSeenFrame;
        public float ReleaseTime;
        public bool Releasing;
    }

    private static MissileTrailVfxView _instance;
    private static Material _smokeMaterial;
    private static Material _coreMaterial;

    private readonly Dictionary<Entity, TrailInstance> _active = new();
    private readonly Stack<TrailInstance> _pool = new();
    private readonly List<Entity> _releaseScratch = new();

    public static void Sync(Entity projectile, float3 position, float3 direction)
    {
        EnsureInstance();
        _instance.SyncInternal(projectile, (Vector3)position, (Vector3)math.normalizesafe(direction, new float3(0f, 0f, 1f)));
    }

    public static void ClearAll()
    {
        if (_instance == null)
            return;

        _instance.ClearAllInternal();
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        GameObject root = new("MissileTrailVfxView");
        if (Application.isPlaying)
            DontDestroyOnLoad(root);
        _instance = root.AddComponent<MissileTrailVfxView>();
    }

    private void SyncInternal(Entity projectile, Vector3 position, Vector3 direction)
    {
        if (!_active.TryGetValue(projectile, out TrailInstance trail))
        {
            trail = GetOrCreateTrail();
            _active.Add(projectile, trail);
            ActivateTrail(trail, position, direction);
        }

        trail.Root.transform.SetPositionAndRotation(position, ResolveRotation(direction));
        trail.LastSeenFrame = Time.frameCount;
        if (!trail.Releasing)
            return;

        trail.Releasing = false;
        SetEmitting(trail, true);
    }

    private void Update()
    {
        int frame = Time.frameCount;
        float now = Time.time;
        _releaseScratch.Clear();

        foreach (KeyValuePair<Entity, TrailInstance> pair in _active)
        {
            TrailInstance trail = pair.Value;
            if (!trail.Releasing && frame - trail.LastSeenFrame > MissingFrameTolerance)
            {
                trail.Releasing = true;
                trail.ReleaseTime = now + SmokeTrailSeconds;
                SetEmitting(trail, false);
            }

            if (trail.Releasing && now >= trail.ReleaseTime)
                _releaseScratch.Add(pair.Key);
        }

        for (int i = 0; i < _releaseScratch.Count; i++)
        {
            Entity entity = _releaseScratch[i];
            if (!_active.TryGetValue(entity, out TrailInstance trail))
                continue;

            _active.Remove(entity);
            DeactivateTrail(trail);
            _pool.Push(trail);
        }
    }

    private void ClearAllInternal()
    {
        foreach (KeyValuePair<Entity, TrailInstance> pair in _active)
        {
            TrailInstance trail = pair.Value;
            DeactivateTrail(trail);
            _pool.Push(trail);
        }

        _active.Clear();
        _releaseScratch.Clear();
    }

    private TrailInstance GetOrCreateTrail()
    {
        if (_pool.Count > 0)
            return _pool.Pop();

        GameObject root = new("MissileTrail");
        root.transform.SetParent(transform, worldPositionStays: false);

        TrailRenderer smokeTrail = CreateTrailRenderer(
            root,
            "SmokeTrail",
            SmokeTrailSeconds,
            0.22f,
            1.35f,
            BuildSmokeGradient(),
            SmokeMaterial);
        smokeTrail.minVertexDistance = 0.38f;

        TrailRenderer coreTrail = CreateTrailRenderer(
            root,
            "HotCoreTrail",
            CoreTrailSeconds,
            0.16f,
            0.03f,
            BuildCoreGradient(),
            CoreMaterial);
        coreTrail.minVertexDistance = 0.16f;

        root.SetActive(false);
        return new TrailInstance
        {
            Root = root,
            SmokeTrail = smokeTrail,
            CoreTrail = coreTrail
        };
    }

    private static TrailRenderer CreateTrailRenderer(
        GameObject root,
        string childName,
        float time,
        float startWidth,
        float endWidth,
        Gradient colorGradient,
        Material material)
    {
        GameObject child = new(childName);
        child.transform.SetParent(root.transform, worldPositionStays: false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        TrailRenderer trail = child.AddComponent<TrailRenderer>();
        trail.time = time;
        trail.autodestruct = false;
        trail.emitting = false;
        trail.alignment = LineAlignment.View;
        trail.textureMode = LineTextureMode.Stretch;
        trail.numCornerVertices = 5;
        trail.numCapVertices = 4;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.material = material;
        trail.colorGradient = colorGradient;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, startWidth),
            new Keyframe(1f, endWidth));
        return trail;
    }

    private void ActivateTrail(TrailInstance trail, Vector3 position, Vector3 direction)
    {
        trail.Root.SetActive(true);
        trail.Root.transform.SetPositionAndRotation(position, ResolveRotation(direction));
        trail.SmokeTrail.Clear();
        trail.CoreTrail.Clear();
        SetEmitting(trail, true);
        trail.LastSeenFrame = Time.frameCount;
        trail.ReleaseTime = 0f;
        trail.Releasing = false;
    }

    private static void DeactivateTrail(TrailInstance trail)
    {
        SetEmitting(trail, false);
        trail.SmokeTrail.Clear();
        trail.CoreTrail.Clear();
        trail.Root.SetActive(false);
        trail.ReleaseTime = 0f;
        trail.Releasing = false;
    }

    private static void SetEmitting(TrailInstance trail, bool emitting)
    {
        trail.SmokeTrail.emitting = emitting;
        trail.CoreTrail.emitting = emitting;
    }

    private static Quaternion ResolveRotation(Vector3 direction)
    {
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
    }

    private static Gradient BuildSmokeGradient()
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.78f, 0.82f, 0.82f), 0f),
                new GradientColorKey(new Color(0.55f, 0.58f, 0.58f), 0.55f),
                new GradientColorKey(new Color(0.36f, 0.38f, 0.38f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.38f, 0f),
                new GradientAlphaKey(0.22f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Gradient BuildCoreGradient()
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.93f, 0.58f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.12f), 0.55f),
                new GradientColorKey(new Color(0.26f, 0.67f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.48f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Material SmokeMaterial => _smokeMaterial != null
        ? _smokeMaterial
        : _smokeMaterial = CreateMaterial(new Color(0.62f, 0.66f, 0.66f, 0.34f));

    private static Material CoreMaterial => _coreMaterial != null
        ? _coreMaterial
        : _coreMaterial = CreateMaterial(new Color(1f, 0.78f, 0.18f, 0.8f));

    private static Material CreateMaterial(Color color)
    {
        Shader shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Unlit/Color");
        Material material = new(shader)
        {
            hideFlags = HideFlags.DontSave,
            color = color
        };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }
}
