using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Runtime
{
    internal enum ResourceExchangeVisualActorKind : byte
    {
        None = 0,
        ExchangeMarker = 1,
        TransportPlane = 2,
        ResourceTruck = 3,
        CompletionMarker = 4,
        CancellationMarker = 5
    }

    internal sealed class ResourceExchangeVisualPresentationSystemHelper : IDisposable
    {
        private const string RuntimeRootName = "ResourceExchangeVisualPresentation";

        public delegate bool TryResolvePrefabDelegate(
            ResourceExchangeVisualActorKind actorKind,
            ResourceExchangeVisualRequestComponent request,
            out GameObject prefab);

        private readonly struct ActiveActor
        {
            public readonly int QueueItemId;
            public readonly ResourceExchangeVisualActorKind ActorKind;
            public readonly GameObject Prefab;
            public readonly GameObject Instance;

            public ActiveActor(
                int queueItemId,
                ResourceExchangeVisualActorKind actorKind,
                GameObject prefab,
                GameObject instance)
            {
                QueueItemId = queueItemId;
                ActorKind = actorKind;
                Prefab = prefab;
                Instance = instance;
            }
        }

        public readonly struct Context
        {
            public readonly Transform RuntimeRoot;
            public readonly TryResolvePrefabDelegate TryResolvePrefab;
            public readonly bool ClearConsumedRequests;

            public Context(
                Transform runtimeRoot,
                TryResolvePrefabDelegate tryResolvePrefab,
                bool clearConsumedRequests = true)
            {
                RuntimeRoot = runtimeRoot;
                TryResolvePrefab = tryResolvePrefab;
                ClearConsumedRequests = clearConsumedRequests;
            }
        }

        public struct Result
        {
            public int ProcessedCount;
            public int PlayedCount;
            public int MissingAnchorCount;
            public int MissingPrefabCount;
            public int ReleasedActorCount;
            public int ClearedRequestCount;
        }

        private readonly Dictionary<GameObject, Stack<GameObject>> _poolByPrefab = new();
        private readonly Dictionary<GameObject, Collider[]> _collidersByInstance = new();
        private readonly List<ActiveActor> _activeActors = new();
        private Transform _runtimeRoot;
        private bool _ownsRuntimeRoot;
        private int _createdActorCount;

        public int ActiveActorCount => _activeActors.Count;
        public int CreatedActorCount => _createdActorCount;

        public void SetRuntimeRoot(Transform runtimeRoot)
        {
            _runtimeRoot = runtimeRoot;
            _ownsRuntimeRoot = false;
        }

        public Result ConsumeVisualRequests(
            Context context,
            DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests)
        {
            var result = new Result();
            if (visualRequests.Length == 0)
                return result;

            for (int i = 0; i < visualRequests.Length; i++)
            {
                ResourceExchangeVisualRequestComponent request = visualRequests[i];
                result.ProcessedCount++;
                ResourceExchangeVisualActorKind actorKind = ResolveActorKind(request.CueKind);
                if (actorKind == ResourceExchangeVisualActorKind.None)
                    continue;

                if (IsTerminalActor(actorKind))
                    result.ReleasedActorCount += ReleaseActorsForQueue(request.QueueItemId, includeTerminalMarkers: false);

                if (request.AnchorResolved == 0)
                {
                    result.MissingAnchorCount++;
                    continue;
                }

                if (context.TryResolvePrefab == null ||
                    !context.TryResolvePrefab(actorKind, request, out GameObject prefab) ||
                    prefab == null)
                {
                    result.MissingPrefabCount++;
                    continue;
                }

                GameObject instance = ResolveActiveOrAcquire(context, request, actorKind, prefab);
                PositionActor(instance, request);
                result.PlayedCount++;
            }

            if (context.ClearConsumedRequests)
            {
                result.ClearedRequestCount = visualRequests.Length;
                visualRequests.Clear();
            }

            return result;
        }

        public int ReleaseActorsForQueue(int queueItemId, bool includeTerminalMarkers = true)
        {
            int releasedCount = 0;
            for (int i = _activeActors.Count - 1; i >= 0; i--)
            {
                ActiveActor actor = _activeActors[i];
                if (actor.QueueItemId != queueItemId)
                    continue;

                if (!includeTerminalMarkers && IsTerminalActor(actor.ActorKind))
                    continue;

                _activeActors.RemoveAt(i);
                ReturnActor(actor.Prefab, actor.Instance);
                releasedCount++;
            }

            return releasedCount;
        }

        public void ReleaseAll()
        {
            for (int i = _activeActors.Count - 1; i >= 0; i--)
            {
                ActiveActor actor = _activeActors[i];
                ReturnActor(actor.Prefab, actor.Instance);
            }

            _activeActors.Clear();
        }

        public int GetPooledActorCount(GameObject prefab)
        {
            return prefab != null && _poolByPrefab.TryGetValue(prefab, out Stack<GameObject> pool)
                ? pool.Count
                : 0;
        }

        public static ResourceExchangeVisualActorKind ResolveActorKind(ResourceExchangeVisualCueKind cueKind)
        {
            switch (cueKind)
            {
                case ResourceExchangeVisualCueKind.ExchangeStarted:
                    return ResourceExchangeVisualActorKind.ExchangeMarker;
                case ResourceExchangeVisualCueKind.ExportLoadStarted:
                case ResourceExchangeVisualCueKind.ImportUnloadStarted:
                    return ResourceExchangeVisualActorKind.ResourceTruck;
                case ResourceExchangeVisualCueKind.TransportPlaneLanding:
                case ResourceExchangeVisualCueKind.TransportPlaneDeparting:
                    return ResourceExchangeVisualActorKind.TransportPlane;
                case ResourceExchangeVisualCueKind.ExchangeCompleted:
                    return ResourceExchangeVisualActorKind.CompletionMarker;
                case ResourceExchangeVisualCueKind.ExchangeCancelled:
                    return ResourceExchangeVisualActorKind.CancellationMarker;
                default:
                    return ResourceExchangeVisualActorKind.None;
            }
        }

        public void Dispose()
        {
            for (int i = _activeActors.Count - 1; i >= 0; i--)
            {
                ActiveActor actor = _activeActors[i];
                DestroyRuntimeObject(actor.Instance);
            }

            _activeActors.Clear();

            foreach (Stack<GameObject> pool in _poolByPrefab.Values)
            {
                while (pool.Count > 0)
                    DestroyRuntimeObject(pool.Pop());
            }

            _poolByPrefab.Clear();
            _collidersByInstance.Clear();

            if (_ownsRuntimeRoot && _runtimeRoot != null)
                DestroyRuntimeObject(_runtimeRoot.gameObject);
            _runtimeRoot = null;
            _ownsRuntimeRoot = false;
        }

        private GameObject ResolveActiveOrAcquire(
            Context context,
            in ResourceExchangeVisualRequestComponent request,
            ResourceExchangeVisualActorKind actorKind,
            GameObject prefab)
        {
            int activeIndex = FindActiveActorIndex(request.QueueItemId, actorKind);
            if (activeIndex >= 0)
            {
                ActiveActor active = _activeActors[activeIndex];
                if (active.Prefab == prefab && active.Instance != null)
                    return active.Instance;

                _activeActors.RemoveAt(activeIndex);
                ReturnActor(active.Prefab, active.Instance);
            }

            GameObject instance = AcquireActor(context, prefab);
            _activeActors.Add(new ActiveActor(request.QueueItemId, actorKind, prefab, instance));
            return instance;
        }

        private int FindActiveActorIndex(int queueItemId, ResourceExchangeVisualActorKind actorKind)
        {
            for (int i = 0; i < _activeActors.Count; i++)
            {
                ActiveActor actor = _activeActors[i];
                if (actor.QueueItemId == queueItemId && actor.ActorKind == actorKind)
                    return i;
            }

            return -1;
        }

        private GameObject AcquireActor(Context context, GameObject prefab)
        {
            Stack<GameObject> pool = GetPool(prefab);
            GameObject instance = pool.Count > 0 ? pool.Pop() : CreateActor(context, prefab);
            instance.SetActive(true);
            DisableActorColliders(instance);
            return instance;
        }

        private GameObject CreateActor(Context context, GameObject prefab)
        {
            Transform root = EnsureRuntimeRoot(context);
            GameObject instance = root != null
                ? Object.Instantiate(prefab, root, false)
                : Object.Instantiate(prefab);
            instance.name = prefab.name + "_ResourceExchangeActor";
            instance.SetActive(false);
            CacheActorColliders(instance);
            _createdActorCount++;
            return instance;
        }

        private void ReturnActor(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null)
                return;

            Transform root = EnsureRuntimeRoot(default);
            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(root, false);
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;
            instance.SetActive(false);
            GetPool(prefab).Push(instance);
        }

        private Stack<GameObject> GetPool(GameObject prefab)
        {
            if (!_poolByPrefab.TryGetValue(prefab, out Stack<GameObject> pool))
            {
                pool = new Stack<GameObject>();
                _poolByPrefab[prefab] = pool;
            }

            return pool;
        }

        private Transform EnsureRuntimeRoot(Context context)
        {
            if (context.RuntimeRoot != null)
            {
                _runtimeRoot = context.RuntimeRoot;
                _ownsRuntimeRoot = false;
                return context.RuntimeRoot;
            }

            if (_runtimeRoot != null)
                return _runtimeRoot;

            var root = new GameObject(RuntimeRootName);
            _runtimeRoot = root.transform;
            _ownsRuntimeRoot = true;
            return _runtimeRoot;
        }

        private static void PositionActor(
            GameObject instance,
            in ResourceExchangeVisualRequestComponent request)
        {
            if (instance == null)
                return;

            Transform instanceTransform = instance.transform;
            instanceTransform.SetPositionAndRotation(
                ToVector3(request.AnchorPosition),
                ToQuaternion(request.AnchorRotation));
            instanceTransform.localScale = Vector3.one;
        }

        private void CacheActorColliders(GameObject instance)
        {
            if (instance == null || _collidersByInstance.ContainsKey(instance))
                return;

            _collidersByInstance[instance] = instance.GetComponentsInChildren<Collider>(true);
        }

        private void DisableActorColliders(GameObject instance)
        {
            if (instance == null)
                return;

            if (!_collidersByInstance.TryGetValue(instance, out Collider[] colliders))
            {
                CacheActorColliders(instance);
                _collidersByInstance.TryGetValue(instance, out colliders);
            }

            if (colliders == null)
                return;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }

        private static bool IsTerminalActor(ResourceExchangeVisualActorKind actorKind)
        {
            return actorKind == ResourceExchangeVisualActorKind.CompletionMarker ||
                   actorKind == ResourceExchangeVisualActorKind.CancellationMarker;
        }

        private static Vector3 ToVector3(Unity.Mathematics.float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private static Quaternion ToQuaternion(Unity.Mathematics.quaternion value)
        {
            return new Quaternion(value.value.x, value.value.y, value.value.z, value.value.w);
        }

        private static void DestroyRuntimeObject(GameObject instance)
        {
            if (instance == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }
    }
}
