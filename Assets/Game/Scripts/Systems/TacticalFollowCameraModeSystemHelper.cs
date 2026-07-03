using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed class TacticalFollowCameraModeSystemHelper
    {
        private const float SingleUnitRadius = 2f;
        private const float MinGroupRadius = 3f;
        private const float UnitDesiredDistance = 12f;
        private const float UnitDesiredDistanceRadiusScale = 2.65f;
        private const float UnitDesiredDistancePadding = 7f;
        private const float GroupDesiredDistancePadding = 10f;
        private const float UnitDesiredHeight = 5f;
        private const float UnitDesiredHeightRadiusScale = 0.9f;
        private const float UnitDesiredHeightPadding = 4f;
        private const float GroupDesiredHeightPadding = 4f;
        private const float BuildingDesiredDistancePadding = 14f;
        private const float BuildingDesiredDistanceRadiusScale = 2.15f;
        private const float BuildingDesiredHeightPadding = 8f;
        private const float BuildingDesiredHeightRadiusScale = 0.75f;
        private const float MinCameraGroundClearance = 3f;
        private const float MinTargetVerticalClearance = 3f;
        private const float MinTargetHorizontalClearance = 6f;
        private const float TargetRadiusHorizontalClearancePadding = 4f;
        private const float RenderSafetyBoundsExtentThreshold = 60f;
        private const float TacticalFollowFieldOfView = 38f;
        private const float TacticalFollowPositionDampingSeconds = 0.32f;
        private const float TacticalFollowRotationDampingSeconds = 0.22f;
        private const float TacticalFollowMaxTransitionSpeed = 80f;
        private const float MissileFollowRadius = 1f;
        private const float MissileFollowDesiredDistance = 10f;
        private const float MissileFollowDesiredHeight = 4f;
        private const float MissileFollowLookAheadDistance = 8f;
        private const float MissileFollowLookAtHeight = 1.25f;
        private const float MissileImpactHoldSeconds = 1.15f;
        private const float MissileImpactRadius = 3f;
        private const float MissileImpactDesiredDistance = 14f;
        private const float MissileImpactDesiredHeight = 6f;
        private static readonly Unity.Mathematics.float3 BuildingForwardHint =
            math.normalize(new Unity.Mathematics.float3(0.55f, 0f, 0.83f));

        private World _singletonQueryWorld;
        private EntityQuery _targetQuery;
        private EntityQuery _poseQuery;
        private EntityQuery _requestQueueQuery;
        private EntityQuery _modeQuery;
        private EntityQuery _uiReadModelQuery;

        public delegate bool TryResolveSelectedBuildingTargetDelegate(out Vector3 worldPosition, out float boundsRadius);

        public readonly struct Context
        {
            public readonly TryResolveSelectedBuildingTargetDelegate TryResolveSelectedBuildingTarget;

            public Context(TryResolveSelectedBuildingTargetDelegate tryResolveSelectedBuildingTarget)
            {
                TryResolveSelectedBuildingTarget = tryResolveSelectedBuildingTarget;
            }
        }

        public bool ProcessPendingRequests(EntityManager em, Camera worldCamera = null)
        {
            return ProcessPendingRequests(em, worldCamera, default);
        }

        public bool ProcessPendingRequests(EntityManager em, Camera worldCamera, Context context)
        {
            if (!TryGetRequestEntity(em, out Entity requestEntity))
            {
                PublishUiReadModel(em, EnsureModeEntity(em), TacticalCommandReasonCode.None, context);
                return false;
            }

            DynamicBuffer<TacticalFollowCameraRequestElement> requests =
                em.GetBuffer<TacticalFollowCameraRequestElement>(requestEntity);
            bool handledAny = false;

            for (int i = 0; i < requests.Length;)
            {
                TacticalFollowCameraRequestElement request = requests[i];
                if (!IsModeRequest(request.Kind))
                {
                    i++;
                    continue;
                }

                requests.RemoveAt(i);
                handledAny = true;
                ProcessRequest(em, request, worldCamera, context);
            }

            if (!handledAny)
                PublishUiReadModel(em, EnsureModeEntity(em), TacticalCommandReasonCode.None, context);

            return handledAny;
        }

        public bool TryReadMode(EntityManager em, out TacticalFollowCameraModeComponent mode)
        {
            Entity entity = EnsureModeEntity(em);
            mode = em.GetComponentData<TacticalFollowCameraModeComponent>(entity);
            return true;
        }

        public bool TryReadUiReadModel(EntityManager em, out TacticalFollowCameraUiReadModelComponent model)
        {
            Entity entity = EnsureUiReadModelEntity(em);
            model = em.GetComponentData<TacticalFollowCameraUiReadModelComponent>(entity);
            return true;
        }

        public bool TryReadTarget(EntityManager em, out TacticalFollowCameraTargetComponent target)
        {
            Entity entity = EnsureTargetEntity(em);
            target = em.GetComponentData<TacticalFollowCameraTargetComponent>(entity);
            return target.Valid != 0;
        }

        public bool TryReadPose(EntityManager em, out TacticalFollowCameraPoseComponent pose)
        {
            Entity entity = EnsurePoseEntity(em);
            pose = em.GetComponentData<TacticalFollowCameraPoseComponent>(entity);
            return pose.Valid != 0;
        }

        public bool RefreshActiveTargetAndPose(EntityManager em)
        {
            return RefreshActiveTargetAndPose(em, default, Time.time);
        }

        public bool RefreshActiveTargetAndPose(EntityManager em, Context context)
        {
            return RefreshActiveTargetAndPose(em, context, Time.time);
        }

        public bool RefreshActiveTargetAndPose(EntityManager em, Context context, float currentTime)
        {
            Entity modeEntity = EnsureModeEntity(em);
            TacticalFollowCameraModeComponent mode =
                em.GetComponentData<TacticalFollowCameraModeComponent>(modeEntity);
            if (mode.Enabled == 0)
            {
                return false;
            }

            if (!TryResolveLockedBaseTarget(em, modeEntity, mode, out TacticalFollowCameraTargetComponent target))
            {
                if (mode.HasTemporaryTarget != 0 &&
                    TryContinueTemporaryTargetWithoutBase(em, modeEntity, mode, context, currentTime))
                {
                    return true;
                }

                ExitFollowMode(em, modeEntity, mode, context, TacticalFollowCameraFeedbackCode.TargetLost);
                return false;
            }

            mode.HasBaseTarget = 1;
            mode.BaseTargetKind = target.TargetKind;
            mode.BaseTargetEntity = target.TargetEntity;
            bool temporaryHoldExpired =
                mode.HasTemporaryTarget != 0 &&
                mode.ReturnHoldUntilTime > 0f &&
                currentTime >= mode.ReturnHoldUntilTime;
            if (TryResolveTemporaryMissileTarget(em, mode, out TacticalFollowCameraTargetComponent temporaryTarget))
            {
                mode.HasTemporaryTarget = 1;
                mode.TemporaryTargetKind = temporaryTarget.TargetKind;
                mode.TemporaryTargetEntity = temporaryTarget.TargetEntity;
                mode.ReturnHoldUntilTime = 0f;
                em.SetComponentData(modeEntity, mode);
                em.SetComponentData(EnsureTargetEntity(em), temporaryTarget);
                em.SetComponentData(EnsurePoseEntity(em), BuildPose(temporaryTarget, TacticalFollowCameraPoseSource.TemporaryMissile));
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
                return true;
            }

            if (!temporaryHoldExpired &&
                mode.HasTemporaryTarget != 0 &&
                TryResolveTemporaryImpactTarget(em, mode, currentTime, ref mode, out temporaryTarget))
            {
                em.SetComponentData(modeEntity, mode);
                em.SetComponentData(EnsureTargetEntity(em), temporaryTarget);
                em.SetComponentData(EnsurePoseEntity(em), BuildPose(temporaryTarget, TacticalFollowCameraPoseSource.TemporaryMissile));
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
                return true;
            }

            if (!temporaryHoldExpired &&
                mode.HasTemporaryTarget != 0 &&
                TryHoldLastTemporaryTarget(em, currentTime, ref mode, out temporaryTarget))
            {
                em.SetComponentData(modeEntity, mode);
                em.SetComponentData(EnsureTargetEntity(em), temporaryTarget);
                em.SetComponentData(EnsurePoseEntity(em), BuildPose(temporaryTarget, TacticalFollowCameraPoseSource.TemporaryMissile));
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
                return true;
            }

            mode.HasTemporaryTarget = 0;
            mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.None;
            mode.TemporaryTargetEntity = Entity.Null;
            mode.ReturnHoldUntilTime = 0f;
            em.SetComponentData(modeEntity, mode);
            em.SetComponentData(EnsureTargetEntity(em), target);
            em.SetComponentData(EnsurePoseEntity(em), BuildPose(target, TacticalFollowCameraPoseSource.BaseTarget));
            PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
            return true;
        }

        public void ClearPose(EntityManager em)
        {
            ClearPoseData(em);
        }

        private void ProcessRequest(EntityManager em, TacticalFollowCameraRequestElement request, Camera worldCamera, Context context)
        {
            Entity modeEntity = EnsureModeEntity(em);
            TacticalFollowCameraModeComponent mode =
                em.GetComponentData<TacticalFollowCameraModeComponent>(modeEntity);

            if (request.Kind == TacticalFollowCameraRequestKind.ExitFollowMode ||
                (request.Kind == TacticalFollowCameraRequestKind.ToggleFollowMode && mode.Enabled != 0))
            {
                ExitFollowMode(em, modeEntity, mode, context, TacticalFollowCameraFeedbackCode.ExitedFollowMode);
                return;
            }

            if (request.Kind != TacticalFollowCameraRequestKind.ToggleFollowMode &&
                request.Kind != TacticalFollowCameraRequestKind.SetBaseTarget &&
                request.Kind != TacticalFollowCameraRequestKind.RefreshBaseTarget)
            {
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
                return;
            }

            if (!TryResolveBaseTarget(em, context, out TacticalFollowCameraTargetComponent target))
            {
                mode.Enabled = 0;
                mode.PanInputLocked = 0;
                mode.HasBaseTarget = 0;
                mode.BaseTargetKind = TacticalFollowCameraTargetKind.None;
                mode.BaseTargetEntity = Entity.Null;
                mode.HasTemporaryTarget = 0;
                mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.None;
                mode.TemporaryTargetEntity = Entity.Null;
                em.SetComponentData(modeEntity, mode);
                ClearBaseTargetEntities(em, modeEntity);
                ClearTarget(em);
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.NoSelection, context);
                return;
            }

            EnterFollowMode(em, modeEntity, mode, target, worldCamera, context);
        }

        private void EnterFollowMode(
            EntityManager em,
            Entity modeEntity,
            TacticalFollowCameraModeComponent mode,
            TacticalFollowCameraTargetComponent target,
            Camera worldCamera,
            Context context)
        {
            if (worldCamera != null)
            {
                mode.RestorePoseValid = 1;
                mode.RestorePosition = worldCamera.transform.position;
                mode.RestoreRotation = worldCamera.transform.rotation;
                mode.RestoreFieldOfView = worldCamera.fieldOfView;
                mode.RestoreOrthographicSize = worldCamera.orthographicSize;
                mode.RestoreOrthographic = worldCamera.orthographic ? (byte)1 : (byte)0;
            }

            mode.Enabled = 1;
            mode.PanInputLocked = 1;
            mode.HasBaseTarget = 1;
            mode.BaseTargetKind = target.TargetKind;
            mode.BaseTargetEntity = target.TargetEntity;
            mode.HasTemporaryTarget = 0;
            mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.None;
            mode.TemporaryTargetEntity = Entity.Null;
            mode.ModeEnteredFrame = Time.frameCount;
            em.SetComponentData(modeEntity, mode);
            CaptureBaseTargetEntities(em, modeEntity, target);
            em.SetComponentData(EnsureTargetEntity(em), target);
            em.SetComponentData(EnsurePoseEntity(em), BuildPose(target, TacticalFollowCameraPoseSource.BaseTarget));
            PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context, TacticalFollowCameraFeedbackCode.EnteredFollowMode);
        }

        private void ExitFollowMode(
            EntityManager em,
            Entity modeEntity,
            TacticalFollowCameraModeComponent mode,
            Context context,
            TacticalFollowCameraFeedbackCode feedbackCode)
        {
            mode.Enabled = 0;
            mode.PanInputLocked = 0;
            mode.HasBaseTarget = 0;
            mode.BaseTargetKind = TacticalFollowCameraTargetKind.None;
            mode.BaseTargetEntity = Entity.Null;
            mode.HasTemporaryTarget = 0;
            mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.None;
            mode.TemporaryTargetEntity = Entity.Null;
            em.SetComponentData(modeEntity, mode);
            ClearBaseTargetEntities(em, modeEntity);
            ClearTarget(em);
            if (mode.RestorePoseValid != 0)
                em.SetComponentData(EnsurePoseEntity(em), BuildRestorePose(mode));
            else
                ClearPoseData(em);
            PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context, feedbackCode);
        }

        private void PublishUiReadModel(
            EntityManager em,
            Entity modeEntity,
            TacticalCommandReasonCode reasonCode,
            Context context,
            TacticalFollowCameraFeedbackCode feedbackCode = TacticalFollowCameraFeedbackCode.None)
        {
            TacticalFollowCameraModeComponent mode =
                em.GetComponentData<TacticalFollowCameraModeComponent>(modeEntity);
            bool hasSelection = TryResolveBaseTarget(em, context, out _);
            Entity readModelEntity = EnsureUiReadModelEntity(em);
            TacticalFollowCameraUiReadModelComponent previous =
                em.GetComponentData<TacticalFollowCameraUiReadModelComponent>(readModelEntity);
            bool hasFeedback = feedbackCode != TacticalFollowCameraFeedbackCode.None;
            em.SetComponentData(readModelEntity, new TacticalFollowCameraUiReadModelComponent
            {
                Visible = 1,
                Enabled = mode.Enabled != 0 || hasSelection ? (byte)1 : (byte)0,
                Selected = mode.Enabled,
                ReasonCode = reasonCode == TacticalCommandReasonCode.None && !hasSelection
                    ? (int)TacticalCommandReasonCode.NoSelection
                    : (int)reasonCode,
                FeedbackCode = hasFeedback ? (int)feedbackCode : previous.FeedbackCode,
                FeedbackSequence = hasFeedback ? previous.FeedbackSequence + 1 : previous.FeedbackSequence
            });
        }

        private static bool TryResolveBaseTarget(
            EntityManager em,
            Context context,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;

            if (TryReadFocusedTarget(em, out target))
                return true;

            using EntityQuery selectedQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<LocalTransform>());
            if (!selectedQuery.IsEmptyIgnoreFilter)
            {
                EntityTypeHandle entityType = em.GetEntityTypeHandle();
                ComponentTypeHandle<LocalTransform> transformType = em.GetComponentTypeHandle<LocalTransform>(true);
                using NativeArray<ArchetypeChunk> chunks = selectedQuery.ToArchetypeChunkArray(Allocator.Temp);
                if (chunks.Length > 0)
                {
                    Entity singleEntity = Entity.Null;
                    LocalTransform singleTransform = default;
                    int followableCount = 0;
                    using NativeList<LocalTransform> followableTransforms = new NativeList<LocalTransform>(Allocator.Temp);
                    for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                    {
                        NativeArray<Entity> selected = chunks[chunkIndex].GetNativeArray(entityType);
                        NativeArray<LocalTransform> transforms = chunks[chunkIndex].GetNativeArray(ref transformType);
                        for (int i = 0; i < selected.Length; i++)
                        {
                            if (!IsFollowableUnitEntity(em, selected[i]))
                                continue;

                            followableCount++;
                            if (followableCount == 1)
                            {
                                singleEntity = selected[i];
                                singleTransform = transforms[i];
                            }
                            else
                            {
                                if (followableCount == 2)
                                    followableTransforms.Add(singleTransform);
                                followableTransforms.Add(transforms[i]);
                            }
                        }
                    }

                    if (followableCount == 1)
                    {
                        target = BuildSingleUnitTarget(em, singleEntity, singleTransform);
                        return true;
                    }

                    if (followableCount > 1)
                    {
                        target = BuildGroupTarget(followableTransforms.AsArray());
                        return true;
                    }
                }
            }

            return TryReadSelectedBuildingTarget(context, out target);
        }

        private bool TryResolveLockedBaseTarget(
            EntityManager em,
            Entity modeEntity,
            TacticalFollowCameraModeComponent mode,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            if (mode.HasBaseTarget == 0)
                return false;

            if (mode.BaseTargetKind == TacticalFollowCameraTargetKind.Unit)
                return TryBuildUnitTarget(em, mode.BaseTargetEntity, out target);

            if (mode.BaseTargetKind == TacticalFollowCameraTargetKind.UnitGroup)
                return TryBuildGroupTargetFromLockedEntities(em, modeEntity, out target);

            if (mode.BaseTargetKind == TacticalFollowCameraTargetKind.Building)
                return TryReadCurrentTarget(em, TacticalFollowCameraTargetKind.Building, out target);

            return false;
        }

        private static bool TryBuildUnitTarget(
            EntityManager em,
            Entity entity,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            if (!IsFollowableUnitEntity(em, entity) || !em.HasComponent<LocalTransform>(entity))
                return false;

            target = BuildSingleUnitTarget(em, entity, em.GetComponentData<LocalTransform>(entity));
            return true;
        }

        private static bool TryBuildGroupTargetFromLockedEntities(
            EntityManager em,
            Entity modeEntity,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            if (!em.HasBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity))
                return false;

            DynamicBuffer<TacticalFollowCameraBaseTargetElement> entities =
                em.GetBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity);
            Entity singleEntity = Entity.Null;
            LocalTransform singleTransform = default;
            int followableCount = 0;
            using NativeList<LocalTransform> transforms = new NativeList<LocalTransform>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i].Entity;
                if (!IsFollowableUnitEntity(em, entity) || !em.HasComponent<LocalTransform>(entity))
                    continue;

                LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
                followableCount++;
                if (followableCount == 1)
                {
                    singleEntity = entity;
                    singleTransform = transform;
                }
                else
                {
                    if (followableCount == 2)
                        transforms.Add(singleTransform);
                    transforms.Add(transform);
                }
            }

            if (followableCount == 1)
            {
                target = BuildSingleUnitTarget(em, singleEntity, singleTransform);
                return true;
            }

            if (followableCount > 1)
            {
                target = BuildGroupTarget(transforms.AsArray());
                return true;
            }

            return false;
        }

        private bool TryReadCurrentTarget(
            EntityManager em,
            TacticalFollowCameraTargetKind expectedKind,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            Entity targetEntity = EnsureTargetEntity(em);
            if (!em.HasComponent<TacticalFollowCameraTargetComponent>(targetEntity))
                return false;

            target = em.GetComponentData<TacticalFollowCameraTargetComponent>(targetEntity);
            return target.Valid != 0 && target.TargetKind == expectedKind;
        }

        private static void CaptureBaseTargetEntities(
            EntityManager em,
            Entity modeEntity,
            TacticalFollowCameraTargetComponent target)
        {
            DynamicBuffer<TacticalFollowCameraBaseTargetElement> buffer = EnsureBaseTargetEntityBuffer(em, modeEntity);
            buffer.Clear();
            if (target.TargetKind == TacticalFollowCameraTargetKind.Unit && target.TargetEntity != Entity.Null)
            {
                buffer.Add(new TacticalFollowCameraBaseTargetElement { Entity = target.TargetEntity });
                return;
            }

            if (target.TargetKind != TacticalFollowCameraTargetKind.UnitGroup)
                return;

            using EntityQuery selectedQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<LocalTransform>());
            if (selectedQuery.IsEmptyIgnoreFilter)
                return;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> selected = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < selected.Length; i++)
                {
                    if (IsFollowableUnitEntity(em, selected[i]))
                        buffer.Add(new TacticalFollowCameraBaseTargetElement { Entity = selected[i] });
                }
            }
        }

        private static DynamicBuffer<TacticalFollowCameraBaseTargetElement> EnsureBaseTargetEntityBuffer(
            EntityManager em,
            Entity modeEntity)
        {
            if (!em.HasBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity))
                em.AddBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity);

            return em.GetBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity);
        }

        private static void ClearBaseTargetEntities(EntityManager em, Entity modeEntity)
        {
            if (em.HasBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity))
                em.GetBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity).Clear();
        }

        private static bool IsFollowableUnitEntity(EntityManager em, Entity entity)
        {
            return entity != Entity.Null &&
                   em.Exists(entity) &&
                   !em.HasComponent<Disabled>(entity) &&
                   !em.HasComponent<UnitTransportPassenger>(entity) &&
                   !em.HasComponent<UnitTransportCargoPassenger>(entity);
        }

        private static bool TryReadFocusedTarget(EntityManager em, out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            using EntityQuery focusedQuery = em.CreateEntityQuery(ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
            if (focusedQuery.IsEmptyIgnoreFilter)
                return false;

            FocusedUnitUiReadModelComponent model =
                em.GetComponentData<FocusedUnitUiReadModelComponent>(focusedQuery.GetSingletonEntity());
            if (model.HasFocusedUnit == 0 ||
                model.OwnedByPlayer == 0 ||
                model.FocusedUnit == Entity.Null ||
                !IsFollowableUnitEntity(em, model.FocusedUnit))
            {
                return false;
            }

            if (em.HasComponent<LocalTransform>(model.FocusedUnit))
            {
                target = BuildSingleUnitTarget(em, model.FocusedUnit, em.GetComponentData<LocalTransform>(model.FocusedUnit));
                return true;
            }

            if (model.HasPortraitPose != 0)
            {
                target = BuildTarget(
                    TacticalFollowCameraTargetKind.Unit,
                    model.FocusedUnit,
                    model.PortraitWorldPosition,
                    NormalizeOrFallback(model.PortraitForward),
                    SingleUnitRadius);
                return true;
            }

            if (model.HasWorldPosition != 0)
            {
                target = BuildTarget(
                    TacticalFollowCameraTargetKind.Unit,
                    model.FocusedUnit,
                    model.WorldPosition,
                    new Unity.Mathematics.float3(0f, 0f, 1f),
                    SingleUnitRadius);
                return true;
            }

            return false;
        }

        private static TacticalFollowCameraTargetComponent BuildSingleUnitTarget(EntityManager em, Entity entity, LocalTransform transform)
        {
            float radius = ResolveSingleUnitFootprintRadius(em, entity, transform);
            float3 center = transform.Position;
            if (em.HasComponent<UnitSelectionHitbox>(entity) &&
                TryResolveSelectionHitboxFrame(transform, em.GetComponentData<UnitSelectionHitbox>(entity), out FollowBoundsFrame selectionFrame))
            {
                radius = math.max(radius, selectionFrame.HorizontalRadius);
                center = new float3(selectionFrame.Center.x, transform.Position.y, selectionFrame.Center.z);
            }
            else if (TryResolveRenderBoundsFrame(em, entity, out FollowBoundsFrame renderFrame))
            {
                radius = math.max(radius, renderFrame.HorizontalRadius);
                center = new float3(renderFrame.Center.x, transform.Position.y, renderFrame.Center.z);
            }

            TacticalFollowCameraTargetComponent target = BuildTarget(
                TacticalFollowCameraTargetKind.Unit,
                entity,
                center,
                math.forward(transform.Rotation),
                radius);
            ApplySingleUnitFraming(ref target);
            return target;
        }

        private static TacticalFollowCameraTargetComponent BuildGroupTarget(NativeArray<LocalTransform> transforms)
        {
            Unity.Mathematics.float3 sum = default;
            Unity.Mathematics.float3 forwardSum = default;
            for (int i = 0; i < transforms.Length; i++)
            {
                sum += transforms[i].Position;
                forwardSum += math.forward(transforms[i].Rotation);
            }

            Unity.Mathematics.float3 center = sum / transforms.Length;
            float radiusSq = 0f;
            for (int i = 0; i < transforms.Length; i++)
                radiusSq = math.max(radiusSq, math.distancesq(center, transforms[i].Position));

            float radius = math.max(MinGroupRadius, math.sqrt(radiusSq) + SingleUnitRadius);
            TacticalFollowCameraTargetComponent target = BuildTarget(
                TacticalFollowCameraTargetKind.UnitGroup,
                Entity.Null,
                center,
                NormalizeOrFallback(forwardSum),
                radius);
            target.DesiredDistance = radius + GroupDesiredDistancePadding;
            target.DesiredHeight = math.max(UnitDesiredHeight, radius + GroupDesiredHeightPadding);
            return target;
        }

        private static bool TryReadSelectedBuildingTarget(
            Context context,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            if (context.TryResolveSelectedBuildingTarget == null ||
                !context.TryResolveSelectedBuildingTarget(out Vector3 worldPosition, out float boundsRadius))
            {
                return false;
            }

            target = BuildTarget(
                TacticalFollowCameraTargetKind.Building,
                Entity.Null,
                new Unity.Mathematics.float3(worldPosition.x, worldPosition.y, worldPosition.z),
                BuildingForwardHint,
                math.max(1f, boundsRadius));
            target.LookAt = target.Center + new float3(0f, math.clamp(target.BoundsRadius * 0.35f, 3f, 28f), 0f);
            target.DesiredDistance = math.max(
                UnitDesiredDistance,
                math.max(
                    target.BoundsRadius + BuildingDesiredDistancePadding,
                    (target.BoundsRadius * BuildingDesiredDistanceRadiusScale) + BuildingDesiredDistancePadding));
            target.DesiredHeight = math.max(
                UnitDesiredHeight,
                math.max(
                    target.BoundsRadius + BuildingDesiredHeightPadding,
                    (target.BoundsRadius * BuildingDesiredHeightRadiusScale) + BuildingDesiredHeightPadding));
            return true;
        }

        private static void ApplySingleUnitFraming(ref TacticalFollowCameraTargetComponent target)
        {
            target.LookAt = target.Center + new float3(0f, math.clamp(target.BoundsRadius * 0.45f, 1f, 18f), 0f);
            target.DesiredDistance = math.max(
                UnitDesiredDistance,
                (target.BoundsRadius * UnitDesiredDistanceRadiusScale) + UnitDesiredDistancePadding);
            target.DesiredHeight = math.max(
                UnitDesiredHeight,
                (target.BoundsRadius * UnitDesiredHeightRadiusScale) + UnitDesiredHeightPadding);
        }

        private static float ResolveSingleUnitFootprintRadius(EntityManager em, Entity entity, LocalTransform transform)
        {
            float radius = math.max(SingleUnitRadius, math.abs(transform.Scale) * SingleUnitRadius);
            if (em.HasComponent<UnitFootprint>(entity))
            {
                UnitFootprint footprint = em.GetComponentData<UnitFootprint>(entity);
                int2 size = UnitFootprintUtility.ClampSize(footprint.Size);
                float cellSize = ResolveGridCellSize(em);
                float footprintRadius = math.length(new float2(size.x, size.y)) * 0.5f * cellSize;
                radius = math.max(radius, footprintRadius);
            }

            return math.max(SingleUnitRadius, radius);
        }

        private static float ResolveGridCellSize(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (query.IsEmptyIgnoreFilter)
                return 1f;

            GridConfig grid = em.GetComponentData<GridConfig>(query.GetSingletonEntity());
            return math.max(0.1f, grid.CellSize);
        }

        private static bool TryResolveSelectionHitboxFrame(
            LocalTransform transform,
            UnitSelectionHitbox hitbox,
            out FollowBoundsFrame frame)
        {
            float scale = math.abs(transform.Scale);
            float3 extents = math.abs(hitbox.Extents) * scale;
            if (!math.all(math.isfinite(extents)) || math.cmax(extents) <= 0.01f)
            {
                frame = default;
                return false;
            }

            float3 center = transform.Position + math.rotate(transform.Rotation, hitbox.Center * scale);
            frame = new FollowBoundsFrame(
                center,
                math.max(math.length(new float2(extents.x, extents.z)), math.max(extents.x, extents.z)));
            return frame.HorizontalRadius > 0.01f;
        }

        private static bool TryResolveRenderBoundsFrame(EntityManager em, Entity root, out FollowBoundsFrame frame)
        {
            var accumulator = new RenderBoundsAccumulator();
            TryAccumulateRenderBounds(em, root, 0, ref accumulator);
            if (!accumulator.HasBounds)
            {
                frame = default;
                return false;
            }

            frame = accumulator.ToFrame();
            return frame.HorizontalRadius > 0.01f;
        }

        private static void TryAccumulateRenderBounds(
            EntityManager em,
            Entity entity,
            int depth,
            ref RenderBoundsAccumulator accumulator)
        {
            if (entity == Entity.Null || !em.Exists(entity) || depth > 12)
                return;

            if (TryReadWorldAabb(em, entity, out AABB bounds))
                accumulator.Encapsulate(bounds);

            if (!em.HasBuffer<Child>(entity))
                return;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
                TryAccumulateRenderBounds(em, children[i].Value, depth + 1, ref accumulator);
        }

        private static bool TryReadWorldAabb(EntityManager em, Entity entity, out AABB bounds)
        {
            if (em.HasComponent<WorldRenderBounds>(entity))
            {
                bounds = em.GetComponentData<WorldRenderBounds>(entity).Value;
                return IsUsableAabb(bounds);
            }

            if (em.HasComponent<RenderBounds>(entity) && em.HasComponent<LocalToWorld>(entity))
            {
                bounds = AABB.Transform(
                    em.GetComponentData<LocalToWorld>(entity).Value,
                    em.GetComponentData<RenderBounds>(entity).Value);
                return IsUsableAabb(bounds);
            }

            bounds = default;
            return false;
        }

        private static bool IsUsableAabb(AABB bounds)
        {
            return math.all(math.isfinite(bounds.Center)) &&
                   math.all(math.isfinite(bounds.Extents)) &&
                   math.cmax(bounds.Extents) > 0.01f &&
                   !IsRenderSafetyPaddedAabb(bounds);
        }

        private static bool IsRenderSafetyPaddedAabb(AABB bounds)
        {
            float3 extents = math.abs(bounds.Extents);
            return extents.x >= RenderSafetyBoundsExtentThreshold &&
                   extents.y >= RenderSafetyBoundsExtentThreshold &&
                   extents.z >= RenderSafetyBoundsExtentThreshold;
        }

        private readonly struct FollowBoundsFrame
        {
            public readonly float3 Center;
            public readonly float HorizontalRadius;

            public FollowBoundsFrame(float3 center, float horizontalRadius)
            {
                Center = center;
                HorizontalRadius = horizontalRadius;
            }
        }

        private struct RenderBoundsAccumulator
        {
            private float3 _min;
            private float3 _max;

            public bool HasBounds { get; private set; }

            public void Encapsulate(AABB bounds)
            {
                float3 min = bounds.Center - bounds.Extents;
                float3 max = bounds.Center + bounds.Extents;
                if (!HasBounds)
                {
                    _min = min;
                    _max = max;
                    HasBounds = true;
                    return;
                }

                _min = math.min(_min, min);
                _max = math.max(_max, max);
            }

            public FollowBoundsFrame ToFrame()
            {
                float3 center = (_min + _max) * 0.5f;
                float3 extents = (_max - _min) * 0.5f;
                return new FollowBoundsFrame(
                    center,
                    math.max(math.length(new float2(extents.x, extents.z)), math.max(extents.x, extents.z)));
            }
        }

        private static TacticalFollowCameraTargetComponent BuildTarget(
            TacticalFollowCameraTargetKind kind,
            Entity entity,
            Unity.Mathematics.float3 center,
            Unity.Mathematics.float3 forward,
            float radius)
        {
            forward = NormalizeOrFallback(forward);
            float clampedRadius = math.max(0.25f, radius);
            return new TacticalFollowCameraTargetComponent
            {
                Valid = 1,
                TargetKind = kind,
                TargetEntity = entity,
                Center = center,
                LookAt = center + new Unity.Mathematics.float3(0f, math.max(1f, clampedRadius * 0.5f), 0f),
                ForwardHint = forward,
                BoundsRadius = clampedRadius,
                DesiredDistance = UnitDesiredDistance,
                DesiredHeight = UnitDesiredHeight
            };
        }

        private bool TryContinueTemporaryTargetWithoutBase(
            EntityManager em,
            Entity modeEntity,
            TacticalFollowCameraModeComponent mode,
            Context context,
            float currentTime)
        {
            bool temporaryHoldExpired =
                mode.ReturnHoldUntilTime > 0f &&
                currentTime >= mode.ReturnHoldUntilTime;

            if (mode.TemporaryTargetEntity != Entity.Null &&
                TryBuildTemporaryMissileTarget(
                    em,
                    mode,
                    mode.TemporaryTargetEntity,
                    out TacticalFollowCameraTargetComponent temporaryTarget,
                    true))
            {
                mode.ReturnHoldUntilTime = 0f;
                em.SetComponentData(modeEntity, mode);
                em.SetComponentData(EnsureTargetEntity(em), temporaryTarget);
                em.SetComponentData(EnsurePoseEntity(em), BuildPose(temporaryTarget, TacticalFollowCameraPoseSource.TemporaryMissile));
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
                return true;
            }

            if (!temporaryHoldExpired &&
                TryResolveTemporaryImpactTarget(em, mode, currentTime, ref mode, out temporaryTarget))
            {
                em.SetComponentData(modeEntity, mode);
                em.SetComponentData(EnsureTargetEntity(em), temporaryTarget);
                em.SetComponentData(EnsurePoseEntity(em), BuildPose(temporaryTarget, TacticalFollowCameraPoseSource.TemporaryMissile));
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
                return true;
            }

            if (!temporaryHoldExpired &&
                TryHoldLastTemporaryTarget(em, currentTime, ref mode, out temporaryTarget))
            {
                em.SetComponentData(modeEntity, mode);
                em.SetComponentData(EnsureTargetEntity(em), temporaryTarget);
                em.SetComponentData(EnsurePoseEntity(em), BuildPose(temporaryTarget, TacticalFollowCameraPoseSource.TemporaryMissile));
                PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
                return true;
            }

            return false;
        }

        private static bool TryResolveTemporaryMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            if (mode.Enabled == 0 ||
                mode.HasBaseTarget == 0)
            {
                return false;
            }

            if (mode.HasTemporaryTarget != 0 &&
                mode.TemporaryTargetEntity != Entity.Null &&
                TryBuildTemporaryMissileTarget(em, mode, mode.TemporaryTargetEntity, out target, true))
            {
                return true;
            }

            if (mode.HasTemporaryTarget != 0)
                return false;

            return TryFindEligibleGroundMissileTarget(em, mode, out target) ||
                   TryFindEligibleAirMissileTarget(em, mode, out target);
        }

        private static bool TryFindEligibleGroundMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<GroundMissileProjectileComponent>(),
                ComponentType.ReadOnly<LocalTransform>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    if (TryBuildGroundMissileTarget(em, mode, entities[i], out target))
                        return true;
                }
            }

            return false;
        }

        private static bool TryFindEligibleAirMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<AirMissileProjectileComponent>(),
                ComponentType.ReadOnly<LocalTransform>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    if (TryBuildAirMissileTarget(em, mode, entities[i], out target))
                        return true;
                }
            }

            return false;
        }

        private static bool TryBuildTemporaryMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            Entity entity,
            out TacticalFollowCameraTargetComponent target)
        {
            return TryBuildTemporaryMissileTarget(em, mode, entity, out target, false);
        }

        private static bool TryBuildTemporaryMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            Entity entity,
            out TacticalFollowCameraTargetComponent target,
            bool allowAlreadyAdoptedSource)
        {
            return TryBuildGroundMissileTarget(em, mode, entity, out target, allowAlreadyAdoptedSource) ||
                   TryBuildAirMissileTarget(em, mode, entity, out target, allowAlreadyAdoptedSource);
        }

        private static bool TryBuildGroundMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            Entity entity,
            out TacticalFollowCameraTargetComponent target)
        {
            return TryBuildGroundMissileTarget(em, mode, entity, out target, false);
        }

        private static bool TryBuildGroundMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            Entity entity,
            out TacticalFollowCameraTargetComponent target,
            bool allowAlreadyAdoptedSource)
        {
            target = default;
            if (entity == Entity.Null ||
                !em.Exists(entity) ||
                !em.HasComponent<GroundMissileProjectileComponent>(entity) ||
                !em.HasComponent<LocalTransform>(entity))
            {
                return false;
            }

            GroundMissileProjectileComponent projectile = em.GetComponentData<GroundMissileProjectileComponent>(entity);
            bool alreadyAdopted = allowAlreadyAdoptedSource && mode.TemporaryTargetEntity == entity;
            if (!alreadyAdopted && !IsFollowedSource(em, mode, projectile.Source))
                return false;

            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            float3 forward = NormalizeOrFallback(projectile.TargetPosition - transform.Position);
            target = BuildMissileTarget(TacticalFollowCameraTargetKind.GroundMissile, entity, transform.Position, forward);
            return true;
        }

        private static bool TryBuildAirMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            Entity entity,
            out TacticalFollowCameraTargetComponent target)
        {
            return TryBuildAirMissileTarget(em, mode, entity, out target, false);
        }

        private static bool TryBuildAirMissileTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            Entity entity,
            out TacticalFollowCameraTargetComponent target,
            bool allowAlreadyAdoptedSource)
        {
            target = default;
            if (entity == Entity.Null ||
                !em.Exists(entity) ||
                !em.HasComponent<AirMissileProjectileComponent>(entity) ||
                !em.HasComponent<LocalTransform>(entity))
            {
                return false;
            }

            AirMissileProjectileComponent projectile = em.GetComponentData<AirMissileProjectileComponent>(entity);
            bool alreadyAdopted = allowAlreadyAdoptedSource && mode.TemporaryTargetEntity == entity;
            if (!alreadyAdopted && !IsFollowedSource(em, mode, projectile.Source))
                return false;

            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            float3 forward = math.lengthsq(projectile.Velocity) > 0.0001f
                ? projectile.Velocity
                : math.forward(transform.Rotation);
            target = BuildMissileTarget(TacticalFollowCameraTargetKind.AirMissile, entity, transform.Position, forward);
            return true;
        }

        private static TacticalFollowCameraTargetComponent BuildMissileTarget(
            TacticalFollowCameraTargetKind kind,
            Entity entity,
            float3 center,
            float3 forward)
        {
            forward = NormalizeOrFallback(forward);
            TacticalFollowCameraTargetComponent target = new TacticalFollowCameraTargetComponent
            {
                Valid = 1,
                TargetKind = kind,
                TargetEntity = entity,
                Center = center,
                LookAt = center + forward * MissileFollowLookAheadDistance + new float3(0f, MissileFollowLookAtHeight, 0f),
                ForwardHint = forward,
                BoundsRadius = MissileFollowRadius,
            };
            target.DesiredDistance = MissileFollowDesiredDistance;
            target.DesiredHeight = MissileFollowDesiredHeight;
            return target;
        }

        private bool TryResolveTemporaryImpactTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent originalMode,
            float currentTime,
            ref TacticalFollowCameraModeComponent mode,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            if (originalMode.TemporaryTargetEntity == Entity.Null ||
                !em.Exists(originalMode.TemporaryTargetEntity))
            {
                return false;
            }

            if (em.HasComponent<GroundMissileImpactRequestComponent>(originalMode.TemporaryTargetEntity))
            {
                GroundMissileImpactRequestComponent impact =
                    em.GetComponentData<GroundMissileImpactRequestComponent>(originalMode.TemporaryTargetEntity);

                target = BuildMissileImpactTarget(
                    TacticalFollowCameraTargetKind.GroundMissile,
                    originalMode.TemporaryTargetEntity,
                    impact.Position,
                    TryReadCurrentTemporaryForward(em, originalMode));
                EnsureImpactHold(currentTime, ref mode);
                return true;
            }

            if (em.HasComponent<AirMissileImpactRequestComponent>(originalMode.TemporaryTargetEntity))
            {
                AirMissileImpactRequestComponent impact =
                    em.GetComponentData<AirMissileImpactRequestComponent>(originalMode.TemporaryTargetEntity);

                target = BuildMissileImpactTarget(
                    TacticalFollowCameraTargetKind.AirMissile,
                    originalMode.TemporaryTargetEntity,
                    impact.Position,
                    TryReadCurrentTemporaryForward(em, originalMode));
                EnsureImpactHold(currentTime, ref mode);
                return true;
            }

            return false;
        }

        private bool TryHoldLastTemporaryTarget(
            EntityManager em,
            float currentTime,
            ref TacticalFollowCameraModeComponent mode,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            if (!TryReadCurrentTemporaryTarget(em, mode, out target))
                return false;

            EnsureImpactHold(currentTime, ref mode);
            return true;
        }

        private bool TryReadCurrentTemporaryTarget(
            EntityManager em,
            TacticalFollowCameraModeComponent mode,
            out TacticalFollowCameraTargetComponent target)
        {
            target = default;
            Entity targetEntity = EnsureTargetEntity(em);
            target = em.GetComponentData<TacticalFollowCameraTargetComponent>(targetEntity);
            if (target.Valid == 0 ||
                target.TargetEntity != mode.TemporaryTargetEntity)
            {
                return false;
            }

            return target.TargetKind == TacticalFollowCameraTargetKind.GroundMissile ||
                   target.TargetKind == TacticalFollowCameraTargetKind.AirMissile;
        }

        private float3 TryReadCurrentTemporaryForward(EntityManager em, TacticalFollowCameraModeComponent mode)
        {
            return TryReadCurrentTemporaryTarget(em, mode, out TacticalFollowCameraTargetComponent target)
                ? target.ForwardHint
                : new float3(0f, 0f, 1f);
        }

        private static void EnsureImpactHold(float currentTime, ref TacticalFollowCameraModeComponent mode)
        {
            if (mode.ReturnHoldUntilTime <= 0f)
                mode.ReturnHoldUntilTime = currentTime + MissileImpactHoldSeconds;
        }

        private static TacticalFollowCameraTargetComponent BuildMissileImpactTarget(
            TacticalFollowCameraTargetKind kind,
            Entity entity,
            float3 center,
            float3 forward)
        {
            TacticalFollowCameraTargetComponent target = BuildTarget(
                kind,
                entity,
                center,
                NormalizeOrFallback(forward),
                MissileImpactRadius);
            target.DesiredDistance = MissileImpactDesiredDistance;
            target.DesiredHeight = MissileImpactDesiredHeight;
            return target;
        }

        private static bool IsFollowedSource(EntityManager em, TacticalFollowCameraModeComponent mode, Entity source)
        {
            if (source == Entity.Null ||
                !em.Exists(source))
            {
                return false;
            }

            if (mode.BaseTargetEntity != Entity.Null &&
                mode.BaseTargetEntity == source)
            {
                return true;
            }

            return mode.BaseTargetKind == TacticalFollowCameraTargetKind.UnitGroup &&
                   em.HasComponent<SelectedUnitTag>(source);
        }

        private static TacticalFollowCameraPoseComponent BuildPose(
            TacticalFollowCameraTargetComponent target,
            TacticalFollowCameraPoseSource source)
        {
            Unity.Mathematics.float3 forward = NormalizeOrFallback(target.ForwardHint);
            Unity.Mathematics.float3 desiredPosition =
                target.LookAt -
                (forward * math.max(2f, target.DesiredDistance)) +
                new Unity.Mathematics.float3(0f, math.max(2f, target.DesiredHeight), 0f);
            desiredPosition = ClampDesiredPosition(target, desiredPosition, forward);

            return new TacticalFollowCameraPoseComponent
            {
                Valid = 1,
                Source = source,
                DesiredPosition = desiredPosition,
                DesiredRotation = quaternion.LookRotationSafe(
                    math.normalizesafe(target.LookAt - desiredPosition, new Unity.Mathematics.float3(0f, 0f, 1f)),
                    new Unity.Mathematics.float3(0f, 1f, 0f)),
                LookAt = target.LookAt,
                FieldOfView = TacticalFollowFieldOfView,
                OrthographicSize = 0f,
                Orthographic = 0,
                PositionDampingSeconds = TacticalFollowPositionDampingSeconds,
                RotationDampingSeconds = TacticalFollowRotationDampingSeconds,
                MaxTransitionSpeed = TacticalFollowMaxTransitionSpeed
            };
        }

        private static Unity.Mathematics.float3 ClampDesiredPosition(
            TacticalFollowCameraTargetComponent target,
            Unity.Mathematics.float3 desiredPosition,
            Unity.Mathematics.float3 fallbackForward)
        {
            float minTargetClearance = math.max(MinTargetVerticalClearance, target.BoundsRadius * 0.65f);
            float minHeight = math.max(
                MinCameraGroundClearance,
                target.Center.y + minTargetClearance);
            desiredPosition.y = math.max(desiredPosition.y, minHeight);

            Unity.Mathematics.float3 flatOffset = desiredPosition - target.LookAt;
            flatOffset.y = 0f;
            float minHorizontalDistance = math.max(
                MinTargetHorizontalClearance,
                target.BoundsRadius + TargetRadiusHorizontalClearancePadding);
            float flatDistance = math.length(flatOffset);
            if (flatDistance < minHorizontalDistance)
            {
                Unity.Mathematics.float3 backward = NormalizeOrFallback(flatOffset);
                if (math.lengthsq(flatOffset) <= 0.0001f)
                    backward = -NormalizeOrFallback(fallbackForward);

                desiredPosition.x = target.LookAt.x + backward.x * minHorizontalDistance;
                desiredPosition.z = target.LookAt.z + backward.z * minHorizontalDistance;
            }

            return desiredPosition;
        }

        private static TacticalFollowCameraPoseComponent BuildRestorePose(TacticalFollowCameraModeComponent mode)
        {
            Unity.Mathematics.float3 forward = math.forward(mode.RestoreRotation);
            if (math.lengthsq(forward) <= 0.0001f)
                forward = new Unity.Mathematics.float3(0f, 0f, 1f);

            return new TacticalFollowCameraPoseComponent
            {
                Valid = 1,
                Source = TacticalFollowCameraPoseSource.RestoreDefault,
                DesiredPosition = mode.RestorePosition,
                DesiredRotation = mode.RestoreRotation,
                LookAt = mode.RestorePosition + math.normalizesafe(forward, new Unity.Mathematics.float3(0f, 0f, 1f)) * 20f,
                FieldOfView = math.max(1f, mode.RestoreFieldOfView),
                OrthographicSize = math.max(0f, mode.RestoreOrthographicSize),
                Orthographic = mode.RestoreOrthographic,
                PositionDampingSeconds = TacticalFollowPositionDampingSeconds,
                RotationDampingSeconds = TacticalFollowRotationDampingSeconds,
                MaxTransitionSpeed = TacticalFollowMaxTransitionSpeed
            };
        }

        private static Unity.Mathematics.float3 NormalizeOrFallback(Unity.Mathematics.float3 forward)
        {
            float lengthSq = math.lengthsq(forward);
            if (lengthSq <= 0.0001f)
                return new Unity.Mathematics.float3(0f, 0f, 1f);

            Unity.Mathematics.float3 normalized = forward * math.rsqrt(lengthSq);
            normalized.y = 0f;
            lengthSq = math.lengthsq(normalized);
            return lengthSq <= 0.0001f
                ? new Unity.Mathematics.float3(0f, 0f, 1f)
                : normalized * math.rsqrt(lengthSq);
        }

        private void ClearTarget(EntityManager em)
        {
            em.SetComponentData(EnsureTargetEntity(em), default(TacticalFollowCameraTargetComponent));
        }

        private void ClearPoseData(EntityManager em)
        {
            em.SetComponentData(EnsurePoseEntity(em), default(TacticalFollowCameraPoseComponent));
        }

        private Entity EnsureTargetEntity(EntityManager em)
        {
            EnsureSingletonQueries(em);
            if (!_targetQuery.IsEmptyIgnoreFilter)
                return _targetQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(TacticalFollowCameraTargetComponent));
            em.SetName(entity, "TacticalFollowCameraTarget");
            return entity;
        }

        private Entity EnsurePoseEntity(EntityManager em)
        {
            EnsureSingletonQueries(em);
            if (!_poseQuery.IsEmptyIgnoreFilter)
                return _poseQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(TacticalFollowCameraPoseComponent));
            em.SetName(entity, "TacticalFollowCameraPose");
            return entity;
        }

        private bool TryGetRequestEntity(EntityManager em, out Entity entity)
        {
            EnsureSingletonQueries(em);
            if (_requestQueueQuery.IsEmptyIgnoreFilter)
            {
                entity = Entity.Null;
                return false;
            }

            entity = _requestQueueQuery.GetSingletonEntity();
            return true;
        }

        private Entity EnsureModeEntity(EntityManager em)
        {
            EnsureSingletonQueries(em);
            if (!_modeQuery.IsEmptyIgnoreFilter)
            {
                Entity existing = _modeQuery.GetSingletonEntity();
                EnsureBaseTargetEntityBuffer(em, existing);
                return existing;
            }

            Entity entity = em.CreateEntity(
                typeof(TacticalFollowCameraModeComponent),
                typeof(TacticalFollowCameraBaseTargetElement));
            em.SetName(entity, "TacticalFollowCameraMode");
            return entity;
        }

        private Entity EnsureUiReadModelEntity(EntityManager em)
        {
            EnsureSingletonQueries(em);
            if (!_uiReadModelQuery.IsEmptyIgnoreFilter)
                return _uiReadModelQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(TacticalFollowCameraUiReadModelComponent));
            em.SetName(entity, "TacticalFollowCameraUiReadModel");
            em.SetComponentData(entity, new TacticalFollowCameraUiReadModelComponent
            {
                Visible = 1,
                ReasonCode = (int)TacticalCommandReasonCode.NoSelection
            });
            return entity;
        }

        private void EnsureSingletonQueries(EntityManager em)
        {
            World world = em.World;
            if (_singletonQueryWorld == world && world != null && world.IsCreated)
                return;

            _singletonQueryWorld = world;
            _targetQuery = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraTargetComponent>());
            _poseQuery = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraPoseComponent>());
            _requestQueueQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<TacticalFollowCameraRequestQueueComponent>(),
                ComponentType.ReadWrite<TacticalFollowCameraRequestElement>());
            _modeQuery = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraModeComponent>());
            _uiReadModelQuery = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraUiReadModelComponent>());
        }

        private static bool IsModeRequest(TacticalFollowCameraRequestKind kind)
        {
            return kind == TacticalFollowCameraRequestKind.ToggleFollowMode ||
                   kind == TacticalFollowCameraRequestKind.ExitFollowMode ||
                   kind == TacticalFollowCameraRequestKind.SetBaseTarget ||
                   kind == TacticalFollowCameraRequestKind.RefreshBaseTarget;
        }
    }
}
