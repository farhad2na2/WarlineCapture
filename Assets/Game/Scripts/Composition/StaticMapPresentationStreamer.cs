using System;
using System.Collections.Generic;
using Game.Rendering;
using UnityEngine;
namespace Game.Composition
{
    internal sealed class StaticMapPresentationStreamer
    {
        private const int QueueCapacity = 64;
        private enum RequestKind : byte
        {
            Load,
            Unload
        }
        private enum StatusDisplay : byte
        {
            None,
            Preloading,
            Streaming,
            Draining,
            Drained
        }
        private struct Request
        {
            public int ChunkIndex;
            public RequestKind Kind;
            public byte RetryCount;
        }
        private readonly IStaticMapPresentationSceneApi _sceneApi;
        private readonly bool _enabled;
        private readonly Request[] _queue = new Request[QueueCapacity];
        private readonly HashSet<int> _loadTargets = new();
        private readonly HashSet<int> _retainedChunks = new();
        private readonly HashSet<int> _drainSeen = new();
        private readonly HashSet<int> _drainUnloaded = new();
        private readonly StaticMapPresentationDetachedOperation _detachedOperation = new();
        private StaticMapPresentationChunk[] _chunks = Array.Empty<StaticMapPresentationChunk>();
        private Camera _camera;
        private IStaticMapPresentationSceneOperation _activeOperation;
        private Request _activeRequest;
        private int _queueHead, _queueCount;
        private bool _bound;
        private bool _draining;
        private bool _drainFailure;
        private int _drainTotal;
        private int _reconcileCursor;
        private int _drainCursor;
        private bool _reconcilingLoads = true;
        private bool _loadPassMissing;
        private int _preloadLoadedChecks;
        private bool _drainPassFoundLoaded;
        private bool _hasProjectedExtents;
        private int _projectedMinX, _projectedMaxX, _projectedMinZ, _projectedMaxZ;
        private StatusDisplay _statusDisplay;
        private int _statusCurrent = -1, _statusTotal = -1;
        private string _failure;
        internal StaticMapPresentationStreamer(
            IStaticMapPresentationSceneApi sceneApi = null,
            bool? enabledOverride = null)
        {
            _sceneApi = sceneApi ?? new StaticMapPresentationAddressablesSceneApi();
            _enabled = enabledOverride ?? (sceneApi != null || Application.platform == RuntimePlatform.Android);
            Status = "Unbound";
        }
        public bool PreloadComplete { get; private set; }
        public bool DrainComplete { get; private set; }
        public bool Failed => !string.IsNullOrEmpty(_failure) || !string.IsNullOrEmpty(_detachedOperation.Failure);
        public bool IsDraining => _draining;
        public float Progress01 { get; private set; }
        public string Status { get; private set; }
        internal int PendingOperationCount => _queueCount;
        internal bool HasActiveOperation => _activeOperation != null || _detachedOperation.IsPending;
        internal bool HasDetachedOperation => _detachedOperation.IsPending;
        internal int LastSceneStateChecks { get; private set; }
        internal int TargetRebuildCount { get; private set; }
        public bool Bind(StaticMapPresentationManifest manifest, Camera camera)
        {
            if (_detachedOperation.BlocksBind(_sceneApi))
            {
                Status = _detachedOperation.Status;
                return false;
            }
            Reset();
            _bound = true;
            if (!_enabled)
            {
                PreloadComplete = true;
                Progress01 = 1f;
                Status = "Disabled";
                return true;
            }
            if (!StaticMapPresentationManifestIndex.TryCreate(
                    manifest, camera, out _chunks, out _chunkSize, out string error))
            {
                Fail(error);
                return false;
            }
            if (_sceneApi is IStaticMapPresentationManifestBindingSceneApi manifestBinding &&
                !manifestBinding.TryBindManifest(manifest, out error))
            {
                Fail(error);
                return false;
            }
            _camera = camera;
            if (!TryRefreshTargets(out error))
            {
                Fail(error);
                return false;
            }
            ReconcileQueue();
            RefreshPreloadState();
            return !Failed;
        }
        public void Unbind()
        {
            if (_activeOperation != null)
                _detachedOperation.Capture(_activeOperation, _activeRequest.Kind == RequestKind.Load, _activeRequest.RetryCount, _chunks[_activeRequest.ChunkIndex].Path);
            Reset();
        }
        public void Update()
        {
            if (!_bound)
            {
                if (_detachedOperation.BlocksBind(_sceneApi))
                    Status = _detachedOperation.Status;
                return;
            }
            if (!_enabled || (Failed && (!_draining || _drainFailure)))
                return;
            LastSceneStateChecks = 0;
            bool startedOperation = false;
            if (_activeOperation != null)
            {
                if (!_activeOperation.IsDone)
                {
                    RefreshProgress();
                    return;
                }
                CompleteActiveOperation();
                if (Failed)
                    return;
            }
            if (_draining)
            {
                ReconcileDrainQueue();
            }
            else
            {
                if (!TryRefreshTargets(out string error))
                {
                    Fail(error);
                    return;
                }
                ReconcileQueue();
            }
            while (!startedOperation && TryDequeue(out Request request))
            {
                if (LastSceneStateChecks >= 16)
                {
                    TryEnqueue(request);
                    break;
                }
                if (!ShouldExecute(request))
                    continue;
                startedOperation = true;
                StartOperation(request);
            }
            if (_draining)
                RefreshDrainState();
            else
                RefreshPreloadState();
        }
        public void BeginDrain()
        {
            if (_draining)
                return;
            DrainComplete = false;
            PreloadComplete = false;
            if (!_bound)
                return;
            if (!_enabled)
            {
                DrainComplete = true;
                Progress01 = 1f;
                Status = "Disabled";
                return;
            }
            _draining = true;
            _drainFailure = false;
            ClearQueue();
            _drainCursor = 0;
            _drainPassFoundLoaded = false;
            if (_activeOperation != null && _activeRequest.Kind == RequestKind.Load)
            {
                _drainSeen.Add(_activeRequest.ChunkIndex);
                _drainTotal = 1;
            }
            ReconcileDrainQueue();
            RefreshDrainState();
        }
        private void CompleteActiveOperation()
        {
            Request completed = _activeRequest;
            _activeOperation = null;
            _activeRequest = default;
            if (!ShouldExecute(completed))
            {
                if (_draining && completed.Kind == RequestKind.Unload)
                    _drainUnloaded.Add(completed.ChunkIndex);
                return;
            }
            if (completed.RetryCount == 0)
            {
                completed.RetryCount = 1;
                TryEnqueue(completed);
                return;
            }
            Fail($"Scene {completed.Kind.ToString().ToLowerInvariant()} failed twice: {_chunks[completed.ChunkIndex].Path}");
        }
        private void StartOperation(Request request)
        {
            try
            {
                string path = _chunks[request.ChunkIndex].Path;
                _activeOperation = request.Kind == RequestKind.Load
                    ? _sceneApi.LoadAdditive(path)
                    : _sceneApi.Unload(path);
                _activeRequest = request;
                if (_activeOperation == null)
                    HandleStartFailure(request);
            } catch (Exception exception)
            {
                HandleStartFailure(request, exception.Message);
            }
        }
        private void HandleStartFailure(Request request, string detail = null)
        {
            _activeOperation = null;
            _activeRequest = default;
            if (request.RetryCount == 0)
            {
                request.RetryCount = 1;
                TryEnqueue(request);
                return;
            }
            string suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" ({detail})";
            Fail($"Scene {request.Kind.ToString().ToLowerInvariant()} failed to start twice: {_chunks[request.ChunkIndex].Path}{suffix}");
        }
        private bool TryRefreshTargets(out string error)
        {
            if (_camera == null)
            {
                error = "Static map presentation camera is missing.";
                return false;
            }
            if (!StaticMapPresentationManifestIndex.TryGetFootprint(
                    _camera, _chunkSize, out int minX, out int maxX, out int minZ, out int maxZ))
            {
                error = "Static map presentation camera viewport does not project to y=0.";
                return false;
            }
            if (_hasProjectedExtents && minX == _projectedMinX && maxX == _projectedMaxX &&
                minZ == _projectedMinZ && maxZ == _projectedMaxZ)
            {
                error = null;
                return true;
            }
            _projectedMinX = minX;
            _projectedMaxX = maxX;
            _projectedMinZ = minZ;
            _projectedMaxZ = maxZ;
            _hasProjectedExtents = true;
            TargetRebuildCount++;
            ClearQueue();
            _reconcileCursor = 0;
            _reconcilingLoads = true;
            _loadPassMissing = false;
            _preloadLoadedChecks = 0;
            PreloadComplete = false;
            _loadTargets.Clear();
            _retainedChunks.Clear();
            for (int i = 0; i < _chunks.Length; i++)
            {
                StaticMapPresentationChunkCoordinate coordinate = _chunks[i].Coordinate;
                if (StaticMapPresentationManifestIndex.InsideExpandedRange(
                        coordinate, minX, maxX, minZ, maxZ, 1))
                    _loadTargets.Add(i);
                if (StaticMapPresentationManifestIndex.InsideExpandedRange(
                        coordinate, minX, maxX, minZ, maxZ, 2))
                    _retainedChunks.Add(i);
            }
            error = null;
            return true;
        }
        private float _chunkSize;
        private void ReconcileQueue()
        {
            int budget = 15 - LastSceneStateChecks;
            int completedPasses = 0;
            while (budget > 0 && _queueCount < QueueCapacity)
            {
                int index = _reconcileCursor++;
                budget--;
                bool loaded = IsLoaded(index);
                if (_reconcilingLoads)
                {
                    if (_loadTargets.Contains(index))
                    {
                        if (loaded)
                        {
                            _preloadLoadedChecks++;
                        }
                        else
                        {
                            _loadPassMissing = true;
                            TryEnqueue(new Request { ChunkIndex = index, Kind = RequestKind.Load });
                        }
                    }
                } else if (!_retainedChunks.Contains(index) && loaded)
                    TryEnqueue(new Request { ChunkIndex = index, Kind = RequestKind.Unload });
                if (_reconcileCursor < _chunks.Length)
                    continue;
                _reconcileCursor = 0;
                if (_reconcilingLoads)
                {
                    PreloadComplete = !_loadPassMissing;
                    _loadPassMissing = false;
                    if (PreloadComplete)
                        _preloadLoadedChecks = _loadTargets.Count;
                    else
                        _preloadLoadedChecks = 0;
                }
                else
                    _preloadLoadedChecks = 0;
                _reconcilingLoads = !_reconcilingLoads;
                if (++completedPasses >= 2)
                    break;
            }
        }
        private void ReconcileDrainQueue()
        {
            int budget = 15 - LastSceneStateChecks;
            while (budget > 0 && _queueCount < QueueCapacity && !DrainComplete)
            {
                int index = _drainCursor++;
                budget--;
                if (IsLoaded(index))
                {
                    _drainPassFoundLoaded = true;
                    _drainSeen.Add(index);
                    _drainTotal = Math.Max(_drainTotal, _drainSeen.Count);
                    TryEnqueue(new Request { ChunkIndex = index, Kind = RequestKind.Unload });
                }
                if (_drainCursor < _chunks.Length)
                    continue;
                bool active = _activeOperation != null;
                if (!_drainPassFoundLoaded && !active && _queueCount == 0)
                    DrainComplete = true;
                _drainCursor = 0;
                _drainPassFoundLoaded = false;
                break;
            }
        }
        private bool ShouldExecute(Request request)
        {
            bool loaded = IsLoaded(request.ChunkIndex);
            if (_draining)
                return request.Kind == RequestKind.Unload && loaded;
            return request.Kind == RequestKind.Load
                ? _loadTargets.Contains(request.ChunkIndex) && !loaded
                : !_retainedChunks.Contains(request.ChunkIndex) && loaded;
        }
        private bool IsLoaded(int chunkIndex)
        {
            LastSceneStateChecks++;
            return _sceneApi.IsLoaded(_chunks[chunkIndex].Path);
        }
        private bool TryEnqueue(Request request)
        {
            if (_queueCount >= QueueCapacity || ContainsRequest(request.ChunkIndex, request.Kind))
                return false;
            int tail = (_queueHead + _queueCount) % QueueCapacity;
            _queue[tail] = request;
            _queueCount++;
            return true;
        }
        private bool ContainsRequest(int chunkIndex, RequestKind kind)
        {
            if (_activeOperation != null && _activeRequest.ChunkIndex == chunkIndex && _activeRequest.Kind == kind)
                return true;
            for (int i = 0; i < _queueCount; i++)
            {
                Request queued = _queue[(_queueHead + i) % QueueCapacity];
                if (queued.ChunkIndex == chunkIndex && queued.Kind == kind)
                    return true;
            }
            return false;
        }
        private bool TryDequeue(out Request request)
        {
            if (_queueCount == 0)
            {
                request = default;
                return false;
            }
            request = _queue[_queueHead];
            _queueHead = (_queueHead + 1) % QueueCapacity;
            _queueCount--;
            return true;
        }
        private void ClearQueue()
        {
            _queueHead = 0;
            _queueCount = 0;
        }
        private void RefreshPreloadState()
        {
            int loaded = Math.Min(_preloadLoadedChecks, _loadTargets.Count);
            Progress01 = PreloadComplete || _loadTargets.Count == 0
                ? 1f
                : (float)loaded / _loadTargets.Count;
            SetStatus(PreloadComplete ? StatusDisplay.Streaming : StatusDisplay.Preloading, loaded, _loadTargets.Count);
        }
        private void RefreshDrainState()
        {
            bool inflightLoad = _activeOperation != null && _activeRequest.Kind == RequestKind.Load;
            if (inflightLoad)
                DrainComplete = false;
            int completed = _drainUnloaded.Count;
            Progress01 = DrainComplete || _drainTotal == 0 ? 1f : Mathf.Clamp01((float)completed / _drainTotal);
            SetStatus(DrainComplete ? StatusDisplay.Drained : StatusDisplay.Draining, completed, _drainTotal);
        }
        private void SetStatus(StatusDisplay display, int current, int total)
        {
            if (_statusDisplay == display && _statusCurrent == current && _statusTotal == total)
                return;
            _statusDisplay = display;
            _statusCurrent = current;
            _statusTotal = total;
            Status = display switch
            {
                StatusDisplay.Streaming => "Streaming",
                StatusDisplay.Drained => "Drained",
                StatusDisplay.Preloading => $"Preloading {current}/{total}",
                StatusDisplay.Draining => $"Draining {current}/{total}",
                _ => Status
            };
        }
        private void RefreshProgress()
        {
            if (_draining)
            {
                RefreshDrainState();
            }
            else
            {
                RefreshPreloadState();
                if (!PreloadComplete && _activeRequest.Kind == RequestKind.Load && _loadTargets.Count > 0)
                    Progress01 = Mathf.Clamp01(Progress01 + _activeOperation.Progress01 / _loadTargets.Count);
            }
        }
        private void Fail(string message)
        {
            _failure = message;
            _drainFailure = _draining;
            PreloadComplete = false;
            DrainComplete = false;
            Progress01 = 0f;
            Status = $"Failed: {message}";
            ClearQueue();
        }
        private void Reset()
        {
            _chunks = Array.Empty<StaticMapPresentationChunk>();
            _camera = null;
            _activeOperation = null;
            _activeRequest = default;
            _bound = false;
            _draining = false;
            _drainFailure = false;
            _drainTotal = 0;
            _reconcileCursor = 0;
            _drainCursor = 0;
            _reconcilingLoads = true;
            _loadPassMissing = false;
            _preloadLoadedChecks = 0;
            _drainPassFoundLoaded = false;
            _hasProjectedExtents = false;
            _failure = null;
            _statusDisplay = StatusDisplay.None;
            _statusCurrent = -1;
            _statusTotal = -1;
            _loadTargets.Clear();
            _retainedChunks.Clear();
            _drainSeen.Clear();
            _drainUnloaded.Clear();
            ClearQueue();
            LastSceneStateChecks = 0;
            TargetRebuildCount = 0;
            PreloadComplete = false;
            DrainComplete = false;
            Progress01 = 0f;
            Status = "Unbound";
        }
    }
}
