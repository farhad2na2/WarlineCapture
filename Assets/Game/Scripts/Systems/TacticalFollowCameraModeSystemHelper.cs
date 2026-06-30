using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class TacticalFollowCameraModeSystemHelper
{
    private const float SingleUnitRadius = 2f;
    private const float MinGroupRadius = 3f;
    private const float UnitDesiredDistance = 12f;
    private const float GroupDesiredDistancePadding = 10f;
    private const float UnitDesiredHeight = 5f;
    private const float GroupDesiredHeightPadding = 4f;
    private const float BuildingDesiredDistancePadding = 14f;
    private const float BuildingDesiredHeightPadding = 8f;
    private const float MinCameraGroundClearance = 3f;
    private const float MinTargetVerticalClearance = 3f;
    private const float MinTargetHorizontalClearance = 6f;
    private const float TargetRadiusHorizontalClearancePadding = 4f;
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

        if (!TryResolveBaseTarget(em, context, out TacticalFollowCameraTargetComponent target))
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

    private static void ProcessRequest(EntityManager em, TacticalFollowCameraRequestElement request, Camera worldCamera, Context context)
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
            ClearTarget(em);
            PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.NoSelection, context);
            return;
        }

        EnterFollowMode(em, modeEntity, mode, target, worldCamera, context);
    }

    private static void EnterFollowMode(
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
        em.SetComponentData(EnsureTargetEntity(em), target);
        em.SetComponentData(EnsurePoseEntity(em), BuildPose(target, TacticalFollowCameraPoseSource.BaseTarget));
        PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context, TacticalFollowCameraFeedbackCode.EnteredFollowMode);
    }

    private static void ExitFollowMode(
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
        ClearTarget(em);
        if (mode.RestorePoseValid != 0)
            em.SetComponentData(EnsurePoseEntity(em), BuildRestorePose(mode));
        else
            ClearPoseData(em);
        PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context, feedbackCode);
    }

    private static void PublishUiReadModel(
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
            Enabled = hasSelection ? (byte)1 : (byte)0,
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
            using NativeArray<Entity> selected = selectedQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<LocalTransform> transforms = selectedQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            if (selected.Length > 0)
            {
                Entity singleEntity = Entity.Null;
                LocalTransform singleTransform = default;
                int followableCount = 0;
                using NativeList<LocalTransform> followableTransforms = new NativeList<LocalTransform>(Allocator.Temp);
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

                if (followableCount == 1)
                {
                    target = BuildSingleUnitTarget(singleEntity, singleTransform);
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
            target = BuildSingleUnitTarget(model.FocusedUnit, em.GetComponentData<LocalTransform>(model.FocusedUnit));
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

    private static TacticalFollowCameraTargetComponent BuildSingleUnitTarget(Entity entity, LocalTransform transform)
    {
        return BuildTarget(
            TacticalFollowCameraTargetKind.Unit,
            entity,
            transform.Position,
            math.forward(transform.Rotation),
            SingleUnitRadius);
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
        target.DesiredDistance = math.max(UnitDesiredDistance, target.BoundsRadius + BuildingDesiredDistancePadding);
        target.DesiredHeight = math.max(UnitDesiredHeight, target.BoundsRadius + BuildingDesiredHeightPadding);
        return true;
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

    private static bool TryContinueTemporaryTargetWithoutBase(
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

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (TryBuildGroundMissileTarget(em, mode, entities[i], out target))
                return true;
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

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (TryBuildAirMissileTarget(em, mode, entities[i], out target))
                return true;
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

    private static bool TryResolveTemporaryImpactTarget(
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

    private static bool TryHoldLastTemporaryTarget(
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

    private static bool TryReadCurrentTemporaryTarget(
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

    private static float3 TryReadCurrentTemporaryForward(EntityManager em, TacticalFollowCameraModeComponent mode)
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

    private static void ClearTarget(EntityManager em)
    {
        em.SetComponentData(EnsureTargetEntity(em), default(TacticalFollowCameraTargetComponent));
    }

    private static void ClearPoseData(EntityManager em)
    {
        em.SetComponentData(EnsurePoseEntity(em), default(TacticalFollowCameraPoseComponent));
    }

    private static Entity EnsureTargetEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraTargetComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(TacticalFollowCameraTargetComponent));
        em.SetName(entity, "TacticalFollowCameraTarget");
        return entity;
    }

    private static Entity EnsurePoseEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraPoseComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(TacticalFollowCameraPoseComponent));
        em.SetName(entity, "TacticalFollowCameraPose");
        return entity;
    }

    private static bool TryGetRequestEntity(EntityManager em, out Entity entity)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadWrite<TacticalFollowCameraRequestQueueComponent>(),
            ComponentType.ReadWrite<TacticalFollowCameraRequestElement>());
        if (query.IsEmptyIgnoreFilter)
        {
            entity = Entity.Null;
            return false;
        }

        entity = query.GetSingletonEntity();
        return true;
    }

    private static Entity EnsureModeEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraModeComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(TacticalFollowCameraModeComponent));
        em.SetName(entity, "TacticalFollowCameraMode");
        return entity;
    }

    private static Entity EnsureUiReadModelEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraUiReadModelComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(TacticalFollowCameraUiReadModelComponent));
        em.SetName(entity, "TacticalFollowCameraUiReadModel");
        em.SetComponentData(entity, new TacticalFollowCameraUiReadModelComponent
        {
            Visible = 1,
            ReasonCode = (int)TacticalCommandReasonCode.NoSelection
        });
        return entity;
    }

    private static bool IsModeRequest(TacticalFollowCameraRequestKind kind)
    {
        return kind == TacticalFollowCameraRequestKind.ToggleFollowMode ||
               kind == TacticalFollowCameraRequestKind.ExitFollowMode ||
               kind == TacticalFollowCameraRequestKind.SetBaseTarget ||
               kind == TacticalFollowCameraRequestKind.RefreshBaseTarget;
    }
}
