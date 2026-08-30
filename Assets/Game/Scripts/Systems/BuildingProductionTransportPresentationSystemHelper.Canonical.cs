using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingProductionTransportPresentationSystemHelper
    {
        private const byte CanonicalDeliveryArrivalPhase = 0;
        private const byte CanonicalDeliveryDropPhase = 1;
        private const byte CanonicalDeliveryAwaitSpawnPhase = 2;
        private const byte CanonicalDeliveryDeparturePhase = 3;
        private const float CanonicalHelicopterDepartureDistance = 180f;
        private const float CanonicalHelicopterDepartureHeight = 32f;
        private const float CanonicalHelicopterMinimumDepartureSeconds = 4f;

        public delegate void PrepareTransportDropVisualDelegate(GameObject visual);
        public delegate void FocusProductionDeliveryDelegate(Vector3 worldPosition);

        public readonly struct Context
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Camera WorldCamera;
            public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            public readonly BuildingVisualSystem VisualSystem;
            public readonly BuildingRunwaySystem RunwaySystem;
            public readonly BuildingProductionTransportBridgeCompositionSystemHelper TransportBridgeSystem;
            public readonly BuildingProductionTransportBridgeCompositionSystemHelper.Context TransportBridgeContext;
            public readonly PrepareTransportDropVisualDelegate PrepareTransportDropVisual;
            public readonly FocusProductionDeliveryDelegate FocusProductionDelivery;

            public Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                Camera worldCamera,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                BuildingVisualSystem visualSystem,
                BuildingRunwaySystem runwaySystem,
                BuildingProductionTransportBridgeCompositionSystemHelper transportBridgeSystem,
                BuildingProductionTransportBridgeCompositionSystemHelper.Context transportBridgeContext,
                PrepareTransportDropVisualDelegate prepareTransportDropVisual = null,
                FocusProductionDeliveryDelegate focusProductionDelivery = null)
            {
                RuntimeBuildings = runtimeBuildings;
                WorldCamera = worldCamera;
                ProductionSystem = productionSystem;
                VisualSystem = visualSystem;
                RunwaySystem = runwaySystem;
                TransportBridgeSystem = transportBridgeSystem;
                TransportBridgeContext = transportBridgeContext;
                PrepareTransportDropVisual = prepareTransportDropVisual;
                FocusProductionDelivery = focusProductionDelivery;
            }
        }

        private readonly struct CanonicalDeliveryKey : System.IEquatable<CanonicalDeliveryKey>
        {
            public readonly Entity Producer;
            public readonly int RequestId;

            public CanonicalDeliveryKey(Entity producer, int requestId)
            {
                Producer = producer;
                RequestId = requestId;
            }

            public bool Equals(CanonicalDeliveryKey other)
            {
                return Producer.Equals(other.Producer) && RequestId == other.RequestId;
            }

            public override bool Equals(object obj)
            {
                return obj is CanonicalDeliveryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Producer.GetHashCode() * 397) ^ RequestId;
                }
            }
        }

        private sealed class CanonicalDeliverySession
        {
            public GameObject UnitPrefab;
            public GameObject TransportPrefab;
            public GameObject TransportInstance;
            public Transform TransportTransform;
            public Transform DoorTransform;
            public float DoorClosedLocalEulerX;
            public GameObject DropVisual;
            public LineRenderer Rope;
            public Vector3 EntryPosition;
            public Vector3 HoverPosition;
            public Vector3 ExitPosition;
            public Quaternion EntryRotation;
            public Quaternion HoverRotation;
            public Quaternion ExitRotation;
            public Vector3 DropStartPosition;
            public Vector3 DropEndPosition;
            public float ArrivalSeconds;
            public float DepartureSeconds;
            public float PhaseStartedAt;
            public float LastUpdatedAt;
            public int ExpectedRemainingQuantityBeforeSpawn;
            public bool FocusRequested;
            public byte Phase;
        }

        private readonly Dictionary<CanonicalDeliveryKey, CanonicalDeliverySession> _canonicalDeliverySessions = new();
        private readonly List<CanonicalDeliveryKey> _canonicalDeliveryRemovalBuffer = new();

        public BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult UpdateCanonicalOperationMapProductionDelivery(
            Context context,
            Entity producer,
            int requestId,
            GameObject unitPrefab,
            BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings settings,
            ref float3 dropPosition,
            float now)
        {
            if (producer == Entity.Null ||
                requestId <= 0 ||
                unitPrefab == null ||
                settings.TransportPrefab == null ||
                !math.all(math.isfinite(dropPosition)) ||
                float.IsNaN(now) ||
                float.IsInfinity(now))
            {
                return settings.TransportPrefab == null
                    ? BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.NotRequired
                    : BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.Rejected;
            }

            if (settings.Mode != BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Helicopter)
                return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.NotRequired;

            CanonicalDeliveryKey key = new(producer, requestId);
            if (!_canonicalDeliverySessions.TryGetValue(key, out CanonicalDeliverySession session))
            {
                session = StartCanonicalDelivery(
                    context,
                    unitPrefab,
                    settings.TransportPrefab,
                    Mathf.Max(0.5f, settings.ArrivalSeconds),
                    (Vector3)dropPosition,
                    now);
                if (session == null)
                    return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.Rejected;

                _canonicalDeliverySessions.Add(key, session);
                PublishCanonicalDeliveryReadModel(context);
                return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.InProgress;
            }

            if (session.UnitPrefab != unitPrefab || session.TransportPrefab != settings.TransportPrefab)
                return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.Rejected;

            dropPosition = session.DropEndPosition;
            return UpdateCanonicalDelivery(context, key, session, now);
        }

        public void UpdateCanonicalOperationMapProductionDeliveryLifecycle(Context context, float now)
        {
            if (_canonicalDeliverySessions.Count == 0 || float.IsNaN(now) || float.IsInfinity(now))
                return;

            _canonicalDeliveryRemovalBuffer.Clear();
            foreach (KeyValuePair<CanonicalDeliveryKey, CanonicalDeliverySession> pair in _canonicalDeliverySessions)
            {
                CanonicalDeliverySession session = pair.Value;
                if (session == null || session.TransportTransform == null)
                    continue;

                float deltaTime = Mathf.Max(0f, now - session.LastUpdatedAt);
                session.LastUpdatedAt = now;
                RotateProductionTransportBlades(session.TransportInstance, deltaTime);

                if (session.Phase == CanonicalDeliveryAwaitSpawnPhase)
                {
                    session.TransportTransform.position = session.HoverPosition;
                    session.TransportTransform.rotation = session.HoverRotation;
                    if (TryReadCanonicalDeliveryRemainingQuantity(context, pair.Key, out int remainingQuantity))
                    {
                        if (remainingQuantity < session.ExpectedRemainingQuantityBeforeSpawn)
                            BeginCanonicalDrop(context, session, now, remainingQuantity);
                    }
                    else
                    {
                        BeginCanonicalDeparture(session, now);
                    }

                    continue;
                }

                if (session.Phase != CanonicalDeliveryDeparturePhase)
                    continue;

                float t = Mathf.Clamp01((now - session.PhaseStartedAt) / session.DepartureSeconds);
                session.TransportTransform.position = Vector3.Lerp(session.HoverPosition, session.ExitPosition, t);
                session.TransportTransform.rotation = Quaternion.Slerp(session.HoverRotation, session.ExitRotation, t);
                SetCanonicalTransportDoorOpen01(session, 1f - t);
                if (t >= 1f)
                {
                    ReturnProductionTransportInstance(session.TransportPrefab, session.TransportInstance);
                    _canonicalDeliveryRemovalBuffer.Add(pair.Key);
                }
            }

            for (int index = 0; index < _canonicalDeliveryRemovalBuffer.Count; index++)
                _canonicalDeliverySessions.Remove(_canonicalDeliveryRemovalBuffer[index]);
            if (_canonicalDeliveryRemovalBuffer.Count > 0)
                PublishCanonicalDeliveryReadModel(context);
            _canonicalDeliveryRemovalBuffer.Clear();
        }

        internal int CanonicalDeliverySessionCount => _canonicalDeliverySessions.Count;

        internal GameObject CanonicalDeliveryTransportInstanceForTests
        {
            get
            {
                foreach (CanonicalDeliverySession session in _canonicalDeliverySessions.Values)
                    return session?.TransportInstance;
                return null;
            }
        }

        internal GameObject CanonicalDeliveryDropVisualForTests
        {
            get
            {
                foreach (CanonicalDeliverySession session in _canonicalDeliverySessions.Values)
                    return session?.DropVisual;
                return null;
            }
        }

        private CanonicalDeliverySession StartCanonicalDelivery(
            Context context,
            GameObject unitPrefab,
            GameObject transportPrefab,
            float arrivalSeconds,
            Vector3 dropPosition,
            float now)
        {
            Vector3 hoverPosition = dropPosition + new Vector3(0f, 8f, 0f);
            Vector3 horizontalOffset = context.WorldCamera != null
                ? -context.WorldCamera.transform.right.normalized * 60f
                : new Vector3(-60f, 0f, 0f);
            Vector3 entryPosition = hoverPosition + horizontalOffset + new Vector3(0f, 12f, 0f);
            Vector3 departureDirection = -horizontalOffset.normalized;
            Vector3 exitPosition = hoverPosition +
                                   (departureDirection * CanonicalHelicopterDepartureDistance) +
                                   new Vector3(0f, CanonicalHelicopterDepartureHeight, 0f);
            Quaternion hoverRotation = Quaternion.LookRotation((hoverPosition - entryPosition).normalized, Vector3.up);
            Quaternion exitRotation = Quaternion.LookRotation((exitPosition - hoverPosition).normalized, Vector3.up);
            GameObject transportInstance = AcquireProductionTransportInstance(transportPrefab, context.VisualSystem);
            if (transportInstance == null)
                return null;

            var session = new CanonicalDeliverySession
            {
                UnitPrefab = unitPrefab,
                TransportPrefab = transportPrefab,
                TransportInstance = transportInstance,
                TransportTransform = transportInstance.transform,
                EntryPosition = entryPosition,
                HoverPosition = hoverPosition,
                ExitPosition = exitPosition,
                EntryRotation = hoverRotation,
                HoverRotation = hoverRotation,
                ExitRotation = exitRotation,
                DropEndPosition = dropPosition,
                ArrivalSeconds = arrivalSeconds,
                DepartureSeconds = Mathf.Max(CanonicalHelicopterMinimumDepartureSeconds, arrivalSeconds),
                PhaseStartedAt = now,
                LastUpdatedAt = now,
                ExpectedRemainingQuantityBeforeSpawn = 1,
                FocusRequested = false,
                Phase = CanonicalDeliveryArrivalPhase
            };
            session.DoorTransform = GetProductionTransportDoorTransform(transportInstance, context.VisualSystem);
            session.DoorClosedLocalEulerX = session.DoorTransform != null
                ? session.DoorTransform.localEulerAngles.x
                : 0f;
            session.TransportTransform.position = entryPosition;
            session.TransportTransform.rotation = session.EntryRotation;
            SetCanonicalTransportDoorOpen01(session, 0f);
            return session;
        }

        private BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult UpdateCanonicalDelivery(
            Context context,
            CanonicalDeliveryKey key,
            CanonicalDeliverySession session,
            float now)
        {
            float deltaTime = Mathf.Max(0f, now - session.LastUpdatedAt);
            session.LastUpdatedAt = now;
            RotateProductionTransportBlades(session.TransportInstance, deltaTime);

            if (session.Phase == CanonicalDeliveryArrivalPhase)
            {
                float arrivalT = Mathf.Clamp01((now - session.PhaseStartedAt) / session.ArrivalSeconds);
                session.TransportTransform.position = Vector3.Lerp(session.EntryPosition, session.HoverPosition, arrivalT);
                session.TransportTransform.rotation = Quaternion.Slerp(session.EntryRotation, session.HoverRotation, arrivalT);
                if (arrivalT < 1f)
                    return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.InProgress;

                int remainingQuantity = TryReadCanonicalDeliveryRemainingQuantity(
                    context, key, out int queuedQuantity)
                    ? queuedQuantity
                    : 1;
                BeginCanonicalDrop(context, session, now, remainingQuantity);
                return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.InProgress;
            }

            if (session.Phase == CanonicalDeliveryDropPhase)
            {
                session.TransportTransform.position = session.HoverPosition;
                session.TransportTransform.rotation = session.HoverRotation;
                const float dropDurationSeconds = 2f;
                float dropT = Mathf.Clamp01((now - session.PhaseStartedAt) / dropDurationSeconds);
                Vector3 unitPosition = Vector3.Lerp(session.DropStartPosition, session.DropEndPosition, dropT);
                if (session.DropVisual != null)
                    session.DropVisual.transform.position = unitPosition;
                if (session.Rope != null)
                {
                    Vector3 anchor = ResolveCanonicalTransportVisualCenterWorld(session);
                    session.Rope.SetPosition(0, new Vector3(unitPosition.x, anchor.y, unitPosition.z));
                    session.Rope.SetPosition(1, unitPosition);
                }

                if (dropT < 1f)
                    return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.InProgress;

                ReturnTransportDropVisual(session.UnitPrefab, session.DropVisual);
                ReturnTransportDropRope(session.Rope);
                session.DropVisual = null;
                session.Rope = null;
                session.Phase = CanonicalDeliveryAwaitSpawnPhase;
                SetCanonicalTransportDoorOpen01(session, 1f);
                return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.ReadyToSpawn;
            }

            if (session.Phase == CanonicalDeliveryAwaitSpawnPhase)
            {
                session.TransportTransform.position = session.HoverPosition;
                session.TransportTransform.rotation = session.HoverRotation;
                return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.ReadyToSpawn;
            }

            return BuildingProductionRequestSystemHelper.OperationMapProductionDeliveryResult.InProgress;
        }

        private void BeginCanonicalDrop(
            Context context,
            CanonicalDeliverySession session,
            float now,
            int remainingQuantity)
        {
            session.TransportTransform.position = session.HoverPosition;
            session.TransportTransform.rotation = session.HoverRotation;
            if (!session.FocusRequested)
            {
                context.FocusProductionDelivery?.Invoke(session.DropEndPosition);
                session.FocusRequested = true;
            }
            session.DropVisual = AcquireTransportDropVisual(session.UnitPrefab, context.PrepareTransportDropVisual);
            Vector3 anchor = ResolveCanonicalTransportVisualCenterWorld(session);
            session.DropStartPosition = new Vector3(session.DropEndPosition.x, anchor.y, session.DropEndPosition.z);
            if (session.DropVisual != null)
                session.DropVisual.transform.position = session.DropStartPosition;

            session.Rope = AcquireTransportDropRope();
            session.Rope.transform.SetParent(session.TransportTransform, false);
            session.Rope.positionCount = 2;
            session.Rope.widthMultiplier = 0.05f;
            session.Rope.startColor = new Color(0.82f, 0.82f, 0.82f, 0.95f);
            session.Rope.endColor = session.Rope.startColor;
            session.ExpectedRemainingQuantityBeforeSpawn = Mathf.Max(1, remainingQuantity);
            session.Phase = CanonicalDeliveryDropPhase;
            session.PhaseStartedAt = now;
            SetCanonicalTransportDoorOpen01(session, 1f);
        }

        private Vector3 ResolveCanonicalTransportVisualCenterWorld(CanonicalDeliverySession session)
        {
            if (session?.TransportInstance == null)
                return session?.TransportTransform != null ? session.TransportTransform.position : Vector3.zero;

            Renderer[] renderers = GetProductionTransportRenderers(session.TransportInstance);
            bool hasBounds = false;
            Bounds bounds = default;
            for (int index = 0; renderers != null && index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null || !renderer.enabled)
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

            return hasBounds ? bounds.center : session.TransportTransform.position;
        }

        private void SetCanonicalTransportDoorOpen01(CanonicalDeliverySession session, float open01)
        {
            if (session?.DoorTransform == null)
                return;

            Vector3 euler = session.DoorTransform.localEulerAngles;
            euler.x = Mathf.LerpAngle(
                session.DoorClosedLocalEulerX,
                session.DoorClosedLocalEulerX - 92f,
                Mathf.Clamp01(open01));
            session.DoorTransform.localEulerAngles = euler;
        }

        private void PublishCanonicalDeliveryReadModel(Context context)
        {
            BuildingProductionTransportBridgeCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager =
                context.TransportBridgeContext.TryGetEntityManager;
            BuildingSpawnCompositionSystemHelper.TryGetRuntimeBoundaryEntityDelegate tryGetBoundary =
                context.TransportBridgeContext.SpawnContext.TryGetRuntimeBoundaryEntity;
            if (tryGetEntityManager == null || tryGetBoundary == null ||
                !tryGetEntityManager(out EntityManager entityManager) ||
                !tryGetBoundary(entityManager, out Entity boundary) ||
                boundary == Entity.Null || !entityManager.Exists(boundary) ||
                !entityManager.HasComponent<BuildingProductionDeliveryReadModel>(boundary))
            {
                return;
            }

            BuildingProductionDeliveryReadModel current =
                entityManager.GetComponentData<BuildingProductionDeliveryReadModel>(boundary);
            int activeCount = _canonicalDeliverySessions.Count;
            if (current.ActiveCanonicalDeliveryCount == activeCount)
                return;

            current.ActiveCanonicalDeliveryCount = activeCount;
            current.Version++;
            entityManager.SetComponentData(boundary, current);
        }

        private void ClearCanonicalDeliverySessions()
        {
            _canonicalDeliverySessions.Clear();
            _canonicalDeliveryRemovalBuffer.Clear();
        }
    }
}
