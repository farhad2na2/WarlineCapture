using System.Collections.Generic;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Object;
using ProductionTransportMode = BuildingProductionSystem.ProductionTransportMode;

internal sealed class BuildingProductionTransportSystem
{
    private const float ProductionTransportLaneSpacing = 12f;
    private const float RunwaySurfaceClearance = 0.03f;
    private const int DefaultTransportPoolPrewarmCount = 2;
    private const int DefaultTransportStatePoolPrewarmCount = 32;

    public delegate void PrepareTransportDropVisualDelegate(GameObject visual);

    private readonly Dictionary<GameObject, Stack<GameObject>> _transportPoolByPrefab = new();
    private readonly Dictionary<GameObject, Renderer[]> _transportRenderersByInstance = new();
    private readonly Dictionary<GameObject, Transform> _transportDoorByInstance = new();
    private readonly HashSet<GameObject> _transportDoorLookupCompleted = new();
    private readonly Dictionary<GameObject, List<Transform>> _transportBladeTransformsByInstance = new();
    private readonly Dictionary<GameObject, int> _prewarmedTransportCountByPrefab = new();
    private readonly Stack<RuntimeBuildingEntity.ActiveProductionTransport> _transportStatePool = new();
    private readonly List<Transform> _transformSearchBuffer = new(64);
    private IReadOnlyList<GameObject> _configuredPoolSourcePrefabs;
    private IReadOnlyDictionary<string, GameObject> _configuredPoolSourcePrefabsByKey;
    private Transform _runtimeRoot;
    private bool[] _laneUsage = new bool[4];
    private int _createdTransportStateCount;

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Camera WorldCamera;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingVisualSystem VisualSystem;
        public readonly BuildingRunwaySystem RunwaySystem;
        public readonly BuildingProductionTransportBridgeSystem TransportBridgeSystem;
        public readonly BuildingProductionTransportBridgeSystem.Context TransportBridgeContext;
        public readonly PrepareTransportDropVisualDelegate PrepareTransportDropVisual;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Camera worldCamera,
            BuildingProductionSystem productionSystem,
            BuildingVisualSystem visualSystem,
            BuildingRunwaySystem runwaySystem,
            BuildingProductionTransportBridgeSystem transportBridgeSystem,
            BuildingProductionTransportBridgeSystem.Context transportBridgeContext,
            PrepareTransportDropVisualDelegate prepareTransportDropVisual = null)
        {
            RuntimeBuildings = runtimeBuildings;
            WorldCamera = worldCamera;
            ProductionSystem = productionSystem;
            VisualSystem = visualSystem;
            RunwaySystem = runwaySystem;
            TransportBridgeSystem = transportBridgeSystem;
            TransportBridgeContext = transportBridgeContext;
            PrepareTransportDropVisual = prepareTransportDropVisual;
        }
    }

    public void SetRuntimeRoot(Transform runtimeRoot)
    {
        _runtimeRoot = runtimeRoot;
    }

    public bool TryEnsureActiveProductionTransport(
        Context context,
        RuntimeBuildingEntity building,
        RuntimeBuildingEntity.PendingProduction pending,
        float now,
        ref uint randomState)
    {
        if (building == null || pending == null || pending.TransportPrefab == null || building.ActiveTransport != null)
            return building?.ActiveTransport != null;

        Vector3 hoverPosition;
        Vector3 entryPosition;
        Vector3 touchdownPosition;
        Vector3 exitPosition;
        Quaternion hoverRotation;
        Quaternion entryRotation;
        Quaternion exitRotation;
        int laneIndex = 0;

        if (pending.TransportMode == ProductionTransportMode.Plane)
        {
            if (context.RunwaySystem == null ||
                !context.RunwaySystem.TryGetNearestAirportRunway(
                    context.RuntimeBuildings,
                    building.Instance != null ? building.Instance.transform.position : Vector3.zero,
                    out _,
                    out Vector3 runwayCenter,
                    out Quaternion runwayRotation,
                    out Vector3 runwayHalfExtents))
            {
                return false;
            }

            if (!TryAcquireProductionTransportLane(context, pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                return false;

            Vector3 runwayAxis = runwayRotation * Vector3.forward;
            runwayAxis.y = 0f;
            if (runwayAxis.sqrMagnitude <= 0.0001f)
                runwayAxis = Vector3.forward;
            runwayAxis.Normalize();

            float runwayHalfLength = Mathf.Max(8f, runwayHalfExtents.z);
            Vector3 runwayStart = runwayCenter - (runwayAxis * runwayHalfLength);
            touchdownPosition = runwayStart + (runwayAxis * Mathf.Min(8f, runwayHalfLength * 0.35f));
            touchdownPosition.y = runwayCenter.y + RunwaySurfaceClearance;
            hoverPosition = runwayCenter;
            hoverPosition.y = touchdownPosition.y;
            entryPosition = touchdownPosition - (runwayAxis * Mathf.Max(80f, runwayHalfExtents.z * 5f)) + new Vector3(0f, 28f, 0f);
            exitPosition = hoverPosition + (runwayAxis * Mathf.Max(90f, runwayHalfExtents.z * 6f)) + new Vector3(0f, 32f, 0f);
            hoverRotation = Quaternion.LookRotation(runwayAxis, Vector3.up);
            entryRotation = hoverRotation;
            exitRotation = hoverRotation;
        }
        else if (pending.TransportMode == ProductionTransportMode.AirSelf)
        {
            if (!TryAcquireProductionTransportLane(context, pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                return false;

            touchdownPosition = ResolveProductionTransportDropPosition(context, building, pending, ref randomState);
            hoverPosition = touchdownPosition + new Vector3(0f, 6f, 0f);
            hoverPosition += ResolveProductionTransportLaneOffset(context, laneIndex, pending.TransportMaxConcurrent);
            Vector3 horizontalOffset = context.WorldCamera != null
                ? -context.WorldCamera.transform.right.normalized * 70f
                : new Vector3(-70f, 0f, 0f);
            entryPosition = hoverPosition + horizontalOffset + new Vector3(0f, 16f, 0f);
            exitPosition = hoverPosition;
            hoverRotation = Quaternion.LookRotation((hoverPosition - entryPosition).normalized, Vector3.up);
            entryRotation = hoverRotation;
            exitRotation = hoverRotation;
        }
        else
        {
            if (!TryAcquireProductionTransportLane(context, pending.TransportPrefab, pending.TransportMaxConcurrent, out laneIndex))
                return false;

            hoverPosition = ResolveProductionTransportHoverPosition(building, pending);
            hoverPosition += ResolveProductionTransportLaneOffset(context, laneIndex, pending.TransportMaxConcurrent);
            Vector3 horizontalOffset = context.WorldCamera != null
                ? -context.WorldCamera.transform.right.normalized * 60f
                : new Vector3(-60f, 0f, 0f);
            entryPosition = hoverPosition + horizontalOffset;
            exitPosition = hoverPosition - horizontalOffset;
            entryPosition.y = hoverPosition.y + 12f;
            exitPosition.y = hoverPosition.y + 12f;
            touchdownPosition = hoverPosition;
            hoverRotation = Quaternion.LookRotation((hoverPosition - entryPosition).normalized, Vector3.up);
            entryRotation = hoverRotation;
            exitRotation = Quaternion.LookRotation((exitPosition - hoverPosition).normalized, Vector3.up);
        }

        GameObject instance = AcquireProductionTransportInstance(pending.TransportPrefab, context.VisualSystem);
        Transform doorTransform = GetProductionTransportDoorTransform(instance, context.VisualSystem);

        RuntimeBuildingEntity.ActiveProductionTransport transport = AcquireProductionTransportState();
        transport.LaneIndex = laneIndex;
        transport.Prefab = pending.TransportPrefab;
        transport.Instance = instance;
        transport.Transform = instance.transform;
        transport.VisualRenderers = GetProductionTransportRenderers(instance);
        transport.DoorTransform = doorTransform;
        transport.DoorOpenLocalEulerX = doorTransform != null ? doorTransform.localEulerAngles.x : 0f;
        transport.HoverPosition = hoverPosition;
        transport.EntryPosition = entryPosition;
        transport.TouchdownPosition = touchdownPosition;
        transport.ExitPosition = exitPosition;
        transport.HoverRotation = hoverRotation;
        transport.EntryRotation = entryRotation;
        transport.ExitRotation = exitRotation;
        transport.ArrivalSeconds = Mathf.Max(0.5f, pending.TransportArrivalSeconds);
        transport.HoldForNextReadySeconds = Mathf.Max(0.5f, pending.TransportHoldForNextReadySeconds);
        transport.PhaseStartedAt = now;
        transport.HoverEnteredAt = -1f;
        transport.NextDropReadyAt = now;
        transport.Phase = 0;
        transport.Mode = pending.TransportMode;
        transport.ActiveDrop = null;

        transport.Transform.position = transport.EntryPosition;
        transport.Transform.rotation = transport.EntryRotation;
        SetProductionTransportDoorOpen01(transport, 0f);
        building.ActiveTransport = transport;
        return true;
    }

    public void UpdateActiveProductionTransport(Context context, RuntimeBuildingEntity building, float now, float deltaTime, ref uint randomState)
    {
        if (building == null || building.ActiveTransport == null || building.ActiveTransport.Transform == null)
            return;

        RuntimeBuildingEntity.ActiveProductionTransport transport = building.ActiveTransport;
        if (transport.Mode == ProductionTransportMode.Helicopter || transport.Mode == ProductionTransportMode.AirSelf)
            RotateProductionTransportBlades(transport.Instance, deltaTime);

        switch (transport.Phase)
        {
            case 0:
                UpdateArrivalPhase(building, transport, now);
                break;

            case 1:
                UpdateDeliveryPhase(context, building, transport, now, ref randomState);
                break;

            case 2:
                UpdateDeparturePhase(building, transport, now);
                break;
        }
    }

    private void UpdateArrivalPhase(RuntimeBuildingEntity building, RuntimeBuildingEntity.ActiveProductionTransport transport, float now)
    {
        float duration = Mathf.Max(0.5f, transport.ArrivalSeconds);
        float t = Mathf.Clamp01((now - transport.PhaseStartedAt) / duration);
        if (transport.Mode == ProductionTransportMode.Plane)
        {
            if (t < 0.65f)
            {
                float landingT = t / 0.65f;
                transport.Transform.position = Vector3.Lerp(transport.EntryPosition, transport.TouchdownPosition, landingT);
            }
            else
            {
                float taxiT = (t - 0.65f) / 0.35f;
                transport.Transform.position = Vector3.Lerp(transport.TouchdownPosition, transport.HoverPosition, taxiT);
            }
        }
        else
        {
            transport.Transform.position = Vector3.Lerp(transport.EntryPosition, transport.HoverPosition, t);
        }

        transport.Transform.rotation = Quaternion.Slerp(transport.EntryRotation, transport.HoverRotation, t);
        if (transport.Mode == ProductionTransportMode.Plane)
            SetProductionTransportDoorOpen01(transport, 0f);

        if (t < 1f)
            return;

        transport.Phase = 1;
        transport.PhaseStartedAt = now;
        transport.HoverEnteredAt = now;
        transport.NextDropReadyAt = transport.Mode == ProductionTransportMode.Plane ? now + 2f : now;
    }

    private void UpdateDeliveryPhase(Context context, RuntimeBuildingEntity building, RuntimeBuildingEntity.ActiveProductionTransport transport, float now, ref uint randomState)
    {
        if (transport.Mode == ProductionTransportMode.AirSelf)
        {
            float landingT = Mathf.Clamp01((now - transport.PhaseStartedAt) / 1.5f);
            transport.Transform.position = Vector3.Lerp(transport.HoverPosition, transport.TouchdownPosition, landingT);
            transport.Transform.rotation = transport.HoverRotation;

            if (landingT < 1f)
                return;
        }
        else
        {
            transport.Transform.position = transport.HoverPosition;
            transport.Transform.rotation = transport.HoverRotation;
        }

        if (transport.Mode == ProductionTransportMode.Plane)
            SetProductionTransportDoorOpen01(transport, Mathf.Clamp01((now - transport.PhaseStartedAt) / 1.25f));

        if (TryCompleteSelfArrival(context, building, transport, now, ref randomState))
            return;

        if (transport.ActiveDrop != null)
        {
            UpdateActiveTransportDrop(context, building, transport, now, ref randomState);
            return;
        }

        RuntimeBuildingEntity.PendingProduction readyPending = context.ProductionSystem.FindNextReadyTransportPending(building.PendingProductions, transport.Prefab, now);
        if (readyPending != null && now >= transport.NextDropReadyAt)
        {
            StartActiveTransportDrop(context, building, transport, readyPending, now);
            return;
        }

        RuntimeBuildingEntity.PendingProduction soonPending = context.ProductionSystem.FindNextSoonTransportPending(building.PendingProductions, transport.Prefab, now, transport.HoldForNextReadySeconds);
        bool shouldDepart = soonPending == null && now >= transport.HoverEnteredAt + transport.HoldForNextReadySeconds;
        if (shouldDepart)
        {
            transport.Phase = 2;
            transport.PhaseStartedAt = now;
        }
    }

    private bool TryCompleteSelfArrival(Context context, RuntimeBuildingEntity building, RuntimeBuildingEntity.ActiveProductionTransport transport, float now, ref uint randomState)
    {
        if (transport.Mode == ProductionTransportMode.AirSelf)
        {
            RuntimeBuildingEntity.PendingProduction readyAirPending = context.ProductionSystem.FindNextReadyTransportPending(building.PendingProductions, transport.Prefab, now);
            if (readyAirPending == null)
                return false;

            int2 airCell = ResolveProductionGroundGoalCell(context, transport.TouchdownPosition);
            if (TrySpawnPlayerUnitNearBuilding(context, building, readyAirPending.ProductionIndex, readyAirPending.ReservedProductionSlotIndex, transport.TouchdownPosition, airCell, ref randomState))
            {
                bool removedPending = context.ProductionSystem.RemovePendingProduction(building.PendingProductions, readyAirPending);
                if (removedPending)
                    context.ProductionSystem.RebuildPendingProductionTimeline(building.PendingProductions, now, preserveActiveProgress: false);
                AlignNewestProducedUnitRotation(context, building, transport.Transform.forward);
                if (removedPending)
                    context.ProductionSystem.ReleasePendingProduction(readyAirPending);
            }

            DestroyTransport(building, transport);
            return true;
        }

        if (transport.Mode != ProductionTransportMode.Plane)
            return false;

        RuntimeBuildingEntity.PendingProduction readySelfArrivalPending = context.ProductionSystem.FindNextReadyTransportPending(building.PendingProductions, transport.Prefab, now);
        if (readySelfArrivalPending == null || readySelfArrivalPending.Prefab != transport.Prefab)
            return false;

        Vector3 runwaySpawnPosition = transport.HoverPosition;
        int2 runwayCell = ResolveProductionGroundGoalCell(context, runwaySpawnPosition);
        int2 finalGoalCell = ResolveProductionGroundGoalCell(context, ResolveProductionTransportDropPosition(building, readySelfArrivalPending));

        if (TrySpawnPlayerUnitNearBuilding(
            context,
            building,
            readySelfArrivalPending.ProductionIndex,
            readySelfArrivalPending.ReservedProductionSlotIndex,
            runwaySpawnPosition,
            runwayCell,
            ref randomState))
        {
            bool removedPending = context.ProductionSystem.RemovePendingProduction(building.PendingProductions, readySelfArrivalPending);
            if (removedPending)
                context.ProductionSystem.RebuildPendingProductionTimeline(building.PendingProductions, now, preserveActiveProgress: false);
            AlignNewestProducedUnitRotation(context, building, transport.Transform.forward);
            ConfigureNewestRunwayUnit(building, readySelfArrivalPending, transport, runwayCell, context);
            MoveNewestProducedUnitToCell(context, building, finalGoalCell);
            if (removedPending)
                context.ProductionSystem.ReleasePendingProduction(readySelfArrivalPending);
        }

        DestroyTransport(building, transport);
        return true;
    }

    private static void ConfigureNewestRunwayUnit(
        RuntimeBuildingEntity building,
        RuntimeBuildingEntity.PendingProduction pending,
        RuntimeBuildingEntity.ActiveProductionTransport transport,
        int2 runwayCell,
        Context context)
    {
        if (Unity.Entities.World.DefaultGameObjectInjectionWorld == null ||
            context.TransportBridgeSystem == null)
        {
            return;
        }

        EntityManager em = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!BuildingProductionTransportBridgeSystem.TryGetNewestProducedUnit(
                context.TransportBridgeContext,
                building,
                em,
                out Entity newest))
        {
            return;
        }

        if (!em.HasComponent<UnitSpawnTransitTag>(newest))
            em.AddComponent<UnitSpawnTransitTag>(newest);

        if (!em.HasComponent<UnitAirComponent>(newest))
            return;

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(newest);
        airState.UsesRunway = 1;
        airState.RunwayTakeoffPosition = transport.TouchdownPosition;
        airState.RunwayTakeoffCell = ResolveProductionGroundGoalCell(context, transport.TouchdownPosition);
        airState.RunwayLandingPosition = transport.HoverPosition;
        airState.RunwayLandingCell = runwayCell;
        airState.Airborne = 0;
        airState.ReturningHome = 0;
        em.SetComponentData(newest, airState);
    }

    private void UpdateDeparturePhase(RuntimeBuildingEntity building, RuntimeBuildingEntity.ActiveProductionTransport transport, float now)
    {
        float duration = Mathf.Max(0.5f, transport.ArrivalSeconds);
        float t = Mathf.Clamp01((now - transport.PhaseStartedAt) / duration);
        transport.Transform.position = Vector3.Lerp(transport.HoverPosition, transport.ExitPosition, t);
        transport.Transform.rotation = Quaternion.Slerp(transport.HoverRotation, transport.ExitRotation, t);
        if (transport.Mode == ProductionTransportMode.Plane)
            SetProductionTransportDoorOpen01(transport, 1f - t);

        if (t < 1f)
            return;

        DestroyTransport(building, transport);
    }

    private bool TryAcquireProductionTransportLane(Context context, GameObject transportPrefab, int maxConcurrent, out int laneIndex)
    {
        int safeMax = Mathf.Max(1, maxConcurrent);
        EnsureLaneUsageCapacity(safeMax);
        System.Array.Clear(_laneUsage, 0, safeMax);
        if (context.RuntimeBuildings != null)
        {
            foreach (var pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity.ActiveProductionTransport transport = pair.Value?.ActiveTransport;
                if (transport == null || transport.Prefab != transportPrefab)
                    continue;

                if (transport.LaneIndex >= 0 && transport.LaneIndex < safeMax)
                    _laneUsage[transport.LaneIndex] = true;
            }
        }

        for (int i = 0; i < safeMax; i++)
        {
            if (_laneUsage[i])
                continue;

            laneIndex = i;
            return true;
        }

        laneIndex = -1;
        return false;
    }

    public void PrewarmProductionTransportPools(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingVisualSystem visualSystem)
    {
        if (runtimeBuildings == null)
            return;

        foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildings)
        {
            List<RuntimeBuildingEntity.PendingProduction> pendingProductions = pair.Value?.PendingProductions;
            if (pendingProductions == null)
                continue;

            for (int i = 0; i < pendingProductions.Count; i++)
            {
                RuntimeBuildingEntity.PendingProduction pending = pendingProductions[i];
                if (pending?.TransportPrefab == null)
                    continue;

                int count = Mathf.Max(DefaultTransportPoolPrewarmCount, pending.TransportMaxConcurrent);
                PrewarmProductionTransportPool(pending.TransportPrefab, visualSystem, count);
            }
        }
    }

    public void PrewarmConfiguredProductionTransportPools(
        BuildingProductionSystem productionSystem,
        IReadOnlyList<GameObject> unitSpawnPrefabs,
        IReadOnlyDictionary<string, GameObject> unitSpawnPrefabsByKey,
        BuildingProductionSystem.TryGetPrefabLocalBoundsDelegate tryGetPrefabLocalBounds,
        BuildingVisualSystem visualSystem)
    {
        if (productionSystem == null || unitSpawnPrefabs == null)
            return;

        if (ReferenceEquals(_configuredPoolSourcePrefabs, unitSpawnPrefabs) &&
            ReferenceEquals(_configuredPoolSourcePrefabsByKey, unitSpawnPrefabsByKey))
        {
            return;
        }

        _configuredPoolSourcePrefabs = unitSpawnPrefabs;
        _configuredPoolSourcePrefabsByKey = unitSpawnPrefabsByKey;

        productionSystem.PrewarmProductionTransportSettings(
            unitSpawnPrefabs,
            unitSpawnPrefabsByKey,
            tryGetPrefabLocalBounds);

        for (int i = 0; i < unitSpawnPrefabs.Count; i++)
        {
            GameObject unitPrefab = unitSpawnPrefabs[i];
            if (unitPrefab == null)
                continue;

            BuildingProductionSystem.ProductionTransportSettings settings =
                productionSystem.ResolveProductionTransportSettings(
                    unitPrefab,
                    unitSpawnPrefabs,
                    unitSpawnPrefabsByKey,
                    tryGetPrefabLocalBounds);
            if (settings.TransportPrefab == null)
                continue;

            int count = Mathf.Max(DefaultTransportPoolPrewarmCount, settings.MaxConcurrent);
            PrewarmProductionTransportPool(settings.TransportPrefab, visualSystem, count);
        }
    }

    private static Vector3 ResolveProductionTransportLaneOffset(Context context, int laneIndex, int maxConcurrent)
    {
        int safeMax = Mathf.Max(1, maxConcurrent);
        float centered = laneIndex - ((safeMax - 1) * 0.5f);
        Vector3 axis = context.WorldCamera != null
            ? context.WorldCamera.transform.forward.normalized
            : Vector3.forward;
        axis.y = 0f;
        if (axis.sqrMagnitude <= 0.0001f)
            axis = Vector3.forward;
        axis.Normalize();
        return axis * (centered * ProductionTransportLaneSpacing);
    }

    private static void StartActiveTransportDrop(
        Context context,
        RuntimeBuildingEntity building,
        RuntimeBuildingEntity.ActiveProductionTransport transport,
        RuntimeBuildingEntity.PendingProduction pending,
        float now)
    {
        if (building == null || transport == null || pending == null)
            return;

        Vector3 dropStartPosition = transport.Mode == ProductionTransportMode.Plane
            ? ResolvePlaneTransportInteriorWorldPosition(transport)
            : transport.HoverPosition;
        Vector3 finalSpawnPosition = ResolveProductionTransportDropPosition(building, pending);
        Vector3 dropEndPosition = transport.Mode == ProductionTransportMode.Plane
            ? ResolvePlaneTransportRolloutWorldPosition(transport)
            : finalSpawnPosition;
        int2 finalGoalCell = transport.Mode == ProductionTransportMode.Plane
            ? ResolveProductionGroundGoalCell(context, finalSpawnPosition)
            : ResolveProductionGroundGoalCell(context, dropEndPosition);

        GameObject visual = Instantiate(pending.Prefab);
        visual.name = $"{pending.Prefab.name}_TransportDrop";
        HideTransportRuntimeMarkers(visual.transform);
        ApplyTemporaryCharacterIdlePose(visual);
        context.PrepareTransportDropVisual?.Invoke(visual);

        visual.transform.position = dropStartPosition;
        if (transport.Mode == ProductionTransportMode.Plane && transport.Transform != null)
            visual.transform.rotation = Quaternion.LookRotation(-transport.Transform.forward, Vector3.up);

        LineRenderer rope = null;
        if (transport.Mode == ProductionTransportMode.Helicopter)
        {
            rope = new GameObject("TransportDropRope").AddComponent<LineRenderer>();
            rope.transform.SetParent(transport.Transform, false);
            rope.positionCount = 2;
            rope.widthMultiplier = 0.05f;
            rope.material = new Material(Shader.Find("Sprites/Default"));
            rope.startColor = new Color(0.82f, 0.82f, 0.82f, 0.95f);
            rope.endColor = rope.startColor;
        }

        transport.ActiveDrop = new RuntimeBuildingEntity.PendingDropVisual
        {
            Production = pending,
            Visual = visual,
            Rope = rope,
            StartedAt = now,
            Duration = transport.Mode == ProductionTransportMode.Plane ? 3f : 2f,
            StartPosition = dropStartPosition,
            EndPosition = dropEndPosition,
            FinalGoalCell = finalGoalCell
        };
    }

    private static void ApplyTemporaryCharacterIdlePose(GameObject visual)
    {
        if (visual == null || !visual.name.StartsWith("Unit_Chr_", System.StringComparison.Ordinal))
            return;

        MaterialAnimatorIndexAuthoring indexAuthoring = visual.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
        if (indexAuthoring == null || indexAuthoring.animator == null)
            return;

        MaterialAnimatorAuthoring animatorAuthoring = indexAuthoring.animator.GetComponent<MaterialAnimatorAuthoring>();
        if (animatorAuthoring == null || animatorAuthoring.animations == null || animatorAuthoring.animations.Count < 2)
            return;

        MaterialAnimatorBake idleAnimation = animatorAuthoring.animations[1];
        int startPixel = idleAnimation.start;
        int endPixel = startPixel + Mathf.Max(1, idleAnimation.frames);
        Transform animatedRoot = indexAuthoring.transform;
        LODGroup lodGroup = animatedRoot.GetComponentInChildren<LODGroup>(true);
        if (lodGroup == null)
            return;

        MaterialPropertyBlock propertyBlock = new();
        int modelShownId = Shader.PropertyToID("_SnivelerModelShown");
        int renderPixelId = Shader.PropertyToID("_SnivelerRenderPixel");
        var lods = lodGroup.GetLODs();
        for (int i = 0; i < lods.Length; ++i)
        {
            if (lods[i].renderers == null)
                continue;

            for (int rendererIndex = 0; rendererIndex < lods[i].renderers.Length; rendererIndex++)
            {
                Renderer lodRenderer = lods[i].renderers[rendererIndex];
                if (lodRenderer == null)
                    continue;

                for (int materialIndex = 0; materialIndex < lodRenderer.sharedMaterials.Length; materialIndex++)
                {
                    lodRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                    propertyBlock.SetFloat(modelShownId, 1f);
                    propertyBlock.SetVector(renderPixelId, new Vector4(startPixel, endPixel, 0f, 0f));
                    lodRenderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
            }
        }
    }

    private static void UpdateActiveTransportDrop(Context context, RuntimeBuildingEntity building, RuntimeBuildingEntity.ActiveProductionTransport transport, float now, ref uint randomState)
    {
        RuntimeBuildingEntity.PendingDropVisual drop = transport.ActiveDrop;
        if (drop == null)
            return;

        float t = Mathf.Clamp01((now - drop.StartedAt) / Mathf.Max(0.01f, drop.Duration));
        Vector3 unitPosition = Vector3.Lerp(drop.StartPosition, drop.EndPosition, t);
        if (transport.Mode == ProductionTransportMode.Plane)
            unitPosition.y = Mathf.Lerp(drop.StartPosition.y, drop.EndPosition.y, Mathf.SmoothStep(0f, 1f, t));

        if (drop.Visual != null)
        {
            drop.Visual.transform.position = unitPosition;
            if (transport.Mode == ProductionTransportMode.Plane && transport.Transform != null)
            {
                Vector3 rolloutDirection = -transport.Transform.forward;
                rolloutDirection.y = 0f;
                if (rolloutDirection.sqrMagnitude > 0.0001f)
                {
                    rolloutDirection.Normalize();
                    float pitch = Mathf.Lerp(26f, 0f, Mathf.SmoothStep(0f, 1f, t));
                    drop.Visual.transform.rotation = Quaternion.LookRotation(rolloutDirection, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
                }
            }
        }

        if (drop.Rope != null)
        {
            drop.Rope.SetPosition(0, ResolveTransportVisualCenterWorld(transport));
            drop.Rope.SetPosition(1, unitPosition);
        }

        if (t < 1f)
            return;

        if (drop.Visual != null)
            Destroy(drop.Visual);
        if (drop.Rope != null)
            Destroy(drop.Rope.gameObject);

        RuntimeBuildingEntity.PendingProduction production = drop.Production;
        bool removedProduction = context.ProductionSystem.RemovePendingProduction(building.PendingProductions, production);
        if (removedProduction)
            context.ProductionSystem.RebuildPendingProductionTimeline(building.PendingProductions, now, preserveActiveProgress: false);

        if (transport.Mode == ProductionTransportMode.Plane)
        {
            int2 startCell = ResolveProductionGroundGoalCell(context, drop.EndPosition);
            if (TrySpawnPlayerUnitNearBuilding(context, building, production.ProductionIndex, production.ReservedProductionSlotIndex, drop.EndPosition, startCell, ref randomState))
            {
                AlignNewestProducedUnitRotation(context, building, -transport.Transform.forward);
                MoveNewestProducedUnitToCell(context, building, drop.FinalGoalCell);
            }
        }
        else if (TrySpawnPlayerUnitNearBuilding(context, building, production.ProductionIndex, production.ReservedProductionSlotIndex, null, null, ref randomState))
        {
            MoveNewestProducedUnitToCell(context, building, drop.FinalGoalCell);
        }

        if (removedProduction)
            context.ProductionSystem.ReleasePendingProduction(production);
        transport.ActiveDrop = null;
        transport.NextDropReadyAt = now;
    }

    public static int2 ResolveProductionGroundGoalCell(Context context, Vector3 worldPosition)
    {
        if (context.TransportBridgeSystem == null)
            return int2.zero;

        return context.TransportBridgeSystem.ResolveProductionGroundGoalCell(context.TransportBridgeContext, worldPosition);
    }

    public static void MoveNewestProducedUnitToCell(Context context, RuntimeBuildingEntity building, int2 goalCell)
    {
        context.TransportBridgeSystem?.MoveNewestProducedUnitToCell(context.TransportBridgeContext, building, goalCell);
    }

    public static void AlignNewestProducedUnitRotation(Context context, RuntimeBuildingEntity building, Vector3 forward)
    {
        context.TransportBridgeSystem?.AlignNewestProducedUnitRotation(context.TransportBridgeContext, building, forward);
    }

    public static bool TrySpawnPlayerUnitNearBuilding(
        Context context,
        RuntimeBuildingEntity building,
        int productionIndex,
        int reservedProductionSlotIndex,
        Vector3? overrideWorldPosition,
        int2? overrideCell,
        ref uint randomState)
    {
        if (context.TransportBridgeSystem == null)
            return false;

        return context.TransportBridgeSystem.TrySpawnPlayerUnitNearBuilding(
            context.TransportBridgeContext,
            building,
            productionIndex,
            reservedProductionSlotIndex,
            overrideWorldPosition,
            overrideCell,
            ref randomState);
    }

    private static Vector3 ResolveTransportVisualCenterWorld(RuntimeBuildingEntity.ActiveProductionTransport transport)
    {
        if (transport?.Instance == null)
            return transport?.Transform != null ? transport.Transform.position : Vector3.zero;

        Renderer[] renderers = transport.VisualRenderers;
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; renderers != null && i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return bounds.center;

        return transport.Transform != null ? transport.Transform.position : transport.Instance.transform.position;
    }

    private static void SetProductionTransportDoorOpen01(RuntimeBuildingEntity.ActiveProductionTransport transport, float open01)
    {
        if (transport?.DoorTransform == null)
            return;

        Vector3 localEuler = transport.DoorTransform.localEulerAngles;
        localEuler.x = Mathf.LerpAngle(0f, transport.DoorOpenLocalEulerX, Mathf.Clamp01(open01));
        transport.DoorTransform.localEulerAngles = localEuler;
    }

    private static Vector3 ResolvePlaneTransportDoorWorldPosition(RuntimeBuildingEntity.ActiveProductionTransport transport)
    {
        if (transport?.DoorTransform != null)
        {
            Vector3 localPosition = transport.DoorTransform.localPosition;
            localPosition.x = 0f;
            return transport.Transform.TransformPoint(localPosition);
        }

        if (transport?.Transform != null)
            return transport.Transform.position - (transport.Transform.forward * 6f);
        return Vector3.zero;
    }

    private static Vector3 ResolvePlaneTransportInteriorWorldPosition(RuntimeBuildingEntity.ActiveProductionTransport transport)
    {
        Vector3 doorPosition = ResolvePlaneTransportDoorWorldPosition(transport);
        if (transport?.Transform == null)
            return doorPosition + new Vector3(0f, 1.2f, 5f);

        Vector3 inwardDirection = transport.Transform.forward;
        inwardDirection.y = 0f;
        if (inwardDirection.sqrMagnitude <= 0.0001f)
            inwardDirection = Vector3.forward;
        inwardDirection.Normalize();
        Vector3 interior = doorPosition + (inwardDirection * 9.5f);
        interior.y += 1.45f;
        return interior;
    }

    private static Vector3 ResolvePlaneTransportRolloutWorldPosition(RuntimeBuildingEntity.ActiveProductionTransport transport)
    {
        Vector3 doorPosition = ResolvePlaneTransportDoorWorldPosition(transport);
        if (transport?.Transform == null)
            return new Vector3(doorPosition.x, 0.5f, doorPosition.z);

        Vector3 backDirection = -transport.Transform.forward;
        backDirection.y = 0f;
        if (backDirection.sqrMagnitude <= 0.0001f)
            backDirection = Vector3.back;
        backDirection.Normalize();
        Vector3 rollout = doorPosition + (backDirection * 6f);
        rollout.y = 0.5f;
        return rollout;
    }

    private static Vector3 ResolveProductionTransportHoverPosition(RuntimeBuildingEntity building, RuntimeBuildingEntity.PendingProduction pending)
    {
        return ResolveProductionTransportDropPosition(building, pending) + new Vector3(0f, 8f, 0f);
    }

    private static Vector3 ResolveProductionTransportDropPosition(
        Context context,
        RuntimeBuildingEntity building,
        RuntimeBuildingEntity.PendingProduction pending,
        ref uint randomState)
    {
        if (pending?.TransportMode == ProductionTransportMode.AirSelf &&
            pending.Prefab != null &&
            context.TransportBridgeSystem != null)
        {
            byte factionId = BuildingSpawnSystem.ResolveProducedUnitFaction(building);
            if (context.TransportBridgeSystem.TryResolveAvailableFactionHelipadSpawn(
                    context.TransportBridgeContext,
                    factionId,
                    building,
                    pending.Prefab,
                    ref randomState,
                    out _,
                    out Vector3 helipadPosition))
            {
                return new Vector3(helipadPosition.x, Mathf.Max(0.5f, helipadPosition.y), helipadPosition.z);
            }
        }

        return ResolveProductionTransportDropPosition(building, pending);
    }

    private static Vector3 ResolveProductionTransportDropPosition(RuntimeBuildingEntity building, RuntimeBuildingEntity.PendingProduction pending)
    {
        if (building?.Instance != null &&
            pending != null &&
            pending.ReservedProductionSlotIndex >= 0 &&
            building.ProductionSpawnLocalPositions != null &&
            pending.ReservedProductionSlotIndex < building.ProductionSpawnLocalPositions.Length)
        {
            Vector3 slotWorld = building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[pending.ReservedProductionSlotIndex]);
            return new Vector3(slotWorld.x, 0.5f, slotWorld.z);
        }

        if (building?.Instance != null)
        {
            Vector3 position = building.Instance.transform.position + (building.Instance.transform.forward * 4f);
            return new Vector3(position.x, 0.5f, position.z);
        }

        return new Vector3(0f, 0.5f, 0f);
    }

    private static void HideTransportRuntimeMarkers(Transform root)
    {
        if (root == null)
            return;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string name = child.name;
            if (name == "Destroyed" || name == "SelectionMarker")
                child.gameObject.SetActive(false);
        }
    }

    private void RotateProductionTransportBlades(GameObject instance, float deltaTime)
    {
        if (instance == null)
            return;

        float degrees = 1440f * deltaTime;
        List<Transform> bladeTransforms = GetProductionTransportBladeTransforms(instance);
        for (int i = 0; i < bladeTransforms.Count; i++)
        {
            Transform child = bladeTransforms[i];
            if (child == null)
                continue;

            string name = child.name;
            if (name.EndsWith("_X", System.StringComparison.Ordinal))
                child.Rotate(Vector3.right, degrees, Space.Self);
            else if (name.EndsWith("_Y", System.StringComparison.Ordinal))
                child.Rotate(Vector3.up, degrees, Space.Self);
            else if (name.EndsWith("_Z", System.StringComparison.Ordinal))
                child.Rotate(Vector3.forward, degrees, Space.Self);
        }
    }

    private void DestroyTransport(RuntimeBuildingEntity building, RuntimeBuildingEntity.ActiveProductionTransport transport)
    {
        if (transport.Instance != null)
            ReturnProductionTransportInstance(transport.Prefab, transport.Instance);
        ReturnProductionTransportState(transport);
        building.ActiveTransport = null;
    }

    private void EnsureLaneUsageCapacity(int capacity)
    {
        if (_laneUsage.Length >= capacity)
            return;

        _laneUsage = new bool[capacity];
    }

    private void PrewarmProductionTransportPool(GameObject prefab, BuildingVisualSystem visualSystem, int count)
    {
        if (prefab == null || count <= 0)
            return;

        PrewarmProductionTransportStatePool(DefaultTransportStatePoolPrewarmCount);

        if (_prewarmedTransportCountByPrefab.TryGetValue(prefab, out int prewarmedCount) &&
            prewarmedCount >= count)
        {
            return;
        }

        Stack<GameObject> pool = GetProductionTransportPool(prefab);
        int missingCount = count - Mathf.Max(0, prewarmedCount);
        for (int i = 0; i < missingCount; i++)
        {
            GameObject instance = CreateProductionTransportInstance(prefab, visualSystem);
            pool.Push(instance);
        }

        _prewarmedTransportCountByPrefab[prefab] = count;
    }

    private GameObject AcquireProductionTransportInstance(GameObject prefab, BuildingVisualSystem visualSystem)
    {
        Stack<GameObject> pool = GetProductionTransportPool(prefab);
        GameObject instance = pool.Count > 0
            ? pool.Pop()
            : CreateProductionTransportInstance(prefab, visualSystem);

        instance.SetActive(true);
        return instance;
    }

    private void ReturnProductionTransportInstance(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
            return;

        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(EnsureRuntimeRoot(), false);
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instance.SetActive(false);
        GetProductionTransportPool(prefab).Push(instance);
    }

    private GameObject CreateProductionTransportInstance(GameObject prefab, BuildingVisualSystem visualSystem)
    {
        Transform runtimeRoot = EnsureRuntimeRoot();
        GameObject instance = runtimeRoot != null
            ? Instantiate(prefab, runtimeRoot, false)
            : Instantiate(prefab);
        HideTransportRuntimeMarkers(instance.transform);
        CacheProductionTransportInstanceMetadata(instance, visualSystem);
        instance.SetActive(false);
        return instance;
    }

    private Transform EnsureRuntimeRoot()
    {
        if (_runtimeRoot != null)
            return _runtimeRoot;

        var root = new GameObject("RuntimeTransports");
        _runtimeRoot = root.transform;
        return _runtimeRoot;
    }

    private Stack<GameObject> GetProductionTransportPool(GameObject prefab)
    {
        if (!_transportPoolByPrefab.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            _transportPoolByPrefab[prefab] = pool;
        }

        return pool;
    }

    private RuntimeBuildingEntity.ActiveProductionTransport AcquireProductionTransportState()
    {
        if (_transportStatePool.Count > 0)
            return _transportStatePool.Pop();

        _createdTransportStateCount++;
        return new RuntimeBuildingEntity.ActiveProductionTransport();
    }

    private void PrewarmProductionTransportStatePool(int count)
    {
        while (_createdTransportStateCount < count)
        {
            _transportStatePool.Push(new RuntimeBuildingEntity.ActiveProductionTransport());
            _createdTransportStateCount++;
        }
    }

    private void ReturnProductionTransportState(RuntimeBuildingEntity.ActiveProductionTransport transport)
    {
        if (transport == null)
            return;

        transport.LaneIndex = 0;
        transport.Prefab = null;
        transport.Instance = null;
        transport.Transform = null;
        transport.VisualRenderers = null;
        transport.DoorTransform = null;
        transport.DoorOpenLocalEulerX = 0f;
        transport.EntryPosition = default;
        transport.TouchdownPosition = default;
        transport.HoverPosition = default;
        transport.ExitPosition = default;
        transport.HoverRotation = default;
        transport.EntryRotation = default;
        transport.ExitRotation = default;
        transport.ArrivalSeconds = 0f;
        transport.HoldForNextReadySeconds = 0f;
        transport.PhaseStartedAt = 0f;
        transport.Phase = 0;
        transport.HoverEnteredAt = 0f;
        transport.NextDropReadyAt = 0f;
        transport.Mode = default;
        transport.ActiveDrop = null;
        _transportStatePool.Push(transport);
    }

    private Renderer[] GetProductionTransportRenderers(GameObject instance)
    {
        if (instance == null)
            return null;

        if (!_transportRenderersByInstance.TryGetValue(instance, out Renderer[] renderers))
            CacheProductionTransportInstanceMetadata(instance, null);

        return _transportRenderersByInstance.TryGetValue(instance, out renderers) ? renderers : null;
    }

    private Transform GetProductionTransportDoorTransform(GameObject instance, BuildingVisualSystem visualSystem)
    {
        if (instance == null)
            return null;

        if (!_transportDoorByInstance.TryGetValue(instance, out Transform doorTransform) ||
            (!_transportDoorLookupCompleted.Contains(instance) && visualSystem != null))
        {
            CacheProductionTransportInstanceMetadata(instance, visualSystem);
        }

        return _transportDoorByInstance.TryGetValue(instance, out doorTransform) ? doorTransform : null;
    }

    private List<Transform> GetProductionTransportBladeTransforms(GameObject instance)
    {
        if (instance == null)
            return EmptyTransformList;

        if (_transportBladeTransformsByInstance.TryGetValue(instance, out List<Transform> bladeTransforms))
            return bladeTransforms;

        CacheProductionTransportInstanceMetadata(instance, null);
        return _transportBladeTransformsByInstance.TryGetValue(instance, out bladeTransforms)
            ? bladeTransforms
            : EmptyTransformList;
    }

    private void CacheProductionTransportInstanceMetadata(GameObject instance, BuildingVisualSystem visualSystem)
    {
        if (instance == null)
            return;

        if (!_transportRenderersByInstance.ContainsKey(instance))
            _transportRenderersByInstance[instance] = instance.GetComponentsInChildren<Renderer>(true);

        if (!_transportDoorLookupCompleted.Contains(instance) && visualSystem != null)
        {
            Transform doorTransform = visualSystem.FindDescendantByName(instance.transform, "Door_X");
            _transportDoorByInstance[instance] = doorTransform;
            _transportDoorLookupCompleted.Add(instance);
        }
        else if (!_transportDoorByInstance.ContainsKey(instance))
        {
            _transportDoorByInstance[instance] = null;
        }

        if (!_transportBladeTransformsByInstance.ContainsKey(instance))
            _transportBladeTransformsByInstance[instance] = FindProductionTransportBladeTransforms(instance.transform);
    }

    private List<Transform> FindProductionTransportBladeTransforms(Transform root)
    {
        List<Transform> bladeTransforms = new();
        if (root == null)
            return bladeTransforms;

        _transformSearchBuffer.Clear();
        root.GetComponentsInChildren(true, _transformSearchBuffer);
        for (int i = 0; i < _transformSearchBuffer.Count; i++)
        {
            Transform child = _transformSearchBuffer[i];
            if (child == null)
                continue;

            string name = child.name;
            if (name.EndsWith("_X", System.StringComparison.Ordinal) ||
                name.EndsWith("_Y", System.StringComparison.Ordinal) ||
                name.EndsWith("_Z", System.StringComparison.Ordinal))
            {
                bladeTransforms.Add(child);
            }
        }

        _transformSearchBuffer.Clear();
        return bladeTransforms;
    }

    private static readonly List<Transform> EmptyTransformList = new(0);
}
