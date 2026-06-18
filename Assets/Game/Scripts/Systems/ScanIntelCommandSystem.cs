using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct ScanIntelCommandSystem : ISystem
{
    public const int DefaultScanRadiusCells = 12;
    public const int DefaultCombatUnitScanRadiusCells = 6;
    public const float DefaultSelectedUnitScanDurationSeconds = 8f;

    public readonly struct Result
    {
        public readonly TacticalCommandResult CommandResult;
        public readonly int2 CenterCell;
        public readonly float3 CenterWorld;
        public readonly int RadiusCells;
        public readonly int RevealedCount;
        public readonly Entity SourceEntity;
        public readonly bool HasWorldPosition;
        public readonly bool HasSourceEntity;
        public readonly bool DeferredToSource;

        private Result(
            TacticalCommandResult commandResult,
            int2 centerCell,
            float3 centerWorld,
            int radiusCells,
            int revealedCount,
            Entity sourceEntity,
            bool hasWorldPosition,
            bool hasSourceEntity,
            bool deferredToSource)
        {
            CommandResult = commandResult;
            CenterCell = centerCell;
            CenterWorld = centerWorld;
            RadiusCells = radiusCells;
            RevealedCount = revealedCount;
            SourceEntity = sourceEntity;
            HasWorldPosition = hasWorldPosition;
            HasSourceEntity = hasSourceEntity;
            DeferredToSource = deferredToSource;
        }

        public static Result Success(
            int2 centerCell,
            float3 centerWorld,
            int radiusCells,
            int revealedCount,
            Entity sourceEntity = default,
            bool hasSourceEntity = false,
            bool deferredToSource = false)
        {
            return new Result(
                TacticalCommandResult.Success(),
                centerCell,
                centerWorld,
                radiusCells,
                revealedCount,
                sourceEntity,
                true,
                hasSourceEntity,
                deferredToSource);
        }

        public static Result Rejected(TacticalCommandReasonCode reasonCode)
        {
            return new Result(TacticalCommandResult.Rejected(reasonCode), default, default, DefaultScanRadiusCells, 0, default, false, false, false);
        }

        public static Result FromCommandResult(
            TacticalCommandResult commandResult,
            int2 centerCell,
            float3 centerWorld,
            int radiusCells,
            int revealedCount,
            Entity sourceEntity,
            bool hasWorldPosition,
            bool hasSourceEntity,
            bool deferredToSource)
        {
            return new Result(
                commandResult,
                centerCell,
                centerWorld,
                radiusCells,
                revealedCount,
                sourceEntity,
                hasWorldPosition,
                hasSourceEntity,
                deferredToSource);
        }
    }

    private EntityQuery _queueQuery;
    private EntityQuery _commandIntentQueueQuery;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _unitScanTargetQuery;
    private EntityQuery _buildingScanTargetQuery;
    private EntityQuery _feedQueueQuery;
    private EntityTypeHandle _entityType;
    private ComponentTypeHandle<Faction> _factionType;
    private ComponentTypeHandle<UnitGrid> _unitGridType;
    private ComponentTypeHandle<UnitHealth> _healthType;
    private ComponentTypeHandle<LocalTransform> _transformType;
    private ComponentTypeHandle<RuntimeBuildingCombatInfo> _buildingInfoType;

    public void OnCreate(ref SystemState state)
    {
        _queueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<ScanIntelCommandQueueComponent>(),
            ComponentType.ReadWrite<ScanIntelCommandRequestElement>(),
            ComponentType.ReadWrite<ScanIntelCommandResultElement>());
        _commandIntentQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
            ComponentType.ReadWrite<RtsSelectionCommandResultElement>());
        _gridConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _unitScanTargetQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>());
        _buildingScanTargetQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatInfo>());
        _feedQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<ScanIntelFeedQueueTag>(),
            ComponentType.ReadWrite<ScanIntelFeedEntry>());
        _entityType = state.GetEntityTypeHandle();
        _factionType = state.GetComponentTypeHandle<Faction>(true);
        _unitGridType = state.GetComponentTypeHandle<UnitGrid>(true);
        _healthType = state.GetComponentTypeHandle<UnitHealth>(true);
        _transformType = state.GetComponentTypeHandle<LocalTransform>(true);
        _buildingInfoType = state.GetComponentTypeHandle<RuntimeBuildingCombatInfo>(true);
        EnsureCommandEntity(state.EntityManager, _queueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        _entityType.Update(ref state);
        _factionType.Update(ref state);
        _unitGridType.Update(ref state);
        _healthType.Update(ref state);
        _transformType.Update(ref state);
        _buildingInfoType.Update(ref state);

        ProcessPendingRequests(
            state.EntityManager,
            _queueQuery,
            _gridConfigQuery,
            _unitScanTargetQuery,
            _buildingScanTargetQuery,
            _feedQueueQuery,
            _entityType,
            _factionType,
            _unitGridType,
            _healthType,
            _transformType,
            _buildingInfoType);
        ProcessPreResolvedCommandIntentRequests(
            state.EntityManager,
            _commandIntentQueueQuery,
            _gridConfigQuery,
            _unitScanTargetQuery,
            _buildingScanTargetQuery,
            _feedQueueQuery,
            _entityType,
            _factionType,
            _unitGridType,
            _healthType,
            _transformType,
            _buildingInfoType);
    }

    public readonly Result TryIssueScan(
        EntityManager em,
        Vector2 screenPosition,
        int requestId,
        int frame,
        EntityQuery gridConfigQuery,
        SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetClickedCell)
    {
        if (gridConfigQuery.IsEmptyIgnoreFilter)
            return Result.Rejected(TacticalCommandReasonCode.ScanUnavailable);

        if (tryGetClickedCell == null ||
            !tryGetClickedCell(screenPosition, em, out int2 centerCell, out Vector3 centerWorld))
        {
            return Result.Rejected(TacticalCommandReasonCode.TargetOutOfBounds);
        }

        GridConfig grid = em.GetComponentData<GridConfig>(gridConfigQuery.GetSingletonEntity());
        if (!GridUtils.InBounds(centerCell, grid.Width, grid.Height))
            return Result.Rejected(TacticalCommandReasonCode.TargetOutOfBounds);

        bool hasSourceEntity = TryResolveSelectedScanSource(em, out Entity sourceEntity);
        int radiusCells = hasSourceEntity ? ResolveSelectedUnitScanRadiusCells(em, sourceEntity) : DefaultScanRadiusCells;
        EnqueueScan(em, requestId, frame, centerCell, centerWorld, sourceEntity, hasSourceEntity, hasSourceEntity, radiusCells);
        ProcessPendingRequests(em);
        return TryGetResult(em, requestId, out ScanIntelCommandResultElement result)
            ? ToResult(result)
            : Result.Rejected(TacticalCommandReasonCode.ScanUnavailable);
    }

    public readonly bool ProcessCommandIntentRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        EntityQuery gridConfigQuery,
        SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetClickedCell)
    {
        bool handledAny = false;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Scan ||
                IsPreResolvedScanRequest(request))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            handledAny = true;
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            Result result = TryIssueScan(
                em,
                screenPosition,
                request.RequestId,
                request.Frame,
                gridConfigQuery,
                tryGetClickedCell);

            AddCommandResult(em, commandEntity, commandResults, ToCommandResultElement(request, result));
            if (em.Exists(commandEntity))
            {
                if (em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity))
                    commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
                if (em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
                    commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            }
        }

        return handledAny;
    }

    private static bool ProcessPreResolvedCommandIntentRequests(
        EntityManager em,
        EntityQuery commandIntentQueueQuery,
        EntityQuery gridConfigQuery,
        EntityQuery unitScanTargetQuery,
        EntityQuery buildingScanTargetQuery,
        EntityQuery feedQueueQuery,
        EntityTypeHandle entityType,
        ComponentTypeHandle<Faction> factionType,
        ComponentTypeHandle<UnitGrid> unitGridType,
        ComponentTypeHandle<UnitHealth> healthType,
        ComponentTypeHandle<LocalTransform> transformType,
        ComponentTypeHandle<RuntimeBuildingCombatInfo> buildingInfoType)
    {
        if (commandIntentQueueQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandIntentQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        bool handledAny = false;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Scan ||
                !IsPreResolvedScanRequest(request))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            handledAny = true;
            ScanIntelCommandRequestElement scanRequest = new()
            {
                RequestId = request.RequestId,
                Frame = request.Frame,
                SourceEntity = request.SourceEntity,
                CenterCell = request.TargetCell,
                CenterWorld = request.WorldPosition,
                RadiusCells = DefaultScanRadiusCells,
                HasWorldPosition = request.HasWorldPosition,
                HasSourceEntity = request.HasSourceEntity,
                DeferRevealUntilSourceArrives = request.HasSourceEntity
            };
            if (scanRequest.HasSourceEntity != 0 && IsValidScanSource(em, scanRequest.SourceEntity))
            {
                scanRequest.RadiusCells = ResolveSelectedUnitScanRadiusCells(em, scanRequest.SourceEntity);
            }
            else if (TryResolveSelectedScanSource(em, out Entity selectedScanSource))
            {
                scanRequest.SourceEntity = selectedScanSource;
                scanRequest.HasSourceEntity = 1;
                scanRequest.DeferRevealUntilSourceArrives = 1;
                scanRequest.RadiusCells = ResolveSelectedUnitScanRadiusCells(em, selectedScanSource);
            }

            TacticalCommandResult commandResult = TryApplyScan(
                em,
                gridConfigQuery,
                unitScanTargetQuery,
                buildingScanTargetQuery,
                feedQueueQuery,
                entityType,
                factionType,
                unitGridType,
                healthType,
                transformType,
                buildingInfoType,
                scanRequest,
                out int2 centerCell,
                out float3 centerWorld,
                out int radiusCells,
                out int revealedCount,
                out Entity sourceEntity,
                out bool hasWorldPosition,
                out bool hasSourceEntity,
                out bool deferredToSource);

            Result result = Result.FromCommandResult(
                commandResult,
                centerCell,
                centerWorld,
                radiusCells,
                revealedCount,
                sourceEntity,
                hasWorldPosition,
                hasSourceEntity,
                deferredToSource);
            AddCommandResult(em, commandEntity, commandResults, ToCommandResultElement(request, result));
            if (em.Exists(commandEntity))
            {
                if (em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity))
                    commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
                if (em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
                    commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            }
        }

        return handledAny;
    }

    private static bool IsPreResolvedScanRequest(RtsSelectionCommandIntentRequestElement request)
    {
        return request.HasTargetCell != 0 && request.HasWorldPosition != 0;
    }

    public static int EnqueueScan(
        EntityManager em,
        int requestId,
        int frame,
        int2 centerCell,
        float3 centerWorld)
    {
        return EnqueueScan(em, requestId, frame, centerCell, centerWorld, Entity.Null, false, false);
    }

    public static int EnqueueScan(
        EntityManager em,
        int requestId,
        int frame,
        int2 centerCell,
        float3 centerWorld,
        Entity sourceEntity,
        bool hasSourceEntity,
        bool deferRevealUntilSourceArrives,
        int radiusCells = DefaultScanRadiusCells)
    {
        Entity queueEntity = EnsureCommandEntity(em);
        ScanIntelCommandQueueComponent queue = em.GetComponentData<ScanIntelCommandQueueComponent>(queueEntity);
        queue.LastRequestId = math.max(queue.LastRequestId, requestId);
        em.SetComponentData(queueEntity, queue);

        em.GetBuffer<ScanIntelCommandRequestElement>(queueEntity).Add(new ScanIntelCommandRequestElement
        {
            RequestId = requestId,
            Frame = frame,
            SourceEntity = sourceEntity,
            CenterCell = centerCell,
            CenterWorld = centerWorld,
            RadiusCells = radiusCells,
            HasWorldPosition = 1,
            HasSourceEntity = hasSourceEntity ? (byte)1 : (byte)0,
            DeferRevealUntilSourceArrives = deferRevealUntilSourceArrives ? (byte)1 : (byte)0
        });
        return requestId;
    }

    public static void ProcessPendingRequests(EntityManager em)
    {
        using EntityQuery queueQuery = em.CreateEntityQuery(ComponentType.ReadOnly<ScanIntelCommandQueueComponent>());
        using EntityQuery gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        using EntityQuery unitScanTargetQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>());
        using EntityQuery buildingScanTargetQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatInfo>());
        using EntityQuery feedQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<ScanIntelFeedQueueTag>(),
            ComponentType.ReadWrite<ScanIntelFeedEntry>());

        ProcessPendingRequests(
            em,
            queueQuery,
            gridConfigQuery,
            unitScanTargetQuery,
            buildingScanTargetQuery,
            feedQueueQuery,
            em.GetEntityTypeHandle(),
            em.GetComponentTypeHandle<Faction>(true),
            em.GetComponentTypeHandle<UnitGrid>(true),
            em.GetComponentTypeHandle<UnitHealth>(true),
            em.GetComponentTypeHandle<LocalTransform>(true),
            em.GetComponentTypeHandle<RuntimeBuildingCombatInfo>(true));
    }

    private static void ProcessPendingRequests(
        EntityManager em,
        EntityQuery queueQuery,
        EntityQuery gridConfigQuery,
        EntityQuery unitScanTargetQuery,
        EntityQuery buildingScanTargetQuery,
        EntityQuery feedQueueQuery,
        EntityTypeHandle entityType,
        ComponentTypeHandle<Faction> factionType,
        ComponentTypeHandle<UnitGrid> unitGridType,
        ComponentTypeHandle<UnitHealth> healthType,
        ComponentTypeHandle<LocalTransform> transformType,
        ComponentTypeHandle<RuntimeBuildingCombatInfo> buildingInfoType)
    {
        Entity queueEntity = EnsureCommandEntity(em, queueQuery);
        DynamicBuffer<ScanIntelCommandRequestElement> requests = em.GetBuffer<ScanIntelCommandRequestElement>(queueEntity);
        if (requests.Length == 0)
            return;

        using NativeList<ScanIntelCommandRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
        for (int i = 0; i < requests.Length; i++)
            pendingRequests.Add(requests[i]);
        requests.Clear();

        DynamicBuffer<ScanIntelCommandResultElement> results = em.GetBuffer<ScanIntelCommandResultElement>(queueEntity);
        results.Clear();
        NativeArray<ScanIntelCommandRequestElement> pendingRequestArray = pendingRequests.AsArray();
        for (int i = 0; i < pendingRequestArray.Length; i++)
        {
            ScanIntelCommandRequestElement request = pendingRequestArray[i];
            TacticalCommandResult commandResult = TryApplyScan(
                em,
                gridConfigQuery,
                unitScanTargetQuery,
                buildingScanTargetQuery,
                feedQueueQuery,
                entityType,
                factionType,
                unitGridType,
                healthType,
                transformType,
                buildingInfoType,
                request,
                out int2 centerCell,
                out float3 centerWorld,
                out int radiusCells,
                out int revealedCount,
                out Entity sourceEntity,
                out bool hasWorldPosition,
                out bool hasSourceEntity,
                out bool deferredToSource);

            if (!em.Exists(queueEntity) || !em.HasBuffer<ScanIntelCommandResultElement>(queueEntity))
                continue;

            results = em.GetBuffer<ScanIntelCommandResultElement>(queueEntity);
            results.Add(new ScanIntelCommandResultElement
            {
                RequestId = request.RequestId,
                Frame = request.Frame,
                SourceEntity = sourceEntity,
                CenterCell = centerCell,
                CenterWorld = centerWorld,
                RadiusCells = radiusCells,
                RevealedCount = revealedCount,
                ReasonCode = (int)commandResult.ReasonCode,
                Accepted = commandResult.Accepted ? (byte)1 : (byte)0,
                HasWorldPosition = hasWorldPosition ? (byte)1 : (byte)0,
                HasSourceEntity = hasSourceEntity ? (byte)1 : (byte)0,
                DeferredToSource = deferredToSource ? (byte)1 : (byte)0
            });
        }
    }

    private static TacticalCommandResult TryApplyScan(
        EntityManager em,
        EntityQuery gridConfigQuery,
        EntityQuery unitScanTargetQuery,
        EntityQuery buildingScanTargetQuery,
        EntityQuery feedQueueQuery,
        EntityTypeHandle entityType,
        ComponentTypeHandle<Faction> factionType,
        ComponentTypeHandle<UnitGrid> unitGridType,
        ComponentTypeHandle<UnitHealth> healthType,
        ComponentTypeHandle<LocalTransform> transformType,
        ComponentTypeHandle<RuntimeBuildingCombatInfo> buildingInfoType,
        ScanIntelCommandRequestElement request,
        out int2 centerCell,
        out float3 centerWorld,
        out int radiusCells,
        out int revealedCount,
        out Entity sourceEntity,
        out bool hasWorldPosition,
        out bool hasSourceEntity,
        out bool deferredToSource)
    {
        centerCell = request.CenterCell;
        centerWorld = request.CenterWorld;
        radiusCells = request.RadiusCells > 0 ? request.RadiusCells : DefaultScanRadiusCells;
        revealedCount = 0;
        sourceEntity = request.SourceEntity;
        hasWorldPosition = request.HasWorldPosition != 0;
        hasSourceEntity = request.HasSourceEntity != 0 && IsValidScanSource(em, request.SourceEntity);
        deferredToSource = false;

        SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
            $"scanApply begin request={request.RequestId} cell={centerCell} hasWorld={hasWorldPosition} " +
            $"requestedSource={request.SourceEntity} hasValidSource={hasSourceEntity} deferRequested={request.DeferRevealUntilSourceArrives} frame={request.Frame}");

        if (gridConfigQuery.IsEmptyIgnoreFilter)
        {
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanApply rejected request={request.RequestId} reason=NoGridConfig frame={request.Frame}");
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.ScanUnavailable);
        }

        Entity gridEntity = gridConfigQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        if (!GridUtils.InBounds(centerCell, grid.Width, grid.Height))
        {
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanApply rejected request={request.RequestId} reason=TargetOutOfBounds cell={centerCell} grid={grid.Width}x{grid.Height} frame={request.Frame}");
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetOutOfBounds);
        }

        if (!hasWorldPosition)
        {
            centerWorld = GridUtils.CellToWorldCenter(grid, centerCell);
            hasWorldPosition = true;
        }

        if (request.HasSourceEntity != 0 && !hasSourceEntity)
        {
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanApply rejected request={request.RequestId} reason=InvalidSource source={request.SourceEntity} frame={request.Frame}");
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.ScanUnavailable);
        }

        if (hasSourceEntity && request.DeferRevealUntilSourceArrives != 0)
        {
            IssueSelectedUnitScanOrder(em, sourceEntity, request.RequestId, request.Frame, centerCell, centerWorld, radiusCells);
            deferredToSource = true;
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                $"scanApply deferredToSource request={request.RequestId} source={sourceEntity} cell={centerCell} radius={radiusCells} frame={request.Frame}");
            return TacticalCommandResult.Success();
        }

        int expectedCandidates = math.max(
            1,
            (unitScanTargetQuery.IsEmptyIgnoreFilter ? 0 : unitScanTargetQuery.CalculateEntityCount()) +
            (buildingScanTargetQuery.IsEmptyIgnoreFilter ? 0 : buildingScanTargetQuery.CalculateEntityCount()));
        using NativeList<ScanRevealCandidate> candidates = new(expectedCandidates, Allocator.Temp);
        CollectRevealUnits(
            unitScanTargetQuery,
            entityType,
            factionType,
            unitGridType,
            healthType,
            transformType,
            buildingInfoType,
            grid,
            centerCell,
            radiusCells,
            candidates);
        CollectRevealBuildings(
            buildingScanTargetQuery,
            entityType,
            factionType,
            buildingInfoType,
            healthType,
            transformType,
            grid,
            centerCell,
            radiusCells,
            candidates);
        for (int i = 0; i < candidates.Length; i++)
        {
            ScanRevealCandidate candidate = candidates[i];
            RevealEntity(em, candidate.Entity, candidate.Cell, candidate.Position, request.Frame);
        }

        revealedCount = candidates.Length;
        SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
            $"scanApply revealed request={request.RequestId} count={revealedCount} cell={centerCell} radius={radiusCells} frame={request.Frame}");
        AppendFeedEntry(
            em,
            feedQueueQuery,
            request.RequestId,
            request.Frame,
            sourceEntity,
            hasSourceEntity,
            centerCell,
            centerWorld,
            radiusCells,
            revealedCount);

        return TacticalCommandResult.Success();
    }

    private static bool TryGetResult(EntityManager em, int requestId, out ScanIntelCommandResultElement result)
    {
        result = default;
        Entity queueEntity = EnsureCommandEntity(em);
        DynamicBuffer<ScanIntelCommandResultElement> results = em.GetBuffer<ScanIntelCommandResultElement>(queueEntity);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].RequestId == requestId)
            {
                result = results[i];
                return true;
            }
        }

        return false;
    }

    private static Result ToResult(ScanIntelCommandResultElement result)
    {
        return result.Accepted != 0
            ? Result.Success(
                result.CenterCell,
                result.CenterWorld,
                result.RadiusCells,
                result.RevealedCount,
                result.SourceEntity,
                result.HasSourceEntity != 0,
                result.DeferredToSource != 0)
            : Result.Rejected((TacticalCommandReasonCode)result.ReasonCode);
    }

    private static void AddCommandResult(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandResultElement> fallbackResults,
        RtsSelectionCommandResultElement result)
    {
        if (commandEntity != Entity.Null && em.Exists(commandEntity) && em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
        {
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Add(result);
            return;
        }

        fallbackResults.Add(result);
    }

    private static RtsSelectionCommandResultElement ToCommandResultElement(
        RtsSelectionCommandIntentRequestElement request,
        Result result)
    {
        TacticalCommandResult commandResult = result.CommandResult;
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            SourceEntity = result.SourceEntity,
            TargetCell = result.CenterCell,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.CenterWorld,
            TargetKind = commandResult.Accepted
                ? RtsSelectionCommandTargetKind.Cell
                : RtsSelectionCommandTargetKind.None,
            CommandMode = (int)TacticalCommandMode.Scan,
            HasCommandResult = 1,
            Accepted = commandResult.Accepted ? (byte)1 : (byte)0,
            ReasonCode = (int)commandResult.ReasonCode,
            FeedbackLifetime = RtsSelectionCommandFeedbackLifetime.Transient,
            EmitScreenMarker = commandResult.Accepted ? (byte)1 : (byte)0,
            HasSourceEntity = result.HasSourceEntity ? (byte)1 : (byte)0,
            DeferredToSource = result.DeferredToSource ? (byte)1 : (byte)0,
            HasTargetCell = commandResult.Accepted ? (byte)1 : (byte)0,
            HasWorldPosition = result.HasWorldPosition ? (byte)1 : (byte)0,
            ShowWorldMarkers = commandResult.Accepted ? (byte)1 : (byte)0,
            RevealedCount = result.RevealedCount,
            RadiusCells = result.RadiusCells
        };
    }

    private static Entity EnsureCommandEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<ScanIntelCommandQueueComponent>());
        return EnsureCommandEntity(em, query);
    }

    private static Entity EnsureCommandEntity(EntityManager em, EntityQuery query)
    {
        Entity entity;
        if (!query.IsEmptyIgnoreFilter)
        {
            entity = query.GetSingletonEntity();
            EnsureBuffers(em, entity);
            return entity;
        }

        entity = em.CreateEntity(typeof(ScanIntelCommandQueueComponent));
        em.SetName(entity, "ScanIntelCommands");
        EnsureBuffers(em, entity);
        return entity;
    }

    private static void EnsureBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<ScanIntelCommandRequestElement>(entity))
            em.AddBuffer<ScanIntelCommandRequestElement>(entity);
        if (!em.HasBuffer<ScanIntelCommandResultElement>(entity))
            em.AddBuffer<ScanIntelCommandResultElement>(entity);
    }

    private static void CollectRevealUnits(
        EntityQuery unitScanTargetQuery,
        EntityTypeHandle entityType,
        ComponentTypeHandle<Faction> factionType,
        ComponentTypeHandle<UnitGrid> unitGridType,
        ComponentTypeHandle<UnitHealth> healthType,
        ComponentTypeHandle<LocalTransform> transformType,
        ComponentTypeHandle<RuntimeBuildingCombatInfo> buildingInfoType,
        in GridConfig grid,
        int2 centerCell,
        int radiusCells,
        NativeList<ScanRevealCandidate> candidates)
    {
        if (unitScanTargetQuery.IsEmptyIgnoreFilter)
            return;

        using NativeArray<ArchetypeChunk> chunks = unitScanTargetQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            if (chunk.Has(ref buildingInfoType))
                continue;

            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
            NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
            bool hasHealth = chunk.Has(ref healthType);
            bool hasTransform = chunk.Has(ref transformType);
            NativeArray<UnitHealth> healths = hasHealth
                ? chunk.GetNativeArray(ref healthType)
                : default;
            NativeArray<LocalTransform> transforms = hasTransform
                ? chunk.GetNativeArray(ref transformType)
                : default;
            for (int i = 0; i < chunk.Count; i++)
            {
                if (!IsRevealableScanTarget(factions[i], hasHealth, hasHealth ? healths[i] : default))
                    continue;

                int2 cell = unitGrids[i].Cell;
                if (ChebyshevDistance(centerCell, cell) > radiusCells)
                    continue;

                float3 position = hasTransform
                    ? transforms[i].Position
                    : GridUtils.CellToWorldCenter(grid, cell);
                candidates.Add(new ScanRevealCandidate(entities[i], cell, position));
            }
        }
    }

    private static void CollectRevealBuildings(
        EntityQuery buildingScanTargetQuery,
        EntityTypeHandle entityType,
        ComponentTypeHandle<Faction> factionType,
        ComponentTypeHandle<RuntimeBuildingCombatInfo> buildingInfoType,
        ComponentTypeHandle<UnitHealth> healthType,
        ComponentTypeHandle<LocalTransform> transformType,
        in GridConfig grid,
        int2 centerCell,
        int radiusCells,
        NativeList<ScanRevealCandidate> candidates)
    {
        if (buildingScanTargetQuery.IsEmptyIgnoreFilter)
            return;

        using NativeArray<ArchetypeChunk> chunks = buildingScanTargetQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
            NativeArray<RuntimeBuildingCombatInfo> buildings = chunk.GetNativeArray(ref buildingInfoType);
            bool hasHealth = chunk.Has(ref healthType);
            bool hasTransform = chunk.Has(ref transformType);
            NativeArray<UnitHealth> healths = hasHealth
                ? chunk.GetNativeArray(ref healthType)
                : default;
            NativeArray<LocalTransform> transforms = hasTransform
                ? chunk.GetNativeArray(ref transformType)
                : default;
            for (int i = 0; i < chunk.Count; i++)
            {
                if (!IsRevealableScanTarget(factions[i], hasHealth, hasHealth ? healths[i] : default))
                    continue;

                RuntimeBuildingCombatInfo building = buildings[i];
                if (DistanceToFootprint(centerCell, building.OriginCell, building.FootprintCells) > radiusCells)
                    continue;

                int2 center = building.OriginCell + math.max(new int2(1, 1), building.FootprintCells) / 2;
                float3 position = hasTransform
                    ? transforms[i].Position
                    : GridUtils.CellToWorldCenter(grid, center);
                candidates.Add(new ScanRevealCandidate(entities[i], center, position));
            }
        }
    }

    private static bool IsRevealableScanTarget(Faction faction, bool hasHealth, UnitHealth health)
    {
        if (hasHealth && health.Current <= 0)
            return false;

        return FactionIdentity.IsHostileToPlayer(faction.Id);
    }

    private static bool TryResolveSelectedScanSource(EntityManager em, out Entity sourceEntity)
    {
        sourceEntity = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        if (query.IsEmptyIgnoreFilter)
        {
            SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace("scanSourceResolve result=False reason=NoSelectedUnits");
            return false;
        }

        int selectedCount = query.CalculateEntityCount();
        int candidateIndex = 0;
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> selectedEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < selectedEntities.Length; i++, candidateIndex++)
            {
                Entity candidate = selectedEntities[i];
                if (!IsValidScanSource(em, candidate))
                {
                    SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                        $"scanSourceResolve candidateRejected index={candidateIndex} entity={candidate}");
                    continue;
                }

                sourceEntity = candidate;
                SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
                    $"scanSourceResolve result=True index={candidateIndex} entity={candidate} selectedCount={selectedCount}");
                return true;
            }
        }

        SelectionRuntimeDiagnosticsSystem.LogScanCommandTrace(
            $"scanSourceResolve result=False reason=NoScanCapableSelected selectedCount={selectedCount}");
        return false;
    }

    private static bool IsValidScanSource(EntityManager em, Entity sourceEntity)
    {
        if (sourceEntity == Entity.Null ||
            !em.Exists(sourceEntity) ||
            !em.HasComponent<Faction>(sourceEntity) ||
            !FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(sourceEntity).Id) ||
            !em.HasComponent<UnitGrid>(sourceEntity) ||
            !em.HasComponent<UnitMove>(sourceEntity) ||
            em.HasComponent<Disabled>(sourceEntity) ||
            em.HasComponent<UnitDeathAnimationComponent>(sourceEntity) ||
            em.HasComponent<UnitTransportPassenger>(sourceEntity))
        {
            return false;
        }

        if (em.HasComponent<UnitHealth>(sourceEntity) &&
            em.GetComponentData<UnitHealth>(sourceEntity).Current <= 0)
        {
            return false;
        }

        return SelectionUiReadModelLookup.IsSelectedUnitScanCapable(em, sourceEntity);
    }

    private static int ResolveSelectedUnitScanRadiusCells(EntityManager em, Entity sourceEntity)
    {
        return SelectionUiReadModelLookup.IsSelectedUnitScanSpecialist(em, sourceEntity)
            ? DefaultScanRadiusCells
            : DefaultCombatUnitScanRadiusCells;
    }

    private static void IssueSelectedUnitScanOrder(
        EntityManager em,
        Entity sourceEntity,
        int requestId,
        int frame,
        int2 centerCell,
        float3 centerWorld,
        int radiusCells)
    {
        new UnitMoveOrderSystem().IssueImmediateMoveCommand(em, sourceEntity, centerCell);

        UnitScanOrder scanOrder = new()
        {
            RequestId = requestId,
            StartedFrame = frame,
            SourceEntity = sourceEntity,
            CenterCell = centerCell,
            CenterWorld = centerWorld,
            RadiusCells = math.max(1, radiusCells),
            StartedTimeSeconds = 0f,
            NextRevealTimeSeconds = 0f,
            NextPatrolMoveTimeSeconds = 0f,
            DurationSeconds = DefaultSelectedUnitScanDurationSeconds,
            PatrolWaypointIndex = 0,
            EngageDetectedTargets = 1,
            ReturnHomeAfterCompletion = em.HasComponent<UnitAirMovement>(sourceEntity) ? (byte)1 : (byte)0,
            HasStarted = 0
        };

        if (em.HasComponent<UnitScanOrder>(sourceEntity))
            em.SetComponentData(sourceEntity, scanOrder);
        else
            em.AddComponentData(sourceEntity, scanOrder);
    }

    private static void RevealEntity(EntityManager em, Entity entity, int2 cell, float3 position, int frame)
    {
        if (!em.HasComponent<ScanIntelRevealedTag>(entity))
            em.AddComponent<ScanIntelRevealedTag>(entity);

        ScanIntelLastSeen lastSeen = new()
        {
            Cell = cell,
            Position = position,
            LastScanFrame = frame,
            FactionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0
        };

        if (em.HasComponent<ScanIntelLastSeen>(entity))
            em.SetComponentData(entity, lastSeen);
        else
            em.AddComponentData(entity, lastSeen);
    }

    private static void AppendFeedEntry(
        EntityManager em,
        EntityQuery feedQueueQuery,
        int requestId,
        int frame,
        Entity sourceEntity,
        bool hasSourceEntity,
        int2 centerCell,
        float3 centerWorld,
        int radiusCells,
        int revealedCount)
    {
        Entity feedEntity = EnsureFeedQueue(em, feedQueueQuery);
        DynamicBuffer<ScanIntelFeedEntry> feed = em.GetBuffer<ScanIntelFeedEntry>(feedEntity);
        feed.Add(new ScanIntelFeedEntry
        {
            RequestId = requestId,
            Frame = frame,
            SourceEntity = sourceEntity,
            CenterCell = centerCell,
            CenterWorld = centerWorld,
            RadiusCells = radiusCells,
            RevealedCount = revealedCount,
            HasSourceEntity = hasSourceEntity ? (byte)1 : (byte)0
        });
    }

    private static Entity EnsureFeedQueue(EntityManager em, EntityQuery feedQueueQuery)
    {
        if (!feedQueueQuery.IsEmptyIgnoreFilter)
            return feedQueueQuery.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(ScanIntelFeedQueueTag));
        em.SetName(entity, "ScanIntelFeedQueue");
        em.AddBuffer<ScanIntelFeedEntry>(entity);
        return entity;
    }

    private static int ChebyshevDistance(int2 a, int2 b)
    {
        int2 delta = math.abs(a - b);
        return math.max(delta.x, delta.y);
    }

    private static int DistanceToFootprint(int2 cell, int2 origin, int2 footprint)
    {
        int2 size = math.max(new int2(1, 1), footprint);
        int minX = origin.x;
        int minY = origin.y;
        int maxX = origin.x + size.x - 1;
        int maxY = origin.y + size.y - 1;
        int dx = cell.x < minX ? minX - cell.x : (cell.x > maxX ? cell.x - maxX : 0);
        int dy = cell.y < minY ? minY - cell.y : (cell.y > maxY ? cell.y - maxY : 0);
        return math.max(dx, dy);
    }

    private readonly struct ScanRevealCandidate
    {
        public ScanRevealCandidate(Entity entity, int2 cell, float3 position)
        {
            Entity = entity;
            Cell = cell;
            Position = position;
        }

        public readonly Entity Entity;
        public readonly int2 Cell;
        public readonly float3 Position;
    }
}
