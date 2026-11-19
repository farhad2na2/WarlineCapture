using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Runtime
{
    public sealed class MissileTrailVfxView : MonoBehaviour
    {
        private const float SmokeTrailSeconds = 1.12f;
        private const float CoreTrailSeconds = 0.15f;
        private const int MissingFrameTolerance = 2;

        private sealed class TrailInstance
        {
            public GameObject Root;
            public TrailRenderer SmokeTrail;
            public TrailRenderer CoreTrail;
            public ParticleSystem Exhaust;
            public int LastSeenFrame;
            public float ReleaseTime;
            public bool Releasing;
        }

        private static MissileTrailVfxView _instance;
        private static Material _smokeMaterial;
        private static Material _coreMaterial;
        private static Material _exhaustMaterial;
        private static Texture2D _puffTexture;

        private readonly Dictionary<Entity, TrailInstance> _active = new();
        private readonly Stack<TrailInstance> _pool = new();
        private readonly List<Entity> _releaseScratch = new();

        public static void Sync(Entity projectile, float3 position, float3 direction, bool interceptor = false)
        {
            EnsureInstance();
            _instance.SyncInternal(projectile, (Vector3)position, (Vector3)math.normalizesafe(direction, new float3(0f, 0f, 1f)), interceptor);
        }

        public static void ClearAll()
        {
            if (_instance == null)
                return;

            _instance.ClearAllInternal();
        }

        public static void ReleaseAll()
        {
            MissileTrailVfxView instance = _instance;
            _instance = null;
            if (!object.ReferenceEquals(instance, null))
            {
                instance._active.Clear();
                instance._pool.Clear();
                instance._releaseScratch.Clear();
                // With domain reload disabled, the managed reference may survive
                // destruction of its native component between Editor play sessions.
                if (instance != null)
                    DestroyRuntimeObject(instance.gameObject);
            }

            DestroyExhaustResources();
            DestroyRuntimeMaterial(ref _smokeMaterial);
            DestroyRuntimeMaterial(ref _coreMaterial);
        }

        public static void Release(Entity projectile)
        {
            if (_instance == null)
                return;

            _instance.ReleaseInternal(projectile);
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

        private void OnDestroy()
        {
            if (!object.ReferenceEquals(_instance, this))
                return;

            _instance = null;
            _active.Clear();
            _pool.Clear();
            _releaseScratch.Clear();
            DestroyExhaustResources();
            DestroyRuntimeMaterial(ref _smokeMaterial);
            DestroyRuntimeMaterial(ref _coreMaterial);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBeforeSubsystemRegistration()
        {
            ReleaseAll();
        }

        private void SyncInternal(Entity projectile, Vector3 position, Vector3 direction, bool interceptor)
        {
            if (!_active.TryGetValue(projectile, out TrailInstance trail))
            {
                trail = GetOrCreateTrail();
                _active.Add(projectile, trail);
                ActivateTrail(trail, position, direction, interceptor);
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

        private void ReleaseInternal(Entity projectile)
        {
            if (!_active.TryGetValue(projectile, out TrailInstance trail) ||
                trail.Releasing)
            {
                return;
            }

            trail.Releasing = true;
            trail.ReleaseTime = Time.time + SmokeTrailSeconds;
            SetEmitting(trail, false);
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
                0.4f,
                1.2f,
                BuildSmokeGradient(),
                SmokeMaterial);
            smokeTrail.minVertexDistance = 0.38f;

            TrailRenderer coreTrail = CreateTrailRenderer(
                root,
                "HotCoreTrail",
                CoreTrailSeconds,
                0.85f,
                0.08f,
                BuildCoreGradient(),
                CoreMaterial);
            coreTrail.minVertexDistance = 0.16f;

            root.SetActive(false);
            return new TrailInstance
            {
                Root = root,
                SmokeTrail = smokeTrail,
                Exhaust = CreateExhaust(root.transform),
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
            trail.sharedMaterial = material;
            trail.colorGradient = colorGradient;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, startWidth),
                new Keyframe(1f, endWidth));
            return trail;
        }

        private void ActivateTrail(TrailInstance trail, Vector3 position, Vector3 direction, bool interceptor)
        {
            trail.Root.transform.SetPositionAndRotation(position, ResolveRotation(direction));
            trail.Root.SetActive(true);
            trail.Exhaust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = trail.Exhaust.main;
            main.startSize = interceptor ? new ParticleSystem.MinMaxCurve(0.55f, 0.9f) : new ParticleSystem.MinMaxCurve(1.3f, 2.1f);
            main.startLifetime = interceptor ? 0.64f : SmokeTrailSeconds;
            var emission = trail.Exhaust.emission;
            emission.rateOverDistance = interceptor ? 1.7f : 1.05f;
            trail.SmokeTrail.widthMultiplier = interceptor ? 0.55f : 1.1f;
            trail.CoreTrail.widthMultiplier = interceptor ? 0.75f : 1.1f;
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
            if (emitting)
                trail.Exhaust.Play(false);
            else
                trail.Exhaust.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private static ParticleSystem CreateExhaust(Transform parent)
        {
            var emitter = new GameObject("Exhaust puffs");
            emitter.transform.SetParent(parent, false);
            var particles = emitter.AddComponent<ParticleSystem>();
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.startSpeed = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 240;
            var shape = particles.shape;
            shape.enabled = false;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.42f, 1f, 1.8f));
            var tint = particles.colorOverLifetime;
            tint.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(new Color(1f, 0.82f, 0.54f), 0f),
                new GradientColorKey(new Color(0.93f, 0.88f, 0.79f), 0.16f),
                new GradientColorKey(new Color(0.68f, 0.67f, 0.65f), 1f)
            }, new[] {
                new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.68f, 0.52f), new GradientAlphaKey(0f, 1f)
            });
            tint.color = gradient;
            if (_exhaustMaterial == null)
            {
                _exhaustMaterial = CreateMaterial(Color.white);
                const int resolution = 32;
                _puffTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
                { name = "Exhaust puff", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[resolution * resolution];
                for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    var point = new Vector2((x + 0.5f) / resolution * 2f - 1f, (y + 0.5f) / resolution * 2f - 1f);
                    float angle = Mathf.Atan2(point.y, point.x);
                    float radius = 0.82f + 0.075f * Mathf.Cos(5f * angle) + 0.04f * Mathf.Sin(3f * angle);
                    float alpha = Mathf.SmoothStep(0f, 1f, (radius - point.magnitude) * 7f);
                    float light = Mathf.Clamp01(0.78f + point.y * 0.21f - point.x * 0.1f);
                    pixels[y * resolution + x] = new Color(light, light, light, alpha);
                }
                _puffTexture.SetPixels32(pixels);
                _puffTexture.Apply(false, true);
                _exhaustMaterial.mainTexture = _puffTexture;
            }
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _exhaustMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private static void DestroyExhaustResources()
        {
            DestroyRuntimeMaterial(ref _exhaustMaterial);
            if (_puffTexture != null)
                DestroyRuntimeObject(_puffTexture);
            _puffTexture = null;
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
                    new GradientColorKey(new Color(0.91f, 0.32f, 0.08f), 1f)
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

        private static void DestroyRuntimeMaterial(ref Material material)
        {
            if (material == null)
                return;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
            material = null;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
