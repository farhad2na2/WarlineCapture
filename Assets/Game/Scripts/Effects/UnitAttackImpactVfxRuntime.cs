using System.Collections.Generic;
using UnityEngine;

public sealed class UnitAttackImpactVfxRuntime : MonoBehaviour
{
    private sealed class PooledInstance
    {
        public GameObject Root;
        public ParticleSystem[] ParticleSystems;
        public float ReleaseTime;
        public GameObject Prefab;
    }

    private static UnitAttackImpactVfxRuntime _instance;
    private readonly Dictionary<GameObject, Stack<PooledInstance>> _availableByPrefab = new();
    private readonly List<PooledInstance> _active = new();

    public static void Play(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        if (_instance == null)
        {
            GameObject root = new("UnitAttackImpactVfxRuntime");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<UnitAttackImpactVfxRuntime>();
        }

        _instance.PlayInternal(prefab, position);
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            PooledInstance instance = _active[i];
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

    private void PlayInternal(GameObject prefab, Vector3 position)
    {
        PooledInstance instance = GetOrCreate(prefab);
        instance.Root.transform.SetPositionAndRotation(position, prefab.transform.rotation);
        instance.Root.SetActive(true);
        instance.ReleaseTime = Time.time + PlayOnce(instance.ParticleSystems);
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

    private static float PlayOnce(ParticleSystem[] particleSystems)
    {
        float maxSeconds = 0.5f;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);

            ParticleSystem.MainModule main = particleSystem.main;
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
}
