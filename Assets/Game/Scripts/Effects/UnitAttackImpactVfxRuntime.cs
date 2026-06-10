using System.Collections.Generic;
using UnityEngine;

public sealed class UnitAttackImpactVfxRuntime : MonoBehaviour
{
    private sealed class PooledInstance
    {
        public GameObject Root;
        public ParticleSystem[] ParticleSystems;
        public float ReleaseTime;
        public float StopEmittingTime;
        public bool StopEmittingScheduled;
        public GameObject Prefab;
    }

    private static UnitAttackImpactVfxRuntime _instance;
    private readonly Dictionary<GameObject, Stack<PooledInstance>> _availableByPrefab = new();
    private readonly List<PooledInstance> _active = new();

    public static void Play(GameObject prefab, Vector3 position)
    {
        Play(prefab, position, prefab != null ? prefab.transform.rotation : Quaternion.identity);
    }

    public static void Play(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Play(prefab, position, rotation, float.PositiveInfinity);
    }

    public static void Play(GameObject prefab, Vector3 position, Quaternion rotation, float maxActiveSeconds)
    {
        if (prefab == null)
            return;

        if (_instance == null)
        {
            GameObject root = new("UnitAttackImpactVfxRuntime");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<UnitAttackImpactVfxRuntime>();
        }

        _instance.PlayInternal(prefab, position, rotation, maxActiveSeconds);
    }

    public static void PlayTimedLoop(GameObject prefab, Vector3 position, Quaternion rotation, float emitSeconds, float totalActiveSeconds)
    {
        if (prefab == null)
            return;

        if (_instance == null)
        {
            GameObject root = new("UnitAttackImpactVfxRuntime");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<UnitAttackImpactVfxRuntime>();
        }

        _instance.PlayTimedLoopInternal(prefab, position, rotation, emitSeconds, totalActiveSeconds);
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            PooledInstance instance = _active[i];
            if (instance.StopEmittingScheduled && now >= instance.StopEmittingTime)
            {
                StopEmitting(instance.ParticleSystems);
                instance.StopEmittingScheduled = false;
            }

            if (now < instance.ReleaseTime)
                continue;

            instance.Root.SetActive(false);
            if (!_availableByPrefab.TryGetValue(instance.Prefab, out Stack<PooledInstance> pool))
            {
                pool = new Stack<PooledInstance>();
                _availableByPrefab.Add(instance.Prefab, pool);
            }

            pool.Push(instance);
            _active.RemoveAt(i);
        }
    }

    private void PlayInternal(GameObject prefab, Vector3 position, Quaternion rotation, float maxActiveSeconds)
    {
        PooledInstance instance = GetOrCreate(prefab);
        instance.Root.transform.SetPositionAndRotation(position, rotation);
        instance.Root.SetActive(true);
        float playSeconds = PlayOnce(instance.ParticleSystems, loopDuringPlayback: false);
        if (!float.IsPositiveInfinity(maxActiveSeconds))
            playSeconds = Mathf.Min(playSeconds, Mathf.Max(0.05f, maxActiveSeconds));
        instance.StopEmittingScheduled = false;
        instance.ReleaseTime = Time.time + playSeconds;
        _active.Add(instance);
    }

    private void PlayTimedLoopInternal(GameObject prefab, Vector3 position, Quaternion rotation, float emitSeconds, float totalActiveSeconds)
    {
        PooledInstance instance = GetOrCreate(prefab);
        instance.Root.transform.SetPositionAndRotation(position, rotation);
        instance.Root.SetActive(true);
        PlayOnce(instance.ParticleSystems, loopDuringPlayback: true);

        float safeEmitSeconds = Mathf.Max(0.05f, emitSeconds);
        float safeTotalActiveSeconds = Mathf.Max(safeEmitSeconds + 0.05f, totalActiveSeconds);
        instance.StopEmittingTime = Time.time + safeEmitSeconds;
        instance.StopEmittingScheduled = true;
        instance.ReleaseTime = Time.time + safeTotalActiveSeconds;
        _active.Add(instance);
    }

    private PooledInstance GetOrCreate(GameObject prefab)
    {
        if (_availableByPrefab.TryGetValue(prefab, out Stack<PooledInstance> pool) && pool.Count > 0)
            return pool.Pop();

        GameObject root = Instantiate(prefab, transform);
        root.SetActive(false);
        return new PooledInstance
        {
            Root = root,
            ParticleSystems = root.GetComponentsInChildren<ParticleSystem>(true),
            Prefab = prefab
        };
    }

    private static float PlayOnce(ParticleSystem[] particleSystems, bool loopDuringPlayback)
    {
        float maxSeconds = 0.5f;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = loopDuringPlayback;
            particleSystem.Play(true);
            float startLifetime = main.startLifetime.mode switch
            {
                ParticleSystemCurveMode.TwoConstants => main.startLifetime.constantMax,
                ParticleSystemCurveMode.Curve => main.startLifetime.curveMultiplier,
                ParticleSystemCurveMode.TwoCurves => main.startLifetime.curveMultiplier,
                _ => main.startLifetime.constant
            };

            float duration = Mathf.Max(0f, main.duration) + Mathf.Max(0f, startLifetime);
            maxSeconds = Mathf.Max(maxSeconds, duration);
        }

        return maxSeconds;
    }

    private static void StopEmitting(ParticleSystem[] particleSystems)
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
